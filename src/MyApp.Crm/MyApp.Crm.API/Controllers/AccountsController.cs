using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Crm.Application.Contracts.DTOs;
using MyApp.Crm.Application.Contracts.Services;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Crm.API.Controllers;

/// <summary>
/// Provides Accounts Controller functionality.
/// </summary>
[ApiController]
[Authorize]
[Route("api/crm/accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountService _service;
    private readonly ILogger<AccountsController> _logger;

    /// <summary>I Logger.</summary>
    public AccountsController(IAccountService service, ILogger<AccountsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get All.</summary>
    [HttpGet]
    [HasPermission("CRM", "Read")]
    [ProducesResponseType(typeof(IEnumerable<AccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResult<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        try
        {
            if (Request.Query.Any())
            {
                query.BindFiltersFromQuery(Request.Query);
                query.Validate();
                var result = await _service.QueryAsync(query, cancellationToken);
                return Ok(result);
            }

            var list = await _service.ListAsync(cancellationToken);
            return Ok(list);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query spec for accounts");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Get By Id.</summary>
    [HttpGet("{id:guid}")]
    [HasPermission("CRM", "Read")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _service.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound(new { message = $"Account with ID {id} not found." }) : Ok(item);
    }

    /// <summary>Update Owner.</summary>
    [HttpPut("{id:guid}/owner")]
    [HasPermission("CRM", "Update")]
    public async Task<IActionResult> UpdateOwner(Guid id, [FromBody] UpdateAccountOwnerDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        try
        {
            var updated = await _service.UpdateOwnerAsync(id, dto, cancellationToken);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}

