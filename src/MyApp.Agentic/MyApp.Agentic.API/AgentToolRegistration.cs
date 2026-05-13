using MyApp.Agentic.API.Plugins;
using MyApp.Agentic.Application.AI;

namespace MyApp.Agentic.API;

/// <summary>
/// Registers ERP plugin handlers and metadata in the agent tool registry at application startup.
/// </summary>
public static class AgentToolRegistration
{
    /// <summary>
    /// Registers all ERP read and write tools exposed to agentic chat and agent runtimes.
    /// </summary>
    /// <param name="services">Root service provider used to resolve plugin implementations.</param>
    /// <param name="registry">Agent tool registry that stores tool metadata and handlers.</param>
    public static void RegisterErpTools(IServiceProvider services, IAgentToolRegistry registry)
    {
        var billing = services.GetRequiredService<BillingPlugin>();
        Register(registry, "get_billing", "Get a billing record by id from the ERP billing service.", ToolHttpVerb.Get, billing.GetByIdAsync);
        Register(registry, "get_invoice", "Get an invoice by invoice number from the ERP billing service.", ToolHttpVerb.Get, billing.GetInvoiceByNumberAsync);
        Register(registry, "get_payment_by_external_id", "Get a payment by external payment id from the ERP billing service.", ToolHttpVerb.Get, billing.GetPaymentByExternalIdAsync);
        Register(registry, "search_invoices", "Search ERP invoices by keyword, invoice number, customer, or filters.", ToolHttpVerb.Get, billing.SearchInvoicesAsync);
        Register(registry, "create_billing", "Create a billing resource in the ERP billing service.", ToolHttpVerb.Post, billing.CreateAsync);
        Register(registry, "update_billing", "Update a billing resource in the ERP billing service.", ToolHttpVerb.Put, billing.UpdateAsync);
        Register(registry, "delete_billing", "Delete a billing resource by id in the ERP billing service.", ToolHttpVerb.Delete, billing.DeleteAsync);

        var orders = services.GetRequiredService<OrdersPlugin>();
        Register(registry, "get_order", "Get a sales order by id from the ERP orders service.", ToolHttpVerb.Get, orders.GetByIdAsync);
        Register(registry, "get_order_by_number", "Get a sales order by order number from the ERP orders service.", ToolHttpVerb.Get, orders.GetByOrderNumberAsync);
        Register(registry, "search_orders", "Search ERP operational orders by keyword, order number, or filters.", ToolHttpVerb.Get, orders.SearchOrdersAsync);
        Register(registry, "create_order", "Create an order in the ERP orders service.", ToolHttpVerb.Post, orders.CreateAsync);
        Register(registry, "update_order", "Update an order in the ERP orders service.", ToolHttpVerb.Put, orders.UpdateAsync);
        Register(registry, "delete_order", "Delete an order by id in the ERP orders service.", ToolHttpVerb.Delete, orders.DeleteAsync);

        var inventory = services.GetRequiredService<InventoryPlugin>();
        Register(registry, "get_inventory_stock", "Get warehouse stock / inventory record by id from the ERP inventory service.", ToolHttpVerb.Get, inventory.GetByIdAsync);
        Register(registry, "get_product_by_name", "Get a single ERP product by exact name. Prefer search_products when the name is partial or unknown.", ToolHttpVerb.Get, inventory.GetProductByNameAsync);
        Register(registry, "get_product_by_sku", "Get a single ERP product by SKU code.", ToolHttpVerb.Get, inventory.GetProductBySkuAsync);
        Register(registry, "get_warehouse_by_name", "Get a warehouse by exact name from the ERP inventory service.", ToolHttpVerb.Get, inventory.GetWarehouseByNameAsync);
        Register(registry, "get_inventory_transaction_by_reference", "Get an inventory transaction by reference number from the ERP inventory service.", ToolHttpVerb.Get, inventory.GetTransactionByReferenceNumberAsync);
        Register(registry, "search_products", "Search ERP products by keyword, name, description, SKU, category, or filters. JSON example: {\"searchTerm\":\"bolt\",\"searchFields\":\"name,description,sku\"}.", ToolHttpVerb.Get, inventory.SearchProductsAsync);
        Register(registry, "search_warehouses", "Search ERP warehouses by keyword, name, or filters.", ToolHttpVerb.Get, inventory.SearchWarehousesAsync);
        Register(registry, "search_inventory_transactions", "Search ERP inventory transactions by keyword or filters.", ToolHttpVerb.Get, inventory.SearchInventoryTransactionsAsync);
        Register(registry, "get_low_stock_products", "List ERP products that are below their low-stock threshold.", ToolHttpVerb.Get, _ => inventory.GetLowStockProductsAsync());
        Register(registry, "create_inventory_stock", "Create an inventory warehouse-stock record in the ERP inventory service.", ToolHttpVerb.Post, inventory.CreateAsync);
        Register(registry, "update_inventory_stock", "Update an inventory warehouse-stock record in the ERP inventory service.", ToolHttpVerb.Put, inventory.UpdateAsync);
        Register(registry, "delete_inventory_stock", "Delete an inventory warehouse-stock record by id.", ToolHttpVerb.Delete, inventory.DeleteAsync);

        var sales = services.GetRequiredService<SalesPlugin>();
        Register(registry, "get_sales_record", "Get a sales record by id from the ERP sales service.", ToolHttpVerb.Get, sales.GetByIdAsync);
        Register(registry, "get_customer_by_name", "Get a customer by exact name from the ERP sales service.", ToolHttpVerb.Get, sales.GetCustomerByNameAsync);
        Register(registry, "get_customer_by_email", "Get a customer by email from the ERP sales service.", ToolHttpVerb.Get, sales.GetCustomerByEmailAsync);
        Register(registry, "get_sales_order_by_code", "Get a sales order by order code from the ERP sales service.", ToolHttpVerb.Get, sales.GetSalesOrderByCodeAsync);
        Register(registry, "search_customers", "Search ERP customers by keyword, name, email, or filters.", ToolHttpVerb.Get, sales.SearchCustomersAsync);
        Register(registry, "search_sales_orders", "Search ERP sales orders by keyword, order code, customer, or filters.", ToolHttpVerb.Get, sales.SearchSalesOrdersAsync);
        Register(registry, "create_sales_record", "Create a sales record in the ERP sales service.", ToolHttpVerb.Post, sales.CreateAsync);
        Register(registry, "update_sales_record", "Update a sales record in the ERP sales service.", ToolHttpVerb.Put, sales.UpdateAsync);
        Register(registry, "delete_sales_record", "Delete a sales record by id in the ERP sales service.", ToolHttpVerb.Delete, sales.DeleteAsync);

        var purchasing = services.GetRequiredService<PurchasingPlugin>();
        Register(registry, "get_purchasing_record", "Get a purchasing record by id from the ERP purchasing service.", ToolHttpVerb.Get, purchasing.GetByIdAsync);
        Register(registry, "get_supplier_by_name", "Get a supplier by exact name from the ERP purchasing service.", ToolHttpVerb.Get, purchasing.GetSupplierByNameAsync);
        Register(registry, "get_purchase_order_by_code", "Get a purchase order by code from the ERP purchasing service.", ToolHttpVerb.Get, purchasing.GetPurchaseOrderByCodeAsync);
        Register(registry, "search_purchase_orders", "Search ERP purchase orders by keyword, code, supplier, or filters.", ToolHttpVerb.Get, purchasing.SearchPurchaseOrdersAsync);
        Register(registry, "search_suppliers", "Search ERP suppliers by keyword, name, email, or filters.", ToolHttpVerb.Get, purchasing.SearchSuppliersAsync);
        Register(registry, "create_purchasing_record", "Create a purchasing record in the ERP purchasing service.", ToolHttpVerb.Post, purchasing.CreateAsync);
        Register(registry, "update_purchasing_record", "Update a purchasing record in the ERP purchasing service.", ToolHttpVerb.Put, purchasing.UpdateAsync);
        Register(registry, "delete_purchasing_record", "Delete a purchasing record by id in the ERP purchasing service.", ToolHttpVerb.Delete, purchasing.DeleteAsync);

        var crm = services.GetRequiredService<CrmPlugin>();
        Register(registry, "get_crm_record", "Get a CRM record by id from the ERP CRM service.", ToolHttpVerb.Get, crm.GetByIdAsync);
        Register(registry, "get_crm_account_by_tax_id", "Get a CRM account by tax id from the ERP CRM service.", ToolHttpVerb.Get, crm.GetAccountByTaxIdAsync);
        Register(registry, "get_crm_account_by_customer_id", "Get a CRM account by customer id from the ERP CRM service.", ToolHttpVerb.Get, crm.GetAccountByCustomerIdAsync);
        Register(registry, "create_crm_record", "Create a CRM record in the ERP CRM service.", ToolHttpVerb.Post, crm.CreateAsync);
        Register(registry, "update_crm_record", "Update a CRM record in the ERP CRM service.", ToolHttpVerb.Put, crm.UpdateAsync);
        Register(registry, "delete_crm_record", "Delete a CRM record by id in the ERP CRM service.", ToolHttpVerb.Delete, crm.DeleteAsync);

        var auth = services.GetRequiredService<AuthPlugin>();
        Register(registry, "get_user", "Get a user by id from the ERP auth service.", ToolHttpVerb.Get, auth.GetUserAsync);
        Register(registry, "login_user", "Authenticate a user via the ERP auth service.", ToolHttpVerb.Post, auth.LoginAsync);

        var docs = services.GetRequiredService<DocsPlugin>();
        Register(registry, "search_docs", "Search MyApp ERP microservices documentation by keyword.", ToolHttpVerb.Get, docs.SearchAsync);
        Register(registry, "get_docs_topic", "Get ERP documentation content for a topic.", ToolHttpVerb.Get, docs.GetTopicAsync);
        Register(registry, "get_docs_api_reference", "Get ERP API reference documentation.", ToolHttpVerb.Get, args => docs.GetApiReferenceAsync(args));
        Register(registry, "get_docs_sections", "List available ERP documentation sections.", ToolHttpVerb.Get, _ => docs.GetSectionsAsync());
    }

    /// <summary>
    /// Registers a single ERP tool definition and its execution handler.
    /// </summary>
    /// <param name="registry">Destination tool registry.</param>
    /// <param name="name">Canonical tool name exposed to the LLM.</param>
    /// <param name="description">Human-readable tool description shown to the model.</param>
    /// <param name="verb">HTTP verb classification used for bot-type filtering.</param>
    /// <param name="handler">Plugin delegate that executes the tool.</param>
    private static void Register(
        IAgentToolRegistry registry,
        string name,
        string description,
        ToolHttpVerb verb,
        Func<string, Task<string>> handler)
    {
        registry.RegisterTool(
            new RegisteredAgentTool(name, description, verb),
            (args, ct) => handler(args));
    }
}
