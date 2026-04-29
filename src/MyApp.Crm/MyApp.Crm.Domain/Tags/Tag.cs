using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Tags;

/// <summary>
/// Provides Tag functionality.
/// </summary>
public class Tag(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; private set; } = string.Empty;

    public Tag(Guid id, string name) : this(id)
    {
        Rename(name);
    }

    /// <summary>Rename.</summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name is required.", nameof(name));
        Name = name.Trim();
    }
}

