using Microsoft.AspNetCore.Authorization;
using MyApp.Auth.API.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Domain.Specifications;
using MyApp.Auth.Infrastructure.Services;
using MyApp.Shared.Domain.Authentication;
using System.Security.Claims;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Security;
using MyApp.Shared.Infrastructure.Export;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Auth.API.Controllers;

/// <summary>
/// Manages permissions — CRUD operations, paginated listing, search, export, and per-user permission checks.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeJwt]
[Produces("application/json")]
public class PermissionsController : ControllerBase
{
    private readonly IPermissionService _permissionService;
    private readonly ICacheService _cacheService;
    private readonly IJwtTokenProvider _jwtTokenProvider;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<PermissionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the PermissionsController class.
    /// </summary>
    /// <param name="permissionService">The permission Service.</param>
    /// <param name="cacheService">The cache Service.</param>
    /// <param name="jwtTokenProvider">The jwt Token Provider.</param>
    /// <param name="logSanitizer">The log Sanitizer.</param>
    /// <param name="logger">The logger.</param>
    public PermissionsController(
        IPermissionService permissionService,
        ICacheService cacheService,
        IJwtTokenProvider jwtTokenProvider,
        ILogSanitizer logSanitizer,
        ILogger<PermissionsController> logger)
    {
        _permissionService = permissionService;
        _cacheService = cacheService;
        _jwtTokenProvider = jwtTokenProvider;
        _logSanitizer = logSanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Exports all permissions as an XLSX spreadsheet file.
    /// </summary>
    /// <returns>An XLSX file containing all permissions.</returns>
    [HttpGet("export-xlsx")]
    [HasPermission("Permissions", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var permissions = await _cacheService.GetStateAsync<IEnumerable<PermissionDto>>("all_permissions")
                ?? await _permissionService.GetAllPermissionsAsync();
            var bytes = permissions.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Permissions.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting permissions to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting permissions" });
        }
    }

    /// <summary>
    /// Exports all permissions as a PDF file.
    /// </summary>
    /// <returns>A PDF file containing all permissions.</returns>
    [HttpGet("export-pdf")]
    [HasPermission("Permissions", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var permissions = await _cacheService.GetStateAsync<IEnumerable<PermissionDto>>("all_permissions")
                ?? await _permissionService.GetAllPermissionsAsync();
            var bytes = permissions.ExportToPdf();
            return File(bytes, "application/pdf", "Permissions.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting permissions to PDF");
            return StatusCode(500, new { message = "An error occurred exporting permissions" });
        }
    }

    /// <summary>
    /// Retrieves all permissions. If query parameters are present, applies filtering, sorting, and pagination.
    /// </summary>
    /// <param name="query">Optional query specification for filtering, sorting, and pagination.</param>
    /// <returns>All permissions as a flat list or as a paginated result when query parameters are provided.</returns>
    [HttpGet]
    [HasPermission("Permissions", "Read")]
    [ProducesResponseType(typeof(IEnumerable<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> GetAll([FromQuery] QuerySpec query)
    {
        try
        {
            // If query parameters are provided, perform a search/paginated query
            if (Request.Query.Count != 0)
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new PermissionQuerySpec(query);
                var result = await _permissionService.QueryPermissionsAsync(spec);
                return Ok(result);
            }

            // Otherwise, return all permissions from cache if available
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
            _logger.LogError(ex, "Error retrieving permissions");
            return StatusCode(500, new { message = "An error occurred retrieving permissions" });
        }
    }

    /// <summary>
    /// Retrieves permissions with explicit pagination.
    /// </summary>
    /// <param name="pageNumber">The one-based page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of permissions per page (default: 10).</param>
    /// <returns>A paginated result containing <see cref="PermissionDto"/> objects for the requested page.</returns>
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
    /// Searches permissions using advanced filtering, sorting, and pagination via a query specification.
    /// </summary>
    /// <param name="query">The query specification with filters, sort options, and pagination settings.</param>
    /// <returns>A paginated result containing matching <see cref="PermissionDto"/> objects.</returns>
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
            // Bind filters from query parameters
            query.BindFiltersFromQuery(Request.Query);

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
    /// Retrieves a permission by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the permission.</param>
    /// <returns>The <see cref="PermissionDto"/> if found, or 404 if not found.</returns>
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
    /// Retrieves a permission by its module and action combination.
    /// </summary>
    /// <param name="module">The module name to search for.</param>
    /// <param name="action">The action name to search for.</param>
    /// <returns>The matching <see cref="PermissionDto"/> if found, or 404 if not found.</returns>
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
                _logger.LogInformation(
                    "Retrieved permission by module/action {@Permission} from cache",
                    new { Module = _logSanitizer.Sanitize(module), Action = _logSanitizer.Sanitize(action) });
                return Ok(permission);
            }

            permission = await _permissionService.GetPermissionByModuleActionAsync(module, action);
            if (permission is null)
            {
                _logger.LogWarning(
                    "Permission with module/action {@Permission} not found",
                    new { Module = _logSanitizer.Sanitize(module), Action = _logSanitizer.Sanitize(action) });
                return NotFound(new { message = "Permission not found" });
            }

            await _cacheService.SaveStateAsync(cacheKey, permission);
            _logger.LogInformation(
                "Retrieved permission by module/action {@Permission} from database and cached",
                new { Module = _logSanitizer.Sanitize(module), Action = _logSanitizer.Sanitize(action) });

            return Ok(permission);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving permission by module/action: {@Permission}",
                new { Module = _logSanitizer.Sanitize(module), Action = _logSanitizer.Sanitize(action) });
            return StatusCode(500, new { message = "An error occurred retrieving the permission" });
        }
    }

    /// <summary>
    /// Checks whether a user has a specific permission. Used by other services via Dapr.
    /// Allows anonymous so the Bearer token is validated in-action for service-to-service calls.
    /// </summary>
    /// <param name="module">The module name to check.</param>
    /// <param name="action">The action name to check.</param>
    /// <param name="userId">Optional user ID. If supplied, it must match the token subject.</param>
    /// <returns><c>true</c> if the resolved user has the permission; otherwise, <c>false</c>.</returns>
    [HttpGet("check")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<bool>> CheckPermission(
        string module,
        string action,
        [FromQuery] Guid? userId = null)
    {
        var effectiveUserId = ResolveUserIdFromRequest(userId);
        if (!effectiveUserId.HasValue)
            return Unauthorized();

        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(
                effectiveUserId.Value, module, action);
            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking permission: {@Permission}",
                new
                {
                    UserId = effectiveUserId,
                    Module = _logSanitizer.Sanitize(module),
                    Action = _logSanitizer.Sanitize(action)
                });
            return StatusCode(500, new { message = "An error occurred checking the permission" });
        }
    }

    /// <summary>
    /// Resolves the caller's user ID from the authenticated principal or Bearer token.
    /// When <paramref name="queryUserId"/> is supplied (e.g. from a Dapr caller), it must match the token subject.
    /// </summary>
    /// <param name="queryUserId">An optional user ID from the query string to validate against the token.</param>
    /// <returns>The resolved user ID, or <c>null</c> if authentication fails or there is a mismatch.</returns>
    private Guid? ResolveUserIdFromRequest(Guid? queryUserId)
    {
        var principal = User.Identity?.IsAuthenticated == true
            ? User
            : TryGetPrincipalFromBearerHeader();

        var tokenUserId = GetUserIdFromPrincipal(principal);
        if (!tokenUserId.HasValue)
            return null;

        if (queryUserId.HasValue && queryUserId.Value != tokenUserId.Value)
        {
            _logger.LogWarning(
                "Permission check userId mismatch: query {QueryUserId} vs token {TokenUserId}",
                _logSanitizer.Sanitize(queryUserId.Value.ToString()),
                _logSanitizer.Sanitize(tokenUserId.Value.ToString()));
            return null;
        }

        return queryUserId ?? tokenUserId;
    }

    /// <summary>
    /// Attempts to extract and validate a <see cref="ClaimsPrincipal"/> from the Authorization Bearer header of the current request.
    /// </summary>
    /// <returns>The validated <see cref="ClaimsPrincipal"/>, or <c>null</c> if no valid token is found.</returns>
    private ClaimsPrincipal? TryGetPrincipalFromBearerHeader()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return null;

        var token = BearerTokenHelper.ExtractToken(authHeader);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        return _jwtTokenProvider.ValidateAccessToken(token);
    }

    /// <summary>
    /// Extracts the user ID from the NameIdentifier or sub claim of the given principal.
    /// </summary>
    /// <param name="principal">The claims principal to extract the user ID from.</param>
    /// <returns>The user ID as a <see cref="Guid"/> if found and parseable; otherwise, <c>null</c>.</returns>
    private static Guid? GetUserIdFromPrincipal(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return null;

        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal.FindFirst("sub")?.Value;

        return Guid.TryParse(id, out var userId) ? userId : null;
    }

    /// <summary>
    /// Creates a new permission.
    /// </summary>
    /// <param name="createPermissionDto">The data for the new permission including module and action.</param>
    /// <returns>The created <see cref="PermissionDto"/> with its new ID, or 409 if it already exists.</returns>
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
                _logger.LogWarning(
                    "Failed to create permission: {@Permission}",
                    new
                    {
                        Module = _logSanitizer.Sanitize(createPermissionDto.Module),
                        Action = _logSanitizer.Sanitize(createPermissionDto.Action)
                    });
                return Conflict(new { message = "Permission already exists" });
            }

            await _cacheService.RemoveStateAsync("all_permissions");
            _logger.LogInformation(
                "Permission created: {@Permission}",
                new
                {
                    Module = _logSanitizer.Sanitize(createPermissionDto.Module),
                    Action = _logSanitizer.Sanitize(createPermissionDto.Action)
                });

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating permission: {@Permission}",
                new
                {
                    Module = _logSanitizer.Sanitize(createPermissionDto.Module),
                    Action = _logSanitizer.Sanitize(createPermissionDto.Action)
                });
            return StatusCode(500, new { message = "An error occurred creating the permission" });
        }
    }

    /// <summary>
    /// Updates an existing permission's module, action, and description.
    /// </summary>
    /// <param name="id">The unique identifier of the permission to update.</param>
    /// <param name="updatePermissionDto">The updated permission data.</param>
    /// <returns>204 No Content on success, or 404 if the permission is not found.</returns>
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
    /// Deletes a permission by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the permission to delete.</param>
    /// <returns>204 No Content on success, or 404 if the permission is not found.</returns>
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