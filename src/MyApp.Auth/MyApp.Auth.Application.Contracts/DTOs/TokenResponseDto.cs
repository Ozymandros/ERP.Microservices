
using MyApp.Shared.Domain.DTOs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer",
    UserDto? User = null
);

public record CreateUserDto : AuditableGuidDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    [Required]
    [MinLength(8)]
    public string? Username { get; init; }

    public string? FirstName { get; init; }
    public string? LastName { get; init; }

    [Required]
    [MinLength(8)]
    [PasswordPropertyText]
    public string Password { get; init; } = string.Empty;
}

public record UserDto : AuditableGuidDto
{
    public Guid Id { get; init; }
    public string? Email { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool EmailConfirmed { get; init; }
    public bool IsExternalLogin { get; init; }
    public string? ExternalProvider { get; init; }
    public List<RoleDto?> Roles { get; init; } = new();
    public List<PermissionDto?> Permissions { get; init; } = new();
    public bool IsAdmin { get; init; }
}
