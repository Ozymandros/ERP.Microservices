using MyApp.Purchasing.Application.Contracts.Services;
using MyApp.Purchasing.Application.Services;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Purchasing.Infrastructure.Data;
using MyApp.Purchasing.Infrastructure.Data.Repositories;
using MyApp.Shared.Domain.Permissions;
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
    ServiceName = "MyApp.Purchasing.API",
    ConnectionStringKey = "PurchasingDb",
    DbContextType = typeof(PurchasingDbContext),
    AutoMapperAssembly = typeof(MyApp.Purchasing.Application.Mappings.PurchasingMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        // Register Purchasing-specific repositories
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IPurchaseOrderLineRepository, PurchaseOrderLineRepository>();

        // Register Purchasing-specific services
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPermissionChecker, PermissionChecker>();
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
