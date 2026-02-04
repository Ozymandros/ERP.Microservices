using FluentAssertions;
using MyApp.Inventory.Domain.Entities;
using Xunit;

namespace MyApp.Inventory.Application.Tests.Domain;

public class WarehouseStockEntityTests
{
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
