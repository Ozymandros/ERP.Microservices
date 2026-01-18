namespace MyApp.Shared.Domain.Events;

public record ProductCreatedEvent(
    Guid ProductId,
    string SKU,
    string Name,
    decimal UnitPrice
);

public record StockUpdatedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string TransactionType
);

public record StockReservedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    Guid OrderId,
    int Quantity
);

public record StockReleasedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    int Quantity
);

public record LowStockAlertEvent(
    Guid ProductId,
    Guid WarehouseId,
    int AvailableQuantity,
    int ReorderLevel
);

public record StockTransferredEvent(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity,
    string Reason
);

public record StockAdjustedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string Reason,
    string? Reference
);
