using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories
{
    public class PermissionRepository : Repository<Permission, Guid>, IPermissionRepository
    {
        private readonly AuthDbContext _context;

        public PermissionRepository(AuthDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets permissions assigned to a specific role by module and action.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetByRoleName(string roleName, string module, string action)
        {
            if (string.IsNullOrWhiteSpace(roleName) || string.IsNullOrWhiteSpace(module) || string.IsNullOrWhiteSpace(action))
            {
                return Enumerable.Empty<Permission>();
            }

            // Query RolePermissions join table
            var rolePermissions = await _context.Set<RolePermission>()
                .AsNoTracking()
                .Include(rp => rp.Permission)
                .Where(rp => rp.Role.Name != null && rp.Role.Name.Contains(roleName) &&
                             rp.Permission.Module == module &&
                             rp.Permission.Action == action)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return rolePermissions;
        }

        /// <summary>
        /// Gets permissions assigned directly to a specific user by module and action.
        /// </summary>
        public async Task<IEnumerable<Permission>> GetByUserName(string userName, string module, string action)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(module) || string.IsNullOrWhiteSpace(action))
            {
                return Enumerable.Empty<Permission>();
            }

            var userPermissions = await _context.Set<UserPermission>()
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up => up.User.UserName == userName &&
                             up.Permission.Module == module &&
                             up.Permission.Action == action)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return userPermissions;
        }

        /// <summary>
        /// Gets permissions assigned directly to a specific user and role by UserId.
        /// </summary>
        
        public async Task<IEnumerable<Permission>> GetAllPermissionsByUserId(Guid userId)
        {
            var userPermissions = await _context.Set<UserPermission>()
                .AsNoTracking()
                .Include(up => up.Permission)
                .Where(up => up.User.Id == userId)
                .Select(up => up.Permission)
                .ToListAsync();

            var rolePermissions = await (from ur in _context.Set<ApplicationUserRole>()
                                         join rp in _context.Set<RolePermission>() on ur.RoleId equals rp.RoleId
                                         where ur.UserId == userId
                                         select rp.Permission)
                                 .Distinct()
                                 .ToListAsync();

            userPermissions.AddRange(rolePermissions);
            userPermissions.ToHashSet(new PermissionComparer());
            return userPermissions;
        }
    }
}
