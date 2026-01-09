using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    public class SalesOrder(Guid id) : AuditableEntity<Guid>(id)
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }
        
        // Quote and conversion tracking
        public Guid? ConvertedToOrderId { get; set; }
        public bool IsQuote { get; set; }
        public DateTime? QuoteExpiryDate { get; set; }

        public Customer? Customer { get; set; }
        public List<SalesOrderLine> Lines { get; set; } = new();
    }
}
