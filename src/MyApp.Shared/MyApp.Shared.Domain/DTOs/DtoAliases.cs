namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    public abstract record BaseGuidDto(Guid Id) : BaseDto<Guid>(Id);
    public abstract record BaseIntDto(int Id) : BaseDto<int>(Id);
    public abstract record BaseLongDto(long Id) : BaseDto<long>(Id);

    // Auditable DTO aliases for common ID types
    public abstract record AuditableGuidDto(Guid Id, DateTime CreatedAt = default, string CreatedBy = "", DateTime? UpdatedAt = null, string? UpdatedBy = null) 
        : AuditableDto<Guid>(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
    public abstract record AuditableIntDto(int Id, DateTime CreatedAt = default, string CreatedBy = "", DateTime? UpdatedAt = null, string? UpdatedBy = null) 
        : AuditableDto<int>(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
    public abstract record AuditableLongDto(long Id, DateTime CreatedAt = default, string CreatedBy = "", DateTime? UpdatedAt = null, string? UpdatedBy = null) 
        : AuditableDto<long>(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);
}