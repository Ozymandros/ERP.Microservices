using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.DTOs
{
    public abstract class BaseDto<T> : IDto<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        [Key]
        public virtual T Id { get; set; }
    }
}