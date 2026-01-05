using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

public class UserBuilder
{
    private ApplicationUser _user = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        UserName = "test@example.com",
        FirstName = "Test",
        LastName = "ApplicationUser",
        EmailConfirmed = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    public UserBuilder WithId(Guid id)
    {
        _user.Id = id;
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        _user.UserName = email; // Assuming username follows email
        return this;
    }

    public UserBuilder WithUserName(string userName)
    {
        _user.UserName = userName;
        return this;
    }

    public UserBuilder WithFirstName(string firstName)
    {
        _user.FirstName = firstName;
        return this;
    }

    public UserBuilder WithLastName(string lastName)
    {
        _user.LastName = lastName;
        return this;
    }

    public UserBuilder WithEmailConfirmed(bool confirmed = true)
    {
        _user.EmailConfirmed = confirmed;
        return this;
    }

    public UserBuilder WithCreatedAt(DateTime createdAt)
    {
        _user.CreatedAt = createdAt;
        return this;
    }

    public ApplicationUser Build() => _user;

    public static implicit operator ApplicationUser(UserBuilder builder) => builder.Build();
}

public class LoginDtoBuilder
{
    private LoginDto _dto = new("test@example.com", "TestPassword123!");

    public LoginDto Build() => _dto;

    public static implicit operator LoginDto(LoginDtoBuilder builder) => builder.Build();
}

public class RegisterDtoBuilder
{
    private RegisterDto _dto = new(
        Email: "newuser@example.com",
        Username: "newuser@example.com",
        Password: "NewPassword123!",
        PasswordConfirm: "NewPassword123!",
        FirstName: "New",
        LastName: "ApplicationUser"
    );

    public RegisterDto Build() => _dto;

    public static implicit operator RegisterDto(RegisterDtoBuilder builder) => builder.Build();
}

public class TokenResponseDtoBuilder
{
    private TokenResponseDto _dto = new(
        AccessToken: "sample_access_token",
        RefreshToken: "sample_refresh_token",
        ExpiresIn: 3600,
        TokenType: "Bearer"
    );

    public TokenResponseDto Build() => _dto;

    public static implicit operator TokenResponseDto(TokenResponseDtoBuilder builder) => builder.Build();
}

public class ExternalLoginDtoBuilder
{
    private ExternalLoginDto _dto = new(
        Provider: "Google",
        ExternalId: "google_user_123",
        Email: "external@example.com",
        FirstName: "External",
        LastName: "User"
    );

    public ExternalLoginDto Build() => _dto;

    public static implicit operator ExternalLoginDto(ExternalLoginDtoBuilder builder) => builder.Build();
}

public class RefreshTokenBuilder
{
    private RefreshToken _token = new()
    {
        Id = Guid.NewGuid(),
        Token = "sample_refresh_token",
        UserId = Guid.NewGuid(),
        ExpiresAt = DateTime.UtcNow.AddDays(7),
        IsRevoked = false,
        CreatedAt = DateTime.UtcNow
    };

    public RefreshTokenBuilder WithId(Guid id)
    {
        _token.Id = id;
        return this;
    }

    public RefreshTokenBuilder WithToken(string token)
    {
        _token.Token = token;
        return this;
    }

    public RefreshTokenBuilder WithUserId(Guid userId)
    {
        _token.UserId = userId;
        return this;
    }

    public RefreshTokenBuilder WithExpiresAt(DateTime expiresAt)
    {
        _token.ExpiresAt = expiresAt;
        return this;
    }

    public RefreshTokenBuilder WithIsRevoked(bool isRevoked)
    {
        _token.IsRevoked = isRevoked;
        return this;
    }

    public RefreshTokenBuilder WithCreatedAt(DateTime createdAt)
    {
        _token.CreatedAt = createdAt;
        return this;
    }

    public RefreshTokenBuilder Expired()
    {
        _token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    public RefreshTokenBuilder Revoked()
    {
        _token.IsRevoked = true;
        return this;
    }

    public RefreshToken Build() => _token;

    public static implicit operator RefreshToken(RefreshTokenBuilder builder) => builder.Build();
}