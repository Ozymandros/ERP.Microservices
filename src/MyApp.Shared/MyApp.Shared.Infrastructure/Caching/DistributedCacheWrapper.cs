// DistributedCacheWrapper.cs

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;
using MyApp.Shared.Domain.Caching;

namespace MyApp.Shared.Infrastructure.Caching;

/// <summary>
/// Wraps <see cref="IDistributedCache"/> to implement <see cref="ICacheService"/> with JSON serialization.
/// </summary>
public class DistributedCacheWrapper : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    /// <summary>
    /// Initializes a new instance of <see cref="DistributedCacheWrapper"/> with the provided distributed cache.
    /// </summary>
    public DistributedCacheWrapper(IDistributedCache distributedCache)
    {
        ArgumentNullException.ThrowIfNull(distributedCache);
        _distributedCache = distributedCache;
    }

    /// <summary>Retrieves a cached value by key, deserializing from JSON. Returns null if not found or deserialization fails.</summary>
    public async Task<T?> GetStateAsync<T>(string key) where T : class
    {
        // 1. Get raw bytes from Redis
        var cachedBytes = await _distributedCache.GetAsync(key);

        if (cachedBytes == null)
        {
            return null;
        }

        // 2. Deserialize bytes to typed object (T)
        try
        {
            var json = Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            // Maybe the format is incorrect, remove the entry to avoid future errors
            await _distributedCache.RemoveAsync(key);
            return null;
        }
    }

    /// <summary>Serializes and stores a value in the cache with an optional expiration. Defaults to 1 hour if no expiration is specified.</summary>
    public Task SaveStateAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        // 1. Serialize the typed object (T) to bytes
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        var options = new DistributedCacheEntryOptions();

        // DistributedCacheEntryOptions requires positive expiration values
        // If expiration is null or zero, use default (1 hour)
        if (expiration.HasValue && expiration.Value > TimeSpan.Zero)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
        }

        // 2. Save bytes to Redis with options
        return _distributedCache.SetAsync(key, bytes, options);
    }

    /// <summary>Removes the cached value associated with the specified key.</summary>
    public Task RemoveStateAsync(string key)
    {
        // Simple delegation to base Redis functionality
        return _distributedCache.RemoveAsync(key);
    }
}