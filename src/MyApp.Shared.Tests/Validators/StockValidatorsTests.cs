using FluentAssertions;
using MyApp.Shared.Domain.Validators;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace MyApp.Shared.Tests.Validators;

public class StockValidatorsTests
{
    #region ValidateReservation Tests

    [Fact]
    public void ValidateReservation_WithValidReservation_ReturnsSuccess()
    {
        // Arrange
        var quantity = 10;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateReservation(quantity, availableQuantity);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateReservation_WithZeroQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = 0;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateReservation(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Reservation quantity must be greater than zero");
    }

    [Fact]
    public void ValidateReservation_WithNegativeQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = -1;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateReservation(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Reservation quantity must be greater than zero");
    }

    [Fact]
    public void ValidateReservation_WithQuantityExceedingAvailable_ReturnsValidationError()
    {
        // Arrange
        var quantity = 101;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateReservation(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Cannot reserve 101 units. Only 100 available");
    }

    [Fact]
    public void ValidateReservation_WithExactAvailableQuantity_ReturnsSuccess()
    {
        // Arrange
        var quantity = 100;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateReservation(quantity, availableQuantity);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    #endregion

    #region ValidateTransfer Tests

    [Fact]
    public void ValidateTransfer_WithValidTransfer_ReturnsSuccess()
    {
        // Arrange
        var quantity = 10;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateTransfer(quantity, availableQuantity);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateTransfer_WithZeroQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = 0;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateTransfer(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Transfer quantity must be greater than zero");
    }

    [Fact]
    public void ValidateTransfer_WithNegativeQuantity_ReturnsValidationError()
    {
        // Arrange
        var quantity = -1;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateTransfer(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Transfer quantity must be greater than zero");
    }

    [Fact]
    public void ValidateTransfer_WithQuantityExceedingAvailable_ReturnsValidationError()
    {
        // Arrange
        var quantity = 101;
        var availableQuantity = 100;

        // Act
        var result = StockValidators.ValidateTransfer(quantity, availableQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Cannot transfer 101 units. Only 100 available");
    }

    #endregion

    #region ValidateAdjustment Tests

    [Fact]
    public void ValidateAdjustment_WithValidReason_ReturnsSuccess()
    {
        // Arrange
        var quantityChange = 10;
        var reason = "Stock count correction";

        // Act
        var result = StockValidators.ValidateAdjustment(quantityChange, reason);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateAdjustment_WithNullReason_ReturnsValidationError()
    {
        // Arrange
        var quantityChange = 10;
        string? reason = null;

        // Act
        var result = StockValidators.ValidateAdjustment(quantityChange, reason!);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Adjustment must have a valid reason");
    }

    [Fact]
    public void ValidateAdjustment_WithEmptyReason_ReturnsValidationError()
    {
        // Arrange
        var quantityChange = 10;
        var reason = string.Empty;

        // Act
        var result = StockValidators.ValidateAdjustment(quantityChange, reason);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Adjustment must have a valid reason");
    }

    [Fact]
    public void ValidateAdjustment_WithWhitespaceReason_ReturnsValidationError()
    {
        // Arrange
        var quantityChange = 10;
        var reason = "   ";

        // Act
        var result = StockValidators.ValidateAdjustment(quantityChange, reason);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Adjustment must have a valid reason");
    }

    [Fact]
    public void ValidateAdjustment_WithNegativeQuantityChange_ReturnsSuccess()
    {
        // Arrange - Negative adjustments are allowed (e.g., damage, loss)
        var quantityChange = -10;
        var reason = "Damaged goods";

        // Act
        var result = StockValidators.ValidateAdjustment(quantityChange, reason);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    #endregion

    #region ValidateWarehouseStock Tests

    [Fact]
    public void ValidateWarehouseStock_WithValidQuantities_ReturnsSuccess()
    {
        // Arrange
        var availableQuantity = 100;
        var reservedQuantity = 50;
        var onOrderQuantity = 25;

        // Act
        var result = StockValidators.ValidateWarehouseStock(availableQuantity, reservedQuantity, onOrderQuantity);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateWarehouseStock_WithNegativeAvailableQuantity_ReturnsValidationError()
    {
        // Arrange
        var availableQuantity = -1;
        var reservedQuantity = 0;
        var onOrderQuantity = 0;

        // Act
        var result = StockValidators.ValidateWarehouseStock(availableQuantity, reservedQuantity, onOrderQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Available quantity cannot be negative");
    }

    [Fact]
    public void ValidateWarehouseStock_WithNegativeReservedQuantity_ReturnsValidationError()
    {
        // Arrange
        var availableQuantity = 100;
        var reservedQuantity = -1;
        var onOrderQuantity = 0;

        // Act
        var result = StockValidators.ValidateWarehouseStock(availableQuantity, reservedQuantity, onOrderQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Reserved quantity cannot be negative");
    }

    [Fact]
    public void ValidateWarehouseStock_WithNegativeOnOrderQuantity_ReturnsValidationError()
    {
        // Arrange
        var availableQuantity = 100;
        var reservedQuantity = 50;
        var onOrderQuantity = -1;

        // Act
        var result = StockValidators.ValidateWarehouseStock(availableQuantity, reservedQuantity, onOrderQuantity);

        // Assert
        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("On-order quantity cannot be negative");
    }

    [Fact]
    public void ValidateWarehouseStock_WithZeroQuantities_ReturnsSuccess()
    {
        // Arrange
        var availableQuantity = 0;
        var reservedQuantity = 0;
        var onOrderQuantity = 0;

        // Act
        var result = StockValidators.ValidateWarehouseStock(availableQuantity, reservedQuantity, onOrderQuantity);

        // Assert
        result.Should().Be(ValidationResult.Success);
    }

    #endregion
}
