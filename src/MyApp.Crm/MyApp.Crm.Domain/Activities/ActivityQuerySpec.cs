using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Crm.Domain.Activities;

public class ActivityQuerySpec : BaseSpecification<Activity>
{
    public ActivityQuerySpec(QuerySpec query) : base(query)
    {
    }

    public override IQueryable<Activity> ApplyFilters(IQueryable<Activity> query)
    {
        if (Query.Filters?.TryGetValue(nameof(Activity.AssignedToUsername), out var assigned) == true && !string.IsNullOrWhiteSpace(assigned))
            query = query.Where(a => a.AssignedToUsername.Contains(assigned, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Activity.Status), out var status) == true && !string.IsNullOrWhiteSpace(status))
            query = query.Where(a => a.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Activity.Type), out var type) == true && !string.IsNullOrWhiteSpace(type))
            query = query.Where(a => a.Type.ToString().Equals(type, StringComparison.OrdinalIgnoreCase));

        if (Query.Filters?.TryGetValue(nameof(Activity.CustomerId), out var customerId) == true &&
            Guid.TryParse(customerId, out var customerGuid))
        {
            query = query.Where(a => a.CustomerId == customerGuid);
        }

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(a => a.Subject.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        return query;
    }
}

