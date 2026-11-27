using Microsoft.EntityFrameworkCore;
using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Agentic.Infrastructure.Data;

namespace MyApp.Agentic.Infrastructure.Tests.Helpers;

internal static class AgenticTestDbContextFactory
{
    public static AgenticSqlDbContext Create() =>
        new(new DbContextOptionsBuilder<AgenticSqlDbContext>()
            .UseInMemoryDatabase($"agentic-infra-{Guid.NewGuid()}")
            .Options);

    public static (AIProvider Provider, AIModel Model) SeedProviderAndModel(AgenticSqlDbContext context)
    {
        var providerId = Guid.NewGuid();
        var provider = new AIProvider(providerId, "OpenAI", "https://api.openai.com", null);
        var model = new AIModel(Guid.NewGuid(), providerId, "GPT Test", "gpt-test", 8192, "chat");
        context.AIProviders.Add(provider);
        context.AIModels.Add(model);
        context.SaveChanges();
        return (provider, model);
    }

    public static Agent SeedAgent(AgenticSqlDbContext context, Guid modelId) =>
        new(Guid.NewGuid(), "ERP Assistant", "desc", modelId, 0.5, "Be helpful");

    public static void PersistAgent(AgenticSqlDbContext context, Agent agent)
    {
        context.Agents.Add(agent);
        context.SaveChanges();
    }

    public static AgentSession SeedSession(AgenticSqlDbContext context, Guid agentId, string userId = "user-1") =>
        new(Guid.NewGuid(), agentId, userId, "Test session");
}