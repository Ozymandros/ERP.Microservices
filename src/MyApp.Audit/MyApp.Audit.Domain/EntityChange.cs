using MyApp.Shared.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Audit.Domain;

/// <summary>
/// Represents a recorded change to a domain entity within the audit trail.
/// Captures the entity's identity, the type of operation performed, and the full
/// before/after state as serialized JSON. Individual property-level differences
/// are tracked through the <see cref="PropertyChanges"/> collection.
/// </summary>
public class EntityChange(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>
    /// Gets or sets the fully-qualified or simple name of the entity type that was changed
    /// (e.g., <c>"Product"</c>, <c>"SalesOrder"</c>).
    /// </summary>
    public string EntityName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier of the specific entity instance that was changed.
    /// </summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// Gets or sets the serialized JSON representation of the entity's state
    /// <em>before</em> the change was applied. <see langword="null"/> when
    /// <see cref="ChangeType"/> is <see cref="ChangeTypeEnum.Created"/>.
    /// </summary>
    [Column(TypeName = "json")]
    public string? OriginalValue { get; set; }

    /// <summary>
    /// Gets or sets the serialized JSON representation of the entity's state
    /// <em>after</em> the change was applied. <see langword="null"/> when
    /// <see cref="ChangeType"/> is <see cref="ChangeTypeEnum.Deleted"/>.
    /// </summary>
    [Column(TypeName = "json")]
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the type of operation that produced this audit record.
    /// Possible values are <see cref="ChangeTypeEnum.Created"/>,
    /// <see cref="ChangeTypeEnum.Updated"/>, and <see cref="ChangeTypeEnum.Deleted"/>.
    /// </summary>
    public ChangeTypeEnum ChangeType { get; set; } = default!;

    /// <summary>
    /// Gets or sets the collection of individual property-level changes that occurred
    /// during this entity change. Each <see cref="PropertyChange"/> entry captures
    /// the name of the property together with its before and after values.
    /// </summary>
    public virtual ICollection<PropertyChange> PropertyChanges { get; set; } = new List<PropertyChange>();
}