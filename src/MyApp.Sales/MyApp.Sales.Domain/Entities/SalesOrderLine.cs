using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    public class SalesOrderLine(Guid id) : AuditableEntity<Guid>(id)
    {
        public Guid SalesOrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        
        // Denormalized product info for display
        public string? ProductSKU { get; set; }
        public string? ProductName { get; set; }
    }
}
