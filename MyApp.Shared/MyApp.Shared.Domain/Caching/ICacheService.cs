public interface ICacheService
{
    // Obté l'estat tipat
    Task<T?> GetStateAsync<T>(string key) where T : class;

    // Desa l'estat tipat amb opcions de caducitat
    Task SaveStateAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    // Elimina l'estat
    Task RemoveStateAsync(string key);
}