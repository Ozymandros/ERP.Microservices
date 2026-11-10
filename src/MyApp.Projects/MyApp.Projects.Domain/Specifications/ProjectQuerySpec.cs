using MyApp.Projects.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Projects.Domain.Specifications;

/// <summary>Query specification for projects (filters: Status, CustomerId, CurrencyCode, ProjectManagerUserId; search: number/description/reference).</summary>
public class ProjectQuerySpec : BaseSpecification<Project>
{
    /// <summary>Initializes a new instance of the <see cref="ProjectQuerySpec"/> class.</summary>
    /// <param name="query">The query.</param>
    public ProjectQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <inheritdoc />
    public override IQueryable<Project> ApplyFilters(IQueryable<Project> query)
    {
        var filters = Query.Filters;
        if (filters is not null)
        {
            if (filters.TryGetValue(nameof(Project.Status), out var status)
                && Enum.TryParse<ProjectStatus>(status, true, out var parsedStatus))
                query = query.Where(p => p.Status == parsedStatus);

            if (filters.TryGetValue(nameof(Project.CustomerId), out var customer) && Guid.TryParse(customer, out var customerId))
                query = query.Where(p => p.CustomerId == customerId);

            if (filters.TryGetValue(nameof(Project.ProjectManagerUserId), out var manager) && Guid.TryParse(manager, out var managerId))
                query = query.Where(p => p.ProjectManagerUserId == managerId);

            if (filters.TryGetValue(nameof(Project.CurrencyCode), out var currency) && !string.IsNullOrWhiteSpace(currency))
                query = query.Where(p => p.CurrencyCode == currency.ToUpper());

            if (filters.TryGetValue(nameof(Project.ProjectNumber), out var number) && !string.IsNullOrWhiteSpace(number))
                query = query.Where(p => p.ProjectNumber.Contains(number));
        }

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(p =>
                p.ProjectNumber.Contains(term) ||
                p.Description.Contains(term) ||
                (p.ExternalReference != null && p.ExternalReference.Contains(term)));
        }

        return query;
    }
}

/// <summary>Query specification for resources (filters: Type, ResourceGroupId, Blocked; search: number/name).</summary>
public class ResourceQuerySpec : BaseSpecification<Resource>
{
    /// <summary>Initializes a new instance of the <see cref="ResourceQuerySpec"/> class.</summary>
    /// <param name="query">The query.</param>
    public ResourceQuerySpec(QuerySpec query) : base(query)
    {
    }

    /// <inheritdoc />
    public override IQueryable<Resource> ApplyFilters(IQueryable<Resource> query)
    {
        var filters = Query.Filters;
        if (filters is not null)
        {
            if (filters.TryGetValue(nameof(Resource.Type), out var type) && Enum.TryParse<ResourceType>(type, true, out var parsedType))
                query = query.Where(r => r.Type == parsedType);

            if (filters.TryGetValue(nameof(Resource.ResourceGroupId), out var group) && Guid.TryParse(group, out var groupId))
                query = query.Where(r => r.ResourceGroupId == groupId);

            if (filters.TryGetValue(nameof(Resource.Blocked), out var blocked) && bool.TryParse(blocked, out var isBlocked))
                query = query.Where(r => r.Blocked == isBlocked);
        }

        if (!string.IsNullOrWhiteSpace(Query.SearchTerm))
        {
            var term = Query.SearchTerm;
            query = query.Where(r => r.ResourceNo.Contains(term) || r.Name.Contains(term));
        }

        return query;
    }
}
