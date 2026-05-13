namespace MyApp.Agentic.Domain.Skills;

public class SkillDefinition
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Domain { get; private set; } = string.Empty;
    public string Version { get; private set; } = "1.0.0";
    public string Instructions { get; private set; } = string.Empty;
    public List<string> RequiredTools { get; private set; } = new();
    public List<string> PluginDependencies { get; private set; } = new();
    public Dictionary<string, object> Metadata { get; private set; } = new();
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public SkillDefinition(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    public SkillDefinition(
        Guid id,
        string name,
        string domain,
        string instructions,
        List<string>? requiredTools = null,
        List<string>? pluginDependencies = null) : this(id)
    {
        Name = NormalizeRequired(name, nameof(name));
        Domain = NormalizeRequired(domain, nameof(domain));
        Instructions = instructions?.Trim() ?? string.Empty;
        RequiredTools = requiredTools ?? new List<string>();
        PluginDependencies = pluginDependencies ?? new List<string>();
    }

    public void UpdateInstructions(string instructions)
    {
        Instructions = instructions?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTools(List<string> tools)
    {
        RequiredTools = tools;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddTool(string tool)
    {
        if (!RequiredTools.Contains(tool))
        {
            RequiredTools.Add(tool);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}