using MyApp.Shared.Domain.Entities;

namespace MyApp.Inventory.Domain.Entities;

/// <summary>
/// Enumeration representing the status of an inventory reservation.
/// </summary>
public enum InventoryReservationStatus
{
    /// <summary>
    /// Stock is currently reserved.
    /// </summary>
    Reserved,
    /// <summary>
    /// Reservation has been released.
    /// </summary>
    Released,
    /// <summary>
    /// Reservation has expired.
    /// </summary>
    Expired
}

/// <summary>
/// Tracks stock reservations made by the Inventory service so they can be looked up
/// and reversed by reservationId when a release is requested.
/// </summary>
public class InventoryReservation(Guid id) : AuditableEntity<Guid>(id)
{
    /// <summary>
    /// The product being reserved.
    /// </summary>
    public Guid ProductId { get; set; }
    /// <summary>
    /// The warehouse where the product is reserved.
    /// </summary>
    public Guid WarehouseId { get; set; }
    /// <summary>
    /// The order for which the stock is reserved.
    /// </summary>
    public Guid OrderId { get; set; }
    /// <summary>
    /// Optional order line associated with the reservation.
    /// </summary>
    public Guid? OrderLineId { get; set; }
    /// <summary>
    /// The quantity of items reserved.
    /// </summary>
    public int Quantity { get; set; }
    /// <summary>
    /// The date and time when the reservation will expire if not used.
    /// </summary>
    public DateTime ReservedUntil { get; set; }
    /// <summary>
    /// The current status of the reservation.
    /// </summary>
    public InventoryReservationStatus Status { get; set; } = InventoryReservationStatus.Reserved;

    /// <summary>
    /// Navigation property to the product being reserved.
    /// </summary>
    public Product? Product { get; set; }
    /// <summary>
    /// Navigation property to the warehouse storing the reserved product.
    /// </summary>
    public Warehouse? Warehouse { get; set; }
}
