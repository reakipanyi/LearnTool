using System.IO;
using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using OfficeOpenXml;
using Xunit;

namespace LearningAssistant.Tests.Services.Pdf
{
    /// <summary>
    /// 阅读打卡表导出服务测试：Excel 双工作表内容、未来日期填色、文件名清洗与打印分页。
    /// </summary>
    public sealed class PdfReadingCheckInExportServiceTests : IDisposable
    {
        private static readonly DateTime PeriodStart = new(2026, 1, 1);
        private static readonly DateTime PeriodEnd = new(2026, 1, 31);

        private readonly string _directory;

        public PdfReadingCheckInExportServiceTests()
        {
            ExcelPackage.License.SetNonCommercialPersonal("LearningAssistant");
            _directory = Path.Combine(Path.GetTempPath(), $"pdf_checkin_export_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Fact]
        public void 导出Excel_应生成目录汇总与打卡矩阵双工作表()
        {
            var report = BuildReport(PeriodEnd);
            var outputPath = Path.Combine(_directory, "report.xlsx");

            var fullPath = new PdfReadingCheckInExportService()
                .ExportExcel(report, outputPath, new PdfReadingCheckInExportOptions { UseRelativePaths = true });

            fullPath.Should().Be(Path.GetFullPath(outputPath));
            File.Exists(fullPath).Should().BeTrue();

            using var package = new ExcelPackage(new FileInfo(fullPath));
            package.Workbook.Worksheets.Should().HaveCount(2);

            var summary = package.Workbook.Worksheets["目录汇总"];
            var matrix = package.Workbook.Worksheets["打卡矩阵"];
            summary.Should().NotBeNull();
            matrix.Should().NotBeNull();

            summary!.Cells[1, 1].Value.Should().Be("阅读打卡表 · 目录汇总");
            summary.Cells[2, 1].Text.Should().Contain("根目录：");
            summary.Cells[3, 1].Value.Should().Be("目录 / 文档");

            // 目录行（整行加粗底色）与文档行（缩进）
            summary.Cells[4, 1].Value.Should().Be("（根目录）（2 本）");
            summary.Cells[4, 4].Value.Should().Be("1 天");
            summary.Cells[5, 1].Value.Should().Be("book-1.pdf");
            summary.Cells[5, 4].Value.Should().Be("1 天");
            summary.Cells[5, 8].Value.Should().Be("50%");
            summary.Cells[5, 9].Value.Should().Be("已打卡");
            summary.Cells[6, 1].Value.Should().Be("book-2.pdf（已失效）");

            // A4 横向、按宽度缩放为一页
            summary.PrinterSettings.FitToPage.Should().BeTrue();
            summary.PrinterSettings.FitToWidth.Should().Be(1);
            summary.PrinterSettings.Orientation.Should().Be(eOrientation.Landscape);

            matrix!.Cells[1, 1].Value.Should().Be("阅读打卡表 · 打卡矩阵（日期单元格为当日分钟数）");
            matrix.Cells[2, 1].Value.Should().Be("目录");
            matrix.Cells[2, 5].Value.Should().Be("连续");

            // 日期表头为“日号 + 换行 + 星期”
            var firstColumn = report.Layout.Columns[0];
            matrix.Cells[2, 6].Value.Should().Be($"{firstColumn.DayNumber}\n{firstColumn.WeekdayLabel}");

            // 矩阵行：目录行 + 两个文档行；1 月 1 日 120 秒显示为 2 分钟
            matrix.Cells[3, 1].Value.Should().Be("（根目录）");
            matrix.Cells[3, 2].Value.Should().Be("2 本");
            matrix.Cells[4, 2].Value.Should().Be("book-1.pdf");
            matrix.Cells[4, 6].Value.Should().Be("2");
        }

        [Fact]
        public void 导出Excel_未来日期使用灰色底色且不写分钟数()
        {
            // 以 1 月 10 日为“今天”，11 日之后的列属于未来
            var report = BuildReport(new DateTime(2026, 1, 10));
            var outputPath = Path.Combine(_directory, "future.xlsx");

            new PdfReadingCheckInExportService()
                .ExportExcel(report, outputPath, new PdfReadingCheckInExportOptions());

            using var package = new ExcelPackage(new FileInfo(outputPath));
            var matrix = package.Workbook.Worksheets["打卡矩阵"];

            int firstFutureIndex = report.Layout.Columns
                .Select((column, index) => (column, index))
                .First(item => item.column.IsFuture).index;
            int sheetColumn = 6 + firstFutureIndex;

            var futureCell = matrix.Cells[4, sheetColumn];
            futureCell.Value.Should().BeNull();
            futureCell.Style.Fill.BackgroundColor.Rgb.Should().EndWith("EEEEEE");
        }

        [Fact]
        public void 导出Html_应写入指标条与横向分页打印样式()
        {
            var report = BuildReport(PeriodEnd);
            var outputPath = Path.Combine(_directory, "report.html");

            var fullPath = new PdfReadingCheckInExportService()
                .ExportHtml(report, outputPath, new PdfReadingCheckInExportOptions { UseRelativePaths = true });

            string html = File.ReadAllText(fullPath);
            html.Should().Contain("阅读打卡表");
            html.Should().Contain("打卡矩阵");
            html.Should().Contain("@page");
        }

        [Fact]
        public void 生成文件名_应清洗非法字符并带月份后缀()
        {
            var report = BuildReport(PeriodEnd);
            report = new PdfReadingCheckInReport
            {
                RootPath = @"C:\docs\2026:课程",
                PeriodStart = report.PeriodStart,
                PeriodEnd = report.PeriodEnd,
                MinSeconds = report.MinSeconds,
                Days = report.Days,
                Folders = report.Folders,
                Totals = report.Totals,
                Layout = report.Layout
            };

            PdfReadingCheckInExportService.BuildFileName(report, ".xlsx")
                .Should().Be("阅读打卡表_2026_课程_202601.xlsx");
        }

        [Theory]
        [InlineData(PdfReadingCheckInPrintScope.All, 2)]
        [InlineData(PdfReadingCheckInPrintScope.SummaryOnly, 1)]
        [InlineData(PdfReadingCheckInPrintScope.MatrixOnly, 1)]
        public void 打印文档_应按范围过滤页数(PdfReadingCheckInPrintScope scope, int expectedPages)
        {
            var report = BuildReport(PeriodEnd);

            using var print = new PdfReadingCheckInExportService()
                .CreatePrintDocument(report, new PdfReadingCheckInPrintOptions { Scope = scope });

            print.PageCount.Should().Be(expectedPages);
            print.Document.DocumentName.Should().Be("阅读打卡表");
            print.Document.DefaultPageSettings.Landscape.Should().BeTrue();
        }

        /// <summary>构造一份单目录、双文档的报表；today 决定哪些日期列为未来。</summary>
        private static PdfReadingCheckInReport BuildReport(DateTime today)
        {
            var days = Enumerable.Range(0, 31)
                .Select(offset => PeriodStart.AddDays(offset))
                .ToList();

            var checkedIn = new PdfCheckInDocumentRow
            {
                FullPath = @"C:\docs\book-1.pdf",
                FileName = "book-1.pdf",
                Exists = true,
                Status = PdfCheckInDocumentStatus.CheckedIn,
                DaySeconds = new Dictionary<DateTime, int>
                {
                    [PeriodStart] = 120,
                    [PeriodStart.AddDays(1)] = 30
                },
                CheckInDays = 1,
                TotalSeconds = 150,
                LastCheckInDay = PeriodStart,
                CurrentStreak = 1,
                Coverage = 0.5
            };

            var missing = new PdfCheckInDocumentRow
            {
                FullPath = @"C:\docs\book-2.pdf",
                FileName = "book-2.pdf",
                Exists = false,
                Status = PdfCheckInDocumentStatus.Unread
            };

            var folders = new List<PdfCheckInFolderGroup>
            {
                new()
                {
                    RelativePath = string.Empty,
                    DisplayPath = PdfCheckInLayoutRules.RootFolderDisplay,
                    Depth = 0,
                    Documents = new[] { checkedIn, missing },
                    DocumentCount = 2,
                    ReadDocumentCount = 1,
                    CheckInDays = 1,
                    TotalSeconds = 150,
                    LastCheckInDay = PeriodStart,
                    CurrentStreak = 1
                }
            };

            return new PdfReadingCheckInReport
            {
                RootPath = @"C:\docs",
                PeriodStart = PeriodStart,
                PeriodEnd = PeriodEnd,
                MinSeconds = 60,
                Days = days,
                Folders = folders,
                Totals = new PdfCheckInTotals
                {
                    FolderCount = 1,
                    DocumentCount = 2,
                    ReadDocumentCount = 1,
                    CheckInDays = 1,
                    TotalSeconds = 150,
                    OverallCurrentStreak = 1
                },
                Layout = PdfCheckInLayoutRules.Create(days, folders, today)
            };
        }
    }
}
