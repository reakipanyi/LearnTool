using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
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

        private class AsyncLazy<T>
        {
            private readonly Lazy<Task<T>> _lazy;

            public AsyncLazy(Func<Task<T>> factory)
            {
                _lazy = new Lazy<Task<T>>(() => Task.Run(factory), LazyThreadSafetyMode.ExecutionAndPublication);
            }

            public TaskAwaiter<T> GetAwaiter()
            {
                return _lazy.Value.GetAwaiter();
            }

            public Task<T> GetValueAsync()
            {
                return _lazy.Value;
            }
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
                    if (item.Value is AsyncLazy<T> lazy)
                    {
                        value = lazy.GetValueAsync().Result;
                        item.Value = value;
                    }
                    else
                    {
                        value = (T)item.Value;
                    }
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

            var expiration = expirationMinutes.HasValue 
                ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                : DateTime.MaxValue;

            var lazyFactory = new Func<Task<T>>(async () =>
            {
                var result = await factory().ConfigureAwait(false);
                return result;
            });

            var lazyValue = _cache.GetOrAdd(key, k => new CacheItem
            {
                Value = new AsyncLazy<T>(lazyFactory),
                ExpirationTime = expiration
            });

            if (lazyValue.Value is AsyncLazy<T> lazy)
            {
                value = await lazy.GetValueAsync().ConfigureAwait(false);
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

        private class CachedItemData
        {
            public object Value { get; set; } = null!;
            public DateTime ExpirationTime { get; set; }
        }

        public void Persist()
        {
            try
            {
                var cacheData = _cache.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new CachedItemData { Value = kvp.Value.Value, ExpirationTime = kvp.Value.ExpirationTime });
                
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
                var cacheData = JsonHelper.LoadFromFile<Dictionary<string, CachedItemData>>(_cacheFilePath);
                if (cacheData != null)
                {
                    foreach (var kvp in cacheData)
                    {
                        var cachedItem = kvp.Value;
                        if (cachedItem.ExpirationTime > DateTime.Now)
                        {
                            _cache.TryAdd(kvp.Key, new CacheItem { Value = cachedItem.Value, ExpirationTime = cachedItem.ExpirationTime });
                        }
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