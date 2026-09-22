using System.Linq.Expressions;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Extensions;

namespace MyApp.Shared.Domain.Specifications;

/// <summary>
/// Base interface for specifications that encapsulate filtering, sorting, and pagination logic.
/// Specifications follow the Specification Pattern from domain-driven design.
/// </summary>
/// <typeparam name="T">The entity type to query</typeparam>
public interface ISpecification<T> where T : class
{
    /// <summary>
    /// Apply filters, sorting, and pagination to a queryable.
    /// </summary>
    /// <param name="query">The base queryable</param>
    /// <returns>The modified queryable with filters, sorting, and pagination applied</returns>
    IQueryable<T> Apply(IQueryable<T> query);

    /// <summary>
    /// Apply only filters to a queryable (useful for counting total items before pagination).
    /// </summary>
    /// <param name="query">The base queryable</param>
    /// <returns>The queryable with filters applied</returns>
    IQueryable<T> ApplyFilters(IQueryable<T> query);
}

/// <summary>
/// Abstract base specification that provides common functionality.
/// </summary>
public abstract class BaseSpecification<T> : ISpecification<T> where T : class
{
    /// <summary>
    /// Query parameters (filters, sorting, pagination)
    /// </summary>
    public QuerySpec Query { get; protected set; }

    /// <summary>Initializes a new <see cref="BaseSpecification{T}"/> with the given query parameters.</summary>
    /// <param name="query">Filtering, sorting, and pagination parameters; uses defaults when <see langword="null"/>.</param>
    protected BaseSpecification(QuerySpec query)
    {
        Query = query ?? new QuerySpec();
        Query.Validate();
    }

    /// <summary>
    /// Apply.
    /// </summary>
    /// <param name="query">The query.</param>
    public virtual IQueryable<T> Apply(IQueryable<T> query)
    {
        query = ApplyFilters(query);
        return ApplyPaginationAndSorting(query);
    }

    /// <summary>
    /// Applies query filters to the specification.
    /// Derived classes MUST override this to add entity-specific filters.
    /// </summary>
    /// <param name="query">The query.</param>
    public virtual IQueryable<T> ApplyFilters(IQueryable<T> query)
    {
        return query; // Default: no filters
    }

    /// <summary>
    /// Apply sorting and pagination.
    /// </summary>
    protected IQueryable<T> ApplyPaginationAndSorting(IQueryable<T> query)
    {
        // Apply sorting if specified
        if (!string.IsNullOrEmpty(Query.SortBy))
        {
            query = query.OrderByDynamic(Query.SortBy, Query.SortDesc);
        }

        // Apply pagination
        var skip = (Query.Page - 1) * Query.PageSize;
        query = query.Skip(skip).Take(Query.PageSize);

        return query;
    }

    /// <summary>
    /// Apply search across multiple fields.
    /// </summary>
    protected IQueryable<T> ApplySearch(IQueryable<T> query, Func<IQueryable<T>, string, IQueryable<T>> searchPredicate)
    {
        if (string.IsNullOrEmpty(Query.SearchTerm))
            return query;

        return searchPredicate(query, Query.SearchTerm);
    }
}

