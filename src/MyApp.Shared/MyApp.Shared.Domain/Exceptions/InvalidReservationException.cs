namespace MyApp.Shared.Domain.Exceptions;

public class InvalidReservationException : Exception
{
    public Guid? ReservationId { get; }

    public InvalidReservationException(Guid reservationId, string message)
        : base($"Invalid reservation {reservationId}: {message}")
    {
        ReservationId = reservationId;
    }

    public InvalidReservationException(string message) : base(message)
    {
    }

    public InvalidReservationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
