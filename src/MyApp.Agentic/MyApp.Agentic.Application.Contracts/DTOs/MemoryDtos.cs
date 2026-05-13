namespace MyApp.Agentic.Application.Contracts.DTOs;

public record ProcessAgentMessageRequest(
    Guid AgentId,
    string Message,
    AgentExecutionOptions? Options = null
);

public record AgentExecutionOptions(
    double? Temperature = null,
    int? TopK = null,
    int? MaxTokens = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null
);

public record ProcessAgentMessageResponse(
    Guid SessionId,
    string UserId,
    string UserMessage,
    string AIResponse,
    DateTime Timestamp,
    List<ToolCallResult>? ToolCalls = null
)
{
    public string Content => AIResponse;
}