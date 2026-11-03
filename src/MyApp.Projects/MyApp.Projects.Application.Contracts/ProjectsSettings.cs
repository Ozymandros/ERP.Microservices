namespace MyApp.Projects.Application.Contracts;

/// <summary>Projects service settings (configuration section <c>Projects</c>).</summary>
public class ProjectsSettings
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Projects";

    /// <summary>Gets or sets the local currency (LCY) code. Costs are always stored in LCY.</summary>
    public string LocalCurrency { get; set; } = "EUR";

    /// <summary>Gets or sets the project number prefix (number format: {Prefix}-{yyyy}-{00000}).</summary>
    public string NumberPrefix { get; set; } = "PRJ";
}
