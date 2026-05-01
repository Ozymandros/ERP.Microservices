namespace MyApp.Orders.Domain
{
    /// <summary>Represents the type of order operation.</summary>
    public enum OrderType
    {
        /// <summary>Transfer order from one warehouse to another.</summary>
        Transfer,    // Warehouse -> Warehouse
        /// <summary>Inbound order from supplier to warehouse.</summary>
        Inbound,     // Supplier -> Warehouse
        /// <summary>Outbound order from warehouse to customer.</summary>
        Outbound,    // Warehouse -> Customer
        /// <summary>Return order from customer back to warehouse.</summary>
        Return       // Customer -> Warehouse
    }
}
