using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 文档文本索引实现：文本层优先，扫描页渲染后 OCR，按文件指纹缓存到磁盘。
    /// </summary>
    public sealed class PdfDocumentTextIndex : IPdfDocumentTextIndex
    {
        /// <summary>OCR 渲染宽度（与批量导出保持一致）</summary>
        private const int OcrRenderWidth = 1600;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly IPdfService _pdfService;
        private readonly IPdfOcrService? _ocrService;
        private readonly Func<string, IPdfService>? _renderServiceFactory;
        private readonly ILogger<PdfDocumentTextIndex>? _logger;
        private readonly string _indexRootPath;
        private readonly ConcurrentDictionary<string, Task<PdfTextIndexDocument>> _inflightBuilds = new();

        public PdfDocumentTextIndex(
            IPdfService pdfService,
            IPdfOcrService? ocrService = null,
            Func<string, IPdfService>? renderServiceFactory = null,
            ILogger<PdfDocumentTextIndex>? logger = null,
            string? indexRootPath = null)
        {
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _ocrService = ocrService;
            _renderServiceFactory = renderServiceFactory;
            _logger = logger;
            _indexRootPath = indexRootPath ?? Path.Combine(AppPaths.CurrentUserDir, "pdf_text_index");
        }

        /// <inheritdoc />
        public string GetFingerprint(string pdfPath)
        {
            var fullPath = Path.GetFullPath(pdfPath);
            var info = new FileInfo(fullPath);
            if (!info.Exists)
                throw new FileNotFoundException("PDF 文件不存在", fullPath);

            var raw = $"{fullPath.ToLowerInvariant()}|{info.Length}|{info.LastWriteTimeUtc.Ticks}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }

        /// <inheritdoc />
        public PdfTextIndexDocument? TryLoadCached(string pdfPath)
        {
            try
            {
                if (!File.Exists(pdfPath)) return null;

                var fullPath = Path.GetFullPath(pdfPath);
                var fingerprint = GetFingerprint(fullPath);
                var cachePath = BuildCachePath(fullPath, fingerprint);
                if (!File.Exists(cachePath)) return null;

                var json = File.ReadAllText(cachePath, Encoding.UTF8);
                var document = JsonSerializer.Deserialize<PdfTextIndexDocument>(json, JsonOptions);
                if (document == null ||
                    document.Version != PdfDocumentTextIndexVersion.Current ||
                    !string.Equals(document.DocumentPath, fullPath, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(document.Fingerprint, fingerprint, StringComparison.Ordinal) ||
                    document.PageCount <= 0 ||
                    document.Pages.Count != document.PageCount)
                {
                    DeleteFileQuietly(cachePath);
                    return null;
                }

                return document;
            }
            catch (Exception ex)
            {
                // 缓存损坏时不阻塞主流程，直接按未命中处理
                _logger?.LogWarning(ex, "读取 PDF 文本索引缓存失败: {PdfPath}", pdfPath);
                return null;
            }
        }

        /// <inheritdoc />
        public Task<PdfTextIndexDocument> GetOrBuildAsync(
            string pdfPath,
            bool forceRebuild = false,
            IProgress<PdfTextIndexProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            var fullPath = Path.GetFullPath(pdfPath);
            var fingerprint = GetFingerprint(fullPath);

            if (!forceRebuild)
            {
                var cached = TryLoadCached(fullPath);
                if (cached != null)
                {
                    progress?.Report(new PdfTextIndexProgress
                    {
                        ProcessedPages = cached.PageCount,
                        TotalPages = cached.PageCount,
                        FromCache = true,
                        Message = "已命中文本索引缓存"
                    });
                    return Task.FromResult(cached);
                }
            }

            var key = $"{ComputePathHash(fullPath)}|{fingerprint}|{(forceRebuild ? "1" : "0")}";
            var task = _inflightBuilds.GetOrAdd(key, _ => BuildAndSaveAsync(fullPath, fingerprint, progress, cancellationToken));
            return AwaitAndReleaseAsync(key, task);
        }

        /// <inheritdoc />
        public void Invalidate(string pdfPath)
        {
            var fullPath = Path.GetFullPath(pdfPath);
            var prefix = $"{ComputePathHash(fullPath)}_";
            try
            {
                if (!Directory.Exists(_indexRootPath)) return;
                foreach (var file in Directory.EnumerateFiles(_indexRootPath, "*.json"))
                {
                    if (Path.GetFileName(file).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        DeleteFileQuietly(file);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "清理 PDF 文本索引缓存失败: {PdfPath}", pdfPath);
            }
        }

        private async Task<PdfTextIndexDocument> AwaitAndReleaseAsync(string key, Task<PdfTextIndexDocument> task)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            finally
            {
                // 仅移除已完成的同一任务，避免误删后续新建的构建任务
                if (task.IsCompleted)
                {
                    _inflightBuilds.TryRemove(new KeyValuePair<string, Task<PdfTextIndexDocument>>(key, task));
                }
            }
        }

        private async Task<PdfTextIndexDocument> BuildAndSaveAsync(
            string fullPath,
            string fingerprint,
            IProgress<PdfTextIndexProgress>? progress,
            CancellationToken cancellationToken)
        {
            int pageCount = _pdfService.GetPageCount(fullPath);
            if (pageCount <= 0)
                throw new InvalidOperationException("无法读取 PDF 页数，文本索引构建失败");

            var pages = new List<PdfTextIndexPage>(pageCount);
            bool ocrAvailable = _ocrService?.IsAvailable == true && _renderServiceFactory != null;
            IPdfService? renderService = null;

            try
            {
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    string? text = null;
                    var source = PdfTextSource.None;

                    try
                    {
                        text = _pdfService.ExtractText(fullPath, pageIndex + 1);
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogDebug(ex, "提取 PDF 文本层失败: {PdfPath} 第 {Page} 页", fullPath, pageIndex + 1);
                    }

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        source = PdfTextSource.TextLayer;
                    }
                    else if (ocrAvailable)
                    {
                        renderService ??= CreateRenderService(fullPath);
                        text = await OcrPageAsync(renderService, pageIndex).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(text))
                            source = PdfTextSource.Ocr;
                    }

                    text = (text ?? string.Empty).Trim();
                    pages.Add(new PdfTextIndexPage
                    {
                        PageIndex = pageIndex,
                        Text = text,
                        Source = source,
                        DetectedLanguage = StringLanguageDetector.DetectLanguage(text).ToString(),
                        CharacterCount = text.Length
                    });

                    progress?.Report(new PdfTextIndexProgress
                    {
                        PageIndex = pageIndex,
                        ProcessedPages = pageIndex + 1,
                        TotalPages = pageCount,
                        Source = source,
                        Message = $"正在建立文本索引（{pageIndex + 1}/{pageCount}）"
                    });
                }
            }
            finally
            {
                renderService?.Dispose();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var document = new PdfTextIndexDocument
            {
                DocumentPath = fullPath,
                Fingerprint = fingerprint,
                PageCount = pageCount,
                BuiltAtUtc = DateTime.UtcNow,
                Pages = pages
            };

            await SaveIndexAsync(document).ConfigureAwait(false);
            return document;
        }

        private IPdfService CreateRenderService(string pdfPath)
        {
            var service = _renderServiceFactory!(pdfPath);
            // 工厂可能直接返回当前已加载的 PDF 服务，此时不能重复 Load
            if (!ReferenceEquals(service, _pdfService))
            {
                service.Load(pdfPath);
            }
            return service;
        }

        private async Task<string?> OcrPageAsync(IPdfService renderService, int pageIndex)
        {
            try
            {
                var pageSize = renderService.GetPageSize(pageIndex);
                int targetHeight = pageSize.Width > 0
                    ? Math.Max(1, (int)Math.Round(OcrRenderWidth * pageSize.Height / pageSize.Width))
                    : 2000;

                using var bitmap = renderService.RenderPageBitmap(pageIndex, OcrRenderWidth, targetHeight);
                if (bitmap == null) return null;

                using var memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
                // 使用自动语言检测，中英混排文档无需逐页手动切换语言
                return await _ocrService!.RecognizeTextWithAutoLanguageAsync(memoryStream.ToArray()).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // 单页 OCR 失败不影响整份索引，保留空文本继续后续页
                _logger?.LogWarning(ex, "PDF 页面 OCR 失败，跳过该页文本: 第 {Page} 页", pageIndex + 1);
                return null;
            }
        }

        private async Task SaveIndexAsync(PdfTextIndexDocument document)
        {
            try
            {
                AppPaths.EnsureDirectoryExists(_indexRootPath);
                var cachePath = BuildCachePath(document.DocumentPath, document.Fingerprint);
                var tempPath = cachePath + ".tmp";
                var json = JsonSerializer.Serialize(document, JsonOptions);
                await File.WriteAllTextAsync(tempPath, json, new UTF8Encoding(false)).ConfigureAwait(false);
                File.Move(tempPath, cachePath, overwrite: true);
            }
            catch (Exception ex)
            {
                // 缓存写入失败不阻断使用，内存索引仍然可用
                _logger?.LogWarning(ex, "写入 PDF 文本索引缓存失败: {PdfPath}", document.DocumentPath);
            }
        }

        private string BuildCachePath(string fullPath, string fingerprint)
            => Path.Combine(_indexRootPath, $"{ComputePathHash(fullPath)}_{fingerprint}.json");

        private static string ComputePathHash(string fullPath)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fullPath.ToLowerInvariant()));
            return Convert.ToHexString(hash)[..16].ToLowerInvariant();
        }

        private void DeleteFileQuietly(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "删除索引缓存文件失败: {Path}", path);
            }
        }
    }
}
