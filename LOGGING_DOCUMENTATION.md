# Centralized Serilog Logging System

This document describes the centralized structured logging implementation across the ERP Aspire solution using Serilog.

## Architecture Overview

### Key Components

1. **LoggerSetup.cs** - Centralized configuration for Serilog
2. **SerilogExtensions.cs** - Extension methods for structured logging with context
3. **CorrelationIdMiddleware.cs** - Automatic correlation ID tracking across requests
4. **appsettings.json** - Configuration for log levels, sinks, and output formats

### Logging Levels

| Level | Usage | Threshold |
|-------|-------|-----------|
| **Debug** | Detailed diagnostic information | Development only |
| **Information** | General operational events | Standard production |
| **Warning** | Potentially harmful situations | Important events |
| **Error** | Error events with recovery possible | Application errors |
| **Fatal** | Unrecoverable errors | System failures |

## Configuration

### appsettings.json Structure

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "MyApp.Orders": "Debug"
    }
  },
  "Serilog": {
    "Sinks": {
      "Console": {
        "Enabled": true,
        "Format": "compact",
        "OutputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}"
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

### Configuration Options

**Console Sink:**
- `Enabled`: Whether console logging is active
- `Format`: `"compact"` for text or `"json"` for JSON output
- `OutputTemplate`: Custom output format string

**File Sink:**
- `Enabled`: Whether file logging is active
- `Path`: Rolling log file path (`.log` → `-20250101.log`, etc.)
- `RetainedFileCountLimit`: Number of log files to retain

## Integration in Program.cs

### Basic Setup (All APIs)

```csharp
using Serilog;
using MyApp.Shared.Infrastructure.Logging;
using MyApp.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog before building the app
var logger = LoggerSetup.CreateLogger(builder.Configuration, builder.Environment);
builder.Host.UseSerilog(logger);

// Catch unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
{
    Log.Fatal(args.ExceptionObject as Exception, "Application terminated unexpectedly");
};

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Add correlation ID middleware early in pipeline
app.UseCorrelationIdMiddleware();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Graceful shutdown
app.Lifetime.ApplicationStopping.Register(async () =>
{
    Log.Information("Application shutting down...");
    await LoggerSetup.CloseAndFlushAsync();
});

app.Run();
```

## Usage Examples

### 1. Basic Logging in Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IOrderService _orderService;

    public OrdersController(ILogger<OrdersController> logger, IOrderService orderService)
    {
        _logger = logger;
        _orderService = orderService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        using var operation = _logger.LogOperation("GetOrder", new { orderId = id });
        
        try
        {
            _logger.LogDebugWithContext("Fetching order", ("OrderId", id), ("UserId", User.FindFirst("sub")?.Value));
            
            var order = await _orderService.GetOrderAsync(id);
            if (order == null)
            {
                _logger.LogWarning("Order not found: {OrderId}", id);
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
}
```

### 2. Structured Logging with Context

```csharp
public class OrderService : IOrderService
{
    private readonly ILogger<OrderService> _logger;

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request)
    {
        // Log with structured properties
        using var _ = _logger.LogOperation("CreateOrder", new 
        { 
            CustomerId = request.CustomerId,
            ItemCount = request.Items.Count,
            TotalAmount = request.Items.Sum(x => x.Price * x.Quantity)
        });

        try
        {
            _logger.LogInfoWithContext(
                "Processing new order",
                ("CustomerId", request.CustomerId),
                ("ItemCount", request.Items.Count));

            // Validate
            if (request.Items.Count == 0)
            {
                _logger.LogWarningWithContext(
                    "Order creation failed: no items",
                    ("CustomerId", request.CustomerId),
                    ("Reason", "EmptyItems"));
                throw new InvalidOperationException("Order must contain at least one item");
            }

            var order = await _repository.AddAsync(/* ... */);

            _logger.LogInformation("Order created: {OrderId} for customer {CustomerId}", 
                order.Id, request.CustomerId);

            return order;
        }
        catch (Exception ex)
        {
            _logger.LogExceptionWithContext(
                ex,
                "Failed to create order",
                ("CustomerId", request.CustomerId),
                ("ItemCount", request.Items.Count),
                ("FailureTime", DateTime.UtcNow));
            throw;
        }
    }
}
```

### 3. Correlation ID Tracking

The `CorrelationIdMiddleware` automatically captures correlation IDs from:
- `X-Correlation-ID` request header
- Current Activity ID
- Request trace ID

All logs from a single request automatically include the same `CorrelationId`:

```
[10:23:45 INF] [CorrelationId: 550e8400-e29b-41d4-a716-446655440000] OrdersController GetOrder started
[10:23:45 DBG] [CorrelationId: 550e8400-e29b-41d4-a716-446655440000] Fetching order OrderId=123
[10:23:46 INF] [CorrelationId: 550e8400-e29b-41d4-a716-446655440000] Order retrieved successfully
```

### 4. Background Services/Workers

```csharp
public class OrderProcessingService : IHostedService
{
    private readonly ILogger<OrderProcessingService> _logger;
    private Timer? _timer;

    public OrderProcessingService(ILogger<OrderProcessingService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order processing service started");
        _timer = new Timer(ProcessOrders, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    private void ProcessOrders(object? state)
    {
        using var operation = _logger.LogOperation("ProcessPendingOrders");

        try
        {
            _logger.LogDebugWithContext(
                "Processing batch of orders",
                ("ProcessedAt", DateTime.UtcNow),
                ("ThreadId", Thread.CurrentThread.ManagedThreadId));

            // Process orders...

            _logger.LogInformation("Batch completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithContext(
                "Batch processing failed",
                ex,
                ("FailureTime", DateTime.UtcNow));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Dispose();
        _logger.LogInformation("Order processing service stopped");
        await Task.CompletedTask;
    }
}
```

## Log Enrichment

All logs automatically include:

| Property | Value |
|----------|-------|
| `Timestamp` | When the log was written |
| `Level` | Log level (Debug, Info, Warning, Error, Fatal) |
| `SourceContext` | Fully qualified class name |
| `MachineName` | Computer running the service |
| `ThreadId` | Managed thread ID |
| `EnvironmentName` | Development/Staging/Production |
| `ApplicationName` | Service name |
| `CorrelationId` | Request correlation ID (from middleware) |
| **Custom Properties** | Added via `LogContext.PushProperty()` |

## Output Examples

### Console Output (Compact Format)
```
[10:23:45 INF] [OrdersController] Order retrieved successfully
[10:23:46 DBG] [OrderService] Fetching order OrderId=123
[10:23:47 WRN] [OrderValidator] Order quantity exceeds limit
[10:23:48 ERR] [OrderRepository] Database connection failed
```

### File Output (Detailed Format)
```
2025-01-20 10:23:45.123 [INF] [MyApp.Orders.Controllers.OrdersController] Order retrieved successfully
2025-01-20 10:23:46.456 [DBG] [MyApp.Orders.Services.OrderService] Fetching order OrderId=123 CustomerId=456
2025-01-20 10:23:47.789 [WRN] [MyApp.Orders.Validators.OrderValidator] Order quantity exceeds limit Limit=1000 Requested=1500
2025-01-20 10:23:48.012 [ERR] [MyApp.Orders.Infrastructure.OrderRepository] Database connection failed Exception=SqlException
```

### JSON Output (Structured)
```json
{
  "Timestamp": "2025-01-20T10:23:45.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Order retrieved successfully",
  "Properties": {
    "SourceContext": "MyApp.Orders.Controllers.OrdersController",
    "CorrelationId": "550e8400-e29b-41d4-a716-446655440000",
    "MachineName": "PROD-SERVER-01",
    "EnvironmentName": "Production",
    "OrderId": 123
  }
}
```

##Extension Methods

### LogOperation(message, context)
Logs operation entry/exit with automatic duration tracking.

```csharp
using var _ = logger.LogOperation("FetchInventory", new { warehouseId = 5 });
// ... operation code ...
// Automatically logs: "Operation completed: FetchInventory - Duration: 245ms"
```

### LogDebugWithContext(message, params)
Debug with structured properties.

```csharp
logger.LogDebugWithContext(
    "Processing payment",
    ("PaymentId", paymentId),
    ("Amount", amount),
    ("Currency", "USD"));
```

### LogInfoWithContext(message, params)
Information with structured properties.

```csharp
logger.LogInfoWithContext(
    "Order shipped",
    ("OrderId", orderId),
    ("Carrier", "FedEx"),
    ("TrackingNumber", trackingNumber));
```

### LogWarningWithContext(message, params)
Warning with structured properties.

```csharp
logger.LogWarningWithContext(
    "Inventory low",
    ("ProductId", productId),
    ("CurrentStock", currentStock),
    ("ReorderLevel", reorderLevel));
```

### LogErrorWithContext(message, exception?, params)
Error with optional exception and structured properties.

```csharp
logger.LogErrorWithContext(
    "Order processing failed",
    ex,
    ("OrderId", orderId),
    ("AttemptCount", attemptCount),
    ("LastError", ex.Message));
```

### LogFatalWithContext(message, exception?, params)
Fatal with optional exception and structured properties.

```csharp
logger.LogFatalWithContext(
    "Database unreachable",
    ex,
    ("ConnectionString", obfuscatedConnectionString),
    ("Timeout", timeout));
```

## Implementation Checklist

- [x] Create `LoggerSetup.cs` in `MyApp.Shared.Infrastructure`
- [x] Create `SerilogExtensions.cs` with extension methods
- [x] Create `CorrelationIdMiddleware.cs` for automatic tracking
- [x] Update `MyApp.Shared.Infrastructure.csproj` with Serilog packages
- [ ] Update all API `Program.cs` files to use Serilog
- [ ] Add example `appsettings.json` configuration
- [ ] Test logging across all microservices
- [ ] Configure Application Insights (optional)

## Best Practices

1. **Use structured logging** - Always pass objects/properties rather than string interpolation
2. **Use operation scopes** - Wrap long operations to track duration
3. **Include context** - Add relevant identifiers and business data
4. **Respect log levels** - Don't log errors as warnings or vice versa
5. **Protect sensitive data** - Never log passwords, API keys, PII
6. **Use correlation IDs** - Essential for tracing requests across services
7. **Flush on shutdown** - Always call `LoggerSetup.CloseAndFlushAsync()` in graceful shutdown

## Troubleshooting

### Logs not appearing in file
- Check `logs/` directory exists
- Verify `Sinks:File:Enabled` is `true`
- Check file permissions
- Ensure application has write access to log directory

### Console output too verbose
- Adjust `Logging:LogLevel:Default` in appsettings.json
- Override specific namespaces with higher thresholds
- Use `Format: json` for structured querying

### High memory usage
- Check `RetainedFileCountLimit` setting
- Reduce log level for verbose components
- Enable async file writing

## References

- [Serilog Documentation](https://github.com/serilog/serilog/wiki)
- [Serilog Configuration](https://github.com/serilog/serilog/wiki/Configuration-basics)
- [Structured Logging](https://github.com/serilog/serilog/wiki/Structured-Data)
- [Enrichment](https://github.com/serilog/serilog/wiki/Enrichment)
