using MyApp.Projects.Application.Contracts;
using MyApp.Projects.Application.Contracts.Services;
using MyApp.Projects.Application.Integration;
using MyApp.Projects.Application.Mapping;
using MyApp.Projects.Application.Services;
using MyApp.Projects.Domain.Repositories;
using MyApp.Projects.Infrastructure.Data;
using MyApp.Projects.Infrastructure.Data.Repositories;
using MyApp.Projects.Infrastructure.Data.Seeders;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// Service Defaults Configuration
// ============================================================================
// IMPORTANT: Redis cache must be configured BEFORE AddServiceDefaults (Aspire resource reference).
builder.AddRedisDistributedCache("cache");

var projectsSettings = builder.Configuration.GetSection(ProjectsSettings.SectionName).Get<ProjectsSettings>()
    ?? new ProjectsSettings();

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Projects.API",
    ConnectionStringKey = "projectsdb",
    DbContextType = typeof(ProjectsDbContext),
    AutoMapperAssembly = typeof(ProjectsMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddSingleton(projectsSettings);

        // Repositories
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IProjectTaskRepository, ProjectTaskRepository>();
        services.AddScoped<IProjectPlanningLineRepository, ProjectPlanningLineRepository>();
        services.AddScoped<IProjectLedgerEntryRepository, ProjectLedgerEntryRepository>();
        services.AddScoped<IProjectInvoiceRequestRepository, ProjectInvoiceRequestRepository>();
        services.AddScoped<IProjectConsumptionOrderRepository, ProjectConsumptionOrderRepository>();
        services.AddScoped<IResourceRepository, ResourceRepository>();
        services.AddScoped<IResourceGroupRepository, ResourceGroupRepository>();
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();

        // Dapr service-invocation clients (Sales customers, Inventory products, Orders)
        services.AddScoped<ISalesCustomerClient, SalesCustomerClient>();
        services.AddScoped<IInventoryProductClient, InventoryProductClient>();
        services.AddScoped<IOrdersClient, OrdersClient>();

        // Application services
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTaskService, ProjectTaskService>();
        services.AddScoped<IPlanningLineService, PlanningLineService>();
        services.AddScoped<IProjectPostingService, ProjectPostingService>();
        services.AddScoped<IResourceService, ResourceService>();
    }
});

var app = builder.Build();

// ============================================================================
// Service Defaults Pipeline (applies EF migrations, maps Dapr subscriptions)
// ============================================================================
app.UseServiceDefaults();

// Reference data (currencies, demo rates, resources) — only inserted when tables are empty.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProjectsDbContext>();
    await ProjectsDataSeeder.SeedAsync(db, projectsSettings.LocalCurrency);
}

app.Run();
