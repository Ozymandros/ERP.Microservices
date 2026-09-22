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
    /// Initializes a new instance of the InsufficientStockException class.
    /// </summary>
    /// <param name="productId">The product Id.</param>
    /// <param name="warehouseId">The warehouse Id.</param>
    /// <param name="requestedQuantity">The requested Quantity.</param>
    /// <param name="availableQuantity">The available Quantity.</param>
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
    /// Initializes a new instance of the InsufficientStockException class.
    /// </summary>
    /// <param name="message">The message.</param>
    public InsufficientStockException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InsufficientStockException class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner Exception.</param>
    public InsufficientStockException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
