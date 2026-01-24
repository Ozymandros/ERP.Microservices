namespace MyApp.Shared.Domain.Constants;

public static class ServiceNames
{
    public const string Orders = "orders-service";
    public const string Inventory = "inventory-service";
    public const string Sales = "sales-service";
    public const string Purchasing = "purchasing-service";
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
        public const string ReserveStock = "api/stockoperations/reserve";
        public const string Reservations = "api/stockoperations/reservations";
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
        public const string OrderCancelled = "orders.order.cancelled";
        
        public const string InventoryStockUpdated = "inventory.stock.updated";
        public const string InventoryStockReleased = "inventory.stock.released";
    }
}

public static class OrderTypes
{
    public const string Inbound = "Inbound";
    public const string Outbound = "Outbound";
    public const string Transfer = "Transfer";
}
