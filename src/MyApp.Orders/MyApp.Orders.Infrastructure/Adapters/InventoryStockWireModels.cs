namespace MyApp.Orders.Infrastructure.Adapters;

internal sealed class ReserveStockWireRequest
{
    public Guid ProductId { get; init; }
    public Guid WarehouseId { get; init; }
    public int Quantity { get; init; }
    public Guid OrderId { get; init; }
    public Guid? OrderLineId { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

internal sealed class ReservationWireResponse
{
    public Guid Id { get; init; }
    public DateTime ReservedUntil { get; init; }
}