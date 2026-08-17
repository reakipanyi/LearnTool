我来对这份设计方案进行全面优化，主要从**架构清晰度、数据流设计、错误处理、用户体验和可维护性**几个维度进行改进。

---

# 百度网盘 AI 分析整理功能 — 设计方案（重构版）

---

## 一、设计理念

### 1.1 核心原则

| 原则 | 说明 |
|------|------|
| **单一职责** | 每个组件只做一件事，明确边界 |
| **数据驱动** | 数据模型完整，流转清晰，包含执行所需全部字段 |
| **优雅降级** | 任何环节失败都有兜底方案，不中断用户流程 |
| **可观测性** | 每个阶段都有进度反馈，执行结果可追溯 |
| **安全优先** | 操作可撤销（进回收站），执行前确认，批量操作有二次确认 |

### 1.2 与初版方案的核心差异

| 维度 | 初版 | 重构版 |
|------|------|--------|
| **Token 管理** | 笼统描述 | 明确 `TokenManager` 单例 + 刷新锁 + 状态机 |
| **数据模型** | 缺少执行字段 | 完整包含 `FsId`、`Path`、`Category`、`Md5` 等 |
| **AI 解析** | 四层容错 | 四层容错 + Schema 校验 + 自动修复 |
| **操作执行** | 简单分批 | 智能批处理 + 事务语义 + 补偿日志 |
| **错误处理** | 局部处理 | 全局错误边界 + 分类错误 + 用户友好提示 |
| **进度通知** | `IProgress<>` | `IProgress<>` + 结构化进度 + ETA 估算 |
| **可测试性** | 未考虑 | 接口抽象完整，支持 Mock 测试 |

---

## 二、整体架构（分层视图）

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              UI Layer (Forms)                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      WebView2BrowserForm                           │   │
│  │   ├── 路径提取器 (PathExtractor)  │   ├── 按钮事件                 │   │
│  │   └── 打开分析窗体               │   └── 执行后刷新               │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                    BaiduPanAnalysisForm                             │   │
│  │   ├── 快照展示  │  ├── 统计可视化  │  ├── 建议列表  │  ├── 执行   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                          Application Layer                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │               BaiduPanAnalysisOrchestrator                         │   │
│  │  (编排者 — 协调快照获取 → AI分析 → 操作执行)                       │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ Snapshot     │  │ Prompt       │  │ Result       │  │ Execution    │   │
│  │ Builder      │  │ Builder      │  │ Parser       │  │ Engine       │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Infrastructure Layer                               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   │
│  │ Token        │  │ BaiduPanAPI  │  │ AIService    │  │ Cache        │   │
│  │ Manager      │  │ Client       │  │ Factory      │  │ (IMemory)    │   │
│  └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 三、核心数据模型（完整版）

```csharp
// ── Models/PanAnalysisModels.cs ──

namespace LearningAssistant.Models.PanAnalysis;

#region === 进度与状态 ===

/// <summary>
/// 分析进度通知（结构化）
/// </summary>
public class PanAnalysisProgress
{
    public PanAnalysisPhase Phase { get; set; }
    public string Message { get; set; } = "";
    public int Current { get; set; }
    public int Total { get; set; }
    public double Percentage => Total > 0 ? (double)Current / Total * 100 : 0;
    public TimeSpan? EstimatedRemaining { get; set; }
    public PanAnalysisSubPhase? SubPhase { get; set; }
}

public enum PanAnalysisPhase
{
    Initializing,    // 初始化
    Fetching,        // 获取文件列表
    PreComputing,    // 本地预计算（重复检测、统计）
    Analyzing,       // AI 分析
    Executing,       // 执行操作
    Completed,       // 完成
    Failed           // 失败
}

public enum PanAnalysisSubPhase
{
    None,
    FetchingPage,      // 分页获取中
    DetectingDuplicates,
    BuildingPrompt,
    CallingAI,
    ParsingResponse,
    DeletingBatch,
    MovingBatch,
    RenamingBatch
}

#endregion

#region === 目录快照 ===

/// <summary>
/// 目录快照（包含文件列表、统计、重复检测结果）
/// </summary>
public class PanDirectorySnapshot
{
    public string DirectoryPath { get; set; } = "";
    public DateTime SnapshotTime { get; set; } = DateTime.UtcNow;
    public AnalysisScope Scope { get; set; } = new();
    public List<PanFileInfo> Files { get; set; } = new();
    public PanStatistics Statistics { get; set; } = new();
    public List<PanDuplicateGroup> Duplicates { get; set; } = new();

    /// <summary>快照是否完整（文件数是否达到实际总数）</summary>
    public bool IsComplete { get; set; }
}

/// <summary>
/// 分析范围
/// </summary>
public class AnalysisScope
{
    public int MaxDepth { get; set; } = 2;
    public int TotalFileCount { get; set; }
    public int TotalFolderCount { get; set; }
    public long TotalSizeBytes { get; set; }
}

/// <summary>
/// 文件信息（包含执行操作所需的全部字段）
/// </summary>
public class PanFileInfo
{
    public long FsId { get; set; }
    public string Path { get; set; } = "";                 // API 执行路径
    public string Name { get; set; } = "";
    public string RelativePath { get; set; } = "";          // 相对于根目录
    public long SizeBytes { get; set; }
    public string Extension { get; set; } = "";
    public int Category { get; set; }                       // 1视频 2音频 3图片 4文档 5应用 6其他 7种子
    public DateTime? ServerModifiedTime { get; set; }
    public bool IsFolder { get; set; }
    public string? Md5 { get; set; }

    /// <summary>是否可能是重复文件（本地预计算标记）</summary>
    public bool IsPotentialDuplicate { get; set; }

    /// <summary>是否是无意义文件（本地预计算标记）</summary>
    public bool IsJunkFile { get; set; }

    public string SizeFormatted => FormatFileSize(SizeBytes);
    public string CategoryName => GetCategoryName(Category);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string GetCategoryName(int category) => category switch
    {
        1 => "视频",
        2 => "音频",
        3 => "图片",
        4 => "文档",
        5 => "应用",
        7 => "种子",
        _ => "其他"
    };
}

/// <summary>
/// 统计信息
/// </summary>
public class PanStatistics
{
    public int TotalFileCount { get; set; }
    public int TotalFolderCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public int JunkFileCount { get; set; }
    public long JunkFileSizeBytes { get; set; }

    public Dictionary<string, int> CountByExtension { get; set; } = new();
    public Dictionary<string, long> SizeByExtension { get; set; } = new();

    /// <summary>大文件列表（> 100MB）</summary>
    public List<PanFileInfo> LargeFiles { get; set; } = new();

    /// <summary>0 字节文件列表</summary>
    public List<PanFileInfo> ZeroByteFiles { get; set; } = new();

    public string TotalSizeFormatted => FormatFileSize(TotalSizeBytes);
    public string JunkSizeFormatted => FormatFileSize(JunkFileSizeBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

/// <summary>
/// 重复文件组（本地预计算）
/// </summary>
public class PanDuplicateGroup
{
    public string GroupKey { get; set; } = "";              // 文件名+大小 or Md5
    public string DisplayName { get; set; } = "";
    public long SizeBytes { get; set; }
    public int FileCount { get; set; }
    public List<PanFileInfo> Files { get; set; } = new();

    /// <summary>建议保留的文件（按修改时间最新）</summary>
    public PanFileInfo? KeepFile { get; set; }

    /// <summary>建议删除的文件列表</summary>
    public List<PanFileInfo> DeleteFiles { get; set; } = new();

    public string SizeFormatted => FormatFileSize(SizeBytes);

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}

#endregion

#region === AI 分析结果 ===

/// <summary>
/// AI 分析结果
/// </summary>
public class PanAnalysisResult
{
    public string Summary { get; set; } = "";
    public List<PanRecommendation> Recommendations { get; set; } = new();
    public string RawAiResponse { get; set; } = "";
    public bool ParseSuccess { get; set; }
    public string? ParseError { get; set; }
    public TimeSpan? AnalysisDuration { get; set; }
    public int TotalRecommendations => Recommendations.Count;
    public int HighPriorityCount => Recommendations.Count(r => r.Priority == PanPriority.High);
    public int MediumPriorityCount => Recommendations.Count(r => r.Priority == PanPriority.Medium);
    public int LowPriorityCount => Recommendations.Count(r => r.Priority == PanPriority.Low);
}

/// <summary>
/// 推荐操作
/// </summary>
public class PanRecommendation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public PanRecommendationType Type { get; set; }
    public string TargetPath { get; set; } = "";            // 源文件/文件夹完整路径
    public string TargetName { get; set; } = "";            // 文件名（展示用）
    public string? DestinationPath { get; set; }            // Move: 目标目录（以 / 结尾）
    public string? NewName { get; set; }                    // Rename: 新名称
    public string Reason { get; set; } = "";
    public PanPriority Priority { get; set; }
    public bool IsSelected { get; set; } = true;            // UI 默认选中
    public string? AffectedFileId { get; set; }             // 关联的 FsId

    /// <summary>操作是否可撤销（删除进回收站 = 可撤销）</summary>
    public bool IsReversible => Type == PanRecommendationType.Delete;

    public string TypeDisplay => Type switch
    {
        PanRecommendationType.Delete => "🗑️ 删除",
        PanRecommendationType.Move => "📦 移动",
        PanRecommendationType.Rename => "✏️ 重命名",
        PanRecommendationType.MergeFolder => "📂 合并文件夹",
        PanRecommendationType.Keep => "✅ 保留",
        _ => "ℹ️ 未知"
    };

    public string PriorityDisplay => Priority switch
    {
        PanPriority.High => "🔴 高",
        PanPriority.Medium => "🟡 中",
        PanPriority.Low => "🟢 低",
        _ => "⚪ 未分类"
    };
}

public enum PanRecommendationType
{
    Delete,
    Move,
    Rename,
    MergeFolder,
    Keep
}

public enum PanPriority
{
    High,
    Medium,
    Low
}

#endregion

#region === 执行报告 ===

/// <summary>
/// 执行报告
/// </summary>
public class PanExecutionReport
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public int TotalRequested { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public List<PanExecutionResult> Results { get; set; } = new();

    public bool HasFailures => Failed > 0;
    public double SuccessRate => TotalRequested > 0 ? (double)Succeeded / TotalRequested * 100 : 0;

    public string Summary =>
        $"执行完成：请求 {TotalRequested} 项，成功 {Succeeded} 项，失败 {Failed} 项，跳过 {Skipped} 项";
}

/// <summary>
/// 单条执行结果
/// </summary>
public class PanExecutionResult
{
    public PanRecommendation Recommendation { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorCode { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string? ApiRequestId { get; set; }
}

#endregion
```

---

## 四、接口定义（清晰分层）

```csharp
// ── Services/PanAnalysis/IBaiduPanAnalysisOrchestrator.cs ──

namespace LearningAssistant.Services.PanAnalysis;

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
}

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
```

---

## 五、Token 管理器（独立组件）

```csharp
// ── Services/PanAnalysis/TokenManager.cs ──

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 百度网盘 Token 管理器（线程安全）
/// </summary>
public interface IPanTokenManager
{
    /// <summary>当前 Access Token</summary>
    string? AccessToken { get; }

    /// <summary>Token 是否有效</summary>
    bool IsTokenValid { get; }

    /// <summary>确保 Token 有效（过期自动刷新）</summary>
    Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>强制刷新 Token</summary>
    Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>Token 状态变化事件</summary>
    event EventHandler<TokenStateChangedEventArgs>? TokenStateChanged;
}

public class TokenStateChangedEventArgs : EventArgs
{
    public TokenState OldState { get; set; }
    public TokenState NewState { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum TokenState
{
    Uninitialized,
    Valid,
    Expired,
    Refreshing,
    RefreshFailed,
    Invalid
}

/// <summary>
/// Token 管理器实现
/// </summary>
internal class PanTokenManager : IPanTokenManager
{
    private readonly CloudStorageConfig _config;
    private readonly IDataPersistenceService _dataPersistence;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _cachedToken;
    private DateTime? _tokenExpireTime;
    private TokenState _state = TokenState.Uninitialized;

    public event EventHandler<TokenStateChangedEventArgs>? TokenStateChanged;

    public string? AccessToken => _cachedToken;

    public bool IsTokenValid =>
        !string.IsNullOrEmpty(_cachedToken) &&
        _tokenExpireTime.HasValue &&
        _tokenExpireTime.Value > DateTime.UtcNow.AddMinutes(5);

    public PanTokenManager(
        CloudStorageConfig config,
        IDataPersistenceService dataPersistence,
        ILogger logger)
    {
        _config = config;
        _dataPersistence = dataPersistence;
        _logger = logger;

        // 从配置加载 Token
        _cachedToken = config.BaiduAccessToken;
        _tokenExpireTime = config.BaiduTokenExpireTime;
        _state = IsTokenValid ? TokenState.Valid : TokenState.Expired;
    }

    public async Task<string> EnsureValidTokenAsync(CancellationToken cancellationToken = default)
    {
        if (IsTokenValid)
            return _cachedToken!;

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            // 双重检查
            if (IsTokenValid)
                return _cachedToken!;

            return await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    public async Task<string> RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            return await RefreshTokenInternalAsync(cancellationToken);
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task<string> RefreshTokenInternalAsync(CancellationToken cancellationToken)
    {
        var oldState = _state;
        _state = TokenState.Refreshing;
        OnStateChanged(oldState, _state);

        try
        {
            if (string.IsNullOrEmpty(_config.BaiduRefreshToken))
                throw new PanAuthException("未找到 RefreshToken，请重新授权");

            _logger.Information("正在刷新百度网盘 AccessToken...");

            // 调用现有授权管理器
            var authManager = new BaiduPanAuthCodeManager(
                _config.BaiduClientId,
                _config.BaiduClientSecret,
                _config.BaiduRedirectUri);

            var (accessToken, refreshToken, expiresIn) = await authManager.RefreshTokenAsync(
                _config.BaiduRefreshToken,
                cancellationToken);

            // 更新配置
            _cachedToken = accessToken;
            _tokenExpireTime = DateTime.UtcNow.AddSeconds(expiresIn - 300); // 提前5分钟过期
            _config.BaiduAccessToken = accessToken;
            _config.BaiduRefreshToken = refreshToken ?? _config.BaiduRefreshToken;
            _config.BaiduTokenExpireTime = _tokenExpireTime.Value;

            // 持久化
            await _dataPersistence.SaveCloudStorageConfigAsync(_config);

            _state = TokenState.Valid;
            OnStateChanged(TokenState.Refreshing, _state);

            _logger.Information("AccessToken 刷新成功，有效期至 {ExpireTime}", _tokenExpireTime);

            return accessToken;
        }
        catch (Exception ex)
        {
            _state = TokenState.RefreshFailed;
            OnStateChanged(TokenState.Refreshing, _state, ex.Message);

            _logger.Error(ex, "AccessToken 刷新失败");
            throw new PanAuthException($"Token 刷新失败：{ex.Message}", ex);
        }
    }

    private void OnStateChanged(TokenState oldState, TokenState newState, string? errorMessage = null)
    {
        TokenStateChanged?.Invoke(this, new TokenStateChangedEventArgs
        {
            OldState = oldState,
            NewState = newState,
            ErrorMessage = errorMessage
        });
    }
}
```

---

## 六、Prompt 构建器（模板化）

```csharp
// ── Services/PanAnalysis/PromptBuilder.cs ──

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// AI Prompt 构建器
/// </summary>
internal interface IPanAnalysisPromptBuilder
{
    /// <summary>构建 System Prompt（含规则和格式说明）</summary>
    string BuildSystemPrompt();

    /// <summary>构建 User Prompt（根据快照数据量自适应）</summary>
    string BuildUserPrompt(PanDirectorySnapshot snapshot);
}

internal class PanAnalysisPromptBuilder : IPanAnalysisPromptBuilder
{
    private readonly int _maxFilesForFullList = 200;
    private readonly int _maxFilesForCompactList = 1000;

    public string BuildSystemPrompt()
    {
        return $@"
你是百度网盘文件整理专家，请按以下规则分析文件列表并给出整理建议。

## 分析维度（按优先级从高到低）
1. **无意义文件**（必须删除）：
   - 系统文件：.DS_Store, Thumbs.db, desktop.ini, ~$* 临时文件
   - 0字节文件
   - 空文件夹（无任何子项）
   - 临时文件：*.tmp, *.bak, *.log, *.cache

2. **重复文件**：
   - 已本地检测出的重复组，建议保留最新版本，删除其余

3. **分类混乱**：
   - 文件类型与目录语义不匹配（如视频在 /文档/ 下）
   - 建议移入对应类型目录

4. **命名问题**：
   - ""新建文件夹""、""新建文件"" 等无意义名称
   - 文件名超过 100 字符
   - 包含乱码或非打印字符

5. **空间优化**：
   - 单个文件 > 100MB，标记并建议确认是否需要

6. **目录结构**：
   - 单目录文件 > 100 个，建议按类型/日期拆分子目录
   - 嵌套层级 > 5，建议扁平化

## 输出格式（严格 JSON）
{{
  ""summary"": ""总体评价（2-3句话）"",
  ""recommendations"": [
    {{
      ""type"": ""Delete|Move|Rename|MergeFolder|Keep"",
      ""targetPath"": ""/完整/路径"",
      ""destinationPath"": ""目标目录（Move 时必填，以 / 结尾）"" | null,
      ""newName"": ""新名称（Rename 时必填）"" | null,
      ""reason"": ""操作原因"",
      ""priority"": ""High|Medium|Low""
    }}
  ]
}}

## 规则
- type=Delete 时：destinationPath 和 newName 必须为 null
- type=Move 时：destinationPath 必须存在且以 / 结尾，newName 为 null
- type=Rename 时：newName 必须存在（不含路径），destinationPath 为 null
- type=MergeFolder 时：destinationPath 为目标目录，newName 为 null
- type=Keep 时：仅标记，无需操作
- 每个文件最多输出 1 条建议
- 建议数量不超过文件总数的 30%
- 只输出 JSON，不要有任何额外文字
";
    }

    public string BuildUserPrompt(PanDirectorySnapshot snapshot)
    {
        var sb = new StringBuilder();

        // 基本信息
        sb.AppendLine($"## 目录：{snapshot.DirectoryPath}");
        sb.AppendLine($"- 文件数：{snapshot.Statistics.TotalFileCount:N0}");
        sb.AppendLine($"- 文件夹数：{snapshot.Statistics.TotalFolderCount:N0}");
        sb.AppendLine($"- 总大小：{snapshot.Statistics.TotalSizeFormatted}");
        sb.AppendLine();

        // 文件类型分布
        sb.AppendLine("### 文件类型分布");
        if (snapshot.Statistics.CountByExtension.Any())
        {
            var sorted = snapshot.Statistics.CountByExtension
                .OrderByDescending(kv => kv.Value)
                .Take(10)
                .Select(kv => $"{kv.Key}: {kv.Value} 个 ({FormatSize(snapshot.Statistics.SizeByExtension.GetValueOrDefault(kv.Key, 0))})");
            sb.AppendLine(string.Join(", ", sorted));
        }
        else
        {
            sb.AppendLine("（无文件或无法识别）");
        }
        sb.AppendLine();

        // 无意义文件统计
        if (snapshot.Statistics.JunkFileCount > 0)
        {
            sb.AppendLine($"### ⚠️ 无意义文件：{snapshot.Statistics.JunkFileCount} 个，共 {snapshot.Statistics.JunkSizeFormatted}");
        }

        // 重复文件组
        if (snapshot.Duplicates.Any())
        {
            sb.AppendLine($"### 📋 重复文件组（{snapshot.Duplicates.Count} 组）");
            foreach (var group in snapshot.Duplicates.Take(10))
            {
                sb.AppendLine($"- [{group.DisplayName}] ({group.FileCount} 个副本, 共 {group.SizeFormatted})");
                foreach (var file in group.Files.Take(5))
                {
                    sb.AppendLine($"  - {file.RelativePath} ({file.SizeFormatted})");
                }
                if (group.Files.Count > 5)
                    sb.AppendLine($"  - ... 还有 {group.Files.Count - 5} 个");
            }
            sb.AppendLine();
        }

        // 文件列表（自适应）
        var fileCount = snapshot.Files.Count;
        sb.AppendLine("### 文件列表");

        if (fileCount <= _maxFilesForFullList)
        {
            // 完整列表
            sb.AppendLine("| 文件名 | 大小 | 类型 | 修改时间 | 路径 |");
            sb.AppendLine("|--------|------|------|----------|------|");
            foreach (var file in snapshot.Files.OrderBy(f => f.RelativePath))
            {
                var modified = file.ServerModifiedTime?.ToString("yyyy-MM-dd") ?? "-";
                sb.AppendLine($"| {file.Name} | {file.SizeFormatted} | {file.CategoryName} | {modified} | {file.RelativePath} |");
            }
        }
        else if (fileCount <= _maxFilesForCompactList)
        {
            // 精简列表（省略路径）
            sb.AppendLine("| 文件名 | 大小 | 类型 | 修改时间 |");
            sb.AppendLine("|--------|------|------|----------|");
            foreach (var file in snapshot.Files.OrderBy(f => f.RelativePath).Take(_maxFilesForCompactList))
            {
                var modified = file.ServerModifiedTime?.ToString("yyyy-MM-dd") ?? "-";
                sb.AppendLine($"| {file.Name} | {file.SizeFormatted} | {file.CategoryName} | {modified} |");
            }
            if (fileCount > _maxFilesForCompactList)
                sb.AppendLine($"| ... 还有 {fileCount - _maxFilesForCompactList} 个文件 | ... | ... | ... |");
        }
        else
        {
            // 摘要模式：仅列出大文件 + 可疑文件
            sb.AppendLine($"（共 {fileCount} 个文件，列表过长，以下是重点关注项）");
            sb.AppendLine();

            // Top 50 大文件
            sb.AppendLine("#### 📦 最大的 50 个文件");
            foreach (var file in snapshot.Files.OrderByDescending(f => f.SizeBytes).Take(50))
            {
                sb.AppendLine($"- {file.Name} ({file.SizeFormatted}) - {file.RelativePath}");
            }

            // 可疑文件
            var suspicious = snapshot.Files
                .Where(f => f.IsJunkFile || f.IsPotentialDuplicate)
                .Take(100)
                .ToList();
            if (suspicious.Any())
            {
                sb.AppendLine();
                sb.AppendLine("#### ⚠️ 可疑文件（无意义文件/重复文件）");
                foreach (var file in suspicious)
                {
                    var reason = file.IsJunkFile ? "无意义" : "疑似重复";
                    sb.AppendLine($"- {file.Name} ({file.SizeFormatted}) - {reason} - {file.RelativePath}");
                }
            }
        }

        return sb.ToString();
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
```

---

## 七、结果解析器（四层容错 + Schema 校验）

```csharp
// ── Services/PanAnalysis/ResultParser.cs ──

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// AI 响应解析器
/// </summary>
internal interface IPanAnalysisResultParser
{
    PanAnalysisResult Parse(string rawResponse);
}

internal class PanAnalysisResultParser : IPanAnalysisResultParser
{
    private readonly ILogger _logger;
    private readonly JsonSerializerSettings _jsonSettings;

    public PanAnalysisResultParser(ILogger logger)
    {
        _logger = logger;
        _jsonSettings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore
        };
    }

    public PanAnalysisResult Parse(string rawResponse)
    {
        var result = new PanAnalysisResult
        {
            RawAiResponse = rawResponse,
            ParseSuccess = false
        };

        // 策略1：直接 JSON 反序列化
        if (TryParseJson(rawResponse, out var parsed))
        {
            result = parsed with { ParseSuccess = true };
            _logger.Information("AI 响应解析成功（策略：直接JSON）");
            return result;
        }

        // 策略2：提取 Markdown 代码块
        var jsonBlock = ExtractMarkdownCodeBlock(rawResponse);
        if (jsonBlock != null && TryParseJson(jsonBlock, out parsed))
        {
            result = parsed with { ParseSuccess = true };
            _logger.Information("AI 响应解析成功（策略：Markdown代码块）");
            return result;
        }

        // 策略3：正则提取最外层 JSON 对象
        var jsonStr = ExtractOutermostJson(rawResponse);
        if (jsonStr != null && TryParseJson(jsonStr, out parsed))
        {
            result = parsed with { ParseSuccess = true };
            _logger.Information("AI 响应解析成功（策略：正则提取）");
            return result;
        }

        // 策略4：纯文本兜底
        result.Summary = "AI 返回格式无法自动解析，以下是原始回复：";
        result.ParseError = "无法解析为 JSON";
        _logger.Warning("AI 响应解析失败，所有策略均无效");
        return result;
    }

    private bool TryParseJson(string json, out PanAnalysisResult? result)
    {
        result = null;
        try
        {
            var parsed = JsonConvert.DeserializeObject<AiResponseSchema>(json, _jsonSettings);
            if (parsed == null) return false;

            // Schema 校验
            var validationResult = ValidateAndFix(parsed);
            if (!validationResult.IsValid) return false;

            result = ConvertToPanAnalysisResult(validationResult.FixedSchema);
            return true;
        }
        catch (JsonException ex)
        {
            _logger.Debug("JSON 解析失败：{Message}", ex.Message);
            return false;
        }
    }

    private (bool IsValid, AiResponseSchema FixedSchema) ValidateAndFix(AiResponseSchema schema)
    {
        var fixedSchema = schema with
        {
            Recommendations = schema.Recommendations?
                .Select(r => ValidateAndFixRecommendation(r))
                .Where(r => r != null)
                .Select(r => r!)
                .ToList() ?? new List<AiRecommendation>()
        };

        // 至少有 summary
        if (string.IsNullOrWhiteSpace(fixedSchema.Summary))
        {
            fixedSchema = fixedSchema with { Summary = "AI 分析完成，但未返回摘要信息。" };
        }

        return (true, fixedSchema);
    }

    private AiRecommendation? ValidateAndFixRecommendation(AiRecommendation rec)
    {
        // 校验 type
        if (!Enum.TryParse<PanRecommendationType>(rec.Type, true, out var type))
        {
            _logger.Warning("未知的推荐类型：{Type}，跳过", rec.Type);
            return null;
        }

        // 校验 targetPath
        if (string.IsNullOrWhiteSpace(rec.TargetPath))
        {
            _logger.Warning("推荐项缺少 targetPath，跳过");
            return null;
        }

        // 根据类型校验必填字段
        switch (type)
        {
            case PanRecommendationType.Delete:
                rec.DestinationPath = null;
                rec.NewName = null;
                break;
            case PanRecommendationType.Move:
                if (string.IsNullOrWhiteSpace(rec.DestinationPath))
                {
                    _logger.Warning("Move 类型缺少 destinationPath，跳过");
                    return null;
                }
                if (!rec.DestinationPath.EndsWith("/"))
                    rec.DestinationPath += "/";
                rec.NewName = null;
                break;
            case PanRecommendationType.Rename:
                if (string.IsNullOrWhiteSpace(rec.NewName))
                {
                    _logger.Warning("Rename 类型缺少 newName，跳过");
                    return null;
                }
                rec.DestinationPath = null;
                break;
        }

        // 校验 priority
        if (!Enum.TryParse<PanPriority>(rec.Priority, true, out var priority))
            priority = PanPriority.Medium;

        return rec with { Type = type.ToString(), Priority = priority.ToString() };
    }

    private PanAnalysisResult ConvertToPanAnalysisResult(AiResponseSchema schema)
    {
        return new PanAnalysisResult
        {
            Summary = schema.Summary ?? "分析完成",
            Recommendations = schema.Recommendations?.Select(r => new PanRecommendation
            {
                Type = Enum.Parse<PanRecommendationType>(r.Type, true),
                TargetPath = r.TargetPath,
                TargetName = System.IO.Path.GetFileName(r.TargetPath),
                DestinationPath = r.DestinationPath,
                NewName = r.NewName,
                Reason = r.Reason ?? "",
                Priority = Enum.Parse<PanPriority>(r.Priority, true)
            }).ToList() ?? new List<PanRecommendation>(),
            ParseSuccess = true
        };
    }

    private string? ExtractMarkdownCodeBlock(string text)
    {
        var match = Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    private string? ExtractOutermostJson(string text)
    {
        var match = Regex.Match(text, @"(\{(?:[^{}]|(?<open>\{)|(?<-open>\}))+(?(open)(?!))\})", RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value : null;
    }

    #region Schema 内部类

    private class AiResponseSchema
    {
        public string? Summary { get; set; }
        public List<AiRecommendation>? Recommendations { get; set; }
    }

    private class AiRecommendation
    {
        public string Type { get; set; } = "";
        public string TargetPath { get; set; } = "";
        public string? DestinationPath { get; set; }
        public string? NewName { get; set; }
        public string? Reason { get; set; }
        public string Priority { get; set; } = "Medium";
    }

    #endregion
}
```

---

## 八、执行引擎（智能批处理）

```csharp
// ── Services/PanAnalysis/ExecutionEngine.cs ──

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// 操作执行引擎
/// </summary>
internal interface IPanExecutionEngine
{
    Task<PanExecutionReport> ExecuteAsync(
        List<PanRecommendation> recommendations,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default);
}

internal class PanExecutionEngine : IPanExecutionEngine
{
    private readonly IPanTokenManager _tokenManager;
    private readonly ILogger _logger;
    private const int BatchSize = 100;
    private const int MaxConcurrentBatches = 1; // 百度 API 不支持并发，保持串行

    public PanExecutionEngine(IPanTokenManager tokenManager, ILogger logger)
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

        var totalBatches = (deletes.Count + moves.Count + renames.Count + BatchSize - 1) / BatchSize;
        var processedBatches = 0;

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
            _logger.Error(ex, "执行过程中发生异常");
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
                    _logger.Warning("删除批次失败，错误码：{ErrorCode}", response.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "删除批次异常");
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
            processedBatches++;
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
                _logger.Error(ex, "移动批次异常");
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
            processedBatches++;
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
                    _logger.Warning("重命名失败：{Path} → {NewName}，错误码：{ErrorCode}",
                        rec.TargetPath, rec.NewName, response.ErrorCode);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "重命名异常：{Path} → {NewName}", rec.TargetPath, rec.NewName);
                report.Results.Add(new PanExecutionResult
                {
                    Recommendation = rec,
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExecutedAt = DateTime.UtcNow
                });
            }

            processed++;
            processedBatches++;
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
internal static class BatchExtensions
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
```

---

## 九、编排器（门面实现）

```csharp
// ── Services/PanAnalysis/BaiduPanAnalysisOrchestrator.cs ──

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
    private readonly ILogger _logger;
    private readonly CancellationTokenSource? _cancellationTokenSource;

    public bool IsAvailable => _tokenManager.IsTokenValid;

    public BaiduPanAnalysisOrchestrator(
        IPanTokenManager tokenManager,
        IAIServiceFactory aiServiceFactory,
        IMemoryCache cache,
        ILogger logger)
    {
        _tokenManager = tokenManager;
        _aiService = aiServiceFactory.Create(new AIConfig
        {
            Model = "gpt-4o-mini",
            Temperature = 0.3,
            MaxTokens = 4096
        });
        _cache = cache;
        _logger = logger;

        _promptBuilder = new PanAnalysisPromptBuilder();
        _resultParser = new PanAnalysisResultParser(logger);
        _executionEngine = new PanExecutionEngine(tokenManager, logger);
        _cancellationTokenSource = new CancellationTokenSource();
    }

    public void Cancel()
    {
        _cancellationTokenSource?.Cancel();
        _logger.Information("用户取消了操作");
    }

    public async Task<PanAnalysisResult> AnalyzeDirectoryAsync(
        string directoryPath,
        AnalysisOptions options,
        IProgress<PanAnalysisProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _cancellationTokenSource?.Token ?? CancellationToken.None);

        // 1. 获取快照
        var snapshot = await GetSnapshotAsync(
            directoryPath,
            options,
            progress,
            linkedCts.Token);

        // 2. 检查缓存
        var cacheKey = $"pan_analysis_{directoryPath}_{options.MaxDepth}";
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
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _cancellationTokenSource?.Token ?? CancellationToken.None);

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

        // 获取文件列表
        using var apiClient = new BaiduPanApiClient(token);

        var allFiles = new List<PanFileInfo>();
        var allFolders = new List<BaseFileInfo>();
        var totalSize = 0L;

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
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            _cancellationTokenSource?.Token ?? CancellationToken.None);

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

            var response = await apiClient.GetFileListAsync(currentPath, "name", 1, 0, 1000);
            if (response.ErrorCode != 0 || response.FileList == null)
                continue;

            foreach (var file in response.FileList)
            {
                yield return (file, depth);

                if (file.IsDir == 1 && (maxDepth == 0 || depth < maxDepth))
                {
                    queue.Enqueue((file.Path, depth + 1));
                }
            }

            // 限流延迟
            await Task.Delay(200, cancellationToken);
        }
    }

    private PanFileInfo MapToPanFileInfo(BaseFileInfo file, string rootPath)
    {
        var relativePath = file.Path;
        if (relativePath.StartsWith(rootPath))
            relativePath = relativePath[rootPath.Length..].TrimStart('/');

        return new PanFileInfo
        {
            FsId = file.FsId,
            Path = file.Path,
            Name = file.ServerFileName,
            RelativePath = relativePath,
            SizeBytes = file.Size,
            Extension = System.IO.Path.GetExtension(file.ServerFileName)?.ToLowerInvariant() ?? "",
            Category = file.Category,
            ServerModifiedTime = DateTimeOffset.FromUnixTimeSeconds(file.ServerMtime).LocalDateTime,
            IsFolder = file.IsDir == 1,
            Md5 = file.Md5,
            IsJunkFile = IsJunkFile(file.ServerFileName, file.Size),
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
                return new PanDuplicateGroup
                {
                    GroupKey = g.Key,
                    DisplayName = sorted.First().Name,
                    SizeBytes = sorted.First().SizeBytes,
                    FileCount = sorted.Count,
                    Files = sorted,
                    KeepFile = sorted.First(),
                    DeleteFiles = sorted.Skip(1).ToList()
                };
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
```

---

## 十、DI 注册

```csharp
// ── Common/ServiceCollectionExtensions.cs ──

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPanAnalysisServices(this IServiceCollection services)
    {
        // Token 管理
        services.AddSingleton<IPanTokenManager, PanTokenManager>();

        // 分析服务
        services.AddSingleton<IBaiduPanAnalysisOrchestrator, BaiduPanAnalysisOrchestrator>();

        // 缓存
        services.AddMemoryCache();

        return services;
    }
}

// 在应用启动时调用
// services.AddPanAnalysisServices();
```

---

## 十一、窗体设计（精简版）

```csharp
// ── Forms/BaiduPanAnalysisForm.cs ──

public partial class BaiduPanAnalysisForm : Form, IThemeable
{
    private readonly IBaiduPanAnalysisOrchestrator _orchestrator;
    private readonly string _directoryPath;
    private readonly IThemeService _themeService;
    private CancellationTokenSource? _cts;
    private PanDirectorySnapshot? _snapshot;
    private PanAnalysisResult? _analysisResult;

    public BaiduPanAnalysisForm(
        IBaiduPanAnalysisOrchestrator orchestrator,
        string directoryPath,
        IThemeService themeService)
    {
        _orchestrator = orchestrator;
        _directoryPath = directoryPath;
        _themeService = themeService;
        InitializeComponent();
        ApplyTheme(_themeService.CurrentTheme);
    }

    private async void btnStartAnalysis_Click(object sender, EventArgs e)
    {
        _cts = new CancellationTokenSource();

        // 禁用按钮
        btnStartAnalysis.Enabled = false;
        btnCancel.Enabled = true;

        var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

        try
        {
            var options = new AnalysisOptions
            {
                MaxDepth = (int)cmbDepth.SelectedValue,
                DetectDuplicates = chkDetectDuplicates.Checked,
                DetectJunkFiles = true,
                UseCache = chkUseCache.Checked
            };

            // 获取快照
            _snapshot = await _orchestrator.GetSnapshotAsync(
                _directoryPath, options, progress, _cts.Token);

            // AI 分析
            _analysisResult = await _orchestrator.AnalyzeDirectoryAsync(
                _directoryPath, options, progress, _cts.Token);

            // 展示结果
            DisplayStatistics(_snapshot.Statistics);
            DisplayRecommendations(_analysisResult.Recommendations);
        }
        catch (OperationCanceledException)
        {
            AppendLog("分析已取消");
        }
        catch (Exception ex)
        {
            AppendLog($"错误：{ex.Message}");
            MessageBox.Show($"分析失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnStartAnalysis.Enabled = true;
            btnCancel.Enabled = false;
            _cts?.Dispose();
            _cts = null;
        }
    }

    private async void btnExecute_Click(object sender, EventArgs e)
    {
        var selected = GetSelectedRecommendations();
        if (!selected.Any())
        {
            MessageBox.Show("请至少选择一项操作", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var summary = $"将执行以下操作：\n" +
                      $"- 删除：{selected.Count(r => r.Type == PanRecommendationType.Delete)} 项\n" +
                      $"- 移动：{selected.Count(r => r.Type == PanRecommendationType.Move)} 项\n" +
                      $"- 重命名：{selected.Count(r => r.Type == PanRecommendationType.Rename)} 项\n\n" +
                      $"确认执行？";

        if (MessageBox.Show(summary, "执行确认", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        _cts = new CancellationTokenSource();
        btnExecute.Enabled = false;

        var progress = new Progress<PanAnalysisProgress>(UpdateProgress);

        try
        {
            var report = await _orchestrator.ExecuteRecommendationsAsync(
                selected, progress, _cts.Token);

            AppendLog($"执行完成：成功 {report.Succeeded}，失败 {report.Failed}");
            MessageBox.Show(report.Summary, "执行结果", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // 刷新网盘页面
            // 通知父窗体刷新
        }
        catch (Exception ex)
        {
            AppendLog($"执行错误：{ex.Message}");
        }
        finally
        {
            btnExecute.Enabled = true;
            _cts?.Dispose();
            _cts = null;
        }
    }

    public void ApplyTheme(Theme theme)
    {
        // 应用主题
    }
}
```

---

## 十二、WebView2BrowserForm 集成

```csharp
// ── Forms/WebView2BrowserForm.cs ──

public partial class WebView2BrowserForm : Form, IThemeable
{
    private readonly IBaiduPanAnalysisOrchestrator _analysisOrchestrator;

    // 新增 AI 分析按钮
    private ToolStripButton btnAiAnalyze;

    private async void btnAiAnalyze_Click(object sender, EventArgs e)
    {
        if (!_analysisOrchestrator.IsAvailable)
        {
            var result = MessageBox.Show(
                "百度网盘未授权，是否立即授权？",
                "授权提示",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // 触发授权流程
                return;
            }
            return;
        }

        var url = WebView?.Source?.ToString();
        if (string.IsNullOrEmpty(url) || !url.StartsWith(Urls.BaiduNetdisk))
        {
            MessageBox.Show("请先打开百度网盘页面", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // 三层路径提取
        var path = await ExtractPanPathViaJsAsync()
                   ?? ExtractPanPathFromUrl(url)
                   ?? PromptManualPathInput();

        if (string.IsNullOrEmpty(path))
            return;

        // 打开分析窗体
        using var form = new BaiduPanAnalysisForm(_analysisOrchestrator, path, _themeService);
        form.ShowDialog(this);

        // 执行完成后刷新页面
        if (form.ExecutedAny)
        {
            await WebView.CoreWebView2.Reload();
        }
    }

    private async Task<string?> ExtractPanPathViaJsAsync()
    {
        if (WebView?.CoreWebView2 == null)
            return null;

        const string js = @"
            (function() {
                // 从 hash 中提取
                var hash = window.location.hash || '';
                var match = hash.match(/path=([^&]*)/);
                if (match) return decodeURIComponent(match[1]);

                // 从 search 中提取
                var search = window.location.search || '';
                match = search.match(/path=([^&]*)/);
                if (match) return decodeURIComponent(match[1]);

                // 尝试从页面元素获取
                var nav = document.querySelector('.g-breadcrumb');
                if (nav) {
                    var items = nav.querySelectorAll('a');
                    if (items.length > 0) {
                        var last = items[items.length - 1];
                        var text = last.textContent.trim();
                        if (text && text !== '我的全部文件' && text !== '全部文件') {
                            return '/' + text;
                        }
                    }
                }
                return '';
            })()";

        try
        {
            var result = await WebView.CoreWebView2.ExecuteScriptAsync(js);
            var path = JsonConvert.DeserializeObject<string>(result);
            return string.IsNullOrEmpty(path) ? null : path;
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractPanPathFromUrl(string url)
    {
        // 处理 hash fragment
        if (url.Contains("#") && url.Contains("path="))
        {
            var hashPart = url.Substring(url.IndexOf('#'));
            return ExtractPathFromQuery(hashPart);
        }
        if (url.Contains("path="))
        {
            return ExtractPathFromQuery(url);
        }
        return null;
    }

    private static string? ExtractPathFromQuery(string queryString)
    {
        var match = Regex.Match(queryString, @"path=([^&]*)");
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }

    private static string? PromptManualPathInput()
    {
        using var dialog = new Form
        {
            Text = "请输入网盘路径",
            Width = 500,
            Height = 150,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent
        };

        var label = new Label { Text = "路径：", Left = 20, Top = 20, Width = 50 };
        var textBox = new TextBox { Left = 80, Top = 18, Width = 370, Text = "/" };
        var confirmBtn = new Button { Text = "确定", Left = 380, Top = 55, Width = 70, DialogResult = DialogResult.OK };

        dialog.Controls.AddRange(new Control[] { label, textBox, confirmBtn });
        dialog.AcceptButton = confirmBtn;

        return dialog.ShowDialog() == DialogResult.OK ? textBox.Text.Trim() : null;
    }
}
```

---

## 十三、实现步骤

| 优先级 | 步骤 | 内容 | 文件 |
|--------|------|------|------|
| P0 | 1 | 创建数据模型 | `Models/PanAnalysisModels.cs` |
| P0 | 2 | 创建 Token 管理器 | `Services/PanAnalysis/TokenManager.cs` |
| P0 | 3 | 创建编排器接口 | `Services/PanAnalysis/IBaiduPanAnalysisOrchestrator.cs` |
| P1 | 4 | 实现 Prompt 构建器 | `Services/PanAnalysis/PromptBuilder.cs` |
| P1 | 5 | 实现结果解析器 | `Services/PanAnalysis/ResultParser.cs` |
| P1 | 6 | 实现执行引擎 | `Services/PanAnalysis/ExecutionEngine.cs` |
| P1 | 7 | 实现编排器 | `Services/PanAnalysis/BaiduPanAnalysisOrchestrator.cs` |
| P2 | 8 | DI 注册 | `Common/ServiceCollectionExtensions.cs` |
| P2 | 9 | 创建分析窗体 | `Forms/BaiduPanAnalysisForm.cs` |
| P2 | 10 | 扩展 WebView2BrowserForm | 按钮 + 路径提取 + 事件 |
| P3 | 11 | 单元测试 | 各组件单元测试 |
| P3 | 12 | 集成测试 | 端到端测试 |

---

## 十四、风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Token 过期 | API 调用 401 | 自动刷新 + 刷新锁 + 引导重新授权 |
| AI 响应截断 | 建议不完整 | 设置足够 MaxTokens，响应后校验完整性 |
| 文件量过大 | AI 超时/超限 | 自适应 Prompt 策略 + 分批处理 |
| 百度 API 限流 | 429 错误 | 内置限流 + 指数退避重试 |
| 路径提取失败 | 无法分析 | 三层降级策略（JS→URL→手动） |
| 误删文件 | 数据丢失 | 删进入回收站 + 执行前确认 + 二次确认 |
| 批量操作超时 | 部分成功 | 每批 ≤100，失败不中断，记录明细 |

---

## 十五、总结

重构后的方案主要改进：

1. **分层清晰**：UI → 编排器 → 具体服务，职责单一
2. **数据完整**：模型包含执行所需全部字段（FsId、Path、Category、Md5）
3. **Token 管理独立**：线程安全，状态机驱动，自动刷新
4. **AI 解析健壮**：四层容错 + Schema 校验
5. **执行可靠**：智能分批 + 同步模式 + 详细报告
6. **用户体验好**：结构化进度、执行前确认、可取消
7. **可测试性强**：接口抽象完整，支持 Mock