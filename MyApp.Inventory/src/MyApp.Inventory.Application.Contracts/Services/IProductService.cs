using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MyApp.Inventory.Application.Contracts.DTOs;

namespace MyApp.Inventory.Application.Contracts.Services;

public interface IProductService
{
    Task<ProductDto?> GetProductByIdAsync(Guid id);
    Task<ProductDto?> GetProductBySkuAsync(string sku);
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductDto>> GetLowStockProductsAsync();
    Task<ProductDto> CreateProductAsync(CreateUpdateProductDto dto);
    Task<ProductDto> UpdateProductAsync(Guid id, CreateUpdateProductDto dto);
    Task DeleteProductAsync(Guid id);
}
