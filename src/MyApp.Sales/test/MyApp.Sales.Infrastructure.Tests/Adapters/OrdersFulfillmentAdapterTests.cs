using FluentAssertions;
using Moq;
using MyApp.Sales.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Sales.Infrastructure.Tests.Adapters;

public class OrdersFulfillmentAdapterTests
{
    [Fact]
    public async Task CreateWithReservationAsync_MapsResponseToSnapshot()
    {
        var orderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersFulfillmentAdapter.CreateOrderWithReservationWire, OrdersFulfillmentAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.WithReservation,
                HttpMethod.Post,
                It.IsAny<OrdersFulfillmentAdapter.CreateOrderWithReservationWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersFulfillmentAdapter.OrderWire { Id = orderId, OrderNumber = "SO-1" });

        var sut = new OrdersFulfillmentAdapter(invoker.Object);
        var result = await sut.CreateWithReservationAsync(
            new MyApp.Sales.Application.Contracts.Ports.CreateFulfillmentOrderRequest(
                "SO-1",
                OrderTypes.Outbound,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DocumentSources.Sales,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null,
                [new MyApp.Sales.Application.Contracts.Ports.FulfillmentOrderLineRequest(Guid.NewGuid(), 1)]));

        result.Id.Should().Be(orderId);
        result.OrderNumber.Should().Be("SO-1");
    }

    [Fact]
    public async Task CreateOrderAsync_MapsResponseToSnapshot()
    {
        var orderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersFulfillmentAdapter.CreateOrderWire, OrdersFulfillmentAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.Base,
                HttpMethod.Post,
                It.IsAny<OrdersFulfillmentAdapter.CreateOrderWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersFulfillmentAdapter.OrderWire { Id = orderId, OrderNumber = "RET-1" });

        var sut = new OrdersFulfillmentAdapter(invoker.Object);
        var result = await sut.CreateOrderAsync(
            new MyApp.Sales.Application.Contracts.Ports.CreateOperationalOrderRequest(
                "RET-1",
                OrderTypes.Return,
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                DocumentSources.Sales,
                Guid.NewGuid(),
                DateTime.UtcNow,
                [new MyApp.Sales.Application.Contracts.Ports.FulfillmentOrderLineRequest(Guid.NewGuid(), 2)]));

        result.Id.Should().Be(orderId);
        result.OrderNumber.Should().Be("RET-1");
    }

    [Fact]
    public async Task CreateOrderAsync_NullOrderNumber_MapsToEmptyString()
    {
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersFulfillmentAdapter.CreateOrderWire, OrdersFulfillmentAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.Base,
                HttpMethod.Post,
                It.IsAny<OrdersFulfillmentAdapter.CreateOrderWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersFulfillmentAdapter.OrderWire { Id = Guid.NewGuid(), OrderNumber = null });

        var sut = new OrdersFulfillmentAdapter(invoker.Object);
        var result = await sut.CreateOrderAsync(
            new MyApp.Sales.Application.Contracts.Ports.CreateOperationalOrderRequest(
                "RET-2",
                OrderTypes.Return,
                null,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DocumentSources.Sales,
                Guid.NewGuid(),
                DateTime.UtcNow,
                [new MyApp.Sales.Application.Contracts.Ports.FulfillmentOrderLineRequest(Guid.NewGuid(), 1)]));

        result.OrderNumber.Should().BeEmpty();
    }
}
