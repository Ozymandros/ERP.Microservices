using FluentAssertions;
using MyApp.Shared.Domain.BusinessRules;
using Xunit;

namespace MyApp.Shared.Tests.BusinessRules;

public class StockInvariantsTests
{
    #region IsStockSufficient Tests

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 101, false)]
    [InlineData(0, 1, false)]
    [InlineData(10, 0, true)]
    public void IsStockSufficient_WithVariousQuantities_ReturnsExpectedResult(
        int availableQuantity, int requestedQuantity, bool expected)
    {
        // Act
        var result = StockInvariants.IsStockSufficient(availableQuantity, requestedQuantity);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsQuantityNonNegative Tests

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(-1, false)]
    [InlineData(-100, false)]
    public void IsQuantityNonNegative_WithVariousQuantities_ReturnsExpectedResult(
        int quantity, bool expected)
    {
        // Act
        var result = StockInvariants.IsQuantityNonNegative(quantity);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsReservedQuantityValid Tests

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 0, true)]
    [InlineData(100, 101, false)]
    [InlineData(100, -1, false)]
    public void IsReservedQuantityValid_WithVariousQuantities_ReturnsExpectedResult(
        int availableQuantity, int reservedQuantity, bool expected)
    {
        // Act
        var result = StockInvariants.IsReservedQuantityValid(availableQuantity, reservedQuantity);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region ValidateStock Tests

    [Fact]
    public void ValidateStock_WithValidQuantities_DoesNotThrow()
    {
        // Act & Assert
        Action act = () => StockInvariants.ValidateStock(100, 50, 25);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateStock_WithNegativeAvailableQuantity_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => StockInvariants.ValidateStock(-1, 0, 0);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Available quantity cannot be negative");
    }

    [Fact]
    public void ValidateStock_WithNegativeReservedQuantity_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => StockInvariants.ValidateStock(100, -1, 0);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Reserved quantity cannot be negative");
    }

    [Fact]
    public void ValidateStock_WithNegativeOnOrderQuantity_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => StockInvariants.ValidateStock(100, 50, -1);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("On-order quantity cannot be negative");
    }

    [Fact]
    public void ValidateStock_WithAllNegativeQuantities_ThrowsExceptionForFirstInvalid()
    {
        // Act & Assert
        Action act = () => StockInvariants.ValidateStock(-1, -1, -1);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Available quantity cannot be negative");
    }

    #endregion

    #region CanReserveStock Tests

    [Theory]
    [InlineData(100, 50, true)]
    [InlineData(100, 100, true)]
    [InlineData(100, 101, false)]
    [InlineData(100, 0, false)]
    [InlineData(0, 1, false)]
    [InlineData(10, -1, false)]
    public void CanReserveStock_WithVariousQuantities_ReturnsExpectedResult(
        int availableQuantity, int quantityToReserve, bool expected)
    {
        // Act
        var result = StockInvariants.CanReserveStock(availableQuantity, quantityToReserve);

        // Assert
        result.Should().Be(expected);
    }

    #endregion
}
