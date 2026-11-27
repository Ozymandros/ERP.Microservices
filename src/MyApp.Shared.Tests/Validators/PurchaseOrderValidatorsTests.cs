using FluentAssertions;
using MyApp.Shared.Domain.Validators;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace MyApp.Shared.Tests.Validators;

public class PurchaseOrderValidatorsTests
{
    #region ValidatePurchaseOrderLine Tests

    [Fact]
    public void ValidatePurchaseOrderLine_WithValidValues_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderLine(10, 5.00m);

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidatePurchaseOrderLine_WithZeroQuantity_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderLine(0, 5.00m);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Purchase order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidatePurchaseOrderLine_WithNegativeQuantity_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderLine(-1, 5.00m);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Purchase order line quantity must be greater than zero");
    }

    [Fact]
    public void ValidatePurchaseOrderLine_WithNegativePrice_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderLine(10, -5.00m);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Unit price cannot be negative");
    }

    [Fact]
    public void ValidatePurchaseOrderLine_WithZeroPrice_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderLine(10, 0m);

        result.Should().Be(ValidationResult.Success);
    }

    #endregion

    #region ValidateReceivedQuantity Tests

    [Fact]
    public void ValidateReceivedQuantity_WithValidQuantity_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidateReceivedQuantity(5, 10);

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateReceivedQuantity_WithExactOrderedQuantity_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidateReceivedQuantity(10, 10);

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateReceivedQuantity_WithZeroReceived_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidateReceivedQuantity(0, 10);

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidateReceivedQuantity_WithNegativeReceived_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidateReceivedQuantity(-1, 10);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Received quantity cannot be negative");
    }

    [Fact]
    public void ValidateReceivedQuantity_ExceedingOrdered_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidateReceivedQuantity(11, 10);

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Received quantity (11) cannot exceed ordered quantity (10)");
    }

    #endregion

    #region ValidatePurchaseOrderApproval Tests

    [Fact]
    public void ValidatePurchaseOrderApproval_WithDraftStatus_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderApproval("Draft");

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidatePurchaseOrderApproval_WithNonDraftStatus_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderApproval("Approved");

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Only draft purchase orders can be approved. Current status: Approved");
    }

    #endregion

    #region ValidatePurchaseOrderReceiving Tests

    [Fact]
    public void ValidatePurchaseOrderReceiving_WithApprovedStatus_ReturnsSuccess()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderReceiving("Approved");

        result.Should().Be(ValidationResult.Success);
    }

    [Fact]
    public void ValidatePurchaseOrderReceiving_WithNonApprovedStatus_ReturnsValidationError()
    {
        var result = PurchaseOrderValidators.ValidatePurchaseOrderReceiving("Draft");

        result.Should().NotBeNull();
        result!.ErrorMessage.Should().Be("Only approved purchase orders can be received. Current status: Draft");
    }

    #endregion
}
