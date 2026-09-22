using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Validators;

/// <summary>
/// Provides validation methods for purchase orders and their lifecycle.
/// </summary>
public static class PurchaseOrderValidators
{
    /// <summary>
    /// Validate purchase order line.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    public static ValidationResult? ValidatePurchaseOrderLine(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            return new ValidationResult("Purchase order line quantity must be greater than zero");

        if (unitPrice < 0)
            return new ValidationResult("Unit price cannot be negative");

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validate received quantity.
    /// </summary>
    /// <param name="receivedQuantity">The received Quantity.</param>
    /// <param name="orderedQuantity">The ordered Quantity.</param>
    public static ValidationResult? ValidateReceivedQuantity(int receivedQuantity, int orderedQuantity)
    {
        if (receivedQuantity < 0)
            return new ValidationResult("Received quantity cannot be negative");

        if (receivedQuantity > orderedQuantity)
            return new ValidationResult($"Received quantity ({receivedQuantity}) cannot exceed ordered quantity ({orderedQuantity})");

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validate purchase order approval.
    /// </summary>
    /// <param name="currentStatus">The current Status.</param>
    public static ValidationResult? ValidatePurchaseOrderApproval(string currentStatus)
    {
        if (currentStatus != "Draft")
            return new ValidationResult($"Only draft purchase orders can be approved. Current status: {currentStatus}");

        return ValidationResult.Success;
    }

    /// <summary>
    /// Validate purchase order receiving.
    /// </summary>
    /// <param name="currentStatus">The current Status.</param>
    public static ValidationResult? ValidatePurchaseOrderReceiving(string currentStatus)
    {
        if (currentStatus != "Approved")
            return new ValidationResult($"Only approved purchase orders can be received. Current status: {currentStatus}");

        return ValidationResult.Success;
    }
}
