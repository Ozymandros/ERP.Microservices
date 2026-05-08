using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/agentic/messages")]
public class MessagesController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<MessagesController> _logger;

    public MessagesController(IAgentService agentService, ILogger<MessagesController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    [HttpPost]
    [HasPermission("Agentic", "Execute")]
    [ProducesResponseType(typeof(ProcessAgentMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ProcessMessage([FromBody] ProcessAgentMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User identifier not found in token.");

        try
        {
            var response = await _agentService.ProcessMessageAsync(request, userId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation processing message for Agent {AgentId}", request.AgentId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for Agent {AgentId}", request.AgentId);
            return StatusCode(500, new { message = "An error occurred processing the message" });
        }
    }
}