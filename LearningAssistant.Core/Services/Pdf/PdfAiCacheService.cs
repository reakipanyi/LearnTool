using LearningAssistant.Common;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF AI 结果磁盘缓存实现：单文件单条目，容量超限时按最近访问时间淘汰。
    /// </summary>
    public sealed class PdfAiCacheService : IPdfAiCacheService
    {
        private const long DefaultMaxBytes = 64L * 1024 * 1024;
        private const int DefaultMaxEntries = 500;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly string _cacheRootPath;
        private readonly int _maxEntries;
        private readonly long _maxBytes;
        private readonly ILogger<PdfAiCacheService>? _logger;
        private readonly object _gate = new();

        private readonly Dictionary<string, CacheEntry> _entries = new(StringComparer.Ordinal);
        private long _sequence;
        private long _totalBytes;
        private bool _loaded;

        public PdfAiCacheService(
            string? cacheRootPath = null,
            int maxEntries = DefaultMaxEntries,
            long maxBytes = DefaultMaxBytes,
            ILogger<PdfAiCacheService>? logger = null)
        {
            _cacheRootPath = cacheRootPath ?? AppPaths.PdfAiCacheDir;
            _maxEntries = Math.Max(1, maxEntries);
            _maxBytes = Math.Max(1024, maxBytes);
            _logger = logger;
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                lock (_gate)
                {
                    EnsureLoaded();
                    return _entries.Count;
                }
            }
        }

        /// <inheritdoc />
        public string BuildKey(
            string fingerprint,
            IEnumerable<int> pageIndexes,
            string feature,
            string language,
            string promptVersion,
            string model,
            string? extra = null)
        {
            var pages = string.Join(",", pageIndexes.Where(p => p > 0).Distinct().OrderBy(p => p));
            var raw = $"{fingerprint}|{pages}|{feature}|{language}|{promptVersion}|{model}|{extra}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..40].ToLowerInvariant();
        }

        /// <inheritdoc />
        public bool TryGet(string key, out string content)
        {
            content = string.Empty;
            if (string.IsNullOrWhiteSpace(key)) return false;

            lock (_gate)
            {
                EnsureLoaded();

                if (!_entries.TryGetValue(key, out var entry))
                    return false;

                if (entry.Content == null)
                {
                    entry.Content = ReadEntryContent(entry.Path);
                    if (entry.Content == null)
                    {
                        // 文件损坏或已丢失，按未命中处理并移除条目
                        RemoveEntry(key);
                        return false;
                    }
                }

                entry.Sequence = ++_sequence;
                TouchFile(entry.Path);
                content = entry.Content;
                return true;
            }
        }

        /// <inheritdoc />
        public void Set(string key, string content)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(content)) return;

            lock (_gate)
            {
                EnsureLoaded();
                AppPaths.EnsureDirectoryExists(_cacheRootPath);

                var path = Path.Combine(_cacheRootPath, $"{key}.json");
                var payload = JsonSerializer.Serialize(new CachePayload
                {
                    Key = key,
                    Content = content,
                    CreatedAtUtc = DateTime.UtcNow
                }, JsonOptions);

                try
                {
                    var tempPath = path + ".tmp";
                    File.WriteAllText(tempPath, payload, new UTF8Encoding(false));
                    File.Move(tempPath, path, overwrite: true);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "写入 PDF AI 缓存失败: {Key}", key);
                    return;
                }

                if (_entries.TryGetValue(key, out var existing))
                {
                    _totalBytes -= existing.SizeBytes;
                }

                long size = payload.Length;
                _entries[key] = new CacheEntry(key, path, size)
                {
                    Content = content,
                    Sequence = ++_sequence
                };
                _totalBytes += size;

                EvictIfNeeded();
            }
        }

        private void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            try
            {
                AppPaths.EnsureDirectoryExists(_cacheRootPath);
                foreach (var file in Directory.EnumerateFiles(_cacheRootPath, "*.json"))
                {
                    var key = Path.GetFileNameWithoutExtension(file);
                    if (string.IsNullOrWhiteSpace(key) || _entries.ContainsKey(key)) continue;

                    long size = new FileInfo(file).Length;
                    _entries[key] = new CacheEntry(key, file, size)
                    {
                        Sequence = new FileInfo(file).LastWriteTimeUtc.Ticks
                    };
                    _totalBytes += size;
                }

                _sequence = Math.Max(_sequence, DateTime.UtcNow.Ticks);
                EvictIfNeeded();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "扫描 PDF AI 缓存目录失败: {Path}", _cacheRootPath);
            }
        }

        private string? ReadEntryContent(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;

                var json = File.ReadAllText(path, Encoding.UTF8);
                var payload = JsonSerializer.Deserialize<CachePayload>(json, JsonOptions);
                return string.IsNullOrWhiteSpace(payload?.Content) ? null : payload.Content;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取 PDF AI 缓存失败: {Path}", path);
                return null;
            }
        }

        private void EvictIfNeeded()
        {
            while (_entries.Count > 1 &&
                   (_entries.Count > _maxEntries || _totalBytes > _maxBytes))
            {
                var oldest = _entries.Values.OrderBy(e => e.Sequence).First();
                RemoveEntry(oldest.Key);
            }
        }

        private void RemoveEntry(string key)
        {
            if (!_entries.TryGetValue(key, out var entry)) return;

            _entries.Remove(key);
            _totalBytes -= entry.SizeBytes;
            try
            {
                if (File.Exists(entry.Path)) File.Delete(entry.Path);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "删除 PDF AI 缓存文件失败: {Path}", entry.Path);
            }
        }

        private void TouchFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "刷新 PDF AI 缓存访问时间失败: {Path}", path);
            }
        }

        private sealed class CacheEntry
        {
            public CacheEntry(string key, string path, long sizeBytes)
            {
                Key = key;
                Path = path;
                SizeBytes = sizeBytes;
            }

            public string Key { get; }
            public string Path { get; }
            public long SizeBytes { get; }
            public string? Content { get; set; }
            public long Sequence { get; set; }
        }

        private sealed class CachePayload
        {
            public string Key { get; set; } = string.Empty;
            public string Content { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
