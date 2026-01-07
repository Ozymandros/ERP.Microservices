using MyApp.Shared.Domain.DTOs;

public record PermissionDto(Guid Id) : AuditableGuidDto(Id)
{
    public string Module { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public string? Description { get; init; }
}