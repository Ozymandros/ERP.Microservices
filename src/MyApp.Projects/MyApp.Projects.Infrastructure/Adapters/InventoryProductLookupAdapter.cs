using Microsoft.Extensions.Logging;
using MyApp.Projects.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Projects.Infrastructure.Adapters;

public sealed class InventoryProductLookupAdapter(
    IServiceInvoker serviceInvoker,
    ILogger<InventoryProductLookupAdapter> logger) : IInventoryProductLookupPort
{
    public async Task<InventoryProductSnapshot?> GetProductAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        try
        {
            var wire = await serviceInvoker.InvokeAsync<ProductWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Products}/{productId}",
                HttpMethod.Get,
                cancellationToken);
            return new InventoryProductSnapshot(
                wire.Id,
                wire.Name ?? string.Empty,
                wire.SKU ?? string.Empty,
                wire.UnitPrice);
        }
        catch (Exception ex) when (ServiceInvocationErrors.IsNotFound(ex))
        {
            logger.LogWarning("Product {ProductId} not found in Inventory", productId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"Product {productId} could not be validated against the Inventory service.", ex);
        }
    }

    internal sealed class ProductWire
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? SKU { get; init; }
        public decimal UnitPrice { get; init; }
    }
}