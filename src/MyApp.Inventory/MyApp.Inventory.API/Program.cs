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

        // Register Inventory-specific services
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IInventoryTransactionService, 
            MyApp.Inventory.Application.Services.InventoryTransactionService>();
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IProductService, 
            MyApp.Inventory.Application.Services.ProductService>();
        services.AddScoped<MyApp.Inventory.Application.Contracts.Services.IWarehouseService, 
            MyApp.Inventory.Application.Services.WarehouseService>();
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

app.Run();
