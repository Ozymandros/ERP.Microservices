using FluentAssertions;
using Moq;
using MyApp.Sales.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Sales.Infrastructure.Tests.Adapters;

public class InventoryAvailabilityAdapterTests
{
    [Fact]
    public async Task GetByProductIdAsync_MapsWireToSnapshot()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryAvailabilityAdapter.StockAvailabilityWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Availability}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryAvailabilityAdapter.StockAvailabilityWire
            {
                TotalAvailable = 12,
                WarehouseStocks =
                [
                    new InventoryAvailabilityAdapter.WarehouseStockWire
                    {
                        WarehouseId = warehouseId,
                        WarehouseName = "Main",
                        AvailableQuantity = 12
                    }
                ]
            });

        var sut = new InventoryAvailabilityAdapter(invoker.Object);
        var result = await sut.GetByProductIdAsync(productId);

        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.TotalAvailable.Should().Be(12);
        result.WarehouseStocks.Should().ContainSingle(w =>
            w.WarehouseId == warehouseId && w.WarehouseName == "Main" && w.AvailableQuantity == 12);
    }

    [Fact]
    public async Task GetByProductIdAsync_NullWarehouseStocks_UsesEmptyListAndUnknownName()
    {
        var productId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryAvailabilityAdapter.StockAvailabilityWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Availability}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryAvailabilityAdapter.StockAvailabilityWire
            {
                TotalAvailable = 3,
                WarehouseStocks =
                [
                    new InventoryAvailabilityAdapter.WarehouseStockWire
                    {
                        WarehouseId = warehouseId,
                        WarehouseName = null,
                        AvailableQuantity = 3
                    }
                ]
            });

        var sut = new InventoryAvailabilityAdapter(invoker.Object);
        var result = await sut.GetByProductIdAsync(productId);

        result!.WarehouseStocks.Should().ContainSingle(w => w.WarehouseName == "Unknown");
    }

    [Fact]
    public async Task GetByProductIdAsync_NullWarehouseCollection_ReturnsEmptyStocks()
    {
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryAvailabilityAdapter.StockAvailabilityWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Availability}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryAvailabilityAdapter.StockAvailabilityWire
            {
                TotalAvailable = 0,
                WarehouseStocks = null
            });

        var sut = new InventoryAvailabilityAdapter(invoker.Object);
        var result = await sut.GetByProductIdAsync(productId);

        result!.WarehouseStocks.Should().BeEmpty();
    }
}
