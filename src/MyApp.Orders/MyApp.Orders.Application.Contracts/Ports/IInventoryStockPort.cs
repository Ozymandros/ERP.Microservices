using MyApp.Orders.Application.Contracts.Dtos;

namespace MyApp.Orders.Application.Contracts.Ports;

/// <summary>Driven port: reserve and release stock via Inventory service.</summary>
public interface IInventoryStockPort
{
    /// <summary>Reserves stock for an order line.</summary>
    Task<StockReservationSnapshot> ReserveAsync(ReserveStockRequest request, CancellationToken cancellationToken = default);

    /// <summary>Releases a reservation by id.</summary>
    Task ReleaseReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
}