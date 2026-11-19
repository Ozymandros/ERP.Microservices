using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Purchasing.Application.Contracts.DTOs;
using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Purchasing.Application.Internals;
using MyApp.Purchasing.Domain.Entities;
using MyApp.Purchasing.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Purchasing.Application.UseCases.PurchaseOrders;

public sealed class ReceivePurchaseOrderUseCase(
    IPurchaseOrderRepository purchaseOrderRepository,
    IOrdersOperationalPort ordersOperational,
    IMapper mapper,
    IPurchasingTransactionBoundary boundary,
    ILogger<ReceivePurchaseOrderUseCase> logger)
{
    public async Task<PurchaseOrderDto> ExecuteAsync(
        ReceivePurchaseOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Receiving purchase order: PurchaseOrderId={PurchaseOrderId}, WarehouseId={WarehouseId}",
            dto.PurchaseOrderId, dto.WarehouseId);

        var order = await purchaseOrderRepository.GetWithLinesAsync(dto.PurchaseOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order with ID '{dto.PurchaseOrderId}' not found");

        if (order.Status != PurchaseOrderStatus.Approved)
            throw new InvalidOperationException($"Purchase order cannot be received. Current status: {order.Status}");

        foreach (var receivedLine in dto.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == receivedLine.PurchaseOrderLineId);
            if (orderLine == null)
            {
                logger.LogWarning(
                    "Purchase order line not found: PurchaseOrderLineId={PurchaseOrderLineId}",
                    receivedLine.PurchaseOrderLineId);
                continue;
            }

            orderLine.ReceivedQuantity += receivedLine.ReceivedQuantity;
            orderLine.IsFullyReceived = orderLine.ReceivedQuantity >= orderLine.Quantity;

            try
            {
                var fulfillmentOrder = await ordersOperational.CreateInboundOrderAsync(
                    new CreateInboundOrderRequest(
                        $"{order.OrderNumber}-REC",
                        null,
                        dto.WarehouseId,
                        order.Id,
                        null,
                        dto.WarehouseId,
                        dto.ReceivedDate,
                        [new InboundOrderLineRequest(orderLine.ProductId, receivedLine.ReceivedQuantity)]),
                    cancellationToken);

                logger.LogInformation(
                    "Created Inbound Order {OrderId} for Product {ProductId} to Warehouse {WarehouseId}",
                    fulfillmentOrder.Id, orderLine.ProductId, dto.WarehouseId);

                await ordersOperational.FulfillInboundOrderAsync(
                    new FulfillInboundOrderRequest(
                        fulfillmentOrder.Id,
                        dto.WarehouseId,
                        $"REC-{order.OrderNumber}",
                        null),
                    cancellationToken);

                logger.LogInformation("Fulfilled Inbound Order {OrderId}", fulfillmentOrder.Id);

                await boundary.EventPublisher.PublishAsync(
                    MessagingConstants.Topics.PurchasingLineReceived,
                    new PurchaseOrderLineReceivedEvent(
                        order.Id,
                        orderLine.Id,
                        orderLine.ProductId,
                        receivedLine.ReceivedQuantity,
                        dto.WarehouseId),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to create Inbound Order for Product {ProductId} from PO {PurchaseOrderId}",
                    orderLine.ProductId, order.Id);
                throw;
            }
        }

        var allLinesReceived = order.Lines.All(l => l.IsFullyReceived);
        order.ReceivedDate = dto.ReceivedDate;
        order.ReceivingWarehouseId = dto.WarehouseId;
        order.IsReceived = allLinesReceived;
        order.Status = allLinesReceived ? PurchaseOrderStatus.Received : PurchaseOrderStatus.Approved;

        await purchaseOrderRepository.UpdateAsync(order);
        await boundary.SaveChangesAsync(cancellationToken);

        if (allLinesReceived)
        {
            try
            {
                await boundary.EventPublisher.PublishAsync(
                    "purchasing.order.received",
                    new PurchaseOrderReceivedEvent(order.Id, dto.WarehouseId, dto.ReceivedDate),
                    cancellationToken);
                logger.LogInformation("Published PurchaseOrderReceivedEvent for PO {PurchaseOrderId}", order.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to publish PurchaseOrderReceivedEvent for PO {PurchaseOrderId}", order.Id);
            }
        }

        logger.LogInformation(
            "Purchase order receiving processed: PurchaseOrderId={PurchaseOrderId}, Status={Status}",
            order.Id, order.Status);

        return mapper.Map<PurchaseOrderDto>(order);
    }
}