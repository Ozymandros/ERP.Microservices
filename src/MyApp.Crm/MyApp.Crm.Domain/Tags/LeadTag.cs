namespace MyApp.Crm.Domain.Tags;

public class LeadTag
{
    public Guid LeadId { get; set; }
    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = default!;
}

