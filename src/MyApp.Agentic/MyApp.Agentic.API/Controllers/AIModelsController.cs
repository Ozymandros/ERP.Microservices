using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/agentic/models")]
public class AIModelsController(
    IAIModelService modelService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AIModelDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var models = await modelService.ListAsync(cancellationToken);
        return Ok(models);
    }

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

    [HttpDelete("{id:guid}")]
    [HasPermission("Agentic", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await modelService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
