using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs
{
    public record PermissionDto : AuditableGuidDto
    {
        public string Module { get; init; } = string.Empty;
        public string Action { get; init; } = string.Empty;
        public string? Description { get; init; }
    }
}