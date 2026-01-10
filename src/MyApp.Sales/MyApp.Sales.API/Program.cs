using MyApp.Sales.Infrastructure.Data;
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
    ServiceName = "MyApp.Sales.API",
    ConnectionStringKey = "salesdb",
    DbContextType = typeof(SalesDbContext),
    AutoMapperAssembly = typeof(MyApp.Sales.Application.Mapping.SalesOrderMappingProfile).Assembly,
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
        services.AddScoped<MyApp.Sales.Application.Contracts.Services.ICustomerService,
            MyApp.Sales.Application.Services.CustomerService>();
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
