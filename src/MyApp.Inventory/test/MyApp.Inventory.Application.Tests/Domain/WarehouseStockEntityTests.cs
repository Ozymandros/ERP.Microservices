using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Domain;

/// <summary>Unit tests for the computed properties of the <see cref="WarehouseStock"/> domain entity.</summary>
public class WarehouseStockEntityTests
{
    /// <summary>Verifies that TotalQuantity returns the sum of available and reserved quantities.</summary>
    [Fact]
    public void TotalQuantity_WithAvailableAndReserved_ReturnsSum()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = 100,
            ReservedQuantity = 25
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(125);
    }

    /// <summary>Verifies that TotalQuantity returns zero when both available and reserved quantities are zero.</summary>
    [Fact]
    public void TotalQuantity_WithZeroQuantities_ReturnsZero()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = 0,
            ReservedQuantity = 0
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(0);
    }

    /// <summary>Verifies that TotalQuantity equals the available quantity when reserved quantity is zero.</summary>
    [Fact]
    public void TotalQuantity_WithOnlyAvailable_ReturnsAvailableQuantity()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = 50,
            ReservedQuantity = 0
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(50);
    }

    /// <summary>Verifies that TotalQuantity equals the reserved quantity when available quantity is zero.</summary>
    [Fact]
    public void TotalQuantity_WithOnlyReserved_ReturnsReservedQuantity()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = 0,
            ReservedQuantity = 30
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(30);
    }

    /// <summary>Verifies that TotalQuantity correctly sums large available and reserved quantities without overflow.</summary>
    [Fact]
    public void TotalQuantity_WithLargeQuantities_ReturnsCorrectSum()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = int.MaxValue / 2,
            ReservedQuantity = int.MaxValue / 2
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(int.MaxValue - 1); // Avoid overflow
    }

    /// <summary>Verifies that TotalQuantity does not include the on-order quantity in its calculation.</summary>
    [Fact]
    public void TotalQuantity_IgnoresOnOrderQuantity()
    {
        // Arrange
        var stock = new WarehouseStock(Guid.NewGuid())
        {
            AvailableQuantity = 100,
            ReservedQuantity = 25,
            OnOrderQuantity = 50
        };

        // Act
        var total = stock.TotalQuantity;

        // Assert
        total.Should().Be(125); // Only Available + Reserved, not OnOrder
    }
}
