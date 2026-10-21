using FluentAssertions;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfBatchTextCleanerTests
{
    [Fact]
    public async Task 整理成功_按页返回整理后的文本()
    {
        var ai = new FakeAiService(_ => "```json\n[{\"page\":2,\"text\":\"# 标题\\n\\n整理后的第二页\"},{\"page\":3,\"text\":\"整理后的第三页\"}]\n```");
        var cleaner = new PdfBatchTextCleaner(ai);

        var result = await cleaner.CleanAsync("book", new[] { "原始第二页", "原始第三页" }, 1);

        result.Success.Should().BeTrue();
        result.Pages.Should().HaveCount(2);
        result.Pages[0].Should().Contain("整理后的第二页");
        result.Pages[1].Should().Be("整理后的第三页");
    }

    [Fact]
    public async Task AI异常_回退原始文本且不抛出()
    {
        var ai = new FakeAiService(_ => throw new InvalidOperationException("boom"));
        var cleaner = new PdfBatchTextCleaner(ai);

        var result = await cleaner.CleanAsync("book", new[] { "第一页", "第二页" }, 0);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("boom");
        result.Pages.Should().BeEquivalentTo(new[] { "第一页", "第二页" });
    }

    [Fact]
    public async Task AI返回非JSON_回退原始文本()
    {
        var ai = new FakeAiService(_ => "这不是 JSON");
        var cleaner = new PdfBatchTextCleaner(ai);

        var result = await cleaner.CleanAsync("book", new[] { "原文" }, 0);

        result.Success.Should().BeFalse();
        result.Pages.Should().ContainSingle().Which.Should().Be("原文");
    }

    [Fact]
    public async Task AI漏页_缺失页保留原始文本()
    {
        var ai = new FakeAiService(_ => "[{\"page\":1,\"text\":\"整理后第一页\"}]");
        var cleaner = new PdfBatchTextCleaner(ai);

        var result = await cleaner.CleanAsync("book", new[] { "原始第一页", "原始第二页" }, 0);

        result.Success.Should().BeTrue();
        result.Pages[0].Should().Be("整理后第一页");
        result.Pages[1].Should().Be("原始第二页");
    }

    [Fact]
    public async Task AI不可用_原样返回()
    {
        var cleaner = new PdfBatchTextCleaner(null);

        var result = await cleaner.CleanAsync("book", new[] { "原文" }, 0);

        result.Success.Should().BeFalse();
        result.Pages.Should().ContainSingle().Which.Should().Be("原文");
    }

    private sealed class FakeAiService : IAIService
    {
        private readonly Func<string, string> _handler;

        public FakeAiService(Func<string, string> handler) => _handler = handler;

        public string ModelName => "fake";
        public string ProviderName => "fake";

        public Task<string> AskQuestionAsync(string question, string context = "", CancellationToken cancellationToken = default)
            => Task.FromResult(_handler(question));

        public Task<string> GetExplanationAsync(string text, string language, string subType, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<string> GenerateExerciseAsync(string text, string language, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
        public Task<string> SummarizeAsync(string text, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Empty);
    }
}
