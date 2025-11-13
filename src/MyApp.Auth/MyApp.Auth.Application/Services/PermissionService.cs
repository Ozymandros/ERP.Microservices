using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Services;

public class PermissionService : IPermissionService
{
    private readonly IPermissionRepository _permissionRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PermissionService> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(UserManager<ApplicationUser> userManager,
        IPermissionRepository permissionRepository,
        IMapper mapper,
        ILogger<PermissionService> logger)
    {
        _userManager = userManager;
        _permissionRepository = permissionRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string module, string action)
    {
        try
        {
            var permissions = await _permissionRepository.GetAllPermissionsByUserId(userId);
            return permissions.Any(p => p.Module == module && p.Action == action);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> HasPermissionAsync(string? username, string module, string action)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        try
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null || string.IsNullOrWhiteSpace(username))
            {
                return false;
            }

            var userPermissions = await _permissionRepository.GetByUserName(username, module, action);
            if (userPermissions.Any())
                return true;

            var roles = await _userManager.GetRolesAsync(user);

            if (roles.Any(r => r.Equals("Admin", StringComparison.OrdinalIgnoreCase)))
                return true;

            foreach (var roleName in roles)
            {
                var rolePermissions = await _permissionRepository.GetByRoleName(roleName, module, action);
                if (rolePermissions.Any())
                    return true;
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for username {Username}", username);
        }

        return false;
    }

    public async Task<IEnumerable<PermissionDto>> GetAllPermissionsAsync()
    {
        var entities = await _permissionRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<PermissionDto>>(entities);
    }

    public async Task<PaginatedResult<PermissionDto>> GetAllPermissionsPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedPermissions = await _permissionRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var permissionDtos = _mapper.Map<IEnumerable<PermissionDto>>(paginatedPermissions.Items);
        return new PaginatedResult<PermissionDto>(permissionDtos, paginatedPermissions.PageNumber, paginatedPermissions.PageSize, paginatedPermissions.TotalCount);
    }

    public async Task<PermissionDto?> GetPermissionByIdAsync(Guid id)
    {
        var entity = await _permissionRepository.GetByIdAsync(id);
        return entity == null ? null : _mapper.Map<PermissionDto>(entity);
    }

    public async Task<PermissionDto?> GetPermissionByModuleActionAsync(string module, string action)
    {
        var entities = await _permissionRepository.GetByUserName("", module, action); // Not ideal, but repository offers specific methods
        var match = entities?.FirstOrDefault(e => e.Module == module && e.Action == action);
        return match == null ? null : _mapper.Map<PermissionDto>(match);
    }

    public async Task<PermissionDto?> CreatePermissionAsync(MyApp.Auth.Application.Contracts.DTOs.CreatePermissionDto createPermissionDto)
    {
        try
        {
            var existing = await _permissionRepository.GetAllAsync();
            if (existing.Any(p => p.Module == createPermissionDto.Module && p.Action == createPermissionDto.Action))
            {
                _logger.LogWarning("Permission already exists: {Module}:{Action}", createPermissionDto.Module, createPermissionDto.Action);
                return null;
            }

            var entity = new Permission
            {
                Id = Guid.NewGuid(),
                Module = createPermissionDto.Module,
                Action = createPermissionDto.Action,
                Description = createPermissionDto.Description
            };

            await _permissionRepository.AddAsync(entity);
            return _mapper.Map<PermissionDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission {Module}:{Action}", createPermissionDto.Module, createPermissionDto.Action);
            return null;
        }
    }

    public async Task<bool> UpdatePermissionAsync(Guid id, MyApp.Auth.Application.Contracts.DTOs.UpdatePermissionDto updatePermissionDto)
    {
        try
        {
            var entity = await _permissionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", id);
                return false;
            }

            entity.Module = updatePermissionDto.Module;
            entity.Action = updatePermissionDto.Action;
            entity.Description = updatePermissionDto.Description;

            await _permissionRepository.UpdateAsync(entity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission {PermissionId}", id);
            return false;
        }
    }

    public async Task<bool> DeletePermissionAsync(Guid id)
    {
        try
        {
            var entity = await _permissionRepository.GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", id);
                return false;
            }

            await _permissionRepository.DeleteAsync(entity);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission {PermissionId}", id);
            return false;
        }
    }

    /// <summary>
    /// Query permissions with filtering, sorting, and pagination
    /// </summary>
    public async Task<PaginatedResult<PermissionDto>> QueryPermissionsAsync(ISpecification<Permission> spec)
    {
        try
        {
            var result = await _permissionRepository.QueryAsync(spec);
            
            var dtos = result.Items.Select(p => new PermissionDto
            {
                Id = p.Id,
                Module = p.Module,
                Action = p.Action,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList();
            
            return new PaginatedResult<PermissionDto>(dtos, result.PageNumber, result.PageSize, result.TotalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying permissions");
            throw;
        }
    }
}