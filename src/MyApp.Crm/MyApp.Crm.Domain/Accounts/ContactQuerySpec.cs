using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Accounts;

/// <summary>Specification for querying and filtering contacts.</summary>
public sealed class ContactQuerySpec : BaseSpecification<Contact>
{
    /// <summary>Initializes a new instance of the ContactQuerySpec class.</summary>
    public ContactQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Contact> ApplyFilters(IQueryable<Contact> queryable)
    {
        if (Query.Filters is { Count: > 0 })
        {
            foreach (var (key, value) in Query.Filters)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                var normalized = value.Trim();

                switch (key.Trim().ToLowerInvariant())
                {
                    case "accountid":
                        if (Guid.TryParse(normalized, out var accountId))
                            queryable = queryable.Where(c => c.AccountId == accountId);
                        break;
                    case "isactive":
                        if (bool.TryParse(normalized, out var isActive))
                            queryable = queryable.Where(c => c.IsActive == isActive);
                        break;
                    case "isprimary":
                        if (bool.TryParse(normalized, out var isPrimary))
                            queryable = queryable.Where(c => c.IsPrimary == isPrimary);
                        break;
                }
            }
        }

        queryable = ApplySearch(queryable, (q, term) =>
            q.Where(c => c.FullName.Contains(term) || (c.Email != null && c.Email.Contains(term))));

        return queryable;
    }
}

