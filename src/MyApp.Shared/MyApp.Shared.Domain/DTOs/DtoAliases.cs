namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    /// <summary>
    /// Represents the Base Guid Dto data record.
    /// </summary>
    public abstract record BaseGuidDto(Guid Id) : BaseDto<Guid>(Id);
    /// <summary>
    /// Represents the Base Int Dto data record.
    /// </summary>
    public abstract record BaseIntDto(int Id) : BaseDto<int>(Id);
    /// <summary>
    /// Represents the Base Long Dto data record.
    /// </summary>
    public abstract record BaseLongDto(long Id) : BaseDto<long>(Id);

    // Auditable DTO aliases for common ID types
    // Inherited properties (CreatedAt, CreatedBy, etc.) are not redefined
    /// <summary>
    /// Represents the Auditable Guid Dto data record.
    /// </summary>
    public abstract record AuditableGuidDto(Guid Id) : AuditableDto<Guid>(Id);
    /// <summary>
    /// Represents the Auditable Int Dto data record.
    /// </summary>
    public abstract record AuditableIntDto(int Id) : AuditableDto<int>(Id);
    /// <summary>
    /// Represents the Auditable Long Dto data record.
    /// </summary>
    public abstract record AuditableLongDto(long Id) : AuditableDto<long>(Id);
}
