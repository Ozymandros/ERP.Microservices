namespace MyApp.Shared.Domain.Entities
{
    public abstract class AuditableEntity<T>(T id) : DomainEntity<T>(id), IAuditableEntity<T> 
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
{
    public virtual DateTime CreatedAt { get; set; }
    public virtual string CreatedBy { get; set; } = string.Empty;
    public virtual DateTime? UpdatedAt { get; set; }
    public virtual string? UpdatedBy { get; set; }
}
}
