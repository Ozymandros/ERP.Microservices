using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Data;
using MyApp.Agentic.Infrastructure.Data.Repositories;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.Secrets;
using MyApp.Agentic.Infrastructure.State;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

var memoryDbConnectionString = builder.Configuration.GetConnectionString("AgenticMemory")
    ?? throw new InvalidOperationException("Connection string 'agentic-memory' not found.");

builder.AddRedisDistributedCache("cache");

builder.AddServiceDefaults(new MicroserviceConfigurationOptions
{
    ServiceName = "MyApp.Agentic.API",
    ConnectionStringKey = "agenticdb",
    DbContextType = typeof(AgenticDbContext),
    AutoMapperAssembly = typeof(AgentService).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddDbContext<MemoryDbContext>(opts =>
            opts.UseNpgsql(memoryDbConnectionString, npgsqlOpts =>
                npgsqlOpts.EnableRetryOnFailure()));

        services.AddScoped<IAIProviderRepository, AIProviderRepository>();
        services.AddScoped<IAIModelRepository, AIModelRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();

        services.AddScoped<ISecretStore, DaprSecretStore>();
        services.AddScoped<ISessionStateStore, DaprSessionStateStore>();

        services.AddScoped<IEmbeddingService, StubEmbeddingService>();
        services.AddScoped<IAgentExecutionService, StubAgentExecutionService>();

        services.AddScoped<IAgentService, AgentService>();
    }
});

var app = builder.Build();

app.UseServiceDefaults();

app.Run();