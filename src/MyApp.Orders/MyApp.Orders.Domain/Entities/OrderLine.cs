using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    public class OrderLine(Guid id) : AuditableEntity<Guid>(id)
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        
        // Stock reservation fields
        public Guid? ReservedStockId { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsFulfilled { get; set; }
    }
}
