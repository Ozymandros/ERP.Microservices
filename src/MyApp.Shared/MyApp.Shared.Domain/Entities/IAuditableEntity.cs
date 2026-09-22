
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
        DateTime CreatedAt { get; set; }

        string CreatedBy { get; set; }

        DateTime? UpdatedAt { get; set; }

        string? UpdatedBy { get; set; }
    }
}