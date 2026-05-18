using MyApp.Auth.Infrastructure.Data;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Auth.API",
    ConnectionStringKey = "authdb",
    DbContextType = typeof(AuthDbContext),
    //AutoMapperAssembly = typeof(AuthMappingProfile).Assembly,
    ConfigureServiceDependencies = services =>
    {
        // Register repositories

        // Register application services
    }
});

var app = builder.Build();

app.UseServiceDefaults();

app.Run();
