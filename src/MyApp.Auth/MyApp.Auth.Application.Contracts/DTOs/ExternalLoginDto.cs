using System.ComponentModel.DataAnnotations;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

/// <summary>
/// External login dto.
/// </summary>
/// <param name="Provider">The provider.</param>
/// <param name="ExternalId">The external Id.</param>
/// <param name="Email">The email.</param>
/// <param name="FirstName">The first Name.</param>
/// <param name="LastName">The last Name.</param>
public record ExternalLoginDto(
    string Provider,
    string ExternalId,
    string Email,
    string? FirstName = null,
    string? LastName = null
);

/// <summary>
/// Refresh token dto.
/// </summary>
/// <param name="AccessToken">The access Token.</param>
/// <param name="RefreshToken">The refresh Token.</param>
public record RefreshTokenDto(
    string AccessToken,
    string RefreshToken
);

/// <summary>
/// Creates a role dto.
/// </summary>
/// <param name="Name">The name.</param>
/// <param name="Description">The description.</param>
public record CreateRoleDto(
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Role name must be between 1 and 256 characters")]
    string Name,
    [StringLength(500)]
    string? Description = null
);

/// <summary>
/// Role dto.
/// </summary>
/// <param name="Id">The id.</param>
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
/// Updates the user dto.
/// </summary>
/// <param name="Email">The email.</param>
/// <param name="FirstName">The first Name.</param>
/// <param name="LastName">The last Name.</param>
/// <param name="PhoneNumber">The phone Number.</param>
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
