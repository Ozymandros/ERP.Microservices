namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Product created event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="SKU">The sKU.</param>
/// <param name="Name">The name.</param>
/// <param name="UnitPrice">The unit Price.</param>
public record ProductCreatedEvent(
    Guid ProductId,
    string SKU,
    string Name,
    decimal UnitPrice
);

/// <summary>
/// Stock updated event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="QuantityChange">The quantity Change.</param>
/// <param name="TransactionType">The transaction Type.</param>
public record StockUpdatedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string TransactionType
);

/// <summary>
/// Stock reserved event.
/// </summary>
/// <param name="ReservationId">The reservation Id.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="Quantity">The quantity.</param>
public record StockReservedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    Guid OrderId,
    int Quantity
);

/// <summary>
/// Stock released event.
/// </summary>
/// <param name="ReservationId">The reservation Id.</param>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="Quantity">The quantity.</param>
public record StockReleasedEvent(
    Guid ReservationId,
    Guid ProductId,
    Guid WarehouseId,
    int Quantity
);

/// <summary>
/// Low stock alert event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="AvailableQuantity">The available Quantity.</param>
/// <param name="ReorderLevel">The reorder Level.</param>
public record LowStockAlertEvent(
    Guid ProductId,
    Guid WarehouseId,
    int AvailableQuantity,
    int ReorderLevel
);

/// <summary>
/// Stock transferred event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="FromWarehouseId">The from Warehouse Id.</param>
/// <param name="ToWarehouseId">The to Warehouse Id.</param>
/// <param name="Quantity">The quantity.</param>
/// <param name="Reason">The reason.</param>
public record StockTransferredEvent(
    Guid ProductId,
    Guid FromWarehouseId,
    Guid ToWarehouseId,
    int Quantity,
    string Reason
);

/// <summary>
/// Stock adjusted event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="QuantityChange">The quantity Change.</param>
/// <param name="Reason">The reason.</param>
/// <param name="Reference">The reference.</param>
public record StockAdjustedEvent(
    Guid ProductId,
    Guid WarehouseId,
    int QuantityChange,
    string Reason,
    string? Reference
);
