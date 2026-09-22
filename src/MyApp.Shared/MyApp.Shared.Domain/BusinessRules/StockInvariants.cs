namespace MyApp.Shared.Domain.BusinessRules;

/// <summary>
/// Defines business rule invariants for stock management and validation.
/// </summary>
public static class StockInvariants
{
    /// <summary>
    /// Determines whether stock sufficient.
    /// </summary>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="requestedQuantity">The requested Quantity.</param>
    public static bool IsStockSufficient(int availableQuantity, int requestedQuantity)
    {
        return availableQuantity >= requestedQuantity;
    }

    /// <summary>
    /// Determines whether quantity non negative.
    /// </summary>
    /// <param name="quantity">The quantity.</param>
    public static bool IsQuantityNonNegative(int quantity)
    {
        return quantity >= 0;
    }

    /// <summary>
    /// Determines whether reserved quantity valid.
    /// </summary>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="reservedQuantity">The reserved Quantity.</param>
    public static bool IsReservedQuantityValid(int availableQuantity, int reservedQuantity)
    {
        return reservedQuantity >= 0 && reservedQuantity <= availableQuantity;
    }

    /// <summary>
    /// Validate stock.
    /// </summary>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="reservedQuantity">The reserved Quantity.</param>
    /// <param name="onOrderQuantity">The on Order Quantity.</param>
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
    /// Can reserve stock.
    /// </summary>
    /// <param name="availableQuantity">The available Quantity.</param>
    /// <param name="quantityToReserve">The quantity To Reserve.</param>
    public static bool CanReserveStock(int availableQuantity, int quantityToReserve)
    {
        return availableQuantity >= quantityToReserve && quantityToReserve > 0;
    }
}
