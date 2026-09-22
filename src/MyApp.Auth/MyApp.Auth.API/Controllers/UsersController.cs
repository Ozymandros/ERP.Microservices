using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.API.Authorization;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Shared.Infrastructure.Extensions;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Security;

using MyApp.Shared.Infrastructure.Export;

namespace MyApp.Auth.API.Controllers;

/// <summary>
/// Manages user CRUD operations, role assignments, and user profile queries.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AuthorizeJwt]
[Produces("application/json")]
public partial class UsersController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IUserService _userService;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UsersController"/> class.
    /// </summary>
    /// <param name="userService">The service used to manage users.</param>
    /// <param name="logger">The logger for this controller.</param>
    /// <param name="cacheService">The distributed cache service.</param>
    /// <param name="logSanitizer">The log sanitizer for masking sensitive values in log output.</param>
    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        ICacheService cacheService,
        ILogSanitizer logSanitizer)
    {
        _userService = userService;
        _logger = logger;
        _cacheService = cacheService;
        _logSanitizer = logSanitizer;
    }

    /// <summary>
    /// Gets all users, or executes a filtered/paginated query when query parameters are present.
    /// </summary>
    /// <param name="query">Optional query specification for filtering, sorting, and pagination.</param>
    /// <returns>A list of <see cref="UserDto"/> objects, or a paginated result when query parameters are provided.</returns>
    [HttpGet]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
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
                var spec = new ApplicationUserQuerySpec(query);
                var result = await _userService.QueryUsersAsync(spec);
                return Ok(result);
            }

            var users = await _cacheService.GetStateAsync<IEnumerable<UserDto>>("all_users");
            if (users != null)
            {
                return Ok(users);
            }

            users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    /// <summary>
    /// Exports all users as an XLSX spreadsheet file.
    /// </summary>
    /// <returns>An XLSX file containing all users.</returns>
    [HttpGet("export-xlsx")]
    [HasPermission("Users", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx()
    {
        try
        {
            var users = await _cacheService.GetStateAsync<IEnumerable<UserDto>>("all_users")
                        ?? await _userService.GetAllUsersAsync();
            var bytes = users.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Users.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to XLSX");
            return StatusCode(500, new { message = "An error occurred exporting users" });
        }
    }

    /// <summary>
    /// Exports all users as a PDF file.
    /// </summary>
    /// <returns>A PDF file containing all users.</returns>
    [HttpGet("export-pdf")]
    [HasPermission("Users", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf()
    {
        try
        {
            var users = await _cacheService.GetStateAsync<IEnumerable<UserDto>>("all_users")
                ?? await _userService.GetAllUsersAsync();
            var bytes = users.ExportToPdf();
            return File(bytes, "application/pdf", "Users.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to PDF");
            return StatusCode(500, new { message = "An error occurred exporting users" });
        }
    }

    /// <summary>
    /// Gets all users with explicit pagination.
    /// </summary>
    /// <param name="pageNumber">The 1-based page number to retrieve (default: 1).</param>
    /// <param name="pageSize">The number of items per page (default: 10).</param>
    /// <returns>A paginated result containing users for the requested page.</returns>
    [HttpGet("paginated")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<UserDto>>> GetAllPaginated([FromQuery(Name = "page")] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await _userService.GetAllUsersPaginatedAsync(pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paginated users");
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    /// <summary>
    /// Searches users with advanced filtering, sorting, and pagination.
    /// </summary>
    /// <param name="query">The query specification containing filter, sort, and pagination parameters.</param>
    /// <returns>A paginated result containing users matching the query criteria.</returns>
    /// <remarks>
    /// Supported filters: isActive, email, userName, isExternalLogin
    /// Supported sort fields: createdAt, email, userName, firstName, lastName
    /// </remarks>
    [HttpGet("search")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PaginatedResult<UserDto>>> Search([FromQuery] QuerySpec query)
    {
        try
        {
            // Bind filters from query parameters
            query.BindFiltersFromQuery(Request.Query);

            query.Validate();
            var spec = new ApplicationUserQuerySpec(query);
            var result = await _userService.QueryUsersAsync(spec);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching users");
            return StatusCode(500, new { message = "An error occurred searching users" });
        }
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="user">The data transfer object containing user creation details.</param>
    /// <returns>The created <see cref="UserDto"/>, or 400 if creation failed.</returns>
    [HttpPost("create")]
    [HasPermission("Users", "Create")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserDto user)
    {
        try
        {
            var result = await _userService.CreateUserAsync(user);
            if (result == null)
            {
                return BadRequest(new { message = "Failed to create user" });
            }
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, new { message = "An error occurred creating user" });
        }
    }

    /// <summary>
    /// Gets the profile of the currently authenticated user.
    /// </summary>
    /// <returns>The <see cref="UserDto"/> for the current user, or 404 if the identity cannot be resolved.</returns>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
            {
                _logger.LogWarning("Current user not found");
                return NotFound(new { message = "Current user not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user");
            return StatusCode(500, new { message = "An error occurred retrieving the current user" });
        }
    }

    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to retrieve.</param>
    /// <returns>The <see cref="UserDto"/> for the specified user, or 404 if not found.</returns>
    [HttpGet("{id}")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = "User-" + id;
            var user = await _cacheService.GetStateAsync<UserDto>(cacheKey); // 1. Try to get it from cache

            if (user is not null)
            {
                return Ok(user); // Return from cache
            }

            // 2. The data is NOT in cache, fetch it from the DB
            user = await _userService.GetUserByIdAsync(id);
            if (user is null)
            {
                return NotFound();
            }

            await _cacheService.SaveStateAsync<UserDto>(cacheKey, user);

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred retrieving the user" });
        }
    }

    /// <summary>
    /// Gets a user by their email address.
    /// </summary>
    /// <param name="email">The email address of the user to retrieve.</param>
    /// <returns>The <see cref="UserDto"/> for the specified user, or 404 if not found.</returns>
    [HttpGet("email/{email}")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetByEmail(string email)
    {
        try
        {
            var user = await _userService.GetUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning(
                    "User with email {@User} not found",
                    new { Email = _logSanitizer.Sanitize(email) });
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving user by email: {@User}",
                new { Email = _logSanitizer.Sanitize(email) });
            return StatusCode(500, new { message = "An error occurred retrieving the user" });
        }
    }

    /// <summary>
    /// Updates an existing user's profile information.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="updateUserDto">The data transfer object containing updated user details.</param>
    /// <returns>204 No Content if the update succeeded, or 404 if the user was not found.</returns>
    [HttpPut("{id}")]
    [HasPermission("Users", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto updateUserDto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _userService.UpdateUserAsync(id, updateUserDto);
            if (!result)
            {
                _logger.LogWarning("Failed to update user: {@User}", new { UserId = id });
                return NotFound(new { message = "User not found" });
            }

            string cacheKey = "User-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("User updated: {@User}", new { UserId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred updating the user" });
        }
    }

    /// <summary>
    /// Deletes a user by their unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the user to delete.</param>
    /// <returns>204 No Content if the deletion succeeded, or 404 if the user was not found.</returns>
    [HttpDelete("{id}")]
    [HasPermission("Users", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result)
            {
                _logger.LogWarning("Failed to delete user: {@User}", new { UserId = id });
                return NotFound(new { message = "User not found" });
            }

            string cacheKey = "User-" + id;
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_users");

            _logger.LogInformation("User deleted: {@User}", new { UserId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred deleting the user" });
        }
    }

    /// <summary>
    /// Assigns a role to a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="roleName">The name of the role to assign to the user.</param>
    /// <returns>204 No Content if the role was successfully assigned, or 404 if the user or role was not found.</returns>
    [HttpPost("{id}/roles/{roleName}")]
    [HasPermission("Users", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignRole(Guid id, string roleName)
    {
        try
        {
            var result = await _userService.AssignRoleAsync(id, roleName);
            if (!result)
            {
                _logger.LogWarning("Failed to assign role {@Role} to user {@User}", new { RoleName = roleName }, new { UserId = id });
                return NotFound(new { message = "User or role not found" });
            }

            // Invalidate user cache
            string userCacheKey = "User-" + id;
            await _cacheService.RemoveStateAsync(userCacheKey);

            _logger.LogInformation("Role {@Role} assigned to user {@User}", new { RoleName = roleName }, new { UserId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred assigning the role" });
        }
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    /// <param name="id">The unique identifier of the user to update.</param>
    /// <param name="roleName">The name of the role to remove from the user.</param>
    /// <returns>204 No Content if the role was successfully removed, or 404 if the user or role was not found.</returns>
    [HttpDelete("{id}/roles/{roleName}")]
    [HasPermission("Users", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveRole(Guid id, string roleName)
    {
        try
        {
            var result = await _userService.RemoveRoleAsync(id, roleName);
            if (!result)
            {
                _logger.LogWarning("Failed to remove role {@Role} from user {@User}", new { RoleName = roleName }, new { UserId = id });
                return NotFound(new { message = "User or role not found" });
            }

            // Invalidate user cache
            string userCacheKey = "User-" + id;
            await _cacheService.RemoveStateAsync(userCacheKey);

            _logger.LogInformation("Role {@Role} removed from user {@User}", new { RoleName = roleName }, new { UserId = id });
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred removing the role" });
        }
    }

    /// <summary>
    /// Gets all roles assigned to the specified user.
    /// </summary>
    /// <param name="id">The unique identifier of the user whose roles to retrieve.</param>
    /// <returns>A collection of <see cref="RoleDto"/> objects assigned to the user.</returns>
    [HttpGet("{id}/roles")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles(Guid id)
    {
        try
        {
            string cacheKey = "Roles-" + id;
            var roles = await _cacheService.GetStateAsync<IEnumerable<RoleDto>>(cacheKey); // 1. Try to get it from cache

            if (roles is not null)
            {
                return Ok(roles); // Return from cache
            }

            // 2. The data is NOT in cache, fetch it from the DB
            roles = await _userService.GetUserRolesAsync(id);
            await _cacheService.SaveStateAsync(cacheKey, roles);

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving roles for user: {@User}", new { UserId = id });
            return StatusCode(500, new { message = "An error occurred retrieving roles" });
        }
    }
}
