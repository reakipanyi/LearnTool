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
    /// 共享构建任务与调用者取消隔离：任一调用者取消只停止自己的等待，不中断正在跑的构建。
    /// </summary>
    public sealed class PdfDocumentTextIndex : IPdfDocumentTextIndex
    {
        /// <summary>OCR 渲染宽度（与批量导出保持一致）</summary>
        private const int OcrRenderWidth = 1600;

        /// <summary>断点续建时每处理多少页落一次部分索引</summary>
        private const int PartialSavePageInterval = 20;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = false
        };

        private readonly IPdfService _pdfService;
        private readonly IPdfOcrService? _ocrService;
        private readonly Func<string, IPdfService>? _renderServiceFactory;
        private readonly ILogger<PdfDocumentTextIndex>? _logger;
        private readonly string _indexRootPath;
        private readonly ConcurrentDictionary<string, BuildSession> _inflightBuilds = new();

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

        /// <summary>
        /// 索引构建器身份（OCR 引擎 / 引擎版本 / 当前语言）。任一变化时旧索引与旧 AI 缓存都应失效，
        /// 避免换语言或升级识别引擎后继续复用旧文本。
        /// </summary>
        private string BuilderIdentity
        {
            get
            {
                if (_ocrService == null || !_ocrService.IsAvailable) return "no-ocr";

                var engineName = _ocrService.GetType().Name;
                var engineVersion = _ocrService.GetType().Assembly.GetName().Version?.ToString() ?? "0";
                return $"{engineName}|{engineVersion}|lang:{_ocrService.CurrentLanguage}";
            }
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
                    document.Pages.Count != document.PageCount ||
                    !string.Equals(document.BuilderIdentity, BuilderIdentity, StringComparison.Ordinal))
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
            var session = AcquireSession(key, fullPath, fingerprint);
            return WaitSessionAsync(session, progress, cancellationToken);
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

        /// <summary>
        /// 取得（或创建）共享构建会话。构建任务使用内部令牌，不受任何调用者的取消影响。
        /// </summary>
        private BuildSession AcquireSession(string key, string fullPath, string fingerprint)
        {
            while (true)
            {
                if (_inflightBuilds.TryGetValue(key, out var existing))
                    return existing;

                var session = new BuildSession(key);
                if (!_inflightBuilds.TryAdd(key, session))
                    continue;

                session.Task = BuildAndSaveAsync(fullPath, fingerprint, session, session.Cancellation.Token);
                // 任务结束后移除会话，后续调用重新判断缓存命中
                _ = session.Task.ContinueWith(_ =>
                {
                    _inflightBuilds.TryRemove(new KeyValuePair<string, BuildSession>(key, session));
                }, TaskScheduler.Default);
                return session;
            }
        }

        /// <summary>
        /// 等待共享构建：调用者的取消令牌只停止等待，不取消正在运行的构建任务。
        /// </summary>
        private async Task<PdfTextIndexDocument> WaitSessionAsync(
            BuildSession session,
            IProgress<PdfTextIndexProgress>? progress,
            CancellationToken cancellationToken)
        {
            session.Subscribe(progress);
            try
            {
                // 先补报一次当前进度，避免第二个调用者的进度条一直空白
                session.ReplayLatest(progress);

                return await session.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                session.Unsubscribe(progress);
            }
        }

        private async Task<PdfTextIndexDocument> BuildAndSaveAsync(
            string fullPath,
            string fingerprint,
            BuildSession session,
            CancellationToken cancellationToken)
        {
            int pageCount = _pdfService.GetPageCount(fullPath);
            if (pageCount <= 0)
                throw new InvalidOperationException("无法读取 PDF 页数，文本索引构建失败");

            var partialPath = BuildPartialPath(fullPath, fingerprint);

            // 断点续建：优先加载同一指纹的部分索引，从第一个未处理页继续
            var processed = TryLoadPartialPages(partialPath, fullPath, fingerprint, pageCount);
            int resumedPages = processed.Count;

            bool ocrAvailable = _ocrService?.IsAvailable == true && _renderServiceFactory != null;
            IPdfService? renderService = null;

            var pages = new Dictionary<int, PdfTextIndexPage>(processed);
            int processedSinceSave = 0;

            try
            {
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // 已从部分索引恢复的页直接复用，不再重复 OCR
                    if (pages.ContainsKey(pageIndex))
                    {
                        session.Report(new PdfTextIndexProgress
                        {
                            PageIndex = pageIndex,
                            ProcessedPages = pageIndex + 1,
                            TotalPages = pageCount,
                            Source = pages[pageIndex].Source,
                            Message = $"复用已建索引（{pageIndex + 1}/{pageCount}）"
                        });
                        continue;
                    }

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
                    pages[pageIndex] = new PdfTextIndexPage
                    {
                        PageIndex = pageIndex,
                        Text = text,
                        Source = source,
                        DetectedLanguage = StringLanguageDetector.DetectLanguage(text).ToString(),
                        CharacterCount = text.Length
                    };

                    session.Report(new PdfTextIndexProgress
                    {
                        PageIndex = pageIndex,
                        ProcessedPages = pageIndex + 1,
                        TotalPages = pageCount,
                        Source = source,
                        Message = $"正在建立文本索引（{pageIndex + 1}/{pageCount}）"
                    });

                    // 每 N 页落一次部分索引，取消后重开可续建而不是从头再来
                    processedSinceSave++;
                    if (processedSinceSave >= PartialSavePageInterval)
                    {
                        processedSinceSave = 0;
                        await SavePartialIndexAsync(fullPath, fingerprint, pageCount, pages, partialPath)
                            .ConfigureAwait(false);
                    }
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
                BuilderIdentity = BuilderIdentity,
                IsComplete = true,
                Pages = Enumerable.Range(0, pageCount)
                    .Select(i => pages.TryGetValue(i, out var page) ? page : new PdfTextIndexPage { PageIndex = i })
                    .ToList()
            };

            await SaveDocumentAsync(document, BuildCachePath(fullPath, fingerprint)).ConfigureAwait(false);
            // 构建完整后删除部分索引，避免下次误续建
            DeleteFileQuietly(partialPath);

            if (resumedPages > 0)
                _logger?.LogInformation("PDF 文本索引续建完成: {PdfPath}，复用 {Resumed}/{Total} 页",
                    fullPath, resumedPages, pageCount);

            return document;
        }

        private Dictionary<int, PdfTextIndexPage> TryLoadPartialPages(
            string partialPath,
            string fullPath,
            string fingerprint,
            int pageCount)
        {
            var result = new Dictionary<int, PdfTextIndexPage>();
            try
            {
                if (!File.Exists(partialPath)) return result;

                var json = File.ReadAllText(partialPath, Encoding.UTF8);
                var document = JsonSerializer.Deserialize<PdfTextIndexDocument>(json, JsonOptions);
                if (document == null ||
                    document.Version != PdfDocumentTextIndexVersion.Current ||
                    !string.Equals(document.DocumentPath, fullPath, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(document.Fingerprint, fingerprint, StringComparison.Ordinal) ||
                    !string.Equals(document.BuilderIdentity, BuilderIdentity, StringComparison.Ordinal) ||
                    document.PageCount != pageCount)
                {
                    DeleteFileQuietly(partialPath);
                    return result;
                }

                foreach (var page in document.Pages)
                {
                    if (page.PageIndex >= 0 && page.PageIndex < pageCount)
                        result[page.PageIndex] = page;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取部分索引失败，将从头构建: {PdfPath}", fullPath);
                DeleteFileQuietly(partialPath);
                result.Clear();
            }

            return result;
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

        private Task SavePartialIndexAsync(
            string fullPath,
            string fingerprint,
            int pageCount,
            Dictionary<int, PdfTextIndexPage> pages,
            string partialPath)
        {
            var document = new PdfTextIndexDocument
            {
                DocumentPath = fullPath,
                Fingerprint = fingerprint,
                PageCount = pageCount,
                BuiltAtUtc = DateTime.UtcNow,
                BuilderIdentity = BuilderIdentity,
                IsComplete = false,
                Pages = pages.Values.OrderBy(p => p.PageIndex).ToList()
            };

            return SaveDocumentAsync(document, partialPath);
        }

        private async Task SaveDocumentAsync(PdfTextIndexDocument document, string cachePath)
        {
            try
            {
                AppPaths.EnsureDirectoryExists(_indexRootPath);
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

        private string BuildPartialPath(string fullPath, string fingerprint)
            => Path.Combine(_indexRootPath, $"{ComputePathHash(fullPath)}_{fingerprint}_partial.json");

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

        /// <summary>
        /// 共享构建会话：持有内部取消令牌、构建任务与订阅者列表，进度广播给所有等待者。
        /// </summary>
        private sealed class BuildSession
        {
            private readonly object _gate = new();
            private readonly List<IProgress<PdfTextIndexProgress>> _subscribers = new();
            private PdfTextIndexProgress? _latest;

            public BuildSession(string key)
            {
                Key = key;
            }

            public string Key { get; }
            public CancellationTokenSource Cancellation { get; } = new();
            public Task<PdfTextIndexDocument> Task { get; set; }
                = System.Threading.Tasks.Task.FromResult<PdfTextIndexDocument>(null!);

            public void Subscribe(IProgress<PdfTextIndexProgress>? progress)
            {
                if (progress == null) return;
                lock (_gate) _subscribers.Add(progress);
            }

            public void Unsubscribe(IProgress<PdfTextIndexProgress>? progress)
            {
                if (progress == null) return;
                lock (_gate) _subscribers.Remove(progress);
            }

            /// <summary>新订阅者补报最近一次进度，避免进度界面一直空白</summary>
            public void ReplayLatest(IProgress<PdfTextIndexProgress>? progress)
            {
                if (progress == null) return;

                PdfTextIndexProgress? latest;
                lock (_gate) latest = _latest;
                if (latest != null) progress.Report(latest);
            }

            public void Report(PdfTextIndexProgress progress)
            {
                IProgress<PdfTextIndexProgress>[] subscribers;
                lock (_gate)
                {
                    _latest = progress;
                    subscribers = _subscribers.ToArray();
                }

                foreach (var subscriber in subscribers)
                {
                    try
                    {
                        subscriber.Report(progress);
                    }
                    catch (Exception)
                    {
                        // 订阅者（通常是已关闭的窗体）异常不影响构建
                    }
                }
            }
        }
    }
}
