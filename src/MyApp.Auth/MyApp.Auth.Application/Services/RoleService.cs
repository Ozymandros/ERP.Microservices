using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Services;

public class RoleService : IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IMapper mapper,
        ILogger<RoleService> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string name)
    {
        var role = await _roleManager.FindByNameAsync(name);
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    public async Task<PaginatedResult<RoleDto>> GetAllRolesPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedRoles = await _roleRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(paginatedRoles.Items);
        return new PaginatedResult<RoleDto>(roleDtos, paginatedRoles.PageNumber, paginatedRoles.PageSize, paginatedRoles.TotalCount);
    }

    public async Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        var roleExists = await _roleManager.RoleExistsAsync(createRoleDto.Name);
        if (roleExists)
        {
            _logger.LogWarning("Role already exists: {@RoleData}", new { createRoleDto });
            return null;
        }

        var role = new ApplicationRole(createRoleDto.Name)
        {
            Name = createRoleDto.Name,
            Description = createRoleDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to create role: {@RoleData}", new { createRoleDto });
            return null;
        }

        return _mapper.Map<RoleDto>(role);
    }

    public async Task<bool> UpdateRoleAsync(Guid roleId, CreateRoleDto updateRoleDto)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        _logger.LogInformation("Updating role: {@RoleUpdate}", new { RoleId = roleId, updateRoleDto });

        role.Name = updateRoleDto.Name;
        role.Description = updateRoleDto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update role: {RoleId}", roleId);
            return false;
        }

        return true;
    }

    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        var result = await _roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to delete role: {RoleId}", roleId);
            return false;
        }

        return true;
    }

    public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var users = await _userRepository.GetByRoleAsync(roleName);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    public async Task<bool> AddPermissionToRole(CreateRolePermissionDto createDto)
    {
        var role = await _roleManager.FindByIdAsync(createDto.RoleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", createDto.RoleId);
            return false;
        }

        // Check if permission is already assigned to avoid duplicates
        var alreadyExists = await _roleRepository.HasPermissionAsync(createDto.RoleId, createDto.PermissionId);
        if (alreadyExists)
        {
            _logger.LogWarning("Permission already assigned to role: {RoleId}, {PermissionId}", createDto.RoleId, createDto.PermissionId);
            return false;
        }

        role.UpdatedAt = DateTime.UtcNow;
        role.RolePermissions.Add(new RolePermission
        {
            RoleId = createDto.RoleId,
            PermissionId = createDto.PermissionId
        });

        var result = await _roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to update role: {RoleId}", createDto.RoleId);
            return false;
        }

        _logger.LogInformation("Permission added to role: {RoleId}, {PermissionId}", createDto.RoleId, createDto.PermissionId);
        return true;
    }

    public async Task<bool> RemovePermissionFromRoleAsync(DeleteRolePermissionDto deleteDto)
    {
        var role = await _roleManager.FindByIdAsync(deleteDto.RoleId.ToString());
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", deleteDto.RoleId);
            return false;
        }

        var permissionExists = await _roleRepository.HasPermissionAsync(deleteDto.RoleId, deleteDto.PermissionId);
        if (!permissionExists)
        {
            _logger.LogWarning("Permission not assigned to role: {RoleId}, {PermissionId}", deleteDto.RoleId, deleteDto.PermissionId);
            return false;
        }

        var result = await _roleRepository.RemovePermissionFromRoleAsync(deleteDto.RoleId, deleteDto.PermissionId);
        if (!result)
        {
            _logger.LogWarning("Failed to remove permission from role: {RoleId}, {PermissionId}", deleteDto.RoleId, deleteDto.PermissionId);
            return false;
        }

        _logger.LogInformation("Permission removed from role: {RoleId}, {PermissionId}", deleteDto.RoleId, deleteDto.PermissionId);
        return true;
    }

    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _roleRepository.HasPermissionAsync(roleId, permissionId);
    }

    public async Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(Guid roleId)
    {
        IEnumerable<Permission> permissions = await _roleRepository.GetPermissionsForRoleAsync(roleId);
        return _mapper.Map<IEnumerable<PermissionDto>>(permissions);
    }

    /// <summary>
    /// Query roles with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<RoleDto>> QueryRolesAsync(ISpecification<ApplicationRole> spec)
    {
        try
        {
            var result = await _roleRepository.QueryAsync(spec);
            
            var dtos = result.Items.Select(r => new RoleDto(r.Id)
            {
                CreatedAt = r.CreatedAt,
                CreatedBy = "",
                UpdatedAt = r.UpdatedAt,
                UpdatedBy = null,
                Name = r.Name,
                Description = null
            }).ToList();
            
            return new PaginatedResult<RoleDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying roles");
            throw;
        }
    }
}
