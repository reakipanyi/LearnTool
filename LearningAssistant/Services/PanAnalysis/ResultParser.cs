using System.Text.RegularExpressions;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LearningAssistant.Services.PanAnalysis;

/// <summary>
/// AI 响应解析器
/// </summary>
public interface IPanAnalysisResultParser
{
    PanAnalysisResult Parse(string rawResponse);
}

public class PanAnalysisResultParser : IPanAnalysisResultParser
{
    private readonly ILogger<PanAnalysisResultParser> _logger;
    private readonly JsonSerializerSettings _jsonSettings;

    public PanAnalysisResultParser(ILogger<PanAnalysisResultParser> logger)
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
            result = CopyResult(parsed, rawResponse, true);
            _logger.LogInformation("AI 响应解析成功（策略：直接JSON）");
            return ApplyTolerantFallback(rawResponse, result);
        }

        // 策略2：提取 Markdown 代码块
        var jsonBlock = ExtractMarkdownCodeBlock(rawResponse);
        if (jsonBlock != null && TryParseJson(jsonBlock, out parsed))
        {
            result = CopyResult(parsed, rawResponse, true);
            _logger.LogInformation("AI 响应解析成功（策略：Markdown代码块）");
            return ApplyTolerantFallback(jsonBlock, result);
        }

        // 策略3：正则提取最外层 JSON 对象
        var jsonStr = ExtractOutermostJson(rawResponse);
        if (jsonStr != null && TryParseJson(jsonStr, out parsed))
        {
            result = CopyResult(parsed, rawResponse, true);
            _logger.LogInformation("AI 响应解析成功（策略：正则提取）");
            return ApplyTolerantFallback(jsonStr, result);
        }

        // 策略4：纯文本兜底
        result.Summary = "AI 返回格式无法自动解析，以下是原始回复：";
        result.ParseError = "无法解析为 JSON";
        _logger.LogWarning("AI 响应解析失败，所有策略均无效");
        return result;
    }

    /// <summary>
    /// 严格解析成功但未提取到任何建议/打标时，尝试容错解析。
    /// 网页版 AI（手动粘贴场景）常返回中文 type、不同字段名、或仅文件名路径，
    /// 严格 schema 会把这些条目全部过滤掉，导致列表为空。
    /// </summary>
    private PanAnalysisResult ApplyTolerantFallback(string json, PanAnalysisResult strict)
    {
        if (strict.Recommendations.Count > 0 || strict.FileTags.Count > 0)
            return strict;

        if (TryParseTolerant(json, out var tolerant) &&
            (tolerant.Recommendations.Count > 0 || tolerant.FileTags.Count > 0))
        {
            tolerant.RawAiResponse = strict.RawAiResponse;
            tolerant.ParseSuccess = true;
            _logger.LogInformation("AI 响应解析成功（策略：容错解析，{Rec} 条建议 / {Tag} 个打标）",
                tolerant.Recommendations.Count, tolerant.FileTags.Count);
            return tolerant;
        }

        return strict;
    }

    private PanAnalysisResult CopyResult(PanAnalysisResult parsed, string rawResponse, bool success)
    {
        parsed.RawAiResponse = rawResponse;
        parsed.ParseSuccess = success;
        return parsed;
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
            _logger.LogDebug("JSON 解析失败：{Message}", ex.Message);
            return false;
        }
    }

    private (bool IsValid, AiResponseSchema FixedSchema) ValidateAndFix(AiResponseSchema schema)
    {
        var fixedSchema = schema;
        if (fixedSchema.Recommendations != null)
        {
            fixedSchema.Recommendations = fixedSchema.Recommendations
                .Select(ValidateAndFixRecommendation)
                .Where(r => r != null)
                .Select(r => r!)
                .ToList();
        }
        else
        {
            fixedSchema.Recommendations = new List<AiRecommendation>();
        }

        // 文件打标：过滤掉缺 targetPath 或 targetPath 为空白的项
        if (fixedSchema.FileTags != null)
        {
            fixedSchema.FileTags = fixedSchema.FileTags
                .Where(t => !string.IsNullOrWhiteSpace(t.TargetPath))
                .ToList();
        }
        else
        {
            fixedSchema.FileTags = new List<AiFileTag>();
        }

        // 至少有 summary
        if (string.IsNullOrWhiteSpace(fixedSchema.Summary))
        {
            fixedSchema.Summary = "AI 分析完成，但未返回摘要信息。";
        }

        return (true, fixedSchema);
    }

    private AiRecommendation? ValidateAndFixRecommendation(AiRecommendation rec)
    {
        // 校验 type（兼容中文与大小写变体）
        var normalizedType = NormalizeType(rec.Type);
        if (!Enum.TryParse<PanRecommendationType>(normalizedType, true, out var type))
        {
            _logger.LogWarning("未知的推荐类型：{Type}，跳过", rec.Type);
            return null;
        }

        // 校验 targetPath
        if (string.IsNullOrWhiteSpace(rec.TargetPath))
        {
            _logger.LogWarning("推荐项缺少 targetPath，跳过");
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
            case PanRecommendationType.MergeFolder:
                if (string.IsNullOrWhiteSpace(rec.DestinationPath))
                {
                    _logger.LogWarning("{Type} 类型缺少 destinationPath，跳过", type);
                    return null;
                }
                if (!rec.DestinationPath.EndsWith("/"))
                    rec.DestinationPath += "/";
                rec.NewName = null;
                break;
            case PanRecommendationType.Rename:
                if (string.IsNullOrWhiteSpace(rec.NewName))
                {
                    _logger.LogWarning("Rename 类型缺少 newName，跳过");
                    return null;
                }
                rec.DestinationPath = null;
                break;
        }

        // 校验 priority
        if (!Enum.TryParse<PanPriority>(rec.Priority, true, out _))
            rec.Priority = PanPriority.Medium.ToString();

        rec.Type = type.ToString();
        return rec;
    }

    private PanAnalysisResult ConvertToPanAnalysisResult(AiResponseSchema schema)
    {
        return new PanAnalysisResult
        {
            Summary = schema.Summary ?? "分析完成",
            Recommendations = schema.Recommendations?
                .Select(r => new PanRecommendation
                {
                    Type = Enum.Parse<PanRecommendationType>(r.Type, true),
                    TargetPath = r.TargetPath,
                    TargetName = System.IO.Path.GetFileName(r.TargetPath),
                    DestinationPath = r.DestinationPath,
                    NewName = r.NewName,
                    Reason = r.Reason ?? "",
                    Priority = Enum.TryParse<PanPriority>(r.Priority, true, out var priority) ? priority : PanPriority.Medium
                }).ToList() ?? new List<PanRecommendation>(),
            FileTags = schema.FileTags?
                .Select(t => new PanFileTag
                {
                    TargetPath = t.TargetPath,
                    TargetName = System.IO.Path.GetFileName(t.TargetPath),
                    ContentSummary = t.ContentSummary ?? "",
                    Subject = t.Subject ?? "",
                    ValuesOrientation = t.ValuesOrientation ?? "",
                    AgeRange = t.AgeRange ?? "",
                    Quality = t.Quality ?? "",
                    ComparisonNote = t.ComparisonNote ?? "",
                    Reason = t.Reason ?? ""
                }).ToList() ?? new List<PanFileTag>(),
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

    #region 容错解析（手动粘贴场景）

    /// <summary>
    /// 容错解析：不依赖严格 schema，而是通过字段别名/中文 key 尽力提取。
    /// 支持：中文 type（删除/移动/重命名/合并/保留）、常见英文别名、
    /// 以及 targetPath 仅有文件名（后续由 ReconcileTargetPaths 按名称对齐）。
    /// </summary>
    private bool TryParseTolerant(string json, out PanAnalysisResult result)
    {
        result = new PanAnalysisResult { ParseSuccess = false };
        try
        {
            var token = JToken.Parse(json);
            JObject root;
            if (token is JObject obj)
            {
                root = obj;
            }
            else if (token is JArray arr)
            {
                // 无根对象的数组：视为 recommendations
                root = new JObject { ["recommendations"] = arr };
            }
            else
            {
                return false;
            }

            var recommendations = new List<PanRecommendation>();
            var fileTags = new List<PanFileTag>();

            if (FirstToken(root, "recommendations", "suggestions", "advice", "建议", "整理建议", "operations", "ops") is JArray recArray)
            {
                foreach (var item in recArray.OfType<JObject>())
                {
                    var rec = TolerantRecommendation(item);
                    if (rec != null) recommendations.Add(rec);
                }
            }

            if (FirstToken(root, "fileTags", "file_tags", "tags", "打标", "文件打标", "files") is JArray tagArray)
            {
                foreach (var item in tagArray.OfType<JObject>())
                {
                    var tag = TolerantFileTag(item);
                    if (tag != null) fileTags.Add(tag);
                }
            }

            if (recommendations.Count == 0 && fileTags.Count == 0)
                return false;

            result.Summary = FirstString(root, "summary", "摘要", "结论", "总结") ?? "AI 分析完成，但未返回摘要信息。";
            result.Recommendations = recommendations;
            result.FileTags = fileTags;
            result.ParseSuccess = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private PanRecommendation? TolerantRecommendation(JObject item)
    {
        var typeRaw = FirstString(item, "type", "action", "op", "操作", "动作");
        var normalizedType = NormalizeType(typeRaw);
        if (!Enum.TryParse<PanRecommendationType>(normalizedType, true, out var type))
            return null;

        var targetPath = FirstString(item, "targetPath", "target_path", "path", "filePath", "file_path", "路径", "target", "文件名") ?? "";
        if (string.IsNullOrWhiteSpace(targetPath))
            return null;

        var destination = FirstString(item, "destinationPath", "destination_path", "destination", "destPath", "目标目录", "目标");
        var newName = FirstString(item, "newName", "new_name", "newname", "新名称", "新名字");

        switch (type)
        {
            case PanRecommendationType.Delete:
                destination = null;
                newName = null;
                break;
            case PanRecommendationType.Move:
            case PanRecommendationType.MergeFolder:
                if (string.IsNullOrWhiteSpace(destination)) return null;
                if (!destination.EndsWith("/")) destination += "/";
                newName = null;
                break;
            case PanRecommendationType.Rename:
                if (string.IsNullOrWhiteSpace(newName)) return null;
                destination = null;
                break;
            case PanRecommendationType.Keep:
                break;
        }

        var priorityRaw = FirstString(item, "priority", "优先级");
        if (!Enum.TryParse<PanPriority>(priorityRaw, true, out var priority))
            priority = PanPriority.Medium;

        return new PanRecommendation
        {
            Type = type,
            TargetPath = targetPath,
            TargetName = Path.GetFileName(targetPath.TrimEnd('/')),
            DestinationPath = destination,
            NewName = newName,
            Reason = FirstString(item, "reason", "原因", "说明") ?? "",
            Priority = priority
        };
    }

    private PanFileTag? TolerantFileTag(JObject item)
    {
        var targetPath = FirstString(item, "targetPath", "target_path", "path", "filePath", "file_path", "路径", "target", "文件名") ?? "";
        if (string.IsNullOrWhiteSpace(targetPath))
            return null;

        return new PanFileTag
        {
            TargetPath = targetPath,
            TargetName = Path.GetFileName(targetPath.TrimEnd('/')),
            ContentSummary = FirstString(item, "contentSummary", "content_summary", "content", "摘要", "内容摘要") ?? "",
            Subject = FirstString(item, "subject", "科目") ?? "",
            ValuesOrientation = FirstString(item, "valuesOrientation", "values_orientation", "values", "value", "价值观", "价值观取向") ?? "",
            AgeRange = FirstString(item, "ageRange", "age_range", "age", "年龄段", "适合年龄段") ?? "",
            Quality = FirstString(item, "quality", "质量", "内容质量") ?? "",
            ComparisonNote = FirstString(item, "comparisonNote", "comparison", "对比", "同类对比") ?? "",
            Reason = FirstString(item, "reason", "原因", "依据") ?? ""
        };
    }

    /// <summary>按候选 key 依次查找 JObject 字段（忽略大小写）</summary>
    private static JToken? FirstToken(JObject obj, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (obj.TryGetValue(key, StringComparison.OrdinalIgnoreCase, out var token))
                return token;
        }
        return null;
    }

    /// <summary>按候选 key 依次取字符串值（null/空字符串返回 null）</summary>
    private static string? FirstString(JObject obj, params string[] keys)
    {
        var token = FirstToken(obj, keys);
        if (token == null || token.Type == JTokenType.Null) return null;
        var value = token.ToString().Trim();
        return value.Length == 0 ? null : value;
    }

    /// <summary>归一化操作类型：兼容中文与大小写变体</summary>
    private static string NormalizeType(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        var t = raw.Trim();
        return t.ToLowerInvariant() switch
        {
            "删除" or "删" or "delete" => "Delete",
            "移动" or "移" or "move" => "Move",
            "重命名" or "改名" or "rename" => "Rename",
            "合并" or "合并文件夹" or "merge" or "mergefolder" => "MergeFolder",
            "保留" or "keep" => "Keep",
            _ => t
        };
    }

    #endregion

    #region Schema 内部类

    private class AiResponseSchema
    {
        public string? Summary { get; set; }
        public List<AiRecommendation>? Recommendations { get; set; }
        public List<AiFileTag>? FileTags { get; set; }
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

    private class AiFileTag
    {
        public string TargetPath { get; set; } = "";
        public string? ContentSummary { get; set; }
        public string? Subject { get; set; }
        public string? ValuesOrientation { get; set; }
        public string? AgeRange { get; set; }
        public string? Quality { get; set; }
        public string? ComparisonNote { get; set; }
        public string? Reason { get; set; }
    }

    #endregion
}
