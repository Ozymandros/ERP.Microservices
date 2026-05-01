using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Provides User Repository functionality.
/// </summary>
public class UserRepository : Repository<ApplicationUser, Guid>, IUserRepository
{
    private readonly AuthDbContext _context;

    /// <summary>base.</summary>
    public UserRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Email Async.</summary>
    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>Get By External Id Async.</summary>
    public async Task<ApplicationUser?> GetByExternalIdAsync(string externalProvider, string externalId)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.ExternalProvider == externalProvider && u.ExternalId == externalId);
    }

    /// <summary>Get By Role Async.</summary>
    public async Task<IEnumerable<ApplicationUser>> GetByRoleAsync(string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return Enumerable.Empty<ApplicationUser>();

        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == role.Id))
            .ToListAsync();
    }

    /// <summary>Email Exists Async.</summary>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
