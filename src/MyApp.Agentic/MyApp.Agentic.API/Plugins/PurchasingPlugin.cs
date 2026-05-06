using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin that exposes Purchase Order management functions to the AI kernel.
/// All functions delegate to the Purchasing microservice via <see cref="IServiceInvoker"/>,
/// targeting the <c>api/purchasing/purchaseorders</c> endpoint and returning results as
/// JSON strings for LLM function-calling pipelines.
/// </summary>
public class PurchasingPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of <see cref="PurchasingPlugin"/> with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the Purchasing service.</param>
    public PurchasingPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new purchase order from the supplied JSON payload and returns the created resource.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized purchase order creation request including supplier and line items.</param>
    /// <returns>JSON representation of the newly created purchase order.</returns>
    [Description("Create purchasing resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Purchasing,
            "api/purchasing/purchaseorders",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a purchase order by its unique identifier, including line items and receiving status.
    /// </summary>
    /// <param name="id">The unique identifier of the purchase order.</param>
    /// <returns>JSON representation of the purchase order, or an error description if not found.</returns>
    [Description("Get purchasing resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/purchaseorders/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing purchase order with the values provided in the JSON payload.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized purchase order update request, including the order identifier.</param>
    /// <returns>JSON representation of the updated purchase order.</returns>
    [Description("Update purchasing resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Purchasing,
            "api/purchasing/purchaseorders",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a purchase order by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the purchase order to delete.</param>
    /// <returns>A confirmation message indicating the purchase order was deleted.</returns>
    [Description("Delete purchasing resource by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/purchaseorders/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Purchasing resource {id} deleted successfully.";
    }

    /// <summary>
    /// Gets a supplier by its name.
    /// </summary>
    /// <param name="name">The supplier name.</param>
    /// <returns>JSON representation of the supplier, or an error description if not found.</returns>
    [Description("Get supplier by name")]
    public async Task<string> GetSupplierByNameAsync(string name)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/suppliers/name/{name}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a purchase order by its order number.
    /// </summary>
    /// <param name="orderNumber">The purchase order number.</param>
    /// <returns>JSON representation of the purchase order, or an error description if not found.</returns>
    [Description("Get purchase order by order number")]
    public async Task<string> GetPurchaseOrderByCodeAsync(string orderNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Purchasing,
            $"api/purchasing/purchaseorders/code/{orderNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }
}
