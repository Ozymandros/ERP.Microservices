using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Auth.Application.Contracts;
using MyApp.Shared.Domain.Constants;

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
    private readonly ILogger<InternalPermissionsController> _logger;

    public InternalPermissionsController(
        IPermissionService permissionService,
        ILogger<InternalPermissionsController> logger)
    {
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <summary>
    /// Check permission for a user id (invoked by other microservices via Dapr).
    /// </summary>
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
                Request.Headers.TryGetValue("dapr-caller-app-id", out var c) ? c.ToString() : "(missing)");
            return Unauthorized();
        }

        try
        {
            var hasPermission = await _permissionService.HasPermissionAsync(userId, module, action);
            return Ok(hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Internal permission check failed for {UserId} {Module} {Action}", userId, module, action);
            return StatusCode(500, new { message = "An error occurred checking the permission" });
        }
    }

    private bool IsTrustedDaprCaller()
    {
        if (!Request.Headers.TryGetValue("dapr-caller-app-id", out var caller))
            return false;

        return TrustedCallerAppIds.Contains(caller.ToString());
    }
}
