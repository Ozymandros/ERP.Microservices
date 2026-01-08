namespace MyApp.Shared.Domain.DTOs
{
    public interface IAuditableDto<T>
        : IAuditableDto, IDto<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        //T Id { get; set; }
    }

    public interface IAuditableDto
    {
        DateTime CreatedAt { get; init; }
        string CreatedBy { get; init; }
        DateTime? UpdatedAt { get; init; }
        string? UpdatedBy { get; init; }
    }
}