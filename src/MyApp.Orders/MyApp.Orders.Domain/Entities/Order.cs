using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    /// <summary>Represents an operational order for transferring stock between warehouses or customers.</summary>
    public class Order(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets the unique order number.</summary>
        public string OrderNumber { get; set; } = string.Empty;
        /// <summary>Gets or sets the date the order was created.</summary>
        public DateTime OrderDate { get; set; }
        /// <summary>Gets or sets the current status of the order.</summary>
        public OrderStatus Status { get; set; }

        // Operational Type
        /// <summary>Gets or sets the type of order (Transfer, Inbound, Outbound, or Return).</summary>
        public OrderType Type { get; set; }

        // Logistic Points (Source/Target)
        /// <summary>Gets or sets the source location ID for the order.</summary>
        public Guid? SourceId { get; set; }
        /// <summary>Gets or sets the target location ID for the order.</summary>
        public Guid? TargetId { get; set; }

        // External Reference (link to SalesOrder or PurchaseOrder)
        /// <summary>Gets or sets the external order ID (e.g., SalesOrder or PurchaseOrder ID).</summary>
        public Guid? ExternalOrderId { get; set; }

        // Fulfillment fields
        /// <summary>Gets or sets the warehouse ID associated with the order.</summary>
        public Guid? WarehouseId { get; set; }
        /// <summary>Gets or sets the date the order was fulfilled.</summary>
        public DateTime? FulfilledDate { get; set; }
        /// <summary>Gets or sets the destination address for shipment.</summary>
        public string? DestinationAddress { get; set; }
        /// <summary>Gets or sets the tracking number for the shipment.</summary>
        public string? TrackingNumber { get; set; }

        /// <summary>Gets or sets the collection of order lines.</summary>
        public List<OrderLine> Lines { get; set; } = new();
    }
}
