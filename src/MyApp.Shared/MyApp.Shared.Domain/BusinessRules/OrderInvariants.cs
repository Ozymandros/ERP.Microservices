namespace MyApp.Shared.Domain.BusinessRules;

/// <summary>
/// Defines business rule invariants for order processing and validation.
/// </summary>
public static class OrderInvariants
{
    /// <summary>
    /// Determines whether order lines.
    /// </summary>
    /// <param name="lineCount">The line Count.</param>
    public static bool HasOrderLines(int lineCount)
    {
        return lineCount > 0;
    }

    /// <summary>
    /// Determines whether quantity valid.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    public static bool IsQuantityValid(int quantity)
    {
        return quantity > 0;
    }

    /// <summary>
    /// Determines whether price valid.
    /// </summary>
    /// <param name="price">The price.</param>
    public static bool IsPriceValid(decimal price)
    {
        return price >= 0;
    }

    /// <summary>
    /// Determines whether line total correct.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="lineTotal">The line Total.</param>
    public static bool IsLineTotalCorrect(int quantity, decimal unitPrice, decimal lineTotal)
    {
        return Math.Abs(lineTotal - (quantity * unitPrice)) < 0.01m; // Allow for rounding
    }

    /// <summary>
    /// Validate order line.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    /// <param name="unitPrice">The unit Price.</param>
    /// <param name="lineTotal">The line Total.</param>
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
    /// Validate order.
    /// </summary>
    /// <param name="lineCount">The line Count.</param>
    /// <param name="totalAmount">The total Amount.</param>
    /// <param name="calculatedTotal">The calculated Total.</param>
    public static void ValidateOrder(int lineCount, decimal totalAmount, decimal calculatedTotal)
    {
        if (!HasOrderLines(lineCount))
            throw new InvalidOperationException("Order must have at least one line");

        if (Math.Abs(totalAmount - calculatedTotal) >= 0.01m)
            throw new InvalidOperationException("Order total amount must match sum of line totals");
    }
}
