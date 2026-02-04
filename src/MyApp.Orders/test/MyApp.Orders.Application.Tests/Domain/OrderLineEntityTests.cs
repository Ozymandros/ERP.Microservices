using FluentAssertions;
using MyApp.Orders.Domain.Entities;
using Xunit;

namespace MyApp.Orders.Application.Tests.Domain;

public class OrderLineEntityTests
{
    [Fact]
    public void IsFulfilled_WhenPickedQuantityEqualsQuantity_ShouldBeTrue()
    {
        // Arrange
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            Quantity = 10,
            PickedQuantity = 10
        };

        // Act
        orderLine.IsFulfilled = true;

        // Assert
        orderLine.IsFulfilled.Should().BeTrue();
        orderLine.PickedQuantity.Should().Be(orderLine.Quantity);
    }

    [Fact]
    public void IsFulfilled_WhenPickedQuantityLessThanQuantity_ShouldBeFalse()
    {
        // Arrange
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            Quantity = 10,
            PickedQuantity = 5
        };

        // Act
        orderLine.IsFulfilled = false;

        // Assert
        orderLine.IsFulfilled.Should().BeFalse();
    }

    [Fact]
    public void ReservedQuantity_CanBeSetIndependently()
    {
        // Arrange
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            Quantity = 10,
            ReservedQuantity = 8
        };

        // Assert
        orderLine.ReservedQuantity.Should().Be(8);
        orderLine.Quantity.Should().Be(10);
    }

    [Fact]
    public void ReservedStockId_CanBeSetWhenReserved()
    {
        // Arrange
        var reservedStockId = Guid.NewGuid();
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            ReservedStockId = reservedStockId,
            ReservedQuantity = 5
        };

        // Assert
        orderLine.ReservedStockId.Should().Be(reservedStockId);
        orderLine.ReservedQuantity.Should().Be(5);
    }

    [Fact]
    public void ReservedStockId_CanBeNullWhenNotReserved()
    {
        // Arrange
        var orderLine = new OrderLine(Guid.NewGuid())
        {
            ReservedStockId = null,
            ReservedQuantity = 0
        };

        // Assert
        orderLine.ReservedStockId.Should().BeNull();
        orderLine.ReservedQuantity.Should().Be(0);
    }
}
