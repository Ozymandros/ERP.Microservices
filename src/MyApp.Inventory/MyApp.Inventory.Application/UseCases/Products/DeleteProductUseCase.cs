using MyApp.Inventory.Application.Internals;
using MyApp.Inventory.Domain.Repositories;

namespace MyApp.Inventory.Application.UseCases.Products;

public sealed class DeleteProductUseCase(
    IProductRepository productRepository,
    IInventoryTransactionBoundary boundary)
{
    public async Task ExecuteAsync(Guid id)
    {
        var product = await productRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Product with ID '{id}' not found.");

        await productRepository.DeleteAsync(product);
        await boundary.SaveChangesAsync();
    }
}