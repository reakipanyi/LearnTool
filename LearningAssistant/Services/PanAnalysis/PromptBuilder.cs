using System.Text;
using LearningAssistant.Models.PanAnalysis;

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
                sb.AppendLine($"| {EscapeMarkdown(file.Name)} | {file.SizeFormatted} | {file.CategoryName} | {modified} | {EscapeMarkdown(file.RelativePath)} |");
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
                sb.AppendLine($"| {EscapeMarkdown(file.Name)} | {file.SizeFormatted} | {file.CategoryName} | {modified} |");
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
                sb.AppendLine($"- {EscapeMarkdown(file.Name)} ({file.SizeFormatted}) - {EscapeMarkdown(file.RelativePath)}");
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
                    sb.AppendLine($"- {EscapeMarkdown(file.Name)} ({file.SizeFormatted}) - {reason} - {EscapeMarkdown(file.RelativePath)}");
                }
            }
        }

        return sb.ToString();
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
