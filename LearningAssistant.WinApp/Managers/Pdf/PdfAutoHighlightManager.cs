using System.Drawing.Imaging;
using System.Security.Cryptography;
using System.Text;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// PDF「自动高亮」控制器：当前页按需框选、整本按页缓存并懒加载，点击框时复用选区翻译。
    /// </summary>
    public sealed class PdfAutoHighlightManager
    {
        /// <summary>自动高亮版面版本：分块规则调整时递增，使旧缓存/组标识自然失效</summary>
        internal const int AutoLayoutVersion = 1;

        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private readonly IHighlightService _highlightService;
        private readonly PdfReaderHighlightManager _highlightManager;
        private readonly IPdfAutoLayoutService? _autoLayoutService;

        /// <summary>正在惰性落库的页，避免翻页/双页显示时重复生成</summary>
        private readonly HashSet<int> _materializingPages = new();

        private CancellationTokenSource? _wholeBookCts;

        public PdfAutoHighlightManager(
            ILogger logger,
            IPdfReaderFormAccess form,
            IHighlightService highlightService,
            PdfReaderHighlightManager highlightManager,
            IPdfAutoLayoutService? autoLayoutService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            _highlightManager = highlightManager ?? throw new ArgumentNullException(nameof(highlightManager));
            _autoLayoutService = autoLayoutService;
        }

        /// <summary>是否正在生成当前页框选，避免重复点击。</summary>
        public bool IsBusy { get; private set; }

        /// <summary>是否正在执行整本自动框选。</summary>
        public bool IsWholeBookRunning { get; private set; }

        /// <summary>整本任务是否支持 OCR（决定右键菜单文案与缓存策略）。</summary>
        public bool CanUseWholeBook => _autoLayoutService != null;

        /// <summary>当前可见页是否已有自动高亮（双页模式检查两页）。</summary>
        public bool HasAutoHighlightsForVisiblePages()
        {
            return GetVisiblePages().Any(page => GetAutoHighlights(page).Count > 0);
        }

        /// <summary>
        /// 切换当前页自动高亮：已有则清除，没有则生成。双页模式同时处理两页。
        /// </summary>
        public async Task ToggleVisiblePagesAsync()
        {
            if (IsBusy || IsWholeBookRunning) return;

            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            var pages = GetVisiblePages();
            if (pages.Count == 0) return;

            IsBusy = true;
            try
            {
                if (pages.Any(page => GetAutoHighlights(page).Count > 0))
                {
                    await ClearPagesAsync(pdfPath, pages);
                    return;
                }

                await BuildPagesAsync(pdfPath, pages);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>清除当前可见页的自动高亮。</summary>
        public async Task ClearVisiblePagesAsync()
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            await ClearPagesAsync(pdfPath, GetVisiblePages());
        }

        /// <summary>
        /// 整本自动框选：逐页生成版面缓存；完成后只把当前可见页转成高亮，其余页浏览时懒加载。
        /// </summary>
        /// <param name="allowOcr">无文本层的页是否使用 OCR（整本扫描件）</param>
        /// <param name="progress">逐页进度回调</param>
        public async Task BuildWholeDocumentAsync(
            bool allowOcr,
            IProgress<PdfAutoLayoutProgress>? progress = null)
        {
            if (IsBusy || IsWholeBookRunning) return;

            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            if (_autoLayoutService == null)
            {
                _form.ShowWarning("整本自动框选服务未初始化");
                return;
            }

            IsWholeBookRunning = true;
            _wholeBookCts = new CancellationTokenSource();
            try
            {
                var state = _autoLayoutService.LoadState(pdfPath);
                state.WholeBookEnabled = true;
                state.SuppressedPages.Clear();
                _autoLayoutService.SaveState(pdfPath, state);

                _form.ShowToast(allowOcr
                    ? "开始整本框选（含扫描页 OCR）..."
                    : "开始整本框选（文本层）...");

                await _autoLayoutService.BuildDocumentAsync(
                    pdfPath, allowOcr, progress, _wholeBookCts.Token);

                foreach (var page in GetVisiblePages())
                    await EnsurePageMaterializedAsync(page);

                _form.ShowToast("整本自动框选已就绪，浏览页面时自动加载");
            }
            catch (OperationCanceledException)
            {
                _form.ShowToast("已取消整本自动框选，已完成页会保留");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "整本自动框选失败");
                _form.ShowWarning("整本自动框选失败，请稍后重试");
            }
            finally
            {
                IsWholeBookRunning = false;
                _wholeBookCts?.Dispose();
                _wholeBookCts = null;
            }
        }

        /// <summary>取消正在执行的整本任务；已完成的页面缓存会保留。</summary>
        public void CancelWholeDocument()
        {
            try
            {
                _wholeBookCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 任务恰好结束时忽略
            }
        }

        /// <summary>清除本文档所有自动高亮，并删除整本版面缓存与状态。</summary>
        public async Task ClearWholeDocumentAsync()
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            if (IsWholeBookRunning)
                CancelWholeDocument();

            await Task.Run(() => _highlightService.RemoveAllAutoHighlights(pdfPath));
            _autoLayoutService?.ClearDocument(pdfPath);

            _highlightManager.RefreshHighlightList();
            foreach (var page in GetVisiblePages())
                _highlightManager.RefreshHighlightLayerForPage(page);
            _highlightManager.UpdateSecondHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
            _form.ShowToast("已清除整本自动框选");
        }

        /// <summary>
        /// 页面显示后调用：整本模式开启时，把该页已缓存的版面懒加载为自动高亮。
        /// </summary>
        public async Task EnsurePageMaterializedAsync(int pageIndex)
        {
            if (_autoLayoutService == null || pageIndex < 0) return;

            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;
            if (!_materializingPages.Add(pageIndex)) return;

            try
            {
                var state = _autoLayoutService.LoadState(pdfPath);
                if (!state.WholeBookEnabled || state.SuppressedPages.Contains(pageIndex))
                    return;

                if (GetAutoHighlights(pageIndex).Count > 0)
                    return;

                var layout = await Task.Run(() => _autoLayoutService.TryGetCachedPage(pdfPath, pageIndex));
                if (layout == null || layout.Blocks.Count == 0)
                    return;

                var highlights = CreateHighlights(pdfPath, pageIndex, layout.Blocks);
                await _highlightService.AddHighlightsAsync(pdfPath, highlights);
                RefreshAfterChange(new[] { pageIndex });
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "懒加载第 {Page} 页自动框选失败", pageIndex);
            }
            finally
            {
                _materializingPages.Remove(pageIndex);
            }
        }

        /// <summary>
        /// 点击自动框时翻译该框：已有译文或本页缓存则直接复用，不重复请求翻译服务。
        /// </summary>
        public async Task HandleHighlightSelectedAsync(PdfHighlight? highlight)
        {
            if (highlight == null || !highlight.IsAutoGenerated)
                return;

            if (string.IsNullOrWhiteSpace(highlight.Text))
                return;

            if (!string.IsNullOrWhiteSpace(highlight.TranslationText))
                return;

            if (_form.TryGetCachedSelectionTranslation(highlight.PageIndex, highlight.Text, out var cached)
                && !string.IsNullOrWhiteSpace(cached))
            {
                return;
            }

            var pdfPath = string.IsNullOrWhiteSpace(highlight.PdfPath) ? _form.CurrentPdfPath : highlight.PdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return;

            try
            {
                await _highlightManager.TranslateHighlightAsync(pdfPath, highlight, highlight.Text);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "翻译自动高亮失败: page {Page}", highlight.PageIndex);
            }
        }

        /// <summary>取当前可见页：双页模式为当前页与下一页。</summary>
        public IReadOnlyList<int> GetVisiblePages()
        {
            var page = Math.Max(0, _form.CurrentPageIndex);
            if (_form.IsDualPage)
                return new[] { page, page + 1 };
            return new[] { page };
        }

        /// <summary>判断指定页是否已有自动高亮。</summary>
        public bool HasAutoHighlights(int pageIndex)
        {
            return GetAutoHighlights(pageIndex).Count > 0;
        }

        private List<PdfHighlight> GetAutoHighlights(int pageIndex)
        {
            var pdfPath = _form.CurrentPdfPath;
            if (string.IsNullOrWhiteSpace(pdfPath)) return new List<PdfHighlight>();

            return _highlightService
                .GetHighlightsForPage(pdfPath, pageIndex)
                .Where(h => h.IsAutoGenerated)
                .ToList();
        }

        private async Task ClearPagesAsync(string pdfPath, IReadOnlyList<int> pages)
        {
            if (pages.Count == 0) return;

            await Task.Run(() =>
            {
                foreach (var page in pages)
                {
                    _highlightService.RemoveAutoHighlightsForPage(pdfPath, page);
                    _autoLayoutService?.RemovePageCache(pdfPath, page);
                }
            });

            // 整本模式下，被手动清除的页记录为抑制页，翻页回来不再自动恢复
            if (_autoLayoutService != null)
            {
                var state = _autoLayoutService.LoadState(pdfPath);
                if (state.WholeBookEnabled)
                {
                    foreach (var page in pages)
                        state.SuppressedPages.Add(page);
                    _autoLayoutService.SaveState(pdfPath, state);
                }
            }

            RefreshAfterChange(pages);
            _form.ShowToast("已清除本页自动框选");
        }

        private async Task BuildPagesAsync(string pdfPath, IReadOnlyList<int> pages)
        {
            int added = 0;
            int skipped = 0;
            string? firstWarning = null;

            foreach (var page in pages)
            {
                var result = await BuildLayoutAsync(pdfPath, page, allowOcr: true);
                if (result.Layout.Blocks.Count == 0)
                {
                    skipped++;
                    firstWarning ??= result.Warning;
                    continue;
                }

                var highlights = CreateHighlights(pdfPath, page, result.Layout.Blocks);
                await _highlightService.AddHighlightsAsync(pdfPath, highlights);
                added += highlights.Count;
            }

            // 单页重新生成时，从整本模式的抑制页集合中移除
            if (added > 0 && _autoLayoutService != null)
            {
                var state = _autoLayoutService.LoadState(pdfPath);
                if (state.WholeBookEnabled)
                {
                    bool changed = false;
                    foreach (var page in pages)
                        changed |= state.SuppressedPages.Remove(page);
                    if (changed)
                        _autoLayoutService.SaveState(pdfPath, state);
                }
            }

            RefreshAfterChange(pages);

            if (added > 0)
            {
                _form.ShowToast(skipped > 0
                    ? $"已自动框选 {added} 段；{skipped} 页未找到文字"
                    : $"已自动框选 {added} 段");
                return;
            }

            _form.ShowWarning(firstWarning ?? "未找到可框选的文字区域。扫描页请确认 OCR 语言包已安装。");
        }

        private void RefreshAfterChange(IReadOnlyList<int> pages)
        {
            _highlightManager.RefreshHighlightList();
            foreach (var page in pages)
            {
                if (page == _form.CurrentPageIndex)
                    _highlightManager.RefreshHighlightLayerForPage(page);
            }
            _highlightManager.UpdateSecondHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
        }

        private List<PdfHighlight> CreateHighlights(
            string pdfPath,
            int pageIndex,
            IReadOnlyList<PdfAutoTextBlock> blocks)
        {
            var groupId = BuildAutoGroupId(pdfPath, pageIndex);
            var result = new List<PdfHighlight>(blocks.Count);

            foreach (var block in blocks)
            {
                result.Add(new PdfHighlight
                {
                    PdfPath = pdfPath,
                    PageIndex = pageIndex,
                    NormalizedX = block.X,
                    NormalizedY = block.Y,
                    NormalizedWidth = block.Width,
                    NormalizedHeight = block.Height,
                    Text = block.Text,
                    Color = HighlightColor.Blue,
                    CreatedAt = DateTime.Now,
                    IsAutoGenerated = true,
                    AutoGroupId = groupId
                });
            }

            return result;
        }

        /// <summary>自动组标识：同一文档同一页在同一版面版本下保持稳定。</summary>
        internal static string BuildAutoGroupId(string pdfPath, int pageIndex)
        {
            var raw = $"{pdfPath.ToLowerInvariant()}|{pageIndex}|v{AutoLayoutVersion}";
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
            return "auto-" + Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
        }

        private async Task<PageLayoutResult> BuildLayoutAsync(
            string pdfPath,
            int pageIndex,
            bool allowOcr)
        {
            if (_autoLayoutService != null)
            {
                try
                {
                    var page = await _autoLayoutService.GetOrBuildPageAsync(
                        pdfPath, pageIndex, allowOcr);
                    return new PageLayoutResult(page, null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "生成第 {Page} 页自动框选版面失败", pageIndex);
                }
            }

            // 服务不可用时的降级路径：仅处理当前可见页
            return await BuildLayoutInlineAsync(pageIndex, allowOcr);
        }

        private async Task<PageLayoutResult> BuildLayoutInlineAsync(int pageIndex, bool allowOcr)
        {
            var presenter = _form.Presenter;
            if (presenter == null)
                return new PageLayoutResult(new PdfAutoLayoutPage { PageIndex = pageIndex }, "PDF 尚未加载");

            IReadOnlyList<PdfTextLine> textLines;
            try
            {
                textLines = await Task.Run(() => presenter.GetPageTextLines(pageIndex));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "读取文字层行失败: page {Page}", pageIndex);
                textLines = Array.Empty<PdfTextLine>();
            }

            if (textLines.Count > 0)
            {
                var blocks = PdfTextBlockBuilder.Build(textLines);
                return new PageLayoutResult(new PdfAutoLayoutPage
                {
                    PageIndex = pageIndex,
                    Source = PdfTextLineSource.TextLayer,
                    Blocks = blocks.ToList()
                }, null);
            }

            if (!allowOcr)
            {
                return new PageLayoutResult(
                    new PdfAutoLayoutPage { PageIndex = pageIndex, Source = PdfTextLineSource.None },
                    null);
            }

            // 无文本层时回退到 OCR 行框（扫描页）
            if (!presenter.IsOcrAvailable())
            {
                return new PageLayoutResult(
                    new PdfAutoLayoutPage { PageIndex = pageIndex, Source = PdfTextLineSource.None },
                    presenter.OcrInitErrorMessage ?? "当前 PDF 没有文本层，且 OCR 不可用");
            }

            var pageImage = GetVisiblePageImage(pageIndex);
            if (pageImage == null)
            {
                return new PageLayoutResult(
                    new PdfAutoLayoutPage { PageIndex = pageIndex, Source = PdfTextLineSource.None },
                    "当前页图像尚未就绪，请稍后重试");
            }

            byte[]? pngBytes = ToPngBytes(pageImage);
            if (pngBytes == null)
            {
                return new PageLayoutResult(
                    new PdfAutoLayoutPage { PageIndex = pageIndex, Source = PdfTextLineSource.None },
                    "无法读取当前页图像");
            }

            OcrLayoutResult? ocrLayout;
            try
            {
                ocrLayout = await presenter.RecognizeLayoutWithAutoLanguageAsync(pngBytes);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR 行框识别失败: page {Page}", pageIndex);
                ocrLayout = null;
            }

            if (ocrLayout == null || ocrLayout.ImageWidth <= 0 || ocrLayout.ImageHeight <= 0)
            {
                return new PageLayoutResult(
                    new PdfAutoLayoutPage { PageIndex = pageIndex, Source = PdfTextLineSource.None },
                    "OCR 识别失败，请检查语言包或稍后重试");
            }

            var ocrLines = new List<PdfTextLine>(ocrLayout.Lines.Count);
            foreach (var line in ocrLayout.Lines)
            {
                var bounds = line.Bounds;
                if (bounds.Width <= 0 || bounds.Height <= 0) continue;

                float x = (float)bounds.X / ocrLayout.ImageWidth;
                float y = (float)bounds.Y / ocrLayout.ImageHeight;
                float width = (float)bounds.Width / ocrLayout.ImageWidth;
                float height = (float)bounds.Height / ocrLayout.ImageHeight;

                if (x < -0.02f || y < -0.02f || x > 1.02f || y > 1.02f) continue;

                ocrLines.Add(new PdfTextLine
                {
                    Text = line.Text,
                    X = Math.Clamp(x, 0f, 1f),
                    Y = Math.Clamp(y, 0f, 1f),
                    Width = Math.Min(width, 1f - Math.Clamp(x, 0f, 1f)),
                    Height = Math.Min(height, 1f - Math.Clamp(y, 0f, 1f)),
                    FontSize = bounds.Height
                });
            }

            var ocrBlocks = PdfTextBlockBuilder.Build(ocrLines);
            return new PageLayoutResult(new PdfAutoLayoutPage
            {
                PageIndex = pageIndex,
                Source = PdfTextLineSource.Ocr,
                Blocks = ocrBlocks.ToList()
            }, null);
        }

        private Bitmap? GetVisiblePageImage(int pageIndex)
        {
            if (pageIndex == _form.CurrentPageIndex)
                return _form.CurrentPageImage;

            if (_form.IsDualPage && pageIndex == _form.CurrentPageIndex + 1)
                return _form.SecondPageImage;

            return null;
        }

        private static byte[]? ToPngBytes(Bitmap? bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                using var ms = new MemoryStream();
                bitmap.Save(ms, ImageFormat.Png);
                return ms.ToArray();
            }
            catch
            {
                return null;
            }
        }

        private readonly record struct PageLayoutResult(PdfAutoLayoutPage Layout, string? Warning);
    }
}
