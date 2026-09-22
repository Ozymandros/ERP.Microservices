namespace MyApp.Shared.Domain.DTOs
{
    /// <summary>
    /// Generic interface for data transfer objects with a primary key identifier.
    /// </summary>
    /// <typeparam name="T">The type of the DTO identifier.</typeparam>
    public interface IDto<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        T Id { get; init; }
    }
}