using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LearningAssistant.Managers
{
    public enum AnnotationToolMode
    {
        Highlight,
        Pen,
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
        private List<PointF>? _currentStrokePoints;
        private Pen? _drawingPen;
        private Color _penColor = Color.Red;
        private float _penWidth = 3f;

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
                        _form.Form.BeginInvoke(() => _form.DisplayImage(bmp));
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
                                _form.Form.BeginInvoke(() => _form.DisplayImage(bmp));
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
                        _form.Form.BeginInvoke(() => _form.DisplayImage(bmp));
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
            _form.PageTransitionOverlay.Visible = true;
            _form.PageTransitionOverlay.BackColor = Color.White;

            if (_form.PageTransitionTimer != null)
            {
                _form.PageTransitionTimer.Start();
            }
        }

        public void PageTransitionTimer_Tick()
        {
            if (_form.PageTransitionOverlay == null || !_isAnimating) return;

            _transitionStep++;

            if (_transitionFadeOut)
            {
                int alpha = 255 - (_transitionStep * 25);
                if (alpha <= 0)
                {
                    alpha = 0;
                    _transitionFadeOut = false;
                    _transitionStep = 0;
                }
                _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, 255, 255, 255);
            }
            else
            {
                int alpha = _transitionStep * 25;
                if (alpha >= 255)
                {
                    alpha = 255;
                    _form.PageTransitionTimer?.Stop();
                    _isAnimating = false;
                    _form.PageTransitionOverlay.Visible = false;
                    return;
                }
                _form.PageTransitionOverlay.BackColor = Color.FromArgb(alpha, 255, 255, 255);
            }
        }

        public void MouseDown(object? sender, MouseEventArgs e)
        {
            try
            {
                StopLongPressTimer();

                if (e.Button == MouseButtons.Left)
                {
                    switch (_currentToolMode)
                    {
                        case AnnotationToolMode.Highlight:
                            _logger.LogInformation("MouseDown Left: Starting highlight selection at {X},{Y}", e.Location.X, e.Location.Y);
                            _isSelecting = true;
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Pen:
                            _logger.LogInformation("MouseDown Left: Starting pen drawing at {X},{Y}", e.Location.X, e.Location.Y);
                            _isDrawing = true;
                            EnsureAnnotationBitmap();
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            var imgPt = ClientToImage(e.Location);
                            _currentStrokePoints = new List<PointF>() { imgPt };
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

                    if (_currentToolMode == AnnotationToolMode.Pen || _isDrawing)
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
                            _annotationGraphics!.SmoothingMode = SmoothingMode.AntiAlias;
                            _annotationGraphics.DrawLines(_drawingPen, _currentStrokePoints.ToArray());
                            _form.Presenter?.SaveAnnotationForCurrentPage((Bitmap)_annotationBitmap.Clone());
                            var imgW = _annotationBitmap.Width;
                            var imgH = _annotationBitmap.Height;
                            var pts = new List<float>();
                            foreach (var pt in _currentStrokePoints)
                            {
                                pts.Add(pt.X / imgW);
                                pts.Add(pt.Y / imgH);
                            }
                            _form.Presenter?.AddAnnotationStroke(pts.ToArray(), _drawingPen.Color.ToArgb(), _drawingPen.Width, imgW, imgH);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving annotation");
                    }
                    finally { _currentStrokePoints = null; }
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
                if (_form.CurrentPageImage == null)
                    return;

                int imgWidth, imgHeight;
                try
                {
                    imgWidth = _form.CurrentPageImage.Width;
                    imgHeight = _form.CurrentPageImage.Height;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogWarning("Image was disposed, cannot create annotation bitmap");
                    return;
                }

                if (_annotationBitmap != null)
                {
                    try
                    {
                        if (_annotationBitmap.Width != imgWidth ||
                            _annotationBitmap.Height != imgHeight)
                        {
                            CleanupAnnotationBitmap();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        CleanupAnnotationBitmap();
                    }
                }

                if (_annotationBitmap == null)
                {
                    _annotationBitmap = new Bitmap(imgWidth, imgHeight);
                    _annotationGraphics = Graphics.FromImage(_annotationBitmap);
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

                var annotationBitmap = _form.Presenter.LoadAnnotationForCurrentPage(imgWidth, imgHeight);
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
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error loading annotations for current page");
            }
        }

        public void DrawAnnotations(Graphics g, Rectangle imgRect)
        {
            try
            {
                if (_annotationBitmap != null)
                {
                    g.DrawImage(_annotationBitmap, imgRect);
                }

                if (_isDrawing && _currentStrokePoints != null && _currentStrokePoints.Count >= 2)
                {
                    var scaleX = (float)imgRect.Width / _form.CurrentPageImage.Width;
                    var scaleY = (float)imgRect.Height / _form.CurrentPageImage.Height;

                    var screenPoints = new List<Point>();
                    foreach (var pt in _currentStrokePoints)
                    {
                        screenPoints.Add(new Point(
                            (int)(pt.X * scaleX + imgRect.X),
                            (int)(pt.Y * scaleY + imgRect.Y)));
                    }

                    using var pen = new Pen(_penColor, _penWidth);
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    pen.LineJoin = LineJoin.Round;
                    g.DrawLines(pen, screenPoints.ToArray());
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
                if (_form.CurrentPageImage == null)
                    return new PointF(clientPt.X, clientPt.Y);

                int imgWidth, imgHeight;
                try
                {
                    imgWidth = _form.CurrentPageImage.Width;
                    imgHeight = _form.CurrentPageImage.Height;
                }
                catch (ObjectDisposedException)
                {
                    return new PointF(clientPt.X, clientPt.Y);
                }

                var scaleX = (float)imgWidth / _form.PictureBoxPdf.ClientSize.Width;
                var scaleY = (float)imgHeight / _form.PictureBoxPdf.ClientSize.Height;
                return new PointF(clientPt.X * scaleX, clientPt.Y * scaleY);
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
