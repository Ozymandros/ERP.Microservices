using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Infrastructure.Data.Repositories;

public class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    public ProductRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.SKU == sku);
    }

    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _dbContext.Set<Product>()
            .Where(x => x.QuantityInStock < x.ReorderLevel)
            .ToListAsync();
    }
}
