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
}