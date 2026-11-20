using FluentAssertions;
using MyApp.Purchasing.Domain.Entities;
using Xunit;

namespace MyApp.Purchasing.Application.Tests.Domain;

/// <summary>Unit tests for the <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrderLine"/> entity.</summary>
public class PurchaseOrderLineEntityTests
{
    /// <summary>Verifies that <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrderLine.IsFullyReceived"/> is true when the received quantity equals the ordered quantity.</summary>
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

    /// <summary>Verifies that <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrderLine.IsFullyReceived"/> is false when the received quantity is less than the ordered quantity.</summary>
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

    /// <summary>Verifies that <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrderLine.IsFullyReceived"/> can be set to true even when the received quantity exceeds the ordered quantity.</summary>
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

    /// <summary>Verifies that <see cref="MyApp.Purchasing.Domain.Entities.PurchaseOrderLine.LineTotal"/> equals the product of quantity and unit price.</summary>
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
