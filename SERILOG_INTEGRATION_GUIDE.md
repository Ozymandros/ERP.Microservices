# Centralized Serilog Logging Integration Guide

## Project Status

### ✅ Completed Components

1. **LoggerSetup.cs** - Centralized configuration class in `MyApp.Shared.Infrastructure.Logging`
   - Configures log enrichers (MachineName, ThreadId, EnvironmentName, ApplicationName)
   - Reads logging configuration from `appsettings.json` (Logging and Serilog sections)
   - Supports Console sink with compact/JSON formatting
   - Supports File sink with rolling daily logs
   - Supports custom log levels per namespace

2. **SerilogExtensions.cs** - Structured logging helper methods
   - `LogOperation()` - Logs operation entry/exit with automatic timing
   - `LogDebugWithContext()`, `LogInfoWithContext()`, `LogWarningWithContext()`, `LogErrorWithContext()`, `LogFatalWithContext()`
   - `LogExceptionWithContext()` - Log exceptions with structured properties
   - `PushCorrelationId()` - Manual correlation ID management
   - `PushCorrelationIdFromHttpContext()` - Extract from HTTP headers

3. **CorrelationIdMiddleware.cs** - Automatic correlation ID tracking
   - Extracts correlation ID from `X-Correlation-ID` header
   - Falls back to trace ID if not provided
   - Adds to response headers for client tracking
   - Pushes to Serilog context for all logs within the request

4. **NuGet Packages** - Added to `MyApp.Shared.Infrastructure.csproj`
   - Serilog 4.2.0
   - Serilog.AspNetCore 9.0.0
   - Serilog.Sinks.Console 6.1.0
   - Serilog.Sinks.File 5.0.0
   - Serilog.Sinks.ApplicationInsights 4.1.1
   - Serilog.Enrichers.Environment 3.0.0
   - Serilog.Enrichers.Thread 3.2.0
   - Serilog.Enrichers.Process 3.1.0

5. **Documentation**
   - LOGGING_DOCUMENTATION.md - Complete logging reference
   - SERILOG_SETUP_EXAMPLE.cs - Program.cs template for all APIs

### 📋 Implementation Checklist - Next Steps

Each microservice API needs to be updated with the following changes to their `Program.cs`:

#### Step 1: Add Using Directives
```csharp
using Serilog;
using MyApp.Shared.Infrastructure.Logging;
using MyApp.Shared.Infrastructure.Middleware;
```

#### Step 2: Configure Serilog (right after builder creation)
```csharp
var builder = WebApplication.CreateBuilder(args);

// Configure Serilog BEFORE building
builder.Host.UseSerilog((context, config) =>
    LoggerSetup.Configure(config, context.Configuration, context.HostingEnvironment));

// Handle unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Log.Fatal(args.ExceptionObject as Exception, "Application terminated unexpectedly");
};
```

#### Step 3: Add Middleware (after `app = builder.Build()`)
```csharp
var app = builder.Build();

// Add correlation ID middleware EARLY in the pipeline
app.UseCorrelationIdMiddleware();

// ... rest of middleware chain
```

#### Step 4: Graceful Shutdown (before `app.Run()`)
```csharp
app.Lifetime.ApplicationStopping.Register(async () =>
{
    Log.Information("Application shutting down...");
    await LoggerSetup.CloseAndFlushAsync();
});

app.Run();
```

#### Step 5: Add Try-Catch (optional, at application level)
```csharp
try
{
    // var builder = ...
    // app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
```

### 🔧 Integration for Each API

#### MyApp.Orders.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with controller actions
- [ ] Verify logs in console and file

#### MyApp.Inventory.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with warehouse operations
- [ ] Verify correlation IDs in logs

#### MyApp.Purchasing.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with supplier management
- [ ] Verify file rotation (daily logs)

#### MyApp.Sales.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with sales operations
- [ ] Verify different log levels

#### MyApp.Billing.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with invoice operations
- [ ] Verify async file writes

#### MyApp.Notification.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with background services
- [ ] Verify long-running operation timing

#### MyApp.Auth.API
- [ ] Update Program.cs with Serilog configuration
- [ ] Add CorrelationIdMiddleware
- [ ] Test logging with authentication flows
- [ ] Verify sensitive data is NOT logged

### 🎯 Configuration (appsettings.json)

Ensure each API's `appsettings.json` contains:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "MyApp": "Information"
    }
  },
  "Serilog": {
    "Sinks": {
      "Console": {
        "Enabled": true,
        "Format": "compact"
      },
      "File": {
        "Enabled": true,
        "Path": "logs/app-.log",
        "RetainedFileCountLimit": 30
      }
    }
  }
}
```

### 📊 Logging in Controllers and Services

**Before (Default Logging):**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetOrder(int id)
{
    var order = await _orderService.GetOrderAsync(id);
    if (order == null)
        return NotFound();
    return Ok(order);
}
```

**After (Structured Serilog Logging):**
```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetOrder(int id)
{
    using var operation = _logger.LogOperation("GetOrder", new { orderId = id });
    
    try
    {
        _logger.LogDebugWithContext(
            "Fetching order from database",
            ("OrderId", id),
            ("UserId", User.FindFirst("sub")?.Value));
        
        var order = await _orderService.GetOrderAsync(id);
        
        if (order == null)
        {
            _logger.LogWarningWithContext(
                "Order not found",
                ("OrderId", id));
            return NotFound();
        }

        _logger.LogInformation("Order retrieved successfully: {OrderId}", id);
        return Ok(order);
    }
    catch (Exception ex)
    {
        _logger.LogErrorWithContext(
            "Error retrieving order",
            ex,
            ("OrderId", id));
        return StatusCode(500);
    }
}
```

### 🧪 Testing Logging

**Test Case 1: Console Output**
```bash
# Run API and check console for structured logs
dotnet run
```
Expected output:
```
[10:23:45 INF] [MyApp.Orders.Controllers.OrdersController] Order retrieved successfully
[10:23:46 DBG] [MyApp.Orders.Services.OrderService] Processing order OrderId=123
```

**Test Case 2: File Output**
```bash
# Check log files
ls -la logs/
cat logs/app-20250120.log
```
Expected: Detailed JSON-like structured logs in daily rolling files

**Test Case 3: Correlation ID Tracking**
```bash
# Make request with custom header
curl -H "X-Correlation-ID: test-123" http://localhost:5000/api/orders/1

# Check logs for same correlation ID
grep "test-123" logs/app-20250120.log
```
Expected: All related logs contain the same correlation ID

**Test Case 4: Different Log Levels**
Edit `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  }
}
```
Expected: More verbose output including debug messages

### 🚨 Common Issues & Solutions

**Issue: Console.WriteTo not found**
- Ensure `Serilog.Sinks.Console` NuGet package is installed
- Rebuild solution: `dotnet clean && dotnet build`
- Check package version matches Serilog version

**Issue: Logs not appearing in files**
- Verify `logs/` directory exists and has write permissions
- Check `Sinks:File:Enabled` is `true` in appsettings.json
- Ensure application runs with permissions to create files

**Issue: High memory usage**
- Reduce `RetainedFileCountLimit` (e.g., 10 instead of 30)
- Lower log level for verbose namespaces
- Verify async file writing is enabled

**Issue: Performance degradation**
- Increase log level from Debug → Information in production
- Enable `Sinks:File` async writes
- Filter verbose Microsoft namespace logs

### 📦 Deployment Considerations

**Production appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "MyApp": "Information"
    }
  },
  "Serilog": {
    "Sinks": {
      "Console": {
        "Enabled": true,
        "Format": "compact"
      },
      "File": {
        "Enabled": true,
        "Path": "/var/log/myapp/app-.log",
        "RetainedFileCountLimit": 60
      }
    }
  }
}
```

**Environment Variables (Azure/Docker):**
```bash
# Override log paths for containers
SERILOG__SINKS__FILE__PATH=/app/logs/app-.log

# Override log levels
LOGGING__LOGLEVEL__DEFAULT=Information
LOGGING__LOGLEVEL__MYAPP=Debug
```

### 🔐 Security Best Practices

**DO:**
- Log user IDs and correlation IDs
- Log operation names and timing
- Log error messages and stack traces
- Log business context (order ID, customer ID)

**DON'T:**
- ❌ Log passwords or API keys
- ❌ Log credit card numbers
- ❌ Log social security numbers
- ❌ Log personally identifiable information (PII)
- ❌ Log entire request/response bodies

**Example - Secure Logging:**
```csharp
// ❌ WRONG
_logger.LogInformation("User login: {Email} with password {Password}", email, password);

// ✅ CORRECT
_logger.LogInformation("User login attempt", ("Email", email));
```

### 📈 Monitoring & Alerting

Once logs are centralized, set up monitoring:

1. **Log Aggregation** - Use Azure Application Insights or ELK Stack
2. **Alerts** - Set up alerts for Error/Fatal log levels
3. **Dashboards** - Create dashboards for request patterns
4. **Traces** - Use correlation IDs to trace distributed requests
5. **Performance** - Monitor API response times via structured logs

### 📚 References

- [LOGGING_DOCUMENTATION.md](./LOGGING_DOCUMENTATION.md) - Complete logging guide
- [SERILOG_SETUP_EXAMPLE.cs](./SERILOG_SETUP_EXAMPLE.cs) - Program.cs template
- [Serilog GitHub](https://github.com/serilog/serilog)
- [Serilog Wiki](https://github.com/serilog/serilog/wiki)
- [Structured Logging](https://github.com/serilog/serilog/wiki/Structured-Data)

### ✨ Summary

The centralized Serilog logging system is now ready for integration into all microservices. Key benefits:

- ✅ Consistent structured logging across all APIs
- ✅ Automatic enrichment with context (MachineName, ThreadId, EnvironmentName)
- ✅ Correlation ID tracking for distributed request tracing
- ✅ Configurable log levels per namespace
- ✅ Multiple sinks (Console, File) with rolling daily logs
- ✅ Minimal boilerplate in each API (just 3 lines in Program.cs)
- ✅ Extension methods for easy structured logging
- ✅ Production-ready with performance considerations

**Next Step:** Update each of the 6 microservice APIs' Program.cs files with the Serilog configuration.
