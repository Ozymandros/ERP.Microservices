using System.ComponentModel.DataAnnotations;

namespace MyApp.Shared.Domain.Validators;

/// <summary>
/// Provides validation methods for order and order line items.
/// </summary>
public static class OrderValidators
{
    /// <summary>
    /// Validate order line.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="lineTotal">The line Total.</param>
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

    /// <summary>
    /// Validate order.
    /// </summary>
    /// <param name="lineCount">The line Count.</param>
    /// <param name="totalAmount">The total Amount.</param>
    /// <param name="calculatedTotal">The calculated Total.</param>
    public static ValidationResult? ValidateOrder(int lineCount, decimal totalAmount, decimal calculatedTotal)
    {
        if (lineCount == 0)
            return new ValidationResult("Order must have at least one line");

        if (Math.Abs(totalAmount - calculatedTotal) >= 0.01m)
            return new ValidationResult("Order total amount must match sum of line totals");

        return ValidationResult.Success;
    }
}
