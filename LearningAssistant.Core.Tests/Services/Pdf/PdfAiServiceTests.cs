using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfAiServiceTests
{
    [Fact]
    public async Task 总结_上下文带页码锚点_越界锚点被过滤()
    {
        var index = new FakeTextIndex(3);
        index.SetText(0, "第一页内容");
        index.SetText(1, "第二页内容");
        index.SetText(2, "第三页内容");
        var ai = new FakeAiService(_ => "要点A [[p2]]，要点B [[p9]]");
        var service = new PdfAiService(ai, index);

        var result = await service.SummarizePagesAsync("book.pdf", 0, 2);

        result.Success.Should().BeTrue();
        result.ReferencedPages.Should().BeEquivalentTo(new[] { 2 });
        ai.LastPrompt.Should().Contain("[[p1]]").And.Contain("[[p3]]");
    }

    [Fact]
    public async Task 每页字符上限生效_超长文本被截断()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, new string('长', 100));
        var ai = new FakeAiService(_ => "摘要");
        var service = new PdfAiService(ai, index, options: new PdfAiOptions { MaxCharsPerPage = 10, MaxTotalChars = 500 });

        var result = await service.SummarizePagesAsync("book.pdf", 0, 0);

        result.Success.Should().BeTrue();
        ai.LastPrompt.Should().Contain("…");
        ai.LastPrompt.Should().NotContain(new string('长', 100));
    }

    [Fact]
    public async Task 闪卡返回非JSON_保留原文并标记非结构化()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "内容");
        var ai = new FakeAiService(_ => "这不是 JSON");
        var service = new PdfAiService(ai, index);

        var result = await service.GenerateFlashcardsAsync("book.pdf", 0, 0);

        result.Success.Should().BeTrue();
        result.IsStructured.Should().BeFalse();
        result.Content.Should().Be("这不是 JSON");
    }

    [Fact]
    public async Task 闪卡返回代码块JSON_可识别为结构化()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "内容");
        var ai = new FakeAiService(_ => "```json\n[{\"front\":\"问\",\"back\":\"答\",\"page\":1}]\n```");
        var service = new PdfAiService(ai, index);

        var result = await service.GenerateFlashcardsAsync("book.pdf", 0, 0);

        result.Success.Should().BeTrue();
        result.IsStructured.Should().BeTrue();
    }

    [Fact]
    public async Task AI异常_返回结构化失败而非抛出()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "内容");
        var ai = new FakeAiService(_ => throw new InvalidOperationException("boom"));
        var service = new PdfAiService(ai, index);

        var result = await service.SummarizePagesAsync("book.pdf", 0, 0);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("失败");
    }

    [Fact]
    public async Task 作用域问答_选中内容进入上下文并返回页码()
    {
        var index = new FakeTextIndex(3);
        index.SetText(2, "第三页全文");
        var ai = new FakeAiService(_ => "选中的句子指代前文 [[p3]]");
        var service = new PdfAiService(ai, index);

        var result = await service.AskScopedAsync("book.pdf", "这句是什么意思？", PdfAiScope.Selection, 2, 3, 3, "选中的句子");

        result.Success.Should().BeTrue();
        result.ReferencedPages.Should().BeEquivalentTo(new[] { 3 });
        ai.LastPrompt.Should().Contain("选中的句子").And.Contain("[[p3]]");
    }

    [Fact]
    public async Task 相同摘要第二次命中缓存_不再调用AI()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "内容");
        var ai = new FakeAiService(_ => "摘要 [[p1]]");
        var cache = new PdfAiCacheService(Path.Combine(Path.GetTempPath(), "pdfaicache_" + Guid.NewGuid().ToString("N")));
        var service = new PdfAiService(ai, index, cache);

        var first = await service.SummarizePagesAsync("book.pdf", 0, 0);
        var second = await service.SummarizePagesAsync("book.pdf", 0, 0);

        first.Content.Should().Be(second.Content);
        ai.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task 全文检索问答_仅召回相关页进入上下文()
    {
        var index = new FakeTextIndex(10);
        index.SetText(0, "无关内容");
        index.SetText(6, "光合作用的定义与步骤");
        index.SetText(7, "无关内容");
        var ai = new FakeAiService(_ => "光合作用是植物合成有机物 [[p7]]");
        var service = new PdfAiService(ai, index);

        var result = await service.AskDocumentWithRetrievalAsync("book.pdf", "什么是光合作用？");

        result.Success.Should().BeTrue();
        result.ReferencedPages.Should().BeEquivalentTo(new[] { 7 });
        ai.LastPrompt.Should().Contain("[[p7]]");
        ai.LastPrompt.Should().NotContain("[[p1]]").And.NotContain("[[p10]]");
    }

    [Fact]
    public async Task 全文检索无相关页_不调用AI并明确提示()
    {
        var index = new FakeTextIndex(2);
        index.SetText(0, "苹果");
        index.SetText(1, "香蕉");
        var ai = new FakeAiService(_ => "不应调用");
        var service = new PdfAiService(ai, index);

        var result = await service.AskDocumentWithRetrievalAsync("book.pdf", "量子纠缠");

        result.Success.Should().BeTrue();
        result.Content.Should().Contain("未在文档中找到依据");
        ai.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task 标注整理_提示词带页码与时间戳锚点()
    {
        var index = new FakeTextIndex(5);
        var ai = new FakeAiService(_ => "### 要点\n- [[p3]] [01:05] 复习这一节");
        var service = new PdfAiService(ai, index);

        var items = new[]
        {
            new PdfAnnotationDigestItem { PageIndex = 2, AudioTimestampMs = 65_000, Kind = "文字", Text = "复习这一节" }
        };

        var result = await service.SummarizeAnnotationsAsync("book.pdf", items);

        result.Success.Should().BeTrue();
        result.ReferencedPages.Should().ContainSingle().Which.Should().Be(3);
        ai.LastPrompt.Should().Contain("[[p3]] [01:05] 文字: 复习这一节");
        ai.LastPrompt.Should().Contain("### 待办");
    }

    [Fact]
    public async Task 标注整理_空标注不调用AI()
    {
        var index = new FakeTextIndex(2);
        var ai = new FakeAiService(_ => "不应调用");
        var service = new PdfAiService(ai, index);

        var result = await service.SummarizeAnnotationsAsync("book.pdf", Array.Empty<PdfAnnotationDigestItem>());

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("没有可整理的标注");
        ai.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task AI摘要条目_解析JSON并回链录音锚点()
    {
        var index = new FakeTextIndex(5);
        var ai = new FakeAiService(_ => "```json\n[{\"text\":\"复习这一节\",\"page\":3,\"timeMs\":65000}]\n```");
        var service = new PdfAiService(ai, index);

        var items = new[]
        {
            new PdfAnnotationDigestItem
            {
                PageIndex = 2,
                AudioTimestampMs = 65_000,
                AudioRecordingId = "rec-1",
                Kind = "文字",
                Text = "复习这一节"
            }
        };

        var result = await service.GenerateSummaryEntriesAsync("book.pdf", items);

        result.Success.Should().BeTrue();
        result.Entries.Should().ContainSingle();
        result.Entries[0].PageIndex.Should().Be(2);
        result.Entries[0].TimestampMs.Should().Be(65_000);
        result.Entries[0].RecordingId.Should().Be("rec-1");
    }

    [Fact]
    public async Task AI摘要条目_返回非JSON时失败()
    {
        var index = new FakeTextIndex(2);
        var ai = new FakeAiService(_ => "这不是 JSON");
        var service = new PdfAiService(ai, index);

        var result = await service.GenerateSummaryEntriesAsync("book.pdf", new[]
        {
            new PdfAnnotationDigestItem { PageIndex = 0, Kind = "文字", Text = "标注" }
        });

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AI服务不可用_直接返回失败()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "内容");
        var service = new PdfAiService(null, index);

        var result = await service.AskDocumentAsync("book.pdf", "问题");

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("AI 服务不可用");
    }

    [Fact]
    public async Task 英文页面_提示词要求英文作答()
    {
        var index = new FakeTextIndex(1);
        index.SetText(0, "Photosynthesis is the process by which plants make food.");
        var ai = new FakeAiService(_ => "Summary [[p1]]");
        var service = new PdfAiService(ai, index);

        var result = await service.SummarizePagesAsync("book.pdf", 0, 0);

        result.Success.Should().BeTrue();
        ai.LastPrompt.Should().Contain("文档主要为英文").And.Contain("使用英文作答");
    }

    [Fact]
    public async Task 中英混排文档_提示词按页指明语言()
    {
        var index = new FakeTextIndex(2);
        index.SetText(0, "第一页是中文内容");
        index.SetText(1, "This page is English content.");
        var ai = new FakeAiService(_ => "摘要 [[p1]]");
        var service = new PdfAiService(ai, index);

        var result = await service.SummarizePagesAsync("book.pdf", 0, 1);

        result.Success.Should().BeTrue();
        ai.LastPrompt.Should().Contain("中英混排")
            .And.Contain("第 1 页为中文")
            .And.Contain("第 2 页为英文")
            .And.Contain("按各页原文语言分别处理");
    }

    private sealed class FakeTextIndex : IPdfDocumentTextIndex
    {
        private readonly PdfTextIndexDocument _document;

        public FakeTextIndex(int pageCount)
        {
            _document = new PdfTextIndexDocument
            {
                DocumentPath = "book.pdf",
                Fingerprint = "fp",
                PageCount = pageCount,
                Pages = Enumerable.Range(0, pageCount)
                    .Select(i => new PdfTextIndexPage { PageIndex = i })
                    .ToList()
            };
        }

        public void SetText(int pageIndex, string text)
        {
            var page = _document.Pages[pageIndex];
            page.Text = text;
            page.CharacterCount = text.Length;
        }

        public string GetFingerprint(string pdfPath) => "fp";
        public PdfTextIndexDocument? TryLoadCached(string pdfPath) => _document;
        public Task<PdfTextIndexDocument> GetOrBuildAsync(string pdfPath, bool forceRebuild = false,
            IProgress<PdfTextIndexProgress>? progress = null, CancellationToken cancellationToken = default)
            => Task.FromResult(_document);
        public void Invalidate(string pdfPath) { }
    }

    private sealed class FakeAiService : IAIService
    {
        private readonly Func<string, string> _handler;

        public FakeAiService(Func<string, string> handler) => _handler = handler;

        public string? LastPrompt { get; private set; }
        public int CallCount { get; private set; }
        public string ModelName => "fake";
        public string ProviderName => "fake";

        public Task<string> AskQuestionAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            LastPrompt = question;
            CallCount++;
            return Task.FromResult(_handler(question));
        }

        public Task<string> GetExplanationAsync(string text, string language, string subType, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
    }
}
