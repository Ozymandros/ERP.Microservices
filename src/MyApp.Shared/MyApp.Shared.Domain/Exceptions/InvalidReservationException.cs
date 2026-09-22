namespace MyApp.Shared.Domain.Exceptions;

/// <summary>
/// Exception thrown when a stock reservation operation fails validation or consistency checks.
/// </summary>
public class InvalidReservationException : Exception
{
    /// <summary>
    /// Gets the reservation identifier if known.
    /// </summary>
    public Guid? ReservationId { get; }

    /// <summary>
    /// Initializes a new instance of the InvalidReservationException class.
    /// </summary>
    /// <param name="reservationId">The reservation Id.</param>
    /// <param name="message">The message.</param>
    public InvalidReservationException(Guid reservationId, string message)
        : base($"Invalid reservation {reservationId}: {message}")
    {
        ReservationId = reservationId;
    }

    /// <summary>
    /// Initializes a new instance of the InvalidReservationException class.
    /// </summary>
    /// <param name="message">The message.</param>
    public InvalidReservationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidReservationException class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner Exception.</param>
    public InvalidReservationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
