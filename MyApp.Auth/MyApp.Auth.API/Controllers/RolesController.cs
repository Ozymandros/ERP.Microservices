
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using System;

namespace MyApp.Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<RolesController> _logger;

    public RolesController(IRoleService roleService,
        ILogger<RolesController> logger,
        ICacheService cacheService,
        IPermissionService permissionService)
    {
        _roleService = roleService;
        _logger = logger;
        _cacheService = cacheService;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Get all roles
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
    {
        try
        {
            var roles = await _cacheService.GetStateAsync<IEnumerable<RoleDto>>("all_roles");
            if (roles != null)
            {
                return Ok(roles);
            }

            roles = await _roleService.GetAllRolesAsync();
            await _cacheService.SaveStateAsync("all_roles", roles);

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all roles");
            return StatusCode(500, new { message = "An error occurred retrieving roles" });
        }
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = $"Role-{id}";
            var role = await _cacheService.GetStateAsync<RoleDto>(cacheKey); // 1. Intentar obtenir de la cache

            if (role is not null)
            {
                return Ok(role); // Retorna des de la cache
            }

            // 2. La dada NO �s a la cache, obtenir de la DB
            role = await _roleService.GetRoleByIdAsync(id);
            if (role is null)
            {
                _logger.LogWarning("Role not found: {RoleId}", id);
                return NotFound(new { message = "Role not found" });
            }

            await _cacheService.SaveStateAsync<RoleDto>(cacheKey, role);

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role: {RoleId}", id);
            return NotFound(new { message = "Role not found" });
        }
    }

    /// <summary>
    /// Get role by name
    /// </summary>
    [HttpGet("name/{name}")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> GetByName(string name)
    {
        try
        {
            var role = await _roleService.GetRoleByNameAsync(name);
            if (role == null)
            {
                _logger.LogWarning("Role not found by name: {RoleName}", name);
                return NotFound(new { message = "Role not found" });
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role by name: {RoleName}", name);
            return StatusCode(500, new { message = "An error occurred retrieving the role" });
        }
    }

    /// <summary>
    /// Create new role
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> Create([FromBody] CreateRoleDto createRoleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _roleService.CreateRoleAsync(createRoleDto);
            if (result == null)
            {
                _logger.LogWarning("Failed to create role: {RoleName}", createRoleDto.Name);
                return Conflict(new { message = "Role already exists" });
            }

            await _cacheService.RemoveStateAsync("all_roles");

            _logger.LogInformation("Role created: {RoleName}", createRoleDto.Name);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {RoleName}", createRoleDto.Name);
            return StatusCode(500, new { message = "An error occurred creating the role" });
        }
    }

    /// <summary>
    /// Update role
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateRoleDto updateRoleDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _roleService.UpdateRoleAsync(id, updateRoleDto);
            if (!result)
            {
                _logger.LogWarning("Failed to update role: {RoleId}", id);
                return NotFound(new { message = "Role not found" });
            }

            string cacheKey = $"Role-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("Role updated: {RoleId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred updating the role" });
        }
    }

    /// <summary>
    /// Delete role
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _roleService.DeleteRoleAsync(id);
            if (!result)
            {
                _logger.LogWarning("Failed to delete role: {RoleId}", id);
                return NotFound(new { message = "Role not found" });
            }

            string cacheKey = $"Role-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_roles");

            _logger.LogInformation("Role deleted: {RoleId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {RoleId}", id);
            return StatusCode(500, new { message = "An error occurred deleting the role" });
        }
    }

    /// <summary>
    /// Get users in role
    /// </summary>
    [HttpGet("{name}/users")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsersInRole(string name)
    {
        try
        {
            var users = await _roleService.GetUsersInRoleAsync(name);
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users in role: {RoleName}", name);
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    [HttpPost("{roleId}/permissions")]
    public async Task<IActionResult> AddPermissionToRole(Guid roleId, Guid permissionId)
    {
        try
        {
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role is null)
            {
                return NotFound(new { message = "Role not found" });
            }

            var permission = await _permissionService.GetPermissionByIdAsync(permissionId);
            if (permission is null)
            {
                return NotFound(new { message = "Permission not found" });
            }

            var createDto = new CreateRolePermissionDto(roleId, permissionId);
            var result = await _roleService.AddPermissionToRole(createDto);
            if (result is false)
            {
                _logger.LogWarning($"Failed to create role-permission: {role.Name}-{permission.Module}.{permission.Action}", role.Name);
                return Conflict(new { message = "Role already exists" });
            }

            await _cacheService.RemoveStateAsync("all_roles");
            await _cacheService.RemoveStateAsync("all_permissions");

            _logger.LogInformation($"RolePermission created: {role.Name} ({permission.Module}.{permission.Action})");
            return NoContent();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions in role: {RoleName}", roleId);
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    [HttpDelete("{roleId}/permissions/{permissionId}")]
    public async Task<IActionResult> RemovePermissionFromRole(Guid roleId, Guid permissionId)
    {
        try
        {
            // 1. Check for Existing Association
            var alreadyExists = await _roleService.HasPermissionAsync(roleId, permissionId);

            if (!alreadyExists)
            {
                // Idempotency: If it doesn't exist, we treat the operation as successful (204 No Content)
                // or 404 Not Found if you want to be stricter about the association entity.
                return NoContent();
            }

            // 2. Remove the Association
            var deleteDto = new DeleteRolePermissionDto(roleId, permissionId);
            var success = await _roleService.RemovePermissionFromRoleAsync(deleteDto);

            if (!success)
            {
                // This case should be rare but catches a DB/service failure
                _logger.LogError("Failed to remove RolePermission for Role: {RoleId}, Permission: {PermissionId}", roleId, permissionId);
                return StatusCode(500, new { message = "Failed to unassign permission due to an internal error." });
            }

            // 3. Invalidate Cache and Return Success (204 No Content)
            await _cacheService.RemoveStateAsync("all_roles"); // Cache invalidation

            _logger.LogInformation("Permission {PermissionId} removed from Role {RoleId}.", permissionId, roleId);

            return NoContent(); // 204 No Content is the standard for successful DELETE
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role: {RoleId}", roleId);
            return StatusCode(500, new { message = "An error occurred while unassigning the permission." });
        }
    }

    [HttpGet("{roleId}/permissions")]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        try
        {
            // 1. Validate Role Existence
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role is null)
            {
                return NotFound(new { message = $"Role with ID '{roleId}' not found." });
            }

            // 2. Retrieve Permissions (assuming this method returns a list/collection of Permission DTOs)
            var permissions = await _roleService.GetPermissionsForRoleAsync(roleId);
            if (permissions is null)
            {
                // Handle case where service returns null unexpectedly
                return StatusCode(500, new { message = "Failed to retrieve permissions." });
            }

            // 3. Return Success (200 OK)
            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permissions for role: {RoleId}", roleId);
            return StatusCode(500, new { message = "An error occurred retrieving role permissions." });
        }
    }
}
