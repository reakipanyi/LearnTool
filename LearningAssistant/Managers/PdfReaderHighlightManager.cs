using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public class PdfReaderHighlightManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IHighlightService _highlightService;
        private readonly Stack<HighlightUndoAction> _undoStack = new Stack<HighlightUndoAction>();
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;
        private readonly IAnnotationService? _annotationService;
        private readonly IEventBus? _eventBus;
        private readonly IAppPaths? _appPaths;

        private Bitmap? _highlightBitmap;
        private Graphics? _highlightGraphics;

        private Bitmap? _secondHighlightBitmap;
        private Graphics? _secondHighlightGraphics;

        public HighlightColor CurrentHighlightColor { get; set; } = HighlightColor.Yellow;
        public bool IsHighlightMode { get; set; } = true;

        /// <summary>
        /// 当一个高亮撤销动作被压入内部撤销栈时触发。
        /// 订阅方（如主窗体）可据此把该操作类型记录到统一撤销栈中，
        /// 以便工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
        /// </summary>
        public event EventHandler? UndoActionRecorded;

        public PdfReaderHighlightManager(ILogger logger, IPdfReaderFormAccess form, IHighlightService highlightService,
            IAnnotationService? annotationService = null, IEventBus? eventBus = null, IAppPaths? appPaths = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            _annotationService = annotationService;
            _eventBus = eventBus;
            _appPaths = appPaths;
        }

        public void UpdateHighlightLayer()
        {
            try
            {
                if (_form.CurrentPageImage == null)
                {
                    _logger.LogWarning("UpdateHighlightLayer: CurrentPageImage is null");
                    CleanupHighlightLayer();
                    return;
                }

                int imgWidth = _form.CurrentPageImage.Width;
                int imgHeight = _form.CurrentPageImage.Height;

                _logger.LogInformation("UpdateHighlightLayer: imgSize={Width}x{Height}, CurrentPdfPath={Path}, CurrentPageIndex={Page}",
                    imgWidth, imgHeight, _form.CurrentPdfPath, _form.CurrentPageIndex);

                bool needsRecreate = false;
                if (_highlightBitmap != null)
                {
                    try
                    {
                        if (_highlightBitmap.Width != imgWidth || _highlightBitmap.Height != imgHeight)
                        {
                            needsRecreate = true;
                            CleanupHighlightLayer();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        needsRecreate = true;
                        CleanupHighlightLayer();
                    }
                }

                if (_highlightBitmap == null || needsRecreate)
                {
                    _highlightBitmap = new Bitmap(imgWidth, imgHeight);
                    _highlightGraphics = Graphics.FromImage(_highlightBitmap);
                    _highlightGraphics.Clear(Color.Transparent);
                }

                _highlightGraphics!.Clear(Color.Transparent);

                var highlights = _highlightService.GetHighlightsForPage(_form.CurrentPdfPath, _form.CurrentPageIndex);
                _logger.LogInformation("UpdateHighlightLayer: GetHighlightsForPage returned {Count} highlights", highlights.Count);

                foreach (var highlight in highlights)
                {
                    DrawHighlight(highlight, imgWidth, imgHeight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateHighlightLayer");
                CleanupHighlightLayer();
            }
        }

        /// <summary>
        /// 预渲染双页模式下第二页的高亮图层缓存，避免每帧 Paint 时实时绘制造成卡顿。
        /// </summary>
        public void UpdateSecondHighlightLayer()
        {
            var secondPageImage = _form.SecondPageImage;
            if (secondPageImage == null) return;
            UpdateSecondHighlightLayer(_form.CurrentPageIndex + 1, secondPageImage);
        }

        public void UpdateSecondHighlightLayer(int pageIndex, Bitmap secondPageImage)
        {
            if (secondPageImage == null) return;

            try
            {
                int imgWidth = secondPageImage.Width;
                int imgHeight = secondPageImage.Height;

                bool needsRecreate = false;
                if (_secondHighlightBitmap != null)
                {
                    try
                    {
                        if (_secondHighlightBitmap.Width != imgWidth || _secondHighlightBitmap.Height != imgHeight)
                        {
                            needsRecreate = true;
                            CleanupSecondHighlightLayer();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        needsRecreate = true;
                        CleanupSecondHighlightLayer();
                    }
                }

                if (_secondHighlightBitmap == null || needsRecreate)
                {
                    _secondHighlightBitmap = new Bitmap(imgWidth, imgHeight);
                    _secondHighlightGraphics = Graphics.FromImage(_secondHighlightBitmap);
                    _secondHighlightGraphics.Clear(Color.Transparent);
                }

                _secondHighlightGraphics!.Clear(Color.Transparent);

                var highlights = _highlightService.GetHighlightsForPage(_form.CurrentPdfPath, pageIndex);

                foreach (var highlight in highlights)
                {
                    DrawHighlightToGraphics(_secondHighlightGraphics, highlight, imgWidth, imgHeight);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateSecondHighlightLayer");
                CleanupSecondHighlightLayer();
            }
        }

        public void CleanupSecondHighlightLayer()
        {
            try
            {
                _secondHighlightGraphics?.Dispose();
                _secondHighlightBitmap?.Dispose();
                _secondHighlightGraphics = null;
                _secondHighlightBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up second highlight layer");
            }
        }

        private void DrawHighlight(PdfHighlight highlight, int imgWidth, int imgHeight)
        {
            DrawHighlightToGraphics(_highlightGraphics!, highlight, imgWidth, imgHeight);
        }

        private static void DrawHighlightToGraphics(Graphics g, PdfHighlight highlight, int imgWidth, int imgHeight)
        {
            var colorInfo = HighlightService.GetHighlightColor(highlight.Color);
            var color = Color.FromArgb(colorInfo.A, colorInfo.R, colorInfo.G, colorInfo.B);

            float x, y, width, height;
            if (highlight.NormalizedWidth > 0)
            {
                x = highlight.NormalizedX * imgWidth;
                y = highlight.NormalizedY * imgHeight;
                width = highlight.NormalizedWidth * imgWidth;
                height = highlight.NormalizedHeight * imgHeight;
            }
            else
            {
                x = highlight.X;
                y = highlight.Y;
                width = highlight.Width;
                height = highlight.Height;
            }

            var rect = new RectangleF(x, y, width, height);

            if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0)
                return;

            DrawHighlightFill(g, color, rect);

            if (!string.IsNullOrEmpty(highlight.Note))
            {
                using var font = new Font("Microsoft YaHei UI", 10F);
                using var textBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
                g.DrawString("📝", font, textBrush, rect.Location);
            }
        }

        private static void DrawHighlightFill(Graphics g, Color color, RectangleF rect)
        {
            // 荧光笔效果：半透明纯色填充，无硬边框，保留文字可见性
            using var fillBrush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
            g.FillRectangle(fillBrush, rect);

            // 极淡的内发光边框（仅用于视觉界定区域，不喧宾夺主）
            using var innerPen = new Pen(Color.FromArgb(Math.Min(80, color.A + 10), color.R, color.G, color.B), 1f);
            g.DrawRectangle(innerPen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public void CleanupHighlightLayer()
        {
            try
            {
                _highlightGraphics?.Dispose();
                _highlightBitmap?.Dispose();
                _highlightGraphics = null;
                _highlightBitmap = null;

                _secondHighlightGraphics?.Dispose();
                _secondHighlightBitmap?.Dispose();
                _secondHighlightGraphics = null;
                _secondHighlightBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up highlight layer");
            }
        }

        public void AddHighlight(Rectangle selectionRect)
        {
            _logger.LogInformation("AddHighlight called with rect: {X},{Y} {Width}x{Height}",
                selectionRect.X, selectionRect.Y, selectionRect.Width, selectionRect.Height);
            _ = AddHighlightFromSelectionAsync(selectionRect);
        }

        private async Task AddHighlightFromSelectionAsync(Rectangle selectionRect)
        {
            Bitmap? pageImageCopy = null;
            int targetPageIndex = 0;
            Rectangle imgRect = Rectangle.Empty;
            string currentPdfPath = string.Empty;
            HighlightColor currentHighlightColor = CurrentHighlightColor;

            try
            {
                // 在异步操作开始前捕获所有必要状态，避免竞态条件
                currentPdfPath = _form.CurrentPdfPath;
                
                var centerPoint = new Point(selectionRect.X + selectionRect.Width / 2, selectionRect.Y + selectionRect.Height / 2);
                var (pageIndex, rect, pageImage) = _form.GetPageAtPoint(centerPoint);
                
                targetPageIndex = pageIndex;
                imgRect = rect;

                _logger.LogInformation("AddHighlightFromSelectionAsync: PdfPath={Path}, PageIndex={Page}, ImageSize={Width}x{Height}",
                    currentPdfPath, targetPageIndex, pageImage?.Width, pageImage?.Height);

                if (pageImage == null || string.IsNullOrEmpty(currentPdfPath))
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: targetPageImage or currentPdfPath is null");
                    return;
                }

                // 创建图片副本，防止异步操作期间原始图片被释放
                pageImageCopy = new Bitmap(pageImage);

                _logger.LogInformation("AddHighlightFromSelectionAsync: imgRect={X},{Y} {Width}x{Height}",
                    imgRect.X, imgRect.Y, imgRect.Width, imgRect.Height);

                if (imgRect.Width <= 0 || imgRect.Height <= 0)
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: imgRect has invalid size");
                    return;
                }

                int originalWidth = pageImageCopy.Width;
                int originalHeight = pageImageCopy.Height;

                float scaleX = (float)originalWidth / imgRect.Width;
                float scaleY = (float)originalHeight / imgRect.Height;

                float x = Math.Max(0, (selectionRect.X - imgRect.X) * scaleX);
                float y = Math.Max(0, (selectionRect.Y - imgRect.Y) * scaleY);
                float width = Math.Min(selectionRect.Width * scaleX, originalWidth - x);
                float height = Math.Min(selectionRect.Height * scaleY, originalHeight - y);

                var normalizedRect = new RectangleF(
                    x / originalWidth,
                    y / originalHeight,
                    width / originalWidth,
                    height / originalHeight
                );

                _logger.LogInformation("AddHighlightFromSelectionAsync: normalizedRect={X},{Y} {Width}x{Height}",
                    normalizedRect.X, normalizedRect.Y, normalizedRect.Width, normalizedRect.Height);

                if (normalizedRect.Width < 0.001f || normalizedRect.Height < 0.001f)
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: normalizedRect too small");
                    return;
                }

                // 使用图片副本进行OCR识别
                string ocrText = await GetOcrTextFromSelectionAsync(selectionRect, pageImageCopy, imgRect);

                if (string.IsNullOrEmpty(ocrText))
                {
                    if (_form.Presenter != null && !_form.Presenter.IsOcrAvailable())
                    {
                        _form.ShowWarning("OCR服务不可用，无法识别文字。\n请检查 tessdata 目录和语言数据文件是否存在。");
                    }
                }

                var highlight = new PdfHighlight
                {
                    PdfPath = currentPdfPath,
                    PageIndex = targetPageIndex,
                    NormalizedX = normalizedRect.X,
                    NormalizedY = normalizedRect.Y,
                    NormalizedWidth = normalizedRect.Width,
                    NormalizedHeight = normalizedRect.Height,
                    Text = ocrText,
                    Color = currentHighlightColor,
                    CreatedAt = DateTime.Now
                };

                var highlightId = await _highlightService.AddHighlightAsync(
                    currentPdfPath,
                    targetPageIndex,
                    normalizedRect.X,
                    normalizedRect.Y,
                    normalizedRect.Width,
                    normalizedRect.Height,
                    ocrText,
                    currentHighlightColor
                );

                highlight.Id = highlightId;

                PushUndoAction(new HighlightUndoAction
                {
                    ActionType = HighlightActionType.Add,
                    Highlight = highlight
                });

                _logger.LogInformation("AddHighlightFromSelectionAsync: RefreshHighlightList and UpdateHighlightLayer");
                
                // 检查当前页面是否仍然是目标页面，防止页面已切换
                bool isTargetPageVisible = _form.CurrentPdfPath == currentPdfPath &&
                    (_form.CurrentPageIndex == targetPageIndex || 
                     (_form.IsDualPage && _form.CurrentPageIndex + 1 == targetPageIndex));
                
                if (isTargetPageVisible)
                {
                    RefreshHighlightList();
                    UpdateHighlightLayer();
                    UpdateSecondHighlightLayer();
                    _form.PictureBoxPdf?.Invalidate();
                }

                if (!string.IsNullOrEmpty(ocrText) && _form.TextBoxOriginal != null)
                {
                    _form.TextBoxOriginal.Text = ocrText;
                    if (_form.AutoTranslateAfterOcr)
                    {
                        _form.OnTranslateClicked();
                    }

                    if (_form.AutoSpeakAfterOcr)
                    {
                        _ = _form.Presenter?.TryAutoSpeakAsync(ocrText);
                    }
                }

                if (_eventBus != null && !string.IsNullOrEmpty(ocrText))
                {
                    _eventBus.Publish(new PDFHighlightEvent
                    {
                        UserId = _appPaths?.GetCurrentUserId() ?? AppPaths.GetCurrentUserId(),
                        PdfFileName = Path.GetFileName(currentPdfPath),
                        HighlightedText = ocrText,
                        SourceUrl = currentPdfPath,
                        HighlightedAt = DateTime.Now
                    });
                    _logger.LogInformation("Published PDFHighlightEvent for text: {Text}", ocrText.Substring(0, Math.Min(30, ocrText.Length)));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding highlight from selection");
            }
            finally
            {
                // 释放图片副本
                pageImageCopy?.Dispose();
            }
        }

        private async Task<string> GetOcrTextFromSelectionAsync(Rectangle selectionRect, Bitmap currentPageImage, Rectangle imgRect)
        {
            if (currentPageImage == null || _form.Presenter == null) return string.Empty;

            if (!_form.Presenter.IsOcrAvailable())
            {
                _logger.LogWarning("OCR service is not available, skipping text recognition");
                return string.Empty;
            }

            if (imgRect.Width <= 0 || imgRect.Height <= 0) return string.Empty;

            float scaleX = (float)currentPageImage.Width / imgRect.Width;
            float scaleY = (float)currentPageImage.Height / imgRect.Height;

            float actualX = (selectionRect.X - imgRect.X) * scaleX;
            float actualY = (selectionRect.Y - imgRect.Y) * scaleY;
            float actualWidth = selectionRect.Width * scaleX;
            float actualHeight = selectionRect.Height * scaleY;

            actualX = Math.Max(0, actualX);
            actualY = Math.Max(0, actualY);
            actualWidth = Math.Min(currentPageImage.Width - actualX, actualWidth);
            actualHeight = Math.Min(currentPageImage.Height - actualY, actualHeight);

            if (actualWidth <= 0 || actualHeight <= 0) return string.Empty;

            var cropRect = new Rectangle(
                (int)Math.Round(actualX),
                (int)Math.Round(actualY),
                (int)Math.Round(actualWidth),
                (int)Math.Round(actualHeight)
            );

            var cropped = new Bitmap(cropRect.Width, cropRect.Height, currentPageImage.PixelFormat);
            using (var g = Graphics.FromImage(cropped))
            {
                g.DrawImage(currentPageImage, new Rectangle(0, 0, cropRect.Width, cropRect.Height), cropRect, GraphicsUnit.Pixel);
            }

            try
            {
                using var ocrStream = new MemoryStream();
                cropped.Save(ocrStream, System.Drawing.Imaging.ImageFormat.Png);
                var result = await _form.Presenter.OcrBitmapAsync(ocrStream.ToArray());
                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR recognition failed for selection at ({X}, {Y}, {Width}, {Height})",
                    selectionRect.X, selectionRect.Y, selectionRect.Width, selectionRect.Height);
                return string.Empty;
            }
        }

        private const int MaxUndoStackSize = 50;

        private void PushUndoAction(HighlightUndoAction action)
        {
            if (_undoStack.Count >= MaxUndoStackSize)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = items.Length - 2; i >= 0; i--)
                    _undoStack.Push(items[i]);
            }
            _undoStack.Push(action);
            UndoActionRecorded?.Invoke(this, EventArgs.Empty);
        }

        public void RemoveHighlight(PdfHighlight highlight)
        {
            PushUndoAction(new HighlightUndoAction
            {
                ActionType = HighlightActionType.Remove,
                Highlight = highlight
            });

            _highlightService.RemoveHighlight(_form.CurrentPdfPath, highlight.Id);

            // 单个删除时只精确移除 ListBox 中的对应项，避免调用 RefreshHighlightList()
            // 全量重建（会遍历所有页的标注文件，页数多时每次删除都会卡）
            if (_form.ListBoxHighlights != null)
            {
                _form.ListBoxHighlights.Items.Remove(highlight);
            }

            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
        }

        public void BatchRemoveHighlights()
        {
            var highlights = _highlightService.GetHighlights(_form.CurrentPdfPath) ?? new List<PdfHighlight>();
            int highlightCount = highlights.Count;
            int pageCount = _form.Presenter?.PageCount ?? 0;

            int strokeCount = 0;
            int textCount = 0;
            if (_annotationService != null && pageCount > 0)
            {
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    var strokes = _annotationService.GetStrokes(_form.CurrentPdfPath, pageIndex);
                    strokeCount += strokes.Count();
                    var texts = _annotationService.GetTexts(_form.CurrentPdfPath, pageIndex);
                    textCount += texts.Count();
                }
            }

            int annotationCount = strokeCount + textCount;
            if (highlightCount == 0 && annotationCount == 0)
            {
                _form.ShowWarning("当前文档没有标注可删除");
                return;
            }

            var message = $"确定要删除所有 {highlightCount} 个高亮、{strokeCount} 个笔画和 {textCount} 个文字标注吗？";
            var result = _form.ShowConfirm(message, "确认删除");
            if (!result) return;

            // 批量删除高亮：一次性移除并只写一次磁盘文件，避免逐个删除时反复序列化写文件导致UI卡死
            if (highlightCount > 0)
            {
                var removedHighlights = _highlightService.RemoveAllHighlights(_form.CurrentPdfPath);
                foreach (var highlight in removedHighlights)
                {
                    PushUndoAction(new HighlightUndoAction
                    {
                        ActionType = HighlightActionType.Remove,
                        Highlight = highlight
                    });
                }
            }

            // 清除所有页面标注：直接删除标注文件，比 ClearAllStrokes+ClearAllTexts（读+清空+写）高效得多
            if (_annotationService != null && pageCount > 0)
            {
                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    _annotationService.ClearAnnotation(_form.CurrentPdfPath, pageIndex);
                }
            }

            RefreshHighlightList();
            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();

            // 清除标注图层缓存后，需要重新加载当前页的标注以刷新笔触图层
            if (_form.Presenter != null)
            {
                _ = _form.Presenter.RenderAndDisplayCurrentPageAsync();
            }
            else
            {
                _form.PictureBoxPdf?.Invalidate();
            }

            _form.ShowMessage($"已成功删除 {highlightCount} 个高亮、{strokeCount} 个笔画和 {textCount} 个文字标注", "删除完成");
        }

        /// <summary>
        /// 判断高亮撤销栈中是否还有可撤销的高亮操作。
        /// 供统一撤销调度器在执行撤销前检查内部栈是否与统一栈同步。
        /// </summary>
        public bool CanUndoHighlight()
        {
            return _undoStack.Count > 0;
        }

        public async Task UndoHighlightAsync()
        {
            if (_undoStack.Count == 0)
            {
                _logger.LogInformation("UndoHighlight: 撤销栈为空，无可撤销的操作");
                _form.ShowMessage("没有可撤销的操作", "提示");
                return;
            }
            var lastAction = _undoStack.Pop();
            _logger.LogInformation("UndoHighlight: 执行撤销, ActionType={ActionType}, HighlightId={HighlightId}",
                lastAction.ActionType, lastAction.Highlight?.Id);

            if (lastAction.ActionType == HighlightActionType.Add)
            {
                if (lastAction.Highlight != null)
                {
                    _highlightService.RemoveHighlight(_form.CurrentPdfPath, lastAction.Highlight.Id);
                    _logger.LogInformation("UndoHighlight: 已删除高亮, Id={HighlightId}", lastAction.Highlight.Id);
                }
            }
            else if (lastAction.ActionType == HighlightActionType.Remove)
            {
                if (lastAction.Highlight != null)
                {
                    var highlight = lastAction.Highlight;
                    var newHighlightId = await _highlightService.AddHighlightAsync(
                        _form.CurrentPdfPath,
                        highlight.PageIndex,
                        highlight.NormalizedWidth > 0 ? highlight.NormalizedX : highlight.X,
                        highlight.NormalizedWidth > 0 ? highlight.NormalizedY : highlight.Y,
                        highlight.NormalizedWidth > 0 ? highlight.NormalizedWidth : highlight.Width,
                        highlight.NormalizedWidth > 0 ? highlight.NormalizedHeight : highlight.Height,
                        highlight.Text,
                        highlight.Color
                    );

                    var recoveredHighlight = new PdfHighlight
                    {
                        Id = newHighlightId,
                        PdfPath = _form.CurrentPdfPath,
                        PageIndex = highlight.PageIndex,
                        NormalizedX = highlight.NormalizedWidth > 0 ? highlight.NormalizedX : highlight.X,
                        NormalizedY = highlight.NormalizedWidth > 0 ? highlight.NormalizedY : highlight.Y,
                        NormalizedWidth = highlight.NormalizedWidth > 0 ? highlight.NormalizedWidth : highlight.Width,
                        NormalizedHeight = highlight.NormalizedWidth > 0 ? highlight.NormalizedHeight : highlight.Height,
                        Text = highlight.Text,
                        Color = highlight.Color,
                        CreatedAt = highlight.CreatedAt
                    };

                    PushUndoAction(new HighlightUndoAction
                    {
                        ActionType = HighlightActionType.Add,
                        Highlight = recoveredHighlight
                    });

                    _logger.LogInformation("UndoHighlight: 已恢复高亮, 新Id={HighlightId}", newHighlightId);
                }
            }

            RefreshHighlightList();
            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
        }

        public void RefreshHighlightList()
        {
            if (_form.ListBoxHighlights == null || string.IsNullOrEmpty(_form.CurrentPdfPath)) return;

            _form.ListBoxHighlights.Items.Clear();

            var highlights = _highlightService.GetHighlights(_form.CurrentPdfPath);
            foreach (var highlight in highlights)
            {
                _form.ListBoxHighlights.Items.Add(highlight);
            }

            if (_annotationService != null)
            {
                for (int pageIndex = 0; pageIndex < _form.Presenter?.PageCount; pageIndex++)
                {
                    var strokes = _annotationService.GetStrokes(_form.CurrentPdfPath, pageIndex);
                    foreach (var stroke in strokes)
                    {
                        var annotationItem = new PdfAnnotationItem
                        {
                            Id = stroke.Id,
                            PdfPath = _form.CurrentPdfPath,
                            PageIndex = pageIndex,
                            Type = AnnotationType.Stroke,
                            ColorArgb = stroke.ColorArgb,
                            Thickness = stroke.Thickness,
                            StrokePoints = stroke.Points,
                            CreatedAt = stroke.CreatedAt
                        };
                        _form.ListBoxHighlights.Items.Add(annotationItem);

                    }

                    var texts = _annotationService.GetTexts(_form.CurrentPdfPath, pageIndex);
                    foreach (var text in texts)
                    {
                        var annotationItem = new PdfAnnotationItem
                        {
                            Id = text.Id,
                            PdfPath = _form.CurrentPdfPath,
                            PageIndex = pageIndex,
                            Type = AnnotationType.Text,
                            NormalizedX = text.NormalizedX,
                            NormalizedY = text.NormalizedY,
                            ColorArgb = text.ColorArgb,
                            Text = text.Content,
                            FontSize = text.FontSize,
                            FontFamily = text.FontFamily,
                            CreatedAt = text.CreatedAt
                        };
                        _form.ListBoxHighlights.Items.Add(annotationItem);
                    }
                }
            }
        }

        public void LoadHighlightsForCurrentPage()
        {
            if (string.IsNullOrEmpty(_form.CurrentPdfPath) || _form.CurrentPageImage == null) return;

            UpdateHighlightLayer();
            _form.PictureBoxPdf.Invalidate();
        }

        public void ClearCacheForPdf(string pdfPath)
        {
            _highlightService.ClearCacheForPdf(pdfPath);
        }

        public void DrawHighlightsFromLayer(Graphics g)
        {
            try
            {
                if (_highlightBitmap == null || _form.CurrentPageImage == null)
                    return;

                var imgRect = _form.GetImageDisplayRect();
                g.DrawImage(_highlightBitmap, imgRect);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Object disposed in DrawHighlightsFromLayer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DrawHighlightsFromLayer");
            }
        }

        /// <summary>
        /// 双页模式下用预渲染的第二页高亮图层绘制，避免每帧实时重绘。
        /// 如果第二页图层还未准备好，则回退到实时绘制。
        /// </summary>
        public void DrawSecondHighlightsFromLayer(Graphics g, Rectangle targetRect, int secondPageIndex)
        {
            try
            {
                if (_secondHighlightBitmap != null && _form.SecondPageImage != null)
                {
                    g.DrawImage(_secondHighlightBitmap, targetRect);
                    return;
                }

                // 回退：实时绘制第二页高亮
                if (_form.SecondPageImage != null)
                {
                    DrawHighlightsForPage(g, secondPageIndex, targetRect,
                        _form.SecondPageImage.Width, _form.SecondPageImage.Height);
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Object disposed in DrawSecondHighlightsFromLayer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DrawSecondHighlightsFromLayer");
            }
        }

        public void DrawHighlightsForPage(Graphics g, int pageIndex, Rectangle targetRect, int imgWidth, int imgHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(_form.CurrentPdfPath)) return;

                var highlights = _highlightService.GetHighlightsForPage(_form.CurrentPdfPath, pageIndex);
                if (highlights.Count == 0) return;

                foreach (var highlight in highlights)
                {
                    var colorInfo = HighlightService.GetHighlightColor(highlight.Color);
                    var color = Color.FromArgb(colorInfo.A, colorInfo.R, colorInfo.G, colorInfo.B);

                    float x, y, width, height;
                    if (highlight.NormalizedWidth > 0)
                    {
                        x = highlight.NormalizedX * targetRect.Width + targetRect.X;
                        y = highlight.NormalizedY * targetRect.Height + targetRect.Y;
                        width = highlight.NormalizedWidth * targetRect.Width;
                        height = highlight.NormalizedHeight * targetRect.Height;
                    }
                    else
                    {
                        x = highlight.NormalizedX * targetRect.Width + targetRect.X;
                        y = highlight.NormalizedY * targetRect.Height + targetRect.Y;
                        width = 10;
                        height = 10;
                    }

                    var rect = new RectangleF(x, y, width, height);

                    DrawHighlightFill(g, color, rect);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DrawHighlightsForPage for page {Page}", pageIndex);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                CleanupHighlightLayer();
            }

            _disposed = true;
        }
    }
}
