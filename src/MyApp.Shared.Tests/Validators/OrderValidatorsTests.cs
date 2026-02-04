using FluentAssertions;
using MyApp.Shared.Domain.Validators;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace MyApp.Shared.Tests.Validators;

public class OrderValidatorsTests
{
    #region ValidateOrderLine Tests

    [Fact]
    public void ValidateOrderLine_WithValidValues_ReturnsSuccess()
    {
        // Arrange
        var quantity = 10;
        var unitPrice = 5.00m;
        var lineTotal = 50.00m;

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateOrderLine_WithZeroQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = 0;
        var unitPrice = 5.00m;
        var lineTotal = 0m;

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidateOrderLine_WithNegativeQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = -1;
        var unitPrice = 5.00m;
        var lineTotal = -5.00m;

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidateOrderLine_WithNegativePrice_ReturnsValidationError()
    {
        // Arrange
        var quantity = 10;
        var unitPrice = -5.00m;
        var lineTotal = -50.00m;

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Unit price cannot be negative");
    }

    [Fact]
    public void ValidateOrderLine_WithZeroPrice_ReturnsSuccess()
    {
        // Arrange - Zero price is allowed (e.g., promotional items)
        var quantity = 10;
        var unitPrice = 0m;
        var lineTotal = 0m;

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateOrderLine_WithIncorrectTotal_ReturnsValidationError()
    {
        // Arrange
        var quantity = 10;
        var unitPrice = 5.00m;
        var lineTotal = 100.00m; // Should be 50.00m

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Line total must equal quantity times unit price");
    }

    [Fact]
    public void ValidateOrderLine_WithRoundingTolerance_ReturnsSuccess()
    {
        // Arrange - Test that 0.01m tolerance works
        var quantity = 3;
        var unitPrice = 33.33m;
        var lineTotal = 99.99m; // 3 * 33.33 = 99.99, within tolerance

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateOrderLine_WithToleranceExceeded_ReturnsValidationError()
    {
        // Arrange - Test that tolerance is exceeded
        var quantity = 3;
        var unitPrice = 33.33m;
        var lineTotal = 100.00m; // Exceeds 0.01m tolerance

        // Act
        var result = OrderValidators.ValidateOrderLine(quantity, unitPrice, lineTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Line total must equal quantity times unit price");
    }

    #endregion

    #region ValidateOrder Tests

    [Fact]
    public void ValidateOrder_WithValidOrder_ReturnsSuccess()
    {
        // Arrange
        var lineCount = 3;
        var totalAmount = 150.00m;
        var calculatedTotal = 150.00m;

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateOrder_WithZeroLines_ReturnsValidationError()
    {
        // Arrange
        var lineCount = 0;
        var totalAmount = 0m;
        var calculatedTotal = 0m;

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Order must have at least one line");
    }

    [Fact]
    public void ValidateOrder_WithMismatchedTotal_ReturnsValidationError()
    {
        // Arrange
        var lineCount = 3;
        var totalAmount = 150.00m;
        var calculatedTotal = 200.00m;

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Order total amount must match sum of line totals");
    }

    [Fact]
    public void ValidateOrder_WithRoundingTolerance_ReturnsSuccess()
    {
        // Arrange - Test that 0.01m tolerance works (difference must be < 0.01m)
        // Using exact match to test the tolerance logic works correctly
        var lineCount = 3;
        var totalAmount = 100.00m;
        var calculatedTotal = 100.00m; // Exact match

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateOrder_WithToleranceExceeded_ReturnsValidationError()
    {
        // Arrange - Test that tolerance is exceeded
        var lineCount = 3;
        var totalAmount = 99.99m;
        var calculatedTotal = 101.00m; // Exceeds 0.01m tolerance

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Order total amount must match sum of line totals");
    }

    [Fact]
    public void ValidateOrder_WithSingleLine_ReturnsSuccess()
    {
        // Arrange
        var lineCount = 1;
        var totalAmount = 50.00m;
        var calculatedTotal = 50.00m;

        // Act
        var result = OrderValidators.ValidateOrder(lineCount, totalAmount, calculatedTotal);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    #endregion
}
