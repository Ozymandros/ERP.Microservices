using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Services;

public class UserService : IUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMapper mapper,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IPermissionRepository permissionRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _mapper = mapper;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _permissionRepository = permissionRepository;
    }


    /// <summary>
    /// Get current user
    /// </summary>
    /// <returns></returns>
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            _logger.LogWarning("HttpContext is null");
            return null;
        }
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
        if (user is null)
        {
            _logger.LogWarning("Current user not found in HttpContext");
            return null;
        }

        var baseUserDto = _mapper.Map<UserDto>(user);

        var roles = await GetUserRolesAsync(user.Id);
        var mappedRoles = _mapper.Map<List<RoleDto?>>(roles);

        List<PermissionDto?> permissions;
        bool isAdmin = roles.Any(r => r.Name != null && r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        if (isAdmin)
        {
            var allPermissions = await _permissionRepository.GetAllAsync();
            permissions = _mapper.Map<List<PermissionDto?>>(allPermissions);
        }
        else
        {
            var userPermissions = await _permissionRepository.GetAllPermissionsByUserId(user.Id);
            permissions = _mapper.Map<List<PermissionDto?>>(userPermissions);
        }

        return new UserDto(baseUserDto.Id)
        {
            CreatedAt = baseUserDto.CreatedAt,
            CreatedBy = baseUserDto.CreatedBy,
            UpdatedAt = baseUserDto.UpdatedAt,
            UpdatedBy = baseUserDto.UpdatedBy,
            Email = baseUserDto.Email,
            Username = baseUserDto.Username,
            FirstName = baseUserDto.FirstName,
            LastName = baseUserDto.LastName,
            EmailConfirmed = baseUserDto.EmailConfirmed,
            IsExternalLogin = baseUserDto.IsExternalLogin,
            ExternalProvider = baseUserDto.ExternalProvider,
            Roles = mappedRoles,
            Permissions = permissions,
            IsAdmin = isAdmin,
            IsActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return null;
        }

        var baseUserDto = _mapper.Map<UserDto>(user);

        // Get user roles
        var userRoles = await _roleRepository.GetRolesByUserIdAsync(user.Id);
        bool isAdmin = userRoles.Any(r => r.Name != null && r.Name.Equals("Admin", StringComparison.OrdinalIgnoreCase));

        // Get permissions based on admin status
        List<Permission> permissions;
        if (isAdmin)
        {
            // Admin users have all permissions
            var allPermissions = await _permissionRepository.GetAllAsync();
            permissions = allPermissions.ToList();
        }
        else
        {
            // Get permissions from user's roles
            permissions = new List<Permission>();
            foreach (var role in userRoles)
            {
                var rolePermissions = await _roleRepository.GetPermissionsForRoleAsync(role.Id);
                permissions.AddRange(rolePermissions);
            }

            // Also get direct user permissions
            var userPermissions = await _permissionRepository.GetAllPermissionsByUserId(user.Id);
            permissions.AddRange(userPermissions);
        }

        var roleDtos = userRoles.Select(r => new RoleDto(r.Id)
        {
            Name = r.Name,
            Description = r.Description
        }).ToList();

        var permissionDtos = permissions
            .DistinctBy(p => p.Id)
            .Select(p => new PermissionDto(p.Id)
            {
                Module = p.Module,
                Action = p.Action,
                Description = p.Description
            })
            .ToList();

        return new UserDto(baseUserDto.Id)
        {
            CreatedAt = baseUserDto.CreatedAt,
            CreatedBy = baseUserDto.CreatedBy,
            UpdatedAt = baseUserDto.UpdatedAt,
            UpdatedBy = baseUserDto.UpdatedBy,
            Email = baseUserDto.Email,
            Username = baseUserDto.Username,
            FirstName = baseUserDto.FirstName,
            LastName = baseUserDto.LastName,
            EmailConfirmed = baseUserDto.EmailConfirmed,
            IsExternalLogin = baseUserDto.IsExternalLogin,
            ExternalProvider = baseUserDto.ExternalProvider,
            Roles = roleDtos,
            Permissions = permissionDtos,
            IsAdmin = isAdmin,
            IsActive = user.LockoutEnd == null || user.LockoutEnd <= DateTimeOffset.UtcNow
        };
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user == null ? null : _mapper.Map<UserDto>(user);
    }

    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<PaginatedResult<UserDto>> GetAllUsersPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedUsers = await _userRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var userDtos = _mapper.Map<IEnumerable<UserDto>>(paginatedUsers.Items);
        return new PaginatedResult<UserDto>(userDtos, paginatedUsers.PageNumber, paginatedUsers.PageSize, paginatedUsers.TotalCount);
    }

    public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserDto updateUserDto)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return false;
        }

        _logger.LogInformation("Updating user: {@UserUpdate}", new { UserId = userId, updateUserDto });

        // Validate email uniqueness if requested
        if (!string.IsNullOrWhiteSpace(updateUserDto.Email) && updateUserDto.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                _logger.LogWarning("Email already in use: {Email}", updateUserDto.Email);
                return false;
            }
        }

        // Apply identity-managed changes first (these persist directly via UserManager)
        var identityChanged = false;
        if (!string.IsNullOrWhiteSpace(updateUserDto.Email) && updateUserDto.Email != user.Email)
        {
            var emailResult = await _userManager.SetEmailAsync(user, updateUserDto.Email);
            if (!emailResult.Succeeded)
            {
                _logger.LogWarning("Failed to set email for user: {UserId}", userId);
                return false;
            }

            var userNameResult = await _userManager.SetUserNameAsync(user, updateUserDto.Email);
            if (!userNameResult.Succeeded)
            {
                _logger.LogWarning("Failed to set username for user: {UserId}", userId);
                return false;
            }

            identityChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(updateUserDto.PhoneNumber) && updateUserDto.PhoneNumber != user.PhoneNumber)
        {
            var phoneResult = await _userManager.SetPhoneNumberAsync(user, updateUserDto.PhoneNumber);
            if (!phoneResult.Succeeded)
            {
                _logger.LogWarning("Failed to set phone number for user: {UserId}", userId);
                return false;
            }

            identityChanged = true;
        }

        // If identity changes occurred, reload the user so subsequent profile updates operate on fresh state
        if (identityChanged)
        {
            user = await _userManager.FindByIdAsync(userId.ToString()) ?? user;
        }

        // Apply profile changes and persist once if necessary
        var profileChanged = false;
        if (!string.IsNullOrWhiteSpace(updateUserDto.FirstName) && updateUserDto.FirstName != user.FirstName)
        {
            user.FirstName = updateUserDto.FirstName;
            profileChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(updateUserDto.LastName) && updateUserDto.LastName != user.LastName)
        {
            user.LastName = updateUserDto.LastName;
            profileChanged = true;
        }

        if (profileChanged)
        {
            user.UpdatedAt = DateTime.UtcNow;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                _logger.LogWarning("Failed to update user profile fields: {UserId}", userId);
                return false;
            }
        }

        return true;
    }

    public async Task<bool> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return false;
        }

        var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to change password for user: {UserId}", userId);
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return false;
        }

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete user: {UserId}", userId);
            return false;
        }

        return true;
    }

    public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return false;
        }

        var roleExists = await _roleManager.RoleExistsAsync(roleName);
        if (!roleExists)
        {
            _logger.LogWarning("Role not found: {RoleName}", roleName);
            return false;
        }

        var result = await _userManager.AddToRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to assign role to user: {UserId}", userId);
            return false;
        }

        return true;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return false;
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to remove role from user: {UserId}", userId);
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<RoleDto>> GetUserRolesAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User not found: {@User}", new { UserId = userId });
            return Enumerable.Empty<RoleDto>();
        }

        var roleNames = await _userManager.GetRolesAsync(user);
        var roles = new List<RoleDto>();

        foreach (var roleName in roleNames)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                roles.Add(_mapper.Map<RoleDto>(role));
            }
        }

        return roles;
    }

    public async Task<UserDto?> CreateUserAsync(CreateUserDto user)
    {
        if (user is null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        var userEntity = _mapper.Map<ApplicationUser>(user);
        var result = await _userManager.CreateAsync(userEntity, user.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("Failed to create user: {UserId}. Errors: {Errors}", user.Id, errors);
            return null;
        }

        return _mapper.Map<UserDto>(userEntity);
    }

    /// <summary>
    /// Query users with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<UserDto>> QueryUsersAsync(ISpecification<ApplicationUser> spec)
    {
        try
        {
            var result = await _userRepository.QueryAsync(spec);

            var dtos = result.Items.Select(u => new UserDto(u.Id)
            {
                CreatedAt = u.CreatedAt,
                CreatedBy = "",
                UpdatedAt = u.UpdatedAt,
                UpdatedBy = null,
                Email = u.Email,
                Username = u.UserName,
                FirstName = u.FirstName,
                LastName = u.LastName,
                EmailConfirmed = u.EmailConfirmed,
                IsExternalLogin = u.IsExternalLogin,
                ExternalProvider = u.ExternalProvider,
                Roles = new List<RoleDto?>(),
                Permissions = new List<PermissionDto?>(),
                IsAdmin = false,
                IsActive = u.LockoutEnd == null || u.LockoutEnd <= DateTimeOffset.UtcNow
            }).ToList();

            return new PaginatedResult<UserDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying users");
            throw;
        }
    }
}
