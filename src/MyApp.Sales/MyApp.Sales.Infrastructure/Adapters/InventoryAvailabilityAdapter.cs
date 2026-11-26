using MyApp.Sales.Application.Contracts.Ports;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;

namespace MyApp.Sales.Infrastructure.Adapters;

public sealed class InventoryAvailabilityAdapter(IServiceInvoker serviceInvoker) : IInventoryAvailabilityPort
{
    public async Task<ProductAvailabilitySnapshot?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var wire = await serviceInvoker.InvokeAsync<StockAvailabilityWire>(
            ServiceNames.Inventory,
            $"{ApiEndpoints.Inventory.Availability}/{productId}",
            HttpMethod.Get,
            cancellationToken);

        return new ProductAvailabilitySnapshot(
            productId,
            wire.TotalAvailable,
            (wire.WarehouseStocks ?? []).Select(w => new WarehouseAvailabilitySnapshot(
                w.WarehouseId,
                w.WarehouseName ?? "Unknown",
                w.AvailableQuantity)).ToList());
    }

    internal sealed class StockAvailabilityWire
    {
        public int TotalAvailable { get; init; }
        public List<WarehouseStockWire>? WarehouseStocks { get; init; }
    }

    internal sealed class WarehouseStockWire
    {
        public Guid WarehouseId { get; init; }
        public string? WarehouseName { get; init; }
        public int AvailableQuantity { get; init; }
    }
}