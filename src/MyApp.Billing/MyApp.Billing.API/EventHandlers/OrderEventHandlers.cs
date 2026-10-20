using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Billing.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Billing.API.EventHandlers;

/// <summary>Handles operational order events for billing automation.</summary>
[ApiController]
[Route("api/events/orders")]
public class OrderEventHandlers : ControllerBase
{
    private readonly IInvoiceService _invoiceService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderEventHandlers> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrderEventHandlers"/> class.</summary>
    public OrderEventHandlers(
        IInvoiceService invoiceService,
        ICacheService cacheService,
        ILogger<OrderEventHandlers> logger)
    {
        _invoiceService = invoiceService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Creates or issues invoices / credit notes when operational orders are fulfilled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderFulfilled)]
    [HttpPost("order-fulfilled")]
    public async Task<IActionResult> OnOrderFulfilledAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Billing received OrderFulfilledEvent OrderId={OrderId} Type={Type}",
            @event.OrderId,
            @event.OrderType);

        if (string.Equals(@event.OrderType, OrderTypes.Outbound, StringComparison.OrdinalIgnoreCase))
        {
            var invoice = await _invoiceService.CreateAndIssueFromFulfillmentAsync(@event, cancellationToken);
            if (invoice is not null)
                await InvalidateInvoiceCacheAsync(invoice, cancellationToken);
        }
        else if (string.Equals(@event.OrderType, OrderTypes.Return, StringComparison.OrdinalIgnoreCase))
        {
            var creditNote = await _invoiceService.CreateCreditNoteFromReturnFulfillmentAsync(@event, cancellationToken);
            if (creditNote is not null)
                await _cacheService.RemoveStateAsync("open_invoices");
        }

        return Ok();
    }

    private async Task InvalidateInvoiceCacheAsync(Application.Contracts.DTOs.InvoiceDto invoice)
    {
        await _cacheService.RemoveStateAsync($"invoice_{invoice.Id}");
        await _cacheService.RemoveStateAsync("open_invoices");
        await _cacheService.RemoveStateAsync($"invoices_customer_{invoice.CustomerId}");
        if (invoice.OrderId.HasValue)
            await _cacheService.RemoveStateAsync($"invoices_order_{invoice.OrderId}");
        if (!string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            await _cacheService.RemoveStateAsync($"Invoice-Number-{invoice.InvoiceNumber}");
    }
}
