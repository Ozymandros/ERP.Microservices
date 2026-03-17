using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Crm.Domain.Activities;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Crm.API.Controllers;

[ApiController]
[Authorize]
[Route("api/crm/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _service;
    private readonly ILogger<ActivitiesController> _logger;

    public ActivitiesController(IActivityService service, ILogger<ActivitiesController> logger)
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
                var spec = new ActivityQuerySpec(query);
                var result = await _service.QueryAsync(spec, cancellationToken);
                return Ok(result.ToPaginatedResponse(query.Page, query.PageSize));
            }

            var list = await _service.ListAsync(cancellationToken);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for activities");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound(new { message = $"Activity with ID {id} not found." }) : Ok(item);
    }

    [HttpPost]
    [HasPermission("CRM", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateActivityDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:guid}/complete")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteActivityDto dto, CancellationToken cancellationToken)
    {
        try
        {
            var updated = await _service.CompleteAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

