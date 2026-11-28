using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Orders.Application.Contracts.Dtos;
using MyApp.Orders.Application.Contracts.Ports;
using MyApp.Orders.Application.Internals;
using MyApp.Orders.Application.UseCases.Orders;
using MyApp.Orders.Domain;
using MyApp.Orders.Domain.Entities;
using MyApp.Orders.Domain.Repositories;
using MyApp.Shared.Domain.Constants;
using MyApp.Shared.Domain.DTOs;
using MyApp.Shared.Domain.Events;
using MyApp.Shared.Domain.Exceptions;
using MyApp.Shared.Domain.Messaging;
using MyApp.Shared.Domain.Repositories;
using Xunit;

namespace MyApp.Orders.Application.Tests.UseCases;

public class OrderUseCaseCoverageTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<IReservedStockRepository> _reservations = new();
    private readonly Mock<IInventoryStockPort> _inventory = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IEventPublisher> _publisher = new();
    private readonly OrdersTransactionBoundary _boundary;

    public OrderUseCaseCoverageTests()
    {
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<EntityEntryDto>());
        _boundary = new OrdersTransactionBoundary(
            _unitOfWork.Object,
            _publisher.Object,
            NullLogger<OrdersTransactionBoundary>.Instance);
    }

    [Fact]
    public async Task CreateOrderWithReservation_EmptyLines_Throws()
    {
        var cancel = new CancelOrderUseCase(
            _orders.Object, _reservations.Object, _inventory.Object, _boundary, NullLogger<CancelOrderUseCase>.Instance);
        var sut = new CreateOrderWithReservationUseCase(
            _orders.Object, _reservations.Object, _inventory.Object, cancel, _mapper.Object, _boundary,
            NullLogger<CreateOrderWithReservationUseCase>.Instance);

        var act = () => sut.ExecuteAsync(new CreateOrderWithReservationDto
        {
            Type = OrderType.Outbound,
            WarehouseId = Guid.NewGuid(),
            Lines = []
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task FulfillOrder_NotFound_Throws()
    {
        var orderId = Guid.NewGuid();
        _orders.Setup(o => o.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);
        var sut = new FulfillOrderUseCase(
            _orders.Object, _reservations.Object, _mapper.Object, _boundary, NullLogger<FulfillOrderUseCase>.Instance);

        var act = () => sut.ExecuteAsync(new FulfillOrderDto { OrderId = orderId });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CancelOrder_WhenApproved_ReleasesReservations()
    {
        var orderId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var order = new Order(orderId)
        {
            OrderNumber = "ORD-1",
            Type = OrderType.Outbound,
            Status = OrderStatus.Approved,
            WarehouseId = warehouseId
        };
        var reservation = new ReservedStock(Guid.NewGuid())
        {
            OrderId = orderId,
            ProductId = Guid.NewGuid(),
            WarehouseId = warehouseId,
            Quantity = 2,
            Status = ReservationStatus.Reserved
        };
        _orders.Setup(o => o.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _reservations.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([reservation]);
        _inventory.Setup(i => i.ReleaseReservationAsync(reservation.Id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var sut = new CancelOrderUseCase(
            _orders.Object, _reservations.Object, _inventory.Object, _boundary, NullLogger<CancelOrderUseCase>.Instance);

        await sut.ExecuteAsync(new CancelOrderDto { OrderId = orderId, Reason = "test" });

        order.Status.Should().Be(OrderStatus.Cancelled);
        _inventory.Verify(i => i.ReleaseReservationAsync(reservation.Id, It.IsAny<CancellationToken>()), Times.Once);
    }
}