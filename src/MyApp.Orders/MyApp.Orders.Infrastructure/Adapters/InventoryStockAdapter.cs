using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Orders.Infrastructure.Adapters;

/// <summary>Inventory stock port over Dapr service invocation.</summary>
public sealed class InventoryStockAdapter(IServiceInvoker serviceInvoker) : IInventoryStockPort
{
    /// <inheritdoc />
    public async Task<StockReservationSnapshot> ReserveAsync(
        ReserveStockRequest request,
        CancellationToken cancellationToken = default)
    {
        var wireRequest = new ReserveStockWireRequest
        {
            ProductId = request.ProductId,
            WarehouseId = request.WarehouseId,
            Quantity = request.Quantity,
            OrderId = request.OrderId,
            OrderLineId = request.OrderLineId,
            ExpiresAt = request.ExpiresAt
        };

        var wire = await serviceInvoker.InvokeAsync<ReserveStockWireRequest, ReservationWireResponse>(
            ServiceNames.Inventory,
            ApiEndpoints.Inventory.ReserveStock,
            HttpMethod.Post,
            wireRequest,
            cancellationToken);

        return new StockReservationSnapshot(wire.Id, wire.ReservedUntil);
    }

    /// <inheritdoc />
    public Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default) =>
        serviceInvoker.InvokeAsync(
            ServiceNames.Inventory,
            $"{ApiEndpoints.Inventory.Reservations}/{reservationId}",
            HttpMethod.Delete,
            cancellationToken);
}