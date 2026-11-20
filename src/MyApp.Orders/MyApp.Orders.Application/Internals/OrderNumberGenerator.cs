namespace MyApp.Orders.Application.Internals;

/// <summary>Generates operational order numbers.</summary>
public static class OrderNumberGenerator
{
    /// <summary>Creates a unique order number without scanning the orders table.</summary>
    public static Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTime.UtcNow;
        var random = Guid.NewGuid().ToString("N")[..12];
        return Task.FromResult($"ORD-{now:yyyyMMddHHmmss}-{random}");
    }
}