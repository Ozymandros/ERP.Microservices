using AutoMapper;
using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Fulfills an operational order.</summary>
public sealed class FulfillOrderUseCase(
    IOrderRepository orders,
    IReservedStockRepository reservedStockRepository,
    IMapper mapper,
    IOrdersTransactionBoundary boundary,
    ILogger<FulfillOrderUseCase> logger)
{
    public async Task<OrderDto> ExecuteAsync(FulfillOrderDto dto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Fulfilling order: OrderId={OrderId}", dto.OrderId);

        var order = await orders.GetByIdAsync(dto.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {dto.OrderId} not found");

        if (order.Status != OrderStatus.Approved && order.Status != OrderStatus.Draft)
            throw new OrderFulfillmentException(dto.OrderId, $"Order cannot be fulfilled. Current status: {order.Status}");

        if (order.Type == OrderType.Outbound)
        {
            var reservations = await reservedStockRepository.GetByOrderIdAsync(dto.OrderId, cancellationToken);
            if (reservations.Count == 0)
                throw new OrderFulfillmentException(dto.OrderId, "No stock reservations found for this outbound order");

            foreach (var reservation in reservations)
            {
                if (reservation.Status != ReservationStatus.Reserved)
                    throw new OrderFulfillmentException(dto.OrderId,
                        $"Reservation {reservation.Id} is not in Reserved status: {reservation.Status}");

                reservation.Status = ReservationStatus.Fulfilled;
                await reservedStockRepository.UpdateAsync(reservation);
            }
        }

        order.Status = OrderStatus.Completed;
        order.FulfilledDate = DateTime.UtcNow;
        order.WarehouseId = dto.WarehouseId;
        order.DestinationAddress = dto.ShippingAddress;
        order.TrackingNumber = dto.TrackingNumber;

        foreach (var line in order.Lines)
            line.IsFulfilled = true;

        await orders.UpdateAsync(order);

        var orderFulfilledEvent = new OrderFulfilledEvent(
            order.Id,
            order.OrderNumber,
            order.Type.ToString(),
            dto.WarehouseId,
            order.FulfilledDate.Value,
            dto.TrackingNumber,
            order.ExternalOrderId,
            order.Lines.Select(l => new OrderLineEvent(l.ProductId, l.Quantity)).ToList(),
            order.ExternalOrderSource);

        try
        {
            await boundary.EventPublisher.PublishAsync(MessagingConstants.Topics.OrderFulfilled, orderFulfilledEvent, cancellationToken);
            logger.LogInformation("Published OrderFulfilledEvent for Order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OrderFulfilledEvent for Order {OrderId}", order.Id);
        }

        await boundary.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order fulfilled successfully: OrderId={OrderId}", order.Id);
        return mapper.Map<OrderDto>(order);
    }
}