namespace MyApp.Crm.Domain.Tags;

public class OpportunityTag
{
    public Guid OpportunityId { get; set; }
    public Guid TagId { get; set; }

    public Tag Tag { get; set; } = default!;
}

