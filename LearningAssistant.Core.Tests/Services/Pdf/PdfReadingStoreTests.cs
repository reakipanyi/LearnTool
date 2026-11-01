using System.Text.Json;
using FluentAssertions;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfReadingStoreTests : IDisposable
{
    private const string TimeFileName = "reading_time.json";

    private readonly string _root;

    public PdfReadingStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pdf_reading_store_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void 按当前目录懒解析_切换用户后数据不串()
    {
        string userA = Path.Combine(_root, "userA");
        string userB = Path.Combine(_root, "userB");
        string current = userA;
        var store = new PdfReadingTimeStore(() => current);

        var day = new DateTime(2026, 10, 20);
        store.Add(@"C:\docs\a.pdf", 120, day);
        current = userB;

        store.GetRecords().Should().BeEmpty();
        store.GetSnapshot(@"C:\docs\a.pdf", day).Total.Should().Be(TimeSpan.Zero);

        store.Add(@"C:\docs\b.pdf", 90, day);
        current = userA;

        store.GetRecords().Should().ContainSingle()
            .Which.Seconds.Should().Be(120);
        store.GetRecords().Should().Contain(r => r.DocPath.EndsWith("a.pdf", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void 读取明细_返回副本改动不影响内部数据()
    {
        var store = new PdfReadingTimeStore(Path.Combine(_root, "user"));
        store.Add(@"C:\docs\a.pdf", 120, new DateTime(2026, 10, 20));

        var first = store.GetRecords();
        first[0].Seconds = 9999;
        first[0].DocPath = @"C:\other.pdf";

        var second = store.GetRecords();
        second.Should().ContainSingle();
        second[0].Seconds.Should().Be(120);
        second[0].DocPath.Should().EndWith("a.pdf");
    }

    [Fact]
    public void 读取明细_按日期区间过滤()
    {
        var store = new PdfReadingTimeStore(Path.Combine(_root, "user"));
        store.Add(@"C:\docs\a.pdf", 60, new DateTime(2026, 09, 01));
        store.Add(@"C:\docs\a.pdf", 60, new DateTime(2026, 10, 15));
        store.Add(@"C:\docs\a.pdf", 60, new DateTime(2026, 11, 01));

        var october = store.GetRecords(new DateTime(2026, 10, 01), new DateTime(2026, 10, 31));

        october.Should().ContainSingle();
        october[0].Day.Should().Be("2026-10-15");
    }

    [Fact]
    public void 加载时裁剪超过留存天数的记录_并回写文件()
    {
        string directory = Path.Combine(_root, "user");
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, TimeFileName);

        var today = DateTime.Now.Date;
        var records = new List<PdfReadingTimeRecord>
        {
            new() { DocPath = @"C:\old.pdf", Day = today.AddDays(-800).ToString("yyyy-MM-dd"), Seconds = 60 },
            new() { DocPath = @"C:\new.pdf", Day = today.ToString("yyyy-MM-dd"), Seconds = 60 }
        };
        File.WriteAllText(file, JsonSerializer.Serialize(records));

        var store = new PdfReadingTimeStore(directory);
        var loaded = store.GetRecords();

        loaded.Should().ContainSingle();
        loaded[0].DocPath.Should().EndWith("new.pdf");
        JsonSerializer.Deserialize<List<PdfReadingTimeRecord>>(File.ReadAllText(file))!
            .Should().ContainSingle();
    }

    [Fact]
    public void 记录数超过硬上限_保留最新并回写()
    {
        string directory = Path.Combine(_root, "user");
        Directory.CreateDirectory(directory);
        string file = Path.Combine(directory, TimeFileName);

        var today = DateTime.Now.Date;
        var records = Enumerable.Range(0, 20010)
            .Select(i => new PdfReadingTimeRecord
            {
                DocPath = $@"C:\docs\book{i}.pdf",
                Day = today.ToString("yyyy-MM-dd"),
                Seconds = 60
            })
            .ToList();
        File.WriteAllText(file, JsonSerializer.Serialize(records));

        var store = new PdfReadingTimeStore(directory);
        var loaded = store.GetRecords();

        loaded.Should().HaveCount(20000);
        JsonSerializer.Deserialize<List<PdfReadingTimeRecord>>(File.ReadAllText(file))!
            .Should().HaveCount(20000);
    }

    [Fact]
    public void 断点按当前目录懒解析_切换用户后不串断点()
    {
        string userA = Path.Combine(_root, "userA");
        string userB = Path.Combine(_root, "userB");
        string current = userA;
        var store = new PdfReadingPositionStore(() => current);
        string doc = Path.Combine(_root, "book.pdf");

        store.Save(doc, 3, 5);
        store.Load(doc)!.PageIndex.Should().Be(3);

        current = userB;
        store.Load(doc).Should().BeNull();

        current = userA;
        var restored = store.Load(doc);
        restored.Should().NotBeNull();
        restored!.PageIndex.Should().Be(3);
        restored.SentenceIndex.Should().Be(5);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
    }
}
