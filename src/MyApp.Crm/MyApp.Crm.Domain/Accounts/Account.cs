using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Accounts;

public sealed class Account(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? BillingAddress { get; private set; }
    public string? ShippingAddress { get; private set; }
    public bool IsActive { get; private set; } = true;
    public string? OwnerUsername { get; private set; }
    public DateTimeOffset LastSyncedAt { get; private set; }

    public List<Contact> Contacts { get; private set; } = new();

    public Account(
        Guid id,
        Guid customerId,
        string name,
        string? ownerUsername = null) : this(id)
    {
        if (customerId == Guid.Empty) throw new ArgumentException("CustomerId is required.", nameof(customerId));
        CustomerId = customerId;
        Name = NormalizeRequired(name, nameof(name));
        OwnerUsername = NormalizeOptional(ownerUsername);
        LastSyncedAt = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public void UpsertFromSalesSnapshot(
        string name,
        string? taxId,
        string? billingAddress,
        string? shippingAddress,
        DateTimeOffset? syncedAt = null)
    {
        Name = NormalizeRequired(name, nameof(name));
        TaxId = NormalizeOptional(taxId);
        BillingAddress = NormalizeOptional(billingAddress);
        ShippingAddress = NormalizeOptional(shippingAddress);
        LastSyncedAt = syncedAt ?? DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public void SetOwner(string ownerUsername)
    {
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
    }

    public Contact AddContact(
        Guid id,
        string fullName,
        string? email,
        string? phone,
        string? title,
        bool isPrimary)
    {
        var contact = new Contact(
            id,
            Id,
            fullName,
            email,
            phone,
            title,
            isPrimary);

        if (contact.IsPrimary)
        {
            UnsetOtherPrimaryContacts(contact.Id);
        }

        Contacts.Add(contact);
        return contact;
    }

    public void SetPrimaryContact(Guid contactId)
    {
        var contact = Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new InvalidOperationException($"Contact {contactId} not found for account {Id}.");

        contact.SetPrimary(true);
        UnsetOtherPrimaryContacts(contact.Id);
    }

    private void UnsetOtherPrimaryContacts(Guid keepPrimaryContactId)
    {
        foreach (var c in Contacts.Where(c => c.Id != keepPrimaryContactId && c.IsPrimary))
        {
            c.SetPrimary(false);
        }
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

