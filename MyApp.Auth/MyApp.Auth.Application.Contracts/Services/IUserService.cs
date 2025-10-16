using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.Application.Contracts.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto);
    Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword);
    Task<bool> DeleteUserAsync(Guid userId);
    Task<bool> AssignRoleAsync(Guid userId, string roleName);
    Task<bool> RemoveRoleAsync(Guid userId, string roleName);
    Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId);
}
