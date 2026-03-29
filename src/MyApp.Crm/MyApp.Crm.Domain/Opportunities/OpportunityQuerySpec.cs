using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Opportunities;

public class OpportunityQuerySpec : BaseSpecification<Opportunity>
{
    public OpportunityQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Opportunity> ApplyFilters(IQueryable<Opportunity> query)
    {
        if (Query.Filters?.TryGetValue(nameof(Opportunity.CustomerId), out var customerId) == true &&
            Guid.TryParse(customerId, out var customerGuid))
        {
            query = query.Where(o => o.CustomerId == customerGuid);
        }

        if (Query.Filters?.TryGetValue(nameof(Opportunity.OwnerUsername), out var owner) == true && !string.IsNullOrWhiteSpace(owner))
            query = query.Where(o => o.OwnerUsername.Contains(owner, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Opportunity.Stage), out var stage) == true && !string.IsNullOrWhiteSpace(stage))
            query = query.Where(o => o.Stage.ToString().Equals(stage, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(o => o.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }
}

