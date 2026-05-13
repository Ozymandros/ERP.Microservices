using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Billing.Domain.Specifications;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Permissions;
using MyApp.Shared.Infrastructure.Export;
using MyApp.Shared.Infrastructure.Extensions;

namespace MyApp.Billing.API.Controllers;

/// <summary>
/// Manages billing invoices — creation, lifecycle transitions, payment recording and export.
/// </summary>
[Route("api/billing/invoices")]
[Authorize]
[ApiController]
public class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<InvoicesController> _logger;

    /// <summary>Initialises a new instance of <see cref="InvoicesController"/>.</summary>
    public InvoicesController(
        IInvoiceService invoiceService,
        ICacheService cacheService,
        ILogger<InvoicesController> logger)
    {
        _invoiceService = invoiceService;
        _cacheService = cacheService;
        _logger = logger;
    }

    // ──────────────────────────── QUERIES ────────────────────────────

    /// <summary>
    /// Returns all open (Issued / Sent) invoices.
    /// </summary>
    [HttpGet]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOpenInvoices(CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await _cacheService.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices")
                ?? await _invoiceService.GetOpenInvoicesAsync(cancellationToken);

            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving open invoices");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving invoices" });
        }
    }

    /// <summary>
    /// Returns a single invoice by its unique identifier.
    /// </summary>
    [HttpGet("{id:guid}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"invoice_{id}";
            var invoice = await _cacheService.GetStateAsync<InvoiceDto>(cacheKey)
                ?? await _invoiceService.GetInvoiceByIdAsync(id, cancellationToken);

            if (invoice is null)
                return NotFound(new { message = $"Invoice {id} not found." });

            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving the invoice" });
        }
    }

    /// <summary>
    /// Get invoice by Invoice Number - Requires Billing.Read permission
    /// </summary>
    [HttpGet("number/{invoiceNumber}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByInvoiceNumber(string invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKey = "Invoice-Number-" + invoiceNumber;
            var invoice = await _cacheService.GetStateAsync<InvoiceDto>(cacheKey)
                ?? await _invoiceService.GetInvoiceByInvoiceNumberAsync(invoiceNumber, cancellationToken);

            if (invoice == null)
            {
                _logger.LogWarning("Invoice with number {@InvoiceNumber} not found", new { InvoiceNumber = invoiceNumber });
                return NotFound();
            }

            if (await _cacheService.GetStateAsync<InvoiceDto>(cacheKey) == null)
            {
                await _cacheService.SaveStateAsync(cacheKey, invoice);
            }

            _logger.LogInformation("Retrieved invoice with number {@InvoiceNumber}", new { InvoiceNumber = invoiceNumber });
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoice with number {@InvoiceNumber}", new { InvoiceNumber = invoiceNumber });
            var invoice = await _invoiceService.GetInvoiceByInvoiceNumberAsync(invoiceNumber, cancellationToken);
            return invoice == null ? NotFound() : Ok(invoice);
        }
    }

    /// <summary>
    /// Returns all invoices belonging to the specified customer.
    /// </summary>
    [HttpGet("customer/{customerId:guid}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"invoices_customer_{customerId}";
            var invoices = await _cacheService.GetStateAsync<IEnumerable<InvoiceDto>>(cacheKey)
                ?? await _invoiceService.GetInvoicesByCustomerIdAsync(customerId, cancellationToken);

            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for customer {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving customer invoices" });
        }
    }

    /// <summary>
    /// Returns all invoices linked to the specified order.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByOrder(Guid orderId, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"invoices_order_{orderId}";
            var invoices = await _cacheService.GetStateAsync<IEnumerable<InvoiceDto>>(cacheKey)
                ?? await _invoiceService.GetInvoicesByOrderIdAsync(orderId, cancellationToken);

            return Ok(invoices);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving invoices for order {OrderId}", orderId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving order invoices" });
        }
    }

    /// <summary>
    /// Searches invoices with filter, sort and pagination.
    /// </summary>
    [HttpGet("search")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(PaginatedResult<InvoiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Search([FromQuery] QuerySpec query, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            query.BindFiltersFromQuery(Request.Query);
            query.Validate();

            var spec = new InvoiceQuerySpec(query);
            var result = await _invoiceService.QueryInvoicesAsync(spec, cancellationToken);

            _logger.LogInformation("Searched invoices with query: {@Query}", query);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid query specification for invoice search");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching invoices");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred searching invoices" });
        }
    }

    // ──────────────────────────── EXPORTS ────────────────────────────

    /// <summary>
    /// Exports all open invoices as an Excel workbook (.xlsx).
    /// </summary>
    [HttpGet("export-xlsx")]
    [HasPermission("Billing", "Read")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToXlsx(CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await _cacheService.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices")
                ?? await _invoiceService.GetOpenInvoicesAsync(cancellationToken);

            var bytes = invoices.ExportToXlsx();
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Invoices.xlsx");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices to XLSX");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred exporting invoices" });
        }
    }

    /// <summary>
    /// Exports all open invoices as a PDF document.
    /// </summary>
    [HttpGet("export-pdf")]
    [HasPermission("Billing", "Read")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportToPdf(CancellationToken cancellationToken)
    {
        try
        {
            var invoices = await _cacheService.GetStateAsync<IEnumerable<InvoiceDto>>("open_invoices")
                ?? await _invoiceService.GetOpenInvoicesAsync(cancellationToken);

            var bytes = invoices.ExportToPdf();
            return File(bytes, "application/pdf", "Invoices.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoices to PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred exporting invoices" });
        }
    }

    // ──────────────────────────── COMMANDS ───────────────────────────

    /// <summary>
    /// Creates a new invoice in Draft status.
    /// </summary>
    [HttpPost]
    [HasPermission("Billing", "Create")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateInvoiceDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var invoice = await _invoiceService.CreateInvoiceAsync(dto, cancellationToken);
            await _cacheService.RemoveStateAsync("open_invoices");
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating invoice");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            throw;
        }
    }

    /// <summary>
    /// Issues a draft invoice — assigns an invoice number and sets the due date.
    /// </summary>
    [HttpPost("{id:guid}/issue")]
    [HasPermission("Billing", "Update")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Issue(Guid id, [FromBody] IssueInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var invoice = await _invoiceService.IssueInvoiceAsync(id, request.InvoiceNumber, request.IssueDate, cancellationToken);
            await InvalidateInvoiceCacheAsync(id, invoice.CustomerId, invoice.OrderId);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error issuing invoice {InvoiceId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = $"Invoice {id} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing invoice {InvoiceId}", id);
            throw;
        }
    }

    /// <summary>
    /// Records a payment against an invoice.
    /// </summary>
    [HttpPost("{id:guid}/payments")]
    [HasPermission("Billing", "Update")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Ensure the InvoiceId in the body always matches the route
            var effectiveDto = dto with { InvoiceId = id };
            var invoice = await _invoiceService.RecordPaymentAsync(effectiveDto, cancellationToken);
            await InvalidateInvoiceCacheAsync(id, invoice.CustomerId, invoice.OrderId);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error recording payment for invoice {InvoiceId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = $"Invoice {id} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording payment for invoice {InvoiceId}", id);
            throw;
        }
    }

    /// <summary>
    /// Cancels an invoice. Paid invoices cannot be cancelled.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [HasPermission("Billing", "Update")]
    [ProducesResponseType(typeof(InvoiceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelInvoiceRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var invoice = await _invoiceService.CancelInvoiceAsync(id, request.Reason, cancellationToken);
            await InvalidateInvoiceCacheAsync(id, invoice.CustomerId, invoice.OrderId);
            return Ok(invoice);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error cancelling invoice {InvoiceId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = $"Invoice {id} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invoice {InvoiceId}", id);
            throw;
        }
    }

    /// <summary>
    /// Creates a credit note against an existing invoice.
    /// </summary>
    [HttpPost("{id:guid}/credit-notes")]
    [HasPermission("Billing", "Create")]
    [ProducesResponseType(typeof(CreditNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCreditNote(Guid id, [FromBody] CreateCreditNoteDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Ensure InvoiceId in body matches the route
            var effectiveDto = dto with { InvoiceId = id };
            var creditNote = await _invoiceService.CreateCreditNoteAsync(effectiveDto, cancellationToken);
            await InvalidateInvoiceCacheAsync(id, Guid.Empty, null);
            return CreatedAtAction(
                nameof(CreditNotesController.GetByInvoice),
                "CreditNotes",
                new { invoiceId = id },
                creditNote);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error creating credit note for invoice {InvoiceId}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return NotFound(new { message = $"Invoice {id} not found." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating credit note for invoice {InvoiceId}", id);
            throw;
        }
    }

    // ──────────────────────── HELPERS ────────────────────────────────

    private async Task InvalidateInvoiceCacheAsync(Guid invoiceId, Guid customerId, Guid? orderId)
    {
        await _cacheService.RemoveStateAsync($"invoice_{invoiceId}");
        await _cacheService.RemoveStateAsync("open_invoices");

        if (customerId != Guid.Empty)
            await _cacheService.RemoveStateAsync($"invoices_customer_{customerId}");

        if (orderId.HasValue)
            await _cacheService.RemoveStateAsync($"invoices_order_{orderId}");
    }
}
