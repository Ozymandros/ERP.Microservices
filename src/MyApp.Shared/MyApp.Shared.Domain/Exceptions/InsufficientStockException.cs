namespace MyApp.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to reserve or allocate stock that exceeds available quantity.
/// </summary>
public class InsufficientStockException : Exception
{
    /// <summary>
    /// Gets the product identifier for which stock was insufficient.
    /// </summary>
    public Guid ProductId { get; }

    /// <summary>
    /// Gets the warehouse identifier where stock was insufficient.
    /// </summary>
    public Guid WarehouseId { get; }

    /// <summary>
    /// Gets the quantity that was requested.
    /// </summary>
    public int RequestedQuantity { get; }

    /// <summary>
    /// Gets the quantity that was available.
    /// </summary>
    public int AvailableQuantity { get; }

    /// <summary>
    /// Initializes a new instance of the InsufficientStockException class with product and warehouse details.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the InsufficientStockException class with a message.
    /// </summary>
    public InsufficientStockException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InsufficientStockException class with a message and inner exception.
    /// </summary>
    public InsufficientStockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
