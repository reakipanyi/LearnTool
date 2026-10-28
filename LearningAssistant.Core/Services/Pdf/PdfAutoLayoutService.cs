using System.Drawing;
using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 自动框选版面服务默认实现：文本层优先、扫描页可选 OCR，按页落盘并支持整本断点续跑。
    /// </summary>
    public sealed class PdfAutoLayoutService : IPdfAutoLayoutService
    {
        /// <summary>OCR 渲染宽度：固定宽度兼顾识别质量与耗时</summary>
        private const int OcrRenderWidth = 1600;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true
        };

        private readonly IPdfService _pdfService;
        private readonly IPdfOcrService? _ocrService;
        private readonly IPdfDocumentTextIndex? _textIndex;
        private readonly ILogger<PdfAutoLayoutService>? _logger;
        private readonly string _rootDir;
        private readonly SemaphoreSlim _buildGate = new(1, 1);

        public PdfAutoLayoutService(
            IPdfService pdfService,
            IPdfOcrService? ocrService = null,
            IPdfDocumentTextIndex? textIndex = null,
            ILogger<PdfAutoLayoutService>? logger = null,
            string? rootDir = null)
        {
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _ocrService = ocrService;
            _textIndex = textIndex;
            _logger = logger;
            _rootDir = string.IsNullOrWhiteSpace(rootDir) ? AppPaths.PdfAutoLayoutDir : rootDir;

            try
            {
                Directory.CreateDirectory(_rootDir);
            }
            catch
            {
                // 目录创建失败时后续读写会走异常分支并降级为内存结果
            }
        }

        public string GetFingerprint(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath)) return "missing";

            try
            {
                if (_textIndex != null)
                    return _textIndex.GetFingerprint(pdfPath);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "读取文档指纹失败，改用文件信息: {Path}", pdfPath);
            }

            try
            {
                var info = new FileInfo(pdfPath);
                return info.Exists ? $"{info.Length}_{info.LastWriteTimeUtc.Ticks}" : "missing";
            }
            catch
            {
                return "missing";
            }
        }

        public PdfAutoLayoutPage? TryGetCachedPage(string pdfPath, int pageIndex)
        {
            if (pageIndex < 0) return null;

            var file = GetPageFilePath(pdfPath, pageIndex);
            if (!File.Exists(file)) return null;

            try
            {
                var json = File.ReadAllText(file, Encoding.UTF8);
                var page = JsonSerializer.Deserialize<PdfAutoLayoutPage>(json, JsonOptions);
                if (page == null || page.PageIndex != pageIndex) return null;
                return page;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "读取自动框选版面缓存失败: {File}", file);
                return null;
            }
        }

        public async Task<PdfAutoLayoutPage> GetOrBuildPageAsync(
            string pdfPath,
            int pageIndex,
            bool allowOcr,
            bool forceRebuild = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!forceRebuild && TryGetCachedPage(pdfPath, pageIndex) is { } cached)
                return cached;

            await _buildGate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (!forceRebuild && TryGetCachedPage(pdfPath, pageIndex) is { } cachedAgain)
                    return cachedAgain;

                var page = await BuildPageCoreAsync(pdfPath, pageIndex, allowOcr, cancellationToken)
                    .ConfigureAwait(false);

                SavePage(pdfPath, pageIndex, page);
                return page;
            }
            finally
            {
                _buildGate.Release();
            }
        }

        public async Task BuildDocumentAsync(
            string pdfPath,
            bool allowOcr,
            IProgress<PdfAutoLayoutProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            int totalPages;
            try
            {
                totalPages = _pdfService.PageCount;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取 PDF 页数失败: {Path}", pdfPath);
                return;
            }

            for (int pageIndex = 0; pageIndex < totalPages; pageIndex++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                PdfAutoLayoutPage page;
                try
                {
                    page = await GetOrBuildPageAsync(
                            pdfPath, pageIndex, allowOcr, forceRebuild: false, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "整本自动框选失败: page {Page}", pageIndex);
                    page = new PdfAutoLayoutPage
                    {
                        PageIndex = pageIndex,
                        Source = PdfTextLineSource.None
                    };
                }

                progress?.Report(new PdfAutoLayoutProgress
                {
                    CurrentPage = pageIndex,
                    TotalPages = totalPages,
                    Source = page.Source,
                    Skipped = page.Blocks.Count == 0
                });
            }
        }

        public PdfAutoLayoutState LoadState(string pdfPath)
        {
            var fingerprint = GetFingerprint(pdfPath);
            var file = GetStateFilePath(pdfPath);
            if (!File.Exists(file))
                return new PdfAutoLayoutState { Fingerprint = fingerprint };

            try
            {
                var json = File.ReadAllText(file, Encoding.UTF8);
                var state = JsonSerializer.Deserialize<PdfAutoLayoutState>(json, JsonOptions);
                if (state == null || !string.Equals(state.Fingerprint, fingerprint, StringComparison.Ordinal))
                    return new PdfAutoLayoutState { Fingerprint = fingerprint };

                state.SuppressedPages ??= new HashSet<int>();
                return state;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "读取自动框选状态失败: {File}", file);
                return new PdfAutoLayoutState { Fingerprint = fingerprint };
            }
        }

        public void SaveState(string pdfPath, PdfAutoLayoutState state)
        {
            if (state == null) return;

            var fingerprint = GetFingerprint(pdfPath);
            state.Fingerprint = fingerprint;
            state.SuppressedPages ??= new HashSet<int>();
            state.UpdatedAtUtc = DateTime.UtcNow;

            try
            {
                WriteAllTextAtomic(GetStateFilePath(pdfPath), JsonSerializer.Serialize(state, JsonOptions));
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "保存自动框选状态失败: {Path}", pdfPath);
            }
        }

        public void RemovePageCache(string pdfPath, int pageIndex)
        {
            try
            {
                var file = GetPageFilePath(pdfPath, pageIndex);
                if (File.Exists(file)) File.Delete(file);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "删除自动框选页面缓存失败: page {Page}", pageIndex);
            }
        }

        public void ClearDocument(string pdfPath)
        {
            try
            {
                var prefix = GetPathKey(pdfPath) + "_";
                foreach (var file in Directory.EnumerateFiles(_rootDir, prefix + "*.json"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // 单个文件被占用时继续清理其余文件
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "清理自动框选缓存失败: {Path}", pdfPath);
            }
        }

        private async Task<PdfAutoLayoutPage> BuildPageCoreAsync(
            string pdfPath,
            int pageIndex,
            bool allowOcr,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<PdfTextLine> textLines;
            try
            {
                textLines = await Task.Run(() => _pdfService.GetTextLines(pageIndex), cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "读取文字层失败: page {Page}", pageIndex);
                textLines = Array.Empty<PdfTextLine>();
            }

            var blocks = PdfTextBlockBuilder.Build(textLines);
            if (textLines.Count > 0)
            {
                return new PdfAutoLayoutPage
                {
                    PageIndex = pageIndex,
                    Source = PdfTextLineSource.TextLayer,
                    Blocks = blocks.ToList()
                };
            }

            if (!allowOcr || _ocrService == null || !_ocrService.IsAvailable)
            {
                return new PdfAutoLayoutPage
                {
                    PageIndex = pageIndex,
                    Source = PdfTextLineSource.None
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            byte[]? pngBytes = await RenderPagePngAsync(pageIndex, cancellationToken).ConfigureAwait(false);
            if (pngBytes == null)
            {
                return new PdfAutoLayoutPage
                {
                    PageIndex = pageIndex,
                    Source = PdfTextLineSource.None
                };
            }

            cancellationToken.ThrowIfCancellationRequested();

            OcrLayoutResult? ocrLayout;
            try
            {
                ocrLayout = await _ocrService.RecognizeLayoutWithAutoLanguageAsync(pngBytes)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "OCR 行框识别失败: page {Page}", pageIndex);
                ocrLayout = null;
            }

            var ocrLines = ConvertOcrLines(ocrLayout);
            var ocrBlocks = PdfTextBlockBuilder.Build(ocrLines);

            return new PdfAutoLayoutPage
            {
                PageIndex = pageIndex,
                Source = ocrLayout == null ? PdfTextLineSource.None : PdfTextLineSource.Ocr,
                Blocks = ocrBlocks.ToList()
            };
        }

        private async Task<byte[]?> RenderPagePngAsync(int pageIndex, CancellationToken cancellationToken)
        {
            try
            {
                var size = _pdfService.GetPageSize(pageIndex);
                if (size.Width <= 0 || size.Height <= 0)
                    return null;

                int width = OcrRenderWidth;
                int height = Math.Clamp(
                    (int)Math.Round(width * size.Height / size.Width),
                    200,
                    5000);

                using var bitmap = await Task.Run(
                        () => _pdfService.RenderPageBitmap(pageIndex, width, height),
                        cancellationToken)
                    .ConfigureAwait(false);

                if (bitmap == null) return null;

                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "渲染 OCR 页面失败: page {Page}", pageIndex);
                return null;
            }
        }

        private static List<PdfTextLine> ConvertOcrLines(OcrLayoutResult? ocrLayout)
        {
            var result = new List<PdfTextLine>();
            if (ocrLayout == null || ocrLayout.ImageWidth <= 0 || ocrLayout.ImageHeight <= 0)
                return result;

            foreach (var line in ocrLayout.Lines)
            {
                var bounds = line.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) continue;

                float x = (float)bounds.X / ocrLayout.ImageWidth;
                float y = (float)bounds.Y / ocrLayout.ImageHeight;
                float width = (float)bounds.Width / ocrLayout.ImageWidth;
                float height = (float)bounds.Height / ocrLayout.ImageHeight;

                if (x < -0.02f || y < -0.02f || x > 1.02f || y > 1.02f) continue;

                result.Add(new PdfTextLine
                {
                    Text = line.Text,
                    X = Math.Clamp(x, 0f, 1f),
                    Y = Math.Clamp(y, 0f, 1f),
                    Width = Math.Min(width, 1f - Math.Clamp(x, 0f, 1f)),
                    Height = Math.Min(height, 1f - Math.Clamp(y, 0f, 1f)),
                    FontSize = bounds.Height
                });
            }

            return result;
        }

        private void SavePage(string pdfPath, int pageIndex, PdfAutoLayoutPage page)
        {
            try
            {
                var json = JsonSerializer.Serialize(page, JsonOptions);
                WriteAllTextAtomic(GetPageFilePath(pdfPath, pageIndex), json);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "保存自动框选页面缓存失败: page {Page}", pageIndex);
            }
        }

        private void WriteAllTextAtomic(string file, string content)
        {
            var directory = Path.GetDirectoryName(file);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temp = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temp, content, Encoding.UTF8);
            File.Move(temp, file, overwrite: true);
        }

        private string GetPageFilePath(string pdfPath, int pageIndex)
        {
            var fingerprint = GetFingerprint(pdfPath);
            return Path.Combine(
                _rootDir,
                $"{GetPathKey(pdfPath)}_{HashKey(fingerprint)}_p{pageIndex}.json");
        }

        private string GetStateFilePath(string pdfPath)
        {
            var fingerprint = GetFingerprint(pdfPath);
            return Path.Combine(
                _rootDir,
                $"{GetPathKey(pdfPath)}_{HashKey(fingerprint)}_state.json");
        }

        private static string GetPathKey(string pdfPath)
        {
            return HashKey(pdfPath.ToLowerInvariant());
        }

        private static string HashKey(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
            return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }
    }
}
