using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using System.Security.Claims;

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
    [HasPermission("Permissions", "Read")]
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
    /// Get all permissions with pagination
    /// </summary>
    [HttpGet("paginated")]
    [HasPermission("Permissions", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<PermissionDto>>> GetAllPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _permissionService.GetAllPermissionsPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated permissions");
            return StatusCode(500, new { message = "An error occurred retrieving permissions" });
        }
    }

    /// <summary>
    /// Search permissions with advanced filtering, sorting, and pagination
    /// </summary>
    /// <remarks>
    /// Supported filters: resource, action, description
    /// Supported sort fields: id, resource, action, createdAt
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Permissions", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<PermissionDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            query.Validate();
            var spec = new PermissionQuerySpec(query);
            var result = await _permissionService.QueryPermissionsAsync(spec);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching permissions");
            return StatusCode(500, new { message = "An error occurred searching permissions" });
        }
    }

    /// <summary>
    /// Get permission by ID
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Permissions", "Read")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = "Permission-" + id;
            var permission = await _cacheService.GetStateAsync<PermissionDto>(cacheKey);

            if (permission is not null)
            {
                _logger.LogInformation("Retrieved permission {@Permission} from cache", new { PermissionId = id });
                return Ok(permission);
            }

            permission = await _permissionService.GetPermissionByIdAsync(id);
            if (permission is null)
            {
                _logger.LogWarning("Permission with ID {@Permission} not found", new { PermissionId = id });
                return NotFound(new { message = "Permission not found" });
            }

            await _cacheService.SaveStateAsync(cacheKey, permission);
            _logger.LogInformation("Retrieved permission {@Permission} from database and cached", new { PermissionId = id });

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission: {@Permission}", new { PermissionId = id });
            return StatusCode(500, new { message = "An error occurred retrieving the permission" });
        }
    }

    /// <summary>
    /// Get permission by module and action
    /// </summary>
    [HttpGet("module-action")]
    [HasPermission("Permissions", "Read")]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PermissionDto>> GetByModuleAction(string module, string action)
    {
        try
        {
            string cacheKey = "Permission-" + module + "-" + action;
            var permission = await _cacheService.GetStateAsync<PermissionDto>(cacheKey);

            if (permission is not null)
            {
                _logger.LogInformation("Retrieved permission by module/action {@Permission} from cache", new { Module = module, Action = action });
                return Ok(permission);
            }

            permission = await _permissionService.GetPermissionByModuleActionAsync(module, action);
            if (permission is null)
            {
                _logger.LogWarning("Permission with module/action {@Permission} not found", new { Module = module, Action = action });
                return NotFound(new { message = "Permission not found" });
            }

            await _cacheService.SaveStateAsync(cacheKey, permission);
            _logger.LogInformation("Retrieved permission by module/action {@Permission} from database and cached", new { Module = module, Action = action });

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving permission by module/action: {@Permission}", new { Module = module, Action = action });
            return StatusCode(500, new { message = "An error occurred retrieving the permission" });
        }
    }

    /// <summary>
    /// Check if a user has a specific permission by username
    /// </summary>
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> CheckPermission(string module, string action)
    {
        var user = this.HttpContext.User;
        var username = user.Identity?.Name;
        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(username, module, action);
            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking permission: {@Permission}", new { Username = username, Module = module, Action = action });
            return StatusCode(500, new { message = "An error occurred checking the permission" });
        }
    }

    /// <summary>
    /// Create a new permission
    /// </summary>
    [HttpPost]
    [HasPermission("Permissions", "Create")]
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
                _logger.LogWarning("Failed to create permission: {@Permission}", new { Module = createPermissionDto.Module, Action = createPermissionDto.Action });
                return Conflict(new { message = "Permission already exists" });
            }

            await _cacheService.RemoveStateAsync("all_permissions");
            _logger.LogInformation("Permission created: {@Permission}", new { Module = createPermissionDto.Module, Action = createPermissionDto.Action });

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission: {@Permission}", new { Module = createPermissionDto.Module, Action = createPermissionDto.Action });
            return StatusCode(500, new { message = "An error occurred creating the permission" });
        }
    }

    /// <summary>
    /// Update an existing permission
    /// </summary>
    [HttpPut("{id}")]
    [HasPermission("Permissions", "Update")]
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
                _logger.LogWarning("Failed to update permission: {@Permission}", new { PermissionId = id });
                return NotFound(new { message = "Permission not found" });
            }

            string cacheKey = "Permission-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_permissions");

            _logger.LogInformation("Permission updated: {@Permission}", new { PermissionId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {@Permission}", new { PermissionId = id });
            return StatusCode(500, new { message = "An error occurred updating the permission" });
        }
    }

    /// <summary>
    /// Delete a permission
    /// </summary>
    [HttpDelete("{id}")]
    [HasPermission("Permissions", "Delete")]
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
                _logger.LogWarning("Failed to delete permission: {@Permission}", new { PermissionId = id });
                return NotFound(new { message = "Permission not found" });
            }

            string cacheKey = "Permission-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_permissions");

            _logger.LogInformation("Permission deleted: {@Permission}", new { PermissionId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {@Permission}", new { PermissionId = id });
            return StatusCode(500, new { message = "An error occurred deleting the permission" });
        }
    }
}