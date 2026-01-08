
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

public record CreateUserDto(Guid Id) : AuditableGuidDto(Id)
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

public record UserDto(Guid Id) : AuditableGuidDto(Id)
{
    public string? Email { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool EmailConfirmed { get; init; } = false;
    public bool IsExternalLogin { get; init; } = false;
    public string? ExternalProvider { get; init; }
    public List<RoleDto?> Roles { get; init; } = new();
    public List<PermissionDto?> Permissions { get; init; } = new();
    public bool IsAdmin { get; init; } = false;
}
