using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Events;

namespace MyApp.Orders.API.EventHandlers;

/// <summary>
/// Event handlers for events published by the Inventory service
/// </summary>
[ApiController]
[Route("api/events/inventory")]
public class InventoryEventHandlers : ControllerBase
{
    private readonly IReservedStockRepository _reservedStockRepository;
    private readonly ILogger<InventoryEventHandlers> _logger;

    public InventoryEventHandlers(
        IReservedStockRepository reservedStockRepository,
        ILogger<InventoryEventHandlers> logger)
    {
        _reservedStockRepository = reservedStockRepository;
        _logger = logger;
    }

    /// <summary>
    /// Handles StockReservedEvent - confirms reservation was created in Inventory
    /// </summary>
    [Topic("pubsub", "inventory.stock.reserved")]
    [HttpPost("stock-reserved")]
    public async Task<IActionResult> OnStockReservedAsync(StockReservedEvent @event)
    {
        _logger.LogInformation(
            "Received StockReservedEvent: ReservationId={ReservationId}, ProductId={ProductId}, OrderId={OrderId}",
            @event.ReservationId, @event.ProductId, @event.OrderId);

        try
        {
            // Find the corresponding ReservedStock entry if it exists
            var reservations = await _reservedStockRepository.GetByOrderIdAsync(@event.OrderId);
            var reservation = reservations.FirstOrDefault(r => 
                r.ProductId == @event.ProductId && 
                r.WarehouseId == @event.WarehouseId);

            if (reservation != null)
            {
                _logger.LogInformation(
                    "Confirmed stock reservation for Order {OrderId}, Product {ProductId}",
                    @event.OrderId, @event.ProductId);
            }
            else
            {
                _logger.LogWarning(
                    "Received StockReservedEvent but no matching reservation found: OrderId={OrderId}, ProductId={ProductId}",
                    @event.OrderId, @event.ProductId);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing StockReservedEvent for Order {OrderId}",
                @event.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Handles StockReleasedEvent - marks reservation as released
    /// </summary>
    [Topic("pubsub", "inventory.stock.released")]
    [HttpPost("stock-released")]
    public async Task<IActionResult> OnStockReleasedAsync(StockReleasedEvent @event)
    {
        _logger.LogInformation(
            "Received StockReleasedEvent: ReservationId={ReservationId}, ProductId={ProductId}",
            @event.ReservationId, @event.ProductId);

        try
        {
            var reservation = await _reservedStockRepository.GetByIdAsync(@event.ReservationId);
            if (reservation != null)
            {
                // Reservation is already marked as expired/cancelled in ReservationExpiryService
                _logger.LogInformation(
                    "Stock released confirmed for reservation {ReservationId}",
                    @event.ReservationId);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing StockReleasedEvent for Reservation {ReservationId}",
                @event.ReservationId);
            throw;
        }
    }

    /// <summary>
    /// Handles LowStockAlertEvent - logs the alert
    /// </summary>
    [Topic("pubsub", "inventory.stock.low-stock-alert")]
    [HttpPost("low-stock-alert")]
    public IActionResult OnLowStockAlertAsync(LowStockAlertEvent @event)
    {
        _logger.LogWarning(
            "Received LowStockAlertEvent: ProductId={ProductId}, WarehouseId={WarehouseId}, Available={Available}, ReorderLevel={ReorderLevel}",
            @event.ProductId, @event.WarehouseId, @event.AvailableQuantity, @event.ReorderLevel);

        // Orders service could potentially block new orders for low stock products
        // For now, just log the alert

        return Ok();
    }
}
