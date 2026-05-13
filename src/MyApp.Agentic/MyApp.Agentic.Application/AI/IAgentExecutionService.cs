using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Application.Contracts.DTOs;

namespace MyApp.Agentic.Application.AI;

/// <summary>
/// HTTP verb classification used when filtering tools by <see cref="BotType"/>.
/// </summary>
public enum ToolHttpVerb
{
    /// <summary>Unknown or unspecified verb.</summary>
    Unknown = 0,

    /// <summary>Read-only GET operation.</summary>
    Get = 1,

    /// <summary>Create operation.</summary>
    Post = 2,

    /// <summary>Update operation.</summary>
    Put = 3,

    /// <summary>Delete operation.</summary>
    Delete = 4,

    /// <summary>Partial update operation.</summary>
    Patch = 5
}

/// <summary>
/// Describes a tool exposed to the model during agent execution.
/// </summary>
public class ToolDefinition
{
    /// <summary>Canonical tool name used by the LLM and executor.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Optional endpoint hint associated with the tool.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>HTTP verb classification used for bot-type filtering.</summary>
    public ToolHttpVerb Verb { get; init; } = ToolHttpVerb.Unknown;

    /// <summary>
    /// Human-readable description shown to the model during tool selection.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Initializes a new empty <see cref="ToolDefinition"/>.</summary>
    public ToolDefinition() { }

    /// <summary>
    /// Initializes a new <see cref="ToolDefinition"/> with name and endpoint.
    /// </summary>
    /// <param name="name">Tool name.</param>
    /// <param name="endpoint">Optional endpoint hint.</param>
    public ToolDefinition(string name, string endpoint)
    {
        Name = name;
        Endpoint = endpoint;
    }

    /// <summary>
    /// Initializes a new <see cref="ToolDefinition"/> with name, endpoint, and verb.
    /// </summary>
    /// <param name="name">Tool name.</param>
    /// <param name="endpoint">Optional endpoint hint.</param>
    /// <param name="verb">HTTP verb classification.</param>
    public ToolDefinition(string name, string endpoint, ToolHttpVerb verb)
    {
        Name = name;
        Endpoint = endpoint;
        Verb = verb;
    }
}

/// <summary>
/// Runtime context passed to the agent execution service for a single model turn.
/// </summary>
public class AgentExecutionContext
{
    /// <summary>Agent being executed.</summary>
    public required Agent Agent { get; init; }

    /// <summary>Decrypted provider API key.</summary>
    public required string ApiKey { get; init; }

    /// <summary>Provider base URL for chat completions.</summary>
    public required string BaseUrl { get; init; }

    /// <summary>Final system prompt sent to the model.</summary>
    public string SystemPrompt { get; init; } = string.Empty;

    /// <summary>Prior conversation turns in <c>Role: content</c> format.</summary>
    public List<string> ConversationHistory { get; init; } = new();

    /// <summary>Retrieved memory snippets injected as additional context.</summary>
    public List<string> ContextMemories { get; init; } = new();

    /// <summary>Tools available for this execution.</summary>
    public List<ToolDefinition> Tools { get; init; } = new();

    /// <summary>Sampling temperature for the model.</summary>
    public double Temperature { get; init; } = 0.7;

    /// <summary>Maximum output tokens for the model response.</summary>
    public int MaxTokens { get; init; } = 2048;
}

/// <summary>
/// Result returned after an agent execution completes.
/// </summary>
/// <param name="Content">Assistant text content.</param>
/// <param name="ToolCalls">Tool calls executed during the turn, if any.</param>
/// <param name="FinishReason">Provider finish reason or internal termination reason.</param>
/// <param name="Metadata">Optional execution metadata.</param>
public record AgentExecutionResult(
    string Content,
    List<ToolCallResult>? ToolCalls = null,
    string? FinishReason = null,
    Dictionary<string, string>? Metadata = null
);

/// <summary>
/// Executes an agent turn against the configured AI provider with optional tool calling.
/// </summary>
public interface IAgentExecutionService
{
    /// <summary>
    /// Executes a single agent turn for the supplied user message.
    /// </summary>
    /// <param name="context">Execution context including agent, prompt, history, and tools.</param>
    /// <param name="userMessage">Latest user message for this turn.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Model response and any tool call results.</returns>
    Task<AgentExecutionResult> ExecuteAsync(AgentExecutionContext context, string userMessage, CancellationToken cancellationToken = default);
}
