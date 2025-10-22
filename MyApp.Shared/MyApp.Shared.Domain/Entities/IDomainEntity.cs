
namespace MyApp.Shared.Domain.Entities
{
    public interface IEntity<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        T Id { get; set; }
    }
}