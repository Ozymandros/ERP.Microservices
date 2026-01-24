using Dapr;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Constants;

namespace MyApp.Inventory.API.EventHandlers;

/// <summary>
/// Event handlers for Operational Orders - strictly following the Traceability Hierarchy:
/// Inventory responds ONLY to Operational Orders, NEVER to Commercial Docs (Sales/Purchasing).
/// </summary>
[ApiController]
[Route("api/events/orders")]
public class OrderEventHandlers : ControllerBase
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly ILogger<OrderEventHandlers> _logger;
    private readonly IEventPublisher _eventPublisher;

    public OrderEventHandlers(
        IWarehouseStockRepository warehouseStockRepository,
        IInventoryTransactionRepository transactionRepository,
        ILogger<OrderEventHandlers> logger,
        IEventPublisher eventPublisher)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Handles OrderCreatedEvent - For Inbound orders, update OnOrderQuantity
    /// </summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderCreated)]
    [HttpPost("order-created")]
    public async Task<IActionResult> OnOrderCreatedAsync(OrderCreatedEvent @event)
    {
        _logger.LogInformation(
            "Received OrderCreatedEvent: OrderId={OrderId}, Type={Type}",
            @event.OrderId, @event.OrderType);

        if (@event.OrderType != "Inbound" || !@event.WarehouseId.HasValue)
        {
            return Ok(); // Outbound/Transfer logic might be different or handled elsewhere
        }

        try
        {
            foreach (var line in @event.Lines)
            {
                // Get or create warehouse stock
                var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(line.ProductId, @event.WarehouseId.Value);
                if (warehouseStock == null)
                {
                    warehouseStock = new WarehouseStock(Guid.NewGuid())
                    {
                        ProductId = line.ProductId,
                        WarehouseId = @event.WarehouseId.Value,
                        AvailableQuantity = 0,
                        ReservedQuantity = 0,
                        OnOrderQuantity = 0
                    };
                    await _warehouseStockRepository.AddAsync(warehouseStock);
                }

                // Update OnOrderQuantity
                warehouseStock.OnOrderQuantity += line.Quantity;
                await _warehouseStockRepository.UpdateAsync(warehouseStock);

                _logger.LogInformation(
                    "Updated OnOrderQuantity for Product {ProductId} in Warehouse {WarehouseId}: +{Quantity}",
                    line.ProductId, @event.WarehouseId.Value, line.Quantity);
            }
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderCreatedEvent for Order {OrderId}", @event.OrderId);
            throw;
        }
    }

    /// <summary>
    /// Handles OrderFulfilledEvent - The source of truth for physical stock movements
    /// </summary>
    [Topic(MessagingConstants.PubSubName, MessagingConstants.Topics.OrderFulfilled)]
    [HttpPost("order-fulfilled")]
    public async Task<IActionResult> OnOrderFulfilledAsync(OrderFulfilledEvent @event)
    {
        _logger.LogInformation(
            "Received OrderFulfilledEvent: OrderId={OrderId}, Type={Type}, WarehouseId={WarehouseId}",
            @event.OrderId, @event.OrderType, @event.WarehouseId);

        try
        {
            foreach (var line in @event.Lines)
            {
                // Get or create warehouse stock
                var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(line.ProductId, @event.WarehouseId);
                if (warehouseStock == null)
                {
                    warehouseStock = new WarehouseStock(Guid.NewGuid())
                    {
                        ProductId = line.ProductId,
                        WarehouseId = @event.WarehouseId,
                        AvailableQuantity = 0,
                        ReservedQuantity = 0,
                        OnOrderQuantity = 0
                    };
                    await _warehouseStockRepository.AddAsync(warehouseStock);
                }

                // Determine quantity change
                int quantityChange = 0;
                TransactionType transType;

                if (@event.OrderType == "Inbound")
                {
                    quantityChange = line.Quantity;
                    transType = TransactionType.Inbound;
                    warehouseStock.AvailableQuantity += quantityChange;
                    warehouseStock.OnOrderQuantity = Math.Max(0, warehouseStock.OnOrderQuantity - quantityChange);
                }
                else if (@event.OrderType == "Outbound")
                {
                    quantityChange = -line.Quantity;
                    transType = TransactionType.Outbound;
                    // For Outbound, it was likelyReserved previously
                    warehouseStock.ReservedQuantity = Math.Max(0, warehouseStock.ReservedQuantity - line.Quantity);
                    // Available stay the same because it was already removed from Available when Reserved
                    // OR if not reserved, we subtract from Available
                }
                else // Transfer, etc.
                {
                    return Ok();
                }

                await _warehouseStockRepository.UpdateAsync(warehouseStock);

                // Create inventory transaction
                var transaction = new InventoryTransaction(Guid.NewGuid())
                {
                    ProductId = line.ProductId,
                    WarehouseId = @event.WarehouseId,
                    QuantityChange = quantityChange,
                    TransactionType = transType,
                    TransactionDate = @event.FulfilledDate,
                    OrderId = @event.OrderId,
                    ReferenceNumber = @event.OrderNumber
                };
                await _transactionRepository.AddAsync(transaction);

                // Publish StockUpdatedEvent
                var stockUpdatedEvent = new StockUpdatedEvent(
                    line.ProductId,
                    @event.WarehouseId,
                    quantityChange,
                    @event.OrderType
                );
                
                await _eventPublisher.PublishAsync(MessagingConstants.Topics.InventoryStockUpdated, stockUpdatedEvent);
            }

            _logger.LogInformation("Successfully processed OrderFulfilledEvent for Order {OrderId}", @event.OrderId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing OrderFulfilledEvent for Order {OrderId}", @event.OrderId);
            throw;
        }
    }
}
