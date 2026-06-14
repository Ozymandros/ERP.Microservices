using MyApp.Shared.Domain.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyApp.Audit.Domain;

/// <summary>
/// Represents a change to a single property within an audited entity.
/// Tracks the original and new values of a property when an entity is modified.
/// </summary>
/// <param name="id">The unique identifier for the property change record.</param>
public class PropertyChange(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>
    /// Gets or sets the identifier of the parent entity change that contains this property change.
    /// </summary>
    public Guid EntityChangeId { get; set; }
    
    /// <summary>
    /// Gets or sets the name of the property that was changed.
    /// </summary>
    public string PropertyName { get; set; } = default!;

    /// <summary>
    /// Gets or sets the original value of the property before the change.
    /// Null if the property had no value or the entity was newly created.
    /// </summary>
    public string? OriginalValue { get; set; }
    
    /// <summary>
    /// Gets or sets the new value of the property after the change.
    /// Null if the property was cleared or the entity was deleted.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the parent entity change record that contains this property change.
    /// </summary>
    public virtual EntityChange EntityChange { get; set; } = default!;
}