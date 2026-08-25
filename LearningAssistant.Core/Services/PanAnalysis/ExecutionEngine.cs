using LearningAssistant.Models.PanAnalysis;
using LearningAssistant.Services.Baidu;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 操作执行引擎
/// </summary>
public interface IPanExecutionEngine
{
    Task<PanExecutionReport> ExecuteAsync(
        List<PanRecommendation> recommendations,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

public class PanExecutionEngine : IPanExecutionEngine
{
    private readonly IPanTokenManager _tokenManager;
    private readonly ILogger<PanExecutionEngine> _logger;
    private const int BatchSize = 100;
    private const int MaxConcurrentBatches = 1; // 百度 API 不支持并发，保持串行

    public PanExecutionEngine(IPanTokenManager tokenManager, ILogger<PanExecutionEngine> logger)
    {
        _tokenManager = tokenManager;
        _logger = logger;
    }

    public async Task<PanExecutionReport> ExecuteAsync(
        List<PanRecommendation> recommendations,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var report = new PanExecutionReport
        {
            StartTime = DateTime.UtcNow,
            TotalRequested = recommendations.Count
        };

        if (!recommendations.Any())
        {
            report.EndTime = DateTime.UtcNow;
            return report;
        }

        // 按类型分组
        var deletes = recommendations.Where(r => r.Type == PanRecommendationType.Delete).ToList();
        var moves = recommendations.Where(r => r.Type == PanRecommendationType.Move).ToList();
        var renames = recommendations.Where(r => r.Type == PanRecommendationType.Rename).ToList();

        try
        {
            // 确保 Token 有效
            var token = await _tokenManager.EnsureValidTokenAsync(cancellationToken);

            // 创建 API 客户端
            using var apiClient = new BaiduPanApiClient(token);

            // 1. 执行删除（优先级最高）
            if (deletes.Any())
            {
                await ExecuteDeleteBatchAsync(apiClient, deletes, report, progress, cancellationToken);
            }

            // 2. 执行移动
            if (moves.Any())
            {
                await ExecuteMoveBatchAsync(apiClient, moves, report, progress, cancellationToken);
            }

            // 3. 执行重命名（逐个执行，每个重命名都是独立的）
            if (renames.Any())
            {
                await ExecuteRenameBatchAsync(apiClient, renames, report, progress, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行过程中发生异常");
            // 标记所有未处理的为失败
            foreach (var rec in recommendations.Where(r => !report.Results.Any(rs => rs.Recommendation.Id == r.Id)))
            {
                report.Results.Add(new PanExecutionResult
                {
                    Recommendation = rec,
                    Success = false,
                    ErrorMessage = $"执行异常：{ex.Message}"
                });
                report.Failed++;
            }
        }

        report.EndTime = DateTime.UtcNow;
        report.Succeeded = report.Results.Count(r => r.Success);
        report.Failed = report.Results.Count(r => !r.Success);
        report.Skipped = report.TotalRequested - report.Succeeded - report.Failed;

        return report;
    }

    private async Task ExecuteDeleteBatchAsync(
        BaiduPanApiClient apiClient,
        List<PanRecommendation> deletes,
        PanExecutionReport report,
        IProgress<PanAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var total = deletes.Count;
        var processed = 0;

        foreach (var batch in deletes.Batch(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchList = batch.ToList();
            var fileItems = batchList.Select(r => new FileManagerFileItem
            {
                Path = r.TargetPath
            }).ToList();

            try
            {
                var response = await apiClient.ManageFileAsync(
                    opera: FileOperation.Delete,
                    fileList: fileItems,
                    async: 0,
                    onDup: OnDupStrategy.NewCopy);

                foreach (var rec in batchList)
                {
                    report.Results.Add(new PanExecutionResult
                    {
                        Recommendation = rec,
                        Success = response.ErrorCode == 0,
                        ErrorMessage = response.ErrorCode != 0 ? $"错误码：{response.ErrorCode}" : null,
                        ErrorCode = response.ErrorCode.ToString(),
                        ExecutedAt = DateTime.UtcNow
                    });
                }

                if (response.ErrorCode != 0)
                {
                    _logger.LogWarning("删除批次失败，错误码：{ErrorCode}", response.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除批次异常");
                foreach (var rec in batchList)
                {
                    report.Results.Add(new PanExecutionResult
                    {
                        Recommendation = rec,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });
                }
            }

            processed += batchList.Count;
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Executing,
                SubPhase = PanAnalysisSubPhase.DeletingBatch,
                Message = $"删除中：{processed}/{total}",
                Current = processed,
                Total = total
            });

            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task ExecuteMoveBatchAsync(
        BaiduPanApiClient apiClient,
        List<PanRecommendation> moves,
        PanExecutionReport report,
        IProgress<PanAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var total = moves.Count;
        var processed = 0;

        foreach (var batch in moves.Batch(BatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batchList = batch.ToList();
            var fileItems = batchList.Select(r => new FileManagerFileItem
            {
                Path = r.TargetPath,
                Dest = r.DestinationPath
            }).ToList();

            try
            {
                var response = await apiClient.ManageFileAsync(
                    opera: FileOperation.Move,
                    fileList: fileItems,
                    async: 0,
                    onDup: OnDupStrategy.NewCopy);

                foreach (var rec in batchList)
                {
                    report.Results.Add(new PanExecutionResult
                    {
                        Recommendation = rec,
                        Success = response.ErrorCode == 0,
                        ErrorMessage = response.ErrorCode != 0 ? $"错误码：{response.ErrorCode}" : null,
                        ErrorCode = response.ErrorCode.ToString(),
                        ExecutedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "移动批次异常");
                foreach (var rec in batchList)
                {
                    report.Results.Add(new PanExecutionResult
                    {
                        Recommendation = rec,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ExecutedAt = DateTime.UtcNow
                    });
                }
            }

            processed += batchList.Count;
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Executing,
                SubPhase = PanAnalysisSubPhase.MovingBatch,
                Message = $"移动中：{processed}/{total}",
                Current = processed,
                Total = total
            });

            await Task.Delay(500, cancellationToken);
        }
    }

    private async Task ExecuteRenameBatchAsync(
        BaiduPanApiClient apiClient,
        List<PanRecommendation> renames,
        PanExecutionReport report,
        IProgress<PanAnalysisProgress>? progress,
        CancellationToken cancellationToken)
    {
        var total = renames.Count;
        var processed = 0;

        // 重命名必须逐个执行（每个新名称不同）
        foreach (var rec in renames)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileItem = new FileManagerFileItem
            {
                Path = rec.TargetPath,
                NewName = rec.NewName
            };

            try
            {
                var response = await apiClient.ManageFileAsync(
                    opera: FileOperation.Rename,
                    fileList: new List<FileManagerFileItem> { fileItem },
                    async: 0,
                    onDup: OnDupStrategy.Fail);

                report.Results.Add(new PanExecutionResult
                {
                    Recommendation = rec,
                    Success = response.ErrorCode == 0,
                    ErrorMessage = response.ErrorCode != 0 ? $"错误码：{response.ErrorCode}" : null,
                    ErrorCode = response.ErrorCode.ToString(),
                    ExecutedAt = DateTime.UtcNow
                });

                if (response.ErrorCode != 0)
                {
                    _logger.LogWarning("重命名失败：{Path} → {NewName}，错误码：{ErrorCode}",
                        rec.TargetPath, rec.NewName, response.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "重命名异常：{Path} → {NewName}", rec.TargetPath, rec.NewName);
                report.Results.Add(new PanExecutionResult
                {
                    Recommendation = rec,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });
            }

            processed++;
            progress?.Report(new PanAnalysisProgress
            {
                Phase = PanAnalysisPhase.Executing,
                SubPhase = PanAnalysisSubPhase.RenamingBatch,
                Message = $"重命名中：{processed}/{total}",
                Current = processed,
                Total = total
            });

            await Task.Delay(300, cancellationToken);
        }
    }
}

/// <summary>
/// 批处理扩展
/// </summary>
public static class BatchExtensions
{
    public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        using var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            yield return YieldBatchElements(enumerator, batchSize);
        }
    }

    private static IEnumerable<T> YieldBatchElements<T>(IEnumerator<T> enumerator, int batchSize)
    {
        yield return enumerator.Current;
        int count = 1;
        while (count < batchSize && enumerator.MoveNext())
        {
            yield return enumerator.Current;
            count++;
        }
    }
}
