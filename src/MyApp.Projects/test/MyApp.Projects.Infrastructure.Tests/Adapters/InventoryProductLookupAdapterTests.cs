using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Projects.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Projects.Infrastructure.Tests.Adapters;

public class InventoryProductLookupAdapterTests
{
    [Fact]
    public async Task GetProductAsync_MapsWireToSnapshot()
    {
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryProductLookupAdapter.ProductWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Products}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryProductLookupAdapter.ProductWire
            {
                Id = productId,
                Name = "Widget",
                SKU = "W-1",
                UnitPrice = 9.99m
            });

        var sut = new InventoryProductLookupAdapter(invoker.Object, NullLogger<InventoryProductLookupAdapter>.Instance);
        var result = await sut.GetProductAsync(productId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Name.Should().Be("Widget");
        result.Sku.Should().Be("W-1");
        result.UnitPrice.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetProductAsync_NullFields_MapToEmptyStrings()
    {
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryProductLookupAdapter.ProductWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Products}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InventoryProductLookupAdapter.ProductWire
            {
                Id = productId,
                Name = null,
                SKU = null,
                UnitPrice = 0m
            });

        var sut = new InventoryProductLookupAdapter(invoker.Object, NullLogger<InventoryProductLookupAdapter>.Instance);
        var result = await sut.GetProductAsync(productId);

        result!.Name.Should().BeEmpty();
        result.Sku.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductAsync_NotFound_ReturnsNull()
    {
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryProductLookupAdapter.ProductWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Products}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("wrap", new HttpRequestException("missing", null, HttpStatusCode.NotFound)));

        var sut = new InventoryProductLookupAdapter(invoker.Object, NullLogger<InventoryProductLookupAdapter>.Instance);
        var result = await sut.GetProductAsync(productId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductAsync_OtherError_ThrowsInvalidOperationException()
    {
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<InventoryProductLookupAdapter.ProductWire>(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Products}/{productId}",
                HttpMethod.Get,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("down"));

        var sut = new InventoryProductLookupAdapter(invoker.Object, NullLogger<InventoryProductLookupAdapter>.Instance);
        var act = async () => await sut.GetProductAsync(productId);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*{productId}*");
    }
}
