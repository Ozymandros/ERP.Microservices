using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes Sales order and quote operations to the AI kernel.
/// All functions delegate to the Sales microservice via <see cref="IServiceInvoker"/>,
/// using <see cref="ApiEndpoints.Sales"/> constants for endpoint resolution and
/// returning results as JSON strings for LLM function-calling pipelines.
/// </summary>
public class SalesPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the SalesPlugin class.
    /// </summary>
    /// <param name="serviceInvoker">The service Invoker.</param>
    public SalesPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new sales order or quote from the supplied JSON payload and returns the created resource.
    /// </summary>
    /// <param name="payloadJson">The payload Json.</param>
    /// <returns>JSON representation of the newly created sales order.</returns>
    [Description("Create sales order")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Base,
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a sales order by its unique identifier, including line items and quote status.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>JSON representation of the sales order, or an error description if not found.</returns>
    [Description("Get sales order by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing sales order with the values provided in the JSON payload.
    /// </summary>
    /// <param name="payloadJson">The payload Json.</param>
    /// <returns>JSON representation of the updated sales order.</returns>
    [Description("Update sales order")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Sales,
            ApiEndpoints.Sales.Base,
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a sales order by its unique identifier.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A confirmation message indicating the sales order was deleted.</returns>
    [Description("Delete sales order by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"{ApiEndpoints.Sales.Base}/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Sales order {id} deleted successfully.";
    }

    /// <summary>
    /// Gets a customer by its name.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns>JSON representation of the customer, or an error description if not found.</returns>
    [Description("Get customer by name")]
    public async Task<string> GetCustomerByNameAsync(string name)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"api/sales/customers/name/{name}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a customer by its email.
    /// </summary>
    /// <param name="email">The email.</param>
    /// <returns>JSON representation of the customer, or an error description if not found.</returns>
    [Description("Get customer by email")]
    public async Task<string> GetCustomerByEmailAsync(string email)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"api/sales/customers/email/{email}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a sales order by its order number.
    /// </summary>
    /// <param name="orderNumber">The order Number.</param>
    /// <returns>JSON representation of the sales order, or an error description if not found.</returns>
    [Description("Get sales order by order number")]
    public async Task<string> GetSalesOrderByCodeAsync(string orderNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Sales,
            $"api/sales/salesorders/code/{orderNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Searches ERP customers using the Sales service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">The query Json.</param>
    /// <returns>JSON paginated search result for matching customers.</returns>
    [Description("Search ERP customers by term, name, email, or filters")]
    public Task<string> SearchCustomersAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Sales, "api/sales/customers/search", queryJson);

    /// <summary>
    /// Searches ERP sales orders using the Sales service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">The query Json.</param>
    /// <returns>JSON paginated search result for matching sales orders.</returns>
    [Description("Search ERP sales orders by term, order code, or filters")]
    public Task<string> SearchSalesOrdersAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Sales, "api/sales/orders/search", queryJson);
}
