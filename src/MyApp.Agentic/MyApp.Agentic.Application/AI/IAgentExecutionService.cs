using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.AI;

public class AgentExecutionContext
{
    public required Agent Agent { get; init; }
    public required string ApiKey { get; init; }
    public required string BaseUrl { get; init; }
    public List<string> ConversationHistory { get; init; } = new();
    public List<string> ContextMemories { get; init; } = new();
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 2048;
}

public interface IAgentExecutionService
{
    Task<string> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default);
}