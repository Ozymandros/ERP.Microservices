using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.AI;

public class ToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;

    public ToolDefinition() { }
    public ToolDefinition(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }
}

public class AgentExecutionContext
{
    public required Agent Agent { get; init; }
    public required string ApiKey { get; init; }
    public required string BaseUrl { get; init; }
    public string SystemPrompt { get; init; } = string.Empty;
    public List<string> ConversationHistory { get; init; } = new();
    public List<string> ContextMemories { get; init; } = new();
    public List<ToolDefinition> Tools { get; init; } = new();
    public double Temperature { get; init; } = 0.7;
    public int MaxTokens { get; init; } = 2048;
}

public interface IAgentExecutionService
{
    Task<string> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default);
}