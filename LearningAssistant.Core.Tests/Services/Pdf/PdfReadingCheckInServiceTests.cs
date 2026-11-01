using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfReadingCheckInServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _root;
    private readonly PdfReadingTimeStore _store;

    public PdfReadingCheckInServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "pdf_checkin_tests", Guid.NewGuid().ToString("N"));
        _root = Path.Combine(_tempRoot, "root");
        Directory.CreateDirectory(_root);
        _store = new PdfReadingTimeStore(Path.Combine(_tempRoot, "store"));
    }

    [Fact]
    public void 阈值边界_59秒不计打卡_60秒计打卡_阈值为0时任意秒数计打卡()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string doc = Path.Combine(_root, "book.pdf");
        _store.Add(doc, 59, day);

        var under = Build(day, day, 60, new[] { doc });
        var row = FindRow(under, "book.pdf")!;
        row.Status.Should().Be(PdfCheckInDocumentStatus.UnderThreshold);
        row.CheckInDays.Should().Be(0);
        row.TotalSeconds.Should().Be(59);

        _store.Add(doc, 1, day);
        var hit = Build(day, day, 60, new[] { doc });
        FindRow(hit, "book.pdf")!.CheckInDays.Should().Be(1);

        var zeroThreshold = Build(day, day, 0, new[] { doc });
        FindRow(zeroThreshold, "book.pdf")!.Status.Should().Be(PdfCheckInDocumentStatus.CheckedIn);
        FindRow(zeroThreshold, "book.pdf")!.CheckInDays.Should().Be(1);
    }

    [Fact]
    public void 三种状态_未读_未达标_已打卡()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string unread = Path.Combine(_root, "unread.pdf");
        string under = Path.Combine(_root, "under.pdf");
        string ok = Path.Combine(_root, "ok.pdf");
        _store.Add(under, 30, day);
        _store.Add(ok, 60, day);

        var report = Build(day, day, 60, new[] { unread, under, ok }, includeUnread: true);

        FindRow(report, "unread.pdf")!.Status.Should().Be(PdfCheckInDocumentStatus.Unread);
        FindRow(report, "under.pdf")!.Status.Should().Be(PdfCheckInDocumentStatus.UnderThreshold);
        FindRow(report, "ok.pdf")!.Status.Should().Be(PdfCheckInDocumentStatus.CheckedIn);
        report.Totals.ReadDocumentCount.Should().Be(2);
    }

    [Fact]
    public void 阅读对象仅统计PDF_图片记录与清单一律忽略()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string pdf = Path.Combine(_root, "book.pdf");
        string image = Path.Combine(_root, "cover.jpg");
        _store.Add(image, 600, day);
        _store.Add(pdf, 60, day);

        var report = Build(day, day, 60, new[] { pdf, image }, includeUnread: true);

        report.Totals.DocumentCount.Should().Be(1);
        report.Folders.SelectMany(folder => folder.Documents).Should().ContainSingle()
            .Which.FileName.Should().Be("book.pdf");
        report.Totals.TotalSeconds.Should().Be(60);
    }

    [Fact]
    public void 目录分组_多层相对路径与根层展示()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string rootDoc = Path.Combine(_root, "root.pdf");
        string level1Doc = Path.Combine(_root, "level1", "b.pdf");
        string level2Doc = Path.Combine(_root, "level1", "level2", "c.pdf");
        CreateFile(rootDoc);
        CreateFile(level1Doc);
        CreateFile(level2Doc);
        _store.Add(rootDoc, 60, day);
        _store.Add(level1Doc, 60, day);
        _store.Add(level2Doc, 60, day);

        var report = Build(day, day, 60, new[] { rootDoc, level1Doc, level2Doc });

        report.Folders.Should().HaveCount(3);
        var rootGroup = report.Folders.Single(folder => folder.RelativePath.Length == 0);
        rootGroup.DisplayPath.Should().Be(PdfCheckInLayoutRules.RootFolderDisplay);
        rootGroup.Depth.Should().Be(0);

        var level1 = report.Folders.Single(folder => folder.RelativePath == "level1");
        level1.Depth.Should().Be(1);
        level1.DisplayPath.Should().Be("level1");

        var level2 = report.Folders.Single(folder => folder.RelativePath == Path.Combine("level1", "level2"));
        level2.Depth.Should().Be(2);
        level2.Documents.Single().FileName.Should().Be("c.pdf");
    }

    [Fact]
    public void 打卡天数去重_同一天多次累加只计一天()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string doc = Path.Combine(_root, "book.pdf");
        _store.Add(doc, 30, day);
        _store.Add(doc, 30, day);

        var report = Build(day, day, 60, new[] { doc });
        var row = FindRow(report, "book.pdf")!;

        row.CheckInDays.Should().Be(1);
        row.TotalSeconds.Should().Be(60);
        report.Totals.CheckInDays.Should().Be(1);
    }

    [Fact]
    public void 连续计算_区间无记录沿用历史_中断两天记0()
    {
        var today = DateTime.Now.Date;
        string continuous = Path.Combine(_root, "continuous.pdf");
        string broken = Path.Combine(_root, "broken.pdf");

        // 最近 5 天连续打卡，但统计区间只有今天、今天没有记录
        for (int offset = 1; offset <= 5; offset++)
            _store.Add(continuous, 60, today.AddDays(-offset));

        // 最近一次打卡距今 3 天，已断签
        _store.Add(broken, 60, today.AddDays(-3));
        _store.Add(broken, 60, today.AddDays(-4));

        var report = Build(today, today, 60, new[] { continuous, broken }, includeUnread: true);

        var continuousRow = FindRow(report, "continuous.pdf")!;
        continuousRow.Status.Should().Be(PdfCheckInDocumentStatus.Unread);
        continuousRow.CheckInDays.Should().Be(0);
        continuousRow.CurrentStreak.Should().Be(5);

        FindRow(report, "broken.pdf")!.CurrentStreak.Should().Be(0);
    }

    [Fact]
    public void 连续计算_跨月以区间末日为基准()
    {
        var today = DateTime.Now.Date;
        var lastMonthEnd = new DateTime(today.Year, today.Month, 1).AddDays(-1);
        string doc = Path.Combine(_root, "book.pdf");
        _store.Add(doc, 60, lastMonthEnd);
        _store.Add(doc, 60, lastMonthEnd.AddDays(-1));

        var report = Build(lastMonthEnd.AddDays(-10), lastMonthEnd, 60, new[] { doc });
        FindRow(report, "book.pdf")!.CurrentStreak.Should().Be(2);
    }

    [Fact]
    public void 覆盖率_按已过天数计算_未来区间为0且不抛异常()
    {
        var today = DateTime.Now.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        int elapsedDays = (today - monthStart).Days + 1;
        string doc = Path.Combine(_root, "book.pdf");
        _store.Add(doc, 60, today);

        var report = Build(monthStart, today, 60, new[] { doc });
        FindRow(report, "book.pdf")!.Coverage.Should().BeApproximately(1.0 / elapsedDays, 1e-9);

        var future = Build(today.AddDays(10), today.AddDays(20), 60, Array.Empty<string>());
        future.Days.Should().BeEmpty();
        future.Folders.Should().BeEmpty();
        future.RootUnavailable.Should().BeFalse();
    }

    [Fact]
    public void 失效文件_按开关包含或排除()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string missing = Path.Combine(_root, "gone.pdf");
        _store.Add(missing, 60, day);

        var included = Build(day, day, 60, new[] { missing }, includeMissing: true);
        included.Totals.DocumentCount.Should().Be(1);
        included.Folders.Single().RelativePath.Should().Be(PdfCheckInLayoutRules.MissingGroupKey);
        included.Folders.Single().Documents.Single().Exists.Should().BeFalse();

        var excluded = Build(day, day, 60, new[] { missing }, includeMissing: false);
        excluded.Totals.DocumentCount.Should().Be(0);
        excluded.Folders.Should().BeEmpty();
    }

    [Fact]
    public void 重命名兜底_同级唯一_根内唯一_多候选保持失效()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string siblingOld = Path.Combine(_root, "a", "book.pdf");
        string siblingActual = Path.Combine(_root, "b", "book.pdf");
        string globalOld = Path.Combine(_root, "a", "book2.pdf");
        string globalActual = Path.Combine(_root, "x", "y", "book2.pdf");
        string multiOld = Path.Combine(_root, "a", "book3.pdf");

        CreateFile(siblingActual);
        CreateFile(globalActual);
        CreateFile(Path.Combine(_root, "b", "book3.pdf"));
        CreateFile(Path.Combine(_root, "c", "book3.pdf"));

        _store.Add(siblingOld, 60, day);
        _store.Add(globalOld, 60, day);
        _store.Add(multiOld, 60, day);

        var report = Build(day, day, 60,
            new[] { siblingOld, globalOld, multiOld },
            includeUnread: true,
            includeMissing: true);

        var sibling = FindRow(report, "book.pdf")!;
        sibling.Exists.Should().BeTrue();
        sibling.RenamedMatched.Should().BeTrue();
        sibling.FullPath.Should().Be(siblingActual);

        var global = FindRow(report, "book2.pdf")!;
        global.Exists.Should().BeTrue();
        global.RenamedMatched.Should().BeTrue();
        global.FullPath.Should().Be(globalActual);

        var multi = FindRow(report, "book3.pdf")!;
        multi.Exists.Should().BeFalse();
        multi.RenamedMatched.Should().BeFalse();
    }

    [Fact]
    public void 区间过滤_区间外记录与根目录外文档不进入报表()
    {
        var today = DateTime.Now.Date;
        string inRange = Path.Combine(_root, "in.pdf");
        string outOfRange = Path.Combine(_root, "old.pdf");
        string outOfRoot = Path.Combine(_tempRoot, "outside", "out.pdf");
        CreateFile(inRange);
        _store.Add(inRange, 60, today);
        _store.Add(outOfRange, 60, today.AddDays(-5));
        _store.Add(outOfRoot, 60, today);

        var report = Build(today, today, 60, new[] { inRange, outOfRange });

        report.Totals.DocumentCount.Should().Be(1);
        report.Totals.CheckInDays.Should().Be(1);
        report.Totals.OutOfScopeDocumentCount.Should().Be(1);
        FindRow(report, "in.pdf")!.Exists.Should().BeTrue();
    }

    [Fact]
    public void 月份边界_二月与三十一天月份矩阵列数正确()
    {
        var feb2026 = Build(new DateTime(2026, 2, 1), new DateTime(2026, 2, 28));
        feb2026.Days.Should().HaveCount(28);

        var feb2024 = Build(new DateTime(2024, 2, 1), new DateTime(2024, 2, 29));
        feb2024.Days.Should().HaveCount(29);

        var january = Build(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));
        january.Days.Should().HaveCount(31);
        january.Layout.Columns.Should().HaveCount(31);
    }

    [Fact]
    public void 布局分页_页计划确定且每页重复表头()
    {
        var days = Enumerable.Range(0, 31)
            .Select(index => new DateTime(2026, 1, 1).AddDays(index))
            .ToList();
        var folder = CreateFolder("", 40);

        var layout = PdfCheckInLayoutRules.Create(days, new[] { folder }, new DateTime(2026, 1, 31));

        layout.SegmentMode.Should().Be(PdfCheckInPrintSegmentMode.SinglePage);
        layout.CellWidthMm.Should().Be(6);
        layout.Columns.Should().HaveCount(31);
        layout.Pages.Should().NotBeEmpty();
        layout.Pages.Select(page => page.PageNumber).Should().BeInAscendingOrder();
        layout.Pages.Should().OnlyContain(page => page.RepeatsHeader && page.Rows.Count > 0);

        var summaryPages = layout.Pages.Where(page => page.Kind == PdfCheckInPageKind.Summary).ToList();
        summaryPages.Should().HaveCount(2);
        summaryPages[1].Rows.Should().Contain(row => row.IsContinuation);
        summaryPages[0].Rows.Count.Should().Be(PdfCheckInLayoutRules.RowsPerPage);
    }

    [Fact]
    public void 布局分页_列宽不足时自动分两段()
    {
        var days = Enumerable.Range(0, 31)
            .Select(index => new DateTime(2026, 1, 1).AddDays(index))
            .ToList();
        var folder = CreateFolder("", 1);

        var layout = PdfCheckInLayoutRules.Create(days, new[] { folder }, new DateTime(2026, 1, 31), dateColumnsWidthMm: 120);

        layout.SegmentMode.Should().Be(PdfCheckInPrintSegmentMode.TwoSegments);
        layout.CellWidthMm.Should().Be(7);
        layout.Columns.Count(column => column.InSecondSegment).Should().Be(16);
        var matrixPages = layout.Pages.Where(page => page.Kind == PdfCheckInPageKind.Matrix).ToList();
        matrixPages.Should().HaveCount(2);
        matrixPages[0].SegmentLabel.Should().Be("1-15");
        matrixPages[1].SegmentLabel.Should().Be("16-31");
    }

    [Fact]
    public void HTML导出_自包含且转义目录名()
    {
        var day = DateTime.Now.Date.AddDays(-1);
        string dir = Path.Combine(_root, "sub & dir");
        Directory.CreateDirectory(dir);
        string doc = Path.Combine(dir, "book.pdf");
        CreateFile(doc);
        _store.Add(doc, 60, day);

        var report = Build(day, day, 60, new[] { doc }, includeUnread: true);
        var options = new PdfReadingCheckInHtmlOptions { GeneratedAt = new DateTime(2026, 11, 1, 9, 0, 0) };

        string html = PdfReadingCheckInHtmlBuilder.Build(report, options);

        html.Should().Contain("@page { size: A4 landscape");
        html.Should().Contain("<thead>");
        html.Should().Contain("目录汇总");
        html.Should().Contain("打卡矩阵");
        html.Should().Contain("sub &amp; dir");
        html.Should().Contain("✓");
        html.Should().Contain("仅统计阅读器前台停留时间");
        html.Should().Contain("生成于 2026-11-01 09:00");
        html.Should().NotContain("<script");
        html.Should().NotContain("http://");

        PdfReadingCheckInHtmlBuilder.Build(report, options).Should().Be(html);
    }

    [Fact]
    public void 根目录不可用_返回空报表并标记()
    {
        var report = Build(DateTime.Now.Date.AddDays(-1), DateTime.Now.Date, 60, rootPath: Path.Combine(_tempRoot, "not-exists"));

        report.RootUnavailable.Should().BeTrue();
        report.Folders.Should().BeEmpty();
        report.Totals.DocumentCount.Should().Be(0);
    }

    private PdfReadingCheckInReport Build(
        DateTime start,
        DateTime end,
        int minSeconds = 60,
        IEnumerable<string>? documents = null,
        bool includeUnread = false,
        bool includeMissing = true,
        string? rootPath = null)
        => new PdfReadingCheckInService(_store).Build(new PdfReadingCheckInRequest
        {
            RootPath = rootPath ?? _root,
            Documents = documents?.ToList(),
            PeriodStart = start,
            PeriodEnd = end,
            MinSeconds = minSeconds,
            IncludeUnread = includeUnread,
            IncludeMissing = includeMissing
        });

    private static PdfCheckInDocumentRow? FindRow(PdfReadingCheckInReport report, string fileName)
        => report.Folders
            .SelectMany(folder => folder.Documents)
            .FirstOrDefault(document => string.Equals(document.FileName, fileName, StringComparison.OrdinalIgnoreCase));

    private static PdfCheckInFolderGroup CreateFolder(string relativePath, int documentCount)
    {
        var documents = Enumerable.Range(0, documentCount)
            .Select(index => new PdfCheckInDocumentRow
            {
                FullPath = $@"C:\docs\book{index}.pdf",
                FileName = $"book{index}.pdf",
                Exists = true,
                Status = PdfCheckInDocumentStatus.CheckedIn
            })
            .ToList();

        return new PdfCheckInFolderGroup
        {
            RelativePath = relativePath,
            DisplayPath = relativePath.Length == 0 ? PdfCheckInLayoutRules.RootFolderDisplay : relativePath,
            Documents = documents,
            DocumentCount = documents.Count,
            ReadDocumentCount = documents.Count
        };
    }

    private static void CreateFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "pdf");
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { }
    }
}
