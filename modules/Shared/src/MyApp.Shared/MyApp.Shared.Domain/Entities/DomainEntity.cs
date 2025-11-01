using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Entities
{
    public class DomainEntity<T> : IEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        [Key]
        public T Id { get; set; }
    }
}
