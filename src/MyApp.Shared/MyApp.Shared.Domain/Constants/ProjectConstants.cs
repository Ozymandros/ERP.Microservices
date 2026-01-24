namespace MyApp.Shared.Domain.Constants;

public static class ServiceNames
{
    public const string Orders = "orders";
    public const string Inventory = "inventory";
    public const string Sales = "sales";
    public const string Purchasing = "purchasing";
}

public static class ApiEndpoints
{
    public static class Orders
    {
        public const string Base = "api/orders";
        public const string Fulfill = "api/orders/fulfill";
        public const string WithReservation = "api/orders/with-reservation";
    }

    public static class Inventory
    {
        public const string Base = "api/warehousestocks";
        public const string Availability = "api/warehousestocks/availability";
    }
}

public static class MessagingConstants
{
    public const string PubSubName = "pubsub";

    public static class Topics
    {
        public const string SalesOrderCreated = "sales.order.created";
        public const string SalesOrderConfirmed = "sales.order.confirmed";
        public const string PurchasingOrderApproved = "purchasing.order.approved";
        public const string PurchasingLineReceived = "purchasing.line.received";
        
        public const string OrderCreated = "orders.order.created";
        public const string OrderFulfilled = "orders.order.fulfilled";
        
        public const string InventoryStockUpdated = "inventory.stock.updated";
    }
}
