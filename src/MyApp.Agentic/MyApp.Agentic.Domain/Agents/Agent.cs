using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Agents;

/// <summary>
/// Agent.
/// and optional ERP plugins.
/// </summary>
/// <param name="id">The id.</param>
public class Agent(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the display name of the agent.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets a human-readable description of the agent's purpose.</summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>Gets the identifier of the <see cref="AIModel"/> this agent uses.</summary>
    public Guid ModelId { get; private set; }
    /// <summary>Gets the operational mode of the agent (Chat or Agent).</summary>
    public BotType BotType { get; private set; } = BotType.Chat;

    /// <summary>Gets the sampling temperature used during model inference.</summary>
    public double Temperature { get; private set; } = 0.7;
    /// <summary>Gets the top-K retrieval count used for RAG memory lookups.</summary>
    public int TopK { get; private set; } = 3;
    /// <summary>Gets the maximum number of output tokens per completion.</summary>
    public int MaxTokens { get; private set; } = 2048;
    /// <summary>Gets the embedding vector dimensionality for memory storage.</summary>
    public int EmbeddingDimensions { get; private set; } = 1536;
    /// <summary>Gets a value indicating whether long-term memory is enabled for this agent.</summary>
    public bool EnableMemory { get; private set; } = true;
    /// <summary>Gets a value indicating whether retrieval-augmented generation is enabled.</summary>
    public bool EnableRAG { get; private set; } = true;
    /// <summary>Gets the optional embedding model name used for memory vector generation.</summary>
    public string? EmbeddingModelName { get; private set; }

    /// <summary>Gets the system instructions prepended to every conversation turn.</summary>
    public string SystemInstructions { get; private set; } = string.Empty;
    /// <summary>Gets a value indicating whether this agent is currently active.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Gets the optional user identifier that owns this agent; null means shared.</summary>
    public string? OwnerUserId { get; private set; }

    /// <summary>Gets the <see cref="AIModel"/> navigation property.</summary>
    public AIModel? Model { get; private set; }
    /// <summary>Gets the collection of <see cref="AgentPlugin"/> instances configured for this agent.</summary>
    public ICollection<AgentPlugin> Plugins { get; private set; } = new List<AgentPlugin>();
    /// <summary>Gets the collection of <see cref="AgentSession"/> instances associated with this agent.</summary>
    public ICollection<AgentSession> Sessions { get; private set; } = new List<AgentSession>();

    /// <summary>
    /// Initializes a new instance of the Agent class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="modelId">The model Id.</param>
    /// <param name="temperature">The temperature.</param>
    /// <param name="systemInstructions">The system Instructions.</param>
    /// <param name="ownerUserId">The owner User Id.</param>
    /// <param name="topK">The top K.</param>
    /// <param name="maxTokens">The max Tokens.</param>
    /// <param name="embeddingDimensions">The embedding Dimensions.</param>
    /// <param name="enableMemory">The enable Memory.</param>
    /// <param name="enableRAG">The enable RAG.</param>
    /// <param name="embeddingModelName">The embedding Model Name.</param>
    /// <param name="botType">The bot Type.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is blank.</exception>
    public Agent(
        Guid id,
        string name,
        string description,
        Guid modelId,
        double temperature,
        string systemInstructions,
        string? ownerUserId = null,
        int topK = 3,
        int maxTokens = 2048,
        int embeddingDimensions = 1536,
        bool enableMemory = true,
        bool enableRAG = true,
        string? embeddingModelName = null,
        BotType botType = BotType.Chat) : this(id)
    {
        Name = NormalizeRequired(name, nameof(name));
        Description = description?.Trim() ?? string.Empty;
        ModelId = modelId;
        Temperature = ClampTemperature(temperature);
        SystemInstructions = systemInstructions?.Trim() ?? string.Empty;
        OwnerUserId = string.IsNullOrWhiteSpace(ownerUserId) ? null : ownerUserId.Trim();
        IsActive = true;
        TopK = topK > 0 ? topK : 3;
        MaxTokens = maxTokens > 0 ? maxTokens : 2048;
        EmbeddingDimensions = embeddingDimensions > 0 ? embeddingDimensions : 1536;
        EnableMemory = enableMemory;
        EnableRAG = enableRAG;
        EmbeddingModelName = embeddingModelName?.Trim();
        BotType = botType;
    }

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="description">The description.</param>
    /// <param name="modelId">The model Id.</param>
    /// <param name="temperature">The temperature.</param>
    /// <param name="systemInstructions">The system Instructions.</param>
    /// <param name="topK">The top K.</param>
    /// <param name="maxTokens">The max Tokens.</param>
    /// <param name="embeddingDimensions">The embedding Dimensions.</param>
    /// <param name="enableMemory">The enable Memory.</param>
    /// <param name="enableRAG">The enable RAG.</param>
    /// <param name="embeddingModelName">The embedding Model Name.</param>
    /// <param name="botType">The bot Type.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is blank.</exception>
    public void Update(
        string name,
        string description,
        Guid modelId,
        double temperature,
        string systemInstructions,
        int? topK = null,
        int? maxTokens = null,
        int? embeddingDimensions = null,
        bool? enableMemory = null,
        bool? enableRAG = null,
        string? embeddingModelName = null,
        BotType? botType = null)
    {
        Name = NormalizeRequired(name, nameof(name));
        Description = description?.Trim() ?? string.Empty;

        var modelChanged = ModelId != modelId;
        ModelId = modelId;
        if (modelChanged && Model is not null && Model.Id != modelId)
        {
            Model = null;
        }

        Temperature = ClampTemperature(temperature);
        SystemInstructions = systemInstructions?.Trim() ?? string.Empty;
        if (topK.HasValue) TopK = topK.Value > 0 ? topK.Value : 3;
        if (maxTokens.HasValue) MaxTokens = maxTokens.Value > 0 ? maxTokens.Value : 2048;
        if (embeddingDimensions.HasValue) EmbeddingDimensions = embeddingDimensions.Value > 0 ? embeddingDimensions.Value : 1536;
        if (enableMemory.HasValue) EnableMemory = enableMemory.Value;
        if (enableRAG.HasValue) EnableRAG = enableRAG.Value;
        EmbeddingModelName = embeddingModelName?.Trim();
        if (botType.HasValue) BotType = botType.Value;
    }

    /// <summary>
    /// Set model.
    /// </summary>
    /// <param name="model">The model.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="model"/> is <see langword="null"/>.</exception>
    public void SetModel(AIModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        ModelId = model.Id;
        Model = model;
    }

    /// <summary>Sets the agent's <see cref="IsActive"/> flag to <see langword="true"/>.</summary>
    public void Activate() { IsActive = true; }
    /// <summary>Sets the agent's <see cref="IsActive"/> flag to <see langword="false"/>.</summary>
    public void Deactivate() { IsActive = false; }

    private static double ClampTemperature(double value) => value >= 0 && value <= 2 ? value : 0.7;

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}