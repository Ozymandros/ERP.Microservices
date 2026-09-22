namespace MyApp.Shared.Domain.Events;

/// <summary>
/// Sales order created event.
/// </summary>
/// <param name="SalesOrderId">The sales Order Id.</param>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="OrderNumber">The order Number.</param>
/// <param name="IsQuote">The is Quote.</param>
/// <param name="TotalAmount">The total Amount.</param>
public record SalesOrderCreatedEvent(
    Guid SalesOrderId,
    Guid CustomerId,
    string OrderNumber,
    bool IsQuote,
    decimal TotalAmount
);

/// <summary>
/// Sales order confirmed event.
/// </summary>
/// <param name="SalesOrderId">The sales Order Id.</param>
/// <param name="OrderId">The order Id.</param>
/// <param name="ConfirmedDate">The confirmed Date.</param>
public record SalesOrderConfirmedEvent(
    Guid SalesOrderId,
    Guid OrderId,
    DateTime ConfirmedDate
);

/// <summary>
/// Quote expired event.
/// </summary>
/// <param name="SalesOrderId">The sales Order Id.</param>
/// <param name="ExpiryDate">The expiry Date.</param>
public record QuoteExpiredEvent(
    Guid SalesOrderId,
    DateTime ExpiryDate
);

/// <summary>
/// Sales order status changed event.
/// </summary>
/// <param name="SalesOrderId">The sales Order Id.</param>
/// <param name="OldStatus">The old Status.</param>
/// <param name="NewStatus">The new Status.</param>
public record SalesOrderStatusChangedEvent(
    Guid SalesOrderId,
    string OldStatus,
    string NewStatus
);

/// <summary>
/// Sales customer created event.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="Email">The email.</param>
public record SalesCustomerCreatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);

/// <summary>
/// Sales customer updated event.
/// </summary>
/// <param name="CustomerId">The customer Id.</param>
/// <param name="Name">The name.</param>
/// <param name="Email">The email.</param>
public record SalesCustomerUpdatedEvent(
    Guid CustomerId,
    string Name,
    string Email
);
