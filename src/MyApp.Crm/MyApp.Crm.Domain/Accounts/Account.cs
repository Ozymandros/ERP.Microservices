using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Represents a customer account in the CRM system.</summary>
public sealed class Account(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the customer ID associated with this account.</summary>
    public Guid CustomerId { get; private set; }
    /// <summary>Gets the account name.</summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>Gets the tax identification number.</summary>
    public string? TaxId { get; private set; }
    /// <summary>Gets the billing address.</summary>
    public string? BillingAddress { get; private set; }
    /// <summary>Gets the shipping address.</summary>
    public string? ShippingAddress { get; private set; }
    /// <summary>Gets whether the account is active.</summary>
    public bool IsActive { get; private set; } = true;
    /// <summary>Gets the username of the account owner.</summary>
    public string? OwnerUsername { get; private set; }
    /// <summary>Gets the last synchronization time with external systems.</summary>
    public DateTimeOffset LastSyncedAt { get; private set; }

    /// <summary>Gets the list of contacts associated with this account.</summary>
    public List<Contact> Contacts { get; private set; } = new();

    /// <summary>Initializes a new instance of the Account class.</summary>
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

    /// <summary>Updates account details from a sales snapshot.</summary>
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

    /// <summary>Sets the owner of the account.</summary>
    public void SetOwner(string ownerUsername)
    {
        OwnerUsername = NormalizeRequired(ownerUsername, nameof(ownerUsername));
    }

    /// <summary>Adds a contact to this account.</summary>
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

    /// <summary>Sets a contact as the primary contact for this account.</summary>
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

