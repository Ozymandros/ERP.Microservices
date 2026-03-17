using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Opportunities;

public interface IOpportunityRepository : IRepository<Opportunity, Guid>
{
    Task<IEnumerable<Opportunity>> ListAsync();
}

