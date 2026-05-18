using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Audit.Application.Contracts.DTOs;
using MyApp.Audit.Application.Contracts.Services;
using MyApp.Audit.Domain.Specifications;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Audit.API.Controllers;

/// <summary>HTTP API for querying and recording entity audit trail entries.</summary>
[ApiController]
[Authorize]
[Route("api/audit/entity-changes")]
[Produces("application/json")]
public class EntityChangesController : ControllerBase
{
    private readonly IEntityChangeService _service;
    private readonly ILogger<EntityChangesController> _logger;

    public EntityChangesController(IEntityChangeService service, ILogger<EntityChangesController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Returns paginated entity changes when query parameters are supplied.</summary>
    [HttpGet]
    [HasPermission("Audit", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<EntityChangeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Query([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            if (!Request.Query.Any())
                return BadRequest(new { message = "Query parameters are required for listing entity changes." });

            query.BindFiltersFromQuery(Request.Query);
            query.Validate();

            var spec = new EntityChangeQuerySpec(query);
            var result = await _service.QueryAsync(spec, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for entity changes");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Returns a single audit record by its identifier.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("Audit", "Read")]
    [ProducesResponseType(typeof(EntityChangeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);
        if (result is null)
            return NotFound(new { message = $"Entity change {id} not found." });

        return Ok(result);
    }

    /// <summary>Returns all audit records for a specific business entity instance.</summary>
    [HttpGet("by-entity/{entityName}/{entityId:guid}")]
    [HasPermission("Audit", "Read")]
    [ProducesResponseType(typeof(IReadOnlyList<EntityChangeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByEntity(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetByEntityAsync(entityName, entityId, cancellationToken);
        return Ok(result);
    }

    /// <summary>Records a new append-only audit entry.</summary>
    [HttpPost]
    [HasPermission("Audit", "Create")]
    [ProducesResponseType(typeof(EntityChangeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Record([FromBody] CreateEntityChangeDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _service.RecordAsync(dto, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
