using MyApp.Shared.Domain.Repositories;

namespace MyApp.Crm.Domain.Leads;

/// <summary>Repository interface for managing Lead entities.</summary>
public interface ILeadRepository : IRepository<Lead, Guid>
{
    /// <summary>Gets all leads.</summary>
    Task<IEnumerable<Lead>> ListAsync();

    /// <summary>Lead only (no Includes). Use before update/delete/qualify so base Repository.Update works reliably.</summary>
    Task<Lead?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
}

