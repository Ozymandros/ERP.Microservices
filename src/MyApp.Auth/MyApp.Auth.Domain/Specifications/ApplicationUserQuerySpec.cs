using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;
using MyApp.Auth.Domain.Entities;

namespace MyApp.Auth.Domain.Specifications;

/// <summary>
/// Specification for querying ApplicationUsers with filtering, sorting, and pagination.
/// 
/// Supported filters:
///   - isActive: Filter by active status (true/false)
///   - email: Filter by email substring match
///   - userName: Filter by username substring match
///   - isExternalLogin: Filter by external login (true/false)
/// 
/// Supported search fields: firstName, lastName, email, userName
/// 
/// Example usage:
/// <code>
/// var querySpec = new ApplicationUserQuerySpec(new QuerySpec
/// {
///     Page = 1,
///     PageSize = 20,
///     SortBy = "createdAt",
///     SortDesc = true,
///     Filters = new Dictionary&lt;string, string&gt;
///     {
///         { "isActive", "true" },
///         { "status", "active" }
///     },
///     SearchTerm = "john",
///     SearchFields = "firstName,lastName,email"
/// });
/// var result = await userRepository.QueryAsync(querySpec);
/// </code>
/// </summary>
public class ApplicationUserQuerySpec : BaseSpecification<ApplicationUser>
{
    /// <summary>
    /// Initializes a new instance of the ApplicationUserQuerySpec class.
    /// </summary>
    /// <param name="query">The query.</param>
    public ApplicationUserQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <summary>
    /// Applies query filters to the specification.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <returns>The filtered queryable source.</returns>
    public override IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query)
    {
        query = ApplyDictionaryFilters(query, Query.Filters);
        return ApplySearchFilter(query, Query.SearchTerm);
    }

    private static IQueryable<ApplicationUser> ApplyDictionaryFilters(
        IQueryable<ApplicationUser> query,
        Dictionary<string, string>? filters)
    {
        filters ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (filters.TryGetValue(nameof(ApplicationUser.IsActive), out var isActiveStr)
            && bool.TryParse(isActiveStr, out var isActive))
            query = query.Where(u => u.IsActive == isActive);

        if (filters.TryGetValue(nameof(ApplicationUser.Email), out var email) && !string.IsNullOrEmpty(email))
            query = query.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase));

        if (filters.TryGetValue(nameof(ApplicationUser.UserName), out var userName) && !string.IsNullOrEmpty(userName))
            query = query.Where(u => u.UserName != null && u.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase));

        if (filters.TryGetValue(nameof(ApplicationUser.IsExternalLogin), out var isExtStr)
            && bool.TryParse(isExtStr, out var isExternal))
            query = query.Where(u => u.IsExternalLogin == isExternal);

        return query;
    }

    private static IQueryable<ApplicationUser> ApplySearchFilter(IQueryable<ApplicationUser> query, string? searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return query;

        return query.Where(u =>
            (u.FirstName != null && u.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (u.LastName != null && u.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
            (u.UserName != null && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
    }
}
