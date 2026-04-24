
using MyApp.Shared.Domain.DTOs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// Data transfer object for authentication token response containing access and refresh tokens.
/// </summary>
public record TokenResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer",
    UserDto? User = null
);

/// <summary>
/// Data transfer object for creating a new user with audit information.
/// </summary>
public record CreateUserDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    [Required]
    [EmailAddress]
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or sets the user's password.
    /// </summary>
    [Required]
    [MinLength(8)]
    [PasswordPropertyText]
    public string Password { get; init; } = string.Empty;
}

/// <summary>
/// Data transfer object for representing a user with roles, permissions, and audit information.
/// </summary>
public record UserDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>
    /// Gets or sets the user's email address.
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// Gets or sets the user's username.
    /// </summary>
    public string? Username { get; init; }

    /// <summary>
    /// Gets or sets the user's first name.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets or sets the user's last name.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether the user's email is confirmed.
    /// </summary>
    public bool EmailConfirmed { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user logged in through an external provider.
    /// </summary>
    public bool IsExternalLogin { get; init; } = false;

    /// <summary>
    /// Gets or sets the external authentication provider name.
    /// </summary>
    public string? ExternalProvider { get; init; }

    /// <summary>
    /// Gets or sets the collection of roles assigned to this user.
    /// </summary>
    public List<RoleDto?> Roles { get; init; } = new();

    /// <summary>
    /// Gets or sets the collection of permissions assigned to this user.
    /// </summary>
    public List<PermissionDto?> Permissions { get; init; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the user has administrative privileges.
    /// </summary>
    public bool IsAdmin { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the user account is active.
    /// </summary>
    public bool IsActive { get; init; } = false;
}
