using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Entities
{
    /// <summary>
    /// Base class for domain entities with a primary key identifier.
    /// </summary>
    public abstract class DomainEntity<T>(T id) : IEntity<T>
     where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        /// <summary>
        /// Gets or sets the unique identifier for the entity.
        /// </summary>
        [Key]
        public virtual T Id { get; set; } = id;
    }
}
