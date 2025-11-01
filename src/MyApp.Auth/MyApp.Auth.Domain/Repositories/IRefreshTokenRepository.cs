using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Auth.Domain.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(Guid userId);
    Task<RefreshToken?> GetValidRefreshTokenAsync(Guid userId, string token);
    Task<RefreshToken> CreateAsync(RefreshToken refreshToken);
    Task RevokeAsync(Guid tokenId);
    Task RevokeUserTokensAsync(Guid userId);
}
