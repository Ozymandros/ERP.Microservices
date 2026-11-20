using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Contracts.Services;

/// <summary>
/// Defines operations for managing user accounts and their associated roles and permissions.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all available users.
    /// </summary>
    Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves users with pagination support.
    /// </summary>
    Task<PaginatedResult<UserDto>> GetAllUsersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries users using a specification pattern.
    /// </summary>
    Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user's information.
    /// </summary>
    Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);

    /// <summary>
    /// Deletes a user account.
    /// </summary>
    Task<bool> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    Task<bool> AssignRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);

    /// <summary>
    /// Retrieves all roles assigned to a user.
    /// </summary>
    Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the current authenticated user.
    /// </summary>
    Task<UserDto?> GetCurrentUserAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    Task<UserDto?> CreateUserAsync(CreateUserDto user);
}
