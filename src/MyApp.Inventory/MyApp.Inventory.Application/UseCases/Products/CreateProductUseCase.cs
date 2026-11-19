using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Internals;
using MyApp.Inventory.Domain.Entities;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.UseCases.Products;

public sealed class CreateProductUseCase(
    IProductRepository productRepository,
    IMapper mapper,
    IInventoryTransactionBoundary boundary)
{
    public async Task<ProductDto> ExecuteAsync(CreateUpdateProductDto dto)
    {
        var existingProduct = await productRepository.GetBySkuAsync(dto.SKU);
        if (existingProduct is not null)
            throw new InvalidOperationException($"Product with SKU '{dto.SKU}' already exists.");

        var product = mapper.Map<Product>(dto);
        var createdProduct = await productRepository.AddAsync(product);
        await boundary.SaveChangesAsync();
        return mapper.Map<ProductDto>(createdProduct);
    }
}