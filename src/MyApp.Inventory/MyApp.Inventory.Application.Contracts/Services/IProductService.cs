using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Domain.Entities;
using MyApp.Shared.Domain.Pagination;
using MyApp.Shared.Domain.Specifications;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IProductService
{
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<PaginatedResult<ProductDto>> GetAllProductsPaginatedAsync(int pageNumber, int pageSize);
    Task<PaginatedResult<ProductDto>> QueryProductsAsync(ISpecification<Product> spec);
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
    Task<ProductDto> CreateProductAsync(CreateUpdateProductDto dto);
    Task<ProductDto> UpdateProductAsync(Guid id, CreateUpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
}
