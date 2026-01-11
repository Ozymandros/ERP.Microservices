using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Defaults Configuration
// ============================================================================
// Most options have sensible defaults: EnableAuthentication, EnableHealthChecks, EnableDapr,
// EnableOpenTelemetry, EnableRedisCache, and EnableAutoMapper all default to true.
// 
// IMPORTANT: Redis cache must be configured BEFORE AddServiceDefaults because it's an Aspire
// extension method that requires the Redis resource reference from the AppHost project.
builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Inventory.API", // Optional: defaults to assembly name
    ConnectionStringKey = "inventorydb",
    DbContextType = typeof(InventoryDbContext),
    AutoMapperAssembly = typeof(MyApp.Inventory.Application.Mappings.InventoryMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        // Register Inventory-specific repositories
        services.AddScoped<MyApp.Inventory.Domain.Repositories.IInventoryTransactionRepository,
            MyApp.Inventory.Infrastructure.Data.Repositories.InventoryTransactionRepository>();
        services.AddScoped<MyApp.Inventory.Domain.Repositories.IProductRepository,
            MyApp.Inventory.Infrastructure.Data.Repositories.ProductRepository>();
        services.AddScoped<MyApp.Inventory.Domain.Repositories.IWarehouseRepository,
            MyApp.Inventory.Infrastructure.Data.Repositories.WarehouseRepository>();
        services.AddScoped<MyApp.Inventory.Domain.Repositories.IWarehouseStockRepository,
            MyApp.Inventory.Infrastructure.Repositories.WarehouseStockRepository>();

        // Register Inventory-specific services
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IInventoryTransactionService,
            MyApp.Inventory.Application.Services.InventoryTransactionService>();
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IProductService,
            MyApp.Inventory.Application.Services.ProductService>();
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IWarehouseService,
            MyApp.Inventory.Application.Services.WarehouseService>();
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IWarehouseStockService,
            MyApp.Inventory.Application.Services.WarehouseStockService>();

        // Register background services
        services.AddHostedService<MyApp.Inventory.API.BackgroundServices.LowStockAlertService>();
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
