using MyApp.Agentic.API.Plugins;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin for billing operations including invoice and payment management.
/// </summary>
public class BillingPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the BillingPlugin with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the Billing service.</param>
    public BillingPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new billing resource such as an invoice or payment record.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized billing create request.</param>
    /// <returns>JSON representation of the newly created billing resource.</returns>
    [Description("Create billing resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Billing,
            "api/billing",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a billing resource by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the billing resource.</param>
    /// <returns>JSON representation of the billing resource, or an error description if not found.</returns>
    [Description("Get billing resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing billing resource with new information.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized billing update request.</param>
    /// <returns>JSON representation of the updated billing resource.</returns>
    [Description("Update billing resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Billing,
            "api/billing",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a billing resource (invoice or payment record) by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the billing resource to delete.</param>
    /// <returns>A confirmation message indicating the billing resource was deleted.</returns>
    [Description("Delete billing resource by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"Billing resource {id} deleted successfully.";
    }

    /// <summary>
    /// Gets an invoice by its invoice number.
    /// </summary>
    /// <param name="invoiceNumber">The invoice number.</param>
    /// <returns>JSON representation of the invoice, or an error description if not found.</returns>
    [Description("Get invoice by number")]
    public async Task<string> GetInvoiceByNumberAsync(string invoiceNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/invoices/number/{invoiceNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Gets a payment by its external payment ID.
    /// </summary>
    /// <param name="externalPaymentId">The external payment identifier.</param>
    /// <returns>JSON representation of the payment, or an error description if not found.</returns>
    [Description("Get payment by external payment ID")]
    public async Task<string> GetPaymentByExternalIdAsync(string externalPaymentId)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Billing,
            $"api/billing/payments/external/{externalPaymentId}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Searches ERP invoices using the Billing service <c>/search</c> endpoint.
    /// </summary>
    /// <param name="queryJson">Search term or JSON query specification for invoices.</param>
    /// <returns>JSON paginated search result for matching invoices.</returns>
    [Description("Search ERP invoices by term, invoice number, or filters")]
    public Task<string> SearchInvoicesAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Billing, "api/billing/invoices/search", queryJson);
}
