using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Security;

namespace MyApp.Auth.API.Controllers;

/// <summary>
/// Service-to-service permission checks (Dapr). No class-level JWT required —
/// caller is identified via trusted Dapr app-id and userId query parameter.
/// </summary>
[ApiController]
[Route("api/internal/permissions")]
[AllowAnonymous]
[Produces("application/json")]
public class InternalPermissionsController : ControllerBase
{
    private static readonly HashSet<string> TrustedCallerAppIds = new(StringComparer.OrdinalIgnoreCase)
    {
        ServiceNames.Sales,
        ServiceNames.Orders,
        ServiceNames.Inventory,
        ServiceNames.Purchasing,
        ServiceNames.Billing,
        ServiceNames.Crm,
        ServiceNames.Audit,
    };

    private readonly IPermissionService _permissionService;
    private readonly ILogSanitizer _logSanitizer;
    private readonly ILogger<InternalPermissionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the InternalPermissionsController class.
    /// </summary>
    /// <param name="permissionService">The permission Service.</param>
    /// <param name="logSanitizer">The log Sanitizer.</param>
    /// <param name="logger">The logger.</param>
    public InternalPermissionsController(
        IPermissionService permissionService,
        ILogSanitizer logSanitizer,
        ILogger<InternalPermissionsController> logger)
    {
        _permissionService = permissionService;
        _logSanitizer = logSanitizer;
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the specified user has a permission for the given module and action. Invoked by trusted microservices via Dapr.
    /// </summary>
    /// <param name="userId">The user Id.</param>
    /// <param name="module">The module.</param>
    /// <param name="action">The action.</param>
    /// <returns><c>true</c> if the user has the permission; otherwise, <c>false</c>.</returns>
    [HttpGet("check")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<bool>> Check(
        [FromQuery] Guid userId,
        [FromQuery] string module,
        [FromQuery] string action)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(module) || string.IsNullOrWhiteSpace(action))
            return BadRequest();

        if (!IsTrustedDaprCaller())
        {
            _logger.LogWarning(
                "Rejected internal permission check from untrusted caller {Caller}",
                _logSanitizer.Sanitize(
                    Request.Headers.TryGetValue("dapr-caller-app-id", out var c) ? c.ToString() : "(missing)"));
            return Unauthorized();
        }

        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(userId, module, action);
            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Internal permission check failed for {UserId} {Module} {Action}",
                userId,
                _logSanitizer.Sanitize(module),
                _logSanitizer.Sanitize(action));
            return StatusCode(500, new { message = "An error occurred checking the permission" });
        }
    }

    /// <summary>
    /// Determines whether the current request originated from a trusted Dapr microservice caller.
    /// </summary>
    /// <returns><c>true</c> if the caller's app-id is in the trusted list; otherwise, <c>false</c>.</returns>
    private bool IsTrustedDaprCaller()
    {
        if (!Request.Headers.TryGetValue("dapr-caller-app-id", out var caller))
            return false;

        return TrustedCallerAppIds.Contains(caller.ToString());
    }
}
