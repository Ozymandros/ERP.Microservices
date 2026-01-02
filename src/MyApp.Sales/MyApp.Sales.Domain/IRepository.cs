using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Shared.Domain.Repositories;

namespace MyApp.Sales.Domain;

/// <summary>
/// Sales-specific repository abstraction that extends the shared repository contract
/// with convenience members used across the Sales service.
/// </summary>
public interface IRepository<TEntity, TKey> : MyApp.Shared.Domain.Repositories.IRepository<TEntity, TKey>
    where TEntity : class
{
    Task<IEnumerable<TEntity>> ListAsync();
    Task DeleteAsync(TKey id);
}
