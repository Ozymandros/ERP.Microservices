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

    /// <summary>Gets the identifier for the Audit service.</summary>
    public const string Audit = "audit-service";

    /// <summary>Gets the identifier for the Agentic service.</summary>
    public const string Agentic = "agentic-service";

    /// <summary>Gets the identifier for the Projects service.</summary>
    public const string Projects = "projects-service";
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

        /// <summary>Gets the base endpoint for products API.</summary>
        public const string Products = "api/inventory/products";
    }

    /// <summary>Sales service endpoints.</summary>
    public static class Sales
    {
        /// <summary>Gets the base endpoint for sales orders API.</summary>
        public const string Base = "api/sales/orders";

        /// <summary>Gets the endpoint for sales quotes.</summary>
        public const string Quotes = "api/sales/orders/quotes";

        /// <summary>Gets the base endpoint for customers API.</summary>
        public const string Customers = "api/sales/customers";
    }

    /// <summary>Billing service endpoints.</summary>
    public static class Billing
    {
        /// <summary>Gets the base endpoint for customer invoices API.</summary>
        public const string Invoices = "api/billing/invoices";

        /// <summary>Gets the base endpoint for supplier invoices API.</summary>
        public const string SupplierInvoices = "api/billing/supplier-invoices";
    }

    /// <summary>Projects service endpoints.</summary>
    public static class Projects
    {
        /// <summary>Gets the base endpoint for projects API.</summary>
        public const string Base = "api/projects";
    }

    /// <summary>Audit service endpoints.</summary>
    public static class Audit
    {
        /// <summary>Gets the base endpoint for recording entity changes.</summary>
        public const string EntityChanges = "api/audit/entity-changes";
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

        /// <summary>Gets the topic name for purchasing order received events.</summary>
        public const string PurchasingOrderReceived = "purchasing.order.received";

        /// <summary>Gets the topic name for purchasing order matched (3-way) events.</summary>
        public const string PurchasingOrderMatched = "purchasing.order.matched";

        /// <summary>Gets the topic name for billing invoice created events.</summary>
        public const string BillingInvoiceCreated = "billing.invoice.created";

        /// <summary>Gets the topic name for billing invoice issued events.</summary>
        public const string BillingInvoiceIssued = "billing.invoice.issued";

        /// <summary>Gets the topic name for billing invoice paid events.</summary>
        public const string BillingInvoicePaid = "billing.invoice.paid";

        /// <summary>Gets the topic name for billing invoice cancelled events.</summary>
        public const string BillingInvoiceCancelled = "billing.invoice.cancelled";

        /// <summary>Gets the topic name for billing credit note issued events.</summary>
        public const string BillingCreditNoteIssued = "billing.creditnote.issued";

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

        /// <summary>Gets the topic name for project created events.</summary>
        public const string ProjectCreated = "projects.project.created";

        /// <summary>Gets the topic name for project status changed events.</summary>
        public const string ProjectStatusChanged = "projects.project.status-changed";

        /// <summary>Gets the topic name for project invoice requested events (consumed by Billing).</summary>
        public const string ProjectInvoiceRequested = "projects.invoice.requested";

        /// <summary>Gets the topic name for entity-change audit events after a service DB commit.</summary>
        public const string AuditEntityChangesSaved = "audit.entity-changes.saved";
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

    /// <summary>Gets the order type for customer returns.</summary>
    public const string Return = "Return";
}

/// <summary>
/// Identifies the commercial/project document that caused an operational order or an invoice.
/// A <see langword="null"/> or empty value means <see cref="Sales"/> (legacy behaviour).
/// </summary>
public static class DocumentSources
{
    /// <summary>Gets the source value for sales documents.</summary>
    public const string Sales = "Sales";

    /// <summary>Gets the source value for project documents.</summary>
    public const string Project = "Project";

    /// <summary>Returns <see langword="true"/> when the source denotes a sales document (null/empty = Sales).</summary>
    /// <param name="source">The source value.</param>
    public static bool IsSales(string? source) =>
        string.IsNullOrWhiteSpace(source) || string.Equals(source, Sales, StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns <see langword="true"/> when the source denotes a project.</summary>
    /// <param name="source">The source value.</param>
    public static bool IsProject(string? source) =>
        string.Equals(source, Project, StringComparison.OrdinalIgnoreCase);
}
