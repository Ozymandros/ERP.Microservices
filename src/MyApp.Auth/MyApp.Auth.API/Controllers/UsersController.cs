using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts.DTOs;
using MyApp.Auth.Application.Contracts.Services;
using MyApp.Auth.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public partial class UsersController : ControllerBase
{
    private readonly ICacheService _cacheService;
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger, ICacheService cacheService)
    {
        _userService = userService;
        _logger = logger;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Get all users
    /// </summary>
    [HttpGet]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
    {
        try
        {
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
            _logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { message = "An error occurred retrieving users" });
        }
    }

    /// <summary>
    /// Get all users with pagination
    /// </summary>
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
    /// Search users with advanced filtering, sorting, and pagination
    /// </summary>
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
    /// Create a new user
    /// </summary>
    /// <param name="user">User to create</param>
    /// <returns>Created user</returns>
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
    /// Get current user
    /// </summary>
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
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetById(Guid id)
    {
        try
        {
            string cacheKey = $"User-{id}";
            var user = await _cacheService.GetStateAsync<UserDto>(cacheKey); // 1. Intentar obtenir de la cache

            if (user is not null)
            {
                return Ok(user); // Retorna des de la cache
            }

            // 2. La dada NO �s a la cache, obtenir de la DB
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
            _logger.LogError(ex, "Error retrieving user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving the user" });
        }
    }

    /// <summary>
    /// Get user by email
    /// </summary>
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
                _logger.LogWarning("User not found by email: {Email}", email);
                return NotFound(new { message = "User not found" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user by email: {Email}", email);
            return StatusCode(500, new { message = "An error occurred retrieving the user" });
        }
    }

    /// <summary>
    /// Update user
    /// </summary>
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
                _logger.LogWarning("Failed to update user: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            string cacheKey = $"User-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("User updated: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred updating the user" });
        }
    }

    /// <summary>
    /// Delete user
    /// </summary>
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
                _logger.LogWarning("Failed to delete user: {UserId}", id);
                return NotFound(new { message = "User not found" });
            }

            string cacheKey = $"User-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);
            await _cacheService.RemoveStateAsync("all_users");

            _logger.LogInformation("User deleted: {UserId}", id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred deleting the user" });
        }
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
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
                _logger.LogWarning("Failed to assign role to user: {UserId}, Role: {RoleName}", id, roleName);
                return NotFound(new { message = "User or role not found" });
            }
            string cacheKey = $"User-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("Role assigned to user: {UserId}, Role: {RoleName}", id, roleName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred assigning the role" });
        }
    }

    /// <summary>
    /// Remove role from user
    /// </summary>
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
                _logger.LogWarning("Failed to remove role from user: {UserId}, Role: {RoleName}", id, roleName);
                return NotFound(new { message = "User or role not found" });
            }
            string cacheKey = $"User-{id}";
            await _cacheService.RemoveStateAsync(cacheKey);

            _logger.LogInformation("Role removed from user: {UserId}, Role: {RoleName}", id, roleName);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred removing the role" });
        }
    }

    /// <summary>
    /// Get user roles
    /// </summary>
    [HttpGet("{id}/roles")]
    [HasPermission("Users", "Read")]
    [ProducesResponseType(typeof(IEnumerable<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles(Guid id)
    {
        try
        {
            string cacheKey = $"Roles-{id}";
            var roles = await _cacheService.GetStateAsync<IEnumerable<RoleDto>>(cacheKey); // 1. Intentar obtenir de la cache

            if (roles is not null)
            {
                return Ok(roles); // Retorna des de la cache
            }

            // 2. La dada NO �s a la cache, obtenir de la DB
            roles = await _userService.GetUserRolesAsync(id);
            await _cacheService.SaveStateAsync(cacheKey, roles);

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles: {UserId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving roles" });
        }
    }
}
