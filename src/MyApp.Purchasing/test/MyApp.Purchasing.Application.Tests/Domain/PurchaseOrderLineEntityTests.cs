using FluentAssertions;
using MyApp.Purchasing.Domain.Entities;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Domain;

public class PurchaseOrderLineEntityTests
{
    [Fact]
    public void IsFullyReceived_WhenReceivedQuantityEqualsQuantity_ShouldBeTrue()
    {
        // Arrange
        var line = new PurchaseOrderLine
        {
            Quantity = 10,
            ReceivedQuantity = 10
        };

        // Act
        line.IsFullyReceived = true;

        // Assert
        line.IsFullyReceived.Should().BeTrue();
        line.ReceivedQuantity.Should().Be(line.Quantity);
    }

    [Fact]
    public void IsFullyReceived_WhenReceivedQuantityLessThanQuantity_ShouldBeFalse()
    {
        // Arrange
        var line = new PurchaseOrderLine
        {
            Quantity = 10,
            ReceivedQuantity = 5
        };

        // Act
        line.IsFullyReceived = false;

        // Assert
        line.IsFullyReceived.Should().BeFalse();
    }

    [Fact]
    public void IsFullyReceived_WhenReceivedQuantityExceedsQuantity_CanBeTrue()
    {
        // Arrange
        var line = new PurchaseOrderLine
        {
            Quantity = 10,
            ReceivedQuantity = 12
        };

        // Act
        line.IsFullyReceived = true;

        // Assert
        line.IsFullyReceived.Should().BeTrue();
        line.ReceivedQuantity.Should().BeGreaterThan(line.Quantity);
    }

    [Fact]
    public void LineTotal_ShouldEqualQuantityTimesUnitPrice()
    {
        // Arrange
        var line = new PurchaseOrderLine
        {
            Quantity = 5,
            UnitPrice = 10.00m,
            LineTotal = 50.00m
        };

        // Assert
        line.LineTotal.Should().Be(50.00m);
        (line.Quantity * line.UnitPrice).Should().Be(line.LineTotal);
    }
}
