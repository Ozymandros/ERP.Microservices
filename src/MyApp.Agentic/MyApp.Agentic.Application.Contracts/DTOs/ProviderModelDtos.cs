using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Contracts.DTOs;

public record AIProviderDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string SecretKeyName
);

public record CreateAIProviderDto(
    string Name,
    string BaseUrl,
    string SecretKeyName
);

public record UpdateAIProviderDto(
    string Name,
    string BaseUrl,
    string SecretKeyName
);

public record AIModelDto(
    Guid Id,
    Guid ProviderId,
    string ProviderName,
    string CommercialName,
    string TechnicalName,
    int TokenLimit,
    string Capabilities,
    double DefaultTemperature,
    int DefaultTopK,
    int DefaultMaxTokens,
    int DefaultEmbeddingDimensions,
    bool DefaultEnableMemory,
    bool DefaultEnableRAG,
    string? DefaultEmbeddingModelName,
    BotType DefaultBotType,
    string? DefaultSystemPrompt
);

public record CreateAIModelDto(
    Guid ProviderId,
    string CommercialName,
    string TechnicalName,
    int TokenLimit,
    string Capabilities,
    double DefaultTemperature = 0.7,
    int DefaultTopK = 3,
    int DefaultMaxTokens = 2048,
    int DefaultEmbeddingDimensions = 1536,
    bool DefaultEnableMemory = true,
    bool DefaultEnableRAG = true,
    string? DefaultEmbeddingModelName = null,
    BotType DefaultBotType = BotType.Chat,
    string? DefaultSystemPrompt = null
);

public record UpdateAIModelDto(
    Guid ProviderId,
    string CommercialName,
    string TechnicalName,
    int TokenLimit,
    string Capabilities,
    double DefaultTemperature = 0.7,
    int DefaultTopK = 3,
    int DefaultMaxTokens = 2048,
    int DefaultEmbeddingDimensions = 1536,
    bool DefaultEnableMemory = true,
    bool DefaultEnableRAG = true,
    string? DefaultEmbeddingModelName = null,
    BotType DefaultBotType = BotType.Chat,
    string? DefaultSystemPrompt = null
);
