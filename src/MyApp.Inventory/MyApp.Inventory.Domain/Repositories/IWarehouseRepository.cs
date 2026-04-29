using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Warehouse Repository.
/// </summary>
public interface IWarehouseRepository : IRepository<Warehouse, Guid>
{
    Task<Warehouse?> GetByNameAsync(string name);
}
