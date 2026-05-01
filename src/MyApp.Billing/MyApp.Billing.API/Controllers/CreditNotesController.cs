using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Entities;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Billing.API.Controllers;

/// <summary>
/// Read-only access to credit notes. Credit notes are created via POST /api/invoices/{id}/credit-notes.
/// </summary>
[Route("api/billing/credit-notes")]
[Authorize]
[ApiController]
public class CreditNotesController : ControllerBase
{
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CreditNotesController> _logger;

    /// <summary>Initialises a new instance of <see cref="CreditNotesController"/>.</summary>
    public CreditNotesController(
        ICreditNoteRepository creditNoteRepository,
        ICacheService cacheService,
        ILogger<CreditNotesController> logger)
    {
        _creditNoteRepository = creditNoteRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all credit notes linked to the specified invoice.
    /// This action is also used as the redirect target when a new credit note is created
    /// via <c>POST /api/invoices/{id}/credit-notes</c>.
    /// </summary>
    [HttpGet("invoice/{invoiceId:guid}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<CreditNoteDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"credit_notes_invoice_{invoiceId}";
            var cached = await _cacheService.GetStateAsync<IEnumerable<CreditNoteDto>>(cacheKey);
            if (cached is not null)
                return Ok(cached);

            var creditNotes = await _creditNoteRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken);

            var dtos = creditNotes.Select(cn => new CreditNoteDto(
                cn.Id,
                cn.OriginalInvoiceId,
                cn.Reason,
                cn.Status.ToString(),
                cn.TotalNet,
                cn.TotalTax,
                cn.TotalGross,
                cn.CreatedAt));

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving credit notes for invoice {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving credit notes" });
        }
    }
}
