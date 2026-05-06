namespace MyApp.Agentic.Application.Contracts.DTOs;

public record AgentDto(
    Guid Id,
    string Name,
    string Description,
    Guid ModelId,
    string ModelName,
    double Temperature,
    int TopK,
    int MaxTokens,
    int EmbeddingDimensions,
    bool EnableMemory,
    bool EnableRAG,
    string? EmbeddingModelName,
    string SystemInstructions,
    bool IsActive,
    Guid? TenantId
);

public record AgentListDto(
    Guid Id,
    string Name,
    string Description,
    string ModelName,
    bool IsActive,
    bool EnableMemory,
    bool EnableRAG
);

public record CreateAgentDto(
    string Name,
    string Description,
    Guid ModelId,
    double Temperature,
    string SystemInstructions,
    Guid? TenantId,
    int TopK = 3,
    int MaxTokens = 2048,
    int EmbeddingDimensions = 1536,
    bool EnableMemory = true,
    bool EnableRAG = true,
    string? EmbeddingModelName = null
);

public record UpdateAgentDto(
    string Name,
    string Description,
    Guid ModelId,
    double Temperature,
    string SystemInstructions,
    int? TopK = null,
    int? MaxTokens = null,
    int? EmbeddingDimensions = null,
    bool? EnableMemory = null,
    bool? EnableRAG = null,
    string? EmbeddingModelName = null
);