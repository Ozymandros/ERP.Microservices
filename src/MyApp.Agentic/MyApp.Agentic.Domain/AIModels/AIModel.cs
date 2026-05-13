using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIModels;

public class AIModel(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid ProviderId { get; private set; }
    public string CommercialName { get; private set; } = string.Empty;
    public string TechnicalName { get; private set; } = string.Empty;
    public int TokenLimit { get; private set; }
    public string Capabilities { get; private set; } = string.Empty;
    public double DefaultTemperature { get; private set; } = 0.7;
    public int DefaultTopK { get; private set; } = 3;
    public int DefaultMaxTokens { get; private set; } = 2048;
    public int DefaultEmbeddingDimensions { get; private set; } = 1536;
    public bool DefaultEnableMemory { get; private set; } = true;
    public bool DefaultEnableRAG { get; private set; } = true;
    public string? DefaultEmbeddingModelName { get; private set; }
    public BotType DefaultBotType { get; private set; } = BotType.Chat;
    public string? DefaultSystemPrompt { get; private set; }

    public AIProvider? Provider { get; private set; }
    public ICollection<Agent> Agents { get; private set; } = new List<Agent>();

    public AIModel(
        Guid id,
        Guid providerId,
        string commercialName,
        string technicalName,
        int tokenLimit,
        string capabilities,
        double defaultTemperature = 0.7,
        int defaultTopK = 3,
        int defaultMaxTokens = 2048,
        int defaultEmbeddingDimensions = 1536,
        bool defaultEnableMemory = true,
        bool defaultEnableRAG = true,
        string? defaultEmbeddingModelName = null,
        BotType defaultBotType = BotType.Chat,
        string? defaultSystemPrompt = null) : this(id)
    {
        ProviderId = NormalizeProviderId(providerId);
        CommercialName = NormalizeRequired(commercialName, nameof(commercialName));
        TechnicalName = NormalizeRequired(technicalName, nameof(technicalName));
        TokenLimit = tokenLimit > 0 ? tokenLimit : throw new ArgumentException("TokenLimit must be positive.", nameof(tokenLimit));
        Capabilities = capabilities?.Trim() ?? string.Empty;
        DefaultTemperature = ClampTemperature(defaultTemperature);
        DefaultTopK = defaultTopK > 0 ? defaultTopK : throw new ArgumentException("DefaultTopK must be positive.", nameof(defaultTopK));
        DefaultMaxTokens = defaultMaxTokens > 0 ? defaultMaxTokens : throw new ArgumentException("DefaultMaxTokens must be positive.", nameof(defaultMaxTokens));
        DefaultEmbeddingDimensions = defaultEmbeddingDimensions > 0 ? defaultEmbeddingDimensions : throw new ArgumentException("DefaultEmbeddingDimensions must be positive.", nameof(defaultEmbeddingDimensions));
        DefaultEnableMemory = defaultEnableMemory;
        DefaultEnableRAG = defaultEnableRAG;
        DefaultEmbeddingModelName = defaultEmbeddingModelName?.Trim();
        DefaultBotType = defaultBotType;
        DefaultSystemPrompt = defaultSystemPrompt?.Trim();
    }

    public void Update(
        Guid providerId,
        string commercialName,
        string technicalName,
        int tokenLimit,
        string capabilities,
        double defaultTemperature = 0.7,
        int defaultTopK = 3,
        int defaultMaxTokens = 2048,
        int defaultEmbeddingDimensions = 1536,
        bool defaultEnableMemory = true,
        bool defaultEnableRAG = true,
        string? defaultEmbeddingModelName = null,
        BotType defaultBotType = BotType.Chat,
        string? defaultSystemPrompt = null)
    {
        ProviderId = NormalizeProviderId(providerId);
        CommercialName = NormalizeRequired(commercialName, nameof(commercialName));
        TechnicalName = NormalizeRequired(technicalName, nameof(technicalName));
        TokenLimit = tokenLimit > 0 ? tokenLimit : throw new ArgumentException("TokenLimit must be positive.", nameof(tokenLimit));
        Capabilities = capabilities?.Trim() ?? string.Empty;
        DefaultTemperature = ClampTemperature(defaultTemperature);
        DefaultTopK = defaultTopK > 0 ? defaultTopK : throw new ArgumentException("DefaultTopK must be positive.", nameof(defaultTopK));
        DefaultMaxTokens = defaultMaxTokens > 0 ? defaultMaxTokens : throw new ArgumentException("DefaultMaxTokens must be positive.", nameof(defaultMaxTokens));
        DefaultEmbeddingDimensions = defaultEmbeddingDimensions > 0 ? defaultEmbeddingDimensions : throw new ArgumentException("DefaultEmbeddingDimensions must be positive.", nameof(defaultEmbeddingDimensions));
        DefaultEnableMemory = defaultEnableMemory;
        DefaultEnableRAG = defaultEnableRAG;
        DefaultEmbeddingModelName = defaultEmbeddingModelName?.Trim();
        DefaultBotType = defaultBotType;
        DefaultSystemPrompt = defaultSystemPrompt?.Trim();
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }

    private static Guid NormalizeProviderId(Guid providerId)
    {
        if (providerId == Guid.Empty) throw new ArgumentException("ProviderId is required.", nameof(providerId));
        return providerId;
    }

    private static double ClampTemperature(double value) => value >= 0 && value <= 2 ? value : 0.7;
}