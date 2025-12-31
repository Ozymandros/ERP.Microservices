
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

public record CreateUserDto(
    Guid Id,
    DateTime CreatedAt = default,
    string CreatedBy = "",
    DateTime? UpdatedAt = null,
    string? UpdatedBy = null,
    [property: Required]
    [property: EmailAddress]
    string? Email = null,
    [property: Required]
    [property: MinLength(8)]
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    [property: Required]
    [property: MinLength(8)]
    [property: PasswordPropertyText]
    string Password = ""
) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

public record UserDto(
    Guid Id,
    DateTime CreatedAt = default,
    string CreatedBy = "",
    DateTime? UpdatedAt = null,
    string? UpdatedBy = null,
    string? Email = null,
    string? Username = null,
    string? FirstName = null,
    string? LastName = null,
    bool EmailConfirmed = false,
    bool IsExternalLogin = false,
    string? ExternalProvider = null,
    List<RoleDto?>? Roles = null,
    List<PermissionDto?>? Permissions = null,
    bool IsAdmin = false
) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
{
    public List<RoleDto?> Roles { get; set; } = Roles ?? new();
    public List<PermissionDto?> Permissions { get; set; } = Permissions ?? new();
    public bool IsAdmin { get; set; } = IsAdmin;
}
