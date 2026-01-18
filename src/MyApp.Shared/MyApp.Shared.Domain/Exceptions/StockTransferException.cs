namespace MyApp.Shared.Domain.Exceptions;

public class StockTransferException : Exception
{
    public Guid ProductId { get; }
    public Guid? FromWarehouseId { get; }
    public Guid? ToWarehouseId { get; }

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

    public StockTransferException(string message) : base(message)
    {
    }

    public StockTransferException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
