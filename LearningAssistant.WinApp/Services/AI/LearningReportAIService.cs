using System.Collections.Concurrent;
using System.Text;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.AI
{
    /// <summary>
    /// AI 报告总结服务（05 报告模块 3.3/3.6）
    /// 结合统计底座聚合数据，生成周/月报的自然语言总结与个性化建议；
    /// 失败或无网络时返回 null，由调用方回退到规则文案，保证报告不中断。
    /// 成本控制：按「用户 + 周期」进行内存缓存，同周期不重复调用；设字数预算。
    /// </summary>
    public class LearningReportAIService
    {
        private readonly IAIService _aiService;
        private readonly ILogger<LearningReportAIService>? _logger;

        // 缓存：key = "{userId}|{kind}|{periodKey}"（05 方案 3.6 成本控制）
        private readonly ConcurrentDictionary<string, string> _cache = new();

        /// <summary>单段总结最大字符数（成本预算）</summary>
        private const int MaxSummaryLength = 600;

        public LearningReportAIService(IAIService aiService, ILogger<LearningReportAIService>? logger = null)
        {
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _logger = logger;
        }

        /// <summary>
        /// 生成 AI 报告总结。成功返回总结文本；失败（无网/异常）返回 null，调用方回退规则文案。
        /// </summary>
        public async Task<string?> GenerateSummaryAsync(
            string userId, StructuredReport report, CancellationToken ct = default)
        {
            if (report == null || report.ItemsStudied == 0 && report.TimeSpentMinutes == 0)
            {
                // 无数据不浪费一次调用
                return null;
            }

            var cacheKey = BuildCacheKey(userId, report);
            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            try
            {
                var prompt = BuildPrompt(report);
                var text = await _aiService.AskQuestionAsync(prompt, "学习报告总结", ct);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }

                string trimmed = TrimToBudget(text.Trim());
                _cache[cacheKey] = trimmed;
                return trimmed;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "AI 报告总结失败或无网络，回退规则文案");
            }

            return null;
        }

        /// <summary>清除某用户缓存（数据明显变化时强制重生成）</summary>
        public void Invalidate(string userId, ReportPeriodKind kind, string periodKey)
        {
            _cache.TryRemove($"{userId}|{kind}|{periodKey}", out _);
        }

        private static string BuildCacheKey(string userId, StructuredReport report)
        {
            string periodKey = report.Kind switch
            {
                ReportPeriodKind.Daily => report.StartDate.ToString("yyyyMMdd"),
                ReportPeriodKind.Weekly => report.StartDate.ToString("yyyy'w'MMdd"),
                _ => report.StartDate.ToString("yyyyMM")
            };
            return $"{userId}|{report.Kind}|{periodKey}";
        }

        private static string BuildPrompt(StructuredReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("请根据以下学习报告数据，用中文写一段 120 字以内的自然语言总结与个性化建议。");
            sb.AppendLine("要点：总体表现、进步/退步、弱项提示、下周行动计划（若为日报则写明日行动）。");
            sb.AppendLine("语气积极、具体、可执行，不要重复罗列原始数字。");
            sb.AppendLine("-----");
            sb.AppendLine($"周期：{report.PeriodLabel}");
            sb.AppendLine($"学习时长：{report.TimeSpentMinutes} 分钟（较上期 {report.TimeSpentDeltaMinutes:+0;-0;0} 分钟）");
            sb.AppendLine($"学习项数：{report.ItemsStudied}（较上期 {report.ItemsStudiedDelta:+0;-0;0}）");
            sb.AppendLine($"正确率：{report.Accuracy:F1}%（较上期 {report.AccuracyDelta:+0.0;-0.0;0}%）");
            sb.AppendLine($"连续学习：{report.StreakDays} 天；等级：Lv.{report.Level}；XP：{report.XP}");
            sb.AppendLine($"目标达成：{(report.GoalCompleted ? "是" : "否")}");
            if (!string.IsNullOrEmpty(report.TopCategory)) sb.AppendLine($"优势科目：{report.TopCategory}");
            if (!string.IsNullOrEmpty(report.WeakCategory)) sb.AppendLine($"待提升科目：{report.WeakCategory}");

            var cats = report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(5);
            if (cats.Any())
            {
                sb.AppendLine("分类分布：");
                foreach (var c in cats) sb.AppendLine($"- {c.Category}：{c.TimeSpentMinutes} 分钟，{c.ItemsStudied} 项，正确率 {c.Accuracy:F0}%");
            }
            return sb.ToString();
        }

        private static string TrimToBudget(string text)
        {
            if (text.Length <= MaxSummaryLength) return text;
            return text.Substring(0, MaxSummaryLength).Trim() + "…";
        }
    }
}