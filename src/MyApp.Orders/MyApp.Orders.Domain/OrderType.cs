namespace MyApp.Orders.Domain
{
    public enum OrderType
    {
        Transfer,    // Warehouse -> Warehouse
        Inbound,     // Supplier -> Warehouse
        Outbound,    // Warehouse -> Customer
        Return       // Customer -> Warehouse
    }
}
