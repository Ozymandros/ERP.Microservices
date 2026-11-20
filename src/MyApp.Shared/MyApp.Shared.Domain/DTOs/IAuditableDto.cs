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
        /// <summary>Gets the timestamp when the entity was created.</summary>
        DateTime CreatedAt { get; init; }

        /// <summary>Gets the username of the user who created the entity.</summary>
        string CreatedBy { get; init; }

        /// <summary>Gets the timestamp when the entity was last modified, or <see langword="null"/> if never modified.</summary>
        DateTime? UpdatedAt { get; init; }

        /// <summary>Gets the username of the user who last modified the entity, or <see langword="null"/> if never modified.</summary>
        string? UpdatedBy { get; init; }
    }
}