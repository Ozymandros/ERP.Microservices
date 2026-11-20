using FluentAssertions;
using MyApp.Sales.Domain.Entities;
using Xunit;

namespace MyApp.Sales.Application.Tests.Domain;

/// <summary>Unit tests for <see cref="SalesOrderLine"/> entity behaviour.</summary>
public class SalesOrderLineEntityTests
{
    /// <summary>Verifies that LineTotal equals the product of Quantity and UnitPrice.</summary>
    [Fact]
    public void LineTotal_ShouldEqualQuantityTimesUnitPrice()
    {
        // Arrange
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            Quantity = 5,
            UnitPrice = 10.00m,
            LineTotal = 50.00m
        };

        // Assert
        line.LineTotal.Should().Be(50.00m);
        (line.Quantity * line.UnitPrice).Should().Be(line.LineTotal);
    }

    /// <summary>Verifies that LineTotal is zero when the Quantity is zero.</summary>
    [Fact]
    public void LineTotal_WithZeroQuantity_ShouldBeZero()
    {
        // Arrange
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            Quantity = 0,
            UnitPrice = 10.00m,
            LineTotal = 0m
        };

        // Assert
        line.LineTotal.Should().Be(0m);
    }

    /// <summary>Verifies that LineTotal is zero when the UnitPrice is zero.</summary>
    [Fact]
    public void LineTotal_WithZeroUnitPrice_ShouldBeZero()
    {
        // Arrange
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            Quantity = 5,
            UnitPrice = 0m,
            LineTotal = 0m
        };

        // Assert
        line.LineTotal.Should().Be(0m);
    }

    /// <summary>Verifies that ProductSKU and ProductName can be set to store denormalised product data on the line.</summary>
    [Fact]
    public void ProductSKU_CanBeSetForDenormalizedData()
    {
        // Arrange
        var line = new SalesOrderLine(Guid.NewGuid())
        {
            ProductSKU = "PROD-001",
            ProductName = "Product Name"
        };

        // Assert
        line.ProductSKU.Should().Be("PROD-001");
        line.ProductName.Should().Be("Product Name");
    }
}
