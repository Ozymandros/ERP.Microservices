using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs
{
    public class PermissionDto : AuditableGuidDto
    {
        public string Module { get; set; } = default!;
        public string Action { get; set; } = default!;
        public string? Description { get; set; }
    }
}