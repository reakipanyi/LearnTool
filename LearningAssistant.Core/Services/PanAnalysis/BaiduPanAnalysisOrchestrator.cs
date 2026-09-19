using System.Runtime.CompilerServices;
using System.Text;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Baidu;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 百度网盘 AI 分析编排器（实现）
/// </summary>
public class BaiduPanAnalysisOrchestrator : IBaiduPanAnalysisOrchestrator
{
    private readonly IPanTokenManager _tokenManager;
    private readonly IMemoryCache _cache;
    private readonly PanSnapshotCacheService _diskCache;
    private readonly ILogger<BaiduPanAnalysisOrchestrator> _logger;
    private readonly IPanExecutionEngine _executionEngine;
    private readonly PanAiService? _aiService;
    private readonly string _defaultModelName;

    private CancellationTokenSource? _cancellationTokenSource;

    public bool IsAvailable => _tokenManager.IsTokenValid;

    public BaiduPanAnalysisOrchestrator(
        IPanTokenManager tokenManager,
        IMemoryCache cache,
        ILoggerFactory? loggerFactory = null,
        PanAiService? aiService = null)
    {
        _tokenManager = tokenManager;
        _cache = cache;
        _logger = loggerFactory?.CreateLogger<BaiduPanAnalysisOrchestrator>() ?? null!;
        _diskCache = new PanSnapshotCacheService(loggerFactory?.CreateLogger<PanSnapshotCacheService>());
        _executionEngine = new PanExecutionEngine(_tokenManager, loggerFactory?.CreateLogger<PanExecutionEngine>() ?? null!);
        _aiService = aiService;
        _defaultModelName = "deepseek-v3-241226";
    }

    public async Task<PanAnalysisResult> AnalyzeDirectoryAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Fetching, Current = 0, Total = 100, Message = "正在获取目录快照..." });

        // 1. 获取快照
        var snapshot = await GetSnapshotAsync(directoryPath, options, progress, cancellationToken);

        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Analyzing, Current = 85, Total = 100, Message = "正在 AI 分析..." });

        // 2. 检查内存缓存
        var cacheKey = $"pan_analysis_{directoryPath}_{options.MaxDepth}_{options.MaxFileCount}_{_defaultModelName}";
        if (options.UseCache && _cache.TryGetValue(cacheKey, out PanAnalysisResult? cached) && cached != null)
        {
            progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Completed, Current = 100, Total = 100, Message = "使用缓存的分析结果" });
            return cached;
        }

        // 3. 调用 AI 分析
        PanAnalysisResult result;
        if (_aiService != null)
        {
            try
            {
                var prompt = BuildAnalysisPrompt(snapshot);
                var aiResponse = await _aiService.ChatAsync(prompt, _defaultModelName, cancellationToken);
                result = ParseAiResponse(aiResponse, snapshot);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "AI 分析失败，返回空结果");
                result = new PanAnalysisResult
                {
                    Recommendations = new List<PanRecommendation>(),
                    Summary = "AI 分析失败：" + ex.Message
                };
            }
        }
        else
        {
            result = new PanAnalysisResult
            {
                Recommendations = new List<PanRecommendation>(),
                Summary = "AI 服务未配置（PanAiService 未注册），跳过 AI 分析"
            };
        }

        // 4. 缓存结果
        if (options.UseCache && result.Recommendations.Count > 0)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(options.CacheExpirationMinutes));
        }

        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Completed, Current = 100, Total = 100, Message = "分析完成" });
        return result;
    }

    public async Task<PanDirectorySnapshot> GetSnapshotAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // 缓存 key 需区分「仅文件夹 / 完整」，避免两类快照串用
        var cacheKey = BuildSnapshotCacheKey(directoryPath, options);

        // 1. 尝试磁盘缓存
        var diskHit = _diskCache.TryLoad(directoryPath, options);
        if (diskHit != null)
        {
            if (options.UseCache)
                _cache.Set(cacheKey, diskHit, TimeSpan.FromMinutes(options.CacheExpirationMinutes));
            progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Completed, Current = 100, Total = 100, Message = "使用磁盘缓存快照" });
            return diskHit;
        }

        // 2. 尝试内存缓存
        if (options.UseCache && _cache.TryGetValue(cacheKey, out PanDirectorySnapshot? cached) && cached != null)
        {
            progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Completed, Current = 100, Total = 100, Message = "使用内存缓存快照" });
            return cached;
        }

        // 3. 从 API 拉取
        var sharedCts = GetOrCreateCancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, sharedCts.Token);

        // 刷新 Token
        var token = await _tokenManager.EnsureValidTokenAsync();
        if (string.IsNullOrEmpty(token))
            throw new InvalidOperationException("Token 无效或已过期，请重新授权");

        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Fetching, Current = 5, Total = 100, Message = "正在准备遍历目录..." });

        // 遍历目录
        using var apiClient = new BaiduPanApiClient(token);
        var rootPath = directoryPath.TrimEnd('/');
        var allFiles = new List<PanFileInfo>();
        var rawFolders = new List<BaseFileInfo>();
        long totalSize = 0;
        int totalFiles = 0;

        await foreach (var (file, depth) in TraverseDirectoryAsync(apiClient, rootPath, options.MaxDepth, progress, totalFiles, linkedCts.Token, options.FoldersOnly))
        {
            if (file.IsDir == 1)
            {
                rawFolders.Add(file);
            }
            else
            {
                allFiles.Add(MapToPanFileInfo(file, rootPath, options.SkipFileSizeComputing));
                totalSize += options.SkipFileSizeComputing ? 0 : file.Size;
                totalFiles++;
            }

            // 进度报告（每 100 个文件报告一次，降低首次反馈时间）
            if (totalFiles % 100 == 0)
            {
                var pct = Math.Min(10 + (int)((double)totalFiles / Math.Max(options.MaxFileCount, 1) * 70), 80);
                progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Fetching, Current = pct, Total = 100, Message = $"已遍历 {totalFiles} 个文件..." });
            }

            // 文件数上限截断
            if (options.MaxFileCount > 0 && allFiles.Count >= options.MaxFileCount)
            {
                _logger?.LogInformation("达到文件数上限 {Max}, 截断遍历", options.MaxFileCount);
                break;
            }

            linkedCts.Token.ThrowIfCancellationRequested();
        }

        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.PreComputing, Current = 85, Total = 100, Message = "正在构建快照..." });

        // 过滤空文件夹
        var folders = FilterOutEmptyFolders(allFiles, rawFolders);

        // 构建快照
        var snapshot = new PanDirectorySnapshot
        {
            DirectoryPath = rootPath,
            Files = allFiles,
            Folders = folders.Select(f => MapToPanFolderInfo(f, rootPath)).ToList(),
            Scope = new AnalysisScope
            {
                TotalFileCount = allFiles.Count,
                TotalFolderCount = folders.Count,
                TotalSizeBytes = totalSize,
                MaxDepth = options.MaxDepth,
                MaxFileCount = options.MaxFileCount
            },
            IsComplete = options.FoldersOnly || options.MaxFileCount <= 0 || allFiles.Count < options.MaxFileCount,
            Source = PanSnapshotSource.Api,
            IsFoldersOnly = options.FoldersOnly,
            Statistics = ComputeStatistics(allFiles, folders, options)
        };

        // 逐个文件夹统计总大小/文件数/子文件夹数（自底向上聚合），写入快照供整理界面展示
        ComputeFolderSizes(snapshot.Folders, snapshot.Files);

        // 重复检测
        if (options.DetectDuplicates)
        {
            snapshot.Duplicates = DetectDuplicates(allFiles);
            _logger?.LogInformation("重复检测完成：{Count} 组", snapshot.Duplicates.Count);
        }

        // 缓存结果
        if (options.UseCache)
            _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(options.CacheExpirationMinutes));
        _diskCache.Save(snapshot, options);

        progress?.Report(new PanAnalysisProgress { Phase = PanAnalysisPhase.Completed, Current = 100, Total = 100, Message = "快照获取完成" });
        return snapshot;
    }

    public void Cancel()
    {
        var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
        cts?.Cancel();
        cts?.Dispose();
    }

    public void ReloadTokenState()
    {
        // 授权成功后 OAuth 流程会把新凭据写入共享的 CloudStorageConfig，
        // 此处让 TokenManager 从配置重新加载，使 IsAvailable 反映最新凭据，
        // 避免授权成功后再次点整理仍被判定为未授权。
        _tokenManager.ReloadFromConfig();
    }

    public void ClearCache()
    {
        try
        {
            // 清除内存缓存
            if (_cache is MemoryCache mc)
                mc.Compact(1.0);
            // 清除磁盘缓存
            var removed = _diskCache.ClearAll();
            _logger?.LogInformation("网盘缓存已清除：内存缓存已清空，清理了 {Count} 个磁盘缓存文件", removed);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "清除网盘缓存时发生异常");
        }
    }

    public void ClearCacheByPath(string directoryPath, AnalysisOptions options)
    {
        try
        {
            // 清除内存缓存（快照 key 含目录+参数，简单起见整体压缩；内存缓存分钟级，代价低）
            if (_cache is MemoryCache mc)
                mc.Compact(1.0);
            // 清除磁盘缓存（仅该目录的历史快照文件，其他目录保留）
            var removed = _diskCache.ClearByPath(directoryPath, options);
            _logger?.LogInformation("网盘快照缓存已清除：{Dir}（删除 {Count} 个磁盘快照文件）", directoryPath, removed);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "清除指定路径网盘缓存时发生异常：{Dir}", directoryPath);
        }
    }

    /// <summary>加载最近一次成功拉取的磁盘快照（可选按目录过滤），用于打开整理工具时自动恢复上次快照。</summary>
    public PanDirectorySnapshot? LoadLatestSnapshot(string? directoryPath = null)
        => _diskCache.LoadLatest(directoryPath);

    public async Task<PanExecutionReport> ExecuteRecommendationsAsync(
        List<PanRecommendation> recommendations,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sharedCts = GetOrCreateCancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sharedCts.Token);

        return await _executionEngine.ExecuteAsync(
            recommendations,
            progress,
            linkedCts.Token);
    }

    #region 私有辅助方法

    private CancellationTokenSource GetOrCreateCancellationTokenSource()
    {
        if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
        {
            var newCts = new CancellationTokenSource();
            var old = Interlocked.Exchange(ref _cancellationTokenSource, newCts);
            if (old != null && old != newCts && !old.IsCancellationRequested)
            {
                try { old.Cancel(); } catch { /* ignore */ }
                old.Dispose();
            }
        }
        return _cancellationTokenSource;
    }

    private string BuildAnalysisPrompt(PanDirectorySnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("你是一个网盘整理专家。请分析以下文件列表，输出整理建议。");
        sb.AppendLine();
        sb.AppendLine($"## 目录信息");
        sb.AppendLine($"- 路径：{snapshot.DirectoryPath}");
        sb.AppendLine($"- 总文件数：{snapshot.Files.Count}");
        sb.AppendLine($"- 总文件夹数：{snapshot.Folders.Count}");
        sb.AppendLine();

        sb.AppendLine("## 文件列表");
        foreach (var file in snapshot.Files.OrderBy(f => f.Path))
        {
            var sizeStr = file.SizeBytes >= 1024 * 1024
                ? $"{file.SizeBytes / 1024.0 / 1024.0:F1}MB"
                : file.SizeBytes >= 1024
                    ? $"{file.SizeBytes / 1024.0:F1}KB"
                    : $"{file.SizeBytes}B";
            sb.AppendLine($"- {file.Path} ({sizeStr}, {file.ServerModifiedTime:yyyy-MM-dd})");
        }

        sb.AppendLine();
        sb.AppendLine("## 输出格式");
        sb.AppendLine("请以 JSON 格式输出，格式如下：");
        sb.AppendLine(@"{
  ""summary"": ""分析总结"",
  ""recommendations"": [
    {
      ""type"": ""Move|Delete|Rename"",
      ""sourcePath"": ""原路径"",
      ""destinationPath"": ""目标路径（仅 Move 类型需要）"",
      ""newName"": ""新名称（仅 Rename 类型需要）"",
      ""reason"": ""原因""
    }
  ]
}");
        sb.AppendLine();
        sb.AppendLine("请确保返回合法的 JSON，不要包含其他说明文字。");
        return sb.ToString();
    }

    private PanAnalysisResult ParseAiResponse(string aiResponse, PanDirectorySnapshot snapshot)
    {
        var result = new PanAnalysisResult
        {
            RawAiResponse = aiResponse
        };

        try
        {
            // 尝试提取 JSON 部分
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var json = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = JsonConvert.DeserializeAnonymousType(json, new
                {
                    summary = "",
                    recommendations = new[] { new { type = "", sourcePath = "", destinationPath = "", newName = "", reason = "" } }
                });

                if (parsed != null)
                {
                    result.Summary = parsed.summary ?? "";
                    result.ParseSuccess = true;

                    if (parsed.recommendations != null)
                    {
                        foreach (var rec in parsed.recommendations)
                        {
                            Enum.TryParse<PanRecommendationType>(rec.type, true, out var type);
                            result.Recommendations.Add(new PanRecommendation
                            {
                                Type = type,
                                TargetPath = rec.sourcePath ?? "",
                                DestinationPath = rec.destinationPath,
                                NewName = rec.newName,
                                Reason = rec.reason ?? ""
                            });
                        }
                    }
                }
            }

            if (!result.ParseSuccess)
            {
                result.Summary = "AI 响应解析失败，请检查响应格式";
                result.ParseError = "无法从响应中提取有效 JSON";
            }
        }
        catch (Exception ex)
        {
            result.ParseSuccess = false;
            result.ParseError = ex.Message;
            result.Summary = "AI 响应解析异常";
        }

        return result;
    }

    private async IAsyncEnumerable<(BaseFileInfo File, int Depth)> TraverseDirectoryAsync(
        BaiduPanApiClient apiClient,
        string path,
        int maxDepth,
        IProgress<PanAnalysisProgress>? progress,
        int? totalFiles,
        [EnumeratorCancellation] CancellationToken cancellationToken,
        bool foldersOnly = false)
    {
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((path, 0));
        var localFileCount = totalFiles ?? 0;

        while (queue.Count > 0 && (maxDepth == 0 || queue.Peek().Depth <= maxDepth))
        {
            var (currentPath, depth) = queue.Dequeue();
            cancellationToken.ThrowIfCancellationRequested();

            // 报告当前正在遍历的目录
            var dirName = string.IsNullOrEmpty(currentPath) || currentPath == "/"
                ? "根目录"
                : currentPath.TrimEnd('/').Split('/').LastOrDefault() ?? currentPath;
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Fetching,
                Current = 5,
                Total = 100,
                Message = $"正在遍历目录【{dirName}】..."
            });

            _logger?.LogInformation("开始遍历目录: {Path}, 深度={Depth}, 当前已发现文件数={Count}", currentPath, depth, localFileCount);

            // 分页拉取目录列表（百度 list 接口单次最多返回 1000 条）
            var start = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var response = await apiClient.GetFileListAsync(currentPath, "name", 1, start, 1000);
                if (response.ErrorCode != 0)
                {
                    _logger?.LogWarning("API 返回错误码 {ErrorCode}，路径={Path}，start={Start}", response.ErrorCode, currentPath, start);
                    throw new InvalidOperationException($"百度网盘 API 返回错误 (errno={response.ErrorCode})，路径：{currentPath}。请检查 Token 是否有效或路径是否正确。");
                }
                if (response.FileList == null)
                    break;

                var fileCount = response.FileList.Count;
                _logger?.LogDebug("API 响应: 路径={Path}, start={Start}, 返回={Count} 个条目", currentPath, start, fileCount);

                foreach (var file in response.FileList)
                {
                    // 仅拉取文件夹：跳过文件条目（不 yield、不计数），只保留文件夹用于下钻
                    if (foldersOnly && file.IsDir != 1)
                        continue;

                    localFileCount++;
                    yield return (file, depth);

                    if (file.IsDir == 1 && (maxDepth == 0 || depth < maxDepth))
                    {
                        queue.Enqueue((file.Path, depth + 1));
                    }
                }

                // 报告进度（每页报告一次，及时反馈）
                progress?.Report(new PanAnalysisProgress
                {
                    Phase = PanAnalysisPhase.Fetching,
                    Current = Math.Min(10 + (int)((double)localFileCount / Math.Max(1000, 1) * 70), 80),
                    Total = 100,
                    Message = $"已获取 {localFileCount} 个文件，正在遍历【{dirName}】..."
                });

                // 未取满一页说明已到末尾，结束当前目录分页
                if (fileCount < 1000)
                    break;

                start += fileCount;
                await Task.Delay(200, cancellationToken);
            }

            // 限流延迟
            await Task.Delay(200, cancellationToken);
        }
    }

    private PanFileInfo MapToPanFileInfo(BaseFileInfo file, string rootPath, bool skipSizeComputing = false)
    {
        var relativePath = file.Path ?? "";
        if (relativePath.StartsWith(rootPath, StringComparison.Ordinal))
            relativePath = relativePath[rootPath.Length..].TrimStart('/');

        var size = skipSizeComputing ? 0 : file.Size;
        return new PanFileInfo
        {
            FsId = file.FsId,
            Path = file.Path ?? "",
            Name = file.ServerFileName ?? "",
            RelativePath = relativePath,
            SizeBytes = size,
            Extension = Path.GetExtension(file.ServerFileName ?? "")?.ToLowerInvariant() ?? "",
            Category = file.Category,
            ServerModifiedTime = DateTimeOffset.FromUnixTimeSeconds(file.ServerMtime).LocalDateTime,
            IsFolder = file.IsDir == 1,
            Md5 = skipSizeComputing ? null : file.Md5,
            IsJunkFile = IsJunkFile(file.ServerFileName ?? "", size),
            IsPotentialDuplicate = false
        };
    }

    /// <summary>
    /// 快照拉取完成后，自底向上聚合每个文件夹的总大小 / 子孙文件数 / 直接子文件夹数。
    /// 深度越大的目录先处理，把自身汇总到父目录，父目录再汇总自己，复杂度 O(文件数 + 文件夹数)。
    /// </summary>
    private static void ComputeFolderSizes(List<PanFolderInfo> folders, List<PanFileInfo> files)
    {
        if (folders == null || folders.Count == 0) return;

        var sizeByDir = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        var countByDir = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // 每个目录的直接文件大小/数量
        foreach (var file in files)
        {
            if (string.IsNullOrEmpty(file.Path)) continue;
            var parent = GetParentApiPath(file.Path);
            sizeByDir[parent] = sizeByDir.GetValueOrDefault(parent) + file.SizeBytes;
            countByDir[parent] = countByDir.GetValueOrDefault(parent) + 1;
        }

        // 每个目录的直接子文件夹数
        var subCountByParent = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var folder in folders)
        {
            var parent = GetParentApiPath(folder.Path);
            subCountByParent[parent] = subCountByParent.GetValueOrDefault(parent) + 1;
        }

        // 深度从大到小：子目录先把自己汇总进父目录，父目录再读取（已含子孙）并继续向上聚合
        foreach (var folder in folders.OrderByDescending(f => f.Depth))
        {
            folder.SizeBytes = sizeByDir.GetValueOrDefault(folder.Path);
            folder.FileCount = countByDir.GetValueOrDefault(folder.Path);
            folder.SubFolderCount = subCountByParent.GetValueOrDefault(folder.Path);

            var parent = GetParentApiPath(folder.Path);
            sizeByDir[parent] = sizeByDir.GetValueOrDefault(parent) + folder.SizeBytes;
            countByDir[parent] = countByDir.GetValueOrDefault(parent) + folder.FileCount;
        }
    }

    private PanFolderInfo MapToPanFolderInfo(BaseFileInfo folder, string rootPath)
    {
        var relativePath = folder.Path ?? "";
        if (relativePath.StartsWith(rootPath, StringComparison.Ordinal))
            relativePath = relativePath[rootPath.Length..].TrimStart('/');

        return new PanFolderInfo
        {
            Path = folder.Path ?? "",
            RelativePath = relativePath,
            Name = folder.ServerFileName ?? "",
            Depth = string.IsNullOrEmpty(relativePath)
                ? 0
                : relativePath.Split('/', StringSplitOptions.RemoveEmptyEntries).Length
        };
    }

    /// <summary>
    /// 过滤掉空文件夹（没有任何直接子项：既无文件也无子目录）。
    /// 遍历逻辑本身会继续访问下一级目录，此处仅将确认无子项的空目录剔除，
    /// 使其不再进入统计/目录树/AI 建议（用户已清理空目录，无需反复提示）。
    /// </summary>
    private static List<BaseFileInfo> FilterOutEmptyFolders(
        List<PanFileInfo> files,
        List<BaseFileInfo> folders)
    {
        if (folders.Count == 0) return folders;

        // 统计每个目录的直接子项数（文件 + 子目录），子项数为 0 的目录视为空目录
        var childCountByDir = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var parent = GetParentApiPath(file.Path);
            childCountByDir[parent] = childCountByDir.GetValueOrDefault(parent) + 1;
        }
        foreach (var folder in folders)
        {
            var parent = GetParentApiPath(folder.Path ?? "");
            childCountByDir[parent] = childCountByDir.GetValueOrDefault(parent) + 1;
        }

        return folders
            .Where(f => childCountByDir.GetValueOrDefault(f.Path ?? "") > 0)
            .ToList();
    }

    /// <summary>快照缓存 key（区分「仅文件夹 / 完整」，避免两类快照串用）</summary>
    private static string BuildSnapshotCacheKey(string directoryPath, AnalysisOptions options)
        => $"pan_snapshot_{directoryPath}_{options.MaxDepth}_{options.MaxFileCount}_{(options.FoldersOnly ? "folders" : "all")}";

    /// <summary>返回网盘 API 路径的直接父目录（顶层时返回 "/"）</summary>
    private static string GetParentApiPath(string path)
    {
        var trimmed = (path ?? "").TrimEnd('/');
        var idx = trimmed.LastIndexOf('/');
        if (idx <= 0) return "/";
        return trimmed.Substring(0, idx);
    }

    private bool IsJunkFile(string fileName, long size)
    {
        var name = fileName.ToLowerInvariant();
        // 系统文件
        if (name == ".ds_store" || name == "thumbs.db" || name == "desktop.ini")
            return true;
        // 临时文件
        if (name.StartsWith("~$") || name.EndsWith(".tmp") || name.EndsWith(".bak"))
            return true;
        // 0字节文件
        if (size == 0)
            return true;
        // 空文件夹名
        if (string.IsNullOrWhiteSpace(name))
            return true;
        return false;
    }

    private List<PanDuplicateGroup> DetectDuplicates(List<PanFileInfo> files)
    {
        var groups = new Dictionary<string, List<PanFileInfo>>();

        foreach (var file in files)
        {
            if (file.IsFolder || file.SizeBytes == 0)
                continue;

            // 优先使用 Md5，否则使用 名称+大小
            var key = !string.IsNullOrEmpty(file.Md5)
                ? $"md5_{file.Md5}"
                : $"name_{file.Name}_{file.SizeBytes}";

            if (!groups.TryGetValue(key, out var list))
            {
                list = new List<PanFileInfo>();
                groups[key] = list;
            }
            list.Add(file);
        }

        return groups
            .Where(g => g.Value.Count > 1)
            .Select(g =>
            {
                var sorted = g.Value.OrderByDescending(f => f.ServerModifiedTime).ToList();
                var group = new PanDuplicateGroup
                {
                    GroupKey = g.Key,
                    DisplayName = sorted.First().Name,
                    SizeBytes = sorted.First().SizeBytes,
                    FileCount = sorted.Count,
                    Files = sorted,
                    KeepFile = sorted.First(),
                    DeleteFiles = sorted.Skip(1).ToList()
                };
                foreach (var dup in group.DeleteFiles)
                {
                    dup.IsPotentialDuplicate = true;
                }
                return group;
            })
            .OrderByDescending(g => g.FileCount)
            .ToList();
    }

    private PanStatistics ComputeStatistics(List<PanFileInfo> files, List<BaseFileInfo> folders, AnalysisOptions options)
    {
        var stats = new PanStatistics
        {
            TotalFileCount = files.Count,
            TotalFolderCount = folders.Count,
            TotalSizeBytes = files.Sum(f => f.SizeBytes),
            CountByExtension = new Dictionary<string, int>(),
            SizeByExtension = new Dictionary<string, long>(),
            LargeFiles = new List<PanFileInfo>(),
            ZeroByteFiles = new List<PanFileInfo>()
        };

        foreach (var file in files)
        {
            var ext = file.Extension;
            if (string.IsNullOrEmpty(ext)) ext = "(无)";

            stats.CountByExtension.TryGetValue(ext, out var count);
            stats.CountByExtension[ext] = count + 1;

            stats.SizeByExtension.TryGetValue(ext, out var size);
            stats.SizeByExtension[ext] = size + file.SizeBytes;

            if (file.SizeBytes > options.LargeFileThreshold)
                stats.LargeFiles.Add(file);

            if (file.SizeBytes == 0)
                stats.ZeroByteFiles.Add(file);
        }

        return stats;
    }

    #endregion
}