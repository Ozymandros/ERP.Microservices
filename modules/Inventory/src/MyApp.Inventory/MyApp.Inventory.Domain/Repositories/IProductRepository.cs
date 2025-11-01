using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

public interface IProductRepository : IRepository<Product, Guid>
{
    Task<Product?> GetBySkuAsync(string sku);
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}
