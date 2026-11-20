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
    /// <param name="dbContext">The db Context.</param>
    public ProductRepository(InventoryDbContext dbContext) : base(dbContext)
    {
    }

    /// <summary>Get By Sku Async.</summary>
    /// <param name="sku">The sku.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.SKU == sku, cancellationToken);
    }

    /// <summary>Get By Name Async.</summary>
    /// <param name="name">The name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<Product?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Product>()
            .FirstOrDefaultAsync(x => x.Name == name, cancellationToken);
    }

    /// <summary>Get Low Stock Products Async.</summary>
    public async Task<IEnumerable<Product>> GetLowStockProductsAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<Product>()
            .Where(x => x.QuantityInStock < x.ReorderLevel)
            .ToListAsync(cancellationToken);
    }
}
