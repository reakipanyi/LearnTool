using System.Text.RegularExpressions;
using LearningAssistant.Models.PanAnalysis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

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
            return result;
        }

        // 策略2：提取 Markdown 代码块
        var jsonBlock = ExtractMarkdownCodeBlock(rawResponse);
        if (jsonBlock != null && TryParseJson(jsonBlock, out parsed))
        {
            result = CopyResult(parsed, rawResponse, true);
            _logger.LogInformation("AI 响应解析成功（策略：Markdown代码块）");
            return result;
        }

        // 策略3：正则提取最外层 JSON 对象
        var jsonStr = ExtractOutermostJson(rawResponse);
        if (jsonStr != null && TryParseJson(jsonStr, out parsed))
        {
            result = CopyResult(parsed, rawResponse, true);
            _logger.LogInformation("AI 响应解析成功（策略：正则提取）");
            return result;
        }

        // 策略4：纯文本兜底
        result.Summary = "AI 返回格式无法自动解析，以下是原始回复：";
        result.ParseError = "无法解析为 JSON";
        _logger.LogWarning("AI 响应解析失败，所有策略均无效");
        return result;
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

        // 至少有 summary
        if (string.IsNullOrWhiteSpace(fixedSchema.Summary))
        {
            fixedSchema.Summary = "AI 分析完成，但未返回摘要信息。";
        }

        return (true, fixedSchema);
    }

    private AiRecommendation? ValidateAndFixRecommendation(AiRecommendation rec)
    {
        // 校验 type
        if (!Enum.TryParse<PanRecommendationType>(rec.Type, true, out var type))
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
