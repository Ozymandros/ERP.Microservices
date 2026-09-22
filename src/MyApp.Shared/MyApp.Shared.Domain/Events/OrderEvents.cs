namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Order line event.
/// </summary>
/// <param name="ProductId">The product Id.</param>
/// <param name="Quantity">The quantity.</param>
public record OrderLineEvent(
    Guid ProductId,
    int Quantity
);

/// <summary>
/// Order created event.
/// </summary>
/// <param name="OrderId">The order Id.</param>
/// <param name="OrderNumber">The order Number.</param>
/// <param name="OrderType">The order Type.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="Lines">The lines.</param>
public record OrderCreatedEvent(
    Guid OrderId,
    string OrderNumber,
    string OrderType,
    Guid? WarehouseId,
    List<OrderLineEvent> Lines
);

/// <summary>
/// Order fulfilled event.
/// </summary>
/// <param name="OrderId">The order Id.</param>
/// <param name="OrderNumber">The order Number.</param>
/// <param name="OrderType">The order Type.</param>
/// <param name="WarehouseId">The warehouse Id.</param>
/// <param name="FulfilledDate">The fulfilled Date.</param>
/// <param name="TrackingNumber">The tracking Number.</param>
/// <param name="Lines">The lines.</param>
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
/// Order cancelled event.
/// </summary>
/// <param name="OrderId">The order Id.</param>
/// <param name="Reason">The reason.</param>
public record OrderCancelledEvent(
    Guid OrderId,
    string Reason
);

/// <summary>
/// Order status changed event.
/// </summary>
/// <param name="OrderId">The order Id.</param>
/// <param name="OldStatus">The old Status.</param>
/// <param name="NewStatus">The new Status.</param>
public record OrderStatusChangedEvent(
    Guid OrderId,
    string OldStatus,
    string NewStatus
);
