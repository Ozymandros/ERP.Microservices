namespace MyApp.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when an order cannot be fulfilled due to business rule violations or system errors.
/// </summary>
public class OrderFulfillmentException : Exception
{
    /// <summary>
    /// Gets the order identifier that could not be fulfilled.
    /// </summary>
    public Guid OrderId { get; }

    /// <summary>
    /// Initializes a new instance of the OrderFulfillmentException class with order and message.
    /// </summary>
    public OrderFulfillmentException(Guid orderId, string message)
        : base($"Order {orderId} cannot be fulfilled: {message}")
    {
        OrderId = orderId;
    }

    /// <summary>
    /// Initializes a new instance of the OrderFulfillmentException class with a message.
    /// </summary>
    public OrderFulfillmentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the OrderFulfillmentException class with a message and inner exception.
    /// </summary>
    public OrderFulfillmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
