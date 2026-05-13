using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.DTOs;
using MyApp.Billing.Domain.Repositories;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Permissions;

namespace MyApp.Billing.API.Controllers;

/// <summary>
/// Read-only access to payment records associated with invoices.
/// </summary>
[Route("api/billing/payments")]
[Authorize]
[ApiController]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<PaymentsController> _logger;

    /// <summary>Initialises a new instance of <see cref="PaymentsController"/>.</summary>
    public PaymentsController(
        IPaymentRepository paymentRepository,
        ICacheService cacheService,
        ILogger<PaymentsController> logger)
    {
        _paymentRepository = paymentRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Returns all payments recorded against the specified invoice.
    /// </summary>
    [HttpGet("invoice/{invoiceId:guid}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(typeof(IEnumerable<PaymentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetByInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"payments_invoice_{invoiceId}";
            var cached = await _cacheService.GetStateAsync<IEnumerable<PaymentDto>>(cacheKey);
            if (cached is not null)
                return Ok(cached);

            var payments = await _paymentRepository.GetByInvoiceIdAsync(invoiceId, cancellationToken);

            var dtos = payments.Select(p => new PaymentDto(
                p.Id,
                p.InvoiceId,
                p.Amount,
                p.Currency,
                p.Method,
                p.Status.ToString(),
                p.PaidAt));

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payments for invoice {InvoiceId}", invoiceId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred retrieving payments" });
        }
    }

    /// <summary>
    /// Get payment by External Payment ID - Requires Billing.Read permission
    /// </summary>
    [HttpGet("external/{externalPaymentId}")]
    [HasPermission("Billing", "Read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByExternalPaymentId(string externalPaymentId, CancellationToken cancellationToken)
    {
        try
        {
            string cacheKey = "Payment-External-" + externalPaymentId;
            var cached = await _cacheService.GetStateAsync<PaymentDto>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Retrieved payment with external ID {@ExternalPaymentId} from cache", new { ExternalPaymentId = externalPaymentId });
                return Ok(cached);
            }

            var payment = await _paymentRepository.GetByExternalPaymentIdAsync(externalPaymentId, cancellationToken);
            if (payment == null)
            {
                _logger.LogWarning("Payment with external ID {@ExternalPaymentId} not found", new { ExternalPaymentId = externalPaymentId });
                return NotFound();
            }

            var dto = new PaymentDto(
                payment.Id,
                payment.InvoiceId,
                payment.Amount,
                payment.Currency,
                payment.Method,
                payment.Status.ToString(),
                payment.PaidAt);

            await _cacheService.SaveStateAsync(cacheKey, dto);
            _logger.LogInformation("Retrieved payment with external ID {@ExternalPaymentId} from database and cached", new { ExternalPaymentId = externalPaymentId });
            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment with external ID {@ExternalPaymentId}", new { ExternalPaymentId = externalPaymentId });
            var payment = await _paymentRepository.GetByExternalPaymentIdAsync(externalPaymentId, cancellationToken);
            if (payment == null)
                return NotFound();
            return Ok(new PaymentDto(
                payment.Id,
                payment.InvoiceId,
                payment.Amount,
                payment.Currency,
                payment.Method,
                payment.Status.ToString(),
                payment.PaidAt));
        }
    }
}
