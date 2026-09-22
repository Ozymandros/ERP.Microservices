using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

/// <summary>API controller for managing AI agents (CRUD operations).</summary>
[ApiController]
[Authorize]
[Route("api/agentic/agents")]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentsController> _logger;

    /// <summary>
    /// Initializes a new instance of the AgentsController class.
    /// </summary>
    /// <param name="agentService">The agent Service.</param>
    /// <param name="logger">The logger.</param>
    public AgentsController(IAgentService agentService, ILogger<AgentsController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AgentListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var agents = await _agentService.ListAsync(cancellationToken);
            return Ok(agents);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agents");
            return StatusCode(500, new { message = "An error occurred retrieving agents" });
        }
    }

    /// <summary>
    /// Gets an item by its unique identifier.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(AgentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var agent = await _agentService.GetByIdAsync(id, cancellationToken);
        return agent is null ? NotFound(new { message = $"Agent with ID {id} not found." }) : Ok(agent);
    }

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPost]
    [HasPermission("Agentic", "Create")]
    [ProducesResponseType(typeof(AgentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAgentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _agentService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>
    /// Updates an existing item.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPut("{id:guid}")]
    [HasPermission("Agentic", "Update")]
    [ProducesResponseType(typeof(AgentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAgentDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _agentService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an item.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpDelete("{id:guid}")]
    [HasPermission("Agentic", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _agentService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}