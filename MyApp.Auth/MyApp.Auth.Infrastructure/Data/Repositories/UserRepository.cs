using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

public class UserRepository : Repository<User, Guid>, IUserRepository
{
    private readonly AuthDbContext _context;

    public UserRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByExternalIdAsync(string externalProvider, string externalId)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.ExternalProvider == externalProvider && u.ExternalId == externalId);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string roleName)
    {
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
            return Enumerable.Empty<User>();

        return await _context.Users
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(ur => ur.RoleId == role.Id))
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
