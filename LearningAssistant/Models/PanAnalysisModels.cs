using LearningAssistant.Services.PanAnalysis;
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

/// <summary>目录信息（供目录树展示与分层 AI 上下文使用）</summary>
public List<PanFolderInfo> Folders { get; set; } = new();

public PanStatistics Statistics { get; set; } = new();
public List<PanDuplicateGroup> Duplicates { get; set; } = new();

/// <summary>快照是否完整（达到文件数上限被截断时为 false）</summary>
public bool IsComplete { get; set; }

/// <summary>快照来源（标记是 API 拉取/内存缓存/磁盘缓存命中）</summary>
[Newtonsoft.Json.JsonIgnore]
public PanSnapshotSource Source { get; set; } = PanSnapshotSource.Api;
}

/// <summary>
/// 文件夹信息
/// </summary>
public class PanFolderInfo
{
public string Path { get; set; } = "";              // API 路径
public string RelativePath { get; set; } = "";       // 相对于根目录
public string Name { get; set; } = "";
public int Depth { get; set; }
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

/// <summary>文件数上限（0 = 不限制）。达到上限时快照会被截断（IsComplete=false）</summary>
public int MaxFileCount { get; set; }
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

/// <summary>文件打标结果（内容/科目/价值观/年龄段，供筛选与批量整理）</summary>
public List<PanFileTag> FileTags { get; set; } = new();

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
/// 文件打标（AI 依据文件名/路径推断，用于内容筛选与批量整理）
/// </summary>
public class PanFileTag
{
public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);

/// <summary>完整 API 路径（用于执行批量删除/移动）</summary>
public string TargetPath { get; set; } = "";

/// <summary>文件名（展示用）</summary>
public string TargetName { get; set; } = "";

/// <summary>内容摘要（一句话，如"高中数学必修一教材"）</summary>
public string ContentSummary { get; set; } = "";

/// <summary>科目（语文/数学/英语/物理/化学/生物/历史/地理/政治/计算机/艺术/工具/其他/未知）</summary>
public string Subject { get; set; } = "";

/// <summary>价值观取向（积极/中性/消极/不宜/未知）</summary>
public string ValuesOrientation { get; set; } = "";

/// <summary>适合年龄段（全年龄/6-12/13-18/成人18+/未知）</summary>
public string AgeRange { get; set; } = "";

/// <summary>内容质量（优/良/中/差/未知，优先依据网评口碑）</summary>
public string Quality { get; set; } = "";

/// <summary>同类资源对比（一句话说明相对同类知名资源的优劣）</summary>
public string ComparisonNote { get; set; } = "";

/// <summary>打标依据（简要说明推断来源）</summary>
public string Reason { get; set; } = "";

/// <summary>是否被用户勾选（批量整理用）</summary>
public bool IsSelected { get; set; } = true;
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
CreateFolder,   // P0.4 DAG first
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
public bool IsDryRun { get; set; }

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