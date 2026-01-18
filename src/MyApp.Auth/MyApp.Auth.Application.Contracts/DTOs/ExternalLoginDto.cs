using System.ComponentModel.DataAnnotations;
using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record ExternalLoginDto(
    string Provider,
    string ExternalId,
    string Email,
    string? FirstName = null,
    string? LastName = null
);

public record RefreshTokenDto(
    string AccessToken,
    string RefreshToken
);

public record CreateRoleDto(
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Role name must be between 1 and 256 characters")]
    string Name,
    [StringLength(500)]
    string? Description = null
);

public record RoleDto(Guid Id) : AuditableGuidDto(Id)
{
    public string? Name { get; init; }
    public string? Description { get; init; }
}

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
