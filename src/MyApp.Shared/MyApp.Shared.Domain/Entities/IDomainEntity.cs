
namespace MyApp.Shared.Domain.Entities
{
    /// <summary>
    /// Generic interface for entities with a primary key identifier.
    /// </summary>
    /// <typeparam name="T">The type of the entity identifier.</typeparam>
    public interface IEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        T Id { get; set; }
    }
}