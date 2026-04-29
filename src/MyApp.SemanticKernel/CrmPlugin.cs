using Microsoft.SemanticKernel;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

/// <summary>
/// Semantic Kernel plugin that exposes CRM contact operations to the AI kernel.
/// All functions delegate to the CRM microservice via <see cref="IServiceInvoker"/>,
/// serialising results as JSON strings for consumption by LLM function-calling pipelines.
/// </summary>
public class CrmPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of <see cref="CrmPlugin"/> with the required service invoker.
    /// </summary>
    /// <param name="serviceInvoker">The inter-service HTTP invoker used to call the CRM service.</param>
    public CrmPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new CRM contact from the supplied JSON payload and returns the created resource as JSON.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized contact creation request.</param>
    /// <returns>JSON representation of the newly created CRM contact.</returns>
    [KernelFunction("Create CRM resource")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Crm,
            "api/crm/contacts",
            HttpMethod.Post,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a CRM contact by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the CRM contact.</param>
    /// <returns>JSON representation of the CRM contact, or an error description if not found.</returns>
    [KernelFunction("Get CRM resource by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Crm,
            $"api/crm/contacts/{id}",
            HttpMethod.Get,
            string.Empty);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing CRM contact with the values provided in the JSON payload.
    /// </summary>
    /// <param name="payloadJson">JSON-serialized contact update request, including the contact identifier.</param>
    /// <returns>JSON representation of the updated CRM contact.</returns>
    [KernelFunction("Update CRM resource")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Crm,
            "api/crm/contacts",
            HttpMethod.Put,
            payload);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a CRM contact by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the CRM contact to delete.</param>
    /// <returns>A confirmation message indicating the contact was deleted.</returns>
    [KernelFunction("Delete CRM resource by id")]
    [Description("Deletes a CRM contact by its identifier")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Crm,
            $"api/crm/contacts/{id}",
            HttpMethod.Delete,
            string.Empty);
        return $"CRM contact {id} deleted successfully.";
    }
}
