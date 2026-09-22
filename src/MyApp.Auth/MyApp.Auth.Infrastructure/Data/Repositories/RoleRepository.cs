using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories;

/// <summary>
/// Implements data access operations for <see cref="ApplicationRole"/> entities using Entity Framework Core.
/// </summary>
public class RoleRepository : Repository<ApplicationRole, Guid>, IRoleRepository
{
    private readonly AuthDbContext _context;

    /// <summary>
    /// Initializes a new instance of the RoleRepository class.
    /// </summary>
    /// <param name="context">The context.</param>
    public RoleRepository(AuthDbContext context) : base(context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets the name asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The matching <see cref="ApplicationRole"/>, or <c>null</c> if not found.</returns>
    public async Task<ApplicationRole?> GetByNameAsync(string name)
    {
        return await _context.Roles
            .Include(r => r.RoleClaims)
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    /// <summary>
    /// Name exists asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns><c>true</c> if a role with the name exists; otherwise, <c>false</c>.</returns>
    public async Task<bool> NameExistsAsync(string name)
    {
        return await _context.Roles.AnyAsync(r => r.Name == name);
    }

    /// <summary>
    /// Gets the roles by user id asynchronously.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <returns>A collection of <see cref="ApplicationRole"/> entities for the user.</returns>
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

    /// <summary>
    /// Gets the permissions for role asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <returns>A collection of <see cref="Permission"/> entities assigned to the role.</returns>
    public async Task<IEnumerable<Permission>> GetPermissionsForRoleAsync(Guid roleId)
    {
        var permissions = await this.Queryable
            .Where(r => r.Id == roleId)
            .SelectMany(r => r.RolePermissions.Select(rp => rp.Permission)) // Traverse through the join table to the Permission
            .ToListAsync();

        return permissions;
    }

    /// <summary>
    /// Determines whether permission asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <param name="permissionId">The permission Id.</param>
    /// <returns><c>true</c> if the role has the permission; otherwise, <c>false</c>.</returns>
    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _context.RolePermissions.AsNoTracking()
            .AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
    }

    /// <summary>
    /// Removes the permission from role asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <param name="permissionId">The permission Id.</param>
    /// <returns><c>true</c> if the permission was removed; <c>false</c> if the association was not found.</returns>
    public async Task<bool> RemovePermissionFromRoleAsync(Guid roleId, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

        if (rolePermission == null)
        {
            return false;
        }

        _context.RolePermissions.Remove(rolePermission);

        var role = await _context.Roles.FindAsync(roleId);
        if (role != null)
        {
            role.UpdatedAt = DateTime.UtcNow;
        }

        return true;
    }
}
