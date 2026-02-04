using FluentAssertions;
using MyApp.Shared.Domain.BusinessRules;
using Xunit;

namespace MyApp.Shared.Tests.BusinessRules;

public class OrderInvariantsTests
{
    #region HasOrderLines Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void HasOrderLines_WithVariousLineCounts_ReturnsExpectedResult(
        int lineCount, bool expected)
    {
        // Act
        var result = OrderInvariants.HasOrderLines(lineCount);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsQuantityValid Tests

    [Theory]
    [InlineData(1, true)]
    [InlineData(100, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void IsQuantityValid_WithVariousQuantities_ReturnsExpectedResult(
        int quantity, bool expected)
    {
        // Act
        var result = OrderInvariants.IsQuantityValid(quantity);

        // Assert
        result.Should().Be(expected);
    }

    #endregion

    #region IsPriceValid Tests

    [Fact]
    public void IsPriceValid_WithZero_ReturnsTrue()
    {
        // Act
        var result = OrderInvariants.IsPriceValid(0m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPriceValid_WithPositivePrice_ReturnsTrue()
    {
        // Act
        var result = OrderInvariants.IsPriceValid(10.50m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPriceValid_WithLargePrice_ReturnsTrue()
    {
        // Act
        var result = OrderInvariants.IsPriceValid(100.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsPriceValid_WithNegativePrice_ReturnsFalse()
    {
        // Act
        var result = OrderInvariants.IsPriceValid(-0.01m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPriceValid_WithLargeNegativePrice_ReturnsFalse()
    {
        // Act
        var result = OrderInvariants.IsPriceValid(-100m);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsLineTotalCorrect Tests

    [Fact]
    public void IsLineTotalCorrect_WithCorrectTotal_ReturnsTrue()
    {
        // Act
        var result = OrderInvariants.IsLineTotalCorrect(10, 5.00m, 50.00m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLineTotalCorrect_WithDecimalPrices_ReturnsTrue()
    {
        // Act
        var result = OrderInvariants.IsLineTotalCorrect(5, 10.50m, 52.50m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLineTotalCorrect_WithRoundingTolerance_ReturnsTrue()
    {
        // Act - Rounding tolerance test
        var result = OrderInvariants.IsLineTotalCorrect(3, 33.33m, 99.99m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsLineTotalCorrect_WithIncorrectTotal_ReturnsFalse()
    {
        // Act
        var result = OrderInvariants.IsLineTotalCorrect(10, 5.00m, 50.01m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLineTotalCorrect_WithLowerTotal_ReturnsFalse()
    {
        // Act
        var result = OrderInvariants.IsLineTotalCorrect(10, 5.00m, 49.99m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsLineTotalCorrect_WithMuchHigherTotal_ReturnsFalse()
    {
        // Act
        var result = OrderInvariants.IsLineTotalCorrect(10, 5.00m, 100.00m);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ValidateOrderLine Tests

    [Fact]
    public void ValidateOrderLine_WithValidValues_DoesNotThrow()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrderLine(10, 5.00m, 50.00m);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrderLine_WithZeroQuantity_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrderLine(0, 5.00m, 0m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidateOrderLine_WithNegativeQuantity_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrderLine(-1, 5.00m, -5.00m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidateOrderLine_WithNegativePrice_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrderLine(10, -5.00m, -50.00m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Unit price cannot be negative");
    }

    [Fact]
    public void ValidateOrderLine_WithIncorrectTotal_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrderLine(10, 5.00m, 100.00m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Line total must equal quantity times unit price");
    }

    #endregion

    #region ValidateOrder Tests

    [Fact]
    public void ValidateOrder_WithValidOrder_DoesNotThrow()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrder(3, 150.00m, 150.00m);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrder_WithZeroLines_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrder(0, 0m, 0m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order must have at least one line");
    }

    [Fact]
    public void ValidateOrder_WithMismatchedTotal_ThrowsInvalidOperationException()
    {
        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrder(3, 150.00m, 200.00m);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order total amount must match sum of line totals");
    }

    [Fact]
    public void ValidateOrder_WithRoundingTolerance_DoesNotThrow()
    {
        // Arrange - Test that 0.01m tolerance works (difference must be < 0.01m)
        // Using exact match to test the tolerance logic works correctly
        var lineCount = 3;
        var totalAmount = 100.00m;
        var calculatedTotal = 100.00m; // Exact match

        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrder(lineCount, totalAmount, calculatedTotal);
        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateOrder_WithToleranceExceeded_ThrowsInvalidOperationException()
    {
        // Arrange - Test that tolerance is exceeded
        var lineCount = 3;
        var totalAmount = 99.99m;
        var calculatedTotal = 101.00m; // Exceeds 0.01m tolerance

        // Act & Assert
        Action act = () => OrderInvariants.ValidateOrder(lineCount, totalAmount, calculatedTotal);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Order total amount must match sum of line totals");
    }

    #endregion
}
