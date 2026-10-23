using FluentAssertions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfTranslationGlossaryServiceTests
{
    [Fact]
    public async Task 术语表按指纹缓存_同文档只构建一次()
    {
        var cacheDir = CreateTempDir();
        try
        {
            int calls = 0;
            var ai = new FakeAiService(_ =>
            {
                calls++;
                return "[{\"source\":\"photosynthesis\",\"target\":\"光合作用\"}]";
            });
            var index = new FakeTextIndex("fp1", "Photosynthesis is important.");
            var service = new PdfTranslationGlossaryService(index, ai, cacheDirectory: cacheDir);

            var first = await service.GetOrBuildAsync("book.pdf");
            var second = await service.GetOrBuildAsync("book.pdf");

            calls.Should().Be(1);
            first!.Entries.Should().ContainSingle().Which.Target.Should().Be("光合作用");
            second!.Entries.Should().HaveCount(1);
            Directory.GetFiles(cacheDir, "*.json").Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task 润色成功_使用术语表并报告未统一的术语()
    {
        var cacheDir = CreateTempDir();
        try
        {
            int calls = 0;
            var ai = new FakeAiService(prompt =>
            {
                calls++;
                return calls == 1
                    ? "[{\"source\":\"photosynthesis\",\"target\":\"光合作用\"}]"
                    : "植物通过光合作用制造养分。";
            });
            var index = new FakeTextIndex("fp2", "Photosynthesis makes food.");
            var service = new PdfTranslationGlossaryService(index, ai, cacheDirectory: cacheDir);

            var result = await service.PolishAsync("book.pdf", "Photosynthesis makes food.", "植物通过光合作用制造食物");

            result.Success.Should().BeTrue();
            result.Text.Should().Be("植物通过光合作用制造养分。");
            result.MissingTerms.Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task 润色未使用术语_返回缺失术语()
    {
        var cacheDir = CreateTempDir();
        try
        {
            int calls = 0;
            var ai = new FakeAiService(_ =>
            {
                calls++;
                return calls == 1
                    ? "[{\"source\":\"photosynthesis\",\"target\":\"光合作用\"}]"
                    : "植物制造养分。";
            });
            var index = new FakeTextIndex("fp3", "Photosynthesis makes food.");
            var service = new PdfTranslationGlossaryService(index, ai, cacheDirectory: cacheDir);

            var result = await service.PolishAsync("book.pdf", "Photosynthesis makes food.", "机器译文");

            result.Success.Should().BeTrue();
            result.MissingTerms.Should().ContainSingle().Which.Source.Should().Be("photosynthesis");
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task 润色异常_回退机器译文()
    {
        var cacheDir = CreateTempDir();
        try
        {
            var ai = new FakeAiService(_ => throw new InvalidOperationException("ai down"));
            var index = new FakeTextIndex("fp4", "Some text.");
            var service = new PdfTranslationGlossaryService(index, ai, cacheDirectory: cacheDir);

            var result = await service.PolishAsync("book.pdf", "Some text.", "机器译文");

            result.Success.Should().BeFalse();
            result.Text.Should().Be("机器译文");
            result.ErrorMessage.Should().Contain("ai down");
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task 无AI服务_术语表为空()
    {
        var index = new FakeTextIndex("fp5", "text");
        var service = new PdfTranslationGlossaryService(index, null, cacheDirectory: CreateTempDir());

        var glossary = await service.GetOrBuildAsync("book.pdf");

        glossary.Should().BeNull();
    }

    [Fact]
    public async Task 保存术语表_覆盖缓存并立即生效()
    {
        var cacheDir = CreateTempDir();
        try
        {
            int calls = 0;
            var ai = new FakeAiService(_ =>
            {
                calls++;
                return "[{\"source\":\"alpha\",\"target\":\"阿尔法\"}]";
            });
            var index = new FakeTextIndex("fp6", "alpha beta");
            var service = new PdfTranslationGlossaryService(index, ai, cacheDirectory: cacheDir);

            var saved = await service.SaveAsync("book.pdf", new[]
            {
                new PdfGlossaryEntry { Source = " beta ", Target = " 贝塔 " },
                new PdfGlossaryEntry { Source = "beta", Target = "重复译名" },
                new PdfGlossaryEntry { Source = " ", Target = "空" }
            });

            var glossary = await service.GetOrBuildAsync("book.pdf");

            saved.Should().BeTrue();
            calls.Should().Be(0);
            glossary!.Entries.Should().ContainSingle();
            glossary.Entries[0].Source.Should().Be("beta");
            glossary.Entries[0].Target.Should().Be("贝塔");
            Directory.GetFiles(cacheDir, "*.json").Should().HaveCount(1);
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    [Fact]
    public async Task 保存空术语表_清除磁盘缓存()
    {
        var cacheDir = CreateTempDir();
        try
        {
            var index = new FakeTextIndex("fp7", "text");
            var service = new PdfTranslationGlossaryService(index, null, cacheDirectory: cacheDir);

            await service.SaveAsync("book.pdf", new[] { new PdfGlossaryEntry { Source = "alpha", Target = "阿尔法" } });
            Directory.GetFiles(cacheDir, "*.json").Should().HaveCount(1);

            await service.SaveAsync("book.pdf", Array.Empty<PdfGlossaryEntry>());

            Directory.GetFiles(cacheDir, "*.json").Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(cacheDir, recursive: true);
        }
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "glossary_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private sealed class FakeTextIndex : IPdfDocumentTextIndex
    {
        private readonly string _fingerprint;
        private readonly PdfTextIndexDocument _document;

        public FakeTextIndex(string fingerprint, string text)
        {
            _fingerprint = fingerprint;
            _document = new PdfTextIndexDocument
            {
                Fingerprint = fingerprint,
                PageCount = 1,
                Pages = new List<PdfTextIndexPage>
                {
                    new() { PageIndex = 0, Text = text, CharacterCount = text.Length }
                }
            };
        }

        public string GetFingerprint(string pdfPath) => _fingerprint;

        public PdfTextIndexDocument? TryLoadCached(string pdfPath) => null;

        public Task<PdfTextIndexDocument> GetOrBuildAsync(
            string pdfPath, bool forceRebuild = false,
            IProgress<PdfTextIndexProgress>? progress = null,
            CancellationToken cancellationToken = default)
            => Task.FromResult(_document);

        public void Invalidate(string pdfPath) { }
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
