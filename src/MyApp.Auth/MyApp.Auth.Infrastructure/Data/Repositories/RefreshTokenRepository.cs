using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Implements data access operations for <see cref="RefreshToken"/> entities using Entity Framework Core.
/// </summary>
public class RefreshTokenRepository : Repository<RefreshToken, Guid>, IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Initializes a new instance of the RefreshTokenRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public RefreshTokenRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the token asynchronously.
    /// </summary>
    /// <param name="token">The token.</param>
    /// <returns>The matching <see cref="RefreshToken"/>, or <c>null</c> if not found.</returns>
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    /// <summary>
    /// Gets the user id asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <returns>A collection of <see cref="RefreshToken"/> entities for the user.</returns>
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the valid refresh token asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="token">The token.</param>
    /// <returns>The valid <see cref="RefreshToken"/> if found; otherwise, <c>null</c>.</returns>
    public async Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId
                && rt.Token == token
                && !rt.IsRevoked
                && rt.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a new item asynchronously.
    /// </summary>
    /// <param name="refreshToken">The refresh Token.</param>
    /// <returns>The added <see cref="RefreshToken"/>.</returns>
    public Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        return Task.FromResult(refreshToken);
    }

    /// <summary>
    /// Revoke asynchronously.
    /// </summary>
    /// <param name="tokenId">The token Id.</param>
    public async Task RevokeAsync(Guid tokenId)
    {
        var token = await _context.RefreshTokens.FindAsync(tokenId);
        if (token != null)
        {
            token.IsRevoked = true;
        }
    }

    /// <summary>
    /// Revoke user tokens asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    public async Task RevokeUserTokensAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }
    }
}
