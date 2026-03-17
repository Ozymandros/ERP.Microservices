using MyApp.Shared.Domain.Entities;

namespace MyApp.Crm.Domain.Tags;

public class Tag(Guid id) : AuditableEntity<Guid>(id)
{
    public string Name { get; private set; } = string.Empty;

    public Tag(Guid id, string name) : this(id)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Tag name is required.", nameof(name));
        Name = name.Trim();
    }
}

