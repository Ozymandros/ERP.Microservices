using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

/// <summary>
/// Ai providers controller.
/// </summary>
/// <param name="providerService">The provider Service.</param>
/// <returns>The result of the operation.</returns>
[ApiController]
[Authorize]
[Route("api/agentic/providers")]
public class AIProvidersController(
    IAIProviderService providerService) : ControllerBase
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AIProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var providers = await providerService.ListAsync(cancellationToken);
        return Ok(providers);
    }

    /// <summary>
    /// Gets an item by its unique identifier.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet("{id:guid}")]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(AIProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var provider = await providerService.GetByIdAsync(id, cancellationToken);
        return provider is null
            ? NotFound(new { message = $"AI provider with ID {id} not found." })
            : Ok(provider);
    }

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPost]
    [HasPermission("Agentic", "Create")]
    [ProducesResponseType(typeof(AIProviderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAIProviderDto dto, CancellationToken cancellationToken)
    {
        var created = await providerService.CreateAsync(dto, cancellationToken);
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
    [ProducesResponseType(typeof(AIProviderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAIProviderDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await providerService.UpdateAsync(id, dto, cancellationToken);
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
        await providerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
