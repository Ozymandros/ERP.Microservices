using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Domain.Events;

namespace MyApp.Sales.API.EventHandlers;

/// <summary>
/// Event handlers for events published by the Inventory service
/// </summary>
[ApiController]
[Route("api/events/inventory")]
public class InventoryEventHandlers : ControllerBase
{
    private readonly ILogger<InventoryEventHandlers> _logger;

    /// <summary>
    /// Initializes a new instance of the InventoryEventHandlers class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public InventoryEventHandlers(ILogger<InventoryEventHandlers> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Handles LowStockAlertEvent - could be used to notify sales team or adjust pricing
    /// </summary>
    /// <param name="event">The event.</param>
    [Topic("pubsub", "inventory.stock.low-stock-alert")]
    [HttpPost("low-stock-alert")]
    public IActionResult OnLowStockAlertAsync(LowStockAlertEvent @event)
    {
        _logger.LogWarning(
            "Received LowStockAlertEvent: ProductId={ProductId}, WarehouseId={WarehouseId}, Available={Available}",
            @event.ProductId, @event.WarehouseId, @event.AvailableQuantity);

        // Sales service could use this to:
        // - Notify sales team
        // - Mark product as limited availability
        // - Adjust pricing
        // - Update quote validity rules

        return Ok();
    }

    /// <summary>
    /// Handles StockUpdatedEvent - logs stock changes that might affect quotes
    /// </summary>
    /// <param name="event">The event.</param>
    [Topic("pubsub", "inventory.stock.updated")]
    [HttpPost("stock-updated")]
    public IActionResult OnStockUpdatedAsync(StockUpdatedEvent @event)
    {
        _logger.LogInformation(
            "Received StockUpdatedEvent: ProductId={ProductId}, WarehouseId={WarehouseId}, Change={Change}",
            @event.ProductId, @event.WarehouseId, @event.QuantityChange);

        // Sales service could use this to:
        // - Update quote availability status
        // - Notify customers waiting for quotes

        return Ok();
    }
}
