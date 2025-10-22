using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Contracts
{
    public interface IPermissionService
    {
        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        Task<bool> HasPermissionAsync(Guid userId, string module, string action);

        /// <summary>
        /// Check if a user has a specific permission by username
        /// </summary>
        Task<bool> HasPermissionAsync(string? username, string module, string action);

        /// <summary>
        /// Get all permissions
        /// </summary>
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync();

        /// <summary>
        /// Get permission by ID
        /// </summary>
        Task<PermissionDto?> GetPermissionByIdAsync(Guid id);

        /// <summary>
        /// Get permission by module and action
        /// </summary>
        Task<PermissionDto?> GetPermissionByModuleActionAsync(string module, string action);

        /// <summary>
        /// Create a new permission
        /// </summary>
        Task<PermissionDto?> CreatePermissionAsync(CreatePermissionDto createPermissionDto);

        /// <summary>
        /// Update an existing permission
        /// </summary>
        Task<bool> UpdatePermissionAsync(Guid id, UpdatePermissionDto updatePermissionDto);

        /// <summary>
        /// Delete a permission
        /// </summary>
        Task<bool> DeletePermissionAsync(Guid id);
    }
}