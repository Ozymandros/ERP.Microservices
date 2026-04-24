using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

/// <summary>
/// Defines operations for accessing and managing RefreshToken entities.
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    /// <summary>
    /// Retrieves a refresh token by its token value.
    /// </summary>
    Task<RefreshToken?> GetByTokenAsync(string token);

    /// <summary>
    /// Retrieves all refresh tokens for a specific user.
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Retrieves a valid (not revoked and not expired) refresh token for a specific user.
    /// </summary>
    Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, string token);

    /// <summary>
    /// Creates and persists a new refresh token.
    /// </summary>
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);

    /// <summary>
    /// Revokes a specific refresh token by marking it as revoked.
    /// </summary>
    Task RevokeAsync(Guid tokenId);

    /// <summary>
    /// Revokes all refresh tokens for a specific user.
    /// </summary>
    Task RevokeUserTokensAsync(Guid userId);
}
