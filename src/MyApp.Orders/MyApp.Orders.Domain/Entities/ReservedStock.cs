using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    /// <summary>Represents reserved stock for an order.</summary>
    public class ReservedStock(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets the product ID that is reserved.</summary>
        public Guid ProductId { get; set; }
        /// <summary>Gets or sets the warehouse ID where stock is reserved.</summary>
        public Guid WarehouseId { get; set; }
        /// <summary>Gets or sets the order ID this reservation is associated with.</summary>
        public Guid OrderId { get; set; }
        /// <summary>Gets or sets the order line ID this reservation is associated with.</summary>
        public Guid? OrderLineId { get; set; }
        /// <summary>Gets or sets the quantity reserved.</summary>
        public int Quantity { get; set; }
        /// <summary>Gets or sets the date and time until which the stock is reserved.</summary>
        public DateTime ReservedUntil { get; set; }
        /// <summary>Gets or sets the current reservation status.</summary>
        public ReservationStatus Status { get; set; }
    }
}
