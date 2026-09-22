using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

/// <summary>API controller for managing persistent agent conversation sessions and their messages.</summary>
[ApiController]
[Authorize]
[Route("api/agentic/sessions")]
public class SessionsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<SessionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the SessionsController class.
    /// </summary>
    /// <param name="agentService">The agent Service.</param>
    /// <param name="logger">The logger.</param>
    public SessionsController(IAgentService agentService, ILogger<SessionsController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a session.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPost]
    [HasPermission("Agentic", "Execute")]
    [ProducesResponseType(typeof(StartSessionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSession([FromBody] StartSessionRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetAuthenticatedUserId();

        try
        {
            var response = await _agentService.StartSessionAsync(request, userId, cancellationToken);
            return CreatedAtAction(nameof(GetSession), new { id = response.SessionId }, response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating session");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return StatusCode(500, new { message = "An error occurred creating the session" });
        }
    }

    /// <summary>
    /// Lists sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<SessionListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSessions(CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        try
        {
            var sessions = await _agentService.ListSessionsAsync(userId, cancellationToken);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing sessions");
            return StatusCode(500, new { message = "An error occurred listing sessions" });
        }
    }

    /// <summary>
    /// Gets the session.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(SessionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        try
        {
            var session = await _agentService.GetSessionAsync(id, userId, cancellationToken);
            return session is null ? NotFound(new { message = $"Session with ID {id} not found." }) : Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving the session" });
        }
    }

    /// <summary>
    /// Gets the session messages.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet("{id:guid}/messages")]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(SessionDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSessionMessages(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        try
        {
            var session = await _agentService.GetSessionAsync(id, userId, cancellationToken);
            return session is null ? NotFound(new { message = $"Session with ID {id} not found." }) : Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving messages for session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred retrieving the messages" });
        }
    }

    /// <summary>
    /// Sends the message.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="request">The request.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPost("{id:guid}/messages")]
    [HasPermission("Agentic", "Execute")]
    [ProducesResponseType(typeof(SendMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SendMessage(Guid id, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = GetAuthenticatedUserId();

        try
        {
            var response = await _agentService.SendMessageAsync(id, request, userId, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation sending message to session {SessionId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred sending the message" });
        }
    }

    /// <summary>
    /// Ends the session.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpDelete("{id:guid}")]
    [HasPermission("Agentic", "Execute")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndSession(Guid id, CancellationToken cancellationToken)
    {
        var userId = GetAuthenticatedUserId();

        try
        {
            await _agentService.EndSessionAsync(id, userId, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation ending session {SessionId}", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending session {SessionId}", id);
            return StatusCode(500, new { message = "An error occurred ending the session" });
        }
    }

    private string GetAuthenticatedUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? User.FindFirstValue("sub")
        ?? throw new UnauthorizedAccessException("User identifier not found in token.");
}
