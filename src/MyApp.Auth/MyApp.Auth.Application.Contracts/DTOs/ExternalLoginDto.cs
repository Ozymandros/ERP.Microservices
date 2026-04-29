using System.ComponentModel.DataAnnotations;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// Data transfer object for external authentication login information.
/// </summary>
public record ExternalLoginDto(
    string Provider,
    string ExternalId,
    string Email,
    string? FirstName = null,
    string? LastName = null
);

/// <summary>
/// Data transfer object for refresh token requests.
/// </summary>
public record RefreshTokenDto(
    string AccessToken,
    string RefreshToken
);

/// <summary>
/// Data transfer object for creating a new role.
/// </summary>
public record CreateRoleDto(
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Role name must be between 1 and 256 characters")]
    string Name,
    [StringLength(500)]
    string? Description = null
);

/// <summary>
/// Data transfer object for representing a role with audit information.
/// </summary>
public record RoleDto(Guid Id) : AuditableGuidDto(Id)
{
    /// <summary>
    /// Gets or sets the role name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Gets or sets the role description.
    /// </summary>
    public string? Description { get; init; }
}

/// <summary>
/// Data transfer object for updating user information.
/// </summary>
public record UpdateUserDto(
    [EmailAddress(ErrorMessage = "Invalid email address")]
    string? Email = null,

    [StringLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    string? FirstName = null,

    [StringLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    string? LastName = null,

    [Phone(ErrorMessage = "Invalid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    string? PhoneNumber = null
);
