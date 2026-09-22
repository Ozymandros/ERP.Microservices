namespace MyApp.Shared.Domain.Security;

/// <summary>
/// Strips CR/LF from user-controlled values before they are written to logs.
/// </summary>
public sealed class LogSanitizer : ILogSanitizer
{
    /// <summary>
    /// Strips carriage return and line feed characters from the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The resulting string.</returns>
    public string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        // Use the parameterless Replace overloads so CodeQL's built-in
        // cs/log-forging sanitizers recognize this as a barrier.
        return value.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
