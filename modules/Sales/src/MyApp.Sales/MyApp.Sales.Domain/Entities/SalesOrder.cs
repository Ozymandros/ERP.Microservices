using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    public class SalesOrder : AuditableEntity<Guid>
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public DateTime OrderDate { get; set; }
        public SalesOrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }

        public Customer? Customer { get; set; }
        public List<SalesOrderLine> Lines { get; set; } = new();
    }
}
