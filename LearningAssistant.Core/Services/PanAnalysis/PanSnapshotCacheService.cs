using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Common;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 网盘快照磁盘持久化缓存服务。
/// 解决：关闭程序后 IMemoryCache 失效、大目录重复拉取耗时长的问题。
/// 缓存路径：AppPaths.CacheDir/pan_snapshots/{hash}.json
/// 过期策略：按 AnalysisOptions.DiskCacheExpirationHours，0 = 永不过期。
/// </summary>
public class PanSnapshotCacheService
{
    private readonly ILogger<PanSnapshotCacheService>? _logger;
    private readonly string _cacheDir;
    private readonly object _lock = new();

    public PanSnapshotCacheService(ILogger<PanSnapshotCacheService>? logger = null)
    {
        _logger = logger;
        _cacheDir = Path.Combine(AppPaths.CacheDir, "pan_snapshots");
        try { Directory.CreateDirectory(_cacheDir); } catch { /* ignore */ }
    }

    /// <summary>生成磁盘缓存 key（基于 directoryPath + MaxDepth + MaxFileCount + SkipSize）</summary>
    public string BuildCacheKey(string directoryPath, AnalysisOptions options)
    {
        var raw = $"{directoryPath.TrimEnd('/').ToLowerInvariant()}|D{options.MaxDepth}|F{options.MaxFileCount}|S{(options.SkipFileSizeComputing ? 1 : 0)}";
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 24);
    }

    public string GetCacheFilePath(string key) => Path.Combine(_cacheDir, $"snap_{key}.json");

    /// <summary>尝试读取磁盘缓存，过期或异常返回 null</summary>
    public PanDirectorySnapshot? TryLoad(string directoryPath, AnalysisOptions options)
    {
        try
        {
            if (!options.UseDiskCache) return null;
            var key = BuildCacheKey(directoryPath, options);
            var path = GetCacheFilePath(key);
            if (!File.Exists(path)) return null;

            var fileTime = File.GetLastWriteTimeUtc(path);
            if (options.DiskCacheExpirationHours > 0
                && (DateTime.UtcNow - fileTime).TotalHours > options.DiskCacheExpirationHours)
            {
                try { File.Delete(path); } catch { /* ignore */ }
                _logger?.LogDebug("磁盘快照缓存已过期：{Dir}", directoryPath);
                return null;
            }

            var json = File.ReadAllText(path);
            var snapshot = JsonConvert.DeserializeObject<PanDirectorySnapshot>(json,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            if (snapshot == null) return null;
            snapshot.Source = PanSnapshotSource.DiskCache;
            _logger?.LogInformation("使用磁盘快照缓存：{Dir}（{Count} 个文件）", directoryPath, snapshot.Files.Count);
            return snapshot;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "读取磁盘快照缓存失败，忽略并走 API：{Dir}", directoryPath);
            return null;
        }
    }

    /// <summary>写入磁盘缓存（串行化写入，防止并发写坏 JSON）</summary>
    public void Save(PanDirectorySnapshot snapshot, AnalysisOptions options)
    {
        if (snapshot == null || !options.UseDiskCache) return;
        try
        {
            var key = BuildCacheKey(snapshot.DirectoryPath, options);
            var path = GetCacheFilePath(key);
            var json = JsonConvert.SerializeObject(snapshot, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            lock (_lock)
            {
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, json, Encoding.UTF8);
                if (File.Exists(path)) try { File.Delete(path); } catch { /* ignore */ }
                File.Move(tmp, path);
            }
            _logger?.LogInformation("磁盘快照缓存已保存：{Dir} -> {Path}", snapshot.DirectoryPath, Path.GetFileName(path));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "保存磁盘快照缓存失败，忽略：{Dir}", snapshot.DirectoryPath);
        }
    }

    /// <summary>清理超过 N 天的缓存文件（启动时调用），返回清理数量</summary>
    public int CleanupOldCache(int olderThanDays = 7)
    {
        int removed = 0;
        try
        {
            lock (_lock)
            {
                foreach (var f in Directory.GetFiles(_cacheDir, "snap_*.json"))
                {
                    try
                    {
                        if ((DateTime.UtcNow - File.GetLastWriteTimeUtc(f)).TotalDays > olderThanDays)
                        { File.Delete(f); removed++; }
                    }
                    catch { /* ignore */ }
                }
            }
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "清理旧缓存失败"); }
        return removed;
    }
}
