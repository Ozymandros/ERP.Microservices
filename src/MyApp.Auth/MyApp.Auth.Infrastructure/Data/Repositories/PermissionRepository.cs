using Microsoft.EntityFrameworkCore;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Infrastructure.Repositories;

namespace MyApp.Auth.Infrastructure.Data.Repositories
{
    /// <summary>
    /// Implements data access operations for <see cref="Permission"/> entities using Entity Framework Core.
    /// </summary>
    public class PermissionRepository : Repository<Permission, Guid>, IPermissionRepository
    {
        private readonly AuthDbContext _context;

        /// <summary>
        /// Initializes a new instance of the PermissionRepository class.
        /// </summary>
        /// <param name="context">The context.</param>
        public PermissionRepository(AuthDbContext context) : base(context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the role name.
        /// </summary>
        /// <param name="roleName">The role Name.</param>
        /// <param name="module">The module.</param>
        /// <param name="action">The action.</param>
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
                .Where(rp => rp.Role.Name != null && rp.Role.Name.Contains(roleName, StringComparison.OrdinalIgnoreCase) &&
                             rp.Permission.Module == module &&
                             rp.Permission.Action == action)
                .Select(rp => rp.Permission)
                .ToListAsync();

            return rolePermissions;
        }

        /// <summary>
        /// Gets the user name.
        /// </summary>
        /// <param name="userName">The user Name.</param>
        /// <param name="module">The module.</param>
        /// <param name="action">The action.</param>
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
        /// Gets all permissions by user id.
        /// </summary>
        /// <param name="userId">The user Id.</param>
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
