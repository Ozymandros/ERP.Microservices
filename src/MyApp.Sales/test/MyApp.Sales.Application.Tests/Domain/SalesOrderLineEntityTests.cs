using FluentAssertions;
using MyApp.Sales.Domain.Entities;
using Xunit;

namespace MyApp.Sales.Application.Tests.Domain;

public class SalesOrderLineEntityTests
{
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
