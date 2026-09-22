using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIModels;

/// <summary>
/// Ai model.
/// </summary>
/// <param name="id">The id.</param>
public class AIModel(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the identifier of the owning <see cref="AIProvider"/>.</summary>
    public Guid ProviderId { get; private set; }
    /// <summary>Gets the human-readable commercial name of the model (for example "GPT-4o").</summary>
    public string CommercialName { get; private set; } = string.Empty;
    /// <summary>Gets the provider-specific technical model identifier used in API calls (for example "gpt-4o").</summary>
    public string TechnicalName { get; private set; } = string.Empty;
    /// <summary>Gets the maximum number of tokens the model supports in a single context window.</summary>
    public int TokenLimit { get; private set; }
    /// <summary>Gets a comma-separated list of capability tags (for example "chat,tool-calling,vision").</summary>
    public string Capabilities { get; private set; } = string.Empty;
    /// <summary>Gets the default sampling temperature used when no per-agent override is specified.</summary>
    public double DefaultTemperature { get; private set; } = 0.7;
    /// <summary>Gets the default top-K retrieval count for RAG memory lookups.</summary>
    public int DefaultTopK { get; private set; } = 3;
    /// <summary>Gets the default maximum number of output tokens per completion.</summary>
    public int DefaultMaxTokens { get; private set; } = 2048;
    /// <summary>Gets the default embedding vector dimensionality for memory storage.</summary>
    public int DefaultEmbeddingDimensions { get; private set; } = 1536;
    /// <summary>Gets a value indicating whether memory is enabled by default for agents using this model.</summary>
    public bool DefaultEnableMemory { get; private set; } = true;
    /// <summary>Gets a value indicating whether retrieval-augmented generation is enabled by default.</summary>
    public bool DefaultEnableRAG { get; private set; } = true;
    /// <summary>Gets the optional default embedding model name used for memory vector generation.</summary>
    public string? DefaultEmbeddingModelName { get; private set; }
    /// <summary>Gets the default bot type (Chat or Agent) for agents using this model.</summary>
    public BotType DefaultBotType { get; private set; } = BotType.Chat;
    /// <summary>Gets the optional default system prompt applied to agents using this model.</summary>
    public string? DefaultSystemPrompt { get; private set; }

    /// <summary>Gets the owning <see cref="AIProvider"/> navigation property.</summary>
    public AIProvider? Provider { get; private set; }
    /// <summary>Gets the collection of <see cref="Agent"/> instances configured to use this model.</summary>
    public ICollection<Agent> Agents { get; private set; } = new List<Agent>();

    /// <summary>
    /// Initializes a new instance of the AIModel class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="providerId">The provider Id.</param>
    /// <param name="commercialName">The commercial Name.</param>
    /// <param name="technicalName">The technical Name.</param>
    /// <param name="tokenLimit">The token Limit.</param>
    /// <param name="capabilities">The capabilities.</param>
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
    /// Thrown when <paramref name="providerId"/> is empty, <paramref name="commercialName"/> or
    /// <paramref name="technicalName"/> is blank, or any of the numeric limits is not positive.
    /// </exception>
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

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    /// <param name="providerId">The provider Id.</param>
    /// <param name="commercialName">The commercial Name.</param>
    /// <param name="technicalName">The technical Name.</param>
    /// <param name="tokenLimit">The token Limit.</param>
    /// <param name="capabilities">The capabilities.</param>
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
    /// Thrown when <paramref name="providerId"/> is empty, names are blank, or numeric limits are not positive.
    /// </exception>
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