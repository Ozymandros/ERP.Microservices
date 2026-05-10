using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIProviders;

public class AIProvider(Guid id) : AuditableEntity<Guid>(id)
{
    public string Name { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public string? EncryptedApiKey { get; private set; }
    public double DefaultTemperature { get; private set; } = 0.7;
    public int DefaultTopK { get; private set; } = 3;
    public int DefaultMaxTokens { get; private set; } = 2048;
    public int DefaultEmbeddingDimensions { get; private set; } = 1536;
    public bool DefaultEnableMemory { get; private set; } = true;
    public bool DefaultEnableRAG { get; private set; } = true;
    public string? DefaultEmbeddingModelName { get; private set; }
    public BotType DefaultBotType { get; private set; } = BotType.Chat;
    public string? DefaultSystemPrompt { get; private set; }

    public ICollection<AIModel> Models { get; private set; } = new List<AIModel>();

    public AIProvider(
        Guid id,
        string name,
        string baseUrl,
        string? encryptedApiKey,
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
        Name = NormalizeRequired(name, nameof(name));
        BaseUrl = NormalizeRequired(baseUrl, nameof(baseUrl));
        EncryptedApiKey = NormalizeOptional(encryptedApiKey);
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
        string name,
        string baseUrl,
        string? encryptedApiKey,
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
        Name = NormalizeRequired(name, nameof(name));
        BaseUrl = NormalizeRequired(baseUrl, nameof(baseUrl));
        EncryptedApiKey = NormalizeOptional(encryptedApiKey);
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

    private static string? NormalizeOptional(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Trim();
    }

    private static double ClampTemperature(double value) => value >= 0 && value <= 2 ? value : 0.7;
}
