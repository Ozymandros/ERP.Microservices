using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Role Repository functionality.
/// </summary>
public class RoleRepository : Repository<ApplicationRole, Guid>, IRoleRepository
{
    private readonly AuthDbContext _context;

    /// <summary>base.</summary>
    public RoleRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>Get By Name Async.</summary>
    public async Task<ApplicationRole?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .Include(r => r.RoleClaims)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    /// <summary>Name Exists Async.</summary>
    public async Task<bool> NameExistsAsync(string name)
    {
        return await _context.Roles.AnyAsync(r => r.Name == name);
    }

    /// <summary>Get Roles By User Id Async.</summary>
    public async Task<IEnumerable<ApplicationRole>> GetRolesByUserIdAsync(Guid userId)
    {
        // Query directly from UserRoles join table to get only roles assigned to this user
        var roleIds = await _context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .Distinct()
            .ToListAsync();

        if (!roleIds.Any())
        {
            return Enumerable.Empty<ApplicationRole>();
        }

        return await this.Queryable
            .Where(r => roleIds.Contains(r.Id))
            .Include(r => r.RoleClaims)
            .ToListAsync();
    }

    /// <summary>Get Permissions For Role Async.</summary>
    public async Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(Guid roleId)
    {
        var permissions = await this.Queryable
            .Where(r => r.Id == roleId)
            .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission)) // Traverse through the join table to the Permission
            .ToListAsync();

        return permissions;
    }

    /// <summary>Has Permission Async.</summary>
    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _context.RolePermissions.AsNoTracking()
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
    }

    /// <summary>Remove Permission From Role Async.</summary>
    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
        {
            return false;
        }

        _context.RolePermissions.Remove(rolePermission);
        await this.SaveChangesAsync();

        // Update role's UpdatedAt timestamp
        var role = await _context.Roles.FindAsync(roleId);
        if (role != null)
        {
            role.UpdatedAt = DateTime.UtcNow;
            await this.SaveChangesAsync();
        }

        return true;
    }
}
