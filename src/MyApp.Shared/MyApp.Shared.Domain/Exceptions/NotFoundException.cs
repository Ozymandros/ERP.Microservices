namespace MyApp.Shared.Domain.Exceptions;

/// <summary>
/// Thrown when a requested entity cannot be found.
/// </summary>
public class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a message.
    /// </summary>
    public NotFoundException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with entity name and key.
    /// </summary>
    public NotFoundException(string entityName, object key)
        : base($"{entityName} with key '{key}' was not found.") { }

    /// <summary>
    /// Initializes a new instance of the NotFoundException class with a message and inner exception.
    /// </summary>
    public NotFoundException(string message, Exception inner) : base(message, inner) { }
}
