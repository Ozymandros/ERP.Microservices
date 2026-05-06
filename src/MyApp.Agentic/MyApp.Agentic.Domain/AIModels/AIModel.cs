using MyApp.Agentic.Domain.AIProviders;
using MyApp.Agentic.Domain.Agents;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIModels;

public class AIModel(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid ProviderId { get; private set; }
    public string TechnicalName { get; private set; } = string.Empty;
    public int TokenLimit { get; private set; }
    public string Capabilities { get; private set; } = string.Empty;

    public AIProvider? Provider { get; private set; }
    public ICollection<Agent> Agents { get; private set; } = new List<Agent>();

    public AIModel(Guid id, Guid providerId, string technicalName, int tokenLimit, string capabilities) : this(id)
    {
        ProviderId = providerId;
        TechnicalName = NormalizeRequired(technicalName, nameof(technicalName));
        TokenLimit = tokenLimit > 0 ? tokenLimit : throw new ArgumentException("TokenLimit must be positive.", nameof(tokenLimit));
        Capabilities = capabilities?.Trim() ?? string.Empty;
    }

    public void Update(Guid providerId, string technicalName, int tokenLimit, string capabilities)
    {
        ProviderId = providerId;
        TechnicalName = NormalizeRequired(technicalName, nameof(technicalName));
        TokenLimit = tokenLimit > 0 ? tokenLimit : throw new ArgumentException("TokenLimit must be positive.", nameof(tokenLimit));
        Capabilities = capabilities?.Trim() ?? string.Empty;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}