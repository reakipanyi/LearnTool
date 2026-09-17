using System.Globalization;
using System.Text;
using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using SkiaSharp;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 报告周期类型（05 报告模块 3.1）
    /// </summary>
    public enum ReportPeriodKind
    {
        Daily,
        Weekly,
        Monthly
    }

    /// <summary>
    /// 结构化报告模型（05 报告模块 3.1/3.2）
    /// 由“报告”Tab 渲染为卡片式页面，并支持导出 MD/HTML/TXT。
    /// 指标卡 = 时长(+环比增量)/学习项(+增量)/正确率(+增量)/连击/等级/目标达成；
    /// 附带趋势序列、分类分布、建议列表与可选 AI 总结。
    /// </summary>
    public class StructuredReport
    {
        public ReportPeriodKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // 指标卡
        public int TimeSpentMinutes { get; set; }
        public int TimeSpentDeltaMinutes { get; set; }
        public int ItemsStudied { get; set; }
        public int ItemsStudiedDelta { get; set; }
        public double Accuracy { get; set; }
        public double AccuracyDelta { get; set; }
        public int StreakDays { get; set; }
        public int XP { get; set; }
        public int Level { get; set; }
        public bool GoalCompleted { get; set; }

        // 分布
        public string TopCategory { get; set; } = string.Empty;
        public string WeakCategory { get; set; } = string.Empty;
        public List<CategoryBreakdown> Categories { get; set; } = new();
        public List<TrendPoint> Trend { get; set; } = new();

        // 建议 / AI
        public List<string> Suggestions { get; set; } = new();
        public double EfficiencyScore { get; set; }
        public string EfficiencySummary { get; set; } = string.Empty;
        public string? AiSummary { get; set; }
    }

    /// <summary>
    /// 结构化报告服务（05 报告模块 3.1/3.6）
    /// 仅依赖统计底座的聚合 DTO，不再直连 <see cref="ILearningAnalyticsService"/>，
    /// 输出统一 <see cref="StructuredReport"/> 供报告 Tab 渲染与导出。
    /// </summary>
    public class StructuredReportService
    {
        private readonly ILearningStatsAggregator _aggregator;
        private readonly ILogger? _logger;

        public StructuredReportService(ILearningStatsAggregator aggregator, ILogger? logger = null)
        {
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));
            _logger = logger;
        }

        /// <summary>构建日报（date 当日）</summary>
        public StructuredReport BuildDaily(string userId, DateTime date)
        {
            var report = new StructuredReport
            {
                Kind = ReportPeriodKind.Daily,
                Title = "学习日报",
                PeriodLabel = date.ToString("yyyy年MM月dd日"),
                StartDate = date.Date,
                EndDate = date.Date
            };

            try
            {
                var overview = _aggregator.GetDailyOverview(userId, date.Date);
                report.TimeSpentMinutes = overview.TimeSpentMinutes;
                report.ItemsStudied = overview.ItemsStudied;
                report.Accuracy = overview.Accuracy;
                report.StreakDays = overview.StreakDays;
                report.XP = overview.XP;
                report.Level = overview.Level;
                report.GoalCompleted = overview.GoalCompleted;

                report.Trend = _aggregator.GetTrend(userId, date.AddDays(-6), date.Date)?.Points ?? new();
                report.Categories = _aggregator.GetCategoryBreakdown(userId, date.Date, date.Date) ?? new();
                ApplyEfficiency(userId, report);
                report.Suggestions = BuildSuggestions(report, dailyGoalMinutes: 30);
                return report;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建日报失败");
                return report;
            }
        }

        /// <summary>构建周报（year 年第 weekNumber 周，ISO 周规则）</summary>
        public StructuredReport BuildWeekly(string userId, int year, int weekNumber)
        {
            var start = GetStartOfWeek(year, weekNumber);
            var end = start.AddDays(6);
            var report = new StructuredReport
            {
                Kind = ReportPeriodKind.Weekly,
                Title = "学习周报",
                PeriodLabel = $"{year}年第{weekNumber}周",
                StartDate = start,
                EndDate = end
            };

            try
            {
                var overview = _aggregator.GetWeeklyOverview(userId, end);
                report.TimeSpentMinutes = overview.TimeSpentMinutes;
                report.TimeSpentDeltaMinutes = overview.TimeSpentDeltaMinutes;
                report.ItemsStudied = overview.ItemsStudied;
                report.ItemsStudiedDelta = overview.ItemsStudiedDelta;
                report.Accuracy = overview.Accuracy;
                report.AccuracyDelta = overview.AccuracyDelta;
                report.StreakDays = overview.StreakDays;
                report.XP = overview.XP;
                report.Level = overview.Level;
                report.GoalCompleted = overview.GoalCompleted;
                report.TopCategory = overview.TopCategory;
                report.WeakCategory = overview.WeakCategory;

                report.Trend = _aggregator.GetTrend(userId, start, end)?.Points ?? new();
                report.Categories = _aggregator.GetCategoryBreakdown(userId, start, end) ?? new();
                ApplyEfficiency(userId, report);
                report.Suggestions = BuildSuggestions(report, dailyGoalMinutes: 50);
                return report;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建周报失败");
                return report;
            }
        }

        /// <summary>构建月报（year 年 month 月）</summary>
        public StructuredReport BuildMonthly(string userId, int year, int month)
        {
            var first = new DateTime(year, month, 1);
            var last = first.AddMonths(1).AddDays(-1);
            var report = new StructuredReport
            {
                Kind = ReportPeriodKind.Monthly,
                Title = "学习月报",
                PeriodLabel = $"{year}年{month}月",
                StartDate = first,
                EndDate = last
            };

            try
            {
                var overview = _aggregator.GetMonthlyOverview(userId, last);
                report.TimeSpentMinutes = overview.TimeSpentMinutes;
                report.TimeSpentDeltaMinutes = overview.TimeSpentDeltaMinutes;
                report.ItemsStudied = overview.ItemsStudied;
                report.ItemsStudiedDelta = overview.ItemsStudiedDelta;
                report.Accuracy = overview.Accuracy;
                report.AccuracyDelta = overview.AccuracyDelta;
                report.StreakDays = overview.StreakDays;
                report.XP = overview.XP;
                report.Level = overview.Level;
                report.GoalCompleted = overview.GoalCompleted;
                report.TopCategory = overview.TopCategory;
                report.WeakCategory = overview.WeakCategory;

                report.Trend = _aggregator.GetTrend(userId, first, last)?.Points ?? new();
                report.Categories = _aggregator.GetCategoryBreakdown(userId, first, last) ?? new();
                ApplyEfficiency(userId, report);
                report.Suggestions = BuildSuggestions(report, dailyGoalMinutes: 2000 / Math.Max(1, DateTime.DaysInMonth(year, month)));
                return report;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "构建月报失败");
                return report;
            }
        }

        // ============ 导出（05 方案 3.2） ============

        public string ExportMarkdown(StructuredReport report) => BuildMarkdown(report);

        public string ExportHtml(StructuredReport report) => BuildHtml(report);

        public string ExportText(StructuredReport report) => BuildText(report);

        /// <summary>
        /// 导出 Excel（05 方案 3.2）：EPPlus 生成 .xlsx，含核心指标/分类分布/每日趋势/建议/AI 总结。
        /// </summary>
        public void ExportExcel(StructuredReport report, string outputPath)
        {
            ExcelPackage.License.SetNonCommercialPersonal("LearningAssistant");

            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("学习报告");

            int row = 1;
            ws.Cells[row, 1].Value = $"{report.Title} · {report.PeriodLabel}";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Size = 14;
            row++;

            ws.Cells[row, 1].Value = $"日期范围：{report.StartDate:yyyy-MM-dd} ~ {report.EndDate:yyyy-MM-dd}";
            ws.Cells[row, 1].Style.Font.Italic = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.Gray);
            row++;
            row++;

            // 核心指标
            row = WriteExcelSectionHeader(ws, row, "核心指标");
            var metrics = new (string Name, string Value, string Delta)[]
            {
                ("学习时长", FormatMinutes(report.TimeSpentMinutes), DeltaText(report.TimeSpentDeltaMinutes, "分钟")),
                ("学习项数", report.ItemsStudied.ToString(), DeltaText(report.ItemsStudiedDelta, "项")),
                ("正确率", $"{report.Accuracy:F1}%", $"{report.AccuracyDelta:+0.0;-0.0;0}%"),
                ("连续学习", $"{report.StreakDays} 天", ""),
                ("等级 / XP", $"Lv.{report.Level} / {report.XP}", ""),
                ("目标达成", report.GoalCompleted ? "是" : "否", "")
            };
            ws.Cells[row, 1].Value = "指标"; ws.Cells[row, 2].Value = "数值"; ws.Cells[row, 3].Value = "较上期";
            row++;
            foreach (var m in metrics)
            {
                ws.Cells[row, 1].Value = m.Name;
                ws.Cells[row, 2].Value = m.Value;
                ws.Cells[row, 3].Value = report.Kind == ReportPeriodKind.Daily ? "—" : m.Delta;
                row++;
            }
            row++;

            // 强弱项
            if (!string.IsNullOrEmpty(report.TopCategory) || !string.IsNullOrEmpty(report.WeakCategory))
            {
                ws.Cells[row, 1].Value = "优势科目：" + (report.TopCategory ?? "—");
                row++;
                ws.Cells[row, 1].Value = "待提升：" + (report.WeakCategory ?? "—");
                row++;
                row++;
            }

            // 分类分布
            if (report.Categories.Count > 0)
            {
                row = WriteExcelSectionHeader(ws, row, "分类分布");
                ws.Cells[row, 1].Value = "分类"; ws.Cells[row, 2].Value = "时长"; ws.Cells[row, 3].Value = "学习项"; ws.Cells[row, 4].Value = "正确率"; row++;
                foreach (var c in report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(10))
                {
                    ws.Cells[row, 1].Value = c.Category;
                    ws.Cells[row, 2].Value = FormatMinutes(c.TimeSpentMinutes);
                    ws.Cells[row, 3].Value = c.ItemsStudied;
                    ws.Cells[row, 4].Value = $"{c.Accuracy:F0}%";
                    row++;
                }
                row++;
            }

            // 每日趋势
            if (report.Trend.Count > 0)
            {
                row = WriteExcelSectionHeader(ws, row, "每日趋势");
                ws.Cells[row, 1].Value = "日期"; ws.Cells[row, 2].Value = "学习量"; row++;
                foreach (var p in report.Trend)
                {
                    ws.Cells[row, 1].Value = p.Date.ToString("MM-dd");
                    ws.Cells[row, 2].Value = Math.Round(p.Value);
                    row++;
                }
                row++;
            }

            // 建议
            if (report.Suggestions.Count > 0)
            {
                row = WriteExcelSectionHeader(ws, row, "建议 / 回退文案");
                foreach (var s in report.Suggestions)
                {
                    ws.Cells[row, 1].Value = s;
                    ws.Cells[row, 1].Style.WrapText = true;
                    row++;
                }
                row++;
            }

            // AI 总结
            if (!string.IsNullOrEmpty(report.AiSummary))
            {
                row = WriteExcelSectionHeader(ws, row, "AI 总结");
                ws.Cells[row, 1].Value = report.AiSummary;
                ws.Cells[row, 1].Style.WrapText = true;
                row++;
            }

            ws.Column(1).Width = 26;
            ws.Column(2).Width = 18;
            ws.Column(3).Width = 18;
            ws.Column(4).Width = 12;
            package.SaveAs(new FileInfo(outputPath));
        }

        /// <summary>
        /// 导出 PDF（05 方案 3.2/3.6）：SkiaSharp 原生生成 A4，中文字体 Microsoft YaHei，
        /// 含指标卡、趋势柱状图、分类分布、建议与 AI 总结，自动分页。
        /// </summary>
        public void ExportPdf(StructuredReport report, Stream outputStream)
        {
            using var stream = new SKManagedWStream(outputStream);
            using var document = SKDocument.CreatePdf(stream);
            using var typeface = SKTypeface.FromFamilyName("Microsoft YaHei");
            using var titleFont = new SKFont(typeface, 22);
            using var headingFont = new SKFont(typeface, 14);
            using var bodyFont = new SKFont(typeface, 11);
            using var smallFont = new SKFont(typeface, 9);

            var canvas = document.BeginPage(PdfPageWidth, PdfPageHeight);
            float y = PdfMargin;
            canvas.DrawColor(SKColors.White);

            try
            {
                // 标题
                canvas.DrawText(report.Title + " · " + report.PeriodLabel, PdfMargin, y, titleFont, PdfPrimaryPaint);
                y += 24;
                canvas.DrawText($"日期范围：{report.StartDate:yyyy-MM-dd} ~ {report.EndDate:yyyy-MM-dd}", PdfMargin, y, smallFont, PdfMutedPaint);
                y += 18;
                canvas.DrawLine(PdfMargin, y, PdfPageWidth - PdfMargin, y, PdfDividerPaint);
                y += 12;

                // 指标卡（2x3 网格）
                var cards = new[]
                {
                    ("学习时长", FormatMinutes(report.TimeSpentMinutes), report.Kind == ReportPeriodKind.Daily ? "" : DeltaText(report.TimeSpentDeltaMinutes, "分钟")),
                    ("学习项数", report.ItemsStudied.ToString(), report.Kind == ReportPeriodKind.Daily ? "" : DeltaText(report.ItemsStudiedDelta, "项")),
                    ("正确率", $"{report.Accuracy:F1}%", report.Kind == ReportPeriodKind.Daily ? "" : $"{report.AccuracyDelta:+0.0;-0.0;0}%"),
                    ("连续学习", $"{report.StreakDays} 天", ""),
                    ("等级", $"Lv.{report.Level}", $"XP {report.XP}"),
                    ("目标", report.GoalCompleted ? "已达成" : "未达成", report.EfficiencyScore > 0 ? $"效率 {report.EfficiencyScore:F0}" : "")
                };
                const float cardW = 178, cardH = 52, gapH = 10, gapV = 10;
                for (int i = 0; i < cards.Length; i++)
                {
                    int col = i % 3, r = i / 3;
                    float cx = PdfMargin + col * (cardW + gapH);
                    float cy = y + r * (cardH + gapV);
                    canvas.DrawRoundRect(new SKRect(cx, cy, cx + cardW, cy + cardH), 8, 8, PdfCardPaint);
                    canvas.DrawText(cards[i].Item1, cx + 10, cy + 18, smallFont, PdfMutedPaint);
                    canvas.DrawText(cards[i].Item2, cx + 10, cy + 38, bodyFont, PdfPrimaryPaint);
                    if (!string.IsNullOrEmpty(cards[i].Item3))
                        canvas.DrawText(cards[i].Item3, cx + 10, cy + cardH - 8, smallFont, PdfAccentPaint);
                }
                y += 2 * (cardH + gapV) + 6;

                // 趋势柱状图
                if (report.Trend.Count > 0)
                {
                    y += 8;
                    canvas.DrawText("每日趋势", PdfMargin, y, headingFont, PdfPrimaryPaint);
                    y += 6;
                    float chartH = 110, chartW = PdfPageWidth - 2 * PdfMargin;
                    float baseY = Math.Min(y + chartH + 8, PdfPageHeight - PdfMargin);
                    float topY = baseY - chartH;
                    DrawPdfTrendBars(canvas, report.Trend, PdfMargin, topY, chartW, chartH, smallFont);
                    y = baseY + 16;
                }

                // 分类分布
                if (report.Categories.Count > 0)
                {
                    y = EnsurePdfSpace(ref canvas, document, ref y, 24);
                    canvas.DrawText("分类分布", PdfMargin, y, headingFont, PdfPrimaryPaint);
                    y += 20;
                    foreach (var c in report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(8))
                    {
                        y = EnsurePdfSpace(ref canvas, document, ref y, 18);
                        canvas.DrawText($"• {c.Category}：{FormatMinutes(c.TimeSpentMinutes)}（{c.ItemsStudied} 项 / 正确率 {c.Accuracy:F0}%）", PdfMargin, y, bodyFont, PdfMutedPaint);
                        y += 18;
                    }
                    y += 6;
                }

                // 建议
                if (report.Suggestions.Count > 0)
                {
                    y = EnsurePdfSpace(ref canvas, document, ref y, 24);
                    canvas.DrawText("建议 / 回退文案", PdfMargin, y, headingFont, PdfPrimaryPaint);
                    y += 20;
                    foreach (var s in report.Suggestions)
                    {
                        foreach (var line in WrapText(s, bodyFont, PdfPageWidth - 2 * PdfMargin))
                        {
                            y = EnsurePdfSpace(ref canvas, document, ref y, 17);
                            canvas.DrawText("• " + line, PdfMargin, y, bodyFont, PdfMutedPaint);
                            y += 17;
                        }
                    }
                    y += 6;
                }

                // AI 总结
                if (!string.IsNullOrEmpty(report.AiSummary))
                {
                    y = EnsurePdfSpace(ref canvas, document, ref y, 24);
                    canvas.DrawText("AI 总结", PdfMargin, y, headingFont, PdfPrimaryPaint);
                    y += 20;
                    foreach (var line in WrapText(report.AiSummary, bodyFont, PdfPageWidth - 2 * PdfMargin))
                    {
                        y = EnsurePdfSpace(ref canvas, document, ref y, 17);
                        canvas.DrawText(line, PdfMargin, y, bodyFont, PdfMutedPaint);
                        y += 17;
                    }
                }
            }
            finally
            {
                document.EndPage();
            }

            document.Close();
        }

        #region 私有构建

        private void ApplyEfficiency(string userId, StructuredReport report)
        {
            try
            {
                var eff = _aggregator.GetEfficiencyReport(userId);
                report.EfficiencyScore = eff.EfficiencyScore;
                report.EfficiencySummary = eff.Summary;
            }
            catch
            {
                // 效率数据缺失不影响报告主流程
            }
        }

        /// <summary>
        /// 规则建议（05 方案 3.1 建议列表；AI 不可用时作为回退文案）。
        /// </summary>
        private List<string> BuildSuggestions(StructuredReport report, int dailyGoalMinutes)
        {
            var list = new List<string>();

            if (!string.IsNullOrEmpty(report.WeakCategory))
            {
                list.Add($"📉 弱项科目：{report.WeakCategory} — 建议优先安排针对性复习");
            }

            if (report.TimeSpentMinutes > 0 && report.TimeSpentMinutes < dailyGoalMinutes)
            {
                list.Add($"⏱️ 本期学习 {FormatMinutes(report.TimeSpentMinutes)}，建议达到每日约 {dailyGoalMinutes} 分钟以保持学习节奏");
            }

            if (report.Accuracy > 0 && report.Accuracy < 70)
            {
                list.Add($"❗ 正确率 {report.Accuracy:F0}% 偏低，建议回看错题、巩固薄弱知识点");
            }

            if (!report.GoalCompleted)
            {
                list.Add("🎯 本期学习目标尚未达成，可设置更贴近实际的目标并坚持执行");
            }

            if (report.TopCategory != report.WeakCategory && !string.IsNullOrEmpty(report.TopCategory))
            {
                list.Add($"✅ 优势科目：{report.TopCategory}，可尝试拓展更高阶内容");
            }

            if (!string.IsNullOrEmpty(report.EfficiencySummary))
            {
                list.Add($"⚡ 效率提示：{report.EfficiencySummary}");
            }

            if (list.Count == 0)
            {
                list.Add(list.Count == 0 && report.ItemsStudied == 0 && report.TimeSpentMinutes == 0
                    ? "🚀 本期暂无学习数据 — 开始学习后这里将生成学习建议"
                    : "✅ 状态不错，继续保持当前的学习节奏！");
            }

            return list;
        }

        private string FormatMinutes(int minutes) => minutes >= 60 ? $"{minutes / 60.0:F1}小时" : $"{minutes}分钟";

        private string BuildText(StructuredReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {report.Title}  {report.PeriodLabel}");
            sb.AppendLine($"日期范围: {report.StartDate:yyyy-MM-dd} ~ {report.EndDate:yyyy-MM-dd}");
            sb.AppendLine("────────────────────────");
            sb.AppendLine($"⏱️ 学习时长: {FormatMinutes(report.TimeSpentMinutes)}");
            if (report.Kind != ReportPeriodKind.Daily) sb.AppendLine($"   较上期: {DeltaText(report.TimeSpentDeltaMinutes, "分钟")}");
            sb.AppendLine($"📚 学习项数: {report.ItemsStudied}");
            if (report.Kind != ReportPeriodKind.Daily) sb.AppendLine($"   较上期: {DeltaText(report.ItemsStudiedDelta, "项")}");
            sb.AppendLine($"✅ 正确率: {report.Accuracy:F1}%");
            if (report.Kind != ReportPeriodKind.Daily) sb.AppendLine($"   较上期: {report.AccuracyDelta:+0.0;-0.0;0}%");
            sb.AppendLine($"🔥 连续学习: {report.StreakDays} 天");
            sb.AppendLine($"⭐ 等级: Lv.{report.Level}   XP: {report.XP}");
            sb.AppendLine($"🎯 目标达成: {(report.GoalCompleted ? "是 ✅" : "否 ❌")}");

            if (!string.IsNullOrEmpty(report.TopCategory))
                sb.AppendLine($"📗 优势科目: {report.TopCategory}");
            if (!string.IsNullOrEmpty(report.WeakCategory))
                sb.AppendLine($"📕 待提升: {report.WeakCategory}");

            if (report.Categories.Count > 0)
            {
                sb.AppendLine("\n📊 分类分布:");
                foreach (var c in report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(8))
                {
                    sb.AppendLine($"  • {c.Category}: {FormatMinutes(c.TimeSpentMinutes)}（{c.ItemsStudied} 项）");
                }
            }

            if (report.AiSummary != null)
            {
                sb.AppendLine("\n🤖 AI 总结:");
                sb.AppendLine(report.AiSummary);
            }

            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("\n💡 建议 / 回退文案:");
                foreach (var s in report.Suggestions) sb.AppendLine($"  • {s}");
            }

            return sb.ToString();
        }

        private string BuildMarkdown(StructuredReport report)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {report.Title} · {report.PeriodLabel}");
            sb.AppendLine();
            sb.AppendLine($"> 日期范围：**{report.StartDate:yyyy-MM-dd}** ~ **{report.EndDate:yyyy-MM-dd}**");
            sb.AppendLine();
            sb.AppendLine("## 核心指标");
            sb.AppendLine();
            sb.AppendLine("| 指标 | 数值 | 较上期 |");
            sb.AppendLine("|------|------|--------|");
            sb.AppendLine($"| ⏱️ 学习时长 | {FormatMinutes(report.TimeSpentMinutes)} | {DeltaCell(report.TimeSpentDeltaMinutes, "分钟", report.Kind)} |");
            sb.AppendLine($"| 📚 学习项数 | {report.ItemsStudied} | {DeltaCell(report.ItemsStudiedDelta, "项", report.Kind)} |");
            sb.AppendLine($"| ✅ 正确率 | {report.Accuracy:F1}% | {DeltaCell(0, "", report.Kind, accuracyDelta: report.AccuracyDelta)} |");
            sb.AppendLine($"| 🔥 连续学习 | {report.StreakDays} 天 | - |");
            sb.AppendLine($"| ⭐ 等级 | Lv.{report.Level} | - |");
            sb.AppendLine($"| 🎯 目标达成 | {(report.GoalCompleted ? "是 ✅" : "否 ❌")} | - |");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(report.TopCategory) || !string.IsNullOrEmpty(report.WeakCategory))
            {
                sb.AppendLine("## 强弱项");
                sb.AppendLine();
                if (!string.IsNullOrEmpty(report.TopCategory)) sb.AppendLine($"- 📗 优势科目：{report.TopCategory}");
                if (!string.IsNullOrEmpty(report.WeakCategory)) sb.AppendLine($"- 📕 待提升：{report.WeakCategory}");
                sb.AppendLine();
            }

            if (report.Categories.Count > 0)
            {
                sb.AppendLine("## 分类分布");
                sb.AppendLine();
                sb.AppendLine("| 分类 | 时长 | 学习项 | 正确率 |");
                sb.AppendLine("|------|------|--------|--------|");
                foreach (var c in report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(10))
                {
                    sb.AppendLine($"| {c.Category} | {FormatMinutes(c.TimeSpentMinutes)} | {c.ItemsStudied} | {c.Accuracy:F0}% |");
                }
                sb.AppendLine();
            }

            if (report.Trend.Count > 0)
            {
                sb.AppendLine("## 每日趋势");
                sb.AppendLine();
                sb.AppendLine("| 日期 | 学习量 |");
                sb.AppendLine("|------|--------|");
                foreach (var p in report.Trend)
                {
                    sb.AppendLine($"| {p.Date:MM-dd} | {p.Value:F0} |");
                }
                sb.AppendLine();
            }

            if (report.AiSummary != null)
            {
                sb.AppendLine("## 🤖 AI 总结");
                sb.AppendLine();
                sb.AppendLine(report.AiSummary);
                sb.AppendLine();
            }

            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("## 💡 建议");
                sb.AppendLine();
                foreach (var s in report.Suggestions) sb.AppendLine($"- {s}");
                sb.AppendLine();
            }

            sb.AppendLine("---");
            sb.AppendLine("*本报告由学习助手自动生成*");
            return sb.ToString();
        }

        private string BuildHtml(StructuredReport report)
        {
            var esc = static string (string s) => System.Net.WebUtility.HtmlEncode(s);

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\">");
            sb.AppendLine("<style>body{font-family:'Microsoft YaHei',sans-serif;color:#333;max-width:860px;margin:24px auto;padding:0 12px;}"
                          + "h1{color:#3f51b5;}table{border-collapse:collapse;width:100%;margin:12px 0;}"
                          + "th,td{border:1px solid #e0e0e0;padding:8px 10px;text-align:left;}th{background:#f5f5fa;}"
                          + ".card{border:1px solid #e0e0e0;border-radius:8px;padding:14px;margin:8px 0;}"
                          + ".delta-up{color:#4caf50;}.delta-down{color:#f44336;}"
                          + "</style></head><body>");
            sb.AppendLine($"<h1>{esc(report.Title)}</h1>");
            sb.AppendLine($"<p><strong>周期：</strong>{esc(report.PeriodLabel)}（{report.StartDate:yyyy-MM-dd} ~ {report.EndDate:yyyy-MM-dd}）</p>");

            sb.AppendLine("<h2>核心指标</h2><div style=\"display:flex;flex-wrap:wrap;gap:12px;\">");
            sb.Append(HtmlCard("⏱️ 学习时长", FormatMinutes(report.TimeSpentMinutes),
                report.Kind == ReportPeriodKind.Daily ? null : DeltaText(report.TimeSpentDeltaMinutes, "分钟")));
            sb.Append(HtmlCard("📚 学习项数", report.ItemsStudied.ToString(),
                report.Kind == ReportPeriodKind.Daily ? null : DeltaText(report.ItemsStudiedDelta, "项")));
            sb.Append(HtmlCard("✅ 正确率", $"{report.Accuracy:F1}%",
                report.Kind == ReportPeriodKind.Daily ? null : $"{report.AccuracyDelta:+0.0;-0.0;0}%"));
            sb.Append(HtmlCard("🔥 连续学习", $"{report.StreakDays} 天", null));
            sb.Append(HtmlCard("⭐ 等级", $"Lv.{report.Level}", $"XP {report.XP}"));
            sb.Append(HtmlCard("🎯 目标", report.GoalCompleted ? "已达成 ✅" : "未达成 ❌",
                report.EfficiencyScore > 0 ? $"效率分 {report.EfficiencyScore:F0}" : null));
            sb.AppendLine("</div>");

            if (report.Categories.Count > 0)
            {
                sb.AppendLine("<h2>分类分布</h2><table><thead><tr><th>分类</th><th>时长</th><th>学习项</th><th>正确率</th></tr></thead><tbody>");
                foreach (var c in report.Categories.OrderByDescending(x => x.TimeSpentMinutes).Take(10))
                {
                    sb.AppendLine($"<tr><td>{esc(c.Category)}</td><td>{FormatMinutes(c.TimeSpentMinutes)}</td><td>{c.ItemsStudied}</td><td>{c.Accuracy:F0}%</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            if (report.Trend.Count > 0)
            {
                sb.AppendLine("<h2>每日趋势</h2><table><thead><tr><th>日期</th><th>学习量</th></tr></thead><tbody>");
                foreach (var p in report.Trend)
                {
                    sb.AppendLine($"<tr><td>{p.Date:MM-dd}</td><td>{p.Value:F0}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            if (!string.IsNullOrEmpty(report.AiSummary))
            {
                sb.AppendLine($"<div class=\"card\"><strong>🤖 AI 总结</strong><p>{esc(report.AiSummary)}</p></div>");
            }

            if (report.Suggestions.Count > 0)
            {
                sb.AppendLine("<div class=\"card\"><strong>💡 建议</strong><ul>");
                foreach (var s in report.Suggestions) sb.AppendLine($"<li>{esc(s)}</li>");
                sb.AppendLine("</ul></div>");
            }

            sb.AppendLine("</body></html>");
            return sb.ToString();
        }

        private static string HtmlCard(string title, string value, string? delta)
        {
            var deltaHtml = string.Empty;
            if (!string.IsNullOrEmpty(delta))
            {
                bool up = delta.StartsWith('+');
                deltaHtml = $"<div class=\"{(up ? "delta-up" : "delta-down")}\">{System.Net.WebUtility.HtmlEncode(delta)}</div>";
            }
            var cls = HTML_CARD_CLASS;
            return $"<div class=\"{cls}\"><div style=\"color:#888;font-size:13px;\">{System.Net.WebUtility.HtmlEncode(title)}</div>"
                   + $"<div style=\"font-size:22px;font-weight:600;margin-top:4px;\">{System.Net.WebUtility.HtmlEncode(value)}</div>{deltaHtml}</div>";
        }

        private const string HTML_CARD_CLASS = "card";

        private static string DeltaText(int delta, string unit)
        {
            if (delta == 0) return "持平";
            string sign = delta > 0 ? "+" : "-";
            return $"{sign}{Math.Abs(delta)} {unit}";
        }

        private static string DeltaCell(int delta, string unit, ReportPeriodKind kind, double accuracyDelta = 0)
        {
            if (kind == ReportPeriodKind.Daily) return "—";
            if (accuracyDelta != 0) return $"{accuracyDelta:+0.0;-0.0;0}%";
            return delta == 0 ? "持平" : $"{delta:+0;-0} {unit}";
        }

        private DateTime GetStartOfWeek(int year, int weekNumber)
        {
            var jan1 = new DateTime(year, 1, 1);
            var daysOffset = (int)jan1.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysOffset > 0) daysOffset -= 7;
            var firstMonday = jan1.AddDays(-daysOffset);
            return firstMonday.AddDays((weekNumber - 1) * 7);
        }

        // ============ Excel / PDF 私有辅助（05 方案 3.2） ============

        /// <summary>A4 纵向页宽（pt）</summary>
        private const float PdfPageWidth = 595f;
        /// <summary>A4 纵向页高（pt）</summary>
        private const float PdfPageHeight = 842f;
        /// <summary>PDF 页边距（pt）</summary>
        private const float PdfMargin = 40f;

        private static readonly SKPaint PdfPrimaryPaint = Pdf.NewPaint(SKColor.Parse("#3f51b5"));
        private static readonly SKPaint PdfMutedPaint = Pdf.NewPaint(SKColor.Parse("#4a4a4a"));
        private static readonly SKPaint PdfAccentPaint = Pdf.NewPaint(SKColor.Parse("#4caf50"));
        private static readonly SKPaint PdfDividerPaint = Pdf.NewPaint(SKColor.Parse("#c5c5c5"));
        private static readonly SKPaint PdfCardPaint = Pdf.NewPaint(SKColor.Parse("#f0f0f6"));

        /// <summary>
        /// 写 Excel 小节标题行并返回下一行号。
        /// </summary>
        private static int WriteExcelSectionHeader(ExcelWorksheet ws, int row, string title)
        {
            ws.Cells[row, 1].Value = title;
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(63, 81, 181));
            var range = ws.Cells[row, 1, row, 4];
            range.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(235, 235, 245));
            return row + 1;
        }

        /// <summary>
        /// PDF 自动分页：若剩余空间不足则结束当前页并开新页，返回当前可用顶部 Y。
        /// </summary>
        private static float EnsurePdfSpace(ref SKCanvas canvas, SKDocument document, ref float y, float space)
        {
            if (y + space > PdfPageHeight - PdfMargin)
            {
                document.EndPage();
                canvas = document.BeginPage(PdfPageWidth, PdfPageHeight);
                canvas.DrawColor(SKColors.White);
                y = PdfMargin;
            }
            return y;
        }

        /// <summary>按字符测量宽度实现中文友好换行。</summary>
        private static List<string> WrapText(string text, SKFont font, float maxWidth)
        {
            var lines = new List<string>();
            if (string.IsNullOrEmpty(text)) return lines;
            foreach (var rawLine in text.Replace("\r", "").Split('\n'))
            {
                if (rawLine.Length == 0)
                {
                    lines.Add("");
                    continue;
                }
                var current = string.Empty;
                foreach (var ch in rawLine)
                {
                    var candidate = current + ch;
                    if (current.Length > 0 && font.MeasureText(candidate) > maxWidth)
                    {
                        lines.Add(current);
                        current = ch.ToString();
                    }
                    else
                    {
                        current = candidate;
                    }
                }
                if (current.Length > 0) lines.Add(current);
            }
            return lines;
        }

        /// <summary>绘制趋势柱状图（PDF 内嵌简图）。</summary>
        private static void DrawPdfTrendBars(SKCanvas canvas, List<TrendPoint> points, float x0, float topY,
            float width, float height, SKFont font)
        {
            if (points.Count == 0) return;
            var max = points.Max(p => p.Value);
            if (max <= 0) return;

            var slotW = width / points.Count;
            var barMaxW = slotW * 0.55f;
            var baseLineY = topY + height;
            for (int i = 0; i < points.Count; i++)
            {
                var v = (float)points[i].Value;
                var barH = height * v / (float)max;
                var cx = x0 + i * slotW + slotW / 2;
                canvas.DrawRect(new SKRect(cx - barMaxW / 2, baseLineY - barH, cx + barMaxW / 2, baseLineY), PdfPrimaryPaint);
                canvas.DrawText(points[i].Date.ToString("dd"), cx - 8, baseLineY + 12, font, PdfMutedPaint);
            }
            canvas.DrawLine(x0, baseLineY, x0 + width, baseLineY, PdfDividerPaint);
        }

        private static class Pdf
        {
            internal static SKPaint NewPaint(SKColor color) => new SKPaint { Color = color, IsAntialias = true };
        }

        #endregion
    }
}