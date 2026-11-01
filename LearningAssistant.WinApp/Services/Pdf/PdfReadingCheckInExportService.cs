using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Text;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;
// 配色、表头与文本格式集中由 PdfCheckInExportPalette 提供，Excel 与打印共用同一份定义
using static LearningAssistant.Services.Pdf.PdfCheckInExportPalette;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>打卡表导出选项（Excel / HTML 共用）。</summary>
    public sealed class PdfReadingCheckInExportOptions
    {
        /// <summary>仅输出相对根目录的路径（默认开启，便于外发）。</summary>
        public bool UseRelativePaths { get; init; } = true;

        /// <summary>生成时间；为空时导出结果不写“生成于”，便于测试比对。</summary>
        public DateTime? GeneratedAt { get; init; }
    }

    /// <summary>打印范围。</summary>
    public enum PdfReadingCheckInPrintScope
    {
        /// <summary>目录汇总 + 打卡矩阵。</summary>
        All,

        /// <summary>仅目录汇总。</summary>
        SummaryOnly,

        /// <summary>仅打卡矩阵。</summary>
        MatrixOnly
    }

    /// <summary>打印选项。</summary>
    public sealed class PdfReadingCheckInPrintOptions
    {
        /// <summary>打印范围。</summary>
        public PdfReadingCheckInPrintScope Scope { get; init; } = PdfReadingCheckInPrintScope.All;

        /// <summary>日期单元格显示模式（勾选 / 分钟），与界面一致。</summary>
        public PdfCheckInCellDisplayMode DisplayMode { get; init; } = PdfCheckInCellDisplayMode.CheckMark;

        /// <summary>是否仅输出相对路径（用于页眉根目录展示）。</summary>
        public bool UseRelativePaths { get; init; } = true;

        /// <summary>生成时间（页脚）。</summary>
        public DateTime GeneratedAt { get; init; } = DateTime.Now;
    }

    /// <summary>
    /// 阅读打卡表导出：Excel 双工作表、离线可打印 HTML、布局模型驱动的分页打印。
    /// 口径与界面一致：仅统计阅读器前台停留时间，图片不参与。
    /// </summary>
    public sealed class PdfReadingCheckInExportService
    {
        private static readonly double[] SummaryColumnWidths = { 46, 9, 8, 11, 15, 11, 8, 10, 12 };

        private static readonly double[] MatrixFixedColumnWidths = { 26, 26, 8, 11, 8 };

        private const int SummaryColumnCount = 9;
        private const int MatrixFixedColumnCount = 5;
        private const double MatrixDateColumnWidth = 3.2;

        private readonly ILogger<PdfReadingCheckInExportService>? _logger;

        public PdfReadingCheckInExportService(ILogger<PdfReadingCheckInExportService>? logger = null)
        {
            _logger = logger;
        }

        #region 文件名

        /// <summary>建议文件名：阅读打卡表_{目录名}_{yyyyMM}.{扩展名}，非法字符替换为下划线。</summary>
        public static string BuildFileName(PdfReadingCheckInReport report, string extension)
        {
            string folderName = report?.RootPath?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) ?? string.Empty;
            string name = folderName.Length > 0 ? Path.GetFileName(folderName) : string.Empty;
            string month = report != null
                ? report.PeriodStart.ToString("yyyyMM", CultureInfo.InvariantCulture)
                : DateTime.Now.ToString("yyyyMM", CultureInfo.InvariantCulture);
            string suffix = extension.Trim().TrimStart('.');
            if (suffix.Length == 0)
                suffix = "dat";

            if (string.IsNullOrWhiteSpace(name))
                return $"阅读打卡表_{month}.{suffix}";

            return $"阅读打卡表_{SanitizeFileName(name)}_{month}.{suffix}";
        }

        private static string SanitizeFileName(string name)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(name.Length);
            foreach (char ch in name)
                builder.Append(invalid.Contains(ch) || ch is '\\' or '/' ? '_' : ch);

            string cleaned = builder.ToString().Trim(' ', '_', '.');
            if (cleaned.Length == 0)
                cleaned = "根目录";
            return cleaned.Length <= 60 ? cleaned : cleaned[..60];
        }

        #endregion

        #region Excel

        /// <summary>
        /// 导出 Excel：`目录汇总` + `打卡矩阵` 两个工作表。
        /// 目录行整行底色、日期单元格按连续区间批量填色，避免逐格设色。
        /// </summary>
        public string ExportExcel(
            PdfReadingCheckInReport report,
            string outputPath,
            PdfReadingCheckInExportOptions? options = null)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            string fullPath = EnsureOutputPath(outputPath, ".xlsx");
            options ??= new PdfReadingCheckInExportOptions();

            ExcelPackage.License.SetNonCommercialPersonal("LearningAssistant");

            using (var package = new ExcelPackage())
            {
                WriteSummarySheet(package, report, options);
                WriteMatrixSheet(package, report, options);
                package.SaveAs(new FileInfo(fullPath));
            }

            _logger?.LogInformation("阅读打卡表已导出 Excel：{Path}", fullPath);
            return fullPath;
        }

        private void WriteSummarySheet(
            ExcelPackage package,
            PdfReadingCheckInReport report,
            PdfReadingCheckInExportOptions options)
        {
            var worksheet = package.Workbook.Worksheets.Add("目录汇总");
            for (int index = 0; index < SummaryColumnWidths.Length; index++)
                worksheet.Column(index + 1).Width = SummaryColumnWidths[index];

            int row = 1;
            worksheet.Cells[row, 1].Value = "阅读打卡表 · 目录汇总";
            StyleTitleRow(worksheet, row, SummaryColumnCount);
            row++;

            worksheet.Cells[row, 1].Value = BuildMetaText(report, options);
            StyleMetaRow(worksheet, row, SummaryColumnCount);
            row++;

            int headerRow = row;
            WriteHeaderRow(worksheet, row, SummaryHeaders);
            row++;

            foreach (var folder in report.Folders)
            {
                WriteSummaryFolderRow(worksheet, row, folder, report, options);
                row++;

                foreach (var document in folder.Documents)
                {
                    WriteSummaryDocumentRow(worksheet, row, document);
                    row++;
                }
            }

            bool hasRows = report.Folders.Count > 0;
            if (!hasRows)
            {
                worksheet.Cells[row, 1].Value = report.RootUnavailable
                    ? "根目录不可用，请检查磁盘是否连接后再导出"
                    : "该月份暂无阅读记录";
                worksheet.Cells[row, 1, row, SummaryColumnCount].Merge = true;
                worksheet.Cells[row, 1, row, SummaryColumnCount].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1, row, SummaryColumnCount].Style.Font.Color.SetColor(SecondaryTextColor);
                worksheet.Cells[row, 1, row, SummaryColumnCount].Style.Font.Size = 10;
                worksheet.Row(row).Height = 20;
            }
            else
            {
                WriteSummaryTotalsRow(worksheet, row, report);
                row++;
            }

            int lastRow = Math.Max(headerRow + 1, row - 1);
            worksheet.Cells[headerRow, 1, lastRow, SummaryColumnCount].Style.Border.BorderAround(ExcelBorderStyle.Thin, GridLine);
            worksheet.Cells[1, 1, lastRow, SummaryColumnCount].Style.Font.Name = FontName;

            if (hasRows)
                worksheet.View.FreezePanes(headerRow + 1, 3); // 冻结标题两行 + 表头，并在横向滚动时保留路径与文档数列

            worksheet.PrinterSettings.PrintArea = worksheet.Cells[1, 1, lastRow, SummaryColumnCount];
            ApplyPrintSettings(worksheet);
        }

        private static void WriteSummaryFolderRow(
            ExcelWorksheet worksheet,
            int row,
            PdfCheckInFolderGroup folder,
            PdfReadingCheckInReport report,
            PdfReadingCheckInExportOptions options)
        {
            var range = worksheet.Cells[row, 1, row, SummaryColumnCount];
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(FolderFill);
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 9;
            worksheet.Row(row).Height = 18;

            worksheet.Cells[row, 1].Value = BuildFolderText(folder, includeCount: true, report, options.UseRelativePaths);
            worksheet.Cells[row, 2].Value = folder.DocumentCount;
            worksheet.Cells[row, 3].Value = folder.ReadDocumentCount;
            worksheet.Cells[row, 4].Value = FormatCheckInDays(folder.CheckInDays);
            worksheet.Cells[row, 5].Value = FormatDuration(folder.TotalSeconds);
            worksheet.Cells[row, 6].Value = FormatDay(folder.LastCheckInDay);
            worksheet.Cells[row, 7].Value = FormatStreak(folder.CurrentStreak);

            for (int column = 2; column <= SummaryColumnCount; column++)
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            ApplyRowBottomBorder(worksheet, row, SummaryColumnCount);
        }

        private static void WriteSummaryDocumentRow(
            ExcelWorksheet worksheet,
            int row,
            PdfCheckInDocumentRow document)
        {
            worksheet.Row(row).Height = 16;
            worksheet.Cells[row, 1].Value = document.FileName + GetDocumentSuffix(document);
            worksheet.Cells[row, 1].Style.Indent = 1;
            worksheet.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
            worksheet.Cells[row, 4].Value = FormatCheckInDays(document.CheckInDays);
            worksheet.Cells[row, 5].Value = FormatDuration(document.TotalSeconds);
            worksheet.Cells[row, 6].Value = FormatDay(document.LastCheckInDay);
            worksheet.Cells[row, 7].Value = FormatStreak(document.CurrentStreak);
            worksheet.Cells[row, 8].Value = FormatCoverage(document.Coverage);
            worksheet.Cells[row, 9].Value = FormatStatus(document.Status);

            for (int column = 2; column <= SummaryColumnCount; column++)
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            var range = worksheet.Cells[row, 1, row, SummaryColumnCount];
            range.Style.Font.Size = 9;
            if (!document.Exists)
                range.Style.Font.Color.SetColor(MissingTextColor);
            else if (document.RenamedMatched)
                worksheet.Cells[row, 1].Style.Font.Color.SetColor(RenamedTextColor);

            ApplyRowBottomBorder(worksheet, row, SummaryColumnCount);
        }

        private static void WriteSummaryTotalsRow(ExcelWorksheet worksheet, int row, PdfReadingCheckInReport report)
        {
            var totals = report.Totals;
            var range = worksheet.Cells[row, 1, row, SummaryColumnCount];
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 9;
            range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            range.Style.Border.Top.Color.SetColor(SecondaryTextColor);
            worksheet.Row(row).Height = 18;

            worksheet.Cells[row, 1].Value = "合计";
            worksheet.Cells[row, 2].Value = totals.DocumentCount;
            worksheet.Cells[row, 3].Value = totals.ReadDocumentCount;
            worksheet.Cells[row, 4].Value = FormatCheckInDays(totals.CheckInDays);
            worksheet.Cells[row, 5].Value = FormatDuration(totals.TotalSeconds);
            worksheet.Cells[row, 7].Value = FormatStreak(totals.OverallCurrentStreak);
            worksheet.Cells[row, 9].Value = totals.BestStreak > 0 ? $"最高连续 {totals.BestStreak} 天" : string.Empty;

            for (int column = 2; column <= SummaryColumnCount; column++)
                worksheet.Cells[row, column].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        }

        private void WriteMatrixSheet(
            ExcelPackage package,
            PdfReadingCheckInReport report,
            PdfReadingCheckInExportOptions options)
        {
            var columns = report.Layout.Columns;
            if (columns.Count == 0)
                return;

            var worksheet = package.Workbook.Worksheets.Add("打卡矩阵");
            int columnCount = MatrixFixedColumnCount + columns.Count;

            for (int index = 0; index < MatrixFixedColumnWidths.Length; index++)
                worksheet.Column(index + 1).Width = MatrixFixedColumnWidths[index];
            for (int index = 0; index < columns.Count; index++)
                worksheet.Column(MatrixFixedColumnCount + 1 + index).Width = MatrixDateColumnWidth;

            int row = 1;
            worksheet.Cells[row, 1].Value = "阅读打卡表 · 打卡矩阵（日期单元格为当日分钟数）";
            StyleTitleRow(worksheet, row, columnCount);
            row++;

            int headerRow = row;
            WriteHeaderRow(worksheet, row, MatrixFixedHeaders);
            WriteMatrixDateHeaders(worksheet, row, columns);
            worksheet.Row(row).Height = 32;
            row++;

            foreach (var folder in report.Folders)
            {
                WriteMatrixRow(worksheet, row, folder, null, columns, report, options);
                row++;

                foreach (var document in folder.Documents)
                {
                    WriteMatrixRow(worksheet, row, null, document, columns, report, options);
                    row++;
                }
            }

            bool hasRows = report.Folders.Count > 0;
            if (!hasRows)
            {
                worksheet.Cells[row, 1].Value = "该月份暂无阅读记录";
                worksheet.Cells[row, 1, row, columnCount].Merge = true;
                worksheet.Cells[row, 1, row, columnCount].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[row, 1, row, columnCount].Style.Font.Color.SetColor(SecondaryTextColor);
                worksheet.Row(row).Height = 20;
            }

            int lastRow = Math.Max(headerRow + 1, row - 1);
            worksheet.Cells[headerRow, 1, lastRow, columnCount].Style.Border.BorderAround(ExcelBorderStyle.Thin, GridLine);
            worksheet.Cells[1, 1, lastRow, columnCount].Style.Font.Name = FontName;

            if (hasRows)
                worksheet.View.FreezePanes(headerRow + 1, MatrixFixedColumnCount + 1); // 冻结表头与 5 个固定列

            worksheet.PrinterSettings.PrintArea = worksheet.Cells[1, 1, lastRow, columnCount];
            ApplyPrintSettings(worksheet);
        }

        private static void WriteMatrixDateHeaders(
            ExcelWorksheet worksheet,
            int headerRow,
            IReadOnlyList<PdfCheckInMatrixColumn> columns)
        {
            for (int index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                int sheetColumn = MatrixFixedColumnCount + 1 + index;
                var cell = worksheet.Cells[headerRow, sheetColumn];
                cell.Value = column.DayNumber + "\n" + column.WeekdayLabel;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 9;
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                cell.Style.WrapText = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(column.IsFuture ? FutureFill : HeaderFill);
                cell.Style.Font.Color.SetColor(column.IsFuture ? MissingTextColor : TextColor);
            }
        }

        /// <summary>写入一行矩阵数据：目录行（folder）与文档行（document）二者之一必填。</summary>
        private void WriteMatrixRow(
            ExcelWorksheet worksheet,
            int row,
            PdfCheckInFolderGroup? folder,
            PdfCheckInDocumentRow? document,
            IReadOnlyList<PdfCheckInMatrixColumn> columns,
            PdfReadingCheckInReport report,
            PdfReadingCheckInExportOptions options)
        {
            if (folder == null && document == null)
                return;

            worksheet.Row(row).Height = 18;
            var rowRange = worksheet.Cells[row, 1, row, MatrixFixedColumnCount + columns.Count];
            rowRange.Style.Font.Size = 9;
            rowRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;

            bool isFolder = folder != null;
            if (isFolder)
            {
                rowRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
                rowRange.Style.Fill.BackgroundColor.SetColor(FolderFill);
                rowRange.Style.Font.Bold = true;
                worksheet.Cells[row, 1].Value = BuildFolderText(folder!, includeCount: false, report, options.UseRelativePaths);
                worksheet.Cells[row, 2].Value = $"{folder!.DocumentCount} 本";
            }
            else
            {
                worksheet.Cells[row, 1].Value = string.Empty;
                worksheet.Cells[row, 2].Value = (document?.FileName ?? string.Empty) + GetDocumentSuffix(document);
                worksheet.Cells[row, 2].Style.Indent = 1;
                if (document != null && !document.Exists)
                    worksheet.Cells[row, 1, row, MatrixFixedColumnCount].Style.Font.Color.SetColor(MissingTextColor);
            }

            int checkInDays = isFolder ? folder!.CheckInDays : document!.CheckInDays;
            int totalSeconds = isFolder ? folder!.TotalSeconds : document!.TotalSeconds;
            int streak = isFolder ? folder!.CurrentStreak : document!.CurrentStreak;

            worksheet.Cells[row, 3].Value = FormatCheckInDays(checkInDays);
            worksheet.Cells[row, 4].Value = FormatDuration(totalSeconds);
            worksheet.Cells[row, 5].Value = FormatStreak(streak);

            for (int column = 1; column <= MatrixFixedColumnCount; column++)
                worksheet.Cells[row, column].Style.HorizontalAlignment = column <= 2
                    ? ExcelHorizontalAlignment.Left
                    : ExcelHorizontalAlignment.Center;

            // 日期单元格：先逐格写值，再按连续区间批量底色（未来日期 / 未达标 / 分档）
            var fills = new string[columns.Count];
            for (int index = 0; index < columns.Count; index++)
            {
                var column = columns[index];
                int sheetColumn = MatrixFixedColumnCount + 1 + index;
                var cell = worksheet.Cells[row, sheetColumn];
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                if (column.IsFuture)
                {
                    fills[index] = ColorHex(FutureFill);
                    continue;
                }

                int seconds = GetDaySeconds(folder, document, column.Day);
                if (seconds <= 0)
                {
                    fills[index] = isFolder ? ColorHex(FolderFill) : string.Empty;
                    continue;
                }

                bool checkedIn = seconds >= report.MinSeconds;
                cell.Value = FormatMinuteCell(seconds);
                cell.Style.Font.Color.SetColor(checkedIn ? TextColor : SecondaryTextColor);

                var bucket = checkedIn ? PdfCheckInLayoutRules.FindBucket(seconds) : null;
                fills[index] = checkedIn
                    ? bucket?.BackgroundColor ?? ColorHex(HeaderFill)
                    : ColorHex(UnderThresholdFill);
            }

            FillContiguousRuns(worksheet, row, MatrixFixedColumnCount + 1, fills);
            ApplyRowBottomBorder(worksheet, row, MatrixFixedColumnCount + columns.Count);
        }

        /// <summary>按连续同色区间批量设置填充色，跨列不连续时拆成多个区域。</summary>
        private static void FillContiguousRuns(
            ExcelWorksheet worksheet,
            int row,
            int firstColumn,
            IReadOnlyList<string> fillColors)
        {
            int runStart = -1;
            string runColor = string.Empty;

            void Flush(int endIndex)
            {
                if (runStart < 0 || endIndex < runStart)
                    return;

                var range = worksheet.Cells[row, firstColumn + runStart, row, firstColumn + endIndex];
                range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                range.Style.Fill.BackgroundColor.SetColor(ColorTranslator.FromHtml(runColor));
                runStart = -1;
            }

            for (int index = 0; index <= fillColors.Count; index++)
            {
                string color = index < fillColors.Count ? fillColors[index] ?? string.Empty : string.Empty;
                if (runStart >= 0 && !string.Equals(color, runColor, StringComparison.OrdinalIgnoreCase))
                    Flush(index - 1);

                if (runStart < 0 && color.Length > 0)
                {
                    runStart = index;
                    runColor = color;
                }
            }
        }

        private static int GetDaySeconds(
            PdfCheckInFolderGroup? folder,
            PdfCheckInDocumentRow? document,
            DateTime day)
        {
            if (document != null)
                return document.DaySeconds.TryGetValue(day, out int seconds) ? seconds : 0;

            if (folder == null)
                return 0;

            int total = 0;
            foreach (var item in folder.Documents)
            {
                if (item.DaySeconds.TryGetValue(day, out int seconds))
                    total += seconds;
            }

            return total;
        }

        private static void WriteHeaderRow(ExcelWorksheet worksheet, int row, IEnumerable<string> headers)
        {
            int column = 1;
            foreach (string header in headers)
            {
                var cell = worksheet.Cells[row, column];
                cell.Value = header;
                cell.Style.Font.Bold = true;
                cell.Style.Font.Size = 9;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(HeaderFill);
                cell.Style.HorizontalAlignment = column == 1 ? ExcelHorizontalAlignment.Left : ExcelHorizontalAlignment.Center;
                cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                column++;
            }

            worksheet.Row(row).Height = 22;
        }

        private static void StyleTitleRow(ExcelWorksheet worksheet, int row, int columnCount)
        {
            var range = worksheet.Cells[row, 1, row, columnCount];
            range.Style.Font.Bold = true;
            range.Style.Font.Size = 14;
            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
            range.Style.Fill.BackgroundColor.SetColor(HeaderFill);
            range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            worksheet.Row(row).Height = 26;
        }

        private static void StyleMetaRow(ExcelWorksheet worksheet, int row, int columnCount)
        {
            var range = worksheet.Cells[row, 1, row, columnCount];
            range.Style.Font.Size = 9;
            range.Style.Font.Color.SetColor(SecondaryTextColor);
            worksheet.Row(row).Height = 18;
        }

        private static void ApplyRowBottomBorder(ExcelWorksheet worksheet, int row, int columnCount)
        {
            var range = worksheet.Cells[row, 1, row, columnCount];
            range.Style.Border.Bottom.Style = ExcelBorderStyle.Hair;
            range.Style.Border.Bottom.Color.SetColor(GridLine);
        }

        private static void ApplyPrintSettings(ExcelWorksheet worksheet)
        {
            var settings = worksheet.PrinterSettings;
            settings.PaperSize = ePaperSize.A4;
            settings.Orientation = eOrientation.Landscape;
            settings.FitToPage = true;
            settings.FitToWidth = 1;
            settings.FitToHeight = 0;
            settings.HorizontalCentered = true;
            settings.ShowGridLines = false;
            settings.LeftMargin = 0.4;
            settings.RightMargin = 0.4;
            settings.TopMargin = 0.4;
            settings.BottomMargin = 0.4;
        }

        #endregion

        #region HTML

        /// <summary>导出自包含、离线可打印的 HTML 报表。</summary>
        public string ExportHtml(
            PdfReadingCheckInReport report,
            string outputPath,
            PdfReadingCheckInExportOptions? options = null)
        {
            if (report == null)
                throw new ArgumentNullException(nameof(report));

            options ??= new PdfReadingCheckInExportOptions();
            string fullPath = EnsureOutputPath(outputPath, ".html");

            var htmlOptions = new PdfReadingCheckInHtmlOptions
            {
                UseRelativePaths = options.UseRelativePaths,
                GeneratedAt = options.GeneratedAt
            };

            string html = PdfReadingCheckInHtmlBuilder.Build(report, htmlOptions);
            File.WriteAllText(fullPath, html, new UTF8Encoding(false));
            _logger?.LogInformation("阅读打卡表已导出 HTML：{Path}", fullPath);
            return fullPath;
        }

        #endregion

        #region 打印

        /// <summary>创建布局模型驱动的打印文档（页数在打印前即确定）。</summary>
        public PdfReadingCheckInPrintDocument CreatePrintDocument(
            PdfReadingCheckInReport report,
            PdfReadingCheckInPrintOptions? options = null)
            => new(report, options ?? new PdfReadingCheckInPrintOptions());

        #endregion

        #region 格式化

        private static string BuildMetaText(PdfReadingCheckInReport report, PdfReadingCheckInExportOptions options)
        {
            var totals = report.Totals;
            string period = report.PeriodStart == report.PeriodEnd
                ? report.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{report.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} ~ {report.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";
            string root = GetRootDisplay(report.RootPath, options.UseRelativePaths);

            var parts = new List<string>
            {
                $"根目录：{root}",
                $"统计区间：{period}",
                $"有效打卡阈值：{report.MinSeconds} 秒",
                $"总体连续：{FormatStreak(totals.OverallCurrentStreak)}",
                $"目录：{totals.FolderCount} 个",
                $"PDF：{totals.DocumentCount} 份",
                $"打卡：{totals.CheckInDays} 天",
                $"累计：{FormatDuration(totals.TotalSeconds)}"
            };

            if (report.RootUnavailable)
                parts.Insert(0, "根目录不可用");
            if (options.GeneratedAt.HasValue)
                parts.Add($"生成于 {options.GeneratedAt.Value.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}");

            return string.Join(" · ", parts);
        }

        private static string EnsureOutputPath(string outputPath, string extension)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("导出路径不能为空", nameof(outputPath));

            string fullPath = Path.GetFullPath(outputPath.Trim());
            if (!Path.HasExtension(fullPath))
                fullPath += extension;

            string? directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return fullPath;
        }

        #endregion
    }

    /// <summary>
    /// 打卡表分页打印：按 <see cref="PdfCheckInLayoutModel.Pages"/> 的“页 -> 行”计划绘制，
    /// A4 横向、10mm 边距，页数在打印前即确定；每页重复标题区与表头。
    /// </summary>
    public sealed class PdfReadingCheckInPrintDocument : IDisposable
    {
        private const float HundredthInchToMillimeter = 0.254f;

        private const float TitleHeightMm = 7f;
        private const float MetaHeightMm = 5f;
        private const float HeaderHeightMm = 7.5f;
        private const float FooterHeightMm = 5f;
        private const float CellPaddingMm = 1f;

        private static readonly float[] SummaryColumnWidthsMm = { 92, 18, 16, 22, 30, 24, 18, 20, 37 };
        private static readonly float[] MatrixFixedColumnWidthsMm = { 30, 28, 10, 12, 10 };

        private readonly PdfReadingCheckInReport _report;
        private readonly PdfReadingCheckInPrintOptions _options;
        private readonly IReadOnlyList<PdfCheckInPagePlan> _pages;
        private readonly Dictionary<string, PdfCheckInFolderGroup> _folderLookup;
        private readonly Dictionary<string, PdfCheckInDocumentRow> _documentLookup;

        private readonly Font _titleFont = new(FontName, 4.2f, FontStyle.Bold, GraphicsUnit.Millimeter);
        private readonly Font _metaFont = new(FontName, 2.8f, FontStyle.Regular, GraphicsUnit.Millimeter);
        private readonly Font _headerFont = new(FontName, 2.5f, FontStyle.Bold, GraphicsUnit.Millimeter);
        private readonly Font _cellFont = new(FontName, 2.3f, FontStyle.Regular, GraphicsUnit.Millimeter);
        private readonly Font _boldCellFont = new(FontName, 2.3f, FontStyle.Bold, GraphicsUnit.Millimeter);
        private readonly Font _smallFont = new(FontName, 2.0f, FontStyle.Regular, GraphicsUnit.Millimeter);
        private readonly Font _footerFont = new(FontName, 2.0f, FontStyle.Regular, GraphicsUnit.Millimeter);

        private int _pageIndex;
        private bool _disposed;

        internal PdfReadingCheckInPrintDocument(PdfReadingCheckInReport report, PdfReadingCheckInPrintOptions options)
        {
            _report = report ?? throw new ArgumentNullException(nameof(report));
            _options = options ?? new PdfReadingCheckInPrintOptions();

            _pages = SelectPages(report.Layout.Pages, _options.Scope);

            _folderLookup = new Dictionary<string, PdfCheckInFolderGroup>(StringComparer.OrdinalIgnoreCase);
            _documentLookup = new Dictionary<string, PdfCheckInDocumentRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var folder in report.Folders)
            {
                if (!_folderLookup.ContainsKey(folder.RelativePath))
                    _folderLookup[folder.RelativePath] = folder;

                foreach (var document in folder.Documents)
                {
                    if (!_documentLookup.ContainsKey(document.FullPath))
                        _documentLookup[document.FullPath] = document;
                }
            }

            Document = new PrintDocument { DocumentName = "阅读打卡表" };
            Document.BeginPrint += (_, _) => _pageIndex = 0;
            Document.PrintPage += OnPrintPage;
            ApplyPageSettings(Document);
        }

        /// <summary>底层打印文档；用于打印预览或直接打印。</summary>
        public PrintDocument Document { get; }

        /// <summary>按当前范围过滤后的总页数。</summary>
        public int PageCount => _pages.Count;

        private static IReadOnlyList<PdfCheckInPagePlan> SelectPages(
            IReadOnlyList<PdfCheckInPagePlan> pages,
            PdfReadingCheckInPrintScope scope)
        {
            return scope switch
            {
                PdfReadingCheckInPrintScope.SummaryOnly => pages.Where(page => page.Kind == PdfCheckInPageKind.Summary).ToList(),
                PdfReadingCheckInPrintScope.MatrixOnly => pages.Where(page => page.Kind == PdfCheckInPageKind.Matrix).ToList(),
                _ => pages
            };
        }

        private static void ApplyPageSettings(PrintDocument document)
        {
            var settings = document.DefaultPageSettings;
            settings.Landscape = true;
            settings.Margins = new Margins(39, 39, 39, 39); // 10mm ≈ 0.39in

            try
            {
                foreach (PaperSize size in document.PrinterSettings.PaperSizes)
                {
                    if (size.Kind == PaperKind.A4)
                    {
                        settings.PaperSize = size;
                        break;
                    }
                }
            }
            catch (InvalidPrinterException)
            {
                // 未安装打印机时保持默认纸张，预览仍可打开
            }
            catch (Exception)
            {
                // 部分驱动枚举纸张会抛异常，忽略即可
            }
        }

        private void OnPrintPage(object? sender, PrintPageEventArgs e)
        {
            e.HasMorePages = false;
            if (e.Graphics == null || _pageIndex < 0 || _pageIndex >= _pages.Count)
                return;

            var plan = _pages[_pageIndex];
            var graphics = e.Graphics;
            graphics.PageUnit = GraphicsUnit.Millimeter;

            var area = GetDrawableArea(e);
            float left = area.Left;
            float top = area.Top;
            float width = area.Width;
            float height = area.Height;
            float bottom = top + height;

            float contentTop = DrawPageHeader(graphics, plan, left, top, width);

            if (plan.Kind == PdfCheckInPageKind.Summary)
                DrawSummaryTable(graphics, plan, left, contentTop, width, bottom);
            else
                DrawMatrixTable(graphics, plan, left, contentTop, width, bottom);

            DrawPageFooter(graphics, left, bottom - FooterHeightMm, width);

            _pageIndex++;
            if (_pageIndex < _pages.Count)
                e.HasMorePages = true;
        }

        private static RectangleF GetDrawableArea(PrintPageEventArgs e)
        {
            var margin = e.MarginBounds;
            var area = new RectangleF(
                margin.Left * HundredthInchToMillimeter,
                margin.Top * HundredthInchToMillimeter,
                margin.Width * HundredthInchToMillimeter,
                margin.Height * HundredthInchToMillimeter);

            // 部分驱动返回无效边界时回退 A4 横向可用区域
            if (area.Width < 150 || area.Height < 80)
            {
                return new RectangleF(
                    10,
                    10,
                    PdfCheckInLayoutRules.A4LandscapeUsableWidthMm,
                    PdfCheckInLayoutRules.A4LandscapeUsableHeightMm);
            }

            return area;
        }

        private float DrawPageHeader(Graphics graphics, PdfCheckInPagePlan plan, float left, float top, float width)
        {
            DrawCellText(
                graphics,
                $"阅读打卡表 · {plan.Title}",
                _titleFont,
                TextColor,
                new RectangleF(left, top, width, TitleHeightMm),
                StringAlignment.Near);

            DrawCellText(
                graphics,
                BuildPageMetaText(plan),
                _metaFont,
                SecondaryTextColor,
                new RectangleF(left, top + TitleHeightMm, width, MetaHeightMm),
                StringAlignment.Near);

            float lineY = top + TitleHeightMm + MetaHeightMm + 1f;
            using (var pen = new Pen(GridLine, 0.25f))
                graphics.DrawLine(pen, left, lineY, left + width, lineY);

            return lineY + 1.5f;
        }

        private string BuildPageMetaText(PdfCheckInPagePlan plan)
        {
            string period = _report.PeriodStart == _report.PeriodEnd
                ? _report.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : $"{_report.PeriodStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} ~ {_report.PeriodEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

            var parts = new List<string>
            {
                $"根目录：{GetRootDisplay(_report.RootPath, _options.UseRelativePaths)}",
                $"统计区间：{period}",
                $"有效打卡阈值：{_report.MinSeconds} 秒"
            };

            if (_report.RootUnavailable)
                parts.Add("根目录不可用");
            if (plan.Kind == PdfCheckInPageKind.Matrix
                && _report.Layout.SegmentMode == PdfCheckInPrintSegmentMode.TwoSegments)
            {
                parts.Add("31 列过窄，已自动按 1-15 / 16-31 两段打印");
            }

            return string.Join(" · ", parts);
        }

        private void DrawPageFooter(Graphics graphics, float left, float top, float width)
        {
            string footer =
                $"第 {_pageIndex + 1} 页 / 共 {_pages.Count} 页 · " +
                $"生成于 {_options.GeneratedAt:yyyy-MM-dd HH:mm} · 仅统计阅读器前台停留时间，图片不参与";
            DrawCellText(
                graphics,
                footer,
                _footerFont,
                SecondaryTextColor,
                new RectangleF(left, top, width, FooterHeightMm),
                StringAlignment.Far);
        }

        private void DrawSummaryTable(
            Graphics graphics,
            PdfCheckInPagePlan plan,
            float left,
            float contentTop,
            float width,
            float bottom)
        {
            float scale = width / PdfCheckInLayoutRules.A4LandscapeUsableWidthMm;
            float[] widths = new float[SummaryColumnWidthsMm.Length];
            for (int index = 0; index < widths.Length; index++)
                widths[index] = SummaryColumnWidthsMm[index] * scale;

            float headerTop = contentTop;
            DrawHeaderCells(graphics, SummaryHeaders, widths, left, headerTop, HeaderHeightMm);

            float rowHeight = PdfCheckInLayoutRules.DefaultRowHeightMm;
            float rowsTop = headerTop + HeaderHeightMm;
            float footerTop = bottom - FooterHeightMm - 1f;
            int capacity = Math.Max(0, (int)((footerTop - rowsTop) / rowHeight));
            int rowCount = Math.Min(capacity, plan.Rows.Count);
            float rowTop = rowsTop;

            for (int index = 0; index < rowCount; index++)
            {
                DrawSummaryRow(graphics, plan.Rows[index], left, rowTop, widths, rowHeight);
                rowTop += rowHeight;
            }

            DrawTableGrid(graphics, left, headerTop, widths, HeaderHeightMm, rowHeight, rowCount);
        }

        private void DrawSummaryRow(
            Graphics graphics,
            PdfCheckInPageRow row,
            float left,
            float top,
            float[] widths,
            float height)
        {
            _folderLookup.TryGetValue(row.FolderRelativePath ?? string.Empty, out var folder);
            var document = row.DocumentFullPath != null && _documentLookup.TryGetValue(row.DocumentFullPath, out var found)
                ? found
                : null;

            var values = new string[widths.Length];
            Color textColor = TextColor;
            Font font = _cellFont;

            if (row.IsFolderRow && folder != null)
            {
                using var brush = new SolidBrush(FolderFill);
                graphics.FillRectangle(brush, left, top, Sum(widths), height);

                values[0] = BuildFolderText(folder, includeCount: true, _report, _options.UseRelativePaths);
                values[1] = folder.DocumentCount.ToString(CultureInfo.InvariantCulture);
                values[2] = folder.ReadDocumentCount.ToString(CultureInfo.InvariantCulture);
                values[3] = FormatCheckInDays(folder.CheckInDays);
                values[4] = FormatDuration(folder.TotalSeconds);
                values[5] = FormatDay(folder.LastCheckInDay);
                values[6] = FormatStreak(folder.CurrentStreak);
                font = _boldCellFont;
            }
            else if (document != null)
            {
                values[0] = "  " + document.FileName + GetDocumentSuffix(document);
                values[3] = FormatCheckInDays(document.CheckInDays);
                values[4] = FormatDuration(document.TotalSeconds);
                values[5] = FormatDay(document.LastCheckInDay);
                values[6] = FormatStreak(document.CurrentStreak);
                values[7] = FormatCoverage(document.Coverage);
                values[8] = FormatStatus(document.Status);

                if (!document.Exists)
                    textColor = MissingTextColor;
                else if (document.RenamedMatched)
                    textColor = RenamedTextColor;
            }
            else
            {
                values[0] = row.IsFolderRow ? row.FolderDisplayPath : row.DocumentFileName ?? string.Empty;
            }

            float x = left;
            for (int column = 0; column < widths.Length; column++)
            {
                DrawCellText(
                    graphics,
                    values[column],
                    font,
                    textColor,
                    new RectangleF(x + CellPaddingMm, top, widths[column] - CellPaddingMm * 2, height),
                    column == 0 ? StringAlignment.Near : StringAlignment.Center);
                x += widths[column];
            }
        }

        private void DrawMatrixTable(
            Graphics graphics,
            PdfCheckInPagePlan plan,
            float left,
            float contentTop,
            float width,
            float bottom)
        {
            var columns = GetSegmentColumns(plan.SegmentLabel);
            if (columns.Count == 0)
                return;

            float scale = width / PdfCheckInLayoutRules.A4LandscapeUsableWidthMm;
            float fixedWidth = 0f;
            float[] fixedWidths = new float[MatrixFixedColumnWidthsMm.Length];
            for (int index = 0; index < fixedWidths.Length; index++)
            {
                fixedWidths[index] = MatrixFixedColumnWidthsMm[index] * scale;
                fixedWidth += fixedWidths[index];
            }

            float dateWidth = (width - fixedWidth) / columns.Count;

            float headerTop = contentTop;
            DrawMatrixHeaderCells(graphics, fixedWidths, columns, left, headerTop, HeaderHeightMm, dateWidth);

            float rowHeight = PdfCheckInLayoutRules.DefaultRowHeightMm;
            float rowsTop = headerTop + HeaderHeightMm;
            float footerTop = bottom - FooterHeightMm - 1f;
            int capacity = Math.Max(0, (int)((footerTop - rowsTop) / rowHeight));
            int rowCount = Math.Min(capacity, plan.Rows.Count);
            float rowTop = rowsTop;

            for (int index = 0; index < rowCount; index++)
            {
                DrawMatrixRow(graphics, plan.Rows[index], left, rowTop, fixedWidths, columns, dateWidth, rowHeight);
                rowTop += rowHeight;
            }

            var allWidths = new List<float>(fixedWidths);
            allWidths.AddRange(Enumerable.Repeat(dateWidth, columns.Count));
            DrawTableGrid(graphics, left, headerTop, allWidths.ToArray(), HeaderHeightMm, rowHeight, rowCount);
        }

        private IReadOnlyList<PdfCheckInMatrixColumn> GetSegmentColumns(string segmentLabel)
        {
            var columns = _report.Layout.Columns;
            if (segmentLabel.Length == 0)
                return columns;

            bool second = string.Equals(segmentLabel, "16-31", StringComparison.Ordinal);
            return columns.Where(column => column.InSecondSegment == second).ToList();
        }

        private void DrawMatrixHeaderCells(
            Graphics graphics,
            float[] fixedWidths,
            IReadOnlyList<PdfCheckInMatrixColumn> columns,
            float left,
            float top,
            float height,
            float dateWidth)
        {
            float totalWidth = Sum(fixedWidths) + dateWidth * columns.Count;
            using (var brush = new SolidBrush(HeaderFill))
                graphics.FillRectangle(brush, left, top, totalWidth, height);

            float x = left;
            for (int index = 0; index < fixedWidths.Length; index++)
            {
                DrawCellText(
                    graphics,
                    MatrixFixedHeaders[index],
                    _headerFont,
                    TextColor,
                    new RectangleF(x + CellPaddingMm, top, fixedWidths[index] - CellPaddingMm * 2, height),
                    index <= 1 ? StringAlignment.Near : StringAlignment.Center);
                x += fixedWidths[index];
            }

            foreach (var column in columns)
            {
                if (column.IsFuture)
                {
                    using var brush = new SolidBrush(FutureFill);
                    graphics.FillRectangle(brush, x, top, dateWidth, height);
                }

                var topRect = new RectangleF(x, top, dateWidth, height / 2);
                var bottomRect = new RectangleF(x, top + height / 2, dateWidth, height / 2 - 0.5f);
                DrawCellText(
                    graphics,
                    column.DayNumber.ToString(CultureInfo.InvariantCulture),
                    _headerFont,
                    column.IsFuture ? MissingTextColor : TextColor,
                    topRect,
                    StringAlignment.Center);
                DrawCellText(
                    graphics,
                    column.WeekdayLabel,
                    _smallFont,
                    column.IsWeekend ? SecondaryTextColor : MissingTextColor,
                    bottomRect,
                    StringAlignment.Center);
                x += dateWidth;
            }
        }

        private void DrawMatrixRow(
            Graphics graphics,
            PdfCheckInPageRow row,
            float left,
            float top,
            float[] fixedWidths,
            IReadOnlyList<PdfCheckInMatrixColumn> columns,
            float dateWidth,
            float height)
        {
            _folderLookup.TryGetValue(row.FolderRelativePath ?? string.Empty, out var folder);
            var document = row.DocumentFullPath != null && _documentLookup.TryGetValue(row.DocumentFullPath, out var found)
                ? found
                : null;

            bool isFolder = row.IsFolderRow && folder != null;
            float totalWidth = Sum(fixedWidths) + dateWidth * columns.Count;
            if (isFolder)
            {
                using var brush = new SolidBrush(FolderFill);
                graphics.FillRectangle(brush, left, top, totalWidth, height);
            }

            var values = new string[fixedWidths.Length];
            Font font = isFolder ? _boldCellFont : _cellFont;
            Color textColor = TextColor;

            if (isFolder)
            {
                values[0] = BuildFolderText(folder!, includeCount: false, _report, _options.UseRelativePaths);
                values[1] = $"{folder!.DocumentCount} 本";
                values[2] = FormatCheckInDays(folder.CheckInDays);
                values[3] = FormatDuration(folder.TotalSeconds);
                values[4] = FormatStreak(folder.CurrentStreak);
            }
            else if (document != null)
            {
                values[0] = string.Empty;
                values[1] = "  " + document.FileName + GetDocumentSuffix(document);
                values[2] = FormatCheckInDays(document.CheckInDays);
                values[3] = FormatDuration(document.TotalSeconds);
                values[4] = FormatStreak(document.CurrentStreak);
                if (!document.Exists)
                    textColor = MissingTextColor;
                else if (document.RenamedMatched)
                    textColor = RenamedTextColor;
            }
            else
            {
                values[0] = row.FolderDisplayPath;
                values[1] = row.DocumentFileName ?? string.Empty;
            }

            float x = left;
            for (int index = 0; index < fixedWidths.Length; index++)
            {
                DrawCellText(
                    graphics,
                    values[index],
                    font,
                    textColor,
                    new RectangleF(x + CellPaddingMm, top, fixedWidths[index] - CellPaddingMm * 2, height),
                    index <= 1 ? StringAlignment.Near : StringAlignment.Center);
                x += fixedWidths[index];
            }

            foreach (var column in columns)
            {
                DrawMatrixDayCell(graphics, row, folder, document, column, x, top, dateWidth, height);
                x += dateWidth;
            }
        }

        private void DrawMatrixDayCell(
            Graphics graphics,
            PdfCheckInPageRow row,
            PdfCheckInFolderGroup? folder,
            PdfCheckInDocumentRow? document,
            PdfCheckInMatrixColumn column,
            float left,
            float top,
            float width,
            float height)
        {
            var bounds = new RectangleF(left, top, width, height);

            if (column.IsFuture)
            {
                using var brush = new SolidBrush(FutureFill);
                graphics.FillRectangle(brush, bounds);
                return;
            }

            int seconds = GetDaySeconds(folder, document, column.Day);
            if (seconds <= 0)
                return;

            bool checkedIn = seconds >= _report.MinSeconds;
            Color background = checkedIn
                ? GetBucketColor(seconds)
                : row.IsFolderRow ? FolderFill : Color.White;

            using (var brush = new SolidBrush(background))
                graphics.FillRectangle(brush, bounds);

            Color textColor;
            if (!checkedIn)
                textColor = document != null && !document.Exists ? MissingTextColor : SecondaryTextColor;
            else
                textColor = TextColor;

            string text = _options.DisplayMode == PdfCheckInCellDisplayMode.Minutes
                ? FormatMinuteCell(seconds)
                : checkedIn ? "✓" : "·";

            DrawCellText(graphics, text, _cellFont, textColor, bounds, StringAlignment.Center);
        }

        private static int GetDaySeconds(
            PdfCheckInFolderGroup? folder,
            PdfCheckInDocumentRow? document,
            DateTime day)
        {
            if (document != null)
                return document.DaySeconds.TryGetValue(day, out int seconds) ? seconds : 0;

            if (folder == null)
                return 0;

            int total = 0;
            foreach (var item in folder.Documents)
            {
                if (item.DaySeconds.TryGetValue(day, out int seconds))
                    total += seconds;
            }

            return total;
        }

        private Color GetBucketColor(int seconds)
        {
            var bucket = PdfCheckInLayoutRules.FindBucket(seconds);
            if (bucket == null || string.IsNullOrEmpty(bucket.BackgroundColor))
                return HeaderFill;

            try
            {
                return ColorTranslator.FromHtml(bucket.BackgroundColor);
            }
            catch (Exception)
            {
                return HeaderFill;
            }
        }

        private void DrawHeaderCells(
            Graphics graphics,
            string[] headers,
            float[] widths,
            float left,
            float top,
            float height)
        {
            float totalWidth = Sum(widths);
            using (var brush = new SolidBrush(HeaderFill))
                graphics.FillRectangle(brush, left, top, totalWidth, height);

            float x = left;
            for (int index = 0; index < headers.Length; index++)
            {
                DrawCellText(
                    graphics,
                    headers[index],
                    _headerFont,
                    TextColor,
                    new RectangleF(x + CellPaddingMm, top, widths[index] - CellPaddingMm * 2, height),
                    index == 0 ? StringAlignment.Near : StringAlignment.Center);
                x += widths[index];
            }
        }

        private static void DrawTableGrid(
            Graphics graphics,
            float left,
            float headerTop,
            float[] widths,
            float headerHeight,
            float rowHeight,
            int rowCount)
        {
            float tableBottom = headerTop + headerHeight + rowHeight * rowCount;
            using var pen = new Pen(GridLine, 0.2f);

            float x = left;
            foreach (float width in widths)
            {
                graphics.DrawLine(pen, x, headerTop, x, tableBottom);
                x += width;
            }

            graphics.DrawLine(pen, x, headerTop, x, tableBottom); // 最右竖线
            graphics.DrawLine(pen, left, headerTop, x, headerTop);
            graphics.DrawLine(pen, left, headerTop + headerHeight, x, headerTop + headerHeight);

            for (int index = 1; index <= rowCount; index++)
            {
                float y = headerTop + headerHeight + rowHeight * index;
                graphics.DrawLine(pen, left, y, x, y);
            }
        }

        private static void DrawCellText(
            Graphics graphics,
            string? text,
            Font font,
            Color color,
            RectangleF bounds,
            StringAlignment alignment)
        {
            if (string.IsNullOrEmpty(text) || bounds.Width <= 0 || bounds.Height <= 0)
                return;

            using var format = new StringFormat
            {
                Alignment = alignment,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.LineLimit
            };
            using var brush = new SolidBrush(color);
            graphics.DrawString(text, font, brush, bounds, format);
        }

        private static float Sum(float[] values)
        {
            float total = 0f;
            foreach (float value in values)
                total += value;
            return total;
        }

        private static float Sum(IReadOnlyList<float> values)
        {
            float total = 0f;
            foreach (float value in values)
                total += value;
            return total;
        }

        /// <summary>释放字体与打印文档。</summary>
        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            Document.Dispose();
            _titleFont.Dispose();
            _metaFont.Dispose();
            _headerFont.Dispose();
            _cellFont.Dispose();
            _boldCellFont.Dispose();
            _smallFont.Dispose();
            _footerFont.Dispose();
        }
    }
}
