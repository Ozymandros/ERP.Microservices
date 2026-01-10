using MyApp.Orders.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Defaults Configuration
// ============================================================================
// IMPORTANT: Redis cache must be configured BEFORE AddServiceDefaults because it's an Aspire
// extension method that requires the Redis resource reference from the AppHost project.
builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Orders.API",
    ConnectionStringKey = "ordersdb",
    DbContextType = typeof(OrdersDbContext),
    AutoMapperAssembly = typeof(MyApp.Orders.Application.Mapping.OrderProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        // Register Orders-specific repositories
        services.AddScoped<MyApp.Orders.Domain.IOrderRepository,
            MyApp.Orders.Infrastructure.Repositories.OrderRepository>();
        services.AddScoped<MyApp.Orders.Domain.IOrderLineRepository,
            MyApp.Orders.Infrastructure.Repositories.OrderLineRepository>();
        services.AddScoped<MyApp.Orders.Domain.Repositories.IReservedStockRepository,
            MyApp.Orders.Infrastructure.Repositories.ReservedStockRepository>();

        // Register Orders-specific services
        services.AddScoped<MyApp.Orders.Application.Contracts.IOrderService,
            MyApp.Orders.Application.Services.OrderService>();

        // Register background services
        services.AddHostedService<MyApp.Orders.API.BackgroundServices.ReservationExpiryService>();
    }
});

var app = builder.Build();

// ============================================================================
// Service Defaults Pipeline
// ============================================================================
// Options are automatically reused from AddServiceDefaults via DI.
// MapSubscribeHandler() is automatically called if EnableDapr is true.
app.UseServiceDefaults();

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
