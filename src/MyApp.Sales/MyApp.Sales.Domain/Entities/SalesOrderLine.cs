using MyApp.Shared.Domain.Entities;

namespace MyApp.Sales.Domain.Entities
{
    /// <summary>
    /// Sales order line.
    /// </summary>
    /// <param name="id">The id.</param>
    public class SalesOrderLine(Guid id) : AuditableEntity<Guid>(id)
    {
        /// <summary>Gets or sets Sales Order Id.</summary>
        public Guid SalesOrderId { get; set; }
        /// <summary>Gets or sets Product Id.</summary>
        public Guid ProductId { get; set; }
        /// <summary>Gets or sets Quantity.</summary>
        public int Quantity { get; set; }
        /// <summary>Gets or sets Unit Price.</summary>
        public decimal UnitPrice { get; set; }
        /// <summary>Gets or sets Line Total.</summary>
        public decimal LineTotal { get; set; }

        // Denormalized product info for display
        /// <summary>Gets or sets Product S K U.</summary>
        public string? ProductSKU { get; set; }
        /// <summary>Gets or sets Product Name.</summary>
        public string? ProductName { get; set; }
    }
}
