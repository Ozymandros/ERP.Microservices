using MyApp.Sales.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Common Microservice Configuration
// ============================================================================
builder.AddCommonMicroserviceServices(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Sales.API",
    ConnectionStringKey = "salesdb",
    DbContextType = typeof(SalesDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true,
    EnableDapr = true,
    EnableOpenTelemetry = true,
    EnableRedisCache = true,
    ConfigureServiceDependencies = services =>
    {
        // Register Sales-specific repositories
        services.AddScoped<MyApp.Sales.Domain.ISalesOrderLineRepository, 
            MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderLineRepository>();
        services.AddScoped<MyApp.Sales.Domain.ISalesOrderRepository, 
            MyApp.Sales.Infrastructure.Data.Repositories.SalesOrderRepository>();
        services.AddScoped<MyApp.Sales.Domain.ICustomerRepository, 
            MyApp.Sales.Infrastructure.Data.Repositories.CustomerRepository>();

        // Register Sales-specific services
        services.AddScoped<MyApp.Sales.Application.Contracts.Services.ISalesOrderService, 
            MyApp.Sales.Application.Services.SalesOrderService>();
    }
});

// Redis Cache (Aspire-managed)
builder.AddRedisDistributedCache("cache");

// AutoMapper (service-specific profiles)
builder.Services.AddAutoMapper(
    cfg => { },
    typeof(MyApp.Sales.Application.Mapping.SalesOrderMappingProfile).Assembly);

var app = builder.Build();

// ============================================================================
// Common Microservice Pipeline
// ============================================================================
app.UseCommonMicroservicePipeline(new MicroserviceConfigurationOptions
{
    DbContextType = typeof(SalesDbContext),
    EnableAuthentication = true,
    EnableHealthChecks = true
});

app.Run();
