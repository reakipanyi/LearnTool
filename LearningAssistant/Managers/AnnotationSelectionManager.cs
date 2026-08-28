using LearningAssistant.Abstractions;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public class AnnotationSelectionManager
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;

        // 选中状态
        public AnnotationStroke? SelectedStroke { get; set; }
        public int SelectedStrokeIndex { get; set; } = -1;
        public SelectionInteractionState SelectionState { get; set; } = SelectionInteractionState.None;
        public PointF SelectionDragStart { get; set; } = PointF.Empty;
        public PointF SelectionOriginalPoint1 { get; set; } = PointF.Empty;
        public PointF SelectionOriginalPoint2 { get; set; } = PointF.Empty;

        public PdfHighlight? SelectedHighlight { get; set; }
        public int SelectedHighlightIndex { get; set; } = -1;
        public RectangleF SelectedHighlightOriginalBounds { get; set; } = RectangleF.Empty;

        public AnnotationText? SelectedText { get; set; }
        public int SelectedTextIndex { get; set; } = -1;
        public RectangleF SelectedTextOriginalBounds { get; set; } = RectangleF.Empty;
        public float SelectedTextOriginalFontSize { get; set; } = 16f;

        // 橡皮擦
        public AnnotationStroke? HoveredStroke { get; set; }
        public int HoveredStrokeIndex { get; set; } = -1;
        public bool IsEraserDragging { get; set; }
        public Point EraserDragStart { get; set; }
        public Point EraserDragEnd { get; set; }

        // 常量
        public const float HitTestThreshold = 25f;
        public const float HandleSize = 8f;
        public const float SelectionBorderWidth = 2f;

        // 事件
        public event Action<AnnotationStroke, int, bool>? AnnotationSelected;
        public event Action? SelectionCleared;

        // 回调委托（由主类设置）
        public Func<Point, PointF>? ClientToImage { get; set; }
        public Func<Point, Rectangle>? GetCurrentDisplayRect { get; set; }
        public Action<int, AnnotationStroke>? UpdateStrokeAtCurrentPage { get; set; }
        public Action<int>? RemoveStrokeAtCurrentPage { get; set; }
        public Action<int, AnnotationText>? RemoveTextAtCurrentPage { get; set; }
        public Action<int, AnnotationText>? UpdateTextAtCurrentPage { get; set; }
        public Action? ReloadAnnotations { get; set; }
        public Action? InvalidateView { get; set; }
        public Action<AnnotationStroke>? PushStrokeToUndoStack { get; set; }
        public Action<AnnotationText, int>? EditTextCallback { get; set; }
        public Action<PdfHighlight>? UpdateHighlightCallback { get; set; }

        public AnnotationSelectionManager(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        #region 选中管理

        public void ClearSelection()
        {
            SelectedStroke = null;
            SelectedStrokeIndex = -1;
            SelectedHighlight = null;
            SelectedHighlightIndex = -1;
            SelectedText = null;
            SelectedTextIndex = -1;
            SelectionState = SelectionInteractionState.None;
            InvalidateView?.Invoke();
            SelectionCleared?.Invoke();
        }

        public void DeleteSelectedStroke()
        {
            if (SelectedStrokeIndex < 0) return;

            RemoveStrokeAtCurrentPage?.Invoke(SelectedStrokeIndex);
            SelectedStroke = null;
            SelectedStrokeIndex = -1;
            SelectionState = SelectionInteractionState.None;

            ReloadAnnotations?.Invoke();
            InvalidateView?.Invoke();
        }

        public void DeleteSelectedText()
        {
            if (SelectedTextIndex < 0) return;

            RemoveTextAtCurrentPage?.Invoke(SelectedTextIndex, SelectedText!);
            SelectedText = null;
            SelectedTextIndex = -1;
            SelectionState = SelectionInteractionState.None;

            ReloadAnnotations?.Invoke();
            InvalidateView?.Invoke();
        }

        public void UpdateSelectedStrokeColor(Color color)
        {
            if (SelectedStroke == null || SelectedStrokeIndex < 0) return;
            SelectedStroke.ColorArgb = color.ToArgb();
            UpdateStrokeAtCurrentPage?.Invoke(SelectedStrokeIndex, SelectedStroke);
            ReloadAnnotations?.Invoke();
            InvalidateView?.Invoke();
        }

        public void UpdateSelectedStrokeThickness(float thickness)
        {
            if (SelectedStroke == null || SelectedStrokeIndex < 0) return;
            SelectedStroke.Thickness = Math.Max(1f, Math.Min(20f, thickness));
            UpdateStrokeAtCurrentPage?.Invoke(SelectedStrokeIndex, SelectedStroke);
            ReloadAnnotations?.Invoke();
            InvalidateView?.Invoke();
        }

        #endregion

        #region 选中模式点击处理

        public void HandleSelectModeClick(Point clientPoint)
        {
            if (_form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);
            var strokes = GetCurrentPageStrokes();

            // 检测是否点击了选中标注的手柄（进行缩放操作）
            if (SelectedStroke != null && SelectionState != SelectionInteractionState.None)
            {
                var (_, _, handles) = GetSelectionBounds(SelectedStroke, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint));
                foreach (var (handleRect, state) in handles)
                {
                    var imgRect = ClientRectFromScreenRect(handleRect);
                    if (imgRect.Contains((int)imgPoint.X, (int)imgPoint.Y))
                    {
                        SelectionState = state;
                        SelectionDragStart = imgPoint;
                        if (SelectedStroke.Points.Length >= 4)
                        {
                            SelectionOriginalPoint1 = new PointF(
                                SelectedStroke.Points[0] * _form.CurrentPageImage.Width,
                                SelectedStroke.Points[1] * _form.CurrentPageImage.Height);
                            SelectionOriginalPoint2 = new PointF(
                                SelectedStroke.Points[2] * _form.CurrentPageImage.Width,
                                SelectedStroke.Points[3] * _form.CurrentPageImage.Height);
                        }
                        InvalidateView?.Invoke();
                        return;
                    }
                }
            }

            // 检测是否选中了标注（点击在标注上）
            for (int i = strokes.Count - 1; i >= 0; i--)
            {
                var stroke = strokes[i];
                if (HitTestStroke(stroke, imgPoint))
                {
                    if (SelectedStroke == stroke && SelectedStrokeIndex == i)
                    {
                        // 已选中状态，检测是否为双击
                        var now = DateTime.Now;
                        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
                        if (timeDiff < DoubleClickTime_ms && timeDiff > 0)
                        {
                            AnnotationSelected?.Invoke(stroke, i, true);
                            _lastClickTime = DateTime.MinValue;
                        }
                        else
                        {
                            SelectionState = SelectionInteractionState.Moving;
                            SelectionDragStart = imgPoint;
                            _lastClickTime = now;
                            _lastClickLocation = clientPoint;
                        }
                        return;
                    }

                    SelectedStroke = stroke;
                    SelectedStrokeIndex = i;
                    SelectionState = SelectionInteractionState.Moving;
                    SelectionDragStart = imgPoint;
                    _lastClickTime = DateTime.Now;
                    _lastClickLocation = clientPoint;

                    _logger.LogInformation("Selected stroke at index {Index}, type={ShapeType}", i, stroke.ShapeType);
                    AnnotationSelected?.Invoke(stroke, i, false);
                    InvalidateView?.Invoke();
                    return;
                }
            }

            // 检测是否点击了选中高亮的手柄
            if (SelectedHighlight != null && SelectionState != SelectionInteractionState.None)
            {
                var handles = GetHighlightHandles(SelectedHighlight, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint));
                foreach (var (handleRect, state) in handles)
                {
                    var imgRect = ClientRectFromScreenRect(handleRect);
                    if (imgRect.Contains((int)imgPoint.X, (int)imgPoint.Y))
                    {
                        SelectionState = state;
                        SelectionDragStart = imgPoint;
                        SelectedHighlightOriginalBounds = new RectangleF(
                            SelectedHighlight.NormalizedX, SelectedHighlight.NormalizedY,
                            SelectedHighlight.NormalizedWidth, SelectedHighlight.NormalizedHeight);
                        InvalidateView?.Invoke();
                        return;
                    }
                }
            }

            // 尝试检测高亮点击
            var highlights = GetCurrentPageHighlights();
            for (int i = highlights.Count - 1; i >= 0; i--)
            {
                if (HitTestHighlight(highlights[i], imgPoint))
                {
                    if (SelectedHighlight == highlights[i] && SelectedHighlightIndex == i)
                    {
                        SelectionState = SelectionInteractionState.Moving;
                        SelectionDragStart = imgPoint;
                        _lastClickTime = DateTime.Now;
                        _lastClickLocation = clientPoint;
                    }
                    else
                    {
                        SelectedHighlight = highlights[i];
                        SelectedHighlightIndex = i;
                        SelectionState = SelectionInteractionState.Moving;
                        SelectionDragStart = imgPoint;
                        SelectedHighlightOriginalBounds = new RectangleF(
                            highlights[i].NormalizedX, highlights[i].NormalizedY,
                            highlights[i].NormalizedWidth, highlights[i].NormalizedHeight);
                        _logger.LogInformation("Selected highlight at index {Index}", i);
                    }
                    InvalidateView?.Invoke();
                    return;
                }
            }

            // 检测是否点击了选中文字注解的手柄
            if (SelectedText != null && SelectionState != SelectionInteractionState.None)
            {
                var handles = GetTextHandles(SelectedText, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint));
                foreach (var (handleRect, state) in handles)
                {
                    var imgRect = ClientRectFromScreenRect(handleRect);
                    if (imgRect.Contains((int)imgPoint.X, (int)imgPoint.Y))
                    {
                        SelectionState = state;
                        SelectionDragStart = imgPoint;
                        SelectedTextOriginalBounds = GetTextNormalizedBounds(SelectedText, _form.CurrentPageImage);
                        SelectedTextOriginalFontSize = SelectedText.FontSize;
                        InvalidateView?.Invoke();
                        return;
                    }
                }
            }

            // 尝试检测文字注解点击
            var texts = GetCurrentPageTexts();
            for (int i = texts.Count - 1; i >= 0; i--)
            {
                if (HitTestText(texts[i], imgPoint))
                {
                    if (SelectedText != null && SelectedTextIndex == i)
                    {
                        var now = DateTime.Now;
                        var timeDiff = (now - _lastClickTime).TotalMilliseconds;
                        if (timeDiff < DoubleClickTime_ms && timeDiff > 0)
                        {
                            EditTextCallback?.Invoke(texts[i], i);
                            _lastClickTime = DateTime.MinValue;
                        }
                        else
                        {
                            SelectionState = SelectionInteractionState.Moving;
                            SelectionDragStart = imgPoint;
                            _lastClickTime = now;
                            _lastClickLocation = clientPoint;
                        }
                    }
                    else
                    {
                        SelectedText = texts[i];
                        SelectedTextIndex = i;
                        SelectionState = SelectionInteractionState.Moving;
                        SelectionDragStart = imgPoint;
                        SelectedTextOriginalBounds = GetTextNormalizedBounds(texts[i], _form.CurrentPageImage);
                        SelectedTextOriginalFontSize = texts[i].FontSize;
                        _lastClickTime = DateTime.Now;
                        _lastClickLocation = clientPoint;
                        _logger.LogInformation("Selected text annotation at index {Index}", i);
                    }
                    InvalidateView?.Invoke();
                    return;
                }
            }

            // 点击空白区域 -> 取消选中
            if (SelectedStroke != null || SelectedHighlight != null || SelectedText != null)
            {
                ClearSelection();
                InvalidateView?.Invoke();
            }
            _lastClickTime = DateTime.MinValue;

            _logger.LogInformation("No stroke, highlight or text hit - click at {X},{Y}", imgPoint.X, imgPoint.Y);
        }

        #endregion

        #region 拖拽/缩放处理

        public void HandleSelectionDragMove(Point clientPoint)
        {
            if (SelectedHighlight != null)
            {
                HandleHighlightDragMove(clientPoint);
                return;
            }

            if (SelectedText != null)
            {
                HandleTextDragMove(clientPoint);
                return;
            }

            if (SelectedStroke == null || _form.CurrentPageImage == null) return;
            if (SelectedStroke.Points == null || SelectedStroke.Points.Length < 4) return;

            var imgPoint = ClientToImage!(clientPoint);
            var dragDelta = new PointF(
                imgPoint.X - SelectionDragStart.X,
                imgPoint.Y - SelectionDragStart.Y);

            SelectionDragStart = imgPoint;

            float pageWidth = _form.CurrentPageImage.Width;
            float pageHeight = _form.CurrentPageImage.Height;

            for (int i = 0; i < SelectedStroke.Points.Length - 1; i += 2)
            {
                float newX = SelectedStroke.Points[i] * pageWidth + dragDelta.X;
                float newY = SelectedStroke.Points[i + 1] * pageHeight + dragDelta.Y;

                SelectedStroke.Points[i] = Math.Max(0, Math.Min(pageWidth, newX)) / pageWidth;
                SelectedStroke.Points[i + 1] = Math.Max(0, Math.Min(pageHeight, newY)) / pageHeight;
            }

            UpdateStrokeAtCurrentPage?.Invoke(SelectedStrokeIndex, SelectedStroke);
        }

        public void HandleHighlightDragMove(Point clientPoint)
        {
            if (SelectedHighlight == null || _form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);
            var dragDelta = new PointF(
                imgPoint.X - SelectionDragStart.X,
                imgPoint.Y - SelectionDragStart.Y);

            SelectionDragStart = imgPoint;

            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float dx = dragDelta.X / imgWidth;
            float dy = dragDelta.Y / imgHeight;

            SelectedHighlight.NormalizedX = Math.Max(0, Math.Min(1 - SelectedHighlight.NormalizedWidth,
                SelectedHighlight.NormalizedX + dx));
            SelectedHighlight.NormalizedY = Math.Max(0, Math.Min(1 - SelectedHighlight.NormalizedHeight,
                SelectedHighlight.NormalizedY + dy));

            SelectedHighlightOriginalBounds = new RectangleF(
                SelectedHighlight.NormalizedX, SelectedHighlight.NormalizedY,
                SelectedHighlight.NormalizedWidth, SelectedHighlight.NormalizedHeight);

            UpdateHighlightCallback?.Invoke(SelectedHighlight);
        }

        public void HandleTextDragMove(Point clientPoint)
        {
            if (SelectedText == null || _form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);
            var dragDelta = new PointF(
                imgPoint.X - SelectionDragStart.X,
                imgPoint.Y - SelectionDragStart.Y);

            SelectionDragStart = imgPoint;

            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float dx = dragDelta.X / imgWidth;
            float dy = dragDelta.Y / imgHeight;

            SelectedText.NormalizedX = Math.Max(0, Math.Min(1, SelectedText.NormalizedX + dx));
            SelectedText.NormalizedY = Math.Max(0, Math.Min(1, SelectedText.NormalizedY + dy));

            UpdateTextAtCurrentPage?.Invoke(SelectedTextIndex, SelectedText);
        }

        public void HandleHighlightResize(Point clientPoint)
        {
            if (SelectedHighlight == null || _form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float ox = SelectedHighlightOriginalBounds.X * imgWidth;
            float oy = SelectedHighlightOriginalBounds.Y * imgHeight;
            float ow = SelectedHighlightOriginalBounds.Width * imgWidth;
            float oh = SelectedHighlightOriginalBounds.Height * imgHeight;

            float left = ox, top = oy, right = ox + ow, bottom = oy + oh;

            switch (SelectionState)
            {
                case SelectionInteractionState.ResizingTopLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingTopRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
            }

            SelectedHighlight.NormalizedX = Math.Max(0, Math.Min(1, left / imgWidth));
            SelectedHighlight.NormalizedY = Math.Max(0, Math.Min(1, top / imgHeight));
            SelectedHighlight.NormalizedWidth = Math.Max(0.01f, Math.Min(1, (right - left) / imgWidth));
            SelectedHighlight.NormalizedHeight = Math.Max(0.01f, Math.Min(1, (bottom - top) / imgHeight));

            UpdateHighlightCallback?.Invoke(SelectedHighlight);
        }

        public void HandleSelectionResize(Point clientPoint)
        {
            if (SelectedText != null)
            {
                HandleTextResize(clientPoint);
                return;
            }

            if (SelectedStroke == null || _form.CurrentPageImage == null) return;
            if (SelectedStroke.Points == null || SelectedStroke.Points.Length < 4) return;

            var imgPoint = ClientToImage!(clientPoint);

            float scaleX = _form.CurrentPageImage.Width;
            float scaleY = _form.CurrentPageImage.Height;

            float x1 = SelectionOriginalPoint1.X;
            float y1 = SelectionOriginalPoint1.Y;
            float x2 = SelectionOriginalPoint2.X;
            float y2 = SelectionOriginalPoint2.Y;

            float left = Math.Min(x1, x2);
            float right = Math.Max(x1, x2);
            float top = Math.Min(y1, y2);
            float bottom = Math.Max(y1, y2);

            switch (SelectionState)
            {
                case SelectionInteractionState.ResizingTopLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingTopRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
            }

            SelectedStroke.Points[0] = Math.Max(0, Math.Min(1, left / scaleX));
            SelectedStroke.Points[1] = Math.Max(0, Math.Min(1, top / scaleY));
            SelectedStroke.Points[2] = Math.Max(0, Math.Min(1, right / scaleX));
            SelectedStroke.Points[3] = Math.Max(0, Math.Min(1, bottom / scaleY));

            UpdateStrokeAtCurrentPage?.Invoke(SelectedStrokeIndex, SelectedStroke);
        }

        public void HandleTextResize(Point clientPoint)
        {
            if (SelectedText == null || _form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float origLeft = SelectedTextOriginalBounds.X * imgWidth;
            float origTop = SelectedTextOriginalBounds.Y * imgHeight;
            float origRight = (SelectedTextOriginalBounds.X + SelectedTextOriginalBounds.Width) * imgWidth;
            float origBottom = (SelectedTextOriginalBounds.Y + SelectedTextOriginalBounds.Height) * imgHeight;

            float left = origLeft, top = origTop, right = origRight, bottom = origBottom;

            switch (SelectionState)
            {
                case SelectionInteractionState.ResizingTopLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingTopRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    top = Math.Min(bottom - 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomLeft:
                    left = Math.Min(right - 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
                case SelectionInteractionState.ResizingBottomRight:
                    right = Math.Max(left + 10, imgPoint.X);
                    bottom = Math.Max(top + 10, imgPoint.Y);
                    break;
            }

            float origW = origRight - origLeft;
            float origH = origBottom - origTop;
            float newW = Math.Max(10, right - left);
            float newH = Math.Max(10, bottom - top);
            float scale = Math.Min(newW / origW, newH / origH);
            float newFontSize = Math.Max(8, Math.Min(96, SelectedTextOriginalFontSize * scale));

            SelectedText.FontSize = newFontSize;
            UpdateTextAtCurrentPage?.Invoke(SelectedTextIndex, SelectedText);
        }

        #endregion

        #region 光标更新

        public void UpdateCursorForHandleHit(Point clientPoint)
        {
            if (_form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage!(clientPoint);

            if (SelectedHighlight != null)
            {
                UpdateCursorForHandles(GetHighlightHandles(SelectedHighlight, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint)), imgPoint);
                return;
            }

            if (SelectedText != null)
            {
                UpdateCursorForHandles(GetTextHandles(SelectedText, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint)), imgPoint);
                return;
            }

            if (SelectedStroke == null || _form.CurrentPageImage == null) return;

            var (_, _, strokeHandles) = GetSelectionBounds(SelectedStroke, _form.CurrentPageImage, GetCurrentDisplayRect!(clientPoint));
            UpdateCursorForHandles(strokeHandles, imgPoint);

            _form.PictureBoxPdf.Cursor = Cursors.SizeAll;
        }

        private void UpdateCursorForHandles(List<(Rectangle handleRect, SelectionInteractionState state)> handles, PointF imgPoint)
        {
            foreach (var (handleRect, state) in handles)
            {
                var imgRect = ClientRectFromScreenRect(handleRect);
                if (imgRect.Contains((int)imgPoint.X, (int)imgPoint.Y))
                {
                    _form.PictureBoxPdf.Cursor = state switch
                    {
                        SelectionInteractionState.ResizingTopLeft => Cursors.SizeNWSE,
                        SelectionInteractionState.ResizingTopRight => Cursors.SizeNESW,
                        SelectionInteractionState.ResizingBottomLeft => Cursors.SizeNESW,
                        SelectionInteractionState.ResizingBottomRight => Cursors.SizeNWSE,
                        _ => Cursors.SizeAll
                    };
                    return;
                }
            }
        }

        #endregion

        #region 橡皮擦

        public void HandleEraserModeClick(Point clientPoint)
        {
            if (HoveredStrokeIndex < 0)
            {
                var imgPoint = ClientToImage!(clientPoint);
                var strokes = GetCurrentPageStrokes();
                for (int i = strokes.Count - 1; i >= 0; i--)
                {
                    if (HitTestStroke(strokes[i], imgPoint))
                    {
                        HoveredStrokeIndex = i;
                        HoveredStroke = strokes[i];
                        break;
                    }
                }
                if (HoveredStrokeIndex < 0) return;
            }

            _logger.LogInformation("Eraser deleting stroke at index {Index}", HoveredStrokeIndex);

            if (HoveredStroke != null)
            {
                PushStrokeToUndoStack?.Invoke(HoveredStroke);
            }

            RemoveStrokeAtCurrentPage?.Invoke(HoveredStrokeIndex);
            HoveredStroke = null;
            HoveredStrokeIndex = -1;

            ReloadAnnotations?.Invoke();
            InvalidateView?.Invoke();
        }

        public void HandleEraserHover(Point clientPoint)
        {
            var imgPoint = ClientToImage!(clientPoint);
            var strokes = GetCurrentPageStrokes();

            for (int i = strokes.Count - 1; i >= 0; i--)
            {
                if (HitTestStroke(strokes[i], imgPoint))
                {
                    if (HoveredStroke != strokes[i])
                    {
                        HoveredStroke = strokes[i];
                        HoveredStrokeIndex = i;
                        _form.PictureBoxPdf.Cursor = Cursors.Hand;
                        InvalidateView?.Invoke();
                    }
                    return;
                }
            }

            if (HoveredStroke != null)
            {
                HoveredStroke = null;
                HoveredStrokeIndex = -1;
                _form.PictureBoxPdf.Cursor = Cursors.Default;
                InvalidateView?.Invoke();
            }
        }

        #endregion

        #region 命中检测

        public bool HitTestStroke(AnnotationStroke stroke, PointF imgPoint)
        {
            if (stroke.Points == null || stroke.Points.Length < 4) return false;
            if (_form.CurrentPageImage == null) return false;

            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;
            string shapeType = stroke.ShapeType ?? string.Empty;
            float hitThreshold = Math.Max(stroke.Thickness, HitTestThreshold);

            switch (shapeType)
            {
                case "Rectangle":
                case "Ellipse":
                case "Mosaic":
                    {
                        float x1 = stroke.Points[0] * imgWidth;
                        float y1 = stroke.Points[1] * imgHeight;
                        float x2 = stroke.Points[2] * imgWidth;
                        float y2 = stroke.Points[3] * imgHeight;

                        float left = Math.Min(x1, x2) - hitThreshold;
                        float right = Math.Max(x1, x2) + hitThreshold;
                        float top = Math.Min(y1, y2) - hitThreshold;
                        float bottom = Math.Max(y1, y2) + hitThreshold;

                        return imgPoint.X >= left && imgPoint.X <= right && imgPoint.Y >= top && imgPoint.Y <= bottom;
                    }
                case "Arrow":
                case "Pen":
                case "Strikethrough":
                default:
                    {
                        for (int i = 0; i < stroke.Points.Length - 3; i += 2)
                        {
                            float x1 = stroke.Points[i] * imgWidth;
                            float y1 = stroke.Points[i + 1] * imgHeight;
                            float x2 = stroke.Points[i + 2] * imgWidth;
                            float y2 = stroke.Points[i + 3] * imgHeight;

                            float distance = PointToLineDistance(imgPoint.X, imgPoint.Y, x1, y1, x2, y2);
                            if (distance < hitThreshold) return true;
                        }
                        return false;
                    }
            }
        }

        public bool HitTestHighlight(PdfHighlight highlight, PointF imgPoint)
        {
            if (_form.CurrentPageImage == null) return false;
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float x = highlight.NormalizedX * imgWidth;
            float y = highlight.NormalizedY * imgHeight;
            float w = highlight.NormalizedWidth * imgWidth;
            float h = highlight.NormalizedHeight * imgHeight;

            float padding = HitTestThreshold;
            return imgPoint.X >= x - padding && imgPoint.X <= x + w + padding &&
                   imgPoint.Y >= y - padding && imgPoint.Y <= y + h + padding;
        }

        public bool HitTestText(AnnotationText text, PointF imgPoint)
        {
            if (_form.CurrentPageImage == null) return false;
            var bounds = GetTextNormalizedBounds(text, _form.CurrentPageImage);
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float x = bounds.X * imgWidth;
            float y = bounds.Y * imgHeight;
            float w = bounds.Width * imgWidth;
            float h = bounds.Height * imgHeight;

            float padding = HitTestThreshold;
            return imgPoint.X >= x - padding && imgPoint.X <= x + w + padding &&
                   imgPoint.Y >= y - padding && imgPoint.Y <= y + h + padding;
        }

        public bool StrokeIntersectsRect(AnnotationStroke stroke, RectangleF rect)
        {
            if (stroke.Points == null || stroke.Points.Length < 2) return false;
            if (_form.CurrentPageImage == null) return false;

            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < stroke.Points.Length - 1; i += 2)
            {
                float px = stroke.Points[i] * imgWidth;
                float py = stroke.Points[i + 1] * imgHeight;
                minX = Math.Min(minX, px);
                minY = Math.Min(minY, py);
                maxX = Math.Max(maxX, px);
                maxY = Math.Max(maxY, py);
            }

            if (maxX < minX || maxY < minY) return false;

            var strokeBounds = new RectangleF(minX, minY, maxX - minX, maxY - minY);
            return strokeBounds.IntersectsWith(rect);
        }

        private static float PointToLineDistance(float px, float py, float x1, float y1, float x2, float y2)
        {
            float A = px - x1;
            float B = py - y1;
            float C = x2 - x1;
            float D = y2 - y1;

            float dot = A * C + B * D;
            float lenSq = C * C + D * D;
            float param = -1;

            if (lenSq != 0)
                param = dot / lenSq;

            float xx, yy;

            if (param < 0) { xx = x1; yy = y1; }
            else if (param > 1) { xx = x2; yy = y2; }
            else { xx = x1 + param * C; yy = y1 + param * D; }

            float dx = px - xx;
            float dy = py - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        #endregion

        #region 数据获取

        public List<AnnotationStroke> GetCurrentPageStrokes()
        {
            if (_form.Presenter == null) return new List<AnnotationStroke>();
            return _form.Presenter.GetCurrentPageStrokes().ToList();
        }

        public List<PdfHighlight> GetCurrentPageHighlights()
        {
            try
            {
                var highlightService = _form.HighlightService;
                if (highlightService == null || string.IsNullOrEmpty(_form.CurrentPdfPath))
                    return new List<PdfHighlight>();
                return highlightService.GetHighlightsForPage(_form.CurrentPdfPath, _form.CurrentPageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting current page highlights");
                return new List<PdfHighlight>();
            }
        }

        public List<AnnotationText> GetCurrentPageTexts()
        {
            if (_form.Presenter == null) return new List<AnnotationText>();
            return _form.Presenter.GetCurrentPageTexts().ToList();
        }

        #endregion

        #region 边界计算

        public (RectangleF bounds, List<PointF> corners, List<(Rectangle handleRect, SelectionInteractionState state)> handles) 
            GetSelectionBounds(AnnotationStroke stroke, Bitmap? pageImage, Rectangle? displayRect = null)
        {
            var result = (bounds: RectangleF.Empty, corners: new List<PointF>(), handles: new List<(Rectangle, SelectionInteractionState)>());

            if (stroke == null || stroke.Points == null || stroke.Points.Length < 4 || pageImage == null)
                return result;

            int imgWidth = pageImage.Width;
            int imgHeight = pageImage.Height;
            var imgRect = displayRect ?? _form.GetImageDisplayRect();

            float screenScaleX = (float)imgRect.Width / imgWidth;
            float screenScaleY = (float)imgRect.Height / imgHeight;

            var screenPoints = new List<PointF>();
            for (int i = 0; i < stroke.Points.Length - 1; i += 2)
            {
                float nx = stroke.Points[i] * imgWidth * screenScaleX + imgRect.X;
                float ny = stroke.Points[i + 1] * imgHeight * screenScaleY + imgRect.Y;
                screenPoints.Add(new PointF(nx, ny));
            }

            if (screenPoints.Count < 2) return result;

            var shapeType = stroke.ShapeType ?? string.Empty;
            if (shapeType is "Rectangle" or "Ellipse" or "Mosaic" or "Arrow" or "")
            {
                float x1 = screenPoints[0].X, y1 = screenPoints[0].Y;
                float x2 = screenPoints[^1].X, y2 = screenPoints[^1].Y;

                float left = Math.Min(x1, x2), top = Math.Min(y1, y2);
                float right = Math.Max(x1, x2), bottom = Math.Max(y1, y2);

                if (string.IsNullOrEmpty(shapeType) || shapeType == "Pen" || shapeType == "Strikethrough")
                {
                    left = screenPoints.Min(p => p.X);
                    top = screenPoints.Min(p => p.Y);
                    right = screenPoints.Max(p => p.X);
                    bottom = screenPoints.Max(p => p.Y);
                }

                float thicknessPadding = Math.Max(stroke.Thickness * screenScaleX, 0) / 2f;
                float padding = Math.Max(thicknessPadding, 6f);
                left -= padding; top -= padding; right += padding; bottom += padding;

                result.bounds = new RectangleF(left, top, right - left, bottom - top);

                var corners = new List<PointF>
                {
                    new PointF(left, top), new PointF(right, top),
                    new PointF(left, bottom), new PointF(right, bottom)
                };
                result.corners = corners;

                float halfHandle = HandleSize / 2;
                result.handles = new List<(Rectangle, SelectionInteractionState)>
                {
                    (new Rectangle((int)(left - halfHandle), (int)(top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopLeft),
                    (new Rectangle((int)(right - halfHandle), (int)(top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopRight),
                    (new Rectangle((int)(left - halfHandle), (int)(bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomLeft),
                    (new Rectangle((int)(right - halfHandle), (int)(bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomRight)
                };
            }

            return result;
        }

        public RectangleF GetHighlightScreenBounds(PdfHighlight highlight, Bitmap? pageImage, Rectangle? displayRect = null)
        {
            if (highlight == null || pageImage == null) return RectangleF.Empty;

            var imgRect = displayRect ?? _form.GetImageDisplayRect();
            int imgWidth = pageImage.Width;
            int imgHeight = pageImage.Height;

            float screenScaleX = (float)imgRect.Width / imgWidth;
            float screenScaleY = (float)imgRect.Height / imgHeight;

            float x = highlight.NormalizedX * imgWidth * screenScaleX + imgRect.X;
            float y = highlight.NormalizedY * imgHeight * screenScaleY + imgRect.Y;
            float w = highlight.NormalizedWidth * imgWidth * screenScaleX;
            float h = highlight.NormalizedHeight * imgHeight * screenScaleY;

            return new RectangleF(x, y, w, h);
        }

        public RectangleF GetTextScreenBounds(AnnotationText text, Bitmap? pageImage, Rectangle? displayRect = null)
        {
            if (text == null || pageImage == null) return RectangleF.Empty;

            var imgRect = displayRect ?? _form.GetImageDisplayRect();
            int imgWidth = pageImage.Width;
            int imgHeight = pageImage.Height;

            float screenScaleX = (float)imgRect.Width / imgWidth;
            float screenScaleY = (float)imgRect.Height / imgHeight;

            float x = text.NormalizedX * imgWidth * screenScaleX + imgRect.X;
            float y = text.NormalizedY * imgHeight * screenScaleY + imgRect.Y;

            float scaleX = GetAnnotationFontScale(pageImage);
            var textSize = MeasureTextWithScreenDpi(text.Content, text.FontFamily, text.FontSize * scaleX * screenScaleX);
            if (textSize.IsEmpty) textSize = new SizeF(100, 20);

            float padding = 8f;
            return new RectangleF(x - padding, y - padding, textSize.Width + padding * 2, textSize.Height + padding * 2);
        }

        public List<(Rectangle handleRect, SelectionInteractionState state)> GetHighlightHandles(PdfHighlight highlight, Bitmap? pageImage, Rectangle? displayRect = null)
        {
            var result = new List<(Rectangle, SelectionInteractionState)>();
            if (highlight == null || pageImage == null) return result;

            var bounds = GetHighlightScreenBounds(highlight, pageImage, displayRect);
            if (bounds.Width <= 0 || bounds.Height <= 0) return result;

            float halfHandle = HandleSize / 2;

            result.Add((new Rectangle((int)(bounds.Left - halfHandle), (int)(bounds.Top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopLeft));
            result.Add((new Rectangle((int)(bounds.Right - halfHandle), (int)(bounds.Top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopRight));
            result.Add((new Rectangle((int)(bounds.Left - halfHandle), (int)(bounds.Bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomLeft));
            result.Add((new Rectangle((int)(bounds.Right - halfHandle), (int)(bounds.Bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomRight));

            return result;
        }

        public List<(Rectangle handleRect, SelectionInteractionState state)> GetTextHandles(AnnotationText text, Bitmap? pageImage, Rectangle? displayRect = null)
        {
            var result = new List<(Rectangle, SelectionInteractionState)>();
            if (text == null || pageImage == null) return result;

            var bounds = GetTextScreenBounds(text, pageImage, displayRect);
            if (bounds.Width <= 0 || bounds.Height <= 0) return result;

            float halfHandle = HandleSize / 2;

            result.Add((new Rectangle((int)(bounds.Left - halfHandle), (int)(bounds.Top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopLeft));
            result.Add((new Rectangle((int)(bounds.Right - halfHandle), (int)(bounds.Top - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingTopRight));
            result.Add((new Rectangle((int)(bounds.Left - halfHandle), (int)(bounds.Bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomLeft));
            result.Add((new Rectangle((int)(bounds.Right - halfHandle), (int)(bounds.Bottom - halfHandle), (int)HandleSize, (int)HandleSize), SelectionInteractionState.ResizingBottomRight));

            return result;
        }

        public RectangleF GetTextNormalizedBounds(AnnotationText text, Bitmap? pageImage)
        {
            if (text == null || pageImage == null) return RectangleF.Empty;

            float scaleX = GetAnnotationFontScale(pageImage);
            var textSize = MeasureTextWithScreenDpi(text.Content, text.FontFamily, text.FontSize * scaleX);
            if (textSize.IsEmpty) textSize = new SizeF(100, 20);

            float padding = 8f;
            float w = (textSize.Width + padding * 2) / pageImage.Width;
            float h = (textSize.Height + padding * 2) / pageImage.Height;
            return new RectangleF(text.NormalizedX - (padding / pageImage.Width), text.NormalizedY - (padding / pageImage.Height), w, h);
        }

        public float GetAnnotationFontScale(Bitmap pageImage)
        {
            try
            {
                var (pageW, _) = _form.Presenter?.GetPageSize() ?? (0, 0);
                if (pageW > 0 && pageImage.Width > 0)
                    return (float)pageImage.Width / pageW;
            }
            catch { }
            return 1f;
        }

        public RectangleF ClientRectFromScreenRect(Rectangle screenRect)
        {
            if (_form.CurrentPageImage == null) return screenRect;

            var imgRect = _form.GetImageDisplayRect();
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            float scaleX = (float)imgWidth / imgRect.Width;
            float scaleY = (float)imgHeight / imgRect.Height;

            return new RectangleF(
                (screenRect.X - imgRect.X) * scaleX,
                (screenRect.Y - imgRect.Y) * scaleY,
                screenRect.Width * scaleX,
                screenRect.Height * scaleY);
        }

        #endregion

        #region 双击检测（用于选中模式）

        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTime_ms = 400;
        private const int DoubleClickDistance = 5;

        public bool IsDoubleClick(Point clientPoint)
        {
            var now = DateTime.Now;
            var timeDiff = (now - _lastClickTime).TotalMilliseconds;
            var dist = Math.Abs(clientPoint.X - _lastClickLocation.X) + Math.Abs(clientPoint.Y - _lastClickLocation.Y);

            if (timeDiff < DoubleClickTime_ms && timeDiff > 0 && dist < DoubleClickDistance)
            {
                _lastClickTime = DateTime.MinValue;
                return true;
            }

            _lastClickTime = now;
            _lastClickLocation = clientPoint;
            return false;
        }

        public void ResetLastClick()
        {
            _lastClickTime = DateTime.MinValue;
        }

        #endregion

        #region 绘制

        public void DrawSelectionVisual(Graphics g, Rectangle imgRect, int pageIndex)
        {
            if (pageIndex >= 0 && pageIndex != _form.CurrentPageIndex) return;

            if (SelectedHighlight != null && _form.CurrentPageImage != null)
            {
                DrawHighlightSelection(g, imgRect);
                return;
            }

            if (SelectedText != null && _form.CurrentPageImage != null)
            {
                DrawTextSelection(g, imgRect);
                return;
            }

            if (SelectedStroke == null || _form.CurrentPageImage == null) return;

            var shapeType = SelectedStroke.ShapeType ?? string.Empty;
            if (shapeType is "Pen" or "Strikethrough" && SelectedStroke.Points.Length > 4)
            {
                DrawSimpleSelectionBox(g, imgRect);
                return;
            }

            if (SelectedStroke.Points == null || SelectedStroke.Points.Length < 4) return;

            var (bounds, _, handles) = GetSelectionBounds(SelectedStroke, _form.CurrentPageImage, imgRect);
            if (bounds.Width <= 0 && bounds.Height <= 0) return;

            DrawSelectionFillAndBorder(g, bounds);
            DrawCornerIndicators(g, bounds);
            DrawResizeHandles(g, handles);
        }

        private void DrawHighlightSelection(Graphics g, Rectangle imgRect)
        {
            var hlBounds = GetHighlightScreenBounds(SelectedHighlight!, _form.CurrentPageImage!, imgRect);
            if (hlBounds.Width <= 0 || hlBounds.Height <= 0) return;

            DrawSelectionFillAndBorder(g, hlBounds);
            var hlHandles = GetHighlightHandles(SelectedHighlight!, _form.CurrentPageImage!, imgRect);
            DrawResizeHandles(g, hlHandles);
        }

        private void DrawTextSelection(Graphics g, Rectangle imgRect)
        {
            var textBounds = GetTextScreenBounds(SelectedText!, _form.CurrentPageImage!, imgRect);
            if (textBounds.Width <= 0 || textBounds.Height <= 0) return;

            DrawSelectionFillAndBorder(g, textBounds);
            var textHandles = GetTextHandles(SelectedText!, _form.CurrentPageImage!, imgRect);
            DrawResizeHandles(g, textHandles);
        }

        private static void DrawSelectionFillAndBorder(Graphics g, RectangleF bounds)
        {
            using var fillBrush = new SolidBrush(Color.FromArgb(20, 64, 150, 255));
            g.FillRectangle(fillBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            using var borderPenOuter = new Pen(Color.FromArgb(120, 64, 150, 255), SelectionBorderWidth + 2);
            borderPenOuter.DashStyle = DashStyle.Dash;
            g.DrawRectangle(borderPenOuter, bounds.X - 1, bounds.Y - 1, bounds.Width + 2, bounds.Height + 2);

            using var borderPenInner = new Pen(Color.FromArgb(200, 64, 150, 255), SelectionBorderWidth);
            g.DrawRectangle(borderPenInner, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        private static void DrawCornerIndicators(Graphics g, RectangleF bounds)
        {
            float cornerLen = Math.Min(14, Math.Min(bounds.Width / 3, bounds.Height / 3));
            if (cornerLen <= 4) return;

            using var cornerPen = new Pen(Color.FromArgb(64, 150, 255), 3);
            cornerPen.StartCap = LineCap.Round;
            cornerPen.EndCap = LineCap.Round;
            g.DrawLine(cornerPen, bounds.X, bounds.Y + cornerLen, bounds.X, bounds.Y);
            g.DrawLine(cornerPen, bounds.X, bounds.Y, bounds.X + cornerLen, bounds.Y);
            g.DrawLine(cornerPen, bounds.Right - cornerLen, bounds.Y, bounds.Right, bounds.Y);
            g.DrawLine(cornerPen, bounds.Right, bounds.Y, bounds.Right, bounds.Y + cornerLen);
            g.DrawLine(cornerPen, bounds.X, bounds.Bottom - cornerLen, bounds.X, bounds.Bottom);
            g.DrawLine(cornerPen, bounds.X, bounds.Bottom, bounds.X + cornerLen, bounds.Bottom);
            g.DrawLine(cornerPen, bounds.Right - cornerLen, bounds.Bottom, bounds.Right, bounds.Bottom);
            g.DrawLine(cornerPen, bounds.Right, bounds.Bottom, bounds.Right, bounds.Bottom - cornerLen);
        }

        public void DrawSimpleSelectionBox(Graphics g, Rectangle imgRect)
        {
            if (SelectedStroke == null || _form.CurrentPageImage == null) return;

            var (bounds, _, _) = GetSelectionBounds(SelectedStroke, _form.CurrentPageImage, imgRect);
            if (bounds.Width <= 0 && bounds.Height <= 0) return;

            using var fillBrush = new SolidBrush(Color.FromArgb(15, 64, 150, 255));
            g.FillRectangle(fillBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            using var borderPen = new Pen(Color.FromArgb(64, 150, 255), 2f);
            borderPen.DashStyle = DashStyle.Dash;
            g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public void DrawEraserHoverVisual(Graphics g, Rectangle imgRect, int pageIndex)
        {
            if (HoveredStroke == null || _form.CurrentPageImage == null) return;

            var (bounds, _, _) = GetSelectionBounds(HoveredStroke, _form.CurrentPageImage, imgRect);
            if (bounds.Width <= 0 && bounds.Height <= 0) return;

            using var fillBrush = new SolidBrush(Color.FromArgb(40, 255, 0, 0));
            g.FillRectangle(fillBrush, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            using var borderPen = new Pen(Color.FromArgb(200, 255, 0, 0), 3f);
            borderPen.DashStyle = DashStyle.Dash;
            g.DrawRectangle(borderPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        }

        public void DrawResizeHandles(Graphics g, List<(Rectangle handleRect, SelectionInteractionState state)> handles)
        {
            foreach (var (handleRect, state) in handles)
            {
                using var glowBrush = new SolidBrush(Color.FromArgb(60, 64, 150, 255));
                g.FillEllipse(glowBrush, handleRect.X - 2, handleRect.Y - 2, handleRect.Width + 4, handleRect.Height + 4);

                using var handleBrush = new SolidBrush(Color.White);
                g.FillEllipse(handleBrush, handleRect);

                using var handlePen = new Pen(Color.FromArgb(64, 150, 255), 1.5f);
                g.DrawEllipse(handlePen, handleRect);
            }
        }

        public static SizeF MeasureTextWithScreenDpi(string content, string fontFamily, float fontSize)
        {
            try
            {
                using var font = new Font(fontFamily, fontSize);
                return TextRenderer.MeasureText(content, font);
            }
            catch
            {
                try
                {
                    using var font = new Font("Microsoft YaHei UI", fontSize);
                    return TextRenderer.MeasureText(content, font);
                }
                catch
                {
                    return SizeF.Empty;
                }
            }
        }

        public static Rectangle GetNormalizedDragRect(Point p1, Point p2)
        {
            return new Rectangle(
                Math.Min(p1.X, p2.X),
                Math.Min(p1.Y, p2.Y),
                Math.Abs(p2.X - p1.X),
                Math.Abs(p2.Y - p1.Y));
        }

        #endregion
    }
}