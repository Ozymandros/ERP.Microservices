namespace MyApp.Shared.Domain.DTOs
{
    /// <summary>
    /// Base DTO class for auditable entities that tracks creation and modification metadata.
    /// </summary>
    public abstract record AuditableDto<T>(T Id) : BaseDto<T>(Id), IAuditableDto<T>
        where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        /// <summary>
        /// Initializes a new instance of the AuditableDto class with audit information.
        /// </summary>
        protected AuditableDto(T id,
            DateTime createdAt,
            string createdBy,
            DateTime? updatedAt,
            string? updatedBy) : this(id)
        {
            CreatedAt = createdAt;
            CreatedBy = createdBy;
            UpdatedAt = updatedAt;
            UpdatedBy = updatedBy;
        }

        /// <summary>
        /// Gets the timestamp when the entity was created.
        /// </summary>
        public virtual DateTime CreatedAt { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Gets the username of the user who created the entity.
        /// </summary>
        public virtual string CreatedBy { get; init; } = string.Empty;

        /// <summary>
        /// Gets the timestamp when the entity was last modified.
        /// </summary>
        public virtual DateTime? UpdatedAt { get; init; }

        /// <summary>
        /// Gets the username of the user who last modified the entity.
        /// </summary>
        public virtual string? UpdatedBy { get; init; }
    }


}
