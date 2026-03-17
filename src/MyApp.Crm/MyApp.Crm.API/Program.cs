using MyApp.Crm.Application.Mapping;
using MyApp.Crm.Application.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Crm.Domain.Leads;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Crm.Infrastructure.Data;
using MyApp.Crm.Infrastructure.Data.Repositories;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Shared.Domain.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Crm.API",
    ConnectionStringKey = "crmdb",
    DbContextType = typeof(CrmDbContext),
    AutoMapperAssembly = typeof(CrmMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<MyApp.Crm.Application.Contracts.Services.ILeadService, LeadService>();

        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<MyApp.Crm.Application.Contracts.Services.IOpportunityService, OpportunityService>();

        services.AddScoped<IActivityRepository, ActivityRepository>();
        services.AddScoped<MyApp.Crm.Application.Contracts.Services.IActivityService, ActivityService>();
    }
});

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
