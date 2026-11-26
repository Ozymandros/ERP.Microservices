using AutoMapper;
using MyApp.Inventory.Application.Contracts.DTOs;
using MyApp.Inventory.Application.Internals;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.UseCases.Products;

public sealed class UpdateProductUseCase(
    IProductRepository productRepository,
    IMapper mapper,
    IInventoryTransactionBoundary boundary)
{
    public async Task<ProductDto> ExecuteAsync(Guid id, CreateUpdateProductDto dto)
    {
        var product = await productRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID '{id}' not found.");

        if (product.SKU != dto.SKU)
        {
            var existingProduct = await productRepository.GetBySkuAsync(dto.SKU);
            if (existingProduct is not null)
                throw new InvalidOperationException($"Product with SKU '{dto.SKU}' already exists.");
        }

        mapper.Map(dto, product);
        var updatedProduct = await productRepository.UpdateAsync(product);
        await boundary.SaveChangesAsync();
        return mapper.Map<ProductDto>(updatedProduct);
    }
}