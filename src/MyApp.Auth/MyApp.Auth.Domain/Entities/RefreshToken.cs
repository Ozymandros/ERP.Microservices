namespace MyApp.Auth.Domain.Entities;

/// <summary>
/// Represents a refresh token used for obtaining new access tokens without re-authentication.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the user who owns this token.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the token string value.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time when this token expires.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the date and time when this token was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a value indicating whether this token has been revoked.
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Gets or sets the user who owns this refresh token.
    /// </summary>
    public virtual ApplicationUser? User { get; set; }
}
