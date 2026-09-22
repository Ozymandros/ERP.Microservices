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
    /// <summary>Returns all entities as an enumerable sequence.</summary>
    /// <returns>All entities of type <typeparamref name="TEntity"/>.</returns>
    Task<IEnumerable<TEntity>> ListAsync();

    /// <summary>Deletes the entity with the given primary key.</summary>
    /// <param name="id">The primary key of the entity to delete.</param>
    Task DeleteAsync(TKey id);
}
