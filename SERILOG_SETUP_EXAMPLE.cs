using Serilog;
using MyApp.Shared.Infrastructure.Logging;
using MyApp.Shared.Infrastructure.Middleware;

namespace MyApp.Logging.Example;

/// <summary>
/// Example of how to configure Serilog in any API's Program.cs
/// This template should be copied to all microservice APIs:
/// - MyApp.Orders.API
/// - MyApp.Inventory.API
/// - MyApp.Purchasing.API
/// - MyApp.Sales.API
/// - MyApp.Billing.API
/// - MyApp.Notification.API
/// </summary>
public static class SerilogSetupExample
{
    public static string GetSampleProgramCs()
    {
        return @"
using Microsoft.EntityFrameworkCore;
using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Infrastructure.Logging;
using MyApp.Shared.Infrastructure.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ========== SERILOG CONFIGURATION (ADD THIS BLOCK TO ALL APIS) ==========
    // Configure Serilog before building the app
    builder.Host.UseSerilog((context, config) =>
        LoggerSetup.Configure(config, context.Configuration, context.HostingEnvironment));

    // Log application start
    Log.Information(""Starting {ServiceName} service..."", ""MyApp.Orders.API"");

    // Catch unhandled exceptions
    AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
    {
        Log.Fatal(args.ExceptionObject as Exception, ""Application terminated unexpectedly"");
    };
    // ========== END SERILOG CONFIGURATION ==========

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // JWT Authentication
    builder.Services.AddJwtAuthentication(builder.Configuration);

    // Infrastructure & Application DI
    var ordersDbConnectionString = builder.Configuration.GetConnectionString(""ordersdb"");
    if (string.IsNullOrEmpty(ordersDbConnectionString))
    {
        Log.Fatal(""Connection string 'ordersdb' not found in configuration"");
        throw new InvalidOperationException(""Missing connection string"");
    }

    builder.Services.AddDbContext<OrdersDbContext>(options =>
        options.UseSqlServer(ordersDbConnectionString));

    builder.Services.AddScoped<IOrderRepository, OrderRepository>();
    builder.Services.AddScoped<IOrderLineRepository, OrderLineRepository>();
    builder.Services.AddScoped<IOrderService, OrderService>();
    builder.Services.AddScoped<IPermissionChecker, DaprPermissionChecker>();

    builder.Services.AddAutoMapper(typeof(OrderProfile).Assembly);
    builder.Services.AddCustomHealthChecks(ordersDbConnectionString);

    var app = builder.Build();

    // ========== MIDDLEWARE CONFIGURATION ==========
    // Add correlation ID middleware EARLY in the pipeline
    app.UseCorrelationIdMiddleware();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Map health check endpoint
    app.MapHealthChecks(""/health"", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = ""application/json"";
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                components = report.Entries.Select(e => new { key = e.Key, value = e.Value.Status.ToString() })
            });
            await context.Response.WriteAsync(result);
        }
    });

    // ========== DATABASE INITIALIZATION ==========
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        try
        {
            Log.Information(""Applying database migrations..."");
            dbContext.Database.Migrate();
            Log.Information(""Database migrations completed successfully"");
        }
        catch (Exception ex)
        {
            Log.Error(ex, ""Error applying database migrations"");
            throw;
        }
    }

    // ========== GRACEFUL SHUTDOWN ==========
    app.Lifetime.ApplicationStopping.Register(async () =>
    {
        Log.Information(""Application shutting down..."");
        await LoggerSetup.CloseAndFlushAsync();
    });

    Log.Information(""Starting application..."");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, ""Application terminated unexpectedly"");
}
finally
{
    Log.CloseAndFlush();
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
";
    }
}
