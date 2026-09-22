using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Warehouse Repository.
/// </summary>
public interface IWarehouseRepository : IRepository<Warehouse, Guid>
{
    /// <summary>Retrieves a warehouse by its name.</summary>
    /// <param name="name">The warehouse name to search for.</param>
    /// <returns>The matching <see cref="Warehouse"/>, or <c>null</c> if not found.</returns>
    Task<Warehouse?> GetByNameAsync(string name);
}
