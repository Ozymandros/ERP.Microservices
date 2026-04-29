namespace MyApp.Shared.Domain.BusinessRules;

/// <summary>
/// Defines business rule invariants for order processing and validation.
/// </summary>
public static class OrderInvariants
{
    /// <summary>
    /// Determines whether an order has at least one line item.
    /// </summary>
    public static bool HasOrderLines(int lineCount)
    {
        return lineCount > 0;
    }

    /// <summary>
    /// Determines whether a quantity is valid (greater than zero).
    /// </summary>
    public static bool IsQuantityValid(int quantity)
    {
        return quantity > 0;
    }

    /// <summary>
    /// Determines whether a unit price is valid (non-negative).
    /// </summary>
    public static bool IsPriceValid(decimal price)
    {
        return price >= 0;
    }

    /// <summary>
    /// Determines whether a line total correctly equals quantity times unit price.
    /// </summary>
    public static bool IsLineTotalCorrect(int quantity, decimal unitPrice, decimal lineTotal)
    {
        return Math.Abs(lineTotal - (quantity * unitPrice)) < 0.01m; // Allow for rounding
    }

    /// <summary>
    /// Validates an order line item against all invariants.
    /// </summary>
    public static void ValidateOrderLine(int quantity, decimal unitPrice, decimal lineTotal)
    {
        if (!IsQuantityValid(quantity))
            throw new InvalidOperationException("Order line quantity must be greater than zero");

        if (!IsPriceValid(unitPrice))
            throw new InvalidOperationException("Unit price cannot be negative");

        if (!IsLineTotalCorrect(quantity, unitPrice, lineTotal))
            throw new InvalidOperationException("Line total must equal quantity times unit price");
    }

    /// <summary>
    /// Validates an entire order against all invariants.
    /// </summary>
    public static void ValidateOrder(int lineCount, decimal totalAmount, decimal calculatedTotal)
    {
        if (!HasOrderLines(lineCount))
            throw new InvalidOperationException("Order must have at least one line");

        if (Math.Abs(totalAmount - calculatedTotal) >= 0.01m)
            throw new InvalidOperationException("Order total amount must match sum of line totals");
    }
}
