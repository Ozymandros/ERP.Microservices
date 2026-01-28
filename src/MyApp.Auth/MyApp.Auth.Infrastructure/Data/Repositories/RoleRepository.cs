using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

public class RoleRepository : Repository<ApplicationRole, Guid>, IRoleRepository
{
    private readonly AuthDbContext _context;

    public RoleRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ApplicationRole?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .Include(r => r.RoleClaims)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<bool> NameExistsAsync(string name)
    {
        return await _context.Roles.AnyAsync(r => r.Name == name);
    }

    public async Task<IEnumerable<ApplicationRole>> GetRolesByUserIdAsync(Guid userId)
    {
        return await _context.Roles
            .Include(r => r.UserRoles)
            .Include(r => r.RoleClaims)
            .Where(r => r.UserRoles.Any(ur => ur.UserId == userId))
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(Guid roleId)
    {
        var permissions = await _context.Roles
            .Where(r => r.Id == roleId)
            .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission)) // Traverse through the join table to the Permission
            .ToListAsync();

        return permissions;
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _context.RolePermissions
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
    }

    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
        {
            return false;
        }

        _context.RolePermissions.Remove(rolePermission);
        await _context.SaveChangesAsync();

        // Update role's UpdatedAt timestamp
        var role = await _context.Roles.FindAsync(roleId);
        if (role != null)
        {
            role.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }
}
