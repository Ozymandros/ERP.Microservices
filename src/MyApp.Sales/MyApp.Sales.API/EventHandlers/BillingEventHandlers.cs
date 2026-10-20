using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Sales.API.EventHandlers;

/// <summary>Handles billing events that affect sales order status.</summary>
[ApiController]
[Route("api/events/billing")]
public class BillingEventHandlers : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly ICacheService _cacheService;

    /// <summary>Initializes a new instance of the <see cref="BillingEventHandlers"/> class.</summary>
    public BillingEventHandlers(ISalesOrderService salesOrderService, ICacheService cacheService)
    {
        _salesOrderService = salesOrderService;
        _cacheService = cacheService;
    }

    /// <summary>Marks the sales order as invoiced when billing issues an invoice.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.BillingInvoiceIssued)]
    [HttpPost("invoice-issued")]
    public async Task<IActionResult> OnInvoiceIssuedAsync(InvoiceIssuedEvent @event, CancellationToken cancellationToken)
    {
        var invalidation = await _salesOrderService.MarkInvoicedFromBillingAsync(@event, cancellationToken);
        if (invalidation is not null)
        {
            await _cacheService.RemoveStateAsync($"SalesOrder-{invalidation.SalesOrderId}");
            await _cacheService.RemoveStateAsync($"SalesOrder-Code-{invalidation.OrderNumber}");
            await _cacheService.RemoveStateAsync("all_sales_orders");
        }

        return Ok();
    }
}
