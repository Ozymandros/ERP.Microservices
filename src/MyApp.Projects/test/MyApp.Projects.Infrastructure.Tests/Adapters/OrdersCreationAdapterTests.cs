using FluentAssertions;
using Moq;
using MyApp.Projects.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Projects.Infrastructure.Tests.Adapters;

public class OrdersCreationAdapterTests
{
    [Fact]
    public async Task CreateWithReservationAsync_ReturnsProjectOrderSnapshot()
    {
        var orderId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<OrdersCreationAdapter.CreateOrderWithReservationWire, OrdersCreationAdapter.OrderWire>(
                ServiceNames.Orders,
                ApiEndpoints.Orders.WithReservation,
                HttpMethod.Post,
                It.IsAny<OrdersCreationAdapter.CreateOrderWithReservationWire>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OrdersCreationAdapter.OrderWire { Id = orderId, OrderNumber = "PRJ-CONS-1" });

        var sut = new OrdersCreationAdapter(invoker.Object);
        var result = await sut.CreateWithReservationAsync(
            new MyApp.Projects.Application.Contracts.Ports.CreateProjectOrderWithReservationRequest(
                "PRJ-CONS-1",
                OrderTypes.Outbound,
                Guid.NewGuid(),
                null,
                Guid.NewGuid(),
                DocumentSources.Project,
                Guid.NewGuid(),
                DateTime.UtcNow,
                null,
                [new MyApp.Projects.Application.Contracts.Ports.ProjectOrderLineRequest(Guid.NewGuid(), 2)]));

        result.Id.Should().Be(orderId);
        result.OrderNumber.Should().Be("PRJ-CONS-1");
    }
}