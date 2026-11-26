namespace MyApp.Orders.Application.Contracts.Dtos;

/// <summary>Request to reserve stock in Inventory (ACL).</summary>
public sealed record ReserveStockRequest(
    Guid ProductId,
    Guid WarehouseId,
    int Quantity,
    Guid OrderId,
    Guid OrderLineId,
    DateTime? ExpiresAt);

/// <summary>Result of a stock reservation from Inventory.</summary>
public sealed record StockReservationSnapshot(
    Guid ReservationId,
    DateTime ReservedUntil);