using MyApp.Audit.Application.Contracts.Services;
using MyApp.Audit.Application.Mapping;
using MyApp.Audit.Application.Services;
using MyApp.Audit.Domain.Repositories;
using MyApp.Audit.Infrastructure;
using MyApp.Audit.Infrastructure.Repositories;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Audit.API",
    ConnectionStringKey = "auditdb",
    DbContextType = typeof(AuditSqlDbContext),
    AutoMapperAssembly = typeof(AuditMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddScoped<IEntityChangeRepository, EntityChangeRepository>();
        services.AddScoped<IEntityChangeService, EntityChangeService>();
    }
});

var app = builder.Build();
app.UseServiceDefaults();
app.Run();
