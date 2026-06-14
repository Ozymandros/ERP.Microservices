using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes operational Order management functions to the AI kernel.
/// All functions delegate to the Orders microservice via <see cref="IServiceInvoker"/>,
/// using <see cref="ApiEndpoints.Orders"/> constants for endpoint resolution and
/// returning results as JSON strings for LLM function-calling pipelines.
/// </summary>
public class OrdersPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of <see cref="OrdersPlugin"/> with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the Orders service.</param>
    public OrdersPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new operational order from the supplied JSON payload and returns the created resource.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized order creation request including line items and quantities.</param>
    /// <returns>JSON representation of the newly created order.</returns>
    [Description("Create an order")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Post,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves an order by its unique identifier, including line items and stock-reservation status.
    /// </summary>
    /// <param name="id">The unique identifier of the order.</param>
    /// <returns>JSON representation of the order, or an error description if not found.</returns>
    [Description("Get order by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{id}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing order with the values provided in the JSON payload.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized order update request, including the order identifier.</param>
    /// <returns>JSON representation of the updated order.</returns>
    [Description("Update an order")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Put,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes an order by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the order to delete.</param>
    /// <returns>A confirmation message indicating the order was deleted.</returns>
    [Description("Delete an order by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{id}",
            HttpMethod.Delete,
            string.Empty);

        return $"Order {id} deleted successfully.";
    }

    /// <summary>
    /// Gets an order by its order number.
    /// </summary>
    /// <param name="orderNumber">The order number.</param>
    /// <returns>JSON representation of the order, or an error description if not found.</returns>
    [Description("Get order by order number")]
    public async Task<string> GetByOrderNumberAsync(string orderNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Orders,
            $"api/orders/code/{orderNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Searches ERP operational orders using the Orders service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">Search term or JSON query specification for orders.</param>
    /// <returns>JSON paginated search result for matching orders.</returns>
    [Description("Search ERP operational orders by term, order number, or filters")]
    public Task<string> SearchOrdersAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Orders, "api/orders/search", queryJson);
}
