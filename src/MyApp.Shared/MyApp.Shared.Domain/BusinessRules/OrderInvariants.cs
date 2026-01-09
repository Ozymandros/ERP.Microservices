namespace MyApp.Shared.Domain.BusinessRules;

public static class OrderInvariants
{
    public static bool HasOrderLines(int lineCount)
    {
        return lineCount > 0;
    }

    public static bool IsQuantityValid(int quantity)
    {
        return quantity > 0;
    }

    public static bool IsPriceValid(decimal price)
    {
        return price >= 0;
    }

    public static bool IsLineTotalCorrect(int quantity, decimal unitPrice, decimal lineTotal)
    {
        return Math.Abs(lineTotal - (quantity * unitPrice)) < 0.01m; // Allow for rounding
    }

    public static void ValidateOrderLine(int quantity, decimal unitPrice, decimal lineTotal)
    {
        if (!IsQuantityValid(quantity))
            throw new InvalidOperationException("Order line quantity must be greater than zero");
        
        if (!IsPriceValid(unitPrice))
            throw new InvalidOperationException("Unit price cannot be negative");
        
        if (!IsLineTotalCorrect(quantity, unitPrice, lineTotal))
            throw new InvalidOperationException("Line total must equal quantity times unit price");
    }

    public static void ValidateOrder(int lineCount, decimal totalAmount, decimal calculatedTotal)
    {
        if (!HasOrderLines(lineCount))
            throw new InvalidOperationException("Order must have at least one line");
        
        if (Math.Abs(totalAmount - calculatedTotal) >= 0.01m)
            throw new InvalidOperationException("Order total amount must match sum of line totals");
    }
}
