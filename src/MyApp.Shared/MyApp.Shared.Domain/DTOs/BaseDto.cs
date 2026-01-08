using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.DTOs
{
    public abstract record BaseDto<T>(T Id) : IDto<T>
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        // Re-declare the property to change the init to an init.
        // This satisfies the IDto<T>.Id.init interface
        [Key]
        public T Id { get; init; } = Id;
    }
}