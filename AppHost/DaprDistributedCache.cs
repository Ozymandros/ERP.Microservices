using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Dapr.Client;

namespace AppHost
{
    public class DaprDistributedCache : IDistributedCache
    {
        private readonly DaprClient _dapr;
        private readonly string _storeName;

        public DaprDistributedCache(DaprClient daprClient, string storeName = "statestore")
        {
            _dapr = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
            _storeName = storeName;
        }

        public byte[]? Get(string key)
        {
            return GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            var item = await _dapr.GetStateAsync<byte[]>(_storeName, key).ConfigureAwait(false);
            return item;
        }

        public void Refresh(string key)
        {
            // No-op for Dapr state
        }

        public Task RefreshAsync(string key, CancellationToken token = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _dapr.DeleteStateAsync(_storeName, key).GetAwaiter().GetResult();
        }

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            return _dapr.DeleteStateAsync(_storeName, key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            SetAsync(key, value, options).GetAwaiter().GetResult();
        }

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            return _dapr.SaveStateAsync(_storeName, key, value);
        }
    }
}
