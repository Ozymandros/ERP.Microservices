using MyApp.Shared.Domain.DTOs;

    public record PermissionDto(
        Guid Id,
        DateTime CreatedAt = default,
        string CreatedBy = "",
        DateTime? UpdatedAt = null,
        string? UpdatedBy = null,
        string Module = "",
        string Action = "",
        string? Description = null
    ) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);