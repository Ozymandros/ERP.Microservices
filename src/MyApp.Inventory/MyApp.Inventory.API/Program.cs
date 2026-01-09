using MyApp.Inventory.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Common Microservice Configuration
// ============================================================================
builder.AddCommonMicroserviceServices(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Inventory.API",
    ConnectionStringKey = "inventorydb",
    DbContextType = typeof(InventoryDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true,
    EnableDapr = true,
    EnableOpenTelemetry = true,
    EnableRedisCache = true,
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

// Redis Cache (Aspire-managed)
builder.AddRedisDistributedCache("cache");

// AutoMapper (service-specific profiles)
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(MyApp.Inventory.Application.Mappings.InventoryMappingProfile).Assembly);

var app = builder.Build();

// ============================================================================
// Common Microservice Pipeline
// ============================================================================
app.UseCommonMicroservicePipeline(new MicroserviceConfigurationOptions
{
    DbContextType = typeof(InventoryDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true
});

// Dapr pub/sub subscriptions
app.MapSubscribeHandler();

app.Run();
