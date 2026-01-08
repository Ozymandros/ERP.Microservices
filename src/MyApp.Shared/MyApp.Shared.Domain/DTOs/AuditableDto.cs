namespace MyApp.Shared.Domain.DTOs
{
    public abstract record AuditableDto<T>(T Id) : BaseDto<T>(Id), IAuditableDto<T>
        where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
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

        public virtual DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public virtual string CreatedBy { get; init; } = string.Empty;
        public virtual DateTime? UpdatedAt { get; init; }
        public virtual string? UpdatedBy { get; init; }
    }


}
