using Microsoft.Extensions.Logging;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Contracts.Ports;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Events;

namespace MyApp.Orders.Application.UseCases.Orders;

/// <summary>Cancels an order and releases reservations.</summary>
public sealed class CancelOrderUseCase(
    IOrderRepository orders,
    IReservedStockRepository reservedStockRepository,
    IInventoryStockPort inventoryStock,
    IOrdersTransactionBoundary boundary,
    ILogger<CancelOrderUseCase> logger)
{
    public async Task ExecuteAsync(CancelOrderDto dto, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Cancelling order: {@CancelOrderData}", new { dto.OrderId, dto.Reason });

        var order = await orders.GetByIdAsync(dto.OrderId, cancellationToken)
            ?? throw new InvalidOperationException($"Order {dto.OrderId} not found");

        if (order.Status == OrderStatus.Completed)
            throw new InvalidOperationException($"Cannot cancel completed order {dto.OrderId}");

        var reservations = await reservedStockRepository.GetByOrderIdAsync(dto.OrderId, cancellationToken);
        foreach (var reservation in reservations)
        {
            if (reservation.Status != ReservationStatus.Reserved)
                continue;

            try
            {
                await inventoryStock.ReleaseReservationAsync(reservation.Id, cancellationToken);
                reservation.Status = ReservationStatus.Cancelled;
                await reservedStockRepository.UpdateAsync(reservation);
                logger.LogInformation("Released reservation {ReservationId} for Order {OrderId}", reservation.Id, dto.OrderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to release reservation {ReservationId}", reservation.Id);
            }
        }

        order.Status = OrderStatus.Cancelled;
        await orders.UpdateAsync(order);

        try
        {
            await boundary.EventPublisher.PublishAsync(
                MessagingConstants.Topics.OrderCancelled,
                new OrderCancelledEvent(order.Id, dto.Reason ?? string.Empty, order.ExternalOrderId, order.ExternalOrderSource),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to publish OrderCancelledEvent for Order {OrderId}", order.Id);
        }

        await boundary.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Order cancelled successfully: OrderId={OrderId}", order.Id);
    }
}