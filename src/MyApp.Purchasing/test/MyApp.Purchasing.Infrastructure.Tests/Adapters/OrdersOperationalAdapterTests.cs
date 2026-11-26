using FluentAssertions;
using Moq;
using MyApp.Purchasing.Application.Contracts.Ports;
using MyApp.Purchasing.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Purchasing.Infrastructure.Tests.Adapters;

public class OrdersOperationalAdapterTests
{
    [Fact]
    public async Task CreateInboundOrderAsync_MapsResponseToSnapshot()
    {
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersOperationalAdapter.CreateOrderWire, OrdersOperationalAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.Base,
                HttpMethod.Post,
                It.IsAny<OrdersOperationalAdapter.CreateOrderWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersOperationalAdapter.OrderWire { Id = orderId, OrderNumber = "IN-1" });

        var sut = new OrdersOperationalAdapter(invoker.Object);
        var result = await sut.CreateInboundOrderAsync(
            new CreateInboundOrderRequest(
                "IN-1",
                null,
                warehouseId,
                Guid.NewGuid(),
                "Purchasing",
                warehouseId,
                DateTime.UtcNow,
                [new InboundOrderLineRequest(productId, 4)]));

        result.Id.Should().Be(orderId);
        result.OrderNumber.Should().Be("IN-1");
        invoker.Verify(i => i.InvokeAsync<OrdersOperationalAdapter.CreateOrderWire, OrdersOperationalAdapter.OrderWire>(
            ServiceNames.Orders,
            ApiEndpoints.Orders.Base,
            HttpMethod.Post,
            It.Is<OrdersOperationalAdapter.CreateOrderWire>(w =>
                w.Type == OrderTypes.Inbound &&
                w.WarehouseId == warehouseId &&
                w.Lines != null &&
                w.Lines.Count == 1 &&
                w.Lines[0].ProductId == productId &&
                w.Lines[0].Quantity == 4),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateInboundOrderAsync_NullOrderNumber_MapsToEmptyString()
    {
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersOperationalAdapter.CreateOrderWire, OrdersOperationalAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.Base,
                HttpMethod.Post,
                It.IsAny<OrdersOperationalAdapter.CreateOrderWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersOperationalAdapter.OrderWire { Id = Guid.NewGuid(), OrderNumber = null });

        var sut = new OrdersOperationalAdapter(invoker.Object);
        var result = await sut.CreateInboundOrderAsync(
            new CreateInboundOrderRequest(
                "IN-2",
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                DateTime.UtcNow,
                [new InboundOrderLineRequest(Guid.NewGuid(), 1)]));

        result.OrderNumber.Should().BeEmpty();
    }

    [Fact]
    public async Task FulfillInboundOrderAsync_InvokesOrdersFulfillEndpoint()
    {
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersOperationalAdapter.FulfillOrderWire, OrdersOperationalAdapter.OrderWire>(
                ServiceNames.Orders,
                $"{ApiEndpoints.Orders.Base}/{orderId}/fulfill",
                HttpMethod.Post,
                It.IsAny<OrdersOperationalAdapter.FulfillOrderWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersOperationalAdapter.OrderWire { Id = orderId, OrderNumber = "IN-1" });

        var sut = new OrdersOperationalAdapter(invoker.Object);
        await sut.FulfillInboundOrderAsync(
            new FulfillInboundOrderRequest(orderId, warehouseId, "TRK-1", "Dock A"));

        invoker.Verify(i => i.InvokeAsync<OrdersOperationalAdapter.FulfillOrderWire, OrdersOperationalAdapter.OrderWire>(
            ServiceNames.Orders,
            $"{ApiEndpoints.Orders.Base}/{orderId}/fulfill",
            HttpMethod.Post,
            It.Is<OrdersOperationalAdapter.FulfillOrderWire>(w =>
                w.OrderId == orderId &&
                w.WarehouseId == warehouseId &&
                w.TrackingNumber == "TRK-1" &&
                w.ShippingAddress == "Dock A"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
