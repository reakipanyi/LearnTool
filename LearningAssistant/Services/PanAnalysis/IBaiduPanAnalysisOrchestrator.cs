using LearningAssistant.Models.PanAnalysis;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 分析选项
/// </summary>
public class AnalysisOptions
{
    /// <summary>最大递归深度（1=仅当前目录，0=全部）</summary>
    public int MaxDepth { get; set; } = 2;

    /// <summary>是否包含重复检测</summary>
    public bool DetectDuplicates { get; set; } = true;

    /// <summary>是否标记无意义文件</summary>
    public bool DetectJunkFiles { get; set; } = true;

    /// <summary>大文件阈值（字节）</summary>
    public long LargeFileThreshold { get; set; } = 100 * 1024 * 1024; // 100MB

    /// <summary>是否使用缓存</summary>
    public bool UseCache { get; set; } = true;

    /// <summary>缓存过期时间（分钟）</summary>
    public int CacheExpirationMinutes { get; set; } = 5;
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
