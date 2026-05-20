using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Refresh Token Repository functionality.
/// </summary>
public class RefreshTokenRepository : Repository<RefreshToken, Guid>, IRefreshTokenRepository
{
    private readonly AuthDbContext _context;

    /// <summary>base.</summary>
    public RefreshTokenRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Token Async.</summary>
    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    /// <summary>Get By User Id Async.</summary>
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .Where(rt => rt.UserId == userId)
            .ToListAsync();
    }

    /// <summary>Get Valid Refresh Token Async.</summary>
    public async Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.UserId == userId
                && rt.Token == token
                && !rt.IsRevoked
                && rt.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>Create Async.</summary>
    public async Task<RefreshToken> CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await base.SaveChangesAsync();
        return refreshToken;
    }

    /// <summary>Revoke Async.</summary>
    public async Task RevokeAsync(Guid tokenId)
    {
        var token = await _context.RefreshTokens.FindAsync(tokenId);
        if (token != null)
        {
            token.IsRevoked = true;
            await base.SaveChangesAsync();
        }
    }

    /// <summary>Revoke User Tokens Async.</summary>
    public async Task RevokeUserTokensAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }

        await base.SaveChangesAsync();
    }
}
