namespace MyApp.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when a stock transfer operation between warehouses fails.
/// </summary>
public class StockTransferException : Exception
{
    /// <summary>
    /// Gets the product identifier being transferred.
    /// </summary>
    public Guid ProductId { get; }

    /// <summary>
    /// Gets the source warehouse identifier.
    /// </summary>
    public Guid? FromWarehouseId { get; }

    /// <summary>
    /// Gets the destination warehouse identifier.
    /// </summary>
    public Guid? ToWarehouseId { get; }

    /// <summary>
    /// Initializes a new instance of the StockTransferException class with transfer details.
    /// </summary>
    public StockTransferException(
        Guid productId,
        Guid fromWarehouseId,
        Guid toWarehouseId,
        string message)
        : base($"Stock transfer failed for product {productId} from warehouse {fromWarehouseId} to {toWarehouseId}: {message}")
    {
        ProductId = productId;
        FromWarehouseId = fromWarehouseId;
        ToWarehouseId = toWarehouseId;
    }

    /// <summary>
    /// Initializes a new instance of the StockTransferException class with a message.
    /// </summary>
    public StockTransferException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the StockTransferException class with a message and inner exception.
    /// </summary>
    public StockTransferException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
