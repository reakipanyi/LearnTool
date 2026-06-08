using System.Collections.Concurrent;
using LearningAssistant.Common;

namespace LearningAssistant.Services.Cache
{
    public class CacheService : ICacheService
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();
        private readonly string _cacheFilePath;

        private class CacheItem
        {
            public object Value { get; set; } = null!;
            public DateTime ExpirationTime { get; set; }
            public bool IsExpired => DateTime.Now > ExpirationTime;
        }

        public CacheService(string cacheFilePath)
        {
            _cacheFilePath = cacheFilePath;
            LoadFromFile();
        }

        public bool TryGet<T>(string key, out T value)
        {
            value = default!;
            
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    value = (T)item.Value;
                    return true;
                }
                _cache.TryRemove(key, out _);
            }
            
            return false;
        }

        public void Set<T>(string key, T value, int? expirationMinutes = null)
        {
            var expiration = expirationMinutes.HasValue 
                ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                : DateTime.MaxValue;
            
            _cache[key] = new CacheItem { Value = value, ExpirationTime = expiration };
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? expirationMinutes = null)
        {
            if (TryGet(key, out T value))
                return value;

            var lazyValue = _cache.GetOrAdd(key, k => new CacheItem 
            { 
                Value = Task.Run(async () => 
                {
                    var result = await factory();
                    return result;
                }),
                ExpirationTime = expirationMinutes.HasValue 
                    ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                    : DateTime.MaxValue
            });

            if (lazyValue.Value is Task<T> task)
            {
                value = await task;
                lazyValue.Value = value;
                lazyValue.ExpirationTime = expirationMinutes.HasValue 
                    ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                    : DateTime.MaxValue;
            }
            else
            {
                value = (T)lazyValue.Value;
            }

            return value;
        }

        public void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public void Clear()
        {
            _cache.Clear();
        }

        public void Persist()
        {
            try
            {
                var cacheData = _cache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { Value = kvp.Value.Value, Expiration = kvp.Value.ExpirationTime });
                
                JsonHelper.SaveToFile(_cacheFilePath, cacheData);
            }
            catch
            {
            }
        }

        private void LoadFromFile()
        {
            try
            {
                var cacheData = JsonHelper.LoadFromFile<Dictionary<string, object>>(_cacheFilePath);
                if (cacheData != null)
                {
                    foreach (var kvp in cacheData)
                    {
                        _cache.TryAdd(kvp.Key, new CacheItem { Value = kvp.Value, ExpirationTime = DateTime.MaxValue });
                    }
                }
            }
            catch
            {
            }
        }

        public int Count => _cache.Count;
    }
}