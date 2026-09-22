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
    /// Initializes a new instance of the OrderFulfillmentException class.
    /// </summary>
    /// <param name="orderId">The order Id.</param>
    /// <param name="message">The message.</param>
    public OrderFulfillmentException(Guid orderId, string message)
        : base($"Order {orderId} cannot be fulfilled: {message}")
    {
        OrderId = orderId;
    }

    /// <summary>
    /// Initializes a new instance of the OrderFulfillmentException class.
    /// </summary>
    /// <param name="message">The message.</param>
    public OrderFulfillmentException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the OrderFulfillmentException class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner Exception.</param>
    public OrderFulfillmentException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
