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
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UserService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<UserService> logger,
        IHttpContextAccessor httpContextAccessor,
        IPermissionRepository permissionRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _userRepository = userRepository;
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
        if(_httpContextAccessor.HttpContext == null)
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
            IsAdmin = isAdmin
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user == null ? null : _mapper.Map<UserDto>(user);
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

        if (!string.IsNullOrEmpty(updateUserDto.Email) && updateUserDto.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(updateUserDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Email already in use: {@Email}", new { Email = updateUserDto.Email });
                return false;
            }
            user.Email = updateUserDto.Email;
            user.UserName = updateUserDto.Email;
        }

        if (!string.IsNullOrEmpty(updateUserDto.FirstName))
            user.FirstName = updateUserDto.FirstName;

        if (!string.IsNullOrEmpty(updateUserDto.LastName))
            user.LastName = updateUserDto.LastName;

        if (!string.IsNullOrEmpty(updateUserDto.PhoneNumber))
            user.PhoneNumber = updateUserDto.PhoneNumber;

        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update user: {UserId}", userId);
            return false;
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
                IsAdmin = false
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
