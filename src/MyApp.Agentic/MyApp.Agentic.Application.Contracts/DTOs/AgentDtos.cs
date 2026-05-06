using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Full agent details response.</summary>
public record AgentDto(
    Guid Id,
    string Name,
    string Description,
    Guid ModelId,
    string ModelName,
    BotType BotType,
    string SystemPrompt,
    double Temperature,
    int TopK,
    int MaxTokens,
    int EmbeddingDimensions,
    bool EnableMemory,
    bool EnableRAG,
    string? EmbeddingModelName,
    bool IsActive,
    Guid? TenantId
);

/// <summary>Summary for agent list views.</summary>
public record AgentListDto(
    Guid Id,
    string Name,
    string Description,
    string ModelName,
    BotType BotType,
    bool IsActive,
    bool EnableMemory,
    bool EnableRAG
);

/// <summary>Request to create a new agent.</summary>
public record CreateAgentDto(
    string Name,
    string Description,
    Guid ModelId,
    double Temperature,
    string SystemPrompt,
    Guid? TenantId,
    BotType BotType = BotType.Chat,
    int TopK = 3,
    int MaxTokens = 2048,
    int EmbeddingDimensions = 1536,
    bool EnableMemory = true,
    bool EnableRAG = true,
    string? EmbeddingModelName = null
);

/// <summary>Request to update an existing agent.</summary>
public record UpdateAgentDto(
    string Name,
    string Description,
    Guid ModelId,
    double Temperature,
    string SystemPrompt,
    BotType? BotType = null,
    int? TopK = null,
    int? MaxTokens = null,
    int? EmbeddingDimensions = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null,
    string? EmbeddingModelName = null
);