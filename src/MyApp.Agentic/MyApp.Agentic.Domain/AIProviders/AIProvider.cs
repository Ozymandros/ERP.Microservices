using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIProviders;

/// <summary>
/// Ai provider.
/// </summary>
/// <param name="id">The id.</param>
public class AIProvider(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the display name of the AI provider.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the base URL of the provider's chat completions API.</summary>
    public string BaseUrl { get; private set; } = string.Empty;
    /// <summary>Gets the encrypted API key used to authenticate requests to this provider.</summary>
    public string? EncryptedApiKey { get; private set; }
    /// <summary>Gets the default sampling temperature applied to models that do not override it.</summary>
    public double DefaultTemperature { get; private set; } = 0.7;
    /// <summary>Gets the default top-K retrieval count for RAG memory lookups.</summary>
    public int DefaultTopK { get; private set; } = 3;
    /// <summary>Gets the default maximum number of output tokens per completion.</summary>
    public int DefaultMaxTokens { get; private set; } = 2048;
    /// <summary>Gets the default embedding vector dimensionality for memory storage.</summary>
    public int DefaultEmbeddingDimensions { get; private set; } = 1536;
    /// <summary>Gets a value indicating whether memory is enabled by default for this provider's models.</summary>
    public bool DefaultEnableMemory { get; private set; } = false;
    /// <summary>Gets a value indicating whether retrieval-augmented generation is enabled by default.</summary>
    public bool DefaultEnableRAG { get; private set; } = false;
    /// <summary>Gets the optional default embedding model name used for memory vector generation.</summary>
    public string? DefaultEmbeddingModelName { get; private set; }
    /// <summary>Gets the default bot type (Chat or Agent) for agents using this provider's models.</summary>
    public BotType DefaultBotType { get; private set; } = BotType.Chat;
    /// <summary>Gets the optional default system prompt applied to agents using this provider's models.</summary>
    public string? DefaultSystemPrompt { get; private set; }

    /// <summary>Gets the collection of <see cref="AIModel"/> instances registered under this provider.</summary>
    public ICollection<AIModel> Models { get; private set; } = new List<AIModel>();

    /// <summary>
    /// Initializes a new instance of the AIProvider class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    /// <param name="baseUrl">The base Url.</param>
    /// <param name="encryptedApiKey">The encrypted Api Key.</param>
    /// <param name="defaultTemperature">The default Temperature.</param>
    /// <param name="defaultTopK">The default Top K.</param>
    /// <param name="defaultMaxTokens">The default Max Tokens.</param>
    /// <param name="defaultEmbeddingDimensions">The default Embedding Dimensions.</param>
    /// <param name="defaultEnableMemory">The default Enable Memory.</param>
    /// <param name="defaultEnableRAG">The default Enable RAG.</param>
    /// <param name="defaultEmbeddingModelName">The default Embedding Model Name.</param>
    /// <param name="defaultBotType">The default Bot Type.</param>
    /// <param name="defaultSystemPrompt">The default System Prompt.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="baseUrl"/> is blank, or any numeric limit is not positive.
    /// </exception>
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

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="baseUrl">The base Url.</param>
    /// <param name="encryptedApiKey">The encrypted Api Key.</param>
    /// <param name="defaultTemperature">The default Temperature.</param>
    /// <param name="defaultTopK">The default Top K.</param>
    /// <param name="defaultMaxTokens">The default Max Tokens.</param>
    /// <param name="defaultEmbeddingDimensions">The default Embedding Dimensions.</param>
    /// <param name="defaultEnableMemory">The default Enable Memory.</param>
    /// <param name="defaultEnableRAG">The default Enable RAG.</param>
    /// <param name="defaultEmbeddingModelName">The default Embedding Model Name.</param>
    /// <param name="defaultBotType">The default Bot Type.</param>
    /// <param name="defaultSystemPrompt">The default System Prompt.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name"/> or <paramref name="baseUrl"/> is blank, or numeric limits are not positive.
    /// </exception>
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
