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

public record RoleDto : AuditableGuidDto
{
    public Guid Id { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
}

public record UpdateUserDto(
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    string? PhoneNumber = null
);
