using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Crm.API.Controllers;

[ApiController]
[Authorize]
[Route("api/crm/contacts")]
public sealed class ContactsController : ControllerBase
{
    private readonly IContactService _service;
    private readonly ILogger<ContactsController> _logger;

    public ContactsController(IContactService service, ILogger<ContactsController> logger)
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
                var result = await _service.QueryAsync(query, cancellationToken);
                return Ok(result.ToPaginatedResponse(query.Page, query.PageSize));
            }

            return Ok(Array.Empty<ContactDto>());
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for contacts");
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:guid}")]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound(new { message = $"Contact with ID {id} not found." }) : Ok(item);
    }

    [HttpGet("/api/crm/accounts/{accountId:guid}/contacts")]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetByAccount(Guid accountId, CancellationToken cancellationToken)
    {
        var list = await _service.ListByAccountAsync(accountId, cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    [HasPermission("CRM", "Create")]
    public async Task<IActionResult> Create([FromBody] CreateContactDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var created = await _service.CreateAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
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

    [HttpPut("{id:guid}")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateContactDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var updated = await _service.UpdateAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("/api/crm/accounts/{accountId:guid}/contacts/{contactId:guid}/set-primary")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> SetPrimary(Guid accountId, Guid contactId, CancellationToken cancellationToken)
    {
        try
        {
            await _service.SetPrimaryAsync(accountId, contactId, cancellationToken);
            return NoContent();
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

    [HttpDelete("{id:guid}")]
    [HasPermission("CRM", "Delete")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _service.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}

