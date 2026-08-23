using LearningAssistant.Models.PanAnalysis;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 分析选项
/// </summary>
public class AnalysisOptions
{
    /// <summary>最大递归深度（1=仅当前目录，0=全部）</summary>
    public int MaxDepth { get; set; } = 2;

    /// <summary>
    /// 文件数上限（0 = 不限制）。大目录下达到上限即停止遍历，
    /// 快照会标记 IsComplete=false 并携带截断信息，避免大文件夹拉取过慢、上下文超限。
    /// </summary>
    public int MaxFileCount { get; set; } = 3000;

    /// <summary>是否包含重复检测</summary>
    public bool DetectDuplicates { get; set; } = true;

    /// <summary>是否标记无意义文件</summary>
    public bool DetectJunkFiles { get; set; } = true;

    /// <summary>大文件阈值（字节）</summary>
    public long LargeFileThreshold { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>是否使用内存缓存</summary>
    public bool UseCache { get; set; } = true;

    /// <summary>是否使用磁盘持久化缓存（跨重启复用，默认 24h）。开启后大目录快照不用重复拉取。</summary>
    public bool UseDiskCache { get; set; } = true;

    /// <summary>内存缓存过期时间（分钟）</summary>
    public int CacheExpirationMinutes { get; set; } = 5;

    /// <summary>磁盘缓存过期时间（小时），默认 24 小时。0 = 永不过期（仅手动清理）。</summary>
    public int DiskCacheExpirationHours { get; set; } = 24;

    /// <summary>跳过文件大小相关计算（文件夹大小聚合 / 统计），大目录下可显著加速。默认：不跳过。</summary>
    public bool SkipFileSizeComputing { get; set; } = false;
}

/// <summary>
/// 百度网盘 AI 分析编排器（门面接口）
/// </summary>
public interface IBaiduPanAnalysisOrchestrator
{
    /// <summary>服务是否可用（已授权）</summary>
    bool IsAvailable { get; }

    /// <summary>执行完整的分析流程（快照 → AI分析 → 返回结果）</summary>
    Task<PanAnalysisResult> AnalyzeDirectoryAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>仅获取目录快照（不调用 AI）</summary>
    Task<PanDirectorySnapshot> GetSnapshotAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>执行选中的推荐操作</summary>
    Task<PanExecutionReport> ExecuteRecommendationsAsync(
        List<PanRecommendation> recommendations,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>取消当前正在进行的操作</summary>
    void Cancel();

    /// <summary>重新加载 Token 状态（授权成功后调用，使 IsAvailable 反映最新凭据）</summary>
    void ReloadTokenState();
}
