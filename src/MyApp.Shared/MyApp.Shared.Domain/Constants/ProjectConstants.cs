namespace MyApp.Shared.Domain.Constants;

/// <summary>
/// Contains the identifiers for all microservices in the ERP application.
/// </summary>
public static class ServiceNames
{
    /// <summary>Gets the identifier for the Orders service.</summary>
    public const string Orders = "orders-service";

    /// <summary>Gets the identifier for the Inventory service.</summary>
    public const string Inventory = "inventory-service";

    /// <summary>Gets the identifier for the Sales service.</summary>
    public const string Sales = "sales-service";

    /// <summary>Gets the identifier for the Purchasing service.</summary>
    public const string Purchasing = "purchasing-service";

    /// <summary>Gets the identifier for the Auth service.</summary>
    public const string Auth = "auth-service";

    /// <summary>Gets the identifier for the Billing service.</summary>
    public const string Billing = "billing-service";

    /// <summary>Gets the identifier for the CRM service.</summary>
    public const string Crm = "crm-service";

    /// <summary>Gets the identifier for the Notification service.</summary>
    public const string Notification = "notification-service";
}

/// <summary>
/// Contains API endpoint paths for various services.
/// </summary>
public static class ApiEndpoints
{
    /// <summary>Orders service endpoints.</summary>
    public static class Orders
    {
        /// <summary>Gets the base endpoint for orders API.</summary>
        public const string Base = "api/orders";

        /// <summary>Gets the endpoint for orders with reservation information.</summary>
        public const string WithReservation = "api/orders/with-reservation";
    }

    /// <summary>Inventory service endpoints.</summary>
    public static class Inventory
    {
        /// <summary>Gets the base endpoint for warehouse stocks API.</summary>
        public const string Base = "api/inventory/warehouse-stocks";

        /// <summary>Gets the endpoint for checking stock availability.</summary>
        public const string Availability = "api/inventory/warehouse-stocks/availability";

        /// <summary>Gets the endpoint for reserving stock.</summary>
        public const string ReserveStock = "api/inventory/stock-operations/reserve";

        /// <summary>Gets the endpoint for managing stock reservations.</summary>
        public const string Reservations = "api/inventory/stock-operations/reservations";
    }

    /// <summary>Sales service endpoints.</summary>
    public static class Sales
    {
        /// <summary>Gets the base endpoint for sales orders API.</summary>
        public const string Base = "api/sales/orders";

        /// <summary>Gets the endpoint for sales quotes.</summary>
        public const string Quotes = "api/sales/orders/quotes";
    }
}

/// <summary>
/// Contains constants for message publishing and pub/sub communication.
/// </summary>
public static class MessagingConstants
{
    /// <summary>Gets the name of the pub/sub component.</summary>
    public const string PubSubName = "pubsub";

    /// <summary>Defines message topic names used for pub/sub communication.</summary>
    public static class Topics
    {
        /// <summary>Gets the topic name for sales customer created events.</summary>
        public const string SalesCustomerCreated = "sales.customer.created";

        /// <summary>Gets the topic name for sales customer updated events.</summary>
        public const string SalesCustomerUpdated = "sales.customer.updated";

        /// <summary>Gets the topic name for sales order created events.</summary>
        public const string SalesOrderCreated = "sales.order.created";

        /// <summary>Gets the topic name for sales order confirmed events.</summary>
        public const string SalesOrderConfirmed = "sales.order.confirmed";

        /// <summary>Gets the topic name for purchasing order approved events.</summary>
        public const string PurchasingOrderApproved = "purchasing.order.approved";

        /// <summary>Gets the topic name for purchasing line received events.</summary>
        public const string PurchasingLineReceived = "purchasing.line.received";

        /// <summary>Gets the topic name for order created events.</summary>
        public const string OrderCreated = "orders.order.created";

        /// <summary>Gets the topic name for order fulfilled events.</summary>
        public const string OrderFulfilled = "orders.order.fulfilled";

        /// <summary>Gets the topic name for order cancelled events.</summary>
        public const string OrderCancelled = "orders.order.cancelled";

        /// <summary>Gets the topic name for inventory stock updated events.</summary>
        public const string InventoryStockUpdated = "inventory.stock.updated";

        /// <summary>Gets the topic name for inventory stock released events.</summary>
        public const string InventoryStockReleased = "inventory.stock.released";

        /// <summary>Gets the topic name for CRM lead created events.</summary>
        public const string CrmLeadCreated = "crm.lead.created";

        /// <summary>Gets the topic name for CRM lead updated events.</summary>
        public const string CrmLeadUpdated = "crm.lead.updated";

        /// <summary>Gets the topic name for CRM lead qualified events.</summary>
        public const string CrmLeadQualified = "crm.lead.qualified";

        /// <summary>Gets the topic name for CRM opportunity created events.</summary>
        public const string CrmOpportunityCreated = "crm.opportunity.created";

        /// <summary>Gets the topic name for CRM opportunity stage changed events.</summary>
        public const string CrmOpportunityStageChanged = "crm.opportunity.stage-changed";

        /// <summary>Gets the topic name for CRM opportunity won events.</summary>
        public const string CrmOpportunityWon = "crm.opportunity.won";

        /// <summary>Gets the topic name for CRM opportunity lost events.</summary>
        public const string CrmOpportunityLost = "crm.opportunity.lost";

        /// <summary>Gets the topic name for CRM activity created events.</summary>
        public const string CrmActivityCreated = "crm.activity.created";

        /// <summary>Gets the topic name for CRM activity completed events.</summary>
        public const string CrmActivityCompleted = "crm.activity.completed";
    }
}

/// <summary>
/// Defines types of inventory orders.
/// </summary>
public static class OrderTypes
{
    /// <summary>Gets the order type for inbound orders.</summary>
    public const string Inbound = "Inbound";

    /// <summary>Gets the order type for outbound orders.</summary>
    public const string Outbound = "Outbound";

    /// <summary>Gets the order type for inter-warehouse transfers.</summary>
    public const string Transfer = "Transfer";
}
