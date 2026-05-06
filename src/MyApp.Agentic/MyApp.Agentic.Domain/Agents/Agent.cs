using MyApp.Agentic.Domain.AIModels;
using MyApp.Agentic.Domain.Sessions;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.Agents;

public class Agent(Guid id) : AuditableEntity<Guid>(id)
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid ModelId { get; private set; }
    public BotType BotType { get; private set; } = BotType.Chat;

    public double Temperature { get; private set; } = 0.7;
    public int TopK { get; private set; } = 3;
    public int MaxTokens { get; private set; } = 2048;
    public int EmbeddingDimensions { get; private set; } = 1536;
    public bool EnableMemory { get; private set; } = true;
    public bool EnableRAG { get; private set; } = true;
    public string? EmbeddingModelName { get; private set; }

    public string SystemInstructions { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public Guid? TenantId { get; private set; }

    public AIModel? Model { get; private set; }
    public ICollection<AgentPlugin> Plugins { get; private set; } = new List<AgentPlugin>();
    public ICollection<AgentSession> Sessions { get; private set; } = new List<AgentSession>();

    public Agent(
        Guid id,
        string name,
        string description,
        Guid modelId,
        double temperature,
        string systemInstructions,
        Guid? tenantId = null,
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
        TenantId = tenantId;
        IsActive = true;
        TopK = topK > 0 ? topK : 3;
        MaxTokens = maxTokens > 0 ? maxTokens : 2048;
        EmbeddingDimensions = embeddingDimensions > 0 ? embeddingDimensions : 1536;
        EnableMemory = enableMemory;
        EnableRAG = enableRAG;
        EmbeddingModelName = embeddingModelName?.Trim();
        BotType = botType;
    }

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
        ModelId = modelId;
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

    public void Activate() { IsActive = true; }
    public void Deactivate() { IsActive = false; }

    private static double ClampTemperature(double value) => value >= 0 && value <= 2 ? value : 0.7;

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}