using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

public enum InventoryReservationStatus
{
    Reserved,
    Released,
    Expired
}

/// <summary>
/// Tracks stock reservations made by the Inventory service so they can be looked up
/// and reversed by reservationId when a release is requested.
/// </summary>
public class InventoryReservation(Guid id) : AuditableEntity<Guid>(id)
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? OrderLineId { get; set; }
    public int Quantity { get; set; }
    public DateTime ReservedUntil { get; set; }
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Reserved;

    // Navigation properties
    public Product? Product { get; set; }
    public Warehouse? Warehouse { get; set; }
}
