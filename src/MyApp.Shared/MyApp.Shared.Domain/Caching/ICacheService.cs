namespace MyApp.Shared.Domain.Caching;

public interface ICacheService
{
    // Gets the typed state
    Task<T?> GetStateAsync<T>(string key) where T : class;

    // Saves the typed state with expiration options
    Task SaveStateAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    // Removes the state
    Task RemoveStateAsync(string key);
}