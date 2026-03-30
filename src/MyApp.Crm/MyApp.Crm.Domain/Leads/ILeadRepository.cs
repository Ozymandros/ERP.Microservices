using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Leads;

public interface ILeadRepository : IRepository<Lead, Guid>
{
    Task<IEnumerable<Lead>> ListAsync();

    /// <summary>Lead only (no Includes). Use before update/delete/qualify so base Repository.Update works reliably.</summary>
    Task<Lead?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}

