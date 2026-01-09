using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Validators;

public static class OrderValidators
{
    public static ValidationResult? ValidateOrderLine(int quantity, decimal unitPrice, decimal lineTotal)
    {
        if (quantity <= 0)
            return new ValidationResult("Order line quantity must be greater than zero");
        
        if (unitPrice < 0)
            return new ValidationResult("Unit price cannot be negative");
        
        if (Math.Abs(lineTotal - (quantity * unitPrice)) >= 0.01m)
            return new ValidationResult("Line total must equal quantity times unit price");

        return ValidationResult.Success;
    }

    public static ValidationResult? ValidateOrder(int lineCount, decimal totalAmount, decimal calculatedTotal)
    {
        if (lineCount == 0)
            return new ValidationResult("Order must have at least one line");
        
        if (Math.Abs(totalAmount - calculatedTotal) >= 0.01m)
            return new ValidationResult("Order total amount must match sum of line totals");

        return ValidationResult.Success;
    }
}
