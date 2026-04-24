using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Represents a contact associated with a customer account.</summary>
public sealed class Contact(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets the account ID this contact belongs to.</summary>
    public Guid AccountId { get; private set; }

    /// <summary>Gets the contact's full name.</summary>
    public string FullName { get; private set; } = string.Empty;
    /// <summary>Gets the contact's email address.</summary>
    public string? Email { get; private set; }
    /// <summary>Gets the contact's phone number.</summary>
    public string? Phone { get; private set; }
    /// <summary>Gets the contact's job title.</summary>
    public string? Title { get; private set; }

    /// <summary>Gets whether this is the primary contact for the account.</summary>
    public bool IsPrimary { get; private set; }
    /// <summary>Gets whether the contact is active.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Initializes a new instance of the Contact class.</summary>
    public Contact(
        Guid id,
        Guid accountId,
        string fullName,
        string? email = null,
        string? phone = null,
        string? title = null,
        bool isPrimary = false) : this(id)
    {
        if (accountId == Guid.Empty) throw new ArgumentException("AccountId is required.", nameof(accountId));
        AccountId = accountId;
        FullName = NormalizeRequired(fullName, nameof(fullName));
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        Title = NormalizeOptional(title);
        IsPrimary = isPrimary;
        IsActive = true;
    }

    /// <summary>Updates the contact's information.</summary>
    public void Update(
        string fullName,
        string? email,
        string? phone,
        string? title)
    {
        FullName = NormalizeRequired(fullName, nameof(fullName));
        Email = NormalizeOptional(email);
        Phone = NormalizeOptional(phone);
        Title = NormalizeOptional(title);
    }

    /// <summary>Sets whether this contact is the primary contact.</summary>
    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

    /// <summary>Deactivates the contact.</summary>
    public void Deactivate()
    {
        IsActive = false;
        IsPrimary = false;
    }

    private static string NormalizeRequired(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value is required.", paramName);
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

