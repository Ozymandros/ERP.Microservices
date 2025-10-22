using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Shared.Domain.Caching;

namespace MyApp.Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PermissionsController> _logger;

    public PermissionsController(
        IPermissionService permissionService,
        ICacheService cacheService,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Get all permissions
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetAll()
    {
        try
        {
            var permissions = await _cacheService.GetStateAsync<IEnumerable<PermissionDto>>("all_permissions");
            if (permissions != null)
            {
                _logger.LogInformation("Retrieved all permissions from cache");
                return Ok(permissions);
            }

            permissions = await _permissionService.GetAllPermissionsAsync();
            await _cacheService.SaveStateAsync("all_permissions", permissions);
            _logger.LogInformation("Retrieved all permissions from database and cached");

            return Ok(permissions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all permissions");
            return StatusCode(500, new { message = "An error occurred retrieving permissions" });
        }
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = $"Permission-{id}";
            var permission = await _cacheService.GetStateAsync<PermissionDto>(cacheKey);

            if (permission is not null)
            {
                _logger.LogInformation("Retrieved permission {PermissionId} from cache", id);
                return Ok(permission);
            }

            permission = await _permissionService.GetPermissionByIdAsync(id);
            if (permission is null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", id);
                return NotFound(new { message = "Permission not found" });
            }

            await _cacheService.SaveStateAsync(cacheKey, permission);
            _logger.LogInformation("Retrieved permission {PermissionId} from database and cached", id);

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission: {PermissionId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving the permission" });
        }
    }

    /// <summary>
    /// Get permission by module and action
    /// </summary>
    [HttpGet("module-action")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> GetByModuleAction(string module, string action)
    {
        try
        {
            string cacheKey = $"Permission-{module}-{action}";
            var permission = await _cacheService.GetStateAsync<PermissionDto>(cacheKey);

            if (permission is not null)
            {
                _logger.LogInformation("Retrieved permission {Module}:{Action} from cache", module, action);
                return Ok(permission);
            }

            permission = await _permissionService.GetPermissionByModuleActionAsync(module, action);
            if (permission is null)
            {
                _logger.LogWarning("Permission not found: {Module}:{Action}", module, action);
                return NotFound(new { message = "Permission not found" });
            }

            await _cacheService.SaveStateAsync(cacheKey, permission);
            _logger.LogInformation("Retrieved permission {Module}:{Action} from database and cached", module, action);

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission: {Module}:{Action}", module, action);
            return StatusCode(500, new { message = "An error occurred retrieving the permission" });
        }
    }

    /// <summary>
    /// Check if a user has a specific permission by username
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> CheckPermission(string? username, string module, string action)
    {
        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(username, module, action);
            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission for user: {Username}, module: {Module}, action: {Action}", username, module, action);
            return StatusCode(500, new { message = "An error occurred checking the permission" });
        }
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> Create([FromBody] CreatePermissionDto createPermissionDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _permissionService.CreatePermissionAsync(createPermissionDto);
            if (result == null)
            {
                _logger.LogWarning("Failed to create permission: {Module}:{Action}", createPermissionDto.Module, createPermissionDto.Action);
                return Conflict(new { message = "Permission already exists" });
            }

            await _cacheService.RemoveStateAsync("all_permissions");
            _logger.LogInformation("Permission created: {Module}:{Action}", createPermissionDto.Module, createPermissionDto.Action);

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission: {Module}:{Action}", createPermissionDto.Module, createPermissionDto.Action);
            return StatusCode(500, new { message = "An error occurred creating the permission" });
        }
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePermissionDto updatePermissionDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _permissionService.UpdatePermissionAsync(id, updatePermissionDto);
            if (!result)
            {
                _logger.LogWarning("Failed to update permission: {PermissionId}", id);
                return NotFound(new { message = "Permission not found" });
            }

            string cacheKey = $"Permission-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_permissions");

            _logger.LogInformation("Permission updated: {PermissionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {PermissionId}", id);
            return StatusCode(500, new { message = "An error occurred updating the permission" });
        }
    }

    /// <summary>
    /// Delete a permission
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _permissionService.DeletePermissionAsync(id);
            if (!result)
            {
                _logger.LogWarning("Failed to delete permission: {PermissionId}", id);
                return NotFound(new { message = "Permission not found" });
            }

            string cacheKey = $"Permission-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_permissions");

            _logger.LogInformation("Permission deleted: {PermissionId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {PermissionId}", id);
            return StatusCode(500, new { message = "An error occurred deleting the permission" });
        }
    }
}