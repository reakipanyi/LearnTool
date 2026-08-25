using LearningAssistant.Models.PanAnalysis;
using System.Text;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// AI Prompt 构建器
/// </summary>
public interface IPanAnalysisPromptBuilder
{
    /// <summary>构建 System Prompt（含规则和格式说明）</summary>
    string BuildSystemPrompt();

    /// <summary>构建 User Prompt（根据快照数据量自适应）</summary>
    string BuildUserPrompt(PanDirectorySnapshot snapshot);
}

public class PanAnalysisPromptBuilder : IPanAnalysisPromptBuilder
{
    private readonly int _maxFilesForFullList = 200;
    private readonly int _maxFilesForCompactList = 1000;

    public string BuildSystemPrompt()
    {
        return $@"
你是百度网盘文件整理专家，请按以下规则分析文件列表并给出整理建议。

## 联网能力（若可用）
- 你具备联网检索能力。对教材、影视、小说等能识别的资源，请联网搜索网评（豆瓣评分、家长/读者测评、教育资源站口碑等），用于判断内容质量与年龄段适配。
- 若无法联网或检索不到有效网评，则退回基于文件名/路径的推断，并在 reason 中注明（如 ""依据文件名推断，未见有效网评""）。
- 网评仅供参考：价值观/年龄段仍以文件名与目录语义为主，避免仅凭单一评论下结论。

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

7. **文件打标**（输出 fileTags，用于内容筛选与批量整理）：
   - 对文件名、目录路径能可靠推断的文件打标（无法推断的字段用 ""未知""）
   - contentSummary：一句话内容摘要（如 ""高中数学必修一教材""）
   - subject：科目，取值限 语文/数学/英语/物理/化学/生物/历史/地理/政治/计算机/艺术/音乐/体育/工具/影视/小说/其他/未知
   - valuesOrientation：价值观取向，取值限 积极/中性/消极/不宜/未知
     （依据文件名推断，如教材通常中性、涉黄赌毒/暴力血腥/低俗为不宜）
   - ageRange：适合年龄段，取值限 全年龄/6-12/13-18/成人18+/未知
     （儿童读物/动画=6-12，中学教材=13-18，涉成人内容=成人18+；有网评时结合网评佐证）
   - quality：内容质量，取值限 优/良/中/差/未知
     （优先依据网评口碑：高分/广泛好评为优，存在明显差评/侵权/低质为差，无网评时按文件名推断或为未知）
   - comparisonNote：同类资源对比，用一句话说明相对同类知名资源的优劣
     （如 ""口碑优于市面常见《XX》系列""、""与《XX》同类但更浅显""；无对比依据时留空）
   - 打标数量：文件少（<=200）时全量打标；文件多时对能明确识别的前 100 个文件打标即可，其余忽略

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
  ],
  ""fileTags"": [
    {{
      ""targetPath"": ""/完整/路径"",
      ""contentSummary"": ""内容摘要"",
      ""subject"": ""科目"",
      ""valuesOrientation"": ""积极|中性|消极|不宜|未知"",
      ""ageRange"": ""全年龄|6-12|13-18|成人18+|未知"",
      ""quality"": ""优|良|中|差|未知"",
      ""comparisonNote"": ""同类资源对比（一句话，无则空字符串）"",
      ""reason"": ""打标依据（含网评来源说明）""
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
- fileTags 的 targetPath 必须与输入中的路径完全一致；无法打标的文件不要放入 fileTags
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
        if (!snapshot.IsComplete && snapshot.Scope.MaxFileCount > 0)
        {
            sb.AppendLine($"- ⚠️ 文件数已达上限 {snapshot.Scope.MaxFileCount:N0}，快照被截断，以下仅为基础分析样本，建议对子目录逐一深入分析");
        }
        sb.AppendLine();

        // 目录结构（分层上下文，帮助 AI 理解目录组织方式）
        sb.AppendLine("### 目录结构");
        sb.AppendLine(BuildDirectoryTree(snapshot));
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
            sb.AppendLine();
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

        // 重点关注文件：大文件 + 可疑文件（供 AI 给出具体操作建议）
        sb.AppendLine("### 📦 重点关注文件");
        var largeFiles = snapshot.Files
            .Where(f => !f.IsFolder)
            .OrderByDescending(f => f.SizeBytes)
            .Take(30)
            .ToList();
        if (largeFiles.Any())
        {
            sb.AppendLine("#### 最大的 30 个文件");
            foreach (var file in largeFiles)
            {
                sb.AppendLine($"- {EscapeMarkdown(file.Name)} ({file.SizeFormatted}) - {EscapeMarkdown(file.RelativePath)}");
            }
            sb.AppendLine();
        }

        var suspicious = snapshot.Files
            .Where(f => !f.IsFolder && (f.IsJunkFile || f.IsPotentialDuplicate))
            .Take(50)
            .ToList();
        if (suspicious.Any())
        {
            sb.AppendLine("#### ⚠️ 可疑文件（无意义文件/重复文件）");
            foreach (var file in suspicious)
            {
                var reason = file.IsJunkFile ? "无意义" : "疑似重复";
                sb.AppendLine($"- {EscapeMarkdown(file.Name)} ({file.SizeFormatted}) - {reason} - {EscapeMarkdown(file.RelativePath)}");
            }
            sb.AppendLine();
        }

        // 文件列表（自适应：小目录给完整/精简列表，大目录仅给目录树+重点文件）
        var fileCount = snapshot.Files.Count;
        if (fileCount <= _maxFilesForCompactList)
        {
            sb.AppendLine("### 文件列表");
            if (fileCount <= _maxFilesForFullList)
            {
                // 完整列表
                sb.AppendLine("| 文件名 | 大小 | 类型 | 修改时间 | 路径 |");
                sb.AppendLine("|--------|------|------|----------|------|");
                foreach (var file in snapshot.Files.OrderBy(f => f.RelativePath))
                {
                    var modified = file.ServerModifiedTime?.ToString("yyyy-MM-dd") ?? "-";
                    sb.AppendLine($"| {EscapeMarkdown(file.Name)} | {file.SizeFormatted} | {file.CategoryName} | {modified} | {EscapeMarkdown(file.RelativePath)} |");
                }
            }
            else
            {
                // 精简列表（省略路径）
                sb.AppendLine("| 文件名 | 大小 | 类型 | 修改时间 |");
                sb.AppendLine("|--------|------|------|----------|");
                foreach (var file in snapshot.Files.OrderBy(f => f.RelativePath).Take(_maxFilesForCompactList))
                {
                    var modified = file.ServerModifiedTime?.ToString("yyyy-MM-dd") ?? "-";
                    sb.AppendLine($"| {EscapeMarkdown(file.Name)} | {file.SizeFormatted} | {file.CategoryName} | {modified} |");
                }
                if (fileCount > _maxFilesForCompactList)
                    sb.AppendLine($"| ... 还有 {fileCount - _maxFilesForCompactList} 个文件 | ... | ... | ... |");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// 构建目录树：按文件夹层级输出，附带各目录下直接文件数与大小，
    /// 帮助 AI 在上下文有限时也能把握整体目录组织。
    /// </summary>
    private string BuildDirectoryTree(PanDirectorySnapshot snapshot)
    {
        const int maxFoldersShown = 150;
        var sb = new StringBuilder();

        var rootName = snapshot.DirectoryPath.TrimEnd('/');
        var rootLabel = rootName.Substring(rootName.LastIndexOf('/') + 1);
        if (string.IsNullOrEmpty(rootLabel)) rootLabel = "/";

        // 汇总每个目录下直接包含的文件数与大小（文件相对路径的父目录 作为目录键）
        var dirAgg = new Dictionary<string, (int Count, long Size)>();
        foreach (var file in snapshot.Files.Where(f => !f.IsFolder))
        {
            var parent = GetParentPath(file.RelativePath);
            var cur = dirAgg.GetValueOrDefault(parent);
            dirAgg[parent] = (cur.Count + 1, cur.Size + file.SizeBytes);
        }

        // 根目录直接文件
        var rootStat = dirAgg.GetValueOrDefault("");
        sb.AppendLine($"{rootLabel}/  ({rootStat.Count:N0} 个文件, {FormatSize(rootStat.Size)})");

        var folders = snapshot.Folders
            .Where(f => !string.IsNullOrEmpty(f.RelativePath))
            .OrderBy(f => f.Depth)
            .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)
            .Take(maxFoldersShown)
            .ToList();

        foreach (var folder in folders)
        {
            var stat = dirAgg.GetValueOrDefault(folder.RelativePath);
            var indent = new string(' ', Math.Max(0, folder.Depth - 1) * 2);
            sb.AppendLine($"{indent}└─ {EscapeMarkdown(folder.Name)}/  ({stat.Count:N0} 个文件, {FormatSize(stat.Size)})");
        }

        if (snapshot.Folders.Count > maxFoldersShown)
            sb.AppendLine($"... 还有 {snapshot.Folders.Count - maxFoldersShown} 个子目录未展示");

        return sb.ToString();
    }

    /// <summary>返回文件相对路径的直接父目录（"" 表示根目录）</summary>
    private static string GetParentPath(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return "";
        var idx = relativePath.LastIndexOf('/');
        return idx >= 0 ? relativePath.Substring(0, idx) : "";
    }

    private static string EscapeMarkdown(string text)
    {
        return text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
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
