using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Notes;

public class Note(Guid id) : AuditableEntity<Guid>(id)
{
    public string Body { get; private set; } = string.Empty;

    public Guid? LeadId { get; private set; }
    public Guid? OpportunityId { get; private set; }
    public Guid? ActivityId { get; private set; }

    public static Note ForLead(Guid id, string body, Guid leadId) => new(id)
    {
        Body = NormalizeBody(body),
        LeadId = leadId
    };

    public static Note ForOpportunity(Guid id, string body, Guid opportunityId) => new(id)
    {
        Body = NormalizeBody(body),
        OpportunityId = opportunityId
    };

    public static Note ForActivity(Guid id, string body, Guid activityId) => new(id)
    {
        Body = NormalizeBody(body),
        ActivityId = activityId
    };

    public void UpdateBody(string body) => Body = NormalizeBody(body);

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Note body is required.", nameof(body));
        return body.Trim();
    }
}

