namespace MyApp.Shared.Domain.Security;

/// <summary>
/// Strips CR/LF from user-controlled values before they are written to logs.
/// </summary>
public sealed class LogSanitizer : ILogSanitizer
{
    public string Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\r", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal);
    }
}
