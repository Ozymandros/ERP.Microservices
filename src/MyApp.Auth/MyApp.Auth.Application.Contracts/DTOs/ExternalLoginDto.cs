using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record ExternalLoginDto
{
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public record RefreshTokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public record CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public record RoleDto : AuditableGuidDto
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public record UpdateUserDto
{
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
