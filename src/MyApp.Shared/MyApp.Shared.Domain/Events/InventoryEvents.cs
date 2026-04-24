namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Event raised when a product is created.
/// </summary>
public record ProductCreatedEvent(
    Guid ProductId,
    string SKU,
    string Name,
    decimal UnitPrice
);

/// <summary>
/// Event raised when stock for a product is updated.
/// </summary>
public record StockUpdatedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string TransactionType
);

/// <summary>
/// Event raised when stock is reserved for an order.
/// </summary>
public record StockReservedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    Guid OrderId,
    int Quantity
);

/// <summary>
/// Event raised when a stock reservation is released.
/// </summary>
public record StockReleasedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    int Quantity
);

/// <summary>
/// Event raised when stock falls below reorder level.
/// </summary>
public record LowStockAlertEvent(
    Guid ProductId,
    Guid WarehouseId,
    int AvailableQuantity,
    int ReorderLevel
);

/// <summary>
/// Event raised when stock is transferred between warehouses.
/// </summary>
public record StockTransferredEvent(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity,
    string Reason
);

/// <summary>
/// Event raised when stock quantity is adjusted.
/// </summary>
public record StockAdjustedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string Reason,
    string? Reference
);
