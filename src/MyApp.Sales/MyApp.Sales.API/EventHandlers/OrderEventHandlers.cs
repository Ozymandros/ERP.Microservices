using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Sales.Application.Contracts.Services;
using MyApp.Shared.Domain.Caching;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Sales.API.EventHandlers;

/// <summary>Handles operational order events that affect sales order status.</summary>
[ApiController]
[Route("api/events/orders")]
public class OrderEventHandlers : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<OrderEventHandlers> _logger;

    /// <summary>Initializes a new instance of the <see cref="OrderEventHandlers"/> class.</summary>
    public OrderEventHandlers(
        ISalesOrderService salesOrderService,
        ICacheService cacheService,
        ILogger<OrderEventHandlers> logger)
    {
        _salesOrderService = salesOrderService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>Marks the linked sales order as shipped when an outbound order is fulfilled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderFulfilled)]
    [HttpPost("order-fulfilled")]
    public async Task<IActionResult> OnOrderFulfilledAsync(OrderFulfilledEvent @event, CancellationToken cancellationToken)
    {
        var invalidation = await _salesOrderService.MarkShippedFromFulfillmentAsync(@event, cancellationToken);
        if (invalidation is not null)
            await InvalidateSalesOrderCacheAsync(invalidation.SalesOrderId, invalidation.OrderNumber);

        return Ok();
    }

    /// <summary>Marks the linked sales order as cancelled when an operational order is cancelled.</summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderCancelled)]
    [HttpPost("order-cancelled")]
    public async Task<IActionResult> OnOrderCancelledAsync(OrderCancelledEvent @event, CancellationToken cancellationToken)
    {
        var invalidation = await _salesOrderService.MarkCancelledFromOrderAsync(@event, cancellationToken);
        if (invalidation is not null)
            await InvalidateSalesOrderCacheAsync(invalidation.SalesOrderId, invalidation.OrderNumber);

        return Ok();
    }

    private async Task InvalidateSalesOrderCacheAsync(Guid salesOrderId, string orderNumber)
    {
        await _cacheService.RemoveStateAsync($"SalesOrder-{salesOrderId}");
        await _cacheService.RemoveStateAsync($"SalesOrder-Code-{orderNumber}");
        await _cacheService.RemoveStateAsync("all_sales_orders");
    }
}
