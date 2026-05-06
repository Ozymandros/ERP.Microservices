using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;

namespace MyApp.Agentic.Application.Contracts.DTOs;

public record StartSessionRequest(
    Guid? AgentId,
    string? Title
);

public record StartSessionResponse(
    Guid SessionId,
    Guid AgentId,
    string AgentName,
    BotType BotType,
    string UserId,
    string? Title,
    DateTime StartedAt,
    SessionStatus Status
);

public record SendMessageRequest(
    string Message,
    ProcessMessageOptions? Options
);

public record ProcessMessageOptions(
    double? Temperature = null,
    int? MaxTokens = null,
    int? TopK = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null
);

public record SendMessageResponse(
    Guid MessageId,
    string Content,
    DateTime Timestamp,
    List<ToolCallResult>? ToolCalls,
    Guid SessionId
);

public record ToolCallResult(
    string ToolName,
    string Arguments,
    string Result,
    bool Success
);

public record SessionDetailsResponse(
    Guid SessionId,
    Guid AgentId,
    string AgentName,
    BotType BotType,
    string UserId,
    string? Title,
    DateTime StartedAt,
    DateTime? LastMessageAt,
    SessionStatus Status,
    List<SessionMessageDto> Messages
);

public record SessionMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp
);

public record SessionListItemDto(
    Guid SessionId,
    Guid AgentId,
    string AgentName,
    BotType BotType,
    string? Title,
    DateTime StartedAt,
    DateTime? LastMessageAt,
    SessionStatus Status,
    int MessageCount
);