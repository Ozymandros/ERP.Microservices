using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Leads;

public class LeadQuerySpec : BaseSpecification<Lead>
{
    public LeadQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Lead> ApplyFilters(IQueryable<Lead> query)
    {
        if (Query.Filters?.TryGetValue(nameof(Lead.Title), out var title) == true && !string.IsNullOrWhiteSpace(title))
            query = query.Where(l => l.Title.Contains(title, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Lead.Source), out var source) == true && !string.IsNullOrWhiteSpace(source))
            query = query.Where(l => l.Source != null && l.Source.Contains(source, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Lead.OwnerUsername), out var owner) == true && !string.IsNullOrWhiteSpace(owner))
            query = query.Where(l => l.OwnerUsername.Contains(owner, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Lead.Status), out var status) == true && !string.IsNullOrWhiteSpace(status))
            query = query.Where(l => l.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(l =>
                l.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (l.Source != null && l.Source.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (l.ContactName != null && l.ContactName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (l.ContactEmail != null && l.ContactEmail.Contains(term, StringComparison.OrdinalIgnoreCase)));
        }

        return query;
    }
}

