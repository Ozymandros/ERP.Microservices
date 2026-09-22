namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    /// <summary>
    /// Base guid dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record BaseGuidDto(Guid Id) : BaseDto<Guid>(Id);
    /// <summary>
    /// Base int dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record BaseIntDto(int Id) : BaseDto<int>(Id);
    /// <summary>
    /// Base long dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record BaseLongDto(long Id) : BaseDto<long>(Id);

    // Auditable DTO aliases for common ID types
    // Inherited properties (CreatedAt, CreatedBy, etc.) are not redefined
    /// <summary>
    /// Auditable guid dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record AuditableGuidDto(Guid Id) : AuditableDto<Guid>(Id);
    /// <summary>
    /// Auditable int dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record AuditableIntDto(int Id) : AuditableDto<int>(Id);
    /// <summary>
    /// Auditable long dto.
    /// </summary>
    /// <param name="Id">The id.</param>
    public abstract record AuditableLongDto(long Id) : AuditableDto<long>(Id);
}
