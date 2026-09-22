using MyApp.Agentic.Domain.Agents;
using MyApp.Agentic.Domain.Sessions;

namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Request to start a new conversation session.</summary>
/// <param name="AgentId">The agent Id.</param>
/// <param name="Title">The title.</param>
public record StartSessionRequest(
    Guid? AgentId,
    string? Title
);

/// <summary>Response from starting a new session.</summary>
/// <param name="SessionId">The session Id.</param>
/// <param name="AgentId">The agent Id.</param>
/// <param name="AgentName">The agent Name.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="UserId">The user Id.</param>
/// <param name="Title">The title.</param>
/// <param name="StartedAt">The started At.</param>
/// <param name="Status">The status.</param>
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
/// <param name="Message">The message.</param>
/// <param name="Options">The options.</param>
public record SendMessageRequest(
    string Message,
    ProcessMessageOptions? Options
);

/// <summary>Options for message processing.</summary>
/// <param name="Temperature">The temperature.</param>
/// <param name="MaxTokens">The max Tokens.</param>
/// <param name="TopK">The top K.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
public record ProcessMessageOptions(
    double? Temperature = null,
    int? MaxTokens = null,
    int? TopK = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null
);

/// <summary>Response from sending a message.</summary>
/// <param name="MessageId">The message Id.</param>
/// <param name="Content">The content.</param>
/// <param name="Timestamp">The timestamp.</param>
/// <param name="ToolCalls">The tool Calls.</param>
/// <param name="SessionId">The session Id.</param>
public record SendMessageResponse(
    Guid MessageId,
    string Content,
    DateTime Timestamp,
    List<ToolCallResult>? ToolCalls,
    Guid SessionId
);

/// <summary>Result of a tool call executed by the agent.</summary>
/// <param name="ToolName">The tool Name.</param>
/// <param name="Arguments">The arguments.</param>
/// <param name="Result">The result.</param>
/// <param name="Success">The success.</param>
public record ToolCallResult(
    string ToolName,
    string Arguments,
    string Result,
    bool Success
);

/// <summary>Detailed response for a session including messages.</summary>
/// <param name="SessionId">The session Id.</param>
/// <param name="AgentId">The agent Id.</param>
/// <param name="AgentName">The agent Name.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="UserId">The user Id.</param>
/// <param name="Title">The title.</param>
/// <param name="StartedAt">The started At.</param>
/// <param name="LastMessageAt">The last Message At.</param>
/// <param name="Status">The status.</param>
/// <param name="Messages">The messages.</param>
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
/// <param name="Id">The id.</param>
/// <param name="Role">The role.</param>
/// <param name="Content">The content.</param>
/// <param name="Timestamp">The timestamp.</param>
public record SessionMessageDto(
    Guid Id,
    string Role,
    string Content,
    DateTime Timestamp
);

/// <summary>Summary of a session for list views.</summary>
/// <param name="SessionId">The session Id.</param>
/// <param name="AgentId">The agent Id.</param>
/// <param name="AgentName">The agent Name.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="Title">The title.</param>
/// <param name="StartedAt">The started At.</param>
/// <param name="LastMessageAt">The last Message At.</param>
/// <param name="Status">The status.</param>
/// <param name="MessageCount">The message Count.</param>
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