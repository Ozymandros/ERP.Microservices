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
    string Name,
    string? Description = null
);

public record RoleDto(
    Guid Id,
    DateTime CreatedAt = default,
    string CreatedBy = "",
    DateTime? UpdatedAt = null,
    string? UpdatedBy = null,
    string? Name = null,
    string? Description = null
) : AuditableGuidDto(Id, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy);

public record UpdateUserDto(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null
);
