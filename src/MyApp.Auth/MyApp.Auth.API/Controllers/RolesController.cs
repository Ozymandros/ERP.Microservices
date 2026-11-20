

using MyApp.Auth.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Security;
using System;


using MyApp.Shared.Infrastructure.Export;
namespace MyApp.Auth.API.Controllers;

/// <summary>
/// Manages role CRUD operations, role-permission assignments, and role membership queries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeJwt]
[Produces("application/json")]
public class RolesController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IRoleService _roleService;
    private readonly IPermissionService _permissionService;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<RolesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RolesController"/> class.
    /// </summary>
    /// <param name="roleService">The service used to manage roles.</param>
    /// <param name="logger">The logger for this controller.</param>
    /// <param name="cacheService">The distributed cache service.</param>
    /// <param name="permissionService">The service used to manage permissions.</param>
    /// <param name="logSanitizer">The log sanitizer for masking sensitive values in log output.</param>
    public RolesController(IRoleService roleService,
        ILogger<RolesController> logger,
        ICacheService cacheService,
        IPermissionService permissionService,
        ILogSanitizer logSanitizer)
    {
        _roleService = roleService;
        _logger = logger;
        _cacheService = cacheService;
        _permissionService = permissionService;
        _logSanitizer = logSanitizer;
    }

    /// <summary>
    /// Exports all roles as an XLSX spreadsheet file.
    /// </summary>
    /// <returns>An XLSX file containing all roles.</returns>
    [HttpGet("export-xlsx")]
    [HasPermission("Roles", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var roles = await _cacheService.GetStateAsync<IEnumerable<RoleDto>>("all_roles")
                ?? await _roleService.GetAllRolesAsync();
            var bytes = roles.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Roles.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting roles to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting roles" });
        }
    }

    /// <summary>
    /// Exports all roles as a PDF file.
    /// </summary>
    /// <returns>A PDF file containing all roles.</returns>
    [HttpGet("export-pdf")]
    [HasPermission("Roles", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var roles = await _cacheService.GetStateAsync<IEnumerable<RoleDto>>("all_roles")
                ?? await _roleService.GetAllRolesAsync();
            var bytes = roles.ExportToPdf();
            return File(bytes, "application/pdf", "Roles.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting roles to PDF");
            return StatusCode(500, new { message = "An error occurred exporting roles" });
        }
    }

    /// <summary>
    /// Gets all roles, or executes a filtered/paginated query when query parameters are present.
    /// </summary>
    /// <param name="query">Optional query specification for filtering, sorting, and pagination.</param>
    /// <returns>A list of <see cref="RoleDto"/> objects, or a paginated result when query parameters are provided.</returns>
    [HttpGet]
    [HasPermission("Roles", "Read")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAll([FromQuery] QuerySpec query)
    {
        try
        {
            // If query parameters are provided, perform a search/paginated query
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new RoleQuerySpec(query);
                var result = await _roleService.QueryRolesAsync(spec);
                return Ok(result);
            }

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
            _logger.LogError(ex, "Error retrieving roles");
            return StatusCode(500, new { message = "An error occurred retrieving roles" });
        }
    }

    /// <summary>
    /// Gets all roles with explicit pagination.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <returns>A paginated result containing roles for the requested page.</returns>
    [HttpGet("paginated")]
    [HasPermission("Roles", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<RoleDto>>> GetAllPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _roleService.GetAllRolesPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated roles");
            return StatusCode(500, new { message = "An error occurred retrieving roles" });
        }
    }

    /// <summary>
    /// Searches roles with advanced filtering, sorting, and pagination.
    /// </summary>
    /// <param name="query">The query specification containing filter, sort, and pagination parameters.</param>
    /// <returns>A paginated result containing roles matching the query criteria.</returns>
    /// <remarks>
    /// Supported filters: name, description
    /// Supported sort fields: id, name, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Roles", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<RoleDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            // Bind filters from query parameters
            query.BindFiltersFromQuery(Request.Query);

            query.Validate();
            var spec = new RoleQuerySpec(query);
            var result = await _roleService.QueryRolesAsync(spec);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching roles");
            return StatusCode(500, new { message = "An error occurred searching roles" });
        }
    }

    /// <summary>
    /// Gets a role by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the role to retrieve.</param>
    /// <returns>The <see cref="RoleDto"/> for the specified role, or 404 if not found.</returns>
    [HttpGet("{id}")]
    [HasPermission("Roles", "Read")]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<RoleDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = "Role-" + id;
            var role = await _cacheService.GetStateAsync<RoleDto>(cacheKey); // 1. Try to get from cache

            if (role is not null)
            {
                return Ok(role); // Return from cache
            }

            // 2. Data NOT in cache, get from DB
            role = await _roleService.GetRoleByIdAsync(id);
            if (role is null)
            {
                _logger.LogWarning("Role with ID {@Role} not found", new { RoleId = id });
                return NotFound(new { message = "Role not found" });
            }

            await _cacheService.SaveStateAsync<RoleDto>(cacheKey, role);

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role: {@Role}", new { RoleId = id });
            return NotFound(new { message = "Role not found" });
        }
    }

    /// <summary>
    /// Gets a role by its name.
    /// </summary>
    /// <param name="name">The name of the role to retrieve.</param>
    /// <returns>The <see cref="RoleDto"/> for the specified role, or 404 if not found.</returns>
    [HttpGet("name/{name}")]
    [HasPermission("Roles", "Read")]
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
                _logger.LogWarning("Role with name {@Role} not found", new { RoleName = name });
                return NotFound(new { message = "Role not found" });
            }

            return Ok(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role by name: {@Role}", new { RoleName = name });
            return StatusCode(500, new { message = "An error occurred retrieving the role" });
        }
    }

    /// <summary>
    /// Creates a new role.
    /// </summary>
    /// <param name="createRoleDto">The data transfer object containing role creation details.</param>
    /// <returns>The created <see cref="RoleDto"/>, or 409 if the role already exists.</returns>
    [HttpPost]
    [HasPermission("Roles", "Create")]
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
                _logger.LogWarning("Failed to create role: {@Role}", new { RoleName = createRoleDto.Name });
                return Conflict(new { message = "Role already exists" });
            }

            await _cacheService.RemoveStateAsync("all_roles");

            _logger.LogInformation("Role created: {@Role}", new { RoleName = createRoleDto.Name });
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {@Role}", new { RoleName = createRoleDto.Name });
            return StatusCode(500, new { message = "An error occurred creating the role" });
        }
    }

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    /// <param name="id">The unique identifier of the role to update.</param>
    /// <param name="updateRoleDto">The data transfer object containing updated role details.</param>
    /// <returns>204 No Content if the update succeeded, or 404 if the role was not found.</returns>
    [HttpPut("{id}")]
    [HasPermission("Roles", "Update")]
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
                _logger.LogWarning("Failed to update role: {@Role}", new { RoleId = id });
                return NotFound(new { message = "Role not found" });
            }

            string cacheKey = "Role-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("Role updated: {@Role}", new { RoleId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {@Role}", new { RoleId = id });
            return StatusCode(500, new { message = "An error occurred updating the role" });
        }
    }

    /// <summary>
    /// Deletes a role by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the role to delete.</param>
    /// <returns>204 No Content if the deletion succeeded, or 404 if the role was not found.</returns>
    [HttpDelete("{id}")]
    [HasPermission("Roles", "Delete")]
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
                _logger.LogWarning("Failed to delete role: {@Role}", new { RoleId = id });
                return NotFound(new { message = "Role not found" });
            }

            string cacheKey = "Role-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_roles");

            _logger.LogInformation("Role deleted: {@Role}", new { RoleId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting role: {@Role}", new { RoleId = id });
            return StatusCode(500, new { message = "An error occurred deleting the role" });
        }
    }

    /// <summary>
    /// Gets all users assigned to the specified role.
    /// </summary>
    /// <param name="name">The name of the role whose members to retrieve.</param>
    /// <returns>A collection of <see cref="UserDto"/> objects representing users in the role.</returns>
    [HttpGet("{name}/users")]
    [HasPermission("Roles", "Read")]
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
            _logger.LogError(ex, "Error retrieving users in role: {@Role}", new { RoleName = name });
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    /// <summary>
    /// Assigns a single permission to a role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="permissionId">The unique identifier of the permission to assign.</param>
    /// <returns>204 No Content if successful, 404 if the role or permission is not found, or 409 if the assignment already exists.</returns>
    [HttpPost("{roleId}/permissions")]
    [HasPermission("Roles", "Update")]
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
                _logger.LogWarning(
                    "Failed to create role permission: {@Permission}",
                    new
                    {
                        RoleName = _logSanitizer.Sanitize(role.Name),
                        Module = _logSanitizer.Sanitize(permission.Module),
                        Action = _logSanitizer.Sanitize(permission.Action)
                    });
                return Conflict(new { message = "Role already exists" });
            }

            // Invalidate role cache
            string roleCacheKey = "Role-" + roleId;
            await _cacheService.RemoveStateAsync(roleCacheKey);
            await _cacheService.RemoveStateAsync("all_roles");
            await _cacheService.RemoveStateAsync("all_permissions");

            // Invalidate cache for all users that have this role
            if (!string.IsNullOrEmpty(role.Name))
            {
                var usersInRole = await _roleService.GetUsersInRoleAsync(role.Name);
                foreach (var user in usersInRole)
                {
                    string userCacheKey = "User-" + user.Id;
                    await _cacheService.RemoveStateAsync(userCacheKey);
                }
                string userRolesCacheKey = "Role-" + roleId;
                await _cacheService.RemoveStateAsync(userRolesCacheKey);
            }

            _logger.LogInformation(
                "Role permission created: {@Permission}",
                new
                {
                    RoleName = _logSanitizer.Sanitize(role.Name),
                    Module = _logSanitizer.Sanitize(permission.Module),
                    Action = _logSanitizer.Sanitize(permission.Action)
                });
            return NoContent();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permissions in role: {@Role}", new { RoleId = roleId });
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    /// <summary>
    /// Removes a permission from a role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="permissionId">The unique identifier of the permission to remove.</param>
    /// <returns>204 No Content if successful (or the assignment did not exist), 404 if the role is not found, or 500 on failure.</returns>
    [HttpDelete("{roleId}/permissions/{permissionId}")]
    [HasPermission("Roles", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
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

            // 2. Get role name before removing (needed for cache invalidation)
            var role = await _roleService.GetRoleByIdAsync(roleId);
            if (role is null)
            {
                return NotFound(new { message = "Role not found" });
            }

            // 3. Remove the Association
            var deleteDto = new DeleteRolePermissionDto(roleId, permissionId);
            var success = await _roleService.RemovePermissionFromRoleAsync(deleteDto);

            if (!success)
            {
                // This case should be rare but catches a DB/service failure
                _logger.LogError("Failed to remove role permission: {@Permission}", new { RoleId = roleId, PermissionId = permissionId });
                return StatusCode(500, new { message = "Failed to unassign permission due to an internal error." });
            }

            // 4. Invalidate Cache and Return Success (204 No Content)
            string roleCacheKey = "Role-" + roleId;
            await _cacheService.RemoveStateAsync(roleCacheKey);
            await _cacheService.RemoveStateAsync("all_roles");

            // Invalidate cache for all users that have this role
            if (!string.IsNullOrEmpty(role.Name))
            {
                var usersInRole = await _roleService.GetUsersInRoleAsync(role.Name);
                foreach (var user in usersInRole)
                {
                    string userCacheKey = "User-" + user.Id;
                    string userRolesCacheKey = "Roles-" + user.Id;
                    await _cacheService.RemoveStateAsync(userCacheKey);
                    await _cacheService.RemoveStateAsync(userRolesCacheKey);
                }
            }

            _logger.LogInformation("Permission removed from role: {@Permission}", new { PermissionId = permissionId, RoleId = roleId });

            return NoContent(); // 204 No Content is the standard for successful DELETE
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing permission from role: {@Role}", new { RoleId = roleId });
            return StatusCode(500, new { message = "An error occurred while unassigning the permission." });
        }
    }

    /// <summary>
    /// Gets all permissions assigned to the specified role.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role whose permissions to retrieve.</param>
    /// <returns>A collection of permission DTOs assigned to the role, or 404 if the role is not found.</returns>
    [HttpGet("{roleId}/permissions")]
    [HasPermission("Roles", "Read")]
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
            _logger.LogError(ex, "Error retrieving permissions for role: {@Role}", new { RoleId = roleId });
            return StatusCode(500, new { message = "An error occurred retrieving role permissions." });
        }
    }

    /// <summary>
    /// Assigns multiple permissions to a role in a single operation.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="permissionIds">The collection of permission identifiers to assign to the role.</param>
    /// <returns>204 No Content if all permissions were successfully assigned, 400 if no IDs are provided, or 404 if the role is not found.</returns>
    [HttpPost("{roleId}/permissions/bulk")]
    [HasPermission("Roles", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPermissionsToRole(Guid roleId, [FromBody] IEnumerable<Guid> permissionIds)
    {
        if (permissionIds == null || !permissionIds.Any())
            return BadRequest(new { message = "No permission IDs provided." });
        var role = await _roleService.GetRoleByIdAsync(roleId);
        if (role is null)
            return NotFound(new { message = "Role not found" });
        var createDto = new CreateRolePermissionsDto(roleId, permissionIds);
        var result = await _roleService.AddPermissionsToRole(createDto);
        if (!result)
        {
            _logger.LogWarning("Failed to add permissions to role: {@Role}", new { RoleId = roleId });
            return StatusCode(500, new { message = "Failed to add permissions to role." });
        }
        // Invalidate role cache
        string roleCacheKey = "Role-" + roleId;
        await _cacheService.RemoveStateAsync(roleCacheKey);
        await _cacheService.RemoveStateAsync("all_roles");
        await _cacheService.RemoveStateAsync("all_permissions");
        _logger.LogInformation("Bulk permissions added to role: {@Role}", new { RoleId = roleId, PermissionIds = permissionIds });
        return NoContent();
    }

    /// <summary>
    /// Removes multiple permissions from a role in a single operation.
    /// </summary>
    /// <param name="roleId">The unique identifier of the role to update.</param>
    /// <param name="permissionIds">The collection of permission identifiers to remove from the role.</param>
    /// <returns>204 No Content if all permissions were successfully removed, 400 if no IDs are provided, or 404 if the role is not found.</returns>
    [HttpDelete("{roleId}/permissions/bulk")]
    [HasPermission("Roles", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemovePermissionsFromRole(Guid roleId, [FromBody] IEnumerable<Guid> permissionIds)
    {
        if (permissionIds == null || !permissionIds.Any())
            return BadRequest(new { message = "No permission IDs provided." });
        var role = await _roleService.GetRoleByIdAsync(roleId);
        if (role is null)
            return NotFound(new { message = "Role not found" });
        var deleteDto = new DeleteRolePermissionsDto(roleId, permissionIds);
        var result = await _roleService.RemovePermissionsFromRoleAsync(deleteDto);
        if (!result)
        {
            _logger.LogWarning("Failed to remove permissions from role: {@Role}", new { RoleId = roleId });
            return StatusCode(500, new { message = "Failed to remove permissions from role." });
        }
        // Invalidate role cache
        string roleCacheKey = "Role-" + roleId;
        await _cacheService.RemoveStateAsync(roleCacheKey);
        await _cacheService.RemoveStateAsync("all_roles");
        await _cacheService.RemoveStateAsync("all_permissions");
        _logger.LogInformation("Bulk permissions removed from role: {@Role}", new { RoleId = roleId, PermissionIds = permissionIds });
        return NoContent();
    }
}
