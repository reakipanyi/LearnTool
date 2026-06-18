using LearningAssistant.Models;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{
    public class PdfReaderHighlightManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IHighlightService _highlightService;
        private readonly Stack<HighlightUndoAction> _undoStack = new Stack<HighlightUndoAction>();
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;

        private Bitmap? _highlightBitmap;
        private Graphics? _highlightGraphics;

        public HighlightColor CurrentHighlightColor { get; set; } = HighlightColor.Yellow;
        public bool IsHighlightMode { get; set; } = true;

        public PdfReaderHighlightManager(ILogger logger, IPdfReaderFormAccess form, IHighlightService highlightService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
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
            int alpha2 = Math.Max(0, color.A - 30);
            int alpha3 = Math.Min(255, color.A + 50);

            using var gradientBrush = new LinearGradientBrush(
                rect,
                Color.FromArgb(alpha1, color.R, color.G, color.B),
                Color.FromArgb(alpha2, color.R, color.G, color.B),
                LinearGradientMode.ForwardDiagonal);

            _highlightGraphics!.FillRectangle(gradientBrush, rect);

            using var pen = new Pen(Color.FromArgb(alpha3, color.R, color.G, color.B), 1.5f);
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
                var currentPageImage = _form.CurrentPageImage;
                var currentPdfPath = _form.CurrentPdfPath;
                var currentPageIndex = _form.CurrentPageIndex;
                var currentHighlightColor = CurrentHighlightColor;

                _logger.LogInformation("AddHighlightFromSelectionAsync: PdfPath={Path}, PageIndex={Page}, ImageSize={Width}x{Height}",
                    currentPdfPath, currentPageIndex, currentPageImage?.Width, currentPageImage?.Height);

                if (currentPageImage == null || string.IsNullOrEmpty(currentPdfPath))
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: currentPageImage or currentPdfPath is null");
                    return;
                }

                var imgRect = _form.GetImageDisplayRect();
                _logger.LogInformation("AddHighlightFromSelectionAsync: imgRect={X},{Y} {Width}x{Height}",
                    imgRect.X, imgRect.Y, imgRect.Width, imgRect.Height);

                if (imgRect.Width <= 0 || imgRect.Height <= 0)
                {
                    _logger.LogWarning("AddHighlightFromSelectionAsync: imgRect has invalid size");
                    return;
                }

                int originalWidth = currentPageImage.Width;
                int originalHeight = currentPageImage.Height;

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

                string ocrText = await GetOcrTextFromSelectionAsync(selectionRect, currentPageImage);

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
                    PageIndex = currentPageIndex,
                    NormalizedX = normalizedRect.X,
                    NormalizedY = normalizedRect.Y,
                    NormalizedWidth = normalizedRect.Width,
                    NormalizedHeight = normalizedRect.Height,
                    Text = ocrText,
                    Color = currentHighlightColor,
                    CreatedAt = DateTime.Now
                };

                // 添加高亮并获取生成的Id
                var highlightId = _highlightService.AddHighlight(
                    currentPdfPath,
                    currentPageIndex,
                    normalizedRect.X,
                    normalizedRect.Y,
                    normalizedRect.Width,
                    normalizedRect.Height,
                    ocrText,
                    currentHighlightColor
                );

                // 更新highlight的Id以匹配实际保存的高亮
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
            if (highlights == null || highlights.Count == 0)
            {
                _form.ShowWarning("当前文档没有高亮可删除");
                return;
            }

            var result = _form.ShowConfirm($"确定要删除所有 {highlights.Count} 个高亮吗？", "确认删除");
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

            RefreshHighlightList();
            UpdateHighlightLayer();
            _form.PictureBoxPdf?.Invalidate();
            _form.ShowMessage($"已成功删除 {highlights.Count} 个高亮", "删除完成");
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
