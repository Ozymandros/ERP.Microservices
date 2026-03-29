using Microsoft.Extensions.Primitives;
using MyApp.Shared.Domain.Pagination;

namespace MyApp.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for query specification support in API controllers.
/// These helpers simplify passing query parameters from HTTP requests to specifications.
/// </summary>
public static class QuerySpecExtensions
{
    private static readonly HashSet<string> KnownQuerySpecProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "page", "pagesize", "sortby", "sortdesc", "searchterm", "searchfields", "filters"
    };

    /// <summary>
    /// Extract filters from HTTP query parameters and populate the Filters dictionary.
    /// Supports formats like: ?filters[name]=value&amp;filters[description]=text
    /// or: ?name=value&amp;description=text (direct filter parameters)
    /// Keys are normalized to match property names (case-insensitive matching).
    /// </summary>
    public static QuerySpec BindFiltersFromQuery(this QuerySpec query, IEnumerable<KeyValuePair<string, StringValues>> request)
    {
        // Ensure Filters dictionary exists with case-insensitive comparer
        if (query.Filters == null)
        {
            query.Filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else if (query.Filters.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            // Convert existing dictionary to case-insensitive if needed
            var newFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in query.Filters)
            {
                newFilters[kvp.Key] = kvp.Value;
            }
            query.Filters = newFilters;
        }

        // Extract filters from query string
        // Support both formats:
        // 1. filters[name]=value&filters[description]=text
        // 2. name=value&description=text (direct parameters, excluding known QuerySpec properties)
        foreach (var kvp in request)
        {
            var key = kvp.Key;

            // Skip known QuerySpec properties
            if (KnownQuerySpecProperties.Contains(key))
                continue;

            string filterKey;

            // Handle filters[key]=value format
            if (key.StartsWith("filters[", StringComparison.OrdinalIgnoreCase) && key.EndsWith("]"))
            {
                filterKey = key.Substring(8, key.Length - 9); // Extract key from filters[key]
            }
            // Handle direct filter parameters (e.g., name=value)
            else
            {
                filterKey = key;
            }

            if (!string.IsNullOrEmpty(filterKey) && kvp.Value.Count > 0)
            {
                // Use the key as-is - the dictionary is case-insensitive, so "name", "Name", "NAME" all work
                // The specifications use nameof() which returns exact property names, but case-insensitive lookup handles it
                query.Filters[filterKey] = kvp.Value.ToString();
            }
        }

        query.Validate();
        return query;
    }

    /// <summary>
    /// Convert HTTP query parameters into a QuerySpec object.
    /// Supports binding from [FromQuery] in ASP.NET Core controllers.
    /// </summary>
    public static QuerySpec ToQuerySpec(this QuerySpec query)
    {
        query.Validate();
        return query;
    }

    /// <summary>
    /// Merge additional filters into the query spec.
    /// Useful for applying controller-level or security-based filters.
    /// </summary>
    public static QuerySpec WithFilter(this QuerySpec spec, string key, string value)
    {
        if (spec.Filters == null)
        {
            spec.Filters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        else if (spec.Filters.Comparer != StringComparer.OrdinalIgnoreCase)
        {
            // Convert to case-insensitive if needed
            var newFilters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in spec.Filters)
            {
                newFilters[kvp.Key] = kvp.Value;
            }
            spec.Filters = newFilters;
        }
        spec.Filters[key] = value;
        return spec;
    }

    /// <summary>
    /// Set default sorting if not already specified.
    /// </summary>
    public static QuerySpec WithDefaultSorting(this QuerySpec spec, string sortBy, bool descending = false)
    {
        if (string.IsNullOrEmpty(spec.SortBy))
        {
            spec.SortBy = sortBy;
            spec.SortDesc = descending;
        }
        return spec;
    }

    /// <summary>
    /// Constrain the maximum page size for security/performance.
    /// </summary>
    public static QuerySpec WithMaxPageSize(this QuerySpec spec, int maxSize)
    {
        if (spec.PageSize > maxSize)
            spec.PageSize = maxSize;
        return spec;
    }
}

/// <summary>
/// Controller extension for building paginated responses with OpenAPI documentation.
/// </summary>
public static class PaginatedResponseExtensions
{
    /// <summary>
    /// Format a paginated result for response, including metadata for frontend pagination controls.
    /// </summary>
    public static object ToPaginatedResponse<T>(
        this PaginatedResult<T> result,
        int page,
        int pageSize)
    {
        var totalPages = (int)Math.Ceiling((double)result.TotalCount / pageSize);
        return new
        {
            items = result.Items,
            pagination = new
            {
                page,
                pageSize,
                total = result.TotalCount,
                totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            }
        };
    }
}
