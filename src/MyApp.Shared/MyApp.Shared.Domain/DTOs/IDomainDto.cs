namespace MyApp.Shared.Domain.DTOs
{
    public interface IDto<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        T Id { get; set; }
    }
}