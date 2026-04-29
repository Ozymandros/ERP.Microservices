namespace MyApp.Crm.Domain.Tags;

/// <summary>
/// Provides Lead Tag functionality.
/// </summary>
public class LeadTag
{
    /// <summary>Gets or sets Lead Id.</summary>
    public Guid LeadId { get; set; }
    /// <summary>Gets or sets Tag Id.</summary>
    public Guid TagId { get; set; }

    /// <summary>Gets or sets Tag.</summary>
    public Tag Tag { get; set; } = default!;
}

