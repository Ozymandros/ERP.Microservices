using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Leads;

public interface ILeadRepository : IRepository<Lead, Guid>
{
    Task<IEnumerable<Lead>> ListAsync();
}

