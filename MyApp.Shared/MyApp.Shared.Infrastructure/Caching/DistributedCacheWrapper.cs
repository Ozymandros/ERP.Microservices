// DistributedCacheWrapper.cs

using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;
using System.Text;

// ?? Aquesta classe rep la IDistributedCache per DI, no la implementa.
public class DistributedCacheWrapper : ICacheService
{
    private readonly IDistributedCache _distributedCache;

    // ?? Rep la inst�ncia d'IDistributedCache per Ctor
    public DistributedCacheWrapper(IDistributedCache distributedCache)
    {
        _distributedCache = distributedCache;
    }

    public async Task<T?> GetStateAsync<T>(string key) where T : class
    {
        // 1. Obt� els bytes crus de Redis
        var cachedBytes = await _distributedCache.GetAsync(key);

        if (cachedBytes == null)
        {
            return null;
        }

        // 2. Deserialitza els bytes a l'objecte tipat (T)
        try
        {
            var json = Encoding.UTF8.GetString(cachedBytes);
            return JsonSerializer.Deserialize<T>(json);
        }
        catch (JsonException)
        {
            // Potser el format �s incorrecte, esborrem l'entrada per evitar errors futurs
            await _distributedCache.RemoveAsync(key);
            return null;
        }
    }

    public Task SaveStateAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        // 1. Serialitza l'objecte tipat (T) a bytes
        var json = JsonSerializer.Serialize(value);
        var bytes = Encoding.UTF8.GetBytes(json);

        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        // 2. Desa els bytes a Redis amb les opcions
        return _distributedCache.SetAsync(key, bytes, options);
    }

    public Task RemoveStateAsync(string key)
    {
        // Simple delegaci� a la funcionalitat base de Redis
        return _distributedCache.RemoveAsync(key);
    }
}