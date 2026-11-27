using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Specification for querying and filtering contacts.</summary>
public sealed class ContactQuerySpec : BaseSpecification<Contact>
{
    /// <summary>Initializes a new instance of the ContactQuerySpec class.</summary>
    /// <param name="query">The query.</param>
    public ContactQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <summary>Applies filter criteria and search term from the query specification to the contact queryable.</summary>
    /// Applies query filters to the specification.
    /// <param name="queryable">The queryable.</param>
    /// <returns>The filtered queryable with all matching conditions applied.</returns>
    public override IQueryable<Contact> ApplyFilters(IQueryable<Contact> queryable)
    {
        if (Query.Filters is { Count: > 0 })
        {
            foreach (var (key, value) in Query.Filters)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                queryable = ApplyContactFilter(queryable, key, value.Trim());
            }
        }

        queryable = ApplySearch(queryable, (q, term) =>
            q.Where(c => c.FullName.Contains(term) || (c.Email != null && c.Email.Contains(term))));

        return queryable;
    }

    private static IQueryable<Contact> ApplyContactFilter(IQueryable<Contact> queryable, string key, string normalized) =>
        key.Trim().ToLowerInvariant() switch
        {
            "accountid" when Guid.TryParse(normalized, out var accountId)
                => queryable.Where(c => c.AccountId == accountId),
            "isactive" when bool.TryParse(normalized, out var isActive)
                => queryable.Where(c => c.IsActive == isActive),
            "isprimary" when bool.TryParse(normalized, out var isPrimary)
                => queryable.Where(c => c.IsPrimary == isPrimary),
            _ => queryable
        };
}

