namespace MyApp.Shared.Domain.Security;

/// <summary>
/// Sanitizes user-controlled values before they are written to logs (CodeQL cs/log-forging).
/// </summary>
public interface ILogSanitizer
{
    /// <summary>Sanitizes the given value by replacing characters that could cause log injection, returning a safe string suitable for structured logging.</summary>
    /// <param name="value">The user-controlled value to sanitize, or <see langword="null"/>.</param>
    /// <returns>A sanitized string safe for use in log messages.</returns>
    string Sanitize(string? value);
}
