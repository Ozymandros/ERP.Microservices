namespace MyApp.Sales.Domain.Entities
{
    /// <summary>
    /// Defines the Sales Order Status enumeration values.
    /// </summary>
    public enum SalesOrderStatus
    {
        Draft,
        Confirmed,
        Invoiced,
        Shipped,
        Cancelled
    }
}
