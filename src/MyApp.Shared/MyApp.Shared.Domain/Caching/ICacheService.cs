namespace MyApp.Shared.Domain.Caching;

/// <summary>
/// Provides caching service abstraction for distributed cache operations.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a cached value by key and deserializes it to the specified type.
    /// </summary>
    Task<T?> GetStateAsync<T>(string key) where T : class;

    /// <summary>
    /// Saves a value to the cache with optional expiration duration.
    /// </summary>
    Task SaveStateAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Removes a cached value by key.
    /// </summary>
    Task RemoveStateAsync(string key);
}