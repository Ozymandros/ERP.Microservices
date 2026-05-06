using MyApp.Agentic.Domain.AIModels;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Agentic.Domain.AIProviders;

public class AIProvider(Guid id) : AuditableEntity<Guid>(id)
{
    public string Name { get; private set; } = string.Empty;
    public string BaseUrl { get; private set; } = string.Empty;
    public string SecretKeyName { get; private set; } = string.Empty;

    public ICollection<AIModel> Models { get; private set; } = new List<AIModel>();

    public AIProvider(Guid id, string name, string baseUrl, string secretKeyName) : this(id)
    {
        Name = NormalizeRequired(name, nameof(name));
        BaseUrl = NormalizeRequired(baseUrl, nameof(baseUrl));
        SecretKeyName = NormalizeRequired(secretKeyName, nameof(secretKeyName));
    }

    public void Update(string name, string baseUrl, string secretKeyName)
    {
        Name = NormalizeRequired(name, nameof(name));
        BaseUrl = NormalizeRequired(baseUrl, nameof(baseUrl));
        SecretKeyName = NormalizeRequired(secretKeyName, nameof(secretKeyName));
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}