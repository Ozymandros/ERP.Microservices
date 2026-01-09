namespace MyApp.Shared.Domain.Exceptions;

public class InsufficientStockException : Exception
{
    public Guid ProductId { get; }
    public Guid WarehouseId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }

    public InsufficientStockException(
        Guid productId,
        Guid warehouseId,
        int requestedQuantity,
        int availableQuantity)
        : base($"Insufficient stock for product {productId} in warehouse {warehouseId}. Requested: {requestedQuantity}, Available: {availableQuantity}")
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        RequestedQuantity = requestedQuantity;
        AvailableQuantity = availableQuantity;
    }

    public InsufficientStockException(string message) : base(message)
    {
    }

    public InsufficientStockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
