using MyApp.Agentic.Domain.Agents;

namespace MyApp.Agentic.Application.Contracts.DTOs;

/// <summary>Represents an AI provider response DTO including masked API key information.</summary>
/// <param name="Id">The id.</param>
/// <param name="Name">The name.</param>
/// <param name="BaseUrl">The base Url.</param>
/// <param name="ApiKey">The api Key.</param>
/// <param name="HasApiKey">The has Api Key.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
public record AIProviderDto(
    Guid Id,
    string Name,
    string BaseUrl,
    string? ApiKey,
    bool HasApiKey,
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

/// <summary>Request payload for creating a new AI provider.</summary>
/// <param name="Name">The name.</param>
/// <param name="BaseUrl">The base Url.</param>
/// <param name="ApiKey">The api Key.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
public record CreateAIProviderDto(
    string Name,
    string BaseUrl,
    string? ApiKey,
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

/// <summary>Request payload for updating an existing AI provider.</summary>
/// <param name="Name">The name.</param>
/// <param name="BaseUrl">The base Url.</param>
/// <param name="ApiKey">The api Key.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
public record UpdateAIProviderDto(
    string Name,
    string BaseUrl,
    string? ApiKey,
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

/// <summary>Represents an AI model response DTO including provider information.</summary>
/// <param name="Id">The id.</param>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="ProviderName">The provider Name.</param>
/// <param name="CommercialName">The commercial Name.</param>
/// <param name="TechnicalName">The technical Name.</param>
/// <param name="TokenLimit">The token Limit.</param>
/// <param name="Capabilities">The capabilities.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
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

/// <summary>Request payload for creating a new AI model. Null fields inherit defaults from the provider.</summary>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="CommercialName">The commercial Name.</param>
/// <param name="TechnicalName">The technical Name.</param>
/// <param name="TokenLimit">The token Limit.</param>
/// <param name="Capabilities">The capabilities.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
public record CreateAIModelDto(
    Guid ProviderId,
    string CommercialName,
    string TechnicalName,
    int TokenLimit,
    string Capabilities,
    double? DefaultTemperature = null,
    int? DefaultTopK = null,
    int? DefaultMaxTokens = null,
    int? DefaultEmbeddingDimensions = null,
    bool? DefaultEnableMemory = null,
    bool? DefaultEnableRAG = null,
    string? DefaultEmbeddingModelName = null,
    BotType? DefaultBotType = null,
    string? DefaultSystemPrompt = null
);

/// <summary>Request payload for updating an existing AI model.</summary>
/// <param name="ProviderId">The provider Id.</param>
/// <param name="CommercialName">The commercial Name.</param>
/// <param name="TechnicalName">The technical Name.</param>
/// <param name="TokenLimit">The token Limit.</param>
/// <param name="Capabilities">The capabilities.</param>
/// <param name="DefaultTemperature">The default Temperature.</param>
/// <param name="DefaultTopK">The default Top K.</param>
/// <param name="DefaultMaxTokens">The default Max Tokens.</param>
/// <param name="DefaultEmbeddingDimensions">The default Embedding Dimensions.</param>
/// <param name="DefaultEnableMemory">The default Enable Memory.</param>
/// <param name="DefaultEnableRAG">The default Enable RAG.</param>
/// <param name="DefaultEmbeddingModelName">The default Embedding Model Name.</param>
/// <param name="DefaultBotType">The default Bot Type.</param>
/// <param name="DefaultSystemPrompt">The default System Prompt.</param>
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
