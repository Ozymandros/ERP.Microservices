namespace MyApp.Shared.Domain.BusinessRules;

public static class StockInvariants
{
    public static bool IsStockSufficient(int availableQuantity, int requestedQuantity)
    {
        return availableQuantity >= requestedQuantity;
    }

    public static bool IsQuantityNonNegative(int quantity)
    {
        return quantity >= 0;
    }

    public static bool IsReservedQuantityValid(int availableQuantity, int reservedQuantity)
    {
        return reservedQuantity >= 0 && reservedQuantity <= availableQuantity;
    }

    public static void ValidateStock(int availableQuantity, int reservedQuantity, int onOrderQuantity)
    {
        if (availableQuantity < 0)
            throw new InvalidOperationException("Available quantity cannot be negative");
        
        if (reservedQuantity < 0)
            throw new InvalidOperationException("Reserved quantity cannot be negative");
        
        if (onOrderQuantity < 0)
            throw new InvalidOperationException("On-order quantity cannot be negative");
    }

    public static bool CanReserveStock(int availableQuantity, int quantityToReserve)
    {
        return availableQuantity >= quantityToReserve && quantityToReserve > 0;
    }
}
