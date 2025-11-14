namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    public abstract record BaseGuidDto : BaseDto<Guid> { }
    public abstract record BaseIntDto : BaseDto<int> { }
    public abstract record BaseLongDto : BaseDto<long> { }

    // Auditable DTO aliases for common ID types
    public abstract record AuditableGuidDto : AuditableDto<Guid> { }
    public abstract record AuditableIntDto : AuditableDto<int> { }
    public abstract record AuditableLongDto : AuditableDto<long> { }
}