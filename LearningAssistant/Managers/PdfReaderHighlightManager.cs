using LearningAssistant.Common.Events;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.DependencyInjection;
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

        private Bitmap? _highlightBitmap;
        private Graphics? _highlightGraphics;

        public HighlightColor CurrentHighlightColor { get; set; } = HighlightColor.Yellow;
        public bool IsHighlightMode { get; set; } = true;

        public PdfReaderHighlightManager(ILogger logger, IPdfReaderFormAccess form, IHighlightService highlightService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));

            var serviceProvider = (ServiceProvider?)form.Form?.Tag;
            _annotationService = serviceProvider?.GetService<IAnnotationService>();
            _eventBus = serviceProvider?.GetService<IEventBus>();
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

        private void DrawHighlight(PdfHighlight highlight, int imgWidth, int imgHeight)
        {
            var color = HighlightService.GetHighlightColor(highlight.Color);

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

            _logger.LogDebug("DrawHighlight: Id={Id}, Page={Page}, Color={Color}, Rect={X},{Y} {Width}x{Height}, imgSize={ImgWidth}x{ImgHeight}",
                highlight.Id, highlight.PageIndex, highlight.Color, rect.X, rect.Y, rect.Width, rect.Height, imgWidth, imgHeight);

            if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0)
            {
                _logger.LogWarning("DrawHighlight: rect has invalid dimensions, skipping");
                return;
            }

            int alpha1 = Math.Max(0, color.A - 0);
            int alpha2 = Math.Max(0, color.A - 20);
            int alpha3 = Math.Min(255, color.A + 120);

            using var gradientBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(alpha1, color.R, color.G, color.B),
                Color.FromArgb(alpha2, color.R, color.G, color.B),
                LinearGradientMode.ForwardDiagonal);

            _highlightGraphics!.FillRectangle(gradientBrush, rect);

            using var pen = new Pen(Color.FromArgb(alpha3, color.R, color.G, color.B), 2.5f);
            _highlightGraphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

            if (!string.IsNullOrEmpty(highlight.Note))
            {
                using var font = new Font("Microsoft YaHei UI", 10F);
                using var textBrush = new SolidBrush(Color.Black);
                _highlightGraphics.DrawString("📝", font, textBrush, rect.Location);
            }
        }

        public void CleanupHighlightLayer()
        {
            try
            {
                _highlightGraphics?.Dispose();
                _highlightBitmap?.Dispose();
                _highlightGraphics = null;
                _highlightBitmap = null;
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
            try
            {
                var currentPdfPath = _form.CurrentPdfPath;
                var currentHighlightColor = CurrentHighlightColor;

                var centerPoint = new Point(selectionRect.X + selectionRect.Width / 2, selectionRect.Y + selectionRect.Height / 2);
                var (targetPageIndex, imgRect, targetPageImage) = _form.GetPageAtPoint(centerPoint);

                _logger.LogInformation("AddHighlightFromSelectionAsync: PdfPath={Path}, PageIndex={Page}, ImageSize={Width}x{Height}",
                    currentPdfPath, targetPageIndex, targetPageImage?.Width, targetPageImage?.Height);

                if (targetPageImage == null || string.IsNullOrEmpty(currentPdfPath))
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: targetPageImage or currentPdfPath is null");
                    return;
                }

                _logger.LogInformation("AddHighlightFromSelectionAsync: imgRect={X},{Y} {Width}x{Height}",
                    imgRect.X, imgRect.Y, imgRect.Width, imgRect.Height);

                if (imgRect.Width <= 0 || imgRect.Height <= 0)
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: imgRect has invalid size");
                    return;
                }

                int originalWidth = targetPageImage.Width;
                int originalHeight = targetPageImage.Height;

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

                string ocrText = await GetOcrTextFromSelectionAsync(selectionRect, targetPageImage);

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

                var highlightId = _highlightService.AddHighlight(
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
                RefreshHighlightList();
                UpdateHighlightLayer();
                _form.PictureBoxPdf?.Invalidate();

                if (!string.IsNullOrEmpty(ocrText) && _form.TextBoxOriginal != null)
                {
                    _form.TextBoxOriginal.Text = ocrText;
                    if (_form.IsTranslationEnabled)
                    {
                        _form.OnTranslateClicked();
                    }
                }

                if (_eventBus != null && !string.IsNullOrEmpty(ocrText))
                {
                    _eventBus.Publish(new PDFHighlightEvent
                    {
                        UserId = "default",
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
        }

        private async Task<string> GetOcrTextFromSelectionAsync(Rectangle selectionRect, Bitmap currentPageImage)
        {
            if (currentPageImage == null || _form.Presenter == null) return string.Empty;

            if (!_form.Presenter.IsOcrAvailable())
            {
                _logger.LogWarning("OCR service is not available, skipping text recognition");
                return string.Empty;
            }

            var imgRect = _form.GetImageDisplayRect();
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

            using var cropped = currentPageImage.Clone(cropRect, currentPageImage.PixelFormat);

            try
            {
                var result = await _form.Presenter.OcrBitmapAsync(cropped);
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
                // 移除最旧的撤销记录
                var tempStack = new Stack<HighlightUndoAction>();
                for (int i = 0; i < MaxUndoStackSize - 1; i++)
                {
                    tempStack.Push(_undoStack.Pop());
                }
                _undoStack.Clear();
                while (tempStack.Count > 0)
                {
                    _undoStack.Push(tempStack.Pop());
                }
                _logger.LogDebug("UndoHighlight: 撤销栈已满，移除最旧的记录");
            }
            _undoStack.Push(action);
        }

        public void RemoveHighlight(PdfHighlight highlight)
        {
            PushUndoAction(new HighlightUndoAction
            {
                ActionType = HighlightActionType.Remove,
                Highlight = highlight
            });

            _highlightService.RemoveHighlight(_form.CurrentPdfPath, highlight.Id);
            RefreshHighlightList();
            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
        }

        public void BatchRemoveHighlights()
        {
            var highlights = _highlightService.GetHighlights(_form.CurrentPdfPath);
            int highlightCount = highlights?.Count ?? 0;

            int strokeCount = 0;
            int textCount = 0;
            if (_annotationService != null)
            {
                for (int pageIndex = 0; pageIndex < _form.Presenter?.PageCount; pageIndex++)
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

            foreach (var highlight in highlights)
            {
                PushUndoAction(new HighlightUndoAction
                {
                    ActionType = HighlightActionType.Remove,
                    Highlight = highlight
                });
                _highlightService.RemoveHighlight(_form.CurrentPdfPath, highlight.Id);
            }

            if (_annotationService != null)
            {
                for (int pageIndex = 0; pageIndex < _form.Presenter?.PageCount; pageIndex++)
                {
                    _annotationService.ClearAllStrokes(_form.CurrentPdfPath, pageIndex);
                    _annotationService.ClearAllTexts(_form.CurrentPdfPath, pageIndex);
                }
            }

            RefreshHighlightList();
            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
            _form.ShowMessage($"已成功删除 {highlightCount} 个高亮、{strokeCount} 个笔画和 {textCount} 个文字标注", "删除完成");
        }

        public void UndoHighlight()
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
                    var newHighlightId = _highlightService.AddHighlight(
                        _form.CurrentPdfPath,
                        highlight.PageIndex,
                        highlight.NormalizedX > 0 ? highlight.NormalizedX : highlight.X,
                        highlight.NormalizedY > 0 ? highlight.NormalizedY : highlight.Y,
                        highlight.NormalizedWidth > 0 ? highlight.NormalizedWidth : highlight.Width,
                        highlight.NormalizedHeight > 0 ? highlight.NormalizedHeight : highlight.Height,
                        highlight.Text,
                        highlight.Color
                    );

                    // 创建恢复后的高亮对象（带新Id），用于后续撤销
                    var recoveredHighlight = new PdfHighlight
                    {
                        Id = newHighlightId,
                        PdfPath = _form.CurrentPdfPath,
                        PageIndex = highlight.PageIndex,
                        NormalizedX = highlight.NormalizedX > 0 ? highlight.NormalizedX : highlight.X,
                        NormalizedY = highlight.NormalizedY > 0 ? highlight.NormalizedY : highlight.Y,
                        NormalizedWidth = highlight.NormalizedWidth > 0 ? highlight.NormalizedWidth : highlight.Width,
                        NormalizedHeight = highlight.NormalizedHeight > 0 ? highlight.NormalizedHeight : highlight.Height,
                        Text = highlight.Text,
                        Color = highlight.Color,
                        CreatedAt = highlight.CreatedAt
                    };

                    // 将恢复的操作压入撤销栈，以便再次撤销
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

        public void DrawHighlightsForPage(Graphics g, int pageIndex, Rectangle targetRect, int imgWidth, int imgHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(_form.CurrentPdfPath)) return;

                var highlights = _highlightService.GetHighlightsForPage(_form.CurrentPdfPath, pageIndex);
                if (highlights.Count == 0) return;

                foreach (var highlight in highlights)
                {
                    var color = HighlightService.GetHighlightColor(highlight.Color);

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

                    int alpha1 = Math.Max(0, color.A - 0);
                    int alpha2 = Math.Max(0, color.A - 20);
                    int alpha3 = Math.Min(255, color.A + 120);

                    using var brush = new SolidBrush(Color.FromArgb(alpha1, color.R, color.G, color.B));
                    g.FillRectangle(brush, rect);

                    using var pen = new Pen(Color.FromArgb(alpha3, color.R, color.G, color.B), 2.5f);
                    pen.DashStyle = DashStyle.Solid;
                    g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
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
