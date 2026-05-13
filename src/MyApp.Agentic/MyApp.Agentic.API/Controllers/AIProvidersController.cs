using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Agentic.Application.Contracts.DTOs;
using MyApp.Agentic.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Agentic.API.Controllers;

[ApiController]
[Authorize]
[Route("api/agentic/providers")]
public class AIProvidersController(
    IAIProviderService providerService) : ControllerBase
{
    [HttpGet]
    [HasPermission("Agentic", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AIProviderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var providers = await providerService.ListAsync(cancellationToken);
        return Ok(providers);
    }

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

    [HttpPost]
    [HasPermission("Agentic", "Create")]
    [ProducesResponseType(typeof(AIProviderDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAIProviderDto dto, CancellationToken cancellationToken)
    {
        var created = await providerService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

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

    [HttpDelete("{id:guid}")]
    [HasPermission("Agentic", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await providerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
