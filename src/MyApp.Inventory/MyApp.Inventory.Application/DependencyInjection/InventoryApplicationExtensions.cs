using Microsoft.Extensions.DependencyInjection;
using MyApp.Inventory.Application.Internals;
using MyApp.Inventory.Application.UseCases.Products;

namespace MyApp.Inventory.Application.DependencyInjection;

public static class InventoryApplicationExtensions
{
    public static IServiceCollection AddInventoryApplication(this IServiceCollection services)
    {
        services.AddScoped<IInventoryTransactionBoundary, InventoryTransactionBoundary>();
        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<DeleteProductUseCase>();
        return services;
    }
}