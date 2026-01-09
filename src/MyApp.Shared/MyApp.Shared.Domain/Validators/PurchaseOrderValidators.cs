using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Validators;

public static class PurchaseOrderValidators
{
    public static ValidationResult? ValidatePurchaseOrderLine(int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            return new ValidationResult("Purchase order line quantity must be greater than zero");
        
        if (unitPrice < 0)
            return new ValidationResult("Unit price cannot be negative");

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidateReceivedQuantity(int receivedQuantity, int orderedQuantity)
    {
        if (receivedQuantity < 0)
            return new ValidationResult("Received quantity cannot be negative");
        
        if (receivedQuantity > orderedQuantity)
            return new ValidationResult($"Received quantity ({receivedQuantity}) cannot exceed ordered quantity ({orderedQuantity})");

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidatePurchaseOrderApproval(string currentStatus)
    {
        if (currentStatus != "Draft")
            return new ValidationResult($"Only draft purchase orders can be approved. Current status: {currentStatus}");

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidatePurchaseOrderReceiving(string currentStatus)
    {
        if (currentStatus != "Approved")
            return new ValidationResult($"Only approved purchase orders can be received. Current status: {currentStatus}");

        return ValidationResult.Success;
    }
}
