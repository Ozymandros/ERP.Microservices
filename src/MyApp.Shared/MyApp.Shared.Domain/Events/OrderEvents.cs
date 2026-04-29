namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Represents a line item within an order.
/// </summary>
public record OrderLineEvent(
    Guid ProductId,
    int Quantity
);

/// <summary>
/// Event raised when an order is created.
/// </summary>
public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    string OrderType,
    Guid? WarehouseId,
    List<OrderLineEvent> Lines
);

/// <summary>
/// Event raised when an order is fulfilled.
/// </summary>
public record OrderFulfilledEvent(
    Guid OrderId,
    string OrderNumber,
    string OrderType,
    Guid WarehouseId,
    DateTime FulfilledDate,
    string? TrackingNumber,
    List<OrderLineEvent> Lines
);

/// <summary>
/// Event raised when an order is cancelled.
/// </summary>
public record OrderCancelledEvent(
    Guid OrderId,
    string Reason
);

/// <summary>
/// Event raised when an order status changes.
/// </summary>
public record OrderStatusChangedEvent(
    Guid OrderId,
    string OldStatus,
    string NewStatus
);
