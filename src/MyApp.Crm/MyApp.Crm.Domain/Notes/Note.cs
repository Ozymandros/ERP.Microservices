using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Notes;

/// <summary>
/// Note.
/// </summary>
/// <param name="id">The id.</param>
public class Note(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Body.</summary>
    public string Body { get; private set; } = string.Empty;

    /// <summary>Gets or sets Lead Id.</summary>
    public Guid? LeadId { get; private set; }
    /// <summary>Gets or sets Opportunity Id.</summary>
    public Guid? OpportunityId { get; private set; }
    /// <summary>Gets or sets Activity Id.</summary>
    public Guid? ActivityId { get; private set; }

    /// <summary>Creates a note associated with a lead.</summary>
    /// For lead.
    /// <param name="id">The id.</param>
    /// <param name="body">The body.</param>
    /// <param name="leadId">The lead Id.</param>
    /// <returns>A new <see cref="Note"/> linked to the specified lead.</returns>
    public static Note ForLead(Guid id, string body, Guid leadId) => new(id)
    {
        Body = NormalizeBody(body),
        LeadId = leadId
    };

    /// <summary>For Opportunity.</summary>
    /// <param name="id">The id.</param>
    /// <param name="body">The body.</param>
    /// <param name="opportunityId">The opportunity Id.</param>
    public static Note ForOpportunity(Guid id, string body, Guid opportunityId) => new(id)
    {
        Body = NormalizeBody(body),
        OpportunityId = opportunityId
    };

    /// <summary>For Activity.</summary>
    /// <param name="id">The id.</param>
    /// <param name="body">The body.</param>
    /// <param name="activityId">The activity Id.</param>
    public static Note ForActivity(Guid id, string body, Guid activityId) => new(id)
    {
        Body = NormalizeBody(body),
        ActivityId = activityId
    };

    /// <summary>Update Body.</summary>
    /// <param name="body">The body.</param>
    public void UpdateBody(string body) => Body = NormalizeBody(body);

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) throw new ArgumentException("Note body is required.", nameof(body));
        return body.Trim();
    }
}

