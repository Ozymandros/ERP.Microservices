using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Activities;

public interface IActivityRepository : IRepository<Activity, Guid>
{
    Task<IEnumerable<Activity>> ListAsync();
}

