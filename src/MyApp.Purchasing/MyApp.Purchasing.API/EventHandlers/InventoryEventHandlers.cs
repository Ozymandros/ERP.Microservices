using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Domain.Events;

namespace MyApp.Purchasing.API.EventHandlers;

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
    /// Handles LowStockAlertEvent - could trigger automatic PO creation
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>200 OK to acknowledge receipt of the event.</returns>
    [Topic("pubsub", "inventory.stock.low-stock-alert")]
    [HttpPost("low-stock-alert")]
    public async Task<IActionResult> OnLowStockAlertAsync(LowStockAlertEvent @event)
    {
        _logger.LogWarning(
            "Received LowStockAlertEvent: ProductId={ProductId}, WarehouseId={WarehouseId}, Available={Available}, ReorderLevel={ReorderLevel}",
            @event.ProductId, @event.WarehouseId, @event.AvailableQuantity, @event.ReorderLevel);

        // Purchasing service could use this to:
        // - Automatically create purchase orders
        // - Notify procurement team
        // - Check supplier availability
        // - Calculate reorder quantity based on lead time and demand

        // TODO: Implement automatic PO creation logic
        _logger.LogInformation(
            "Low stock alert received. Consider creating PO for Product {ProductId}",
            @event.ProductId);

        return Ok();
    }

    /// <summary>
    /// Handles StockUpdatedEvent - tracks inventory levels
    /// </summary>
    /// <param name="event">The event.</param>
    /// <returns>200 OK to acknowledge receipt of the event.</returns>
    [Topic("pubsub", "inventory.stock.updated")]
    [HttpPost("stock-updated")]
    public IActionResult OnStockUpdatedAsync(StockUpdatedEvent @event)
    {
        _logger.LogInformation(
            "Received StockUpdatedEvent: ProductId={ProductId}, Change={Change}, Type={Type}",
            @event.ProductId, @event.QuantityChange, new MyApp.Shared.Domain.Security.LogSanitizer().Sanitize(@event.TransactionType));

        // Purchasing service could use this to:
        // - Track consumption rates
        // - Adjust reorder points
        // - Forecast future demand

        return Ok();
    }
}
