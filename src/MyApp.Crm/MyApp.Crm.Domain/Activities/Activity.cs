using MyApp.Crm.Domain.Notes;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Activities;

public class Activity(Guid id) : AuditableEntity<Guid>(id)
{
    public string Subject { get; private set; } = string.Empty;
    public ActivityType Type { get; private set; }
    public ActivityStatus Status { get; private set; } = ActivityStatus.Open;

    public DateTimeOffset DueAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public string AssignedToUsername { get; private set; } = string.Empty;

    public Guid? LeadId { get; private set; }
    public Guid? OpportunityId { get; private set; }
    public Guid? CustomerId { get; private set; }

    public List<Note> Notes { get; private set; } = new();

    public Activity(
        Guid id,
        string subject,
        ActivityType type,
        DateTimeOffset dueAt,
        string assignedToUsername,
        Guid? leadId = null,
        Guid? opportunityId = null,
        Guid? customerId = null) : this(id)
    {
        Subject = NormalizeRequired(subject, nameof(subject));
        Type = type;
        DueAt = dueAt;
        AssignedToUsername = NormalizeRequired(assignedToUsername, nameof(assignedToUsername));

        LinkTo(leadId, opportunityId, customerId);
    }

    public void Reschedule(DateTimeOffset dueAt)
    {
        EnsureOpen();
        DueAt = dueAt;
    }

    public void UpdateSubject(string subject)
    {
        EnsureOpen();
        Subject = NormalizeRequired(subject, nameof(subject));
    }

    public void Complete(string? note = null)
    {
        EnsureOpen();
        Status = ActivityStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(note))
        {
            Notes.Add(Note.ForActivity(Guid.NewGuid(), note, Id));
        }
    }

    public void Cancel(string reasonNote)
    {
        EnsureOpen();
        Status = ActivityStatus.Cancelled;
        Notes.Add(Note.ForActivity(Guid.NewGuid(), reasonNote, Id));
    }

    public void Reassign(string assignedToUsername)
    {
        AssignedToUsername = NormalizeRequired(assignedToUsername, nameof(assignedToUsername));
    }

    public void LinkTo(Guid? leadId, Guid? opportunityId, Guid? customerId)
    {
        // Exactly one parent link is required to keep navigation/querying predictable.
        var count = 0;
        if (leadId.HasValue) count++;
        if (opportunityId.HasValue) count++;
        if (customerId.HasValue) count++;

        if (count != 1)
            throw new ArgumentException("Activity must be linked to exactly one of LeadId, OpportunityId, or CustomerId.");

        LeadId = leadId;
        OpportunityId = opportunityId;
        CustomerId = customerId;
    }

    private void EnsureOpen()
    {
        if (Status != ActivityStatus.Open)
            throw new InvalidOperationException($"Activity cannot be modified when status is {Status}.");
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }
}

