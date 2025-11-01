using MyApp.Shared.Domain.Entities;

namespace MyApp.Orders.Domain.Entities
{
    public class OrderLine : AuditableEntity<Guid>
    {
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
    }
}
