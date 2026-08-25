using LearningAssistant.Forms;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Managers
{
    public enum AnnotationToolMode
    {
        None,
        Select,
        Highlight,
        Rectangle,
        Ellipse,
        Arrow,
        Pen,
        Mosaic,
        Strikethrough,
        Text
    }

    public class PdfReaderNavigationManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;

        private int _zoomLevel = 100;
        private bool _isSelecting = false;
        private bool _isDrawing = false;
        private bool _isDragging = false;
        private bool _isLocked = false;

        private AnnotationToolMode _currentToolMode = AnnotationToolMode.Highlight;

        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private Point _dragStart = Point.Empty;
        private Point _imageOffset = Point.Empty;
        private Rectangle? _lastSelectionRect = null;
        private Rectangle? _pendingHighlightRect = null;

        private bool _isOcrPanelDragging = false;
        private Point _ocrPanelStartPoint = Point.Empty;
        private bool _isDoubleClickPending = false;
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTime_ms = 200;
        private const int DoubleClickDistance = 5;

        private System.Windows.Forms.Timer? _longPressTimer;
        private bool _isLongPressPending = false;
        private Point _longPressStartLocation = Point.Empty;
        private const int LongPressTime_ms = 300;
        private bool _longPressDragStarted = false;

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;

        private bool _isAnimating = false;
        private int _transitionStep = 0;
        private bool _transitionFadeOut = false;

        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;
        private Bitmap? _secondAnnotationBitmap;
        private Graphics? _secondAnnotationGraphics;
        private List<PointF>? _currentStrokePoints;
        private Pen? _drawingPen;
        private Color _penColor = Color.Black;
        private float _penWidth = 3f;

        private PointF? _shapeStartPoint;
        private PointF? _shapeEndPoint;
        private bool _isDrawingShape = false;
        private int _drawingPageIndex = -1;

        private readonly Stack<AnnotationStroke> _strokeUndoStack = new Stack<AnnotationStroke>();
        private const int MaxUndoStackSize = 50;

        /// <summary>
        /// 当一个画笔/标注撤销动作被压入内部撤销栈时触发。
        /// 订阅方（如主窗体）可据此把该操作类型记录到统一撤销栈中，
        /// 以便工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
        /// </summary>
        public event EventHandler? UndoActionRecorded;

        private AnnotationStroke? _selectedStroke;
        private int _selectedStrokeIndex = -1;
        private const float HitTestThreshold = 25f;

        public int ZoomLevel => _zoomLevel;
        public bool IsLocked => _isLocked;
        public Rectangle? LastSelectionRect => _lastSelectionRect ?? _pendingHighlightRect;
        public Point ImageOffset => _imageOffset;
        public AnnotationToolMode CurrentToolMode => _currentToolMode;
        public Color PenColor => _penColor;
        public float PenWidth => _penWidth;
        public bool IsDrawing => _isDrawing;
        public List<PointF>? CurrentStrokePoints => _currentStrokePoints;

        public Func<bool>? IsHighlightModeCallback { get; set; }
        public Action<Rectangle>? AddHighlightCallback { get; set; }
        public Action<Point>? AddTextCallback { get; set; }

        public PdfReaderNavigationManager(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));

            _drawingPen = new Pen(_penColor, _penWidth);
            _drawingPen.StartCap = LineCap.Round;
            _drawingPen.EndCap = LineCap.Round;
            _drawingPen.LineJoin = LineJoin.Round;
            InitializeLongPressTimer();
        }

        public void SetToolMode(AnnotationToolMode mode)
        {
            _currentToolMode = mode;
            _isDrawing = false;
            _isSelecting = false;
            _currentStrokePoints = null;
            _selectedStroke = null;
            _selectedStrokeIndex = -1;

            if (_form.PictureBoxPdf != null)
            {
                _form.PictureBoxPdf.Cursor = mode == AnnotationToolMode.Select ? Cursors.Hand : Cursors.Default;
            }

            _form.PictureBoxPdf.Invalidate();
        }

        public void SetPenColor(Color color)
        {
            _penColor = color;
            if (_drawingPen != null)
            {
                _drawingPen.Dispose();
            }
            _drawingPen = new Pen(_penColor, _penWidth);
            _drawingPen.StartCap = LineCap.Round;
            _drawingPen.EndCap = LineCap.Round;
            _drawingPen.LineJoin = LineJoin.Round;
        }

        public void SetPenWidth(float width)
        {
            _penWidth = Math.Max(1f, Math.Min(20f, width));
            if (_drawingPen != null)
            {
                _drawingPen.Dispose();
            }
            _drawingPen = new Pen(_penColor, _penWidth);
            _drawingPen.StartCap = LineCap.Round;
            _drawingPen.EndCap = LineCap.Round;
            _drawingPen.LineJoin = LineJoin.Round;
        }

        private void InitializeLongPressTimer()
        {
            _longPressTimer = new System.Windows.Forms.Timer();
            _longPressTimer.Interval = LongPressTime_ms;
            _longPressTimer.Tick += LongPressTimer_Tick;
        }

        public void Zoom(int value)
        {
            if (_isLocked) return;

            _zoomLevel = value;
            _form.LabelZoom.Text = $"{_zoomLevel}%";

            Task.Run(async () =>
            {
                try
                {
                    var page = int.TryParse(_form.TextBoxPage.Text, out var p) ? p - 1 : 0;
                    int targetW = (int)(_form.PictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
                    int targetH = (int)(_form.PictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
                    var bmp = await _form.Presenter!.RenderPageAsync(page, Math.Max(1, targetW), Math.Max(1, targetH));
                    if (bmp != null)
                    {
                        _form.Form.BeginInvoke(() => _form.DisplayImage(new Bitmap(new MemoryStream(bmp))));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Zoom operation cancelled, ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error rendering page during zoom");
                }
            });
        }

        public void ZoomByMouseWheel(int delta, bool ctrlDown)
        {
            try
            {
                if (delta == 0) return;

                if (ctrlDown)
                {
                    if (delta > 0) _zoomLevel = Math.Min(400, _zoomLevel + 10);
                    else _zoomLevel = Math.Max(10, _zoomLevel - 10);

                    Task.Run(async () =>
                    {
                        try
                        {
                            var page = int.TryParse(_form.TextBoxPage.Text, out var p) ? p - 1 : 0;
                            int targetW = (int)(_form.PictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
                            int targetH = (int)(_form.PictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
                            var bmp = await _form.Presenter!.RenderPageAsync(page, Math.Max(1, targetW), Math.Max(1, targetH));
                            if (bmp != null)
                            {
                                _form.Form.BeginInvoke(() => _form.DisplayImage(new Bitmap(new MemoryStream(bmp))));
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Render cancelled, ignore
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error rendering page during zoom");
                        }
                    });
                }
                else
                {
                    if (_form.Presenter != null)
                    {
                        if (delta < 0)
                        {
                            _form.Presenter.NextPage();
                        }
                        else
                        {
                            _form.Presenter.PreviousPage();
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Zoom cancelled, ignore
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation in ZoomByMouseWheel");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ZoomByMouseWheel");
            }
        }

        public void ResetZoom()
        {
            _zoomLevel = 100;
            _imageOffset = Point.Empty;
            _form.TrackBarZoom.Value = 100;
            _form.LabelZoom.Text = "100%";

            Task.Run(async () =>
            {
                try
                {
                    var page = int.TryParse(_form.TextBoxPage.Text, out var p) ? p - 1 : 0;
                    var bmp = await _form.Presenter!.RenderPageAsync(page, _form.PictureBoxPdf.ClientSize.Width, _form.PictureBoxPdf.ClientSize.Height);
                    if (bmp != null)
                    {
                        _form.Form.BeginInvoke(() => _form.DisplayImage(new Bitmap(new MemoryStream(bmp))));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Reset cancelled, ignore
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resetting zoom");
                }
            });
        }

        public void ToggleLockView()
        {
            _isLocked = !_isLocked;
            if (_form.ButtonLockView != null)
            {
                _form.ButtonLockView.Text = _isLocked ? "🔒" : "🔓";
                _form.ButtonLockView.BackColor = _isLocked ? Color.LightSalmon : Color.White;
            }

            _form.TrackBarZoom.Enabled = !_isLocked;
        }

        public void NavigateToPage(int pageIndex)
        {
            _form.Presenter?.RenderPage(pageIndex);
        }

        public void NextPage()
        {
            _form.Presenter?.NextPage();
        }

        public void PreviousPage()
        {
            _form.Presenter?.PreviousPage();
        }

        public void StartPageTransition(bool forward)
        {
            if (_isAnimating || _form.PageTransitionOverlay == null) return;

            _isAnimating = true;
            _transitionStep = 0;
            _transitionFadeOut = true;

            bool isNightMode = _form.IsNightMode;

            _form.PageTransitionOverlay.Visible = true;
            _form.PageTransitionOverlay.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;

            if (_form.PageTransitionTimer != null)
            {
                _form.PageTransitionTimer.Interval = 25;
                _form.PageTransitionTimer.Start();
            }
        }

        public void PageTransitionTimer_Tick()
        {
            if (_form.PageTransitionOverlay == null || !_isAnimating) return;

            _transitionStep++;

            bool isNightMode = _form.IsNightMode;

            int baseR = isNightMode ? 30 : 255;
            int baseG = isNightMode ? 30 : 255;
            int baseB = isNightMode ? 30 : 255;

            if (_transitionFadeOut)
            {
                int alpha = 255 - (_transitionStep * 30);
                if (alpha <= 0)
                {
                    alpha = 0;
                    _transitionFadeOut = false;
                    _transitionStep = 0;
                }
                _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, baseR, baseG, baseB);
            }
            else
            {
                int alpha = _transitionStep * 30;
                if (alpha >= 255)
                {
                    alpha = 255;
                    _form.PageTransitionTimer?.Stop();
                    _isAnimating = false;
                    _form.PageTransitionOverlay.Visible = false;
                    return;
                }
                _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, baseR, baseG, baseB);
            }
        }

        public void MouseDown(object? sender, MouseEventArgs e)
        {
            try
            {
                StopLongPressTimer();

                if (e.Button == MouseButtons.Left)
                {
                    // Ctrl + 左键：按住拖动平移页面（覆盖当前工具模式，避免进入绘制）
                    if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        StartDragging(e.Location);
                        return;
                    }

                    switch (_currentToolMode)
                    {
                        case AnnotationToolMode.Select:
                            _logger.LogInformation("MouseDown Left: Select mode, checking for stroke hit at {X},{Y}", e.Location.X, e.Location.Y);
                            HandleSelectModeClick(e.Location);
                            return;
                        case AnnotationToolMode.Highlight:
                            _logger.LogInformation("MouseDown Left: Starting highlight selection at {X},{Y}", e.Location.X, e.Location.Y);
                            _isDrawingShape = true;
                            _drawingPageIndex = _form.GetPageAtPoint(e.Location).pageIndex;
                            EnsureAnnotationBitmap();
                            _shapeStartPoint = ClientToImage(e.Location);
                            _shapeEndPoint = _shapeStartPoint;
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Pen:
                            _logger.LogInformation("MouseDown Left: Starting pen drawing at {X},{Y}", e.Location.X, e.Location.Y);
                            _isDrawing = true;
                            _drawingPageIndex = _form.GetPageAtPoint(e.Location).pageIndex;
                            EnsureAnnotationBitmap();
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            var imgPt = ClientToImage(e.Location);
                            _currentStrokePoints = new List<PointF>() { imgPt };
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Strikethrough:
                            _logger.LogInformation("MouseDown Left: Starting strikethrough drawing at {X},{Y}", e.Location.X, e.Location.Y);
                            _isDrawing = true;
                            _drawingPageIndex = _form.GetPageAtPoint(e.Location).pageIndex;
                            EnsureAnnotationBitmap();
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            var strikethroughImgPt = ClientToImage(e.Location);
                            _currentStrokePoints = new List<PointF>() { strikethroughImgPt };
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Rectangle:
                        case AnnotationToolMode.Ellipse:
                        case AnnotationToolMode.Arrow:
                        case AnnotationToolMode.Mosaic:
                            _logger.LogInformation("MouseDown Left: Starting shape drawing ({Mode}) at {X},{Y}", _currentToolMode, e.Location.X, e.Location.Y);
                            _isDrawingShape = true;
                            _drawingPageIndex = _form.GetPageAtPoint(e.Location).pageIndex;
                            EnsureAnnotationBitmap();
                            _shapeStartPoint = ClientToImage(e.Location);
                            _shapeEndPoint = _shapeStartPoint;
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Text:
                            _logger.LogInformation("MouseDown Left: Text tool clicked at {X},{Y}", e.Location.X, e.Location.Y);
                            AddTextCallback?.Invoke(e.Location);
                            break;
                    }
                    return;
                }

                if (e.Button == MouseButtons.Right)
                {
                    var now = DateTime.Now;
                    var timeDiff = (now - _lastClickTime).TotalMilliseconds;
                    var distance = Math.Sqrt(Math.Pow(e.Location.X - _lastClickLocation.X, 2) + Math.Pow(e.Location.Y - _lastClickLocation.Y, 2));

                    if (timeDiff < DoubleClickTime_ms && distance < DoubleClickDistance)
                    {
                        _logger.LogInformation("MouseDown Right: DoubleClick detected, canceling selection");
                        _isDoubleClickPending = true;
                        _isSelecting = false;
                        _isDrawing = false;
                        _lastClickTime = DateTime.MinValue;
                        _lastClickLocation = Point.Empty;
                        return;
                    }

                    _isDoubleClickPending = false;
                    _lastClickTime = now;
                    _lastClickLocation = e.Location;

                    if (_currentToolMode == AnnotationToolMode.Pen || _currentToolMode == AnnotationToolMode.Strikethrough || _isDrawing)
                    {
                        _logger.LogInformation("MouseDown Right: Drawing mode, starting annotation");
                        _isDrawing = true;
                        EnsureAnnotationBitmap();
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        var imgPt = ClientToImage(e.Location);
                        _currentStrokePoints = new List<PointF>() { imgPt };
                        _form.PictureBoxPdf.Invalidate();
                        return;
                    }

                    if (_currentToolMode == AnnotationToolMode.Rectangle ||
                        _currentToolMode == AnnotationToolMode.Ellipse ||
                        _currentToolMode == AnnotationToolMode.Arrow ||
                        _currentToolMode == AnnotationToolMode.Mosaic)
                    {
                        _isDrawingShape = true;
                        EnsureAnnotationBitmap();
                        _shapeStartPoint = ClientToImage(e.Location);
                        _shapeEndPoint = _shapeStartPoint;
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        _form.PictureBoxPdf.Invalidate();
                        return;
                    }

                    if (_currentToolMode == AnnotationToolMode.Highlight)
                    {
                        _isLongPressPending = true;
                        _longPressStartLocation = e.Location;
                        _longPressDragStarted = false;
                        StartLongPressTimer();
                        return;
                    }

                    _logger.LogInformation("MouseDown Right: Starting selection at {X},{Y}", e.Location.X, e.Location.Y);
                    _isSelecting = true;
                    _selectStart = e.Location;
                    _selectEnd = e.Location;
                    _form.PictureBoxPdf.Invalidate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MouseDown");
                _isSelecting = false;
                _isDrawing = false;
                StopLongPressTimer();
            }
        }

        public void MouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                if (_isLocked) return;

                if (_isLongPressPending && !_longPressDragStarted)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(e.Location.X - _longPressStartLocation.X, 2) +
                        Math.Pow(e.Location.Y - _longPressStartLocation.Y, 2)
                    );

                    if (distance > DoubleClickDistance)
                    {
                        StopLongPressTimer();
                        StartDragging(e.Location);
                        return;
                    }
                }

                if (_isDrawing)
                {
                    _selectEnd = e.Location;
                    var imgPt = ClientToImage(e.Location);
                    if (_currentStrokePoints != null)
                    {
                        _currentStrokePoints.Add(imgPt);
                    }
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_isDrawingShape)
                {
                    _selectEnd = e.Location;
                    var endPt = ClientToImage(e.Location);

                    if (_shapeStartPoint.HasValue && (Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        var startPt = _shapeStartPoint.Value;
                        var dx = endPt.X - startPt.X;
                        var dy = endPt.Y - startPt.Y;

                        if (_currentToolMode == AnnotationToolMode.Rectangle ||
                            _currentToolMode == AnnotationToolMode.Ellipse ||
                            _currentToolMode == AnnotationToolMode.Mosaic)
                        {
                            var maxSide = Math.Max(Math.Abs(dx), Math.Abs(dy));
                            endPt = new PointF(
                                startPt.X + Math.Sign(dx) * maxSide,
                                startPt.Y + Math.Sign(dy) * maxSide);
                        }
                        else if (_currentToolMode == AnnotationToolMode.Arrow)
                        {
                            if (Math.Abs(dx) > Math.Abs(dy))
                            {
                                endPt = new PointF(endPt.X, startPt.Y);
                            }
                            else
                            {
                                endPt = new PointF(startPt.X, endPt.Y);
                            }
                        }
                    }

                    _shapeEndPoint = endPt;
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_isSelecting)
                {
                    _selectEnd = e.Location;
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_isDragging || _longPressDragStarted)
                {
                    var deltaX = e.Location.X - _dragStart.X;
                    var deltaY = e.Location.Y - _dragStart.Y;
                    _imageOffset = new Point(_imageOffset.X + deltaX, _imageOffset.Y + deltaY);
                    _dragStart = e.Location;
                    _form.PictureBoxPdf.Invalidate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MouseMove");
            }
        }

        public void MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                StopLongPressTimer();
                _isLongPressPending = false;

                if (_isDoubleClickPending)
                {
                    _isDoubleClickPending = false;
                    _isSelecting = false;
                    _isDrawing = false;
                    return;
                }

                if (_isDrawing)
                {
                    _isDrawing = false;
                    try
                    {
                        if (_annotationBitmap != null && _currentStrokePoints != null && _currentStrokePoints.Count >= 2)
                        {
                            bool isSecondPage = _drawingPageIndex > _form.CurrentPageIndex;
                            Graphics activeGfx = isSecondPage ? _secondAnnotationGraphics! : _annotationGraphics!;
                            Bitmap activeBmp = isSecondPage ? _secondAnnotationBitmap! : _annotationBitmap!;

                            activeGfx.SmoothingMode = SmoothingMode.AntiAlias;

                            Color drawColor = _currentToolMode == AnnotationToolMode.Strikethrough ? Color.Red : _penColor;
                            float drawWidth = _currentToolMode == AnnotationToolMode.Strikethrough ? 6f : _penWidth;

                            using var drawPen = new Pen(drawColor, drawWidth);
                            drawPen.StartCap = LineCap.Round;
                            drawPen.EndCap = LineCap.Round;
                            drawPen.LineJoin = LineJoin.Round;

                            activeGfx.DrawLines(drawPen, _currentStrokePoints.ToArray());
                            _form.Presenter?.SaveAnnotationForPage(_drawingPageIndex);
                            var imgW = activeBmp.Width;
                            var imgH = activeBmp.Height;
                            var pts = new List<float>();
                            foreach (var pt in _currentStrokePoints)
                            {
                                pts.Add(pt.X / imgW);
                                pts.Add(pt.Y / imgH);
                            }

                            var stroke = new AnnotationStroke
                            {
                                Points = pts.ToArray(),
                                ColorArgb = drawColor.ToArgb(),
                                Thickness = drawWidth,
                                CreatedAt = DateTime.Now
                            };

                            PushStrokeToUndoStack(stroke);
                            _form.Presenter?.AddAnnotationStroke(pts.ToArray(), drawColor.ToArgb(), drawWidth, imgW, imgH, null, _drawingPageIndex);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving annotation");
                    }
                    finally { _currentStrokePoints = null; _drawingPageIndex = -1; }
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_isDrawingShape)
                {
                    _isDrawingShape = false;
                    try
                    {
                        if (_annotationBitmap != null && _shapeStartPoint.HasValue && _shapeEndPoint.HasValue)
                        {
                            var startPt = _shapeStartPoint.Value;
                            var endPt = _shapeEndPoint.Value;
                            var rect = new RectangleF(
                                Math.Min(startPt.X, endPt.X),
                                Math.Min(startPt.Y, endPt.Y),
                                Math.Abs(endPt.X - startPt.X),
                                Math.Abs(endPt.Y - startPt.Y));

                            if (_currentToolMode == AnnotationToolMode.Highlight)
                            {
                                if (rect.Width > 0 && rect.Height > 0)
                                {
                                    var imgRect = _form.GetImageDisplayRect();

                                    if (_form.IsDualPage)

                                    {

                                        var centerPoint = new Point(_selectStart.X + (_selectEnd.X - _selectStart.X) / 2, _selectStart.Y + (_selectEnd.Y - _selectStart.Y) / 2);

                                        var (_, pageRect, _) = _form.GetPageAtPoint(centerPoint);

                                        imgRect = pageRect;

                                    }
                                    float scaleX = (float)imgRect.Width / _form.CurrentPageImage!.Width;
                                    float scaleY = (float)imgRect.Height / _form.CurrentPageImage.Height;

                                    var screenRect = new Rectangle(
                                        (int)(rect.X * scaleX + imgRect.X),
                                        (int)(rect.Y * scaleY + imgRect.Y),
                                        (int)(rect.Width * scaleX),
                                        (int)(rect.Height * scaleY));

                                    AddHighlightCallback?.Invoke(screenRect);
                                }
                            }
                            else
                            {
                                bool isSecondPage = _drawingPageIndex > _form.CurrentPageIndex;
                                Graphics activeGfx = isSecondPage ? _secondAnnotationGraphics! : _annotationGraphics!;
                                Bitmap activeBmp = isSecondPage ? _secondAnnotationBitmap! : _annotationBitmap!;

                                activeGfx.SmoothingMode = SmoothingMode.AntiAlias;

                                using var drawPen = new Pen(_penColor, _penWidth);
                                drawPen.StartCap = LineCap.Round;
                                drawPen.EndCap = LineCap.Round;

                                switch (_currentToolMode)
                                {
                                    case AnnotationToolMode.Rectangle:
                                        drawPen.DashStyle = DashStyle.Dash;
                                        activeGfx.DrawRectangle(drawPen, rect.X, rect.Y, rect.Width, rect.Height);
                                        break;
                                    case AnnotationToolMode.Ellipse:
                                        drawPen.DashStyle = DashStyle.Dash;
                                        activeGfx.DrawEllipse(drawPen, rect);
                                        break;
                                    case AnnotationToolMode.Arrow:
                                        drawPen.EndCap = LineCap.ArrowAnchor;
                                        activeGfx.DrawLine(drawPen, startPt, endPt);
                                        break;
                                    case AnnotationToolMode.Mosaic:
                                        ApplyMosaic(rect, 10, activeGfx, activeBmp);
                                        break;
                                }

                                _form.Presenter?.SaveAnnotationForPage(_drawingPageIndex);

                                var imgW = activeBmp.Width;
                                var imgH = activeBmp.Height;

                                var strokePts = new List<float>
                                {
                                    startPt.X / imgW, startPt.Y / imgH,
                                    endPt.X / imgW, endPt.Y / imgH
                                };

                                var stroke = new AnnotationStroke
                                {
                                    Points = strokePts.ToArray(),
                                    ColorArgb = _penColor.ToArgb(),
                                    Thickness = _penWidth,
                                    ShapeType = _currentToolMode.ToString(),
                                    CreatedAt = DateTime.Now
                                };

                                PushStrokeToUndoStack(stroke);
                                _form.Presenter?.AddAnnotationStroke(strokePts.ToArray(), _penColor.ToArgb(), _penWidth, imgW, imgH, _currentToolMode.ToString(), _drawingPageIndex);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving shape annotation");
                    }
                    finally
                    {
                        _shapeStartPoint = null;
                        _shapeEndPoint = null;
                        _drawingPageIndex = -1;
                    }
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_isSelecting)
                {
                    _isSelecting = false;
                    _selectEnd = e.Location;
                    _lastSelectionRect = GetSelectionRectangle(_selectStart, _selectEnd);

                    _logger.LogInformation("MouseUp: _isSelecting=true, _lastSelectionRect={X},{Y} {Width}x{Height}",
                        _lastSelectionRect?.X, _lastSelectionRect?.Y, _lastSelectionRect?.Width, _lastSelectionRect?.Height);

                    var isHighlightMode = _currentToolMode == AnnotationToolMode.Highlight;
                    _logger.LogInformation("MouseUp: isHighlightMode={IsHighlightMode}", isHighlightMode);

                    if (isHighlightMode && _lastSelectionRect.HasValue)
                    {
                        _logger.LogInformation("MouseUp: Calling AddHighlightCallback");
                        // 保存选择矩形用于显示
                        _pendingHighlightRect = _lastSelectionRect;
                        // 触发重绘，让用户看到选择矩形
                        _form.PictureBoxPdf.Invalidate();
                        // 调用添加高亮的回调
                        AddHighlightCallback?.Invoke(_lastSelectionRect.Value);
                        // 清除选择矩形
                        _lastSelectionRect = null;
                        // 延迟清除待处理矩形
                        StartClearPendingHighlightTimer();
                    }
                    else
                    {
                        _form.PictureBoxPdf.Invalidate();
                        _form.OnSelectOcrClicked();
                    }
                }

                if (_isDragging || _longPressDragStarted)
                {
                    _isDragging = false;
                    _longPressDragStarted = false;
                    _form.PictureBoxPdf.Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MouseUp");
                _isSelecting = false;
                _isDrawing = false;
                _isDragging = false;
                _longPressDragStarted = false;
                StopLongPressTimer();
            }
        }

        private void StartLongPressTimer()
        {
            if (_longPressTimer != null && !_longPressTimer.Enabled)
            {
                _longPressTimer.Start();
            }
        }

        private void StopLongPressTimer()
        {
            if (_longPressTimer != null && _longPressTimer.Enabled)
            {
                _longPressTimer.Stop();
            }
            _isLongPressPending = false;
        }

        private System.Windows.Forms.Timer? _clearPendingHighlightTimer;

        private void StartClearPendingHighlightTimer()
        {
            // 先停止旧的定时器
            _clearPendingHighlightTimer?.Stop();
            _clearPendingHighlightTimer?.Dispose();

            // 创建新的定时器，延迟清除待处理高亮矩形
            _clearPendingHighlightTimer = new System.Windows.Forms.Timer();
            _clearPendingHighlightTimer.Interval = 100; // 100ms 延迟
            _clearPendingHighlightTimer.Tick += (s, e) =>
            {
                _clearPendingHighlightTimer?.Stop();
                _pendingHighlightRect = null;
                _form.PictureBoxPdf?.Invalidate();
            };
            _clearPendingHighlightTimer.Start();
        }

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {
            StopLongPressTimer();
            if (_isLongPressPending)
            {
                StartDragging(_longPressStartLocation);
            }
        }

        private void StartDragging(Point startLocation)
        {
            if (_isLocked) return;

            _isDragging = true;
            _longPressDragStarted = true;
            _dragStart = startLocation;
            _form.PictureBoxPdf.Cursor = Cursors.Hand;
        }

        private void EnsureAnnotationBitmap()
        {
            try
            {
                bool isSecondPage = _drawingPageIndex > _form.CurrentPageIndex;
                Bitmap? pageImage = isSecondPage ? _form.SecondPageImage : _form.CurrentPageImage;

                if (pageImage == null)
                    return;

                int imgWidth, imgHeight;
                try
                {
                    imgWidth = pageImage.Width;
                    imgHeight = pageImage.Height;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogWarning("Image was disposed, cannot create annotation bitmap");
                    return;
                }

                ref Bitmap? bmpRef = ref isSecondPage ? ref _secondAnnotationBitmap : ref _annotationBitmap;
                ref Graphics? gfxRef = ref isSecondPage ? ref _secondAnnotationGraphics : ref _annotationGraphics;

                if (bmpRef != null)
                {
                    try
                    {
                        if (bmpRef.Width != imgWidth ||
                            bmpRef.Height != imgHeight)
                        {
                            gfxRef?.Dispose();
                            bmpRef?.Dispose();
                            bmpRef = null;
                            gfxRef = null;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        gfxRef?.Dispose();
                        bmpRef?.Dispose();
                        bmpRef = null;
                        gfxRef = null;
                    }
                }

                if (bmpRef == null)
                {
                    bmpRef = new Bitmap(imgWidth, imgHeight);
                    gfxRef = Graphics.FromImage(bmpRef);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnsureAnnotationBitmap");
                CleanupAnnotationBitmap();
            }
        }

        public void CleanupAnnotationBitmap()
        {
            try
            {
                _annotationGraphics?.Dispose();
                _annotationBitmap?.Dispose();
                _annotationGraphics = null;
                _annotationBitmap = null;

                _secondAnnotationGraphics?.Dispose();
                _secondAnnotationBitmap?.Dispose();
                _secondAnnotationGraphics = null;
                _secondAnnotationBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up annotation bitmap");
            }
        }

        public void LoadAnnotationsForCurrentPage()
        {
            try
            {
                if (_form.CurrentPageImage == null || _form.Presenter == null) return;

                int imgWidth = _form.CurrentPageImage.Width;
                int imgHeight = _form.CurrentPageImage.Height;

                var annotationBytes = _form.Presenter.LoadAnnotationForCurrentPage(imgWidth, imgHeight);
                ApplyLoadedAnnotationBitmap(annotationBytes != null ? new Bitmap(new MemoryStream(annotationBytes)) : null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading annotations for current page");
            }
        }

        /// <summary>
        /// 用已加载好的标注位图直接设置当前标注图层，避免再次读磁盘。
        /// 当 annotationBitmap 为 null 时创建空的透明图层。
        /// </summary>
        public void ApplyLoadedAnnotationBitmap(Bitmap? annotationBitmap)
        {
            try
            {
                if (annotationBitmap != null)
                {
                    CleanupAnnotationBitmap();
                    _annotationBitmap = new Bitmap(annotationBitmap);
                    _annotationGraphics = Graphics.FromImage(_annotationBitmap);
                    _annotationGraphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                else
                {
                    EnsureAnnotationBitmap();
                }

                _selectedStroke = null;
                _selectedStrokeIndex = -1;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error applying loaded annotation bitmap");
            }
        }

        private void HandleSelectModeClick(Point clientPoint)
        {
            if (_form.CurrentPageImage == null) return;

            var imgPoint = ClientToImage(clientPoint);
            var strokes = GetCurrentPageStrokes();

            _logger.LogInformation("Select mode click: client={ClientX},{ClientY}, img={ImgX},{ImgY}, strokes count={Count}",
                clientPoint.X, clientPoint.Y, imgPoint.X, imgPoint.Y, strokes.Count);

            for (int i = strokes.Count - 1; i >= 0; i--)
            {
                var stroke = strokes[i];
                if (HitTestStroke(stroke, imgPoint))
                {
                    _selectedStroke = stroke;
                    _selectedStrokeIndex = i;
                    _logger.LogInformation("Selected stroke at index {Index}, type={ShapeType}, deleting...", i, stroke.ShapeType);
                    DeleteSelectedStroke();
                    return;
                }
            }

            _selectedStroke = null;
            _selectedStrokeIndex = -1;
            _logger.LogInformation("No stroke hit - click at {X},{Y}", imgPoint.X, imgPoint.Y);
        }

        private bool HitTestStroke(AnnotationStroke stroke, PointF imgPoint)
        {
            if (stroke.Points == null || stroke.Points.Length < 4) return false;

            if (_form.CurrentPageImage == null) return false;
            int imgWidth = _form.CurrentPageImage.Width;
            int imgHeight = _form.CurrentPageImage.Height;

            var pageSize = _form.Presenter?.GetPageSize() ?? (0f, 0f);
            float pageWidth = pageSize.Width > 0 ? pageSize.Width : imgWidth;
            float pageHeight = pageSize.Height > 0 ? pageSize.Height : imgHeight;

            float scaleX = (float)imgWidth / pageWidth;
            float scaleY = (float)imgHeight / pageHeight;

            string shapeType = stroke.ShapeType ?? string.Empty;
            float hitThreshold = Math.Max(stroke.Thickness, HitTestThreshold);

            switch (shapeType)
            {
                case "Rectangle":
                case "Ellipse":
                    {
                        float x1 = stroke.Points[0] * pageWidth * scaleX;
                        float y1 = stroke.Points[1] * pageHeight * scaleY;
                        float x2 = stroke.Points[2] * pageWidth * scaleX;
                        float y2 = stroke.Points[3] * pageHeight * scaleY;

                        float left = Math.Min(x1, x2);
                        float right = Math.Max(x1, x2);
                        float top = Math.Min(y1, y2);
                        float bottom = Math.Max(y1, y2);

                        bool nearLeftEdge = Math.Abs(imgPoint.X - left) < hitThreshold && imgPoint.Y >= top - hitThreshold && imgPoint.Y <= bottom + hitThreshold;
                        bool nearRightEdge = Math.Abs(imgPoint.X - right) < hitThreshold && imgPoint.Y >= top - hitThreshold && imgPoint.Y <= bottom + hitThreshold;
                        bool nearTopEdge = Math.Abs(imgPoint.Y - top) < hitThreshold && imgPoint.X >= left - hitThreshold && imgPoint.X <= right + hitThreshold;
                        bool nearBottomEdge = Math.Abs(imgPoint.Y - bottom) < hitThreshold && imgPoint.X >= left - hitThreshold && imgPoint.X <= right + hitThreshold;

                        return nearLeftEdge || nearRightEdge || nearTopEdge || nearBottomEdge;
                    }
                case "Mosaic":
                    {
                        float x1 = stroke.Points[0] * pageWidth * scaleX;
                        float y1 = stroke.Points[1] * pageHeight * scaleY;
                        float x2 = stroke.Points[2] * pageWidth * scaleX;
                        float y2 = stroke.Points[3] * pageHeight * scaleY;

                        float left = Math.Min(x1, x2);
                        float right = Math.Max(x1, x2);
                        float top = Math.Min(y1, y2);
                        float bottom = Math.Max(y1, y2);

                        return imgPoint.X >= left && imgPoint.X <= right && imgPoint.Y >= top && imgPoint.Y <= bottom;
                    }
                case "Arrow":
                case "Pen":
                case "Strikethrough":
                default:
                    {
                        for (int i = 0; i < stroke.Points.Length - 3; i += 2)
                        {
                            float x1 = stroke.Points[i] * pageWidth * scaleX;
                            float y1 = stroke.Points[i + 1] * pageHeight * scaleY;
                            float x2 = stroke.Points[i + 2] * pageWidth * scaleX;
                            float y2 = stroke.Points[i + 3] * pageHeight * scaleY;

                            float distance = PointToLineDistance(imgPoint.X, imgPoint.Y, x1, y1, x2, y2);
                            if (distance < hitThreshold)
                            {
                                return true;
                            }
                        }
                        return false;
                    }
            }
        }

        private float PointToLineDistance(float px, float py, float x1, float y1, float x2, float y2)
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

            if (param < 0)
            {
                xx = x1;
                yy = y1;
            }
            else if (param > 1)
            {
                xx = x2;
                yy = y2;
            }
            else
            {
                xx = x1 + param * C;
                yy = y1 + param * D;
            }

            float dx = px - xx;
            float dy = py - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private List<AnnotationStroke> GetCurrentPageStrokes()
        {
            if (_form.Presenter == null) return new List<AnnotationStroke>();
            return _form.Presenter.GetCurrentPageStrokes().ToList();
        }

        public void DeleteSelectedStroke()
        {
            if (_selectedStrokeIndex < 0 || _form.Presenter == null) return;

            _form.Presenter.RemoveStrokeAtCurrentPage(_selectedStrokeIndex);
            _selectedStroke = null;
            _selectedStrokeIndex = -1;

            LoadAnnotationsForCurrentPage();
            _form.PictureBoxPdf.Invalidate();
        }

        public void ClearSelection()
        {
            _selectedStroke = null;
            _selectedStrokeIndex = -1;
            _form.PictureBoxPdf.Invalidate();
        }

        public void DrawAnnotations(Graphics g, Rectangle imgRect)
        {
            DrawAnnotations(g, imgRect, -1);
        }

        public void DrawAnnotations(Graphics g, Rectangle imgRect, int pageIndex)
        {
            try
            {
                // 双页模式下，_annotationBitmap 属于左页，_secondAnnotationBitmap 属于右页
                if (pageIndex < 0 || pageIndex == _form.CurrentPageIndex)
                {
                    if (_annotationBitmap != null)
                    {
                        g.DrawImage(_annotationBitmap, imgRect);
                    }
                }
                else if (pageIndex == _form.CurrentPageIndex + 1)
                {
                    if (_secondAnnotationBitmap != null)
                    {
                        g.DrawImage(_secondAnnotationBitmap, imgRect);
                    }
                }

                if (_isDrawing && _currentStrokePoints != null && _currentStrokePoints.Count >= 2)
                {
                    if (pageIndex >= 0 && pageIndex != _drawingPageIndex)
                        return;

                    Bitmap? srcImage = _form.CurrentPageImage;
                    if (pageIndex > _form.CurrentPageIndex && _form.SecondPageImage != null)
                        srcImage = _form.SecondPageImage;

                    var scaleX = (float)imgRect.Width / srcImage.Width;
                    var scaleY = (float)imgRect.Height / srcImage.Height;

                    var screenPoints = new List<Point>();
                    foreach (var pt in _currentStrokePoints)
                    {
                        screenPoints.Add(new Point(
                            (int)(pt.X * scaleX + imgRect.X),
                            (int)(pt.Y * scaleY + imgRect.Y)));
                    }

                    Color drawColor = _currentToolMode == AnnotationToolMode.Strikethrough ? Color.Red : _penColor;
                    float drawWidth = _currentToolMode == AnnotationToolMode.Strikethrough ? 4f : _penWidth;

                    using var pen = new Pen(drawColor, drawWidth);
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, screenPoints.ToArray());
                }

                if (_isDrawingShape && _shapeStartPoint.HasValue && _shapeEndPoint.HasValue)
                {
                    if (pageIndex >= 0 && pageIndex != _drawingPageIndex)
                        return;

                    Bitmap? srcImage = _form.CurrentPageImage;
                    if (pageIndex > _form.CurrentPageIndex && _form.SecondPageImage != null)
                        srcImage = _form.SecondPageImage;

                    var scaleX = (float)imgRect.Width / srcImage.Width;
                    var scaleY = (float)imgRect.Height / srcImage.Height;

                    var startPt = _shapeStartPoint.Value;
                    var endPt = _shapeEndPoint.Value;
                    var screenStart = new Point(
                        (int)(startPt.X * scaleX + imgRect.X),
                        (int)(startPt.Y * scaleY + imgRect.Y));
                    var screenEnd = new Point(
                        (int)(endPt.X * scaleX + imgRect.X),
                        (int)(endPt.Y * scaleY + imgRect.Y));

                    var rect = new Rectangle(
                        Math.Min(screenStart.X, screenEnd.X),
                        Math.Min(screenStart.Y, screenEnd.Y),
                        Math.Abs(screenEnd.X - screenStart.X),
                        Math.Abs(screenEnd.Y - screenStart.Y));

                    using var pen = new Pen(_penColor, _penWidth);
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;

                    switch (_currentToolMode)
                    {
                        case AnnotationToolMode.Highlight:
                            {
                                var highlightColor = Color.FromArgb(120, 255, 255, 0);
                                using var brush = new SolidBrush(highlightColor);
                                g.FillRectangle(brush, rect);
                                using var highlightPen = new Pen(Color.FromArgb(180, 255, 150, 0), 2);
                                g.DrawRectangle(highlightPen, rect);
                                break;
                            }
                        case AnnotationToolMode.Rectangle:
                            g.DrawRectangle(pen, rect);
                            break;
                        case AnnotationToolMode.Ellipse:
                            g.DrawEllipse(pen, rect);
                            break;
                        case AnnotationToolMode.Arrow:
                            pen.EndCap = LineCap.ArrowAnchor;
                            g.DrawLine(pen, screenStart, screenEnd);
                            break;
                        case AnnotationToolMode.Mosaic:
                            {
                                using var brush = new SolidBrush(Color.FromArgb(80, 128, 128, 128));
                                g.FillRectangle(brush, rect);
                                g.DrawRectangle(pen, rect);
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error drawing annotations");
            }
        }

        private PointF ClientToImage(Point clientPt)
        {
            try
            {
                int imgWidth, imgHeight;
                Bitmap? srcImage = _form.CurrentPageImage;

                if (_form.IsDualPage)
                {
                    var (_, pageRect, pageImage) = _form.GetPageAtPoint(clientPt);
                    if (pageImage != null)
                    {
                        srcImage = pageImage;
                    }
                }

                if (srcImage == null)
                    return new PointF(clientPt.X, clientPt.Y);

                try
                {
                    imgWidth = srcImage.Width;
                    imgHeight = srcImage.Height;
                }
                catch (ObjectDisposedException)
                {
                    return new PointF(clientPt.X, clientPt.Y);
                }

                var imgRect = _form.GetImageDisplayRect();

                if (_form.IsDualPage)
                {
                    var (_, pageRect, _) = _form.GetPageAtPoint(clientPt);
                    imgRect = pageRect;
                }

                if (imgRect.Width <= 0 || imgRect.Height <= 0)
                {
                    var scaleX = (float)imgWidth / _form.PictureBoxPdf.ClientSize.Width;
                    var scaleY = (float)imgHeight / _form.PictureBoxPdf.ClientSize.Height;
                    return new PointF(clientPt.X * scaleX, clientPt.Y * scaleY);
                }

                float relX = (float)(clientPt.X - imgRect.X) / imgRect.Width;
                float relY = (float)(clientPt.Y - imgRect.Y) / imgRect.Height;

                relX = Math.Max(0, Math.Min(1, relX));
                relY = Math.Max(0, Math.Min(1, relY));

                return new PointF(relX * imgWidth, relY * imgHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClientToImage");
                return new PointF(clientPt.X, clientPt.Y);
            }
        }

        public Rectangle GetSelectionRectangle(Point start, Point end)
        {
            return new Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y)
            );
        }

        public void PanelNavigation_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isNavPanelDragging = true;
                _navPanelStartPoint = _form.Form.PointToScreen(e.Location);
                _form.PanelNavigation.Cursor = Cursors.SizeAll;
                _form.PanelNavigation.Capture = true;
            }
        }

        public void PanelNavigation_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isNavPanelDragging)
            {
                Point currentScreenPoint = _form.Form.PointToScreen(e.Location);
                int deltaX = currentScreenPoint.X - _navPanelStartPoint.X;
                int deltaY = currentScreenPoint.Y - _navPanelStartPoint.Y;

                int newX = _form.PanelNavigation.Left + deltaX;
                int newY = _form.PanelNavigation.Top + deltaY;

                int leftBoundary = 0;
                int rightBoundary = _form.Form.ClientSize.Width - _form.PanelNavigation.Width;

                newX = Math.Max(leftBoundary, Math.Min(newX, rightBoundary));
                newY = Math.Max(0, Math.Min(newY, _form.Form.ClientSize.Height - _form.PanelNavigation.Height));

                _form.PanelNavigation.Location = new Point(newX, newY);
                _navPanelStartPoint = currentScreenPoint;
            }
        }

        public void PanelNavigation_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isNavPanelDragging)
            {
                _isNavPanelDragging = false;
                _form.PanelNavigation.Cursor = Cursors.Default;
                _form.PanelNavigation.Capture = false;
            }
        }

        private void PushStrokeToUndoStack(AnnotationStroke stroke)
        {
            if (_strokeUndoStack.Count >= MaxUndoStackSize)
            {
                var tempStack = new Stack<AnnotationStroke>();
                for (int i = 0; i < MaxUndoStackSize - 1; i++)
                {
                    tempStack.Push(_strokeUndoStack.Pop());
                }
                _strokeUndoStack.Clear();
                while (tempStack.Count > 0)
                {
                    _strokeUndoStack.Push(tempStack.Pop());
                }
            }
            _strokeUndoStack.Push(stroke);
            UndoActionRecorded?.Invoke(this, EventArgs.Empty);
        }

        public bool CanUndoStroke()
        {
            return _strokeUndoStack.Count > 0;
        }

        public void UndoStroke()
        {
            if (_strokeUndoStack.Count == 0)
                return;

            var lastStroke = _strokeUndoStack.Pop();

            try
            {
                if (_form.Presenter != null)
                {
                    _form.Presenter.UndoAnnotationStroke();
                }

                CleanupAnnotationBitmap();
                LoadAnnotationsForCurrentPage();
                _form.PictureBoxPdf.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error undoing stroke");
            }
        }

        private void ApplyMosaic(RectangleF rect, int blockSize)
        {
            ApplyMosaic(rect, blockSize, _annotationGraphics!, _annotationBitmap!);
        }

        private void ApplyMosaic(RectangleF rect, int blockSize, Graphics g, Bitmap bmp)
        {
            if (bmp == null || g == null) return;
            if (rect.Width <= 0 || rect.Height <= 0) return;

            try
            {
                int x = (int)Math.Max(0, rect.X);
                int y = (int)Math.Max(0, rect.Y);
                int w = (int)Math.Min(bmp.Width - x, rect.Width);
                int h = (int)Math.Min(bmp.Height - y, rect.Height);

                if (w <= 0 || h <= 0) return;

                using var mosaicPen = new SolidBrush(Color.FromArgb(128, 128, 128));
                for (int blockY = y; blockY < y + h; blockY += blockSize)
                {
                    for (int blockX = x; blockX < x + w; blockX += blockSize)
                    {
                        int bw = Math.Min(blockSize, x + w - blockX);
                        int bh = Math.Min(blockSize, y + h - blockY);
                        g.FillRectangle(mosaicPen, blockX, blockY, bw, bh);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying mosaic");
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
                try
                {
                    if (_longPressTimer != null)
                    {
                        _longPressTimer.Stop();
                        _longPressTimer.Tick -= LongPressTimer_Tick;
                        _longPressTimer.Dispose();
                        _longPressTimer = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing long press timer");
                }

                CleanupAnnotationBitmap();

                try
                {
                    _drawingPen?.Dispose();
                    _drawingPen = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing drawing pen");
                }
            }

            _disposed = true;
        }
    }
}
