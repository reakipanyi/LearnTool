using System.Globalization;
using System.Net;
using System.Text;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>HTML 打卡表导出选项。</summary>
    public sealed class PdfReadingCheckInHtmlOptions
    {
        /// <summary>仅输出相对根目录的路径（默认开启，便于外发）。</summary>
        public bool UseRelativePaths { get; init; } = true;

        /// <summary>生成时间；为空则不输出“生成于”字样，保证快照稳定。</summary>
        public DateTime? GeneratedAt { get; init; }
    }

    /// <summary>
    /// 打卡表 HTML 生成：单个自包含文件（内联 CSS、无外部资源、无脚本），
    /// 屏幕、打印与导出共用布局模型的分档与分页结果。
    /// </summary>
    public static class PdfReadingCheckInHtmlBuilder
    {
        /// <summary>生成离线可打印的 HTML 报表。</summary>
        public static string Build(PdfReadingCheckInReport report, PdfReadingCheckInHtmlOptions? options = null)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            options ??= new PdfReadingCheckInHtmlOptions();
            var builder = new StringBuilder(16 * 1024);

            builder.AppendLine("<!DOCTYPE html>");
            builder.AppendLine("<html lang=\"zh-CN\">");
            builder.AppendLine("<head>");
            builder.AppendLine("<meta charset=\"utf-8\">");
            builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
            builder.AppendLine("<title>阅读打卡表</title>");
            builder.AppendLine("<style>");
            AppendStyles(builder);
            builder.AppendLine("</style>");
            builder.AppendLine("</head>");
            builder.AppendLine("<body>");

            AppendHeader(builder, report);
            AppendSummary(builder, report, options);
            AppendMatrix(builder, report, options);
            AppendFooter(builder, report, options);

            builder.AppendLine("</body>");
            builder.AppendLine("</html>");
            return builder.ToString();
        }

        private static void AppendStyles(StringBuilder builder)
        {
            builder.AppendLine("@page { size: A4 landscape; margin: 10mm; }");
            builder.AppendLine("* { box-sizing: border-box; }");
            builder.AppendLine("body { margin: 0; color: #263238; font-family: \"Microsoft YaHei\", \"PingFang SC\", \"Segoe UI\", sans-serif; font-size: 10px; }");
            builder.AppendLine("h1 { font-size: 18px; margin: 0 0 2mm; }");
            builder.AppendLine("h2 { font-size: 12px; margin: 3mm 0 1.5mm; padding-left: 2mm; border-left: 3px solid #4caf50; }");
            builder.AppendLine(".meta { color: #546e7a; margin-bottom: 1.5mm; }");
            builder.AppendLine(".metrics { margin: 1.5mm 0; font-weight: 600; }");
            builder.AppendLine(".metrics span { margin-right: 6mm; }");
            builder.AppendLine(".legend { margin: 1.5mm 0 2mm; color: #546e7a; }");
            builder.AppendLine(".legend span { display: inline-block; margin-right: 4mm; }");
            builder.AppendLine(".swatch { display: inline-block; width: 8mm; height: 3mm; border: 1px solid #cfd8dc; vertical-align: middle; margin-right: 1mm; }");
            builder.AppendLine(".warn { margin: 2mm 0; padding: 2mm; border: 1px solid #ffb74d; background: #fff3e0; color: #e65100; }");
            builder.AppendLine("table { width: 100%; border-collapse: collapse; table-layout: fixed; }");
            builder.AppendLine("thead { display: table-header-group; }");
            builder.AppendLine("th, td { border: 1px solid #cfd8dc; padding: 1px 2px; overflow: hidden; white-space: nowrap; text-overflow: ellipsis; }");
            builder.AppendLine("th { background: #eceff1; font-weight: 600; text-align: center; }");
            builder.AppendLine("tr { page-break-inside: avoid; }");
            builder.AppendLine("tr.folder td { background: #f5f7f8; font-weight: 600; }");
            builder.AppendLine("tr.missing td { color: #90a4ae; }");
            builder.AppendLine("td.center, th.center { text-align: center; }");
            builder.AppendLine("td.num { text-align: right; }");
            builder.AppendLine("td.path { text-align: left; }");
            builder.AppendLine("td.under { color: #90a4ae; text-align: center; border-style: dashed; }");
            builder.AppendLine("td.checked { color: #1b5e20; font-weight: 700; text-align: center; }");
            builder.AppendLine("td.empty { color: #cfd8dc; text-align: center; }");
            builder.AppendLine(".page-break { page-break-before: always; }");
            builder.AppendLine(".footer { margin-top: 3mm; color: #607d8b; font-size: 9px; }");
            builder.AppendLine(".weekend { background: #f7f7f7; }");
        }

        private static void AppendHeader(
            StringBuilder builder,
            PdfReadingCheckInReport report)
        {
            builder.AppendLine("<header>");
            builder.AppendLine("<h1>阅读打卡表</h1>");
            builder.Append("<div class=\"meta\">根目录：")
                .Append(E(report.RootPath))
                .Append(" · 统计区间：")
                .Append(E(FormatRange(report.PeriodStart, report.PeriodEnd)))
                .Append(" · 有效打卡阈值：")
                .Append(report.MinSeconds)
                .AppendLine(" 秒</div>");

            if (report.RootUnavailable)
                builder.AppendLine("<div class=\"warn\">根目录不可用，请检查磁盘是否连接后再导出。</div>");

            builder.Append("<div class=\"metrics\"><span>总体连续：")
                .Append(report.Totals.OverallCurrentStreak > 0 ? $"{report.Totals.OverallCurrentStreak} 天" : "—")
                .Append("</span><span>目录：")
                .Append(report.Totals.FolderCount)
                .Append(" 个</span><span>PDF：")
                .Append(report.Totals.DocumentCount)
                .Append(" 份</span><span>打卡：")
                .Append(report.Totals.CheckInDays)
                .Append(" 天</span><span>累计：")
                .Append(E(FormatDuration(report.Totals.TotalSeconds)))
                .AppendLine("</span></div>");

            AppendLegend(builder, report);
            builder.AppendLine("</header>");
        }

        private static void AppendLegend(StringBuilder builder, PdfReadingCheckInReport report)
        {
            builder.AppendLine("<div class=\"legend\">");
            foreach (var bucket in report.Layout.ColorBuckets)
            {
                builder.Append("<span><i class=\"swatch\" style=\"background:")
                    .Append(E(bucket.BackgroundColor))
                    .Append("\"></i>")
                    .Append(E(bucket.Label))
                    .AppendLine("</span>");
            }

            builder.AppendLine("<span>· 有阅读但未达阈值</span><span>空白 无记录</span>");
            builder.AppendLine("</div>");
        }

        private static void AppendSummary(
            StringBuilder builder,
            PdfReadingCheckInReport report,
            PdfReadingCheckInHtmlOptions options)
        {
            builder.AppendLine("<section class=\"summary\">");
            builder.AppendLine("<h2>目录汇总</h2>");
            builder.AppendLine("<table>");
            builder.AppendLine("<colgroup><col style=\"width:60mm\"><col style=\"width:10mm\"><col style=\"width:10mm\"><col style=\"width:14mm\"><col style=\"width:20mm\"><col style=\"width:14mm\"><col style=\"width:12mm\"><col style=\"width:14mm\"><col style=\"width:20mm\"></colgroup>");
            builder.AppendLine("<thead><tr><th>目录 / 文档</th><th>文档数</th><th>已读</th><th>打卡天数</th><th>累计时长</th><th>最近打卡</th><th>连续</th><th>覆盖率</th><th>状态</th></tr></thead>");
            builder.AppendLine("<tbody>");

            foreach (var folder in report.Folders)
            {
                string folderClass = folder.RelativePath == PdfCheckInLayoutRules.MissingGroupKey ? "folder missing" : "folder";
                builder.Append("<tr class=\"").Append(folderClass).Append("\"><td class=\"path\" title=\"")
                    .Append(E(GetFolderTitle(folder, report, options)))
                    .Append("\">")
                    .Append(E(folder.DisplayPath))
                    .Append("（").Append(folder.DocumentCount).Append(" 本）</td>")
                    .Append("<td class=\"center\">").Append(folder.DocumentCount).Append("</td>")
                    .Append("<td class=\"center\">").Append(folder.ReadDocumentCount).Append("</td>")
                    .Append("<td class=\"center\">").Append(folder.CheckInDays).Append("</td>")
                    .Append("<td class=\"num\">").Append(E(FormatDuration(folder.TotalSeconds))).Append("</td>")
                    .Append("<td class=\"center\">").Append(E(FormatDay(folder.LastCheckInDay))).Append("</td>")
                    .Append("<td class=\"center\">").Append(folder.CurrentStreak > 0 ? folder.CurrentStreak.ToString(CultureInfo.InvariantCulture) : "—").Append("</td>")
                    .Append("<td class=\"center\"></td><td class=\"center\"></td></tr>");
                builder.AppendLine();

                foreach (var document in folder.Documents)
                {
                    string rowClass = document.Exists ? string.Empty : " class=\"missing\"";
                    builder.Append("<tr").Append(rowClass).Append("><td class=\"path\" title=\"")
                        .Append(E(GetDocumentTitle(document, report, options)))
                        .Append("\">").Append(E("　　" + document.FileName));
                    if (!document.Exists)
                        builder.Append("（已失效）");
                    else if (document.RenamedMatched)
                        builder.Append("（已重命名）");
                    builder.Append("</td><td></td><td></td>")
                        .Append("<td class=\"center\">").Append(document.CheckInDays).Append("</td>")
                        .Append("<td class=\"num\">").Append(E(FormatDuration(document.TotalSeconds))).Append("</td>")
                        .Append("<td class=\"center\">").Append(E(FormatDay(document.LastCheckInDay))).Append("</td>")
                        .Append("<td class=\"center\">").Append(document.CurrentStreak > 0 ? document.CurrentStreak.ToString(CultureInfo.InvariantCulture) : "—").Append("</td>")
                        .Append("<td class=\"center\">").Append(E(FormatPercent(document.Coverage))).Append("</td>")
                        .Append("<td class=\"center\">").Append(E(GetStatusText(document.Status))).Append("</td></tr>");
                    builder.AppendLine();
                }
            }

            if (report.Folders.Count == 0)
            {
                builder.AppendLine("<tr><td colspan=\"9\" class=\"center\">该月份暂无阅读记录</td></tr>");
            }

            builder.AppendLine("</tbody>");
            builder.AppendLine("</table>");
            builder.AppendLine("</section>");
        }

        private static void AppendMatrix(
            StringBuilder builder,
            PdfReadingCheckInReport report,
            PdfReadingCheckInHtmlOptions options)
        {
            if (report.Layout.Columns.Count == 0)
                return;

            bool twoSegments = report.Layout.SegmentMode == PdfCheckInPrintSegmentMode.TwoSegments;
            var segments = twoSegments
                ? new[] { (Label: "1-15", Second: false), (Label: "16-31", Second: true) }
                : new[] { (Label: string.Empty, Second: false) };

            for (int index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                var columns = report.Layout.Columns
                    .Where(column => column.InSecondSegment == segment.Second)
                    .ToList();
                if (columns.Count == 0)
                    continue;

                builder.AppendLine("<section class=\"matrix page-break\">");
                string title = twoSegments ? $"打卡矩阵（{segment.Label}）" : "打卡矩阵";
                builder.Append("<h2>").Append(E(title)).Append("</h2>");
                builder.AppendLine();
                builder.AppendLine("<table>");
                builder.AppendLine("<colgroup><col style=\"width:30mm\"><col style=\"width:28mm\"><col style=\"width:10mm\"><col style=\"width:12mm\"><col style=\"width:10mm\"></colgroup>");
                builder.Append("<thead><tr><th>目录</th><th>文档</th><th>打卡</th><th>时长</th><th>连续</th>");
                foreach (var column in columns)
                {
                    builder.Append("<th").Append(column.IsWeekend ? " class=\"weekend\"" : string.Empty)
                        .Append(" title=\"").Append(E(column.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))).Append("\">")
                        .Append(column.DayNumber).Append("<br><span>")
                        .Append(E(column.WeekdayLabel)).Append("</span></th>");
                }

                builder.AppendLine("</tr></thead>");
                builder.AppendLine("<tbody>");

                foreach (var folder in report.Folders)
                {
                    builder.Append("<tr class=\"folder\"><td class=\"path\" title=\"")
                        .Append(E(GetFolderTitle(folder, report, options)))
                        .Append("\">").Append(E(folder.DisplayPath))
                        .Append("</td><td class=\"center\">").Append(folder.DocumentCount).Append(" 本</td>")
                        .Append("<td class=\"center\">").Append(folder.CheckInDays).Append("</td>")
                        .Append("<td class=\"num\">").Append(E(FormatDuration(folder.TotalSeconds))).Append("</td>")
                        .Append("<td class=\"center\">").Append(folder.CurrentStreak > 0 ? folder.CurrentStreak.ToString(CultureInfo.InvariantCulture) : "—").Append("</td>");

                    foreach (var column in columns)
                    {
                        AppendFolderCell(builder, folder, column, report.MinSeconds);
                    }

                    builder.AppendLine("</tr>");

                    foreach (var document in folder.Documents)
                    {
                        string rowClass = document.Exists ? string.Empty : " class=\"missing\"";
                        builder.Append("<tr").Append(rowClass).Append("><td class=\"path\">")
                            .Append(E(folder.DisplayPath))
                            .Append("</td><td class=\"path\" title=\"")
                            .Append(E(GetDocumentTitle(document, report, options)))
                            .Append("\">").Append(E(document.FileName));
                        if (!document.Exists)
                            builder.Append("（已失效）");
                        else if (document.RenamedMatched)
                            builder.Append("（已重命名）");
                        builder.Append("</td><td class=\"center\">").Append(document.CheckInDays).Append("</td>")
                            .Append("<td class=\"num\">").Append(E(FormatDuration(document.TotalSeconds))).Append("</td>")
                            .Append("<td class=\"center\">").Append(document.CurrentStreak > 0 ? document.CurrentStreak.ToString(CultureInfo.InvariantCulture) : "—").Append("</td>");

                        foreach (var column in columns)
                        {
                            AppendDocumentCell(builder, document, column, report.MinSeconds);
                        }

                        builder.AppendLine("</tr>");
                    }
                }

                if (report.Folders.Count == 0)
                {
                    builder.Append("<tr><td colspan=\"").Append(columns.Count + 5)
                        .AppendLine("\" class=\"center\">该月份暂无阅读记录</td></tr>");
                }

                builder.AppendLine("</tbody>");
                builder.AppendLine("</table>");
                builder.AppendLine("</section>");
            }
        }

        private static void AppendFolderCell(
            StringBuilder builder,
            PdfCheckInFolderGroup folder,
            PdfCheckInMatrixColumn column,
            int minSeconds)
        {
            int total = 0;
            bool anyChecked = false;
            foreach (var document in folder.Documents)
            {
                if (!document.DaySeconds.TryGetValue(column.Day, out int seconds))
                    continue;

                total += seconds;
                if (seconds >= minSeconds)
                    anyChecked = true;
            }

            if (total <= 0)
            {
                builder.Append("<td class=\"empty\"></td>");
                return;
            }

            string className = anyChecked ? "checked" : "under";
            string background = PdfCheckInLayoutRules.FindBucket(total)?.BackgroundColor ?? string.Empty;
            builder.Append("<td class=\"").Append(className)
                .Append("\" style=\"background:").Append(E(background))
                .Append("\" title=\"").Append(E($"{folder.DocumentCount} 份 PDF · 当日 {FormatDuration(total)}"))
                .Append("\">").Append(anyChecked ? "✓" : "·").Append("</td>");
        }

        private static void AppendDocumentCell(
            StringBuilder builder,
            PdfCheckInDocumentRow document,
            PdfCheckInMatrixColumn column,
            int minSeconds)
        {
            if (!document.DaySeconds.TryGetValue(column.Day, out int seconds) || seconds <= 0)
            {
                builder.Append("<td class=\"empty\"></td>");
                return;
            }

            bool checkedIn = seconds >= minSeconds;
            string background = PdfCheckInLayoutRules.FindBucket(seconds)?.BackgroundColor ?? string.Empty;
            string tooltip = checkedIn
                ? FormatDuration(seconds)
                : $"{FormatDuration(seconds)}（未达打卡阈值）";

            builder.Append("<td class=\"").Append(checkedIn ? "checked" : "under")
                .Append("\" style=\"background:").Append(E(background))
                .Append("\" title=\"").Append(E(tooltip))
                .Append("\">").Append(checkedIn ? "✓" : "·").Append("</td>");
        }

        private static void AppendFooter(
            StringBuilder builder,
            PdfReadingCheckInReport report,
            PdfReadingCheckInHtmlOptions options)
        {
            builder.AppendLine("<div class=\"footer\">仅统计阅读器前台停留时间，不含后台阅读。");
            if (options.GeneratedAt.HasValue)
            {
                builder.Append(" 生成于 ")
                    .Append(E(options.GeneratedAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)))
                    .Append("。");
            }

            builder.AppendLine("</div>");
        }

        private static string GetFolderTitle(
            PdfCheckInFolderGroup folder,
            PdfReadingCheckInReport report,
            PdfReadingCheckInHtmlOptions options)
        {
            if (!options.UseRelativePaths || folder.RelativePath == PdfCheckInLayoutRules.MissingGroupKey)
                return folder.DisplayPath;

            if (folder.RelativePath.Length == 0)
                return report.RootPath;

            return Path.Combine(report.RootPath, folder.RelativePath);
        }

        private static string GetDocumentTitle(
            PdfCheckInDocumentRow document,
            PdfReadingCheckInReport report,
            PdfReadingCheckInHtmlOptions options)
        {
            if (!options.UseRelativePaths)
                return document.FullPath;

            string relative = Path.GetRelativePath(report.RootPath, document.FullPath);
            return relative.Length == 0 ? document.FileName : relative;
        }

        private static string GetStatusText(PdfCheckInDocumentStatus status)
            => status switch
            {
                PdfCheckInDocumentStatus.CheckedIn => "已打卡",
                PdfCheckInDocumentStatus.UnderThreshold => "未达标",
                _ => "未读"
            };

        private static string FormatRange(DateTime start, DateTime end)
            => start == end
                ? start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} ~ {end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        private static string FormatDay(DateTime? day)
            => day.HasValue ? day.Value.ToString("M-d", CultureInfo.InvariantCulture) : "—";

        private static string FormatPercent(double coverage)
            => $"{Math.Round(Math.Clamp(coverage, 0, 1) * 100, MidpointRounding.AwayFromZero):0}%";

        private static string FormatDuration(int seconds)
        {
            if (seconds <= 0)
                return "—";

            if (seconds < 60)
                return "<1 分钟";

            int totalMinutes = seconds / 60;
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            if (hours <= 0)
                return $"{minutes} 分钟";
            if (minutes <= 0)
                return $"{hours} 小时";
            return $"{hours} 小时 {minutes} 分钟";
        }

        private static string E(string? value)
            => WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
