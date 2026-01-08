using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Common Microservice Configuration
// ============================================================================
builder.AddCommonMicroserviceServices(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Orders.API",
    ConnectionStringKey = "ordersdb",
    DbContextType = typeof(OrdersDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true,
    EnableDapr = true,
    EnableOpenTelemetry = true,
    EnableRedisCache = true,
    ConfigureServiceDependencies = services =>
    {
        // Register Orders-specific repositories
        services.AddScoped<MyApp.Orders.Domain.IOrderRepository, 
            MyApp.Orders.Infrastructure.Repositories.OrderRepository>();
        services.AddScoped<MyApp.Orders.Domain.IOrderLineRepository, 
            MyApp.Orders.Infrastructure.Repositories.OrderLineRepository>();

        // Register Orders-specific services
        services.AddScoped<MyApp.Orders.Application.Contracts.IOrderService, 
            MyApp.Orders.Application.Services.OrderService>();
    }
});

// Redis Cache (Aspire-managed)
builder.AddRedisDistributedCache("cache");

// AutoMapper (service-specific profiles)
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(MyApp.Orders.Application.Mapping.OrderProfile).Assembly);

var app = builder.Build();

// ============================================================================
// Common Microservice Pipeline
// ============================================================================
app.UseCommonMicroservicePipeline(new MicroserviceConfigurationOptions
{
    DbContextType = typeof(OrdersDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
