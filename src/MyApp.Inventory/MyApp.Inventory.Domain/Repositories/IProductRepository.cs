using MyApp.Shared.Domain.Repositories;
using MyApp.Inventory.Domain.Entities;

namespace MyApp.Inventory.Domain.Repositories;

/// <summary>
/// Defines the contract for I Product Repository.
/// </summary>
public interface IProductRepository : IRepository<Product, Guid>
{
    /// <summary>Retrieves a product by its SKU.</summary>
    /// <param name="sku">The stock-keeping unit identifier to search for.</param>
    /// <returns>The matching <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetBySkuAsync(string sku);

    /// <summary>Retrieves a product by its name.</summary>
    /// <param name="name">The product name to search for.</param>
    /// <returns>The matching <see cref="Product"/>, or <c>null</c> if not found.</returns>
    Task<Product?> GetByNameAsync(string name);

    /// <summary>Retrieves all products whose quantity in stock is below their reorder level.</summary>
    /// <returns>A collection of low-stock <see cref="Product"/> entities.</returns>
    Task<IEnumerable<Product>> GetLowStockProductsAsync();
}
