using MyApp.Agentic.API.Plugins;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin that exposes Inventory warehouse-stock operations to the AI kernel.
/// Functions delegate to the Inventory microservice via <see cref="IServiceInvoker"/>,
/// resolving the correct REST endpoint through <see cref="GetBaseEndpoint"/> and
/// returning results as JSON strings for LLM function-calling pipelines.
/// </summary>
public class InventoryPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of <see cref="InventoryPlugin"/> with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the Inventory service.</param>
    public InventoryPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Resolves the base REST endpoint for a given service name, preferring well-known
    /// <see cref="ApiEndpoints"/> constants and falling back to a conventional <c>api/{service}</c> path.
    /// </summary>
    /// <param name="serviceName">The logical service name (e.g. <see cref="ServiceNames.Inventory"/>).</param>
    /// <returns>The base API path for the service.</returns>
    private static string GetBaseEndpoint(string serviceName)
    {
        if (serviceName == ServiceNames.Inventory) return ApiEndpoints.Inventory.Base;
        if (serviceName == ServiceNames.Orders) return ApiEndpoints.Orders.Base;
        if (serviceName == ServiceNames.Sales) return ApiEndpoints.Sales.Base;
        return $"api/{serviceName.Replace("-service", string.Empty)}";
    }

    /// <summary>
    /// Creates a new inventory warehouse-stock record from the supplied JSON payload.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized warehouse-stock creation request.</param>
    /// <returns>JSON representation of the newly created inventory record.</returns>
    [Description("Create inventory resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Inventory,
            GetBaseEndpoint(ServiceNames.Inventory),
            HttpMethod.Post,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves an inventory warehouse-stock record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the inventory record.</param>
    /// <returns>JSON representation of the inventory record, or an error description if not found.</returns>
    [Description("Get inventory resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"{GetBaseEndpoint(ServiceNames.Inventory)}/{id}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing inventory warehouse-stock record with the values in the JSON payload.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized update request, including the record identifier.</param>
    /// <returns>JSON representation of the updated inventory record.</returns>
    [Description("Update inventory resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Inventory,
            GetBaseEndpoint(ServiceNames.Inventory),
            HttpMethod.Put,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes an inventory record by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the inventory record to delete.</param>
    /// <returns>A confirmation message indicating the record was deleted.</returns>
    [Description("Delete inventory resource by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"{GetBaseEndpoint(ServiceNames.Inventory)}/{id}",
            HttpMethod.Delete,
            string.Empty);

        return $"Inventory resource {id} deleted successfully.";
    }

    /// <summary>
    /// Gets a product by its name.
    /// </summary>
    /// <param name="name">Product name, or JSON such as <c>{"name":"Alpha Bolt"}</c>.</param>
    /// <returns>JSON representation of the product, or an error description if not found.</returns>
    [Description("Get product by name")]
    public async Task<string> GetProductByNameAsync(string name)
    {
        var resolvedName = PluginQueryHelper.ResolveScalarArgument(name);
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"api/inventory/products/name/{resolvedName}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a warehouse by its name.
    /// </summary>
    /// <param name="name">The warehouse name.</param>
    /// <returns>JSON representation of the warehouse, or an error description if not found.</returns>
    [Description("Get warehouse by name")]
    public async Task<string> GetWarehouseByNameAsync(string name)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"api/inventory/warehouses/name/{name}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets an inventory transaction by its reference number.
    /// </summary>
    /// <param name="referenceNumber">The transaction reference number.</param>
    /// <returns>JSON representation of the transaction, or an error description if not found.</returns>
    [Description("Get inventory transaction by reference number")]
    public async Task<string> GetTransactionByReferenceNumberAsync(string referenceNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"api/inventory/transactions/reference/{referenceNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a product by its SKU.
    /// </summary>
    /// <param name="sku">Product SKU, or JSON such as <c>{"sku":"ABC-123"}</c>.</param>
    /// <returns>JSON representation of the product, or an error description if not found.</returns>
    [Description("Get product by SKU")]
    public async Task<string> GetProductBySkuAsync(string sku)
    {
        var resolvedSku = PluginQueryHelper.ResolveScalarArgument(sku, "sku");
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            $"api/inventory/products/sku/{resolvedSku}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Searches ERP products using the Inventory service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">Search term or JSON query specification for products.</param>
    /// <returns>JSON paginated search result for matching products.</returns>
    [Description("Search ERP products by term, name, description, SKU, or filters")]
    public Task<string> SearchProductsAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Inventory, "api/inventory/products/search", queryJson);

    /// <summary>
    /// Searches ERP warehouses using the Inventory service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">Search term or JSON query specification for warehouses.</param>
    /// <returns>JSON paginated search result for matching warehouses.</returns>
    [Description("Search ERP warehouses by term, name, or filters")]
    public Task<string> SearchWarehousesAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Inventory, "api/inventory/warehouses/search", queryJson);

    /// <summary>
    /// Searches ERP inventory transactions using the Inventory service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">Search term or JSON query specification for transactions.</param>
    /// <returns>JSON paginated search result for matching inventory transactions.</returns>
    [Description("Search ERP inventory transactions by term or filters")]
    public Task<string> SearchInventoryTransactionsAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Inventory, "api/inventory/transactions/search", queryJson);

    /// <summary>
    /// Lists ERP products that are below their configured low-stock threshold.
    /// </summary>
    /// <param name="unused">Ignored. Present only to satisfy the tool invocation signature.</param>
    /// <returns>JSON collection of low-stock products.</returns>
    [Description("List ERP products with low stock levels")]
    public async Task<string> GetLowStockProductsAsync(string unused = "")
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Inventory,
            "api/inventory/products/low-stock",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }
}
