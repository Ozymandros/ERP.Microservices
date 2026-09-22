namespace MyApp.Shared.Domain.DTOs
{
    /// <summary>
    /// Generic interface for auditable DTOs with creation and modification tracking.
    /// </summary>
    /// <typeparam name="T">The type of the DTO identifier.</typeparam>
    public interface IAuditableDto<T>
        : IAuditableDto, IDto<T> where T : IComparable, IComparable<T>, IEquatable<T>, IFormattable, IParsable<T>
    {
        //T Id { get; set; }
    }

    /// <summary>
    /// Non-generic interface for DTOs with audit metadata (creation and modification tracking).
    /// </summary>
    public interface IAuditableDto
    {
        DateTime CreatedAt { get; init; }

        string CreatedBy { get; init; }

        DateTime? UpdatedAt { get; init; }

        string? UpdatedBy { get; init; }
    }
}