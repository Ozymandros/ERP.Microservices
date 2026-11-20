using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Contracts.Ports;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Creates an order and reserves stock via Inventory.</summary>
public sealed class CreateOrderWithReservationUseCase(
    IOrderRepository orders,
    IReservedStockRepository reservedStockRepository,
    IInventoryStockPort inventoryStock,
    CancelOrderUseCase cancelOrder,
    IMapper mapper,
    IOrdersTransactionBoundary boundary,
    ILogger<CreateOrderWithReservationUseCase> logger)
{
    public async Task<OrderDto> ExecuteAsync(CreateOrderWithReservationDto dto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Creating operational order with reservation: Type={Type}, WarehouseId={WarehouseId}",
            dto.Type, dto.WarehouseId);

        if (dto.Lines.Count == 0)
            throw new InvalidOperationException("Order must have at least one line");

        var order = new Order(Guid.NewGuid())
        {
            OrderNumber = await OrderNumberGenerator.GenerateAsync(cancellationToken),
            Type = dto.Type,
            SourceId = dto.SourceId,
            TargetId = dto.TargetId,
            ExternalOrderId = dto.ExternalOrderId,
            ExternalOrderSource = dto.ExternalOrderSource,
            OrderDate = dto.OrderDate,
            Status = OrderStatus.Draft,
            WarehouseId = dto.WarehouseId,
            DestinationAddress = dto.DestinationAddress
        };

        foreach (var lineDto in dto.Lines)
        {
            order.Lines.Add(new OrderLine(Guid.NewGuid())
            {
                OrderId = order.Id,
                ProductId = lineDto.ProductId,
                Quantity = lineDto.Quantity
            });
        }

        await orders.AddAsync(order);

        foreach (var line in order.Lines)
        {
            try
            {
                logger.LogInformation(
                    "Reserving stock for OrderLine: ProductId={ProductId}, Quantity={Quantity}, WarehouseId={WarehouseId}",
                    line.ProductId, line.Quantity, dto.WarehouseId);

                var reservation = await inventoryStock.ReserveAsync(
                    new ReserveStockRequest(
                        line.ProductId,
                        dto.WarehouseId,
                        line.Quantity,
                        order.Id,
                        line.Id,
                        null),
                    cancellationToken);

                var reservedStock = new ReservedStock(reservation.ReservationId)
                {
                    ProductId = line.ProductId,
                    WarehouseId = dto.WarehouseId,
                    OrderId = order.Id,
                    OrderLineId = line.Id,
                    Quantity = line.Quantity,
                    ReservedUntil = reservation.ReservedUntil,
                    Status = ReservationStatus.Reserved
                };

                await reservedStockRepository.AddAsync(reservedStock);
                line.ReservedStockId = reservedStock.Id;
                line.ReservedQuantity = line.Quantity;
                logger.LogInformation("Stock reserved successfully for OrderLine {OrderLineId}", line.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to reserve stock for OrderLine {OrderLineId}, Product {ProductId}", line.Id, line.ProductId);
                await cancelOrder.ExecuteAsync(new CancelOrderDto
                {
                    OrderId = order.Id,
                    Reason = $"Stock reservation failed: {ex.Message}"
                }, cancellationToken);
                throw new OrderFulfillmentException(order.Id, $"Failed to reserve stock: {ex.Message}");
            }
        }

        order.Status = OrderStatus.Approved;
        await orders.UpdateAsync(order);

        try
        {
            await boundary.EventPublisher.PublishAsync(
                MessagingConstants.Topics.OrderCreated,
                new OrderCreatedEvent(
                    order.Id,
                    order.OrderNumber,
                    order.Type.ToString(),
                    order.WarehouseId,
                    order.Lines.Select(l => new OrderLineEvent(l.ProductId, l.Quantity)).ToList()),
                cancellationToken);
            logger.LogInformation("Published OrderCreatedEvent for Order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OrderCreatedEvent for Order {OrderId}", order.Id);
        }

        await boundary.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order created successfully with reservations: OrderId={OrderId}", order.Id);
        return mapper.Map<OrderDto>(order);
    }
}