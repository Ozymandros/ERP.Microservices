namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    public abstract record BaseGuidDto(Guid Id) : BaseDto<Guid>(Id);
    public abstract record BaseIntDto(int Id) : BaseDto<int>(Id);
    public abstract record BaseLongDto(long Id) : BaseDto<long>(Id);

    // Auditable DTO aliases for common ID types
    // Propietats heretades (CreatedAt, CreatedBy, etc.) no es redefinixen
    public abstract record AuditableGuidDto(Guid Id) : AuditableDto<Guid>(Id);
    public abstract record AuditableIntDto(int Id) : AuditableDto<int>(Id);
    public abstract record AuditableLongDto(long Id) : AuditableDto<long>(Id);
}