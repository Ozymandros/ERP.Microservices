namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Request to process a single message through a specified agent.</summary>
/// <param name="AgentId">The agent Id.</param>
/// <param name="Message">The message.</param>
/// <param name="Options">The options.</param>
public record ProcessAgentMessageRequest(
    Guid AgentId,
    string Message,
    AgentExecutionOptions? Options = null
);

/// <summary>Per-request overrides for agent execution parameters. Null values fall back to the agent's defaults.</summary>
/// <param name="Temperature">The temperature.</param>
/// <param name="TopK">The top K.</param>
/// <param name="MaxTokens">The max Tokens.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
public record AgentExecutionOptions(
    double? Temperature = null,
    int? TopK = null,
    int? MaxTokens = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null
);

/// <summary>Response from processing a single agent message, including the AI-generated reply and any tool calls.</summary>
/// <param name="SessionId">The session Id.</param>
/// <param name="UserId">The user Id.</param>
/// <param name="UserMessage">The user Message.</param>
/// <param name="AIResponse">The aIResponse.</param>
/// <param name="Timestamp">The timestamp.</param>
/// <param name="ToolCalls">The tool Calls.</param>
public record ProcessAgentMessageResponse(
    Guid SessionId,
    string UserId,
    string UserMessage,
    string AIResponse,
    DateTime Timestamp,
    List<ToolCallResult>? ToolCalls = null
)
{
    /// <summary>Gets the AI response content; alias for <see cref="AIResponse"/>.</summary>
    public string Content => AIResponse;
}