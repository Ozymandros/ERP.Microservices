using FluentAssertions;
using Moq;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Infrastructure.Adapters;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.Messaging;
using Xunit;

namespace MyApp.Orders.Infrastructure.Tests.Adapters;

public class InventoryStockAdapterTests
{
    [Fact]
    public async Task ReserveAsync_MapsWireResponseToSnapshot()
    {
        var reservationId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync<ReserveStockWireRequest, ReservationWireResponse>(
                ServiceNames.Inventory,
                ApiEndpoints.Inventory.ReserveStock,
                HttpMethod.Post,
                It.IsAny<ReserveStockWireRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReservationWireResponse { Id = reservationId, ReservedUntil = DateTime.UtcNow.AddHours(1) });

        var sut = new InventoryStockAdapter(invoker.Object);
        var result = await sut.ReserveAsync(
            new ReserveStockRequest(Guid.NewGuid(), Guid.NewGuid(), 2, Guid.NewGuid(), Guid.NewGuid(), null));

        result.ReservationId.Should().Be(reservationId);
    }

    [Fact]
    public async Task ReleaseReservationAsync_InvokesDeleteOnReservationsEndpoint()
    {
        var reservationId = Guid.NewGuid();
        var invoker = new Mock<IServiceInvoker>();
        invoker
            .Setup(i => i.InvokeAsync(
                ServiceNames.Inventory,
                $"{ApiEndpoints.Inventory.Reservations}/{reservationId}",
                HttpMethod.Delete,
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = new InventoryStockAdapter(invoker.Object);
        await sut.ReleaseReservationAsync(reservationId);

        invoker.Verify(i => i.InvokeAsync(
            ServiceNames.Inventory,
            $"{ApiEndpoints.Inventory.Reservations}/{reservationId}",
            HttpMethod.Delete,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
