using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

/// <summary>
/// Ai models controller.
/// </summary>
/// <param name="modelService">The model Service.</param>
/// <returns>The result of the operation.</returns>
[ApiController]
[Authorize]
[Route("api/agentic/models")]
public class AIModelsController(
    IAIModelService modelService) : ControllerBase
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AIModelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var models = await modelService.ListAsync(cancellationToken);
        return Ok(models);
    }

    /// <summary>
    /// Gets the provider.
    /// </summary>
    /// <param name="providerId">The provider Id.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpGet("by-provider/{providerId:guid}")]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AIModelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByProvider(Guid providerId, CancellationToken cancellationToken)
    {
        try
        {
            var models = await modelService.ListByProviderAsync(providerId, cancellationToken);
            return Ok(models);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
    [ProducesResponseType(typeof(AIModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var model = await modelService.GetByIdAsync(id, cancellationToken);
        return model is null
            ? NotFound(new { message = $"AI model with ID {id} not found." })
            : Ok(model);
    }

    /// <summary>
    /// Creates a new item.
    /// </summary>
    /// <param name="dto">The dto.</param>
    /// <param name="cancellationToken">The cancellation Token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the result.</returns>
    [HttpPost]
    [HasPermission("Agentic", "Create")]
    [ProducesResponseType(typeof(AIModelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateAIModelDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await modelService.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
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
    [ProducesResponseType(typeof(AIModelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAIModelDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await modelService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
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
        await modelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
