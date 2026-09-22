using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Entities;
using MyApp.Auth.Domain.Repositories;
using MyApp.Shared.Application;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Auth.Application.Services;

/// <summary>
/// Provides operations for managing roles and their associated permissions.
/// </summary>
public class RoleService : AppServiceBase<Guid, ApplicationRole, RoleDto>, IRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoleRepository _roleRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<RoleService> _logger;

    /// <summary>
    /// Initializes a new instance of the RoleService class.
    /// </summary>
    /// <param name="roleManager">The role Manager.</param>
    /// <param name="userManager">The user Manager.</param>
    /// <param name="roleRepository">The role Repository.</param>
    /// <param name="userRepository">The user Repository.</param>
    /// <param name="mapper">The mapper.</param>
    /// <param name="unitOfWork">The unit Of Work.</param>
    /// <param name="eventPublisher">The event Publisher.</param>
    /// <param name="logger">The logger.</param>
    public RoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IRoleRepository roleRepository,
        IUserRepository userRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IEventPublisher eventPublisher,
        ILogger<RoleService> logger) : base(roleRepository, unitOfWork, eventPublisher, logger, ServiceNames.Auth)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _roleRepository = roleRepository;
        _userRepository = userRepository;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Gets the role by id asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <returns>The <see cref="RoleDto"/> if found; otherwise, <c>null</c>.</returns>
    public async Task<RoleDto?> GetRoleByIdAsync(Guid roleId)
    {
        var role = await _roleManager.FindByIdAsync(roleId.ToString());
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    /// <summary>
    /// Gets the role by name asynchronously.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>The <see cref="RoleDto"/> if found; otherwise, <c>null</c>.</returns>
    public async Task<RoleDto?> GetRoleByNameAsync(string name)
    {
        var role = await _roleManager.FindByNameAsync(name);
        return role == null ? null : _mapper.Map<RoleDto>(role);
    }

    /// <summary>
    /// Gets all roles asynchronously.
    /// </summary>
    /// <returns>A collection of all <see cref="RoleDto"/> objects.</returns>
    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    /// <summary>
    /// Gets all roles paginated asynchronously.
    /// </summary>
    /// <param name="pageNumber">The page Number.</param>
    /// <param name="pageSize">The page Size.</param>
    /// <returns>A paginated result containing <see cref="RoleDto"/> objects for the requested page.</returns>
    public async Task<PaginatedResult<RoleDto>> GetAllRolesPaginatedAsync(int pageNumber, int pageSize)
    {
        var paginatedRoles = await _roleRepository.GetAllPaginatedAsync(pageNumber, pageSize);
        var roleDtos = _mapper.Map<IEnumerable<RoleDto>>(paginatedRoles.Items);
        return new PaginatedResult<RoleDto>(roleDtos, paginatedRoles.PageNumber, paginatedRoles.PageSize, paginatedRoles.TotalCount);
    }

    /// <summary>
    /// Creates a role asynchronously.
    /// </summary>
    /// <param name="createRoleDto">The create Role Dto.</param>
    /// <returns>The created <see cref="RoleDto"/> on success, or <c>null</c> if a duplicate role name exists.</returns>
    public async Task<RoleDto?> CreateRoleAsync(CreateRoleDto createRoleDto)
    {
        if (await _roleRepository.NameExistsAsync(createRoleDto.Name))
        {
            _logger.LogWarning("Role already exists: {@RoleData}", new { createRoleDto });
            return null;
        }

        var role = new ApplicationRole(createRoleDto.Name)
        {
            Id = Guid.NewGuid(),
            Name = createRoleDto.Name,
            NormalizedName = createRoleDto.Name.ToUpperInvariant(),
            Description = createRoleDto.Description,
            CreatedAt = DateTime.UtcNow
        };

        await _roleRepository.AddAsync(role);
        await SaveChangesAsync();

        return _mapper.Map<RoleDto>(role);
    }

    /// <summary>
    /// Updates the role asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <param name="updateRoleDto">The update Role Dto.</param>
    /// <returns><c>true</c> if the update succeeded; otherwise, <c>false</c>.</returns>
    public async Task<bool> UpdateRoleAsync(Guid roleId, CreateRoleDto updateRoleDto)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        _logger.LogInformation("Updating role: {@RoleUpdate}", new { RoleId = roleId, updateRoleDto });

        role.Name = updateRoleDto.Name;
        role.NormalizedName = updateRoleDto.Name.ToUpperInvariant();
        role.Description = updateRoleDto.Description;
        role.UpdatedAt = DateTime.UtcNow;

        await _roleRepository.UpdateAsync(role);
        await SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deletes the role asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <returns><c>true</c> if deletion succeeded; otherwise, <c>false</c>.</returns>
    public async Task<bool> DeleteRoleAsync(Guid roleId)
    {
        var role = await _roleRepository.GetByIdAsync(roleId);
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", roleId);
            return false;
        }

        await _roleRepository.DeleteAsync(role);
        await SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Gets the users in role asynchronously.
    /// </summary>
    /// <param name="roleName">The role Name.</param>
    /// <returns>A collection of <see cref="UserDto"/> objects for users in the role.</returns>
    public async Task<IEnumerable<UserDto>> GetUsersInRoleAsync(string roleName)
    {
        var users = await _userRepository.GetByRoleAsync(roleName);
        return _mapper.Map<IEnumerable<UserDto>>(users);
    }

    /// <summary>
    /// Adds a permission to role.
    /// </summary>
    /// <param name="createDto">The create Dto.</param>
    /// <returns><c>true</c> if the permission was successfully assigned; <c>false</c> if the role was not found or the permission is already assigned.</returns>
    public async Task<bool> AddPermissionToRole(CreateRolePermissionDto createDto)
    {
        var role = await _roleRepository.GetByIdAsync(createDto.RoleId);
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

        await _roleRepository.UpdateAsync(role);
        await SaveChangesAsync();

        _logger.LogInformation("Permission added to role: {RoleId}, {PermissionId}", createDto.RoleId, createDto.PermissionId);
        return true;
    }

    /// <summary>
    /// Removes the permission from role asynchronously.
    /// </summary>
    /// <param name="deleteDto">The delete Dto.</param>
    /// <returns><c>true</c> if the permission was successfully removed; <c>false</c> if the role was not found or the permission was not assigned.</returns>
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
        await SaveChangesAsync();
        if (!result)
        {
            _logger.LogWarning("Failed to remove permission from role: {RoleId}, {PermissionId}", deleteDto.RoleId, deleteDto.PermissionId);
            return false;
        }

        _logger.LogInformation("Permission removed from role: {RoleId}, {PermissionId}", deleteDto.RoleId, deleteDto.PermissionId);
        return true;
    }

    /// <summary>
    /// Determines whether permission asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <param name="permissionId">The permission Id.</param>
    /// <returns><c>true</c> if the role has the permission; otherwise, <c>false</c>.</returns>
    public async Task<bool> HasPermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _roleRepository.HasPermissionAsync(roleId, permissionId);
    }

    /// <summary>
    /// Gets the permissions for role asynchronously.
    /// </summary>
    /// <param name="roleId">The role Id.</param>
    /// <returns>A collection of <see cref="PermissionDto"/> objects assigned to the role.</returns>
    public async Task<IEnumerable<PermissionDto>> GetPermissionsForRoleAsync(Guid roleId)
    {
        IEnumerable<Permission> permissions = await _roleRepository.GetPermissionsForRoleAsync(roleId);
        return _mapper.Map<IEnumerable<PermissionDto>>(permissions);
    }

    /// <summary>
    /// Adds a permissions to role.
    /// </summary>
    /// <param name="createDto">The create Dto.</param>
    /// <returns><c>true</c> if at least one permission was added; otherwise, <c>false</c>.</returns>
    public async Task<bool> AddPermissionsToRole(CreateRolePermissionsDto createDto)
    {
        var role = await _roleRepository.GetByIdAsync(createDto.RoleId);
        if (role == null)
        {
            _logger.LogWarning("Role not found: {RoleId}", createDto.RoleId);
            return false;
        }

        var addedCount = 0;
        var skippedCount = 0;

        foreach (var permissionId in createDto.PermissionIds)
        {
            // Check if permission is already assigned to avoid duplicates
            var alreadyExists = await _roleRepository.HasPermissionAsync(createDto.RoleId, permissionId);
            if (alreadyExists)
            {
                skippedCount++;
                continue;
            }

            role.RolePermissions.Add(new RolePermission
            {
                RoleId = createDto.RoleId,
                PermissionId = permissionId
            });
            addedCount++;
        }

        if (addedCount > 0)
        {
            role.UpdatedAt = DateTime.UtcNow;
            await _roleRepository.UpdateAsync(role);
            await SaveChangesAsync();
        }

        _logger.LogInformation("Added {AddedCount} permissions to role {RoleId}, skipped {SkippedCount} duplicates",
            addedCount, createDto.RoleId, skippedCount);
        return addedCount > 0;
    }

    /// <summary>
    /// Removes the permissions from role asynchronously.
    /// </summary>
    /// <param name="deleteDto">The delete Dto.</param>
    /// <returns><c>true</c> if at least one permission was removed; otherwise, <c>false</c>.</returns>
    public async Task<bool> RemovePermissionsFromRoleAsync(DeleteRolePermissionsDto deleteDto)
    {
        if (await _roleRepository.GetByIdAsync(deleteDto.RoleId) is null)
        {
            _logger.LogWarning("Role not found: {RoleId}", deleteDto.RoleId);
            return false;
        }

        var removedCount = 0;
        var notFoundCount = 0;

        foreach (var permissionId in deleteDto.PermissionIds)
        {
            var permissionExists = await _roleRepository.HasPermissionAsync(deleteDto.RoleId, permissionId);
            if (!permissionExists)
            {
                notFoundCount++;
                continue;
            }

            var result = await _roleRepository.RemovePermissionFromRoleAsync(deleteDto.RoleId, permissionId);
            if (result)
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
            await SaveChangesAsync();

        _logger.LogInformation("Removed {RemovedCount} permissions from role {RoleId}, {NotFoundCount} not found",
            removedCount, deleteDto.RoleId, notFoundCount);
        return removedCount > 0;
    }

    /// <summary>
    /// Query roles asynchronously.
    /// </summary>
    /// <param name="spec">The spec.</param>
    /// <returns>A paginated result containing matched <see cref="RoleDto"/> objects.</returns>
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
