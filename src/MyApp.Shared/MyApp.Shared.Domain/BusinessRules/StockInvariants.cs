namespace MyApp.Shared.Domain.BusinessRules;

/// <summary>
/// Defines business rule invariants for stock management and validation.
/// </summary>
public static class StockInvariants
{
    /// <summary>
    /// Determines whether sufficient stock is available for a requested quantity.
    /// </summary>
    public static bool IsStockSufficient(int availableQuantity, int requestedQuantity)
    {
        return availableQuantity >= requestedQuantity;
    }

    /// <summary>
    /// Determines whether a quantity is non-negative.
    /// </summary>
    public static bool IsQuantityNonNegative(int quantity)
    {
        return quantity >= 0;
    }

    /// <summary>
    /// Determines whether a reserved quantity is valid relative to available quantity.
    /// </summary>
    public static bool IsReservedQuantityValid(int availableQuantity, int reservedQuantity)
    {
        return reservedQuantity >= 0 && reservedQuantity <= availableQuantity;
    }

    /// <summary>
    /// Validates all stock quantities against invariants.
    /// </summary>
    public static void ValidateStock(int availableQuantity, int reservedQuantity, int onOrderQuantity)
    {
        if (availableQuantity < 0)
            throw new InvalidOperationException("Available quantity cannot be negative");

        if (reservedQuantity < 0)
            throw new InvalidOperationException("Reserved quantity cannot be negative");

        if (onOrderQuantity < 0)
            throw new InvalidOperationException("On-order quantity cannot be negative");
    }

    /// <summary>
    /// Determines whether stock can be reserved for a given quantity.
    /// </summary>
    public static bool CanReserveStock(int availableQuantity, int quantityToReserve)
    {
        return availableQuantity >= quantityToReserve && quantityToReserve > 0;
    }
}
