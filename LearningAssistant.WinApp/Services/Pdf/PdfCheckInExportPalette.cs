using System.Drawing;
using System.Globalization;
using LearningAssistant.Models.Pdf;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 导出与打印共用的配色、表头与文本格式。
    /// Excel、HTML 与分页打印引用同一份定义，保证三种输出的视觉与口径一致。
    /// </summary>
    internal static class PdfCheckInExportPalette
    {
        /// <summary>统一字体族（Excel 字体名与打印字体均使用）。</summary>
        internal const string FontName = "Microsoft YaHei";

        /// <summary>表头/标题底色。</summary>
        internal static readonly Color HeaderFill = Color.FromArgb(236, 239, 241);

        /// <summary>目录行底色。</summary>
        internal static readonly Color FolderFill = Color.FromArgb(245, 247, 248);

        /// <summary>表格线颜色。</summary>
        internal static readonly Color GridLine = Color.FromArgb(207, 216, 220);

        /// <summary>未来日期底色。</summary>
        internal static readonly Color FutureFill = Color.FromArgb(238, 238, 238);

        /// <summary>未达阈值日期底色。</summary>
        internal static readonly Color UnderThresholdFill = Color.FromArgb(247, 247, 247);

        /// <summary>正文颜色。</summary>
        internal static readonly Color TextColor = Color.FromArgb(38, 50, 56);

        /// <summary>次要文字颜色（元信息、页脚、未达标数值）。</summary>
        internal static readonly Color SecondaryTextColor = Color.FromArgb(84, 110, 122);

        /// <summary>占位文字颜色（失效文件、未来日期）。</summary>
        internal static readonly Color MissingTextColor = Color.FromArgb(144, 164, 174);

        /// <summary>重命名提示颜色。</summary>
        internal static readonly Color RenamedTextColor = Color.FromArgb(230, 81, 0);

        /// <summary>目录汇总表头（9 列）。</summary>
        internal static readonly string[] SummaryHeaders =
        {
            "目录 / 文档", "文档数", "已读", "打卡天数", "累计时长", "最近打卡", "连续", "覆盖率", "状态"
        };

        /// <summary>打卡矩阵前 5 个固定列表头。</summary>
        internal static readonly string[] MatrixFixedHeaders = { "目录", "文档", "打卡", "时长", "连续" };

        /// <summary>根目录展示：相对路径模式只显示末级目录名，避免外发时泄露完整路径。</summary>
        internal static string GetRootDisplay(string rootPath, bool useRelativePaths)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
                return "（未选择）";

            if (!useRelativePaths)
                return rootPath;

            string trimmed = rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string name = Path.GetFileName(trimmed);
            return string.IsNullOrEmpty(name) ? rootPath : name;
        }

        /// <summary>目录行文本：按层级缩进，可附文档数；失效文件组使用固定文案。</summary>
        internal static string BuildFolderText(
            PdfCheckInFolderGroup folder,
            bool includeCount,
            PdfReadingCheckInReport report,
            bool useRelativePaths)
        {
            string indent = folder.Depth > 0 ? new string('　', Math.Min(folder.Depth, 8)) : string.Empty;
            string title;
            if (folder.RelativePath == PdfCheckInLayoutRules.MissingGroupKey)
            {
                title = PdfCheckInLayoutRules.MissingGroupDisplay;
            }
            else if (string.IsNullOrEmpty(folder.RelativePath))
            {
                title = PdfCheckInLayoutRules.RootFolderDisplay;
            }
            else
            {
                title = useRelativePaths || string.IsNullOrWhiteSpace(report.RootPath)
                    ? folder.DisplayPath
                    : Path.Combine(report.RootPath, folder.RelativePath);
            }

            return includeCount
                ? $"{indent}{title}（{folder.DocumentCount} 本）"
                : indent + title;
        }

        /// <summary>文件名后缀标记：失效或已重命名。</summary>
        internal static string GetDocumentSuffix(PdfCheckInDocumentRow? document)
        {
            if (document == null)
                return string.Empty;
            if (document.RenamedMatched)
                return "（已重命名）";
            return document.Exists ? string.Empty : "（已失效）";
        }

        internal static string FormatCheckInDays(int days) => days > 0 ? $"{days} 天" : "—";

        internal static string FormatStreak(int days) => days > 0 ? $"{days} 天" : "—";

        internal static string FormatDay(DateTime? day)
            => day.HasValue ? day.Value.ToString("M-d", CultureInfo.InvariantCulture) : "—";

        internal static string FormatCoverage(double coverage)
            => $"{(int)Math.Round(Math.Clamp(coverage, 0, 1) * 100, MidpointRounding.AwayFromZero)}%";

        internal static string FormatStatus(PdfCheckInDocumentStatus status)
            => status switch
            {
                PdfCheckInDocumentStatus.CheckedIn => "已打卡",
                PdfCheckInDocumentStatus.UnderThreshold => "有记录",
                _ => "未读"
            };

        internal static string FormatDuration(int seconds)
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
            return minutes <= 0 ? $"{hours} 小时" : $"{hours} 小时 {minutes} 分钟";
        }

        /// <summary>日期单元格的分钟数；不足 1 分钟显示 &lt;1。</summary>
        internal static string FormatMinuteCell(int seconds)
            => seconds < 60 ? "<1" : (seconds / 60).ToString(CultureInfo.InvariantCulture);

        /// <summary>颜色转 HTML 十六进制文本，供 Excel 批量设色使用。</summary>
        internal static string ColorHex(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";
    }
}
