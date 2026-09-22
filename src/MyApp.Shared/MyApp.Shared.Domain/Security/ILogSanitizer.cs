namespace MyApp.Shared.Domain.Security;

/// <summary>
/// Sanitizes user-controlled values before they are written to logs (CodeQL cs/log-forging).
/// </summary>
public interface ILogSanitizer
{
    string Sanitize(string? value);
}
