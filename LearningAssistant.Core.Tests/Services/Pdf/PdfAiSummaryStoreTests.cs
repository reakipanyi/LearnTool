using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAiSummaryStoreTests : IDisposable
{
    private readonly string _root;

    public PdfAiSummaryStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "pdf_ai_summary_tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    [Fact]
    public void 追加条目_同一文档可读回()
    {
        var store = new PdfAiSummaryStore(_root);
        store.AddEntries("book.pdf", new[]
        {
            new PdfAiSummaryEntry { PageIndex = 2, TimestampMs = 65_000, RecordingId = "rec-1", Text = "复习这一节" }
        });

        var entries = store.GetEntries("book.pdf");

        entries.Should().ContainSingle();
        entries[0].PageIndex.Should().Be(2);
        entries[0].RecordingId.Should().Be("rec-1");
        entries[0].TimestampMs.Should().Be(65_000);
    }

    [Fact]
    public void 相同页码文字时间戳_不重复追加()
    {
        var store = new PdfAiSummaryStore(_root);
        var entry = new PdfAiSummaryEntry { PageIndex = 1, TimestampMs = 5_000, Text = "同一条" };

        store.AddEntries("book.pdf", new[] { entry });
        store.AddEntries("book.pdf", new[] { new PdfAiSummaryEntry { PageIndex = 1, TimestampMs = 5_000, Text = "同一条" } });

        store.GetEntries("book.pdf").Should().ContainSingle();
    }

    [Fact]
    public void 不同文档_数据隔离()
    {
        var store = new PdfAiSummaryStore(_root);
        store.AddEntries("a.pdf", new[] { new PdfAiSummaryEntry { PageIndex = 0, Text = "A" } });

        store.GetEntries("b.pdf").Should().BeEmpty();
    }

    [Fact]
    public void 清空后无条目()
    {
        var store = new PdfAiSummaryStore(_root);
        store.AddEntries("book.pdf", new[] { new PdfAiSummaryEntry { PageIndex = 0, Text = "A" } });

        store.Clear("book.pdf");

        store.GetEntries("book.pdf").Should().BeEmpty();
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
    }
}
