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
    public ApplicationUserQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <summary>
    /// Apply user-specific filters.
    /// </summary>
    public override IQueryable<ApplicationUser> ApplyFilters(IQueryable<ApplicationUser> query)
    {
        // Apply filters based on the Filters dictionary (case-insensitive key matching)
        var filters = Query.Filters ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        if (filters.TryGetValue(nameof(ApplicationUser.IsActive), out var isActiveStr))
        {
            if (bool.TryParse(isActiveStr, out var isActive))
            {
                query = query.Where(u => u.IsActive == isActive);
            }
        }

        if (filters.TryGetValue(nameof(ApplicationUser.Email), out var email) && !string.IsNullOrEmpty(email))
        {
            query = query.Where(u => u.Email != null && u.Email.Contains(email, StringComparison.OrdinalIgnoreCase));
        }

        if (filters.TryGetValue(nameof(ApplicationUser.UserName), out var userName) && !string.IsNullOrEmpty(userName))
        {
            query = query.Where(u => u.UserName != null && u.UserName.Contains(userName, StringComparison.OrdinalIgnoreCase));
        }

        if (filters.TryGetValue(nameof(ApplicationUser.IsExternalLogin), out var isExtStr))
        {
            if (bool.TryParse(isExtStr, out var isExternal))
            {
                query = query.Where(u => u.IsExternalLogin == isExternal);
            }
        }

        // Apply search across multiple fields
        if (!string.IsNullOrEmpty(Query.SearchTerm))
        {
            var searchTerm = Query.SearchTerm;
            query = query.Where(u =>
                (u.FirstName != null && u.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (u.LastName != null && u.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                (u.UserName != null && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            );
        }

        return query;
    }
}
