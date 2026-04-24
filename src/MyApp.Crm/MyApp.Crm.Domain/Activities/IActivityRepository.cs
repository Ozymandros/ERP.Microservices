using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Activities;

/// <summary>Repository interface for managing Activity entities.</summary>
public interface IActivityRepository : IRepository<Activity, Guid>
{
    /// <summary>Gets all activities.</summary>
    Task<IEnumerable<Activity>> ListAsync();
}

