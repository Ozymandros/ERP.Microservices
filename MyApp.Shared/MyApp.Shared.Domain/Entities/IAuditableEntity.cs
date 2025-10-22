
namespace MyApp.Shared.Domain.Entities
{
    public interface IAuditableEntity<T>: IAuditableEntity, IEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>;

    public interface IAuditableEntity
    {
        DateTime CreatedAt { get; set; }
        string CreatedBy { get; set; }
        DateTime? UpdatedAt { get; set; }
        string? UpdatedBy { get; set; }
    }
}