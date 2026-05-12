using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.AI;

public enum ToolHttpVerb
{
    Unknown = 0,
    Get = 1,
    Post = 2,
    Put = 3,
    Delete = 4,
    Patch = 5
}

public class ToolDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public ToolHttpVerb Verb { get; init; } = ToolHttpVerb.Unknown;

    public ToolDefinition() { }
    public ToolDefinition(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    public ToolDefinition(string name, string endpoint, ToolHttpVerb verb)
    {
        Name = name;
        Endpoint = endpoint;
        Verb = verb;
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

public record AgentExecutionResult(
    string Content,
    List<ToolCallResult>? ToolCalls = null,
    string? FinishReason = null,
    Dictionary<string, string>? Metadata = null
);

public interface IAgentExecutionService
{
    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default);
}