using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Accounts;

public sealed class Contact(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid AccountId { get; private set; }

    public string FullName { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Title { get; private set; }

    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; } = true;

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

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }

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

