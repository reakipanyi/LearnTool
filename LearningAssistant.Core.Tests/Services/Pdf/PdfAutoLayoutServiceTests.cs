using System.Drawing;
using LearningAssistant.Abstractions;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Xunit;

namespace LearningAssistant.Core.Tests.Services.Pdf;

public sealed class PdfAutoLayoutServiceTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _pdfPath;
    private readonly FakePdfService _pdfService;

    public PdfAutoLayoutServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "pdf_auto_layout_tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
        _pdfPath = Path.Combine(_tempRoot, "sample.pdf");
        File.WriteAllText(_pdfPath, "fake pdf");

        _pdfService = new FakePdfService();
        _pdfService.Pages[0] = new[]
        {
            TextLine("第一段正文内容", 0.1f, 0.20f, 0.6f, 0.02f),
            TextLine("第二段正文内容", 0.1f, 0.24f, 0.6f, 0.02f)
        };
        _pdfService.Pages[1] = new[]
        {
            TextLine("Page two body text", 0.1f, 0.30f, 0.6f, 0.02f)
        };
        // 第 3 页模拟扫描页：没有文本层
        _pdfService.Pages[2] = Array.Empty<PdfTextLine>();
    }

    public void Dispose()
    {
        _pdfService.Dispose();
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // 临时目录清理失败不影响测试结论
        }
    }

    [Fact]
    public async Task GetOrBuildPageAsync_BuildsAndCachesPage()
    {
        var service = CreateService();

        var first = await service.GetOrBuildPageAsync(_pdfPath, 0, allowOcr: false);
        var second = await service.GetOrBuildPageAsync(_pdfPath, 0, allowOcr: false);

        Assert.Equal(PdfTextLineSource.TextLayer, first.Source);
        Assert.NotEmpty(first.Blocks);
        Assert.Equal(1, _pdfService.TextLineCalls);

        Assert.Equal(first.Blocks.Count, second.Blocks.Count);
    }

    [Fact]
    public async Task BuildDocumentAsync_ReportsProgressForEveryPage()
    {
        var service = CreateService();
        var reports = new List<PdfAutoLayoutProgress>();
        var progress = new Progress<PdfAutoLayoutProgress>(reports.Add);

        await service.BuildDocumentAsync(_pdfPath, allowOcr: false, progress);

        Assert.Equal(3, _pdfService.TextLineCalls);
        Assert.NotNull(service.TryGetCachedPage(_pdfPath, 0));
        Assert.NotNull(service.TryGetCachedPage(_pdfPath, 1));
        Assert.NotNull(service.TryGetCachedPage(_pdfPath, 2));
    }

    [Fact]
    public async Task ScannedPage_WithoutOcr_ReturnsNone()
    {
        var ocr = new FakeOcrService();
        var service = CreateService(ocr);

        var page = await service.GetOrBuildPageAsync(_pdfPath, 2, allowOcr: false);

        Assert.Equal(PdfTextLineSource.None, page.Source);
        Assert.Empty(page.Blocks);
        Assert.Equal(0, ocr.Calls);
    }

    [Fact]
    public async Task ScannedPage_WithOcr_BuildsBlocksAndCachesResult()
    {
        var ocr = new FakeOcrService();
        var service = CreateService(ocr);

        var first = await service.GetOrBuildPageAsync(_pdfPath, 2, allowOcr: true);
        var second = await service.GetOrBuildPageAsync(_pdfPath, 2, allowOcr: true);

        Assert.Equal(PdfTextLineSource.Ocr, first.Source);
        Assert.NotEmpty(first.Blocks);
        Assert.Equal(1, ocr.Calls);
        Assert.Equal(first.Blocks.Count, second.Blocks.Count);
    }

    [Fact]
    public async Task BuildDocumentAsync_WithOcr_ReportsOcrAndTextLayerPages()
    {
        var ocr = new FakeOcrService();
        var service = CreateService(ocr);

        var summary = await service.BuildDocumentAsync(_pdfPath, allowOcr: true);

        Assert.Equal(3, summary.TotalPages);
        Assert.Equal(2, summary.TextLayerPages);
        Assert.Equal(1, summary.OcrPages);
        Assert.Equal(0, summary.FailedPages);
    }

    [Fact]
    public async Task BuildDocumentAsync_OcrFailure_DoesNotBreakWholeDocument()
    {
        var ocr = new FakeOcrService { Result = null };
        var service = CreateService(ocr);

        var summary = await service.BuildDocumentAsync(_pdfPath, allowOcr: true);

        Assert.Equal(3, summary.TotalPages);
        Assert.Equal(2, summary.TextLayerPages);
        Assert.Equal(0, summary.OcrPages);
        Assert.Equal(1, summary.SkippedPages);
    }

    [Fact]
    public void State_CanBeSavedAndCleared()
    {
        var service = CreateService();
        var state = service.LoadState(_pdfPath);
        state.WholeBookEnabled = true;
        state.SuppressedPages.Add(1);
        service.SaveState(_pdfPath, state);

        var loaded = service.LoadState(_pdfPath);
        Assert.True(loaded.WholeBookEnabled);
        Assert.Contains(1, loaded.SuppressedPages);

        service.ClearDocument(_pdfPath);

        var cleared = service.LoadState(_pdfPath);
        Assert.False(cleared.WholeBookEnabled);
        Assert.Empty(cleared.SuppressedPages);
    }

    private PdfAutoLayoutService CreateService(IPdfOcrService? ocrService = null)
    {
        return new PdfAutoLayoutService(
            _pdfService,
            ocrService: ocrService,
            textIndex: null,
            logger: null,
            rootDir: Path.Combine(_tempRoot, "cache"));
    }

    private static PdfTextLine TextLine(string text, float x, float y, float width, float height)
    {
        return new PdfTextLine
        {
            Text = text,
            X = x,
            Y = y,
            Width = width,
            Height = height,
            FontSize = height * 800f
        };
    }

    private sealed class FakeOcrService : IPdfOcrService
    {
        public int Calls { get; private set; }
        public OcrLayoutResult? Result { get; set; } = new()
        {
            ImageWidth = 1000,
            ImageHeight = 1000,
            Text = "OCR line one\nOCR line two",
            Lines =
            {
                new OcrTextLine { Text = "OCR line one", Bounds = new RectInt(100, 100, 600, 30) },
                new OcrTextLine { Text = "OCR line two", Bounds = new RectInt(100, 150, 600, 30) }
            }
        };

        public bool IsAvailable => true;
        public string? InitErrorMessage => null;

        public Task<OcrLayoutResult?> RecognizeLayoutWithAutoLanguageAsync(byte[] image)
        {
            Calls++;
            return Task.FromResult(Result);
        }

        public Task<string?> RecognizeTextAsync(byte[] image) => Task.FromResult<string?>(null);
        public Task<string?> RecognizeTextAsync(byte[] image, RectInt region) => Task.FromResult<string?>(null);
        public Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image) => Task.FromResult<string?>(null);
        public Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image, RectInt region) => Task.FromResult<string?>(null);
        public bool SetLanguage(string language) => true;
    }

    private sealed class FakePdfService : IPdfService
    {
        public Dictionary<int, IReadOnlyList<PdfTextLine>> Pages { get; } = new();
        public int TextLineCalls { get; private set; }

        public int PageCount => Pages.Count;

        public IReadOnlyList<PdfTextLine> GetTextLines(int pageIndex)
        {
            TextLineCalls++;
            return Pages.TryGetValue(pageIndex, out var lines) ? lines : Array.Empty<PdfTextLine>();
        }

        public void Load(string path) { }
        public void Unload() { }
        public byte[]? RenderPage(int pageIndex, int width, int height) => null;
        public Bitmap? RenderPageBitmap(int pageIndex, int width, int height) => new Bitmap(10, 10);
        public SizeFInfo GetPageSize(int pageIndex) => new(600f, 800f);
        public string GetPdfText(int pageIndex) => string.Empty;
        public int GetPageCount(string pdfPath) => PageCount;
        public string ExtractText(string pdfPath, int pageNumber) => string.Empty;
        public bool Print(bool printDialog = true, int fromPage = 0, int toPage = -1) => false;
        public void Dispose() { }
    }
}

