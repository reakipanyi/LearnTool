using FluentAssertions;
using LearningAssistant.Abstractions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using System.Drawing;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public class PdfDocumentTextIndexTests
{
    [Fact]
    public async Task 文本层优先_扫描页回退OCR_二次读取命中缓存()
    {
        var root = CreateTempRoot();
        var pdfPath = Path.Combine(root, "book.pdf");
        await File.WriteAllTextAsync(pdfPath, "fake pdf");

        var pdfService = new FakePdfService { PageCount = 3 };
        pdfService.Texts[0] = "第一页文本";
        pdfService.Texts[2] = "第三页文本";
        var ocrService = new FakeOcrService { Result = "第二页识别文本" };
        var index = new PdfDocumentTextIndex(
            pdfService,
            ocrService,
            _ => new FakePdfService { PageCount = 3 },
            indexRootPath: Path.Combine(root, "index"));

        var document = await index.GetOrBuildAsync(pdfPath);

        document.PageCount.Should().Be(3);
        document.Pages[0].Source.Should().Be(PdfTextSource.TextLayer);
        document.Pages[1].Source.Should().Be(PdfTextSource.Ocr);
        document.Pages[1].Text.Should().Be("第二页识别文本");
        document.Pages[2].Text.Should().Be("第三页文本");
        document.Pages[1].DetectedLanguage.Should().Be("Chinese");
        document.TotalCharacterCount.Should().BeGreaterThan(0);

        int extractCalls = pdfService.ExtractCalls;
        PdfTextIndexProgress? cachedProgress = null;
        var second = await index.GetOrBuildAsync(pdfPath,
            progress: new SyncProgress<PdfTextIndexProgress>(p => cachedProgress = p));

        second.Fingerprint.Should().Be(document.Fingerprint);
        pdfService.ExtractCalls.Should().Be(extractCalls);
        cachedProgress.Should().NotBeNull();
        cachedProgress!.FromCache.Should().BeTrue();
    }

    [Fact]
    public async Task 文件修改后指纹变化_缓存自动失效并重建()
    {
        var root = CreateTempRoot();
        var pdfPath = Path.Combine(root, "book.pdf");
        await File.WriteAllTextAsync(pdfPath, "v1");

        var pdfService = new FakePdfService { PageCount = 1 };
        pdfService.Texts[0] = "内容A";
        var index = new PdfDocumentTextIndex(
            pdfService,
            null,
            null,
            indexRootPath: Path.Combine(root, "index"));

        var first = await index.GetOrBuildAsync(pdfPath);
        var firstFingerprint = first.Fingerprint;

        await File.WriteAllTextAsync(pdfPath, "v2 longer");
        File.SetLastWriteTimeUtc(pdfPath, DateTime.UtcNow.AddMinutes(5));

        index.GetFingerprint(pdfPath).Should().NotBe(firstFingerprint);
        index.TryLoadCached(pdfPath).Should().BeNull();

        pdfService.Texts[0] = "内容B";
        var second = await index.GetOrBuildAsync(pdfPath);

        second.Fingerprint.Should().NotBe(firstFingerprint);
        second.Pages[0].Text.Should().Be("内容B");
    }

    [Fact]
    public async Task 缓存损坏时返回未命中_并重新构建()
    {
        var root = CreateTempRoot();
        var pdfPath = Path.Combine(root, "book.pdf");
        await File.WriteAllTextAsync(pdfPath, "fake");

        var pdfService = new FakePdfService { PageCount = 1 };
        pdfService.Texts[0] = "有效文本";
        var indexDir = Path.Combine(root, "index");
        var index = new PdfDocumentTextIndex(pdfService, null, null, indexRootPath: indexDir);

        await index.GetOrBuildAsync(pdfPath);
        var cacheFile = Directory.GetFiles(indexDir, "*.json").Single();
        await File.WriteAllTextAsync(cacheFile, "{ broken json");

        index.TryLoadCached(pdfPath).Should().BeNull();
        var rebuilt = await index.GetOrBuildAsync(pdfPath);
        rebuilt.Pages[0].Text.Should().Be("有效文本");
    }

    [Fact]
    public async Task Invalidate删除该文档缓存()
    {
        var root = CreateTempRoot();
        var pdfPath = Path.Combine(root, "book.pdf");
        await File.WriteAllTextAsync(pdfPath, "fake");

        var pdfService = new FakePdfService { PageCount = 1 };
        pdfService.Texts[0] = "文本";
        var index = new PdfDocumentTextIndex(pdfService, null, null, indexRootPath: Path.Combine(root, "index"));

        await index.GetOrBuildAsync(pdfPath);
        index.TryLoadCached(pdfPath).Should().NotBeNull();

        index.Invalidate(pdfPath);

        index.TryLoadCached(pdfPath).Should().BeNull();
    }

    private static string CreateTempRoot()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pdfindex_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>同步回调的进度实现，避免 Progress&lt;T&gt; 异步投递导致断言竞态</summary>
    private sealed class SyncProgress<T> : IProgress<T>
    {
        private readonly Action<T> _callback;

        public SyncProgress(Action<T> callback) => _callback = callback;

        public void Report(T value) => _callback(value);
    }

    private sealed class FakePdfService : IPdfService
    {
        public int PageCount { get; set; }
        public Dictionary<int, string> Texts { get; } = new();
        public int ExtractCalls { get; private set; }

        public void Load(string path) { }
        public void Unload() { }
        public byte[]? RenderPage(int pageIndex, int width, int height) => null;
        public Bitmap? RenderPageBitmap(int pageIndex, int width, int height) => new Bitmap(10, 10);
        public SizeFInfo GetPageSize(int pageIndex) => new SizeFInfo(100, 140);
        public string GetPdfText(int pageIndex) => Texts.TryGetValue(pageIndex, out var text) ? text : string.Empty;
        public IReadOnlyList<LearningAssistant.Models.Pdf.PdfTextLine> GetTextLines(int pageIndex) =>
            Array.Empty<LearningAssistant.Models.Pdf.PdfTextLine>();

        public int GetPageCount(string pdfPath) => PageCount;

        public string ExtractText(string pdfPath, int pageNumber)
        {
            ExtractCalls++;
            return Texts.TryGetValue(pageNumber - 1, out var text) ? text : string.Empty;
        }

        public bool Print(bool printDialog = true, int fromPage = 0, int toPage = -1) => true;
        public void Dispose() { }
    }

    private sealed class FakeOcrService : IPdfOcrService
    {
        public string? Result { get; set; }
        public bool IsAvailable => true;
        public string? InitErrorMessage => null;
        public string CurrentLanguage => "eng";
        public Task<string?> RecognizeTextAsync(byte[] image) => Task.FromResult(Result);
        public Task<string?> RecognizeTextAsync(byte[] image, RectInt region) => Task.FromResult(Result);
        public Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image) => Task.FromResult(Result);
        public Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image, RectInt region) => Task.FromResult(Result);
        public Task<LearningAssistant.Models.Pdf.OcrLayoutResult?> RecognizeLayoutWithAutoLanguageAsync(byte[] image) =>
            Task.FromResult<LearningAssistant.Models.Pdf.OcrLayoutResult?>(null);
        public bool SetLanguage(string language) => true;
    }
}
