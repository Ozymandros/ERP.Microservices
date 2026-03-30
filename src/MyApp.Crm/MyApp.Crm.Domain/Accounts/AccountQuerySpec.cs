using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Accounts;

public sealed class AccountQuerySpec : BaseSpecification<Account>
{
    public AccountQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Account> ApplyFilters(IQueryable<Account> queryable)
    {
        if (Query.Filters is { Count: > 0 })
        {
            foreach (var (key, value) in Query.Filters)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                var normalized = value.Trim();

                switch (key.Trim().ToLowerInvariant())
                {
                    case "name":
                        queryable = queryable.Where(a => a.Name.Contains(normalized));
                        break;
                    case "ownerusername":
                    case "owner":
                        queryable = queryable.Where(a => a.OwnerUsername != null && a.OwnerUsername == normalized);
                        break;
                    case "customerid":
                        if (Guid.TryParse(normalized, out var customerId))
                            queryable = queryable.Where(a => a.CustomerId == customerId);
                        break;
                    case "isactive":
                        if (bool.TryParse(normalized, out var isActive))
                            queryable = queryable.Where(a => a.IsActive == isActive);
                        break;
                }
            }
        }

        queryable = ApplySearch(queryable, (q, term) =>
            q.Where(a => a.Name.Contains(term)));

        return queryable;
    }
}

