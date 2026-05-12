using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http;
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
    DbContextType = typeof(AgenticSqlDbContext),
    AutoMapperAssembly = typeof(AgentService).Assembly,
    ConfigureServiceDependencies = services =>
    {
        services.AddHttpClient("MemoryEmbeddingProvider", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<DeterministicTextEmbeddingGenerator>();
        services.AddSingleton<IMemoryEmbeddingGenerator, ProviderBackedMemoryEmbeddingGenerator>();

        services.AddScoped<IAIProviderRepository, AIProviderRepository>();
        services.AddScoped<IAIModelRepository, AIModelRepository>();
        services.AddScoped<IAgentRepository, AgentRepository>();
        services.AddScoped<IAgentSessionRepository, AgentSessionRepository>();
        services.AddScoped<IMemoryRepository, MemoryRepository>();

        services.AddScoped<ISessionStateStore, DaprSessionStateStore>();

        services.AddScoped<IAgentRuntimeFactory, AgentRuntimeFactory>();
        services.AddSingleton<IAgentToolRegistry, AgentToolRegistry>();
        services.AddScoped<IAgentToolExecutor, DefaultAgentToolExecutor>();

        services.AddScoped<IEmbeddingService, StubEmbeddingService>();
        services.AddScoped<IAgentExecutionService, MicrosoftAgentExecutionService>();

        services.AddScoped<BillingPlugin>();
        services.AddScoped<OrdersPlugin>();
        services.AddScoped<InventoryPlugin>();
        services.AddScoped<PurchasingPlugin>();
        services.AddScoped<SalesPlugin>();
        services.AddScoped<AuthPlugin>();
        services.AddScoped<CrmPlugin>();
        services.AddScoped<DocsPlugin>();

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
    // Migrate SQL Server (Metadata)
    var sqlDbContext = scope.ServiceProvider.GetRequiredService<AgenticSqlDbContext>();
    await sqlDbContext.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<AgenticCatalogSeeder>();
    await seeder.SeedAsync();

    // Register Tools in the Registry
    var toolRegistry = scope.ServiceProvider.GetRequiredService<IAgentToolRegistry>();
    
    // Billing
    var billing = scope.ServiceProvider.GetRequiredService<BillingPlugin>();
    toolRegistry.RegisterTool("get_invoice", (args, ct) => billing.GetInvoiceByNumberAsync(args));
    toolRegistry.RegisterTool("get_billing", (args, ct) => billing.GetByIdAsync(args));
    
    // Orders
    var orders = scope.ServiceProvider.GetRequiredService<OrdersPlugin>();
    toolRegistry.RegisterTool("get_order", (args, ct) => orders.GetByIdAsync(args));
    
    // Inventory
    var inventory = scope.ServiceProvider.GetRequiredService<InventoryPlugin>();
    toolRegistry.RegisterTool("get_product", (args, ct) => inventory.GetByIdAsync(args));
    
    // Docs
    var docs = scope.ServiceProvider.GetRequiredService<DocsPlugin>();
    toolRegistry.RegisterTool("search_docs", (args, ct) => docs.SearchAsync(args));
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
