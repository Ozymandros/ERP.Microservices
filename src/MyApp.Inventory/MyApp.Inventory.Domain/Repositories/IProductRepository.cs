using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Product Repository.
/// </summary>
public interface IProductRepository : IRepository<Product, Guid>
{
    Task<Product?> GetBySkuAsync(string sku);
    Task<Product?> GetByNameAsync(string name);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}
