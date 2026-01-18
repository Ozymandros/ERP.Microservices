using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    public class Order(Guid id) : AuditableEntity<Guid>(id)
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }

        // Fulfillment fields
        public Guid? WarehouseId { get; set; }
        public DateTime? FulfilledDate { get; set; }
        public string? ShippingAddress { get; set; }
        public string? TrackingNumber { get; set; }

        public List<OrderLine> Lines { get; set; } = new();
    }
}
