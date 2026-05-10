using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Agentic.Application.AI;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Agentic.Application.Services;
using MyApp.Agentic.API.Plugins;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Domain.Skills;
using MyApp.Agentic.Infrastructure.Data;
using MyApp.Agentic.Infrastructure.Data.Repositories;
using MyApp.Agentic.Infrastructure.Memory;
using MyApp.Agentic.Infrastructure.State;
using MyApp.Agentic.Infrastructure.Data.Seeders;
using MyApp.Shared.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

var memoryDbConnectionString = builder.Configuration.GetConnectionString("AgenticMemory")
    ?? throw new InvalidOperationException("Connection string 'agentic-memory' not found.");

builder.AddRedisDistributedCache("cache");

builder.Services.AddHttpClient<DocsPlugin>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Add Agent Skills
builder.Services.AddSingleton<SkillService>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<SkillService>>();
    var service = new SkillService(logger);
    
    // Load CollectionsAgent Skill
    var instructions = LoadSkillInstructions("Agent.Skills/CollectionsAgent/skill.md");
    var collectionsDef = new SkillDefinition(
        Guid.NewGuid(),
        "CollectionsAgent",
        "Account Receivables & Collections",
        instructions,
        new List<string> { "get_invoice", "search_invoices", "get_customer" },
        new List<string> { "BillingPlugin", "CrmPlugin", "DocsPlugin" });
    service.Load(collectionsDef);
    
    return service;
});

builder.Services.AddSingleton<ISkillService>(sp => sp.GetRequiredService<SkillService>());
builder.Services.AddSecretCrypto(builder.Configuration, "ProviderSecretsCrypto");

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

        services.AddScoped<ISessionStateStore, DaprSessionStateStore>();

        services.AddScoped<IEmbeddingService, StubEmbeddingService>();
        services.AddScoped<IAgentExecutionService, StubAgentExecutionService>();

        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<IAIProviderService, AIProviderService>();
        services.AddScoped<IAIModelService, AIModelService>();
        services.AddScoped<AgenticCatalogSeeder>();
    }
});

var app = builder.Build();

app.UseServiceDefaults();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AgenticDbContext>();
    await dbContext.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<AgenticCatalogSeeder>();
    await seeder.SeedAsync();
}

app.Run();

static string LoadSkillInstructions(string path)
{
    try
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(fullPath))
            return File.ReadAllText(fullPath);
        
        // Try alternate path (development)
        var altPath = Path.Combine(Directory.GetCurrentDirectory(), "src", "MyApp.Agentic", path);
        if (File.Exists(altPath))
            return File.ReadAllText(altPath);
    }
    catch { }
    return string.Empty;
}

static Dictionary<string, object> LoadSkillConfig(string path)
{
    try
    {
        var fullPath = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(fullPath))
        {
            var json = File.ReadAllText(fullPath);
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(json) 
                ?? new Dictionary<string, object>();
        }
    }
    catch { }
    return new Dictionary<string, object>();
}

public static class AgentSkillExtensions
{
    public static IServiceCollection AddAgentSkills(
        this IServiceCollection services,
        Action<AgentSkillOptions> configure)
    {
        var options = new AgentSkillOptions();
        configure(options);

        services.AddSingleton<SkillService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SkillService>>();
            var service = new SkillService(logger);
            options.LoadSkills(service);
            return service;
        });

        services.AddSingleton<ISkillService>(sp => sp.GetRequiredService<SkillService>());

        return services;
    }
}
