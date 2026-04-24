using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    /// <summary>Represents a single line item in an order.</summary>
    public class OrderLine(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets the ID of the parent order.</summary>
        public Guid OrderId { get; set; }
        /// <summary>Gets or sets the product ID for this line item.</summary>
        public Guid ProductId { get; set; }
        /// <summary>Gets or sets the quantity ordered.</summary>
        public int Quantity { get; set; }

        // Operational tracking fields
        /// <summary>Gets or sets the quantity that has been picked.</summary>
        public int PickedQuantity { get; set; }
        /// <summary>Gets or sets the reserved stock ID associated with this line.</summary>
        public Guid? ReservedStockId { get; set; }
        /// <summary>Gets or sets the quantity that has been reserved.</summary>
        public int ReservedQuantity { get; set; }
        /// <summary>Gets or sets a value indicating whether this line has been fulfilled.</summary>
        public bool IsFulfilled { get; set; }
    }
}
