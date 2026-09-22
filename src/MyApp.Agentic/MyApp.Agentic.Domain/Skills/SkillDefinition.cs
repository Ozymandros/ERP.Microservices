namespace MyApp.Agentic.Domain.Skills;

/// <summary>
/// Defines a named skill that bundles system instructions and required tools for an agent.
/// </summary>
public class SkillDefinition
{
    /// <summary>Gets the unique identifier of the skill.</summary>
    public Guid Id { get; private set; }
    /// <summary>Gets the canonical name used to look up the skill.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the ERP domain this skill targets (for example "Inventory", "Sales").</summary>
    public string Domain { get; private set; } = string.Empty;
    /// <summary>Gets the semantic version of the skill definition.</summary>
    public string Version { get; private set; } = "1.0.0";
    /// <summary>Gets the system-prompt instructions injected when this skill is active.</summary>
    public string Instructions { get; private set; } = string.Empty;
    /// <summary>Gets the list of tool names required by this skill.</summary>
    public List<string> RequiredTools { get; private set; } = new();
    /// <summary>Gets the list of plugin dependencies needed to run this skill.</summary>
    public List<string> PluginDependencies { get; private set; } = new();
    /// <summary>Gets additional metadata associated with this skill.</summary>
    public Dictionary<string, object> Metadata { get; private set; } = new();
    /// <summary>Gets a value indicating whether this skill is currently active.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Gets the UTC timestamp when this skill was created.</summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>Gets the UTC timestamp of the most recent update, if any.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the SkillDefinition class.
    /// </summary>
    /// <param name="id">The id.</param>
    public SkillDefinition(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the SkillDefinition class.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    /// <param name="domain">The domain.</param>
    /// <param name="instructions">The instructions.</param>
    /// <param name="requiredTools">The required Tools.</param>
    /// <param name="pluginDependencies">The plugin Dependencies.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> or <paramref name="domain"/> is blank.</exception>
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

    /// <summary>
    /// Updates the instructions.
    /// </summary>
    /// <param name="instructions">The instructions.</param>
    public void UpdateInstructions(string instructions)
    {
        Instructions = instructions?.Trim() ?? string.Empty;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the tools.
    /// </summary>
    /// <param name="tools">The tools.</param>
    public void UpdateTools(List<string> tools)
    {
        RequiredTools = tools;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a tool.
    /// </summary>
    /// <param name="tool">The tool.</param>
    public void AddTool(string tool)
    {
        if (!RequiredTools.Contains(tool))
        {
            RequiredTools.Add(tool);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>Sets <see cref="IsActive"/> to <see langword="true"/>.</summary>
    public void Activate() => IsActive = true;
    /// <summary>Sets <see cref="IsActive"/> to <see langword="false"/>.</summary>
    public void Deactivate() => IsActive = false;

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}