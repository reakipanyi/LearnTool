using LearningAssistant.Models.PanAnalysis;

// ReSharper disable once CheckNamespace
namespace LearningAssistant.Models.PanAnalysis;

#region === 待办操作 + 状态 ===

/// <summary>待办操作的执行状态</summary>
public enum TodoStatus
{
    Confirmed,   // 已确认（默认勾选）
    Skipped,     // 用户跳过
    Executing,   // 正在执行
    Succeeded,   // 成功
    Failed       // 失败
}

/// <summary>整理工具中的待办操作（扩展自 PanRecommendation，支持 UI 状态跟踪 + 文件夹重命名专属配置）。P0.1 仅占位。</summary>
public class PanTodoItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public string? SourceRecommendationId { get; set; }
    public PanRecommendationType Type { get; set; }
    public string SourcePath { get; set; } = "";
    public string SourceName { get; set; } = "";
    public string? DestinationPath { get; set; }
    public string? NewName { get; set; }
    public string? ParentPath { get; set; }
    public string? FolderName { get; set; }
    public string Reason { get; set; } = "";
    public TodoStatus Status { get; set; } = TodoStatus.Confirmed;
    public PanExecutionResult? ExecutionResult { get; set; }
    public long? SourceFsId { get; set; }
    public bool IsFolder { get; set; }
    public FolderRenameOptions? RenameOptions { get; set; }
}

#endregion

#region === 剪贴板状态 ===

public enum ClipboardAction
{
    None,
    Cut,    // 剪切 -> 粘贴时 Move
    Copy    // 复制 -> 粘贴时 Copy
}

/// <summary>整理工具内部剪贴板（剪切/复制粘贴，不碰系统剪贴板）。</summary>
public class PanClipboardState
{
    public ClipboardAction Action { get; set; } = ClipboardAction.None;
    public List<PanFileInfo> Items { get; set; } = new();
    public string? SourceDirectory { get; set; }
}

#endregion

#region === 撤销栈 ===

/// <summary>撤销栈条目（P2 阶段开始启用）。</summary>
public class PanUndoEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
    public PanRecommendation OriginalOperation { get; set; } = new();
    public PanRecommendation ReverseOperation { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public bool CanUndo { get; set; } = true;
}

#endregion

#region === 文件夹重命名选项（用户补充需求：手动勾选是否追加文件大小后缀）===

/// <summary>文件夹重命名弹窗 FolderRenameDialog 的配置模型。</summary>
public class FolderRenameOptions
{
    /// <summary>是否追加文件夹总大小后缀。⭐ 用户决策：首次默认不勾选，记忆上次选择。</summary>
    public bool AppendSizeSuffix { get; set; } = false;

    /// <summary>后缀格式。⭐ 用户决策：默认 BracketGB（_[3.25 GB]_ 空格+中括号）。</summary>
    public FolderSizeSuffixFormat SuffixFormat { get; set; } = FolderSizeSuffixFormat.BracketGB;

    /// <summary>小数位数 0~3。默认 2。</summary>
    public int DecimalPlaces { get; set; } = 2;

    /// <summary>后缀放置位置：Suffix(后缀) / Prefix(前缀)。默认 Suffix。</summary>
    public SuffixPosition Position { get; set; } = SuffixPosition.Suffix;

    /// <summary>预览中显示「N 个文件 + M 个子目录」（仅预览，不写入文件名）。</summary>
    public bool ShowCountInPreview { get; set; } = true;

    // --- 计算结果缓存 ---
    public long ComputedSizeBytes { get; set; }
    public int ComputedFileCount { get; set; }
    public int ComputedSubFolderCount { get; set; }

    /// <summary>true=基于部分快照估算（可能偏小），false=实时拉 API 精确值。</summary>
    public bool IsSizeEstimated { get; set; }
}

public enum FolderSizeSuffixFormat
{
    ParenthesisGB,   // _(3.25GB)
    BracketGB,       // _[3.25 GB]   ⭐ 用户默认
    ChineseBracket,  // 【3.25GB】
    HyphenGB,        // -3.25GB
    PrefixGB,        // 前缀 [3.25 GB] 高中数学资料
    Custom           // 自定义模板（预留）
}

public enum SuffixPosition
{
    Suffix,  // 放在后面：高中数学资料_[3.25 GB]
    Prefix   // 放在前面：[3.25 GB]高中数学资料
}

#endregion