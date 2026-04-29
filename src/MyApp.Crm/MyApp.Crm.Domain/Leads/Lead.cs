using MyApp.Crm.Domain.Notes;
using MyApp.Crm.Domain.Tags;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Leads;

/// <summary>Represents a sales lead in the CRM system.</summary>
public class Lead(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the lead title or subject.</summary>
    public string Title { get; private set; } = string.Empty;
    /// <summary>Gets the source of the lead.</summary>
    public string? Source { get; private set; }

    /// <summary>Gets the contact person's name.</summary>
    public string? ContactName { get; private set; }
    /// <summary>Gets the contact person's email address.</summary>
    public string? ContactEmail { get; private set; }
    /// <summary>Gets the contact person's phone number.</summary>
    public string? ContactPhone { get; private set; }

    /// <summary>Gets the customer ID if this lead has been qualified.</summary>
    public Guid? CustomerId { get; private set; }

    /// <summary>Gets the status of the lead.</summary>
    public LeadStatus Status { get; private set; } = LeadStatus.New;

    /// <summary>Gets the username of the lead owner.</summary>
    public string OwnerUsername { get; private set; } = string.Empty;

    /// <summary>Gets the list of notes attached to this lead.</summary>
    public List<Note> Notes { get; private set; } = new();
    /// <summary>Gets the list of tags associated with this lead.</summary>
    public List<LeadTag> Tags { get; private set; } = new();

    /// <summary>Initializes a new instance of the Lead class.</summary>
    public Lead(Guid id, string title, string ownerUsername, string? source = null) : this(id)
    {
        Title = NormalizeRequired(title, nameof(title));
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
        Source = NormalizeOptional(source);
        Status = LeadStatus.New;
    }

    /// <summary>Updates the lead's contact and other details.</summary>
    public void UpdateDetails(
        string title,
        string? source,
        string? contactName,
        string? contactEmail,
        string? contactPhone)
    {
        EnsureMutable();
        Title = NormalizeRequired(title, nameof(title));
        Source = NormalizeOptional(source);
        ContactName = NormalizeOptional(contactName);
        ContactEmail = NormalizeOptional(contactEmail);
        ContactPhone = NormalizeOptional(contactPhone);
    }

    /// <summary>Assigns the lead to a new owner.</summary>
    public void AssignTo(string ownerUsername)
    {
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
    }

    /// <summary>Disqualifies the lead with a reason note.</summary>
    public void Disqualify(string reasonNote)
    {
        EnsureMutable();
        Status = LeadStatus.Disqualified;
        Notes.Add(Note.ForLead(Guid.NewGuid(), reasonNote, Id));
    }

    /// <summary>Qualifies the lead and associates it with a customer.</summary>
    public void Qualify(Guid customerId)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        EnsureMutable();

        CustomerId = customerId;
        Status = LeadStatus.Qualified;
    }

    private void EnsureMutable()
    {
        if (Status != LeadStatus.New) throw new InvalidOperationException($"Lead cannot be modified when status is {Status}.");
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

