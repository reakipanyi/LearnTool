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

        // 缓存命中率统计
        private int _hitCount = 0;
        private int _missCount = 0;
        private readonly object _statsLock = new object();

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
            StartCleanupTask(cleanupIntervalMinutes);
            StartBackgroundLoad();
        }

        private void StartBackgroundLoad()
        {
            Task.Run(() =>
            {
                try
                {
                    LoadFromFile();
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Background cache load failed");
                }
            });
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

            // 一次性快照 keys，避免在遍历过程中 _cache 发生变化
            foreach (var key in _cache.Keys)
            {
                if (_cache.TryGetValue(key, out var item) && item.IsExpired)
                {
                    if (_cache.TryRemove(key, out var removedItem))
                    {
                        removedCount++;
                        // 释放 IDisposable 资源（如果有）
                        if (removedItem.Value is IDisposable disposable)
                        {
                            try { disposable.Dispose(); }
                            catch (Exception ex)
                            {
                                _logger?.LogWarning(ex, "释放缓存资源失败: {Key}", key);
                            }
                        }
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
                            RecordMiss();
                            return false;
                        }
                        item.Value = value;
                    }
                    else
                    {
                        value = (T)item.Value;
                    }
                    RecordHit();
                    return true;
                }
                _cache.TryRemove(key, out _);
            }
            RecordMiss();
            return false;
        }

        private void RecordHit()
        {
            lock (_statsLock)
            {
                _hitCount++;
            }
        }

        private void RecordMiss()
        {
            lock (_statsLock)
            {
                _missCount++;
            }
        }

        public (int Hits, int Misses) GetStatistics()
        {
            lock (_statsLock)
            {
                return (_hitCount, _missCount);
            }
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

        public void SetMany<T>(IDictionary<string, T> items, int? expirationMinutes = null)
        {
            if (items == null || items.Count == 0)
                return;

            var expiration = expirationMinutes.HasValue 
                ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                : DateTime.MaxValue;

            foreach (var kvp in items)
            {
                _cache[kvp.Key] = new CacheItem { Value = kvp.Value, ExpirationTime = expiration };
            }

            _logger?.LogDebug("Batch set {Count} cache items", items.Count);
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? expirationMinutes = null)
        {
            if (TryGet(key, out T value))
                return value;

            // 先尝试原子插入一个 AsyncLazy 占位，避免并发场景下重复调用 factory
            var lazyFactory = new Func<Task<T>>(async () =>
            {
                var result = await factory().ConfigureAwait(false);
                return result;
            });

            var lazyValue = _cache.GetOrAdd(key, k => new CacheItem
            {
                Value = new AsyncLazy<T>(lazyFactory),
                ExpirationTime = DateTime.MaxValue
            });

            // 如果当前项是 AsyncLazy，等待工厂完成
            if (lazyValue.Value is AsyncLazy<T> lazy)
            {
                value = await lazy.GetValueAsync().ConfigureAwait(false);
                lock (lazyValue)
                {
                    // 用实际值替换占位，并设置真正的过期时间
                    lazyValue.Value = value;
                    lazyValue.ExpirationTime = expirationMinutes.HasValue
                        ? DateTime.Now.AddMinutes(expirationMinutes.Value)
                        : DateTime.MaxValue;
                }
            }
            else
            {
                // 已有实际值且未过期，复用
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

        /// <summary>
        /// 缓存预热 - 异步加载常用数据到缓存
        /// 并发执行多个预热任务，提高启动速度
        /// </summary>
        public async Task<int> WarmupAsync(IDictionary<string, Func<Task>> warmupTasks, CancellationToken cancellationToken = default)
        {
            if (warmupTasks == null || warmupTasks.Count == 0)
                return 0;

            _logger?.LogInformation("开始缓存预热，共 {Count} 个任务", warmupTasks.Count);

            int successCount = 0;
            var tasks = new List<Task>();

            foreach (var kvp in warmupTasks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var keyPrefix = kvp.Key;
                var factory = kvp.Value;

                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        await factory().ConfigureAwait(false);
                        Interlocked.Increment(ref successCount);
                        _logger?.LogDebug("预热任务完成: {KeyPrefix}", keyPrefix);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "预热任务失败: {KeyPrefix}", keyPrefix);
                    }
                }, cancellationToken));
            }

            // 使用 Timeout 防止预热任务无限等待
            var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
            var allTasks = Task.WhenAll(tasks);

            try
            {
                await (await Task.WhenAny(allTasks, timeoutTask).ConfigureAwait(false)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogWarning("缓存预热已取消");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "缓存预热异常");
            }

            _logger?.LogInformation("缓存预热完成，成功: {Success}/{Total}", successCount, warmupTasks.Count);
            return successCount;
        }

        public void Persist()
        {
            try
            {
                // 仅持久化未过期的项，避免下次启动时再加载无效数据
                var cacheData = _cache
                    .Where(kvp => !kvp.Value.IsExpired)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => new CachedItemData { Value = kvp.Value.Value, ExpirationTime = kvp.Value.ExpirationTime });

                JsonHelper.SaveToFile(_cacheFilePath, cacheData);
                _logger?.LogDebug("已持久化 {Count} 个缓存项到 {Path}", cacheData.Count, _cacheFilePath);
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