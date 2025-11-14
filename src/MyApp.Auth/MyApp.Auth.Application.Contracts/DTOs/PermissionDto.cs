using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs
{
    public record PermissionDto : AuditableGuidDto
    {
        public string Module { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}