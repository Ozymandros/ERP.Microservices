namespace MyApp.Shared.Domain.DTOs
{
    // Base DTO aliases for common ID types
    public abstract class BaseGuidDto : BaseDto<Guid> { }
    public abstract class BaseIntDto : BaseDto<int> { }
    public abstract class BaseLongDto : BaseDto<long> { }

    // Auditable DTO aliases for common ID types
    public abstract class AuditableGuidDto : AuditableDto<Guid> { }
    public abstract class AuditableIntDto : AuditableDto<int> { }
    public abstract class AuditableLongDto : AuditableDto<long> { }
}