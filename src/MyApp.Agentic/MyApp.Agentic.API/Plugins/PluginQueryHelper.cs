using System.Text.Json;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Agentic.API.Plugins;

/// <summary>
/// Shared helpers for building ERP search query strings and normalizing LLM tool arguments.
/// </summary>
internal static class PluginQueryHelper
{
    /// <summary>
    /// Invokes an ERP microservice search endpoint and returns the JSON-serialized response.
    /// </summary>
    /// <param name="serviceInvoker">Dapr-backed service invoker used for inter-service calls.</param>
    /// <param name="serviceName">Target microservice app id (for example <c>inventory-service</c>).</param>
    /// <param name="searchEndpointPath">Relative search path (for example <c>api/inventory/products/search</c>).</param>
    /// <param name="queryJson">Plain search term or JSON <see cref="ParseQueryParameters"/> input.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>JSON string containing the search result payload.</returns>
    public static async Task<string> SearchAsync(
        IServiceInvoker serviceInvoker,
        string serviceName,
        string searchEndpointPath,
        string queryJson,
        CancellationToken cancellationToken = default)
    {
        var queryParams = ParseQueryParameters(queryJson);
        var request = serviceInvoker.CreateRequest(
            serviceName,
            searchEndpointPath,
            HttpMethod.Get,
            queryParams: queryParams);

        var result = await serviceInvoker.InvokeAsync<object>(request, cancellationToken);
        return JsonSerializer.Serialize(result);
    }

    /// <summary>
    /// Builds query-string parameters for ERP <c>/search</c> endpoints from JSON or a plain search term.
    /// </summary>
    /// <param name="args">
    /// Plain text search term, or JSON containing <c>searchTerm</c>, <c>searchFields</c>, <c>filters</c>,
    /// <c>name</c>, <c>description</c>, pagination, and sorting properties.
    /// </param>
    /// <returns>Query parameters ready to append to a GET search request.</returns>
    public static Dictionary<string, string?> ParseQueryParameters(string? args)
    {
        var queryParams = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(args))
            return queryParams;

        var trimmed = args.Trim();
        if (!trimmed.StartsWith('{'))
        {
            queryParams["searchTerm"] = trimmed;
            queryParams["page"] = "1";
            queryParams["pageSize"] = "20";
            return queryParams;
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return queryParams;

        AddIfPresent(queryParams, root, "page");
        AddIfPresent(queryParams, root, "pageSize");
        AddIfPresent(queryParams, root, "sortBy");
        AddIfPresent(queryParams, root, "sortDesc");
        AddIfPresent(queryParams, root, "searchTerm");
        AddIfPresent(queryParams, root, "searchFields");

        if (root.TryGetProperty("filters", out var filters) && filters.ValueKind == JsonValueKind.Object)
        {
            foreach (var filter in filters.EnumerateObject())
            {
                if (!string.IsNullOrWhiteSpace(filter.Name) && filter.Value.ValueKind == JsonValueKind.String)
                    queryParams[filter.Name] = filter.Value.GetString();
            }
        }

        if (!queryParams.ContainsKey("searchTerm")
            && root.TryGetProperty("name", out var nameElement)
            && nameElement.ValueKind == JsonValueKind.String)
        {
            queryParams["name"] = nameElement.GetString();
        }

        if (!queryParams.ContainsKey("searchTerm")
            && root.TryGetProperty("description", out var descriptionElement)
            && descriptionElement.ValueKind == JsonValueKind.String)
        {
            queryParams["description"] = descriptionElement.GetString();
        }

        if (!queryParams.ContainsKey("page"))
            queryParams["page"] = "1";

        if (!queryParams.ContainsKey("pageSize"))
            queryParams["pageSize"] = "20";

        return queryParams;
    }

    /// <summary>
    /// Resolves a scalar tool argument from either a plain string or a JSON object supplied by the LLM.
    /// </summary>
    /// <param name="args">Raw tool argument text.</param>
    /// <param name="jsonPropertyName">Preferred JSON property name when <paramref name="args"/> is an object.</param>
    /// <returns>Resolved scalar value suitable for route parameters or exact lookups.</returns>
    public static string ResolveScalarArgument(string? args, string jsonPropertyName = "name")
    {
        if (string.IsNullOrWhiteSpace(args))
            return string.Empty;

        var trimmed = args.Trim().Trim('"');
        if (!trimmed.StartsWith('{'))
            return trimmed;

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
            return trimmed;

        if (root.TryGetProperty(jsonPropertyName, out var preferred) && preferred.ValueKind == JsonValueKind.String)
            return preferred.GetString() ?? string.Empty;

        if (root.TryGetProperty("value", out var value) && value.ValueKind == JsonValueKind.String)
            return value.GetString() ?? string.Empty;

        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString() ?? string.Empty;
        }

        return trimmed;
    }

    /// <summary>
    /// Copies a JSON property into the query parameter dictionary when present.
    /// </summary>
    /// <param name="target">Destination query parameter map.</param>
    /// <param name="root">Source JSON object.</param>
    /// <param name="propertyName">Property name to copy.</param>
    private static void AddIfPresent(Dictionary<string, string?> target, JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
            return;

        target[propertyName] = value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => null
        };
    }
}
