using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Contracts.Services;

/// <summary>
/// Defines the contract for I Product Service.
/// </summary>
public interface IProductService
{
    /// <summary>Retrieves a product by its unique identifier.</summary>
    /// <param name="id">The unique identifier of the product.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    Task<ProductDto?> GetProductByIdAsync(Guid id);

    /// <summary>Retrieves a product by its SKU.</summary>
    /// <param name="sku">The stock-keeping unit identifier to search for.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    Task<ProductDto?> GetProductBySkuAsync(string sku);

    /// <summary>Retrieves a product by its name.</summary>
    /// <param name="name">The product name to search for.</param>
    /// <returns>The matching <see cref="ProductDto"/>, or <c>null</c> if not found.</returns>
    Task<ProductDto?> GetProductByNameAsync(string name);

    /// <summary>Retrieves all products in the inventory.</summary>
    /// <returns>A collection of all <see cref="ProductDto"/> records.</returns>
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();

    /// <summary>Retrieves a paginated list of all products.</summary>
    /// <param name="pageNumber">The one-based page number to return.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A <see cref="PaginatedResult{ProductDto}"/> for the requested page.</returns>
    Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(int pageNumber, int pageSize);

    /// <summary>Queries products using a specification that encapsulates filtering, sorting and pagination.</summary>
    /// <param name="spec">The specification to apply.</param>
    /// <returns>A <see cref="PaginatedResult{ProductDto}"/> matching the specification.</returns>
    Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec);

    /// <summary>Retrieves all products whose stock level is below their configured reorder level.</summary>
    /// <returns>A collection of low-stock <see cref="ProductDto"/> records.</returns>
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();

    /// <summary>Creates a new product from the supplied data.</summary>
    /// <param name="dto">The data used to create the product.</param>
    /// <returns>The newly created <see cref="ProductDto"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a product with the same SKU already exists.</exception>
    Task<ProductDto> CreateProductAsync(CreateUpdateProductDto dto);

    /// <summary>Updates an existing product with the supplied data.</summary>
    /// <param name="id">The unique identifier of the product to update.</param>
    /// <param name="dto">The updated product data.</param>
    /// <returns>The updated <see cref="ProductDto"/>.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when no product with the given identifier is found.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the new SKU is already in use by another product.</exception>
    Task<ProductDto> UpdateProductAsync(Guid id, CreateUpdateProductDto dto);

    /// <summary>Deletes the product with the specified identifier.</summary>
    /// <param name="id">The unique identifier of the product to delete.</param>
    /// <exception cref="KeyNotFoundException">Thrown when no product with the given identifier is found.</exception>
    Task DeleteProductAsync(Guid id);
}
