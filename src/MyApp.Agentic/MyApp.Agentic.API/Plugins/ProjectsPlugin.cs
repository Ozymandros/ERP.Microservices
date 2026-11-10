using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using System.ComponentModel;
using System.Text.Json;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Semantic Kernel plugin that exposes Project management functions to the AI kernel.
/// All functions delegate to the Projects microservice via <see cref="IServiceInvoker"/>,
/// targeting the <c>api/projects</c> endpoint and returning results as JSON strings for
/// LLM function-calling pipelines.
/// </summary>
public class ProjectsPlugin
{
    private readonly IServiceInvoker _serviceInvoker;

    /// <summary>
    /// Initializes a new instance of the ProjectsPlugin class.
    /// </summary>
    /// <param name="serviceInvoker">The service invoker.</param>
    public ProjectsPlugin(IServiceInvoker serviceInvoker)
    {
        _serviceInvoker = serviceInvoker;
    }

    /// <summary>
    /// Creates a new project from the supplied JSON payload and returns the created resource.
    /// </summary>
    /// <param name="payloadJson">The payload json.</param>
    /// <returns>JSON representation of the newly created project.</returns>
    [Description("Create project")]
    public async Task<string> CreateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement?>(payloadJson) ?? default;
        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Projects,
            ApiEndpoints.Projects.Base,
            HttpMethod.Post,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Retrieves a project by its unique identifier.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>JSON representation of the project, or an error description if not found.</returns>
    [Description("Get project by id")]
    public async Task<string> GetByIdAsync(string id)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Projects,
            $"{ApiEndpoints.Projects.Base}/{id}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Updates an existing project with values from the supplied JSON payload.
    /// The payload must include an <c>id</c> property.
    /// </summary>
    /// <param name="payloadJson">The payload json.</param>
    /// <returns>JSON representation of the updated project.</returns>
    [Description("Update project")]
    public async Task<string> UpdateAsync(string payloadJson)
    {
        var payload = JsonSerializer.Deserialize<JsonElement>(payloadJson);
        if (!payload.TryGetProperty("id", out var idElement) || idElement.ValueKind != JsonValueKind.String)
            throw new ArgumentException("Projects update payload must include an 'id' field.", nameof(payloadJson));

        var id = idElement.GetString();
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Projects update payload contains an empty 'id' field.", nameof(payloadJson));

        var result = await _serviceInvoker.InvokeAsync<JsonElement, object>(
            ServiceNames.Projects,
            $"{ApiEndpoints.Projects.Base}/{id}",
            HttpMethod.Put,
            payload);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Permanently deletes a project by its unique identifier.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns>A confirmation message indicating the project was deleted.</returns>
    [Description("Delete project by id")]
    public async Task<string> DeleteAsync(string id)
    {
        await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Projects,
            $"{ApiEndpoints.Projects.Base}/{id}",
            HttpMethod.Delete,
            string.Empty);

        return $"Project {id} deleted successfully.";
    }

    /// <summary>
    /// Gets a project by its project number.
    /// </summary>
    /// <param name="projectNumber">The project number.</param>
    /// <returns>JSON representation of the project, or an error description if not found.</returns>
    [Description("Get project by project number")]
    public async Task<string> GetByNumberAsync(string projectNumber)
    {
        var result = await _serviceInvoker.InvokeAsync<string, object>(
            ServiceNames.Projects,
            $"{ApiEndpoints.Projects.Base}/number/{projectNumber}",
            HttpMethod.Get,
            string.Empty);

        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Searches ERP projects using the Projects service query endpoint.
    /// </summary>
    /// <param name="queryJson">The query json.</param>
    /// <returns>JSON paginated search result for matching projects.</returns>
    [Description("Search ERP projects by term, number, status, customer, or filters")]
    public Task<string> SearchProjectsAsync(string queryJson) =>
        PluginQueryHelper.SearchAsync(_serviceInvoker, ServiceNames.Projects, ApiEndpoints.Projects.Base, queryJson);
}
