using MyApp.Crm.Domain.Notes;
using MyApp.Crm.Domain.Tags;
using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Leads;

public class Lead(Guid id) : AuditableEntity<Guid>(id)
{
    public string Title { get; private set; } = string.Empty;
    public string? Source { get; private set; }

    public string? ContactName { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }

    public Guid? CustomerId { get; private set; }

    public LeadStatus Status { get; private set; } = LeadStatus.New;

    public string OwnerUsername { get; private set; } = string.Empty;

    public List<Note> Notes { get; private set; } = new();
    public List<LeadTag> Tags { get; private set; } = new();

    public Lead(Guid id, string title, string ownerUsername, string? source = null) : this(id)
    {
        Title = NormalizeRequired(title, nameof(title));
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
        Source = NormalizeOptional(source);
        Status = LeadStatus.New;
    }

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

    public void AssignTo(string ownerUsername)
    {
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
    }

    public void Disqualify(string reasonNote)
    {
        EnsureMutable();
        Status = LeadStatus.Disqualified;
        Notes.Add(Note.ForLead(Guid.NewGuid(), reasonNote, Id));
    }

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

