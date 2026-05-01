using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    /// <summary>
    /// Provides Sales Order functionality.
    /// </summary>
    public class SalesOrder(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets Order Number.</summary>
        public string OrderNumber { get; set; } = string.Empty;
        /// <summary>Gets or sets Customer Id.</summary>
        public Guid CustomerId { get; set; }
        /// <summary>Gets or sets Order Date.</summary>
        public DateTime OrderDate { get; set; }
        /// <summary>Gets or sets Status.</summary>
        public SalesOrderStatus Status { get; set; }
        /// <summary>Gets or sets Total Amount.</summary>
        public decimal TotalAmount { get; set; }

        // Quote and conversion tracking
        /// <summary>Gets or sets Converted To Order Id.</summary>
        public Guid? ConvertedToOrderId { get; set; }
        /// <summary>Gets or sets Is Quote.</summary>
        public bool IsQuote { get; set; }
        /// <summary>Gets or sets Quote Expiry Date.</summary>
        public DateTime? QuoteExpiryDate { get; set; }

        /// <summary>Gets or sets Customer.</summary>
        public Customer? Customer { get; set; }
        /// <summary>Gets or sets Lines.</summary>
        public List<SalesOrderLine> Lines { get; set; } = new();
    }
}
