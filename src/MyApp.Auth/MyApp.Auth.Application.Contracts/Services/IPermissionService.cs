using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Contracts
{
    /// <summary>
    /// Defines operations for managing permissions and checking user access rights.
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// Check if a user has a specific permission
        /// </summary>
        Task<bool> HasPermissionAsync(Guid userId, string module, string action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a user has a specific permission by username
        /// </summary>
        Task<bool> HasPermissionAsync(string? username, string module, string action, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all permissions
        /// </summary>
        Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get all permissions with pagination
        /// </summary>
        Task<PaginatedResult<PermissionDto>> GetAllPermissionsPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Query permissions with filtering, sorting, and pagination
        /// </summary>
        Task<PaginatedResult<PermissionDto>> QueryPermissionsAsync(ISpecification<Permission> spec, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get permission by ID
        /// </summary>
        Task<PermissionDto?> GetPermissionByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get permission by module and action
        /// </summary>
        Task<PermissionDto?> GetPermissionByModuleActionAsync(string module, string action, CancellationToken cancellationToken = default);

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