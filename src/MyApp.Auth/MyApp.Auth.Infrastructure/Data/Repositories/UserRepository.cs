using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Implements data access operations for <see cref="ApplicationUser"/> entities using Entity Framework Core.
/// </summary>
public class UserRepository : Repository<ApplicationUser, Guid>, IUserRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Initializes a new instance of the UserRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public UserRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the email asynchronously.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns>The matching <see cref="ApplicationUser"/>, or <c>null</c> if not found.</returns>
    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Gets the external id asynchronously.
    /// </summary>
    /// <param name="externalProvider">The external Provider.</param>
    /// <param name="externalId">The external Id.</param>
    /// <returns>The matching <see cref="ApplicationUser"/>, or <c>null</c> if not found.</returns>
    public async Task<ApplicationUser?> GetByExternalIdAsync(string externalProvider, string externalId)
    {
        return await _context.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.ExternalProvider == externalProvider && u.ExternalId == externalId);
    }

    /// <summary>
    /// Gets the role asynchronously.
    /// </summary>
    /// <param name="roleName">The role Name.</param>
    /// <returns>A collection of <see cref="ApplicationUser"/> entities in the role.</returns>
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

    /// <summary>
    /// Email exists asynchronously.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns><c>true</c> if a user with the email exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _context.Users.AnyAsync(u => u.Email == email);
    }
}
