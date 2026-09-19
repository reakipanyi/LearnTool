using System.Text;
using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 网盘快照磁盘持久化缓存服务。
/// 解决：关闭程序后 IMemoryCache 失效、大目录重复拉取耗时长的问题。
/// 缓存路径：AppPaths.CacheDir/pan_snapshots/{目录名}_{yyyyMMdd_HHmmss}.json
/// 命名规则：当前目录 + 拉取日期时间，同一目录多次拉取会累积为历史快照文件。
/// 过期策略：按 AnalysisOptions.DiskCacheExpirationHours，0 = 永不过期（仅手动清理）。
/// </summary>
public class PanSnapshotCacheService
{
    private readonly ILogger<PanSnapshotCacheService>? _logger;
    private readonly string _cacheDir;
    private readonly object _lock = new();
    private readonly IAppPaths? _appPaths;

    public PanSnapshotCacheService(ILogger<PanSnapshotCacheService>? logger = null, IAppPaths? appPaths = null)
    {
        _logger = logger;
        _appPaths = appPaths;
        _cacheDir = Path.Combine(_appPaths?.CacheDir ?? AppPaths.CacheDir, "pan_snapshots");
        try { Directory.CreateDirectory(_cacheDir); } catch { /* ignore */ }
    }

    /// <summary>由目录路径生成文件名前缀（取最后一级目录名，根目录用 root）</summary>
    private static string SanitizeDirName(string? directoryPath)
    {
        var name = (directoryPath ?? "").Trim().TrimEnd('/');
        if (string.IsNullOrEmpty(name)) return "root";
        var idx = name.LastIndexOf('/');
        if (idx >= 0) name = name.Substring(idx + 1);
        if (string.IsNullOrEmpty(name)) return "root";
        var invalid = Path.GetInvalidFileNameChars();
        foreach (var c in invalid) name = name.Replace(c, '_');
        return name;
    }

    /// <summary>目录路径相等比较（忽略大小写与尾斜杠）</summary>
    private static bool PathEqualsIgnoreCase(string? a, string? b)
    {
        var na = (a ?? "").TrimEnd('/');
        var nb = (b ?? "").TrimEnd('/');
        return string.Equals(na, nb, StringComparison.OrdinalIgnoreCase);
    }

    private static PanDirectorySnapshot? Deserialize(string path)
    {
        var json = File.ReadAllText(path);
        return JsonConvert.DeserializeObject<PanDirectorySnapshot>(json,
            new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
    }

    /// <summary>生成快照文件路径：{目录名}_{yyyyMMdd_HHmmss}.json，同一秒重复时追加毫秒避免覆盖历史。</summary>
    private string BuildSnapshotFilePath(string directoryPath)
    {
        var dirName = SanitizeDirName(directoryPath);
        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var path = Path.Combine(_cacheDir, $"{dirName}_{ts}.json");
        if (File.Exists(path))
            path = Path.Combine(_cacheDir, $"{dirName}_{ts}_{DateTime.Now:fff}.json");
        return path;
    }

    /// <summary>
    /// 尝试读取磁盘缓存：取该目录最新且未过期（过期跳过，保留历史文件）的快照，找不到或异常返回 null。
    /// </summary>
    public PanDirectorySnapshot? TryLoad(string directoryPath, AnalysisOptions options)
    {
        try
        {
            if (!options.UseDiskCache) return null;
            var dirName = SanitizeDirName(directoryPath);
            var files = Directory.GetFiles(_cacheDir, $"{dirName}_*.json")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f));
            foreach (var path in files)
            {
                try
                {
                    if (options.DiskCacheExpirationHours > 0
                        && (DateTime.UtcNow - File.GetLastWriteTimeUtc(path)).TotalHours > options.DiskCacheExpirationHours)
                        continue;   // 过期：命中下一个更早的快照；全部过期则返回 null 走 API

                    var snapshot = Deserialize(path);
                    if (snapshot == null
                        || !PathEqualsIgnoreCase(snapshot.DirectoryPath, directoryPath)
                        || snapshot.IsFoldersOnly != options.FoldersOnly)   // 区分「仅文件夹/完整」快照，避免串用
                        continue;
                    snapshot.Source = PanSnapshotSource.DiskCache;
                    _logger?.LogInformation("使用磁盘快照缓存：{Dir}（{Count} 个文件）", directoryPath, snapshot.Files.Count);
                    return snapshot;
                }
                catch { /* 单个文件损坏则尝试下一个 */ }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "读取磁盘快照缓存失败，忽略并走 API：{Dir}", directoryPath);
        }
        return null;
    }

    /// <summary>写入磁盘缓存（命名：{目录名}_{日期时间}.json，串行化写入防止并发写坏 JSON）</summary>
    public void Save(PanDirectorySnapshot snapshot, AnalysisOptions options)
    {
        if (snapshot == null || !options.UseDiskCache) return;
        try
        {
            var path = BuildSnapshotFilePath(snapshot.DirectoryPath);
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

    /// <summary>
    /// 加载最近一次成功拉取的磁盘快照。
    /// directoryPath 为空时返回全局最新的快照；指定目录时只返回该目录的快照。
    /// </summary>
    public PanDirectorySnapshot? LoadLatest(string? directoryPath = null)
    {
        try
        {
            IEnumerable<string> files = Directory.GetFiles(_cacheDir, "*.json");
            if (!string.IsNullOrEmpty(directoryPath))
            {
                var dirName = SanitizeDirName(directoryPath);
                files = files.Where(f => Path.GetFileName(f).StartsWith(dirName + "_", StringComparison.OrdinalIgnoreCase));
            }
            foreach (var path in files.OrderByDescending(f => File.GetLastWriteTimeUtc(f)))
            {
                try
                {
                    var snapshot = Deserialize(path);
                    if (snapshot == null) continue;
                    if (!string.IsNullOrEmpty(directoryPath)
                        && !PathEqualsIgnoreCase(snapshot.DirectoryPath, directoryPath)) continue;
                    snapshot.Source = PanSnapshotSource.DiskCache;
                    return snapshot;
                }
                catch { /* 单个文件损坏则尝试下一个 */ }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "加载最近磁盘快照失败");
        }
        return null;
    }

    /// <summary>清除指定目录的磁盘缓存（删除该目录全部历史快照文件），返回删除数量</summary>
    public int ClearByPath(string directoryPath, AnalysisOptions options)
    {
        int removed = 0;
        try
        {
            var dirName = SanitizeDirName(directoryPath);
            lock (_lock)
            {
                foreach (var f in Directory.GetFiles(_cacheDir, $"{dirName}_*.json"))
                {
                    try
                    {
                        var snapshot = Deserialize(f);
                        // 同名目录可能来自不同父路径，读取文件内 DirectoryPath 二次校验，避免误删
                        if (snapshot != null && !PathEqualsIgnoreCase(snapshot.DirectoryPath, directoryPath)) continue;
                        File.Delete(f); removed++;
                    }
                    catch { try { File.Delete(f); removed++; } catch { /* ignore */ } }
                }
            }
            _logger?.LogInformation("已清除 {Dir} 的磁盘快照缓存：{Count} 个文件", directoryPath, removed);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "清除磁盘快照缓存失败：{Dir}", directoryPath);
        }
        return removed;
    }

    /// <summary>清除所有磁盘缓存文件，返回清理数量</summary>
    public int ClearAll()
    {
        int removed = 0;
        try
        {
            lock (_lock)
            {
                foreach (var f in Directory.GetFiles(_cacheDir, "*.json"))
                {
                    try { File.Delete(f); removed++; }
                    catch { /* ignore */ }
                }
            }
            _logger?.LogInformation("已清除 {Count} 个磁盘快照缓存文件", removed);
        }
        catch (Exception ex) { _logger?.LogWarning(ex, "清除磁盘缓存失败"); }
        return removed;
    }

    /// <summary>清理超过 N 天的缓存文件（启动时调用），返回清理数量</summary>
    public int CleanupOldCache(int olderThanDays = 7)
    {
        int removed = 0;
        try
        {
            lock (_lock)
            {
                foreach (var f in Directory.GetFiles(_cacheDir, "*.json"))
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
