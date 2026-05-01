using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Leads;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Crm.API.Controllers;

/// <summary>
/// Provides Leads Controller functionality.
/// </summary>
[ApiController]
[Authorize]
[Route("api/crm/leads")]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _leadService;
    private readonly ILogger<LeadsController> _logger;

    /// <summary>I Logger.</summary>
    public LeadsController(ILeadService leadService, ILogger<LeadsController> logger)
    {
        _leadService = leadService;
        _logger = logger;
    }

    /// <summary>Get All.</summary>
    [HttpGet]
    [HasPermission("CRM", "Read")]
    [ProducesResponseType(typeof(IEnumerable<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<LeadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new LeadQuerySpec(query);
                var result = await _leadService.QueryAsync(spec, cancellationToken);
                return Ok(result);
            }

            var leads = await _leadService.ListAsync(cancellationToken);
            return Ok(leads);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for leads");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leads");
            return StatusCode(500, new { message = "An error occurred retrieving leads" });
        }
    }

    /// <summary>Get By Id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("CRM", "Read")]
    [ProducesResponseType(typeof(LeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var lead = await _leadService.GetByIdAsync(id, cancellationToken);
        return lead is null ? NotFound(new { message = $"Lead with ID {id} not found." }) : Ok(lead);
    }

    /// <summary>Create.</summary>
    [HttpPost]
    [HasPermission("CRM", "Create")]
    [ProducesResponseType(typeof(LeadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var created = await _leadService.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Update.</summary>
    [HttpPut("{id:guid}")]
    [HasPermission("CRM", "Update")]
    [ProducesResponseType(typeof(LeadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _leadService.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Qualify.</summary>
    [HttpPost("{id:guid}/qualify")]
    [HasPermission("CRM", "Update")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Qualify(Guid id, [FromBody] QualifyLeadDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await _leadService.QualifyAsync(id, dto, cancellationToken);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>Delete.</summary>
    [HttpDelete("{id:guid}")]
    [HasPermission("CRM", "Delete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _leadService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

