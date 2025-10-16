using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

public interface IWarehouseRepository : IRepository<Warehouse, Guid>
{
    Task<Warehouse?> GetByNameAsync(string name);
}
