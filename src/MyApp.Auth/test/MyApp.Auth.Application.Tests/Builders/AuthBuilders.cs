using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Application.Tests.Builders;

/// <summary>
/// Builder for constructing <see cref="ApplicationUser"/> test fixtures using a fluent API.
/// </summary>
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

    /// <summary>
    /// Sets the user's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithId(Guid id)
    {
        _user.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the user's email address and username.
    /// </summary>
    /// <param name="email">The email address to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithEmail(string email)
    {
        _user.Email = email;
        _user.UserName = email; // Assuming username follows email
        return this;
    }

    /// <summary>
    /// Sets the user's username independently of the email address.
    /// </summary>
    /// <param name="userName">The username to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithUserName(string userName)
    {
        _user.UserName = userName;
        return this;
    }

    /// <summary>
    /// Sets the user's first name.
    /// </summary>
    /// <param name="firstName">The first name to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithFirstName(string firstName)
    {
        _user.FirstName = firstName;
        return this;
    }

    /// <summary>
    /// Sets the user's last name.
    /// </summary>
    /// <param name="lastName">The last name to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithLastName(string lastName)
    {
        _user.LastName = lastName;
        return this;
    }

    /// <summary>
    /// Sets whether the user's email address has been confirmed.
    /// </summary>
    /// <param name="confirmed">Whether the email is confirmed (default: <c>true</c>).</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithEmailConfirmed(bool confirmed = true)
    {
        _user.EmailConfirmed = confirmed;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the user was created.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to assign.</param>
    /// <returns>The current <see cref="UserBuilder"/> instance for chaining.</returns>
    public UserBuilder WithCreatedAt(DateTime createdAt)
    {
        _user.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="ApplicationUser"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="ApplicationUser"/>.</returns>
    public ApplicationUser Build() => _user;

    /// <summary>
    /// Implicitly converts a <see cref="UserBuilder"/> to an <see cref="ApplicationUser"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="ApplicationUser"/>.</returns>
    public static implicit operator ApplicationUser(UserBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="LoginDto"/> test fixtures with default credentials.
/// </summary>
public class LoginDtoBuilder
{
    private LoginDto _dto = new("test@example.com", "TestPassword123!");

    /// <summary>
    /// Builds and returns the configured <see cref="LoginDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="LoginDto"/>.</returns>
    public LoginDto Build() => _dto;

    /// <summary>
    /// Implicitly converts a <see cref="LoginDtoBuilder"/> to a <see cref="LoginDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="LoginDto"/>.</returns>
    public static implicit operator LoginDto(LoginDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="RegisterDto"/> test fixtures with default registration data.
/// </summary>
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

    /// <summary>
    /// Builds and returns the configured <see cref="RegisterDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="RegisterDto"/>.</returns>
    public RegisterDto Build() => _dto;

    /// <summary>
    /// Implicitly converts a <see cref="RegisterDtoBuilder"/> to a <see cref="RegisterDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="RegisterDto"/>.</returns>
    public static implicit operator RegisterDto(RegisterDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="TokenResponseDto"/> test fixtures with default token data.
/// </summary>
public class TokenResponseDtoBuilder
{
    private TokenResponseDto _dto = new(
        AccessToken: "sample_access_token",
        RefreshToken: "sample_refresh_token",
        ExpiresIn: 3600,
        TokenType: "Bearer"
    );

    /// <summary>
    /// Builds and returns the configured <see cref="TokenResponseDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="TokenResponseDto"/>.</returns>
    public TokenResponseDto Build() => _dto;

    /// <summary>
    /// Implicitly converts a <see cref="TokenResponseDtoBuilder"/> to a <see cref="TokenResponseDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="TokenResponseDto"/>.</returns>
    public static implicit operator TokenResponseDto(TokenResponseDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="ExternalLoginDto"/> test fixtures with default external-provider data.
/// </summary>
public class ExternalLoginDtoBuilder
{
    private ExternalLoginDto _dto = new(
        Provider: "Google",
        ExternalId: "google_user_123",
        Email: "external@example.com",
        FirstName: "External",
        LastName: "User"
    );

    /// <summary>
    /// Builds and returns the configured <see cref="ExternalLoginDto"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="ExternalLoginDto"/>.</returns>
    public ExternalLoginDto Build() => _dto;

    /// <summary>
    /// Implicitly converts an <see cref="ExternalLoginDtoBuilder"/> to an <see cref="ExternalLoginDto"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="ExternalLoginDto"/>.</returns>
    public static implicit operator ExternalLoginDto(ExternalLoginDtoBuilder builder) => builder.Build();
}

/// <summary>
/// Builder for constructing <see cref="RefreshToken"/> test fixtures using a fluent API.
/// </summary>
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

    /// <summary>
    /// Sets the token's unique identifier.
    /// </summary>
    /// <param name="id">The identifier to assign.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithId(Guid id)
    {
        _token.Id = id;
        return this;
    }

    /// <summary>
    /// Sets the token string value.
    /// </summary>
    /// <param name="token">The token string to assign.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithToken(string token)
    {
        _token.Token = token;
        return this;
    }

    /// <summary>
    /// Sets the identifier of the user who owns the token.
    /// </summary>
    /// <param name="userId">The user identifier to assign.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithUserId(Guid userId)
    {
        _token.UserId = userId;
        return this;
    }

    /// <summary>
    /// Sets the token's expiry timestamp.
    /// </summary>
    /// <param name="expiresAt">The expiry timestamp to assign.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithExpiresAt(DateTime expiresAt)
    {
        _token.ExpiresAt = expiresAt;
        return this;
    }

    /// <summary>
    /// Sets whether the token has been revoked.
    /// </summary>
    /// <param name="isRevoked">Whether the token is revoked.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithIsRevoked(bool isRevoked)
    {
        _token.IsRevoked = isRevoked;
        return this;
    }

    /// <summary>
    /// Sets the timestamp at which the token was created.
    /// </summary>
    /// <param name="createdAt">The creation timestamp to assign.</param>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder WithCreatedAt(DateTime createdAt)
    {
        _token.CreatedAt = createdAt;
        return this;
    }

    /// <summary>
    /// Configures the token to be already expired (expiry set to yesterday).
    /// </summary>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder Expired()
    {
        _token.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        return this;
    }

    /// <summary>
    /// Configures the token as revoked.
    /// </summary>
    /// <returns>The current <see cref="RefreshTokenBuilder"/> instance for chaining.</returns>
    public RefreshTokenBuilder Revoked()
    {
        _token.IsRevoked = true;
        return this;
    }

    /// <summary>
    /// Builds and returns the configured <see cref="RefreshToken"/> instance.
    /// </summary>
    /// <returns>The constructed <see cref="RefreshToken"/>.</returns>
    public RefreshToken Build() => _token;

    /// <summary>
    /// Implicitly converts a <see cref="RefreshTokenBuilder"/> to a <see cref="RefreshToken"/>.
    /// </summary>
    /// <param name="builder">The builder to convert.</param>
    /// <returns>The constructed <see cref="RefreshToken"/>.</returns>
    public static implicit operator RefreshToken(RefreshTokenBuilder builder) => builder.Build();
}