namespace MyApp.Crm.Domain.Tags;

/// <summary>
/// Provides Opportunity Tag functionality.
/// </summary>
public class OpportunityTag
{
    /// <summary>Gets or sets Opportunity Id.</summary>
    public Guid OpportunityId { get; set; }
    /// <summary>Gets or sets Tag Id.</summary>
    public Guid TagId { get; set; }

    /// <summary>Gets or sets Tag.</summary>
    public Tag Tag { get; set; } = default!;
}

