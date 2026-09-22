using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Full agent details response.</summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="ProviderName">The provider Name.</param>
/// <param name="ModelId">The model Id.</param>
/// <param name="ModelName">The model Name.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="SystemPrompt">The system Prompt.</param>
/// <param name="Temperature">The temperature.</param>
/// <param name="TopK">The top K.</param>
/// <param name="MaxTokens">The max Tokens.</param>
/// <param name="EmbeddingDimensions">The embedding Dimensions.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
/// <param name="EmbeddingModelName">The embedding Model Name.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="OwnerUserId">The owner User Id.</param>
public record AgentDto(
    Guid Id,
    string Name,
    string Description,
    Guid ProviderId,
    string ProviderName,
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
    string? OwnerUserId
);

/// <summary>Summary for agent list views.</summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="ModelName">The model Name.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="IsActive">The is Active.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
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
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="ModelId">The model Id.</param>
/// <param name="Temperature">The temperature.</param>
/// <param name="SystemPrompt">The system Prompt.</param>
/// <param name="OwnerUserId">The owner User Id.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="TopK">The top K.</param>
/// <param name="MaxTokens">The max Tokens.</param>
/// <param name="EmbeddingDimensions">The embedding Dimensions.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
/// <param name="EmbeddingModelName">The embedding Model Name.</param>
public record CreateAgentDto(
    string Name,
    string Description,
    Guid ProviderId,
    Guid ModelId,
    double Temperature,
    string SystemPrompt,
    string? OwnerUserId,
    BotType BotType = BotType.Chat,
    int TopK = 3,
    int MaxTokens = 2048,
    int EmbeddingDimensions = 1536,
    bool EnableMemory = true,
    bool EnableRAG = true,
    string? EmbeddingModelName = null
);

/// <summary>Request to update an existing agent.</summary>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="ModelId">The model Id.</param>
/// <param name="Temperature">The temperature.</param>
/// <param name="SystemPrompt">The system Prompt.</param>
/// <param name="BotType">The bot Type.</param>
/// <param name="TopK">The top K.</param>
/// <param name="MaxTokens">The max Tokens.</param>
/// <param name="EmbeddingDimensions">The embedding Dimensions.</param>
/// <param name="EnableMemory">The enable Memory.</param>
/// <param name="EnableRAG">The enable RAG.</param>
/// <param name="EmbeddingModelName">The embedding Model Name.</param>
public record UpdateAgentDto(
    string Name,
    string Description,
    Guid ProviderId,
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