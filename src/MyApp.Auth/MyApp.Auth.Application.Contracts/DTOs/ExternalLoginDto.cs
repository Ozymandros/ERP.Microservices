using MyApp.Shared.Domain.DTOs;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record ExternalLoginDto
{
    public ExternalLoginDto() { }
    
    public ExternalLoginDto(
        string provider,
        string externalId,
        string email,
        string? firstName = null,
        string? lastName = null)
    {
        Provider = provider;
        ExternalId = externalId;
        Email = email;
        FirstName = firstName;
        LastName = lastName;
    }
    
    public string Provider { get; set; } = string.Empty;
    public string ExternalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public record RefreshTokenDto
{
    public RefreshTokenDto() { }
    
    public RefreshTokenDto(string accessToken, string refreshToken)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }
    
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public record CreateRoleDto
{
    public CreateRoleDto() { }
    
    public CreateRoleDto(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }
    
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
    public UpdateUserDto() { }
    
    public UpdateUserDto(
        string? email = null,
        string? firstName = null,
        string? lastName = null,
        string? phoneNumber = null)
    {
        Email = email;
        FirstName = firstName;
        LastName = lastName;
        PhoneNumber = phoneNumber;
    }
    
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
}
