using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Tags;

/// <summary>
/// Tag.
/// </summary>
/// <param name="id">The id.</param>
public class Tag(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>Gets or sets Name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Initializes a new instance of the Tag class with a name.</summary>
    /// Initializes a new instance of the Tag class.
    /// <param name="id">The id.</param>
    /// <param name="name">The name.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public Tag(Guid id, string name) : this(id)
    {
        Rename(name);
    }

    /// <summary>Rename.</summary>
    /// <param name="name">The name.</param>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name is required.", nameof(name));
        Name = name.Trim();
    }
}

