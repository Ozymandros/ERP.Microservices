using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Opportunities;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Crm.API.Controllers;

[ApiController]
[Authorize]
[Route("api/crm/opportunities")]
public class OpportunitiesController : ControllerBase
{
    private readonly IOpportunityService _service;
    private readonly ILogger<OpportunitiesController> _logger;

    public OpportunitiesController(IOpportunityService service, ILogger<OpportunitiesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetAll([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var spec = new OpportunityQuerySpec(query);
                var result = await _service.QueryAsync(spec, cancellationToken);
                return Ok(result.ToPaginatedResponse(query.Page, query.PageSize));
            }

            var list = await _service.ListAsync(cancellationToken);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for opportunities");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound(new { message = $"Opportunity with ID {id} not found." }) : Ok(item);
    }

    [HttpPost]
    [HasPermission("CRM", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateOpportunityDto dto, CancellationToken cancellationToken)
    {
        var created = await _service.CreateAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}/forecast")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> UpdateForecast(Guid id, [FromBody] UpdateOpportunityForecastDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.UpdateForecastAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/move-stage")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> MoveStage(Guid id, [FromBody] MoveOpportunityStageDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.MoveStageAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-won")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> MarkWon(Guid id, [FromBody] MarkOpportunityWonRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.MarkWonAsync(id, request, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Domain invariants (already closed, already converted, etc.)
            return Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/mark-lost")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> MarkLost(Guid id, [FromBody] MarkOpportunityLostDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.MarkLostAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

