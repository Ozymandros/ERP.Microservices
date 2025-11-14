
using MyApp.Shared.Domain.DTOs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace MyApp.Auth.Application.Contracts.DTOs;

public record TokenResponseDto
{
    public TokenResponseDto() { }
    
    public TokenResponseDto(
        string accessToken,
        string refreshToken,
        int expiresIn,
        string tokenType = "Bearer",
        UserDto? user = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        ExpiresIn = expiresIn;
        TokenType = tokenType;
        User = user;
    }
    
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
    public UserDto? User { get; set; }
}

public record CreateUserDto : AuditableGuidDto
{
    [Required]
    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    [MinLength(8)]
    public string? Username { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    [Required]
    [MinLength(8)]
    [PasswordPropertyText]
    public string Password { get; set; } = string.Empty;
}

public record UserDto : AuditableGuidDto
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? Username { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool IsExternalLogin { get; set; }
    public string? ExternalProvider { get; set; }
    public List<RoleDto?> Roles { get; set; } = new();
    public List<PermissionDto?> Permissions { get; set; } = new();
    public bool IsAdmin { get; set; }
}
