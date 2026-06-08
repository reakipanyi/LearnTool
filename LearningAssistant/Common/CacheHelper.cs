using System.Collections.Concurrent;

namespace LearningAssistant.Common
{
    public static class CacheHelper
    {
        private static readonly ConcurrentDictionary<string, CacheItem> _cache = new ConcurrentDictionary<string, CacheItem>();

        private class CacheItem
        {
            public object Value { get; set; }
            public DateTime ExpirationTime { get; set; }
            public bool IsExpired => DateTime.Now > ExpirationTime;
        }

        public static bool TryGet<T>(string key, out T value)
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

        public static void Set<T>(string key, T value, int? expirationMinutes = null)
        {
            var expiration = expirationMinutes.HasValue 
                ? DateTime.Now.AddMinutes(expirationMinutes.Value) 
                : DateTime.MaxValue;
            
            _cache[key] = new CacheItem { Value = value, ExpirationTime = expiration };
        }

        public static T? GetOrCreate<T>(string key, Func<T> factory, int? expirationMinutes = null)
        {
            if (TryGet(key, out T value))
                return value;
            
            value = factory();
            Set(key, value, expirationMinutes);
            return value;
        }

        public static void Remove(string key)
        {
            _cache.TryRemove(key, out _);
        }

        public static void Clear()
        {
            _cache.Clear();
        }

        public static int Count => _cache.Count;

        public static void CleanupExpired()
        {
            foreach (var key in _cache.Keys.Where(k => _cache[k].IsExpired).ToList())
            {
                _cache.TryRemove(key, out _);
            }
        }
    }
}