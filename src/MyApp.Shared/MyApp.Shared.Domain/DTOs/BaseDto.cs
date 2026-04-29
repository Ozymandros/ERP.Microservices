using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.DTOs
{
    /// <summary>
    /// Base DTO class for all data transfer objects with a primary key identifier.
    /// </summary>
    public abstract record BaseDto<T>(T Id) : IDto<T>
    where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        /// <summary>
        /// Gets the unique identifier for the DTO.
        /// </summary>
        [Key]
        public T Id { get; init; } = Id;
    }
}