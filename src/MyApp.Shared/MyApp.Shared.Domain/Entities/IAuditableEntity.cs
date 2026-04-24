
namespace MyApp.Shared.Domain.Entities
{
    /// <summary>
    /// Generic interface for auditable entities with creation and modification tracking.
    /// </summary>
    /// <typeparam name="T">The type of the entity identifier.</typeparam>
    public interface IAuditableEntity<T>
        : IAuditableEntity, IEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        //T Id { get; set; }
    }

    /// <summary>
    /// Non-generic interface for entities with audit metadata (creation and modification tracking).
    /// </summary>
    public interface IAuditableEntity
    {
        /// <summary>
        /// Gets or sets the timestamp when the entity was created.
        /// </summary>
        DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who created the entity.
        /// </summary>
        string CreatedBy { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the entity was last modified.
        /// </summary>
        DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who last modified the entity.
        /// </summary>
        string? UpdatedBy { get; set; }
    }
}