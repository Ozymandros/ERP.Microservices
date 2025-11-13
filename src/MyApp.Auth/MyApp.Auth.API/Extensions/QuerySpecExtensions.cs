using Microsoft.AspNetCore.Mvc;
using MyApp.Shared.Domain.Pagination;
using MyApp.Auth.Application.Contracts.DTOs;

namespace MyApp.Auth.API.Extensions;

/// <summary>
/// Extension methods for query specification support in API controllers.
/// These helpers simplify passing query parameters from HTTP requests to specifications.
/// </summary>
public static class QuerySpecExtensions
{
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
        spec.Filters ??= new Dictionary<string, string>();
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
            data = result.Items,
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
