namespace MyApp.Shared.Domain.Entities
{
    /// <summary>
    /// Base class for auditable domain entities that tracks creation and modification metadata.
    /// </summary>
    public abstract class AuditableEntity<T>(T id) : DomainEntity<T>(id), IAuditableEntity<T>
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        /// <summary>
        /// Gets or sets the timestamp when the entity was created.
        /// </summary>
        public virtual DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who created the entity.
        /// </summary>
        public virtual string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp when the entity was last modified, or null if never modified.
        /// </summary>
        public virtual DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the username of the user who last modified the entity.
        /// </summary>
        public virtual string? UpdatedBy { get; set; }
    }
}
