using MyApp.Crm.Domain.Notes;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Activities;

/// <summary>Represents an activity such as a task, call, meeting, or email.</summary>
public class Activity(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the activity subject or description.</summary>
    public string Subject { get; private set; } = string.Empty;
    /// <summary>Gets the type of activity.</summary>
    public ActivityType Type { get; private set; }
    /// <summary>Gets the status of the activity.</summary>
    public ActivityStatus Status { get; private set; } = ActivityStatus.Open;

    /// <summary>Gets the due date and time for the activity.</summary>
    public DateTimeOffset DueAt { get; private set; }
    /// <summary>Gets the date and time when the activity was completed.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Gets the username of the person assigned to this activity.</summary>
    public string AssignedToUsername { get; private set; } = string.Empty;

    /// <summary>Gets the lead ID if the activity is linked to a lead.</summary>
    public Guid? LeadId { get; private set; }
    /// <summary>Gets the opportunity ID if the activity is linked to an opportunity.</summary>
    public Guid? OpportunityId { get; private set; }
    /// <summary>Gets the customer ID if the activity is linked to a customer.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>Gets the list of notes attached to this activity.</summary>
    public List<Note> Notes { get; private set; } = new();

    /// <summary>Initializes a new instance of the Activity class.</summary>
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

    /// <summary>Reschedules the activity to a new due date.</summary>
    public void Reschedule(DateTimeOffset dueAt)
    {
        EnsureOpen();
        DueAt = dueAt;
    }

    /// <summary>Updates the activity subject.</summary>
    public void UpdateSubject(string subject)
    {
        EnsureOpen();
        Subject = NormalizeRequired(subject, nameof(subject));
    }

    /// <summary>Completes the activity with an optional note.</summary>
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

    /// <summary>Cancels the activity with a reason note.</summary>
    public void Cancel(string reasonNote)
    {
        EnsureOpen();
        Status = ActivityStatus.Cancelled;
        Notes.Add(Note.ForActivity(Guid.NewGuid(), reasonNote, Id));
    }

    /// <summary>Reassigns the activity to a different user.</summary>
    public void Reassign(string assignedToUsername)
    {
        AssignedToUsername = NormalizeRequired(assignedToUsername, nameof(assignedToUsername));
    }

    /// <summary>Links the activity to a lead, opportunity, or customer.</summary>
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

