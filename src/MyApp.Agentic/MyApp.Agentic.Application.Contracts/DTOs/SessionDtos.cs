using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;

namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Request to start a new conversation session.</summary>
public record StartSessionRequest(
    Guid? AgentId,
    string UserId,
    string? Title
);

/// <summary>Response from starting a new session.</summary>
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

/// <summary>Request to send a message in an existing session.</summary>
public record SendMessageRequest(
    string Message,
    ProcessMessageOptions? Options
);

/// <summary>Options for message processing.</summary>
public record ProcessMessageOptions(
    double? Temperature = null,
    int? MaxTokens = null,
    int? TopK = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null
);

/// <summary>Response from sending a message.</summary>
public record SendMessageResponse(
    Guid MessageId,
    string Content,
    DateTime Timestamp,
    List<ToolCallResult>? ToolCalls,
    Guid SessionId
);

/// <summary>Result of a tool call executed by the agent.</summary>
public record ToolCallResult(
    string ToolName,
    string Arguments,
    string Result,
    bool Success
);

/// <summary>Detailed response for a session including messages.</summary>
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

/// <summary>A message within a session.</summary>
public record SessionMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp
);

/// <summary>Summary of a session for list views.</summary>
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