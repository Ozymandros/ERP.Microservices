using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.DTOs
{
    public abstract record BaseDto<T>(T Id) : IDto<T>
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        // Tornem a declarar la propietat per canviar l'init per un set.
        // Això satisfà la interfície IDto<T>.Id.set
        [Key]
        public T Id { get; set; } = Id;
    }
}