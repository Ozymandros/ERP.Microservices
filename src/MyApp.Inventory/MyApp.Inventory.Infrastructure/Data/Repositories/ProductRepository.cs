using Microsoft.EntityFrameworkCore;
using MyApp.Shared.Infrastructure.Repositories;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Infrastructure.Data.Repositories;

/// <summary>
/// Provides Product Repository functionality.
/// </summary>
public class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    /// <summary>base.</summary>
    public ProductRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Sku Async.</summary>
    public async Task<Product?> GetBySkuAsync(string sku)
    {
        return await _dbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.SKU == sku);
    }

    /// <summary>Get By Name Async.</summary>
    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _dbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.Name == name);
    }

    /// <summary>Get Low Stock Products Async.</summary>
    public async Task<IEnumerable<Product>> GetLowStockProductsAsync()
    {
        return await _dbContext.Set<Product>()
            .Where(x => x.QuantityInStock < x.ReorderLevel)
            .ToListAsync();
    }
}
