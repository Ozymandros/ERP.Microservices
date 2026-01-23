using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    public class Order(Guid id) : AuditableEntity<Guid>(id)
    {
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        
        // Operational Type
        public OrderType Type { get; set; }
        
        // Logistic Points (Source/Target)
        public Guid? SourceId { get; set; }
        public Guid? TargetId { get; set; }
        
        // External Reference (link to SalesOrder or PurchaseOrder)
        public Guid? ExternalOrderId { get; set; }

        // Fulfillment fields
        public Guid? WarehouseId { get; set; }
        public DateTime? FulfilledDate { get; set; }
        public string? DestinationAddress { get; set; }
        public string? TrackingNumber { get; set; }

        public List<OrderLine> Lines { get; set; } = new();
    }
}
