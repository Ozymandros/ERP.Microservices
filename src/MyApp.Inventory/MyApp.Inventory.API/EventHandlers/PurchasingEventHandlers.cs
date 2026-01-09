using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;
using MyApp.Shared.Domain.Events;

namespace MyApp.Inventory.API.EventHandlers;

/// <summary>
/// Event handlers for events published by the Purchasing service
/// </summary>
[ApiController]
[Route("api/events/purchasing")]
public class PurchasingEventHandlers : ControllerBase
{
    private readonly IWarehouseStockRepository _warehouseStockRepository;
    private readonly IInventoryTransactionRepository _transactionRepository;
    private readonly ILogger<PurchasingEventHandlers> _logger;
    private readonly DaprClient _daprClient;

    public PurchasingEventHandlers(
        IWarehouseStockRepository warehouseStockRepository,
        IInventoryTransactionRepository transactionRepository,
        ILogger<PurchasingEventHandlers> logger,
        DaprClient daprClient)
    {
        _warehouseStockRepository = warehouseStockRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
        _daprClient = daprClient;
    }

    /// <summary>
    /// Handles PurchaseOrderApprovedEvent - updates OnOrderQuantity
    /// </summary>
    [Topic("pubsub", "purchasing.po.approved")]
    [HttpPost("po-approved")]
    public async Task<IActionResult> OnPurchaseOrderApprovedAsync(PurchaseOrderApprovedEvent @event)
    {
        _logger.LogInformation(
            "Received PurchaseOrderApprovedEvent: POId={POId}, SupplierId={SupplierId}",
            @event.PurchaseOrderId, @event.SupplierId);

        try
        {
            // TODO: Get PO lines and update OnOrderQuantity for each product
            // This would require service-to-service call to Purchasing service
            // For now, just log the event
            
            _logger.LogInformation("Processed PurchaseOrderApprovedEvent for PO {POId}", @event.PurchaseOrderId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PurchaseOrderApprovedEvent for PO {POId}", @event.PurchaseOrderId);
            throw;
        }
    }

    /// <summary>
    /// Handles PurchaseOrderReceivedEvent - creates inbound transaction and updates stock
    /// </summary>
    [Topic("pubsub", "purchasing.po.received")]
    [HttpPost("po-received")]
    public async Task<IActionResult> OnPurchaseOrderReceivedAsync(PurchaseOrderReceivedEvent @event)
    {
        _logger.LogInformation(
            "Received PurchaseOrderReceivedEvent: POId={POId}, WarehouseId={WarehouseId}",
            @event.PurchaseOrderId, @event.WarehouseId);

        try
        {
            // TODO: Get PO lines from Purchasing service and create inbound transactions
            // For now, just log the event
            
            _logger.LogInformation("Processed PurchaseOrderReceivedEvent for PO {POId}", @event.PurchaseOrderId);
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing PurchaseOrderReceivedEvent for PO {POId}", @event.PurchaseOrderId);
            throw;
        }
    }

    /// <summary>
    /// Handles PurchaseOrderLineReceivedEvent - creates inbound transaction and updates stock
    /// </summary>
    [Topic("pubsub", "purchasing.po.line-received")]
    [HttpPost("po-line-received")]
    public async Task<IActionResult> OnPurchaseOrderLineReceivedAsync(PurchaseOrderLineReceivedEvent @event)
    {
        _logger.LogInformation(
            "Received PurchaseOrderLineReceivedEvent: ProductId={ProductId}, WarehouseId={WarehouseId}, Quantity={Quantity}",
            @event.ProductId, @event.WarehouseId, @event.ReceivedQuantity);

        try
        {
            // Get or create warehouse stock
            var warehouseStock = await _warehouseStockRepository.GetByProductAndWarehouseAsync(@event.ProductId, @event.WarehouseId);
            if (warehouseStock == null)
            {
                warehouseStock = new WarehouseStock(Guid.NewGuid())
                {
                    ProductId = @event.ProductId,
                    WarehouseId = @event.WarehouseId,
                    AvailableQuantity = 0,
                    ReservedQuantity = 0,
                    OnOrderQuantity = 0
                };
                await _warehouseStockRepository.AddAsync(warehouseStock);
            }

            // Update stock quantities
            warehouseStock.AvailableQuantity += @event.ReceivedQuantity;
            warehouseStock.OnOrderQuantity = Math.Max(0, warehouseStock.OnOrderQuantity - @event.ReceivedQuantity);
            await _warehouseStockRepository.UpdateAsync(warehouseStock);

            // Create inbound transaction
            var transaction = new InventoryTransaction(Guid.NewGuid())
            {
                ProductId = @event.ProductId,
                WarehouseId = @event.WarehouseId,
                QuantityChange = @event.ReceivedQuantity,
                TransactionType = TransactionType.Inbound,
                TransactionDate = DateTime.UtcNow,
                PurchaseOrderId = @event.PurchaseOrderId,
                ReferenceNumber = $"PO-{@event.PurchaseOrderId}"
            };
            await _transactionRepository.AddAsync(transaction);

            // Publish StockUpdatedEvent
            var stockUpdatedEvent = new StockUpdatedEvent(
                @event.ProductId,
                @event.WarehouseId,
                @event.ReceivedQuantity,
                "Inbound"
            );
            
            await _daprClient.PublishEventAsync("pubsub", "inventory.stock.updated", stockUpdatedEvent);

            _logger.LogInformation(
                "Processed PurchaseOrderLineReceivedEvent for Product {ProductId}, New Available: {Available}",
                @event.ProductId, warehouseStock.AvailableQuantity);
            
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing PurchaseOrderLineReceivedEvent for Product {ProductId}",
                @event.ProductId);
            throw;
        }
    }
}
