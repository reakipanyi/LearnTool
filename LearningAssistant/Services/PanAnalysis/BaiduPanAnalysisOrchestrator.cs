using System.Runtime.CompilerServices;
using LearningAssistant.Baidu;
using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 百度网盘 AI 分析编排器（实现）
/// </summary>
public class BaiduPanAnalysisOrchestrator : IBaiduPanAnalysisOrchestrator
{
    private readonly IPanTokenManager _tokenManager;
    private readonly IAIService _aiService;
    private readonly IPanAnalysisPromptBuilder _promptBuilder;
    private readonly IPanAnalysisResultParser _resultParser;
    private readonly IPanExecutionEngine _executionEngine;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BaiduPanAnalysisOrchestrator> _logger;
    private CancellationTokenSource _cancellationTokenSource = new();

    public bool IsAvailable => _tokenManager.IsTokenValid;

    public BaiduPanAnalysisOrchestrator(
        IPanTokenManager tokenManager,
        IAIService aiService,
        IPanAnalysisPromptBuilder promptBuilder,
        IPanAnalysisResultParser resultParser,
        IPanExecutionEngine executionEngine,
        IMemoryCache cache,
        ILogger<BaiduPanAnalysisOrchestrator> logger)
    {
        _tokenManager = tokenManager;
        _aiService = aiService;
        _promptBuilder = promptBuilder;
        _resultParser = resultParser;
        _executionEngine = executionEngine;
        _cache = cache;
        _logger = logger;
    }

    public void Cancel()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
            return;
        _cancellationTokenSource.Cancel();
        _logger.LogInformation("用户取消了操作");
    }

    /// <summary>
    /// 获取当前可用的取消源。取消后自动重建，保证一次取消后仍可再次发起操作
    /// （CancellationTokenSource 一旦取消便不可复用）。
    /// </summary>
    private CancellationTokenSource GetOrCreateCancellationTokenSource()
    {
        if (_cancellationTokenSource.IsCancellationRequested)
        {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }
        return _cancellationTokenSource;
    }

    public void ReloadTokenState()
    {
        _tokenManager.ReloadFromConfig();
        _logger.LogInformation("Token 状态已重新加载，当前可用：{IsAvailable}", IsAvailable);
    }

    public async Task<PanAnalysisResult> AnalyzeDirectoryAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sharedCts = GetOrCreateCancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sharedCts.Token);

        // 1. 获取快照
        var snapshot = await GetSnapshotAsync(
            directoryPath,
            options,
            progress,
            linkedCts.Token);

        // 2. 检查缓存（key 附加模型名，切换 AI 模型后避免命中旧模型的分析结果）
        var modelName = _aiService?.ModelName ?? "default";
        var cacheKey = $"pan_analysis_{directoryPath}_{options.MaxDepth}_{modelName}";
        if (options.UseCache && _cache.TryGetValue(cacheKey, out PanAnalysisResult? cached) && cached != null)
        {
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Completed,
                Message = $"使用缓存结果（{cached.Recommendations.Count} 条建议）"
            });
            return cached;
        }

        // 3. 构建 Prompt
        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Analyzing,
            SubPhase = PanAnalysisSubPhase.BuildingPrompt,
            Message = "构建 AI 分析指令...",
            Total = 1
        });

        var systemPrompt = _promptBuilder.BuildSystemPrompt();
        var userPrompt = _promptBuilder.BuildUserPrompt(snapshot);

        // 4. 调用 AI
        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Analyzing,
            SubPhase = PanAnalysisSubPhase.CallingAI,
            Message = "AI 分析中...",
            Total = 1
        });

        var startTime = DateTime.UtcNow;
        var aiResponse = await _aiService.AskQuestionAsync(
            question: userPrompt,
            context: systemPrompt,
            cancellationToken: linkedCts.Token);
        var duration = DateTime.UtcNow - startTime;

        // 5. 解析结果
        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Analyzing,
            SubPhase = PanAnalysisSubPhase.ParsingResponse,
            Message = "解析 AI 响应...",
            Total = 1
        });

        var result = _resultParser.Parse(aiResponse);
        result.AnalysisDuration = duration;

        // 6. 缓存结果
        if (options.UseCache && result.ParseSuccess)
        {
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(options.CacheExpirationMinutes));
        }

        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Completed,
            Message = $"分析完成，共 {result.Recommendations.Count} 条建议",
            Total = 1,
            Current = 1
        });

        return result;
    }

    public async Task<PanDirectorySnapshot> GetSnapshotAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sharedCts = GetOrCreateCancellationTokenSource();
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            sharedCts.Token);

        // 检查缓存
        var cacheKey = $"pan_snapshot_{directoryPath}_{options.MaxDepth}";
        if (options.UseCache && _cache.TryGetValue(cacheKey, out PanDirectorySnapshot? cached) && cached != null)
        {
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Completed,
                Message = $"使用缓存快照（{cached.Files.Count} 个文件）"
            });
            return cached;
        }

        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Fetching,
            Message = $"获取文件列表：{directoryPath}",
            Total = 0
        });

        // 确保 Token 有效
        var token = await _tokenManager.EnsureValidTokenAsync(linkedCts.Token);

        // 获取文件列表（共享 HttpClient）
        using var apiClient = new BaiduPanApiClient(token);

        // Token 过期（errno=-6）时刷新后重试一次，避免分析中途因 Token 过期失败
        const int TokenExpiredErrorCode = -6;

        var allFiles = new List<PanFileInfo>();
        var allFolders = new List<BaseFileInfo>();
        var totalSize = 0L;

        for (var attempt = 0; ; attempt++)
        {
            // 每次尝试都重新累积（失败重试时丢弃已遍历部分，保证结果完整一致）
            allFiles = new List<PanFileInfo>();
            allFolders = new List<BaseFileInfo>();
            totalSize = 0L;

            try
            {
                await foreach (var (file, depth) in TraverseDirectoryAsync(
                    apiClient, directoryPath, options.MaxDepth, linkedCts.Token))
                {
                    if (file.IsDir == 1)
                    {
                        allFolders.Add(file);
                    }
                    else
                    {
                        var panFile = MapToPanFileInfo(file, directoryPath);
                        allFiles.Add(panFile);
                        totalSize += file.Size;
                    }

                    // 进度更新
                    var total = allFiles.Count + allFolders.Count;
                    if (total % 100 == 0)
                    {
                        progress?.Report(new PanAnalysisProgress
                        {
                            Phase = PanAnalysisPhase.Fetching,
                            SubPhase = PanAnalysisSubPhase.FetchingPage,
                            Message = $"已获取 {total} 个条目...",
                            Current = total
                        });
                    }
                }
                break; // 遍历成功完成
            }
            catch (PanApiException ex) when (ex.ErrorCode == TokenExpiredErrorCode && attempt == 0)
            {
                _logger.LogWarning("访问 Token 已过期（errno=-6），刷新后重试快照获取");
                token = await _tokenManager.RefreshTokenAsync(linkedCts.Token);
                apiClient.UpdateAccessToken(token);
            }
        }

        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.PreComputing,
            Message = $"获取完成：{allFiles.Count} 个文件，{allFolders.Count} 个文件夹",
            Total = 1
        });

        // 本地预计算
        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.PreComputing,
            SubPhase = PanAnalysisSubPhase.DetectingDuplicates,
            Message = "检测重复文件...",
            Total = 1
        });

        var duplicates = options.DetectDuplicates
            ? DetectDuplicates(allFiles)
            : new List<PanDuplicateGroup>();

        var statistics = ComputeStatistics(allFiles, allFolders, options);
        statistics.JunkFileCount = allFiles.Count(f => f.IsJunkFile);
        statistics.JunkFileSizeBytes = allFiles.Where(f => f.IsJunkFile).Sum(f => f.SizeBytes);

        var snapshot = new PanDirectorySnapshot
        {
            DirectoryPath = directoryPath,
            SnapshotTime = DateTime.UtcNow,
            Scope = new AnalysisScope
            {
                MaxDepth = options.MaxDepth,
                TotalFileCount = allFiles.Count,
                TotalFolderCount = allFolders.Count,
                TotalSizeBytes = totalSize
            },
            Files = allFiles,
            Statistics = statistics,
            Duplicates = duplicates,
            IsComplete = true
        };

        // 缓存
        if (options.UseCache)
        {
            _cache.Set(cacheKey, snapshot, TimeSpan.FromMinutes(options.CacheExpirationMinutes));
        }

        progress?.Report(new PanAnalysisProgress
        {
            Phase = PanAnalysisPhase.Completed,
            Message = $"快照完成：{allFiles.Count} 个文件，{duplicates.Count} 组重复"
        });

        return snapshot;
    }

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

    private async IAsyncEnumerable<(BaseFileInfo File, int Depth)> TraverseDirectoryAsync(
        BaiduPanApiClient apiClient,
        string path,
        int maxDepth,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var queue = new Queue<(string Path, int Depth)>();
        queue.Enqueue((path, 0));

        while (queue.Count > 0 && (maxDepth == 0 || queue.Peek().Depth <= maxDepth))
        {
            var (currentPath, depth) = queue.Dequeue();
            cancellationToken.ThrowIfCancellationRequested();

            // 分页拉取目录列表（百度 list 接口单次最多返回 1000 条）
            var start = 0;
            while (true)
            {
                var response = await apiClient.GetFileListAsync(currentPath, "name", 1, start, 1000);
                if (response.ErrorCode != 0 || response.FileList == null)
                    break;

                foreach (var file in response.FileList)
                {
                    yield return (file, depth);

                    if (file.IsDir == 1 && (maxDepth == 0 || depth < maxDepth))
                    {
                        queue.Enqueue((file.Path, depth + 1));
                    }
                }

                // 未取满一页说明已到末尾，结束当前目录分页
                if (response.FileList.Count < 1000)
                    break;

                start += response.FileList.Count;
                await Task.Delay(200, cancellationToken);
            }

            // 限流延迟
            await Task.Delay(200, cancellationToken);
        }
    }

    private PanFileInfo MapToPanFileInfo(BaseFileInfo file, string rootPath)
    {
        var relativePath = file.Path ?? "";
        if (relativePath.StartsWith(rootPath, StringComparison.Ordinal))
            relativePath = relativePath[rootPath.Length..].TrimStart('/');

        return new PanFileInfo
        {
            FsId = file.FsId,
            Path = file.Path ?? "",
            Name = file.ServerFileName ?? "",
            RelativePath = relativePath,
            SizeBytes = file.Size,
            Extension = Path.GetExtension(file.ServerFileName ?? "")?.ToLowerInvariant() ?? "",
            Category = file.Category,
            ServerModifiedTime = DateTimeOffset.FromUnixTimeSeconds(file.ServerMtime).LocalDateTime,
            IsFolder = file.IsDir == 1,
            Md5 = file.Md5,
            IsJunkFile = IsJunkFile(file.ServerFileName ?? "", file.Size),
            IsPotentialDuplicate = false
        };
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
