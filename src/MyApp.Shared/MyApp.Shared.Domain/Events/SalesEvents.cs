namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Event raised when a sales order is created.
/// </summary>
public record SalesOrderCreatedEvent(
    Guid SalesOrderId,
    Guid CustomerId,
    string OrderNumber,
    bool IsQuote,
    decimal TotalAmount
);

/// <summary>
/// Event raised when a sales order is confirmed into an order.
/// </summary>
public record SalesOrderConfirmedEvent(
    Guid SalesOrderId,
    Guid OrderId,
    DateTime ConfirmedDate
);

/// <summary>
/// Event raised when a sales quote expires.
/// </summary>
public record QuoteExpiredEvent(
    Guid SalesOrderId,
    DateTime ExpiryDate
);

/// <summary>
/// Event raised when a sales order status changes.
/// </summary>
public record SalesOrderStatusChangedEvent(
    Guid SalesOrderId,
    string OldStatus,
    string NewStatus
);

/// <summary>
/// Event raised when a sales customer is created.
/// </summary>
public record SalesCustomerCreatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);

/// <summary>
/// Event raised when a sales customer is updated.
/// </summary>
public record SalesCustomerUpdatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);
