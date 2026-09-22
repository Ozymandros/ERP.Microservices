namespace MyApp.Sales.Domain.Entities
{
    /// <summary>
    /// Defines the lifecycle states of a sales order.
    /// </summary>
    public enum SalesOrderStatus
    {
        /// <summary>The sales order (or quote) has been created but not yet confirmed.</summary>
        Draft,
        /// <summary>The sales order has been confirmed and is awaiting fulfilment.</summary>
        Confirmed,
        /// <summary>An invoice has been raised for the sales order.</summary>
        Invoiced,
        /// <summary>The goods associated with the sales order have been shipped.</summary>
        Shipped,
        /// <summary>The sales order has been cancelled and will not be fulfilled.</summary>
        Cancelled
    }
}
