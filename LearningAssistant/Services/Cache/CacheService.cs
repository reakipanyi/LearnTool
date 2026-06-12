using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using LearningAssistant.Common;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Cache
{
    public class CacheService : ICacheService, IDisposable
    {
        private readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();
        private readonly string _cacheFilePath;
        private readonly ILogger<CacheService>? _logger;
        private System.Timers.Timer? _cleanupTimer;
        private bool _disposed = false;

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

        public CacheService(string cacheFilePath, ILogger<CacheService>? logger = null, int cleanupIntervalMinutes = 30)
        {
            _cacheFilePath = cacheFilePath;
            _logger = logger;
            LoadFromFile();
            StartCleanupTask(cleanupIntervalMinutes);
        }

        private void StartCleanupTask(int intervalMinutes)
        {
            if (_cleanupTimer != null)
            {
                _cleanupTimer.Dispose();
            }

            _cleanupTimer = new System.Timers.Timer(TimeSpan.FromMinutes(intervalMinutes).TotalMilliseconds);
            _cleanupTimer.Elapsed += CleanupTimer_Elapsed;
            _cleanupTimer.Start();
            
            _logger?.LogInformation("Cache cleanup task started with interval: {Interval} minutes", intervalMinutes);
        }

        private void CleanupTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            CleanupExpiredItems();
        }

        public void CleanupExpiredItems()
        {
            int removedCount = 0;
            
            foreach (var key in _cache.Keys.ToList())
            {
                if (_cache.TryGetValue(key, out var item) && item.IsExpired)
                {
                    if (_cache.TryRemove(key, out _))
                    {
                        removedCount++;
                    }
                }
            }

            if (removedCount > 0)
            {
                _logger?.LogInformation("Cleaned up {Count} expired cache items", removedCount);
            }
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
                        try
                        {
                            value = lazy.GetValueAsync().GetAwaiter().GetResult();
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to get async cached value for key: {Key}", key);
                            _cache.TryRemove(key, out _);
                            return false;
                        }
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

        public async Task<T?> TryGetAsync<T>(string key)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                if (!item.IsExpired)
                {
                    if (item.Value is AsyncLazy<T> lazy)
                    {
                        try
                        {
                            var value = await lazy.GetValueAsync().ConfigureAwait(false);
                            item.Value = value;
                            return value;
                        }
                        catch (Exception ex)
                        {
                            _logger?.LogWarning(ex, "Failed to get async cached value for key: {Key}", key);
                            _cache.TryRemove(key, out _);
                            return default;
                        }
                    }
                    return (T)item.Value;
                }
                _cache.TryRemove(key, out _);
            }
            return default;
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
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to persist cache to file: {Path}", _cacheFilePath);
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
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to load cache from file: {Path}", _cacheFilePath);
            }
        }

        public int Count => _cache.Count;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _cleanupTimer?.Dispose();
            }

            _disposed = true;
        }
    }
}