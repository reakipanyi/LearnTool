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
        Text,
        Eraser,
        LaserPointer,
        Checklist,
        ImageEmbed,
        Spotlight
    }

    public enum SelectionInteractionState
    {
        None,
        Idle,
        Moving,
        ResizingTopLeft,
        ResizingTopRight,
        ResizingBottomLeft,
        ResizingBottomRight
    }

    public class PdfReaderNavigationManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private bool _disposed = false;

        private bool _isSelecting = false;
        private bool _isDragging = false;

        private AnnotationToolMode _currentToolMode = AnnotationToolMode.Highlight;

        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private Point _dragStart = Point.Empty;
        private Rectangle? _lastSelectionRect = null;
        private Rectangle? _pendingHighlightRect = null;

        private bool _isOcrPanelDragging = false;
        private Point _ocrPanelStartPoint = Point.Empty;
        private bool _isDoubleClickPending = false;
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTime_ms = 400;
        private const int DoubleClickDistance = 5;

        private System.Windows.Forms.Timer? _longPressTimer;
        private bool _isLongPressPending = false;
        private Point _longPressStartLocation = Point.Empty;
        private const int LongPressTime_ms = 300;
        private bool _longPressDragStarted = false;

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;

        private readonly PageTransitionAnimator _pageTransitionAnimator;
        private readonly PdfZoomController _zoomController;
        private readonly AnnotationLayerManager _annotationLayerManager;
        private readonly AnnotationSelectionManager _selectionManager;
        private readonly AnnotationToolHandler _toolHandler;

        private bool _annotationLayerVisible = true;
        private bool _spotlightActive = false;
        private Point _spotlightPosition = Point.Empty;
        private bool _favoriteToolbarActive = false;

        private readonly Stack<AnnotationStroke> _strokeUndoStack = new Stack<AnnotationStroke>();
        private const int MaxUndoStackSize = 50;

        /// <summary>
        /// 当一个画笔/标注撤销动作被压入内部撤销栈时触发。
        /// 订阅方（如主窗体）可据此把该操作类型记录到统一撤销栈中，
        /// 以便工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
        /// </summary>
        public event EventHandler? UndoActionRecorded;

        // 激光笔模式
        private bool _isLaserPointerActive = false;
        private Point _laserPointerPosition = Point.Empty;

        private System.Windows.Forms.Timer? _clearPendingHighlightTimer;

        /// <summary>
        /// 标注被选中时触发，携带(selectedStroke, selectedIndex, isDoubleClick)
        /// </summary>
        public event Action<AnnotationStroke, int, bool>? AnnotationSelected;

        public int ZoomLevel => _zoomController.ZoomLevel;
        public bool IsLocked => _zoomController.IsLocked;
        public Rectangle? LastSelectionRect => _lastSelectionRect ?? _pendingHighlightRect;
        public Point ImageOffset => _zoomController.ImageOffset;
        public AnnotationToolMode CurrentToolMode => _currentToolMode;
        public Color PenColor => _toolHandler.PenColor;
        public float PenWidth => _toolHandler.PenWidth;
        public bool IsDashed => _toolHandler.IsDashed;
        public string PenType => _toolHandler.PenType;
        public bool IsDrawing => _toolHandler.IsDrawing;
        public List<PointF>? CurrentStrokePoints => _toolHandler.CurrentStrokePoints;
        public AnnotationStroke? SelectedStroke => _selectionManager.SelectedStroke;
        public int SelectedStrokeIndex => _selectionManager.SelectedStrokeIndex;
        public AnnotationStroke? HoveredStroke => _selectionManager.HoveredStroke;
        public SelectionInteractionState SelectionState => _selectionManager.SelectionState;
        public bool AnnotationLayerVisible
        {
            get => _annotationLayerVisible;
            set
            {
                _annotationLayerVisible = value;
                _form.PictureBoxPdf.Invalidate();
            }
        }

        public Func<bool>? IsHighlightModeCallback { get; set; }
        public Action<Rectangle>? AddHighlightCallback { get; set; }
        public Action<Point>? AddTextCallback { get; set; }
        public Action<AnnotationText, int>? EditTextCallback { get; set; }
        /// <summary>拖拽高亮时触发，通知外部刷新高亮图层渲染</summary>
        public Action? UpdateHighlightLayerCallback { get; set; }
        /// <summary>获取当前音频录制时间戳(毫秒)，用于标注时同步记录</summary>
        public Func<long?>? GetCurrentAudioTimestampMs { get; set; }

        public PdfReaderNavigationManager(ILogger logger, IPdfReaderFormAccess form)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));

            _pageTransitionAnimator = new PageTransitionAnimator(logger, form);
            _zoomController = new PdfZoomController(logger, form);
            _annotationLayerManager = new AnnotationLayerManager(logger, form);
            _selectionManager = new AnnotationSelectionManager(logger, form);
            _toolHandler = new AnnotationToolHandler(logger, form, _annotationLayerManager);
            InitializeSelectionManagerDelegates();
            InitializeToolHandlerDelegates();
            InitializeLongPressTimer();
        }

        private void InitializeSelectionManagerDelegates()
        {
            _selectionManager.ClientToImage = ClientToImage;
            _selectionManager.GetCurrentDisplayRect = GetCurrentDisplayRect;
            _selectionManager.UpdateStrokeAtCurrentPage = (idx, stroke) => _form.Presenter?.UpdateStrokeAtCurrentPage(idx, stroke);
            _selectionManager.RemoveStrokeAtCurrentPage = (idx) => _form.Presenter?.RemoveStrokeAtCurrentPage(idx);
            _selectionManager.RemoveTextAtCurrentPage = (idx, text) => _form.Presenter?.RemoveTextAtCurrentPage(idx);
            _selectionManager.UpdateTextAtCurrentPage = (idx, text) => _form.Presenter?.UpdateTextAtCurrentPage(idx, text);
            _selectionManager.ReloadAnnotations = () => LoadAnnotationsForCurrentPage();
            _selectionManager.InvalidateView = () => { try { _form.PictureBoxPdf?.Invalidate(); } catch { } };
            _selectionManager.PushStrokeToUndoStack = PushStrokeToUndoStack;
            _selectionManager.EditTextCallback = (text, idx) => EditTextCallback?.Invoke(text, idx);
            _selectionManager.UpdateHighlightCallback = (highlight) => _form.HighlightService?.UpdateHighlight(_form.CurrentPdfPath, highlight);
        }

        private void InitializeToolHandlerDelegates()
        {
            _toolHandler.PushStrokeToUndoStack = PushStrokeToUndoStack;
            _toolHandler.GetCurrentAudioTimestampMs = () => GetCurrentAudioTimestampMs?.Invoke();
            _toolHandler.SaveAnnotationForPage = (pageIndex) => _form.Presenter?.SaveAnnotationForPage(pageIndex);
            _toolHandler.AddAnnotationStroke = (pts, color, thickness, imgW, imgH, shapeType, pageIdx, strokeStyle) =>
                _form.Presenter?.AddAnnotationStroke(pts, color, thickness, imgW, imgH, shapeType, pageIdx, strokeStyle);
            _toolHandler.ClientToImage = ClientToImage;
        }

        public void SetToolMode(AnnotationToolMode mode)
        {
            _currentToolMode = mode;
            _toolHandler.IsDrawing = false;
            _isSelecting = false;
            _toolHandler.CurrentStrokePoints = null;
            _isLaserPointerActive = (mode == AnnotationToolMode.LaserPointer);
            _spotlightActive = (mode == AnnotationToolMode.Spotlight);
            _selectionManager.ClearSelection();

            if (_form.PictureBoxPdf != null)
            {
                _form.PictureBoxPdf.Cursor = mode == AnnotationToolMode.Select ? Cursors.SizeAll : Cursors.Default;
            }

            _form.PictureBoxPdf.Invalidate();
        }

        public void SetPenColor(Color color)
        {
            _toolHandler.SetPenColor(color);
        }

        public void SetPenWidth(float width)
        {
            _toolHandler.SetPenWidth(width);
        }

        public void SetDashStyle(bool dashed)
        {
            _toolHandler.SetDashStyle(dashed);
        }

        public void SetPenType(string penType)
        {
            _toolHandler.SetPenType(penType);
        }

        public void SetStrokeStyle(string style)
        {
            _toolHandler.SetStrokeStyle(style);
        }

        public void ToggleFavoriteToolbar()
        {
            _favoriteToolbarActive = !_favoriteToolbarActive;
            _form.ShowMessage(_favoriteToolbarActive ? "已启用收藏工具栏布局" : "已恢复默认工具栏布局", "收藏工具栏");
        }

        public bool IsFavoriteToolbarActive => _favoriteToolbarActive;

        private void InitializeLongPressTimer()
        {
            _longPressTimer = new System.Windows.Forms.Timer();
            _longPressTimer.Interval = LongPressTime_ms;
            _longPressTimer.Tick += LongPressTimer_Tick;
        }

        public void Zoom(int value)
        {
            _zoomController.Zoom(value);
        }

        public void ZoomByMouseWheel(int delta, bool ctrlDown)
        {
            try
            {
                if (delta == 0) return;

                if (ctrlDown)
                {
                    _zoomController.ZoomByMouseWheel(delta);
                }
                else
                {
                    if (_form.Presenter != null)
                    {
                        if (delta < 0) _form.Presenter.NextPage();
                        else _form.Presenter.PreviousPage();
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
            _zoomController.ResetZoom();
        }

        public void ToggleLockView()
        {
            _zoomController.ToggleLockView();
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
            _pageTransitionAnimator.StartPageTransition(forward);
        }

        public void OnPageTransitionTick()
        {
            _pageTransitionAnimator.OnPageTransitionTick();
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

                    // 标注工具模式分发：选择 / 高亮 / 自由绘制 / 形状 / 文字 / 橡皮擦 / 特殊工具
                    switch (_currentToolMode)
                    {
                        case AnnotationToolMode.Select:
                            _logger.LogInformation("MouseDown Left: Select mode, checking for stroke hit at {X},{Y}", e.Location.X, e.Location.Y);
                            _selectionManager.HandleSelectModeClick(e.Location);
                            return;
                        case AnnotationToolMode.Highlight:
                            _logger.LogInformation("MouseDown Left: Starting highlight selection at {X},{Y}", e.Location.X, e.Location.Y);
                            _toolHandler.BeginShape(e.Location, _form.GetPageAtPoint(e.Location).pageIndex);
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Pen:
                            _logger.LogInformation("MouseDown Left: Starting pen drawing at {X},{Y}", e.Location.X, e.Location.Y);
                            EnsureAnnotationBitmap();
                            _toolHandler.BeginStroke(e.Location, _form.GetPageAtPoint(e.Location).pageIndex);
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Strikethrough:
                            _logger.LogInformation("MouseDown Left: Starting strikethrough drawing at {X},{Y}", e.Location.X, e.Location.Y);
                            EnsureAnnotationBitmap();
                            _toolHandler.BeginStroke(e.Location, _form.GetPageAtPoint(e.Location).pageIndex);
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Rectangle:
                        case AnnotationToolMode.Ellipse:
                        case AnnotationToolMode.Arrow:
                        case AnnotationToolMode.Mosaic:
                        case AnnotationToolMode.Checklist:
                            _logger.LogInformation("MouseDown Left: Starting shape drawing ({Mode}) at {X},{Y}", _currentToolMode, e.Location.X, e.Location.Y);
                            _toolHandler.BeginShape(e.Location, _form.GetPageAtPoint(e.Location).pageIndex);
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Text:
                            _logger.LogInformation("MouseDown Left: Text tool clicked at {X},{Y}", e.Location.X, e.Location.Y);
                            AddTextCallback?.Invoke(e.Location);
                            break;
                        case AnnotationToolMode.Eraser:
                            _logger.LogInformation("MouseDown Left: Eraser at {X},{Y}", e.Location.X, e.Location.Y);
                            _selectionManager.IsEraserDragging = true;
                            _selectionManager.EraserDragStart = e.Location;
                            _selectionManager.EraserDragEnd = e.Location;
                            _selectionManager.HoveredStroke = null;
                            _selectionManager.HoveredStrokeIndex = -1;
                            _form.PictureBoxPdf.Cursor = Cursors.Cross;
                            return;
                        case AnnotationToolMode.ImageEmbed:
                            _logger.LogInformation("MouseDown Left: Image embed at {X},{Y}", e.Location.X, e.Location.Y);
                            _toolHandler.BeginShape(e.Location, _form.GetPageAtPoint(e.Location).pageIndex);
                            _selectStart = e.Location;
                            _selectEnd = e.Location;
                            _form.PictureBoxPdf.Invalidate();
                            break;
                        case AnnotationToolMode.Spotlight:
                            _logger.LogInformation("MouseDown Left: Spotlight at {X},{Y}", e.Location.X, e.Location.Y);
                            _spotlightActive = true;
                            _spotlightPosition = e.Location;
                            _form.PictureBoxPdf.Invalidate();
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
                        _toolHandler.IsDrawing = false;
                        _lastClickTime = DateTime.MinValue;
                        _lastClickLocation = Point.Empty;
                        return;
                    }

                    _isDoubleClickPending = false;
                    _lastClickTime = now;
                    _lastClickLocation = e.Location;

                    if (_currentToolMode == AnnotationToolMode.Pen || _currentToolMode == AnnotationToolMode.Strikethrough || _toolHandler.IsDrawing)
                    {
                        _logger.LogInformation("MouseDown Right: Drawing mode, starting annotation");
                        _toolHandler.IsDrawing = true;
                        EnsureAnnotationBitmap();
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        var imgPt = ClientToImage(e.Location);
                        _toolHandler.CurrentStrokePoints = new List<PointF>() { imgPt };
                        _form.PictureBoxPdf.Invalidate();
                        return;
                    }

                    if (_currentToolMode == AnnotationToolMode.Rectangle ||
                        _currentToolMode == AnnotationToolMode.Ellipse ||
                        _currentToolMode == AnnotationToolMode.Arrow ||
                        _currentToolMode == AnnotationToolMode.Mosaic)
                    {
                        _toolHandler.IsDrawingShape = true;
                        EnsureAnnotationBitmap();
                        _toolHandler.ShapeStartPoint = ClientToImage(e.Location);
                        _toolHandler.ShapeEndPoint = _toolHandler.ShapeStartPoint;
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
                _toolHandler.IsDrawing = false;
                _toolHandler.IsDrawingShape = false;
                StopLongPressTimer();
            }
        }

        public void MouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                if (_zoomController.IsLocked) return;

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

                if (_toolHandler.IsDrawing)
                {
                    _selectEnd = e.Location;
                    var imgPt = ClientToImage(e.Location);
                    _toolHandler.AddStrokePoint(imgPt);
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_toolHandler.IsDrawingShape)
                {
                    _selectEnd = e.Location;
                    var endPt = ClientToImage(e.Location);

                    _toolHandler.UpdateShapeEnd(endPt, _toolHandler.ShapeStartPoint, _currentToolMode, (Control.ModifierKeys & Keys.Shift) == Keys.Shift);
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
                    _zoomController.SetImageOffset(new Point(_zoomController.ImageOffset.X + deltaX, _zoomController.ImageOffset.Y + deltaY));
                    _dragStart = e.Location;
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                // 选中标注的拖拽移动/缩放（含文字注解）
                if (_currentToolMode == AnnotationToolMode.Select && (_selectionManager.SelectedStroke != null || _selectionManager.SelectedHighlight != null || _selectionManager.SelectedText != null))
                {
                    if (_selectionManager.SelectionState == SelectionInteractionState.Moving)
                    {
                        _selectionManager.HandleSelectionDragMove(e.Location);
                        _form.PictureBoxPdf.Invalidate();
                        return;
                    }
                    else if (_selectionManager.SelectionState == SelectionInteractionState.ResizingTopLeft ||
                             _selectionManager.SelectionState == SelectionInteractionState.ResizingTopRight ||
                             _selectionManager.SelectionState == SelectionInteractionState.ResizingBottomLeft ||
                             _selectionManager.SelectionState == SelectionInteractionState.ResizingBottomRight)
                    {
                        if (_selectionManager.SelectedHighlight != null)
                        {
                            _selectionManager.HandleHighlightResize(e.Location);
                        }
                        else
                        {
                            _selectionManager.HandleSelectionResize(e.Location);
                        }
                        _form.PictureBoxPdf.Invalidate();
                        return;
                    }

                    // 检测鼠标悬停在手柄上，改变光标
                    _selectionManager.UpdateCursorForHandleHit(e.Location);
                }

                // 橡皮擦模式：区域拖拽或悬停检测
                if (_currentToolMode == AnnotationToolMode.Eraser)
                {
                    if (_selectionManager.IsEraserDragging)
                    {
                        _selectionManager.EraserDragEnd = e.Location;
                        _form.PictureBoxPdf.Invalidate();
                    }
                    else
                    {
                        _selectionManager.HandleEraserHover(e.Location);
                    }
                }

                // 激光笔模式：更新鼠标位置
                if (_currentToolMode == AnnotationToolMode.LaserPointer && _isLaserPointerActive)
                {
                    _laserPointerPosition = e.Location;
                    _form.PictureBoxPdf.Invalidate();
                }

                // 聚光灯模式：更新鼠标位置
                if (_currentToolMode == AnnotationToolMode.Spotlight && _spotlightActive)
                {
                    _spotlightPosition = e.Location;
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
                    _toolHandler.IsDrawing = false;
                    return;
                }

                if (_toolHandler.IsDrawing)
                {
                    _toolHandler.FinalizeStroke(_currentToolMode);
                    _form.PictureBoxPdf.Invalidate();
                    return;
                }

                if (_toolHandler.IsDrawingShape)
                {
                    _toolHandler.IsDrawingShape = false;
                    try
                    {
                        // 高亮模式需要外部回调（屏幕坐标转换 + 调用 AddHighlightCallback）
                        if (_currentToolMode == AnnotationToolMode.Highlight && _toolHandler.ShapeStartPoint.HasValue && _toolHandler.ShapeEndPoint.HasValue)
                        {
                            var startPt = _toolHandler.ShapeStartPoint.Value;
                            var endPt = _toolHandler.ShapeEndPoint.Value;
                            var rect = new RectangleF(
                                Math.Min(startPt.X, endPt.X),
                                Math.Min(startPt.Y, endPt.Y),
                                Math.Abs(endPt.X - startPt.X),
                                Math.Abs(endPt.Y - startPt.Y));

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
                            _toolHandler.FinalizeShape(_currentToolMode);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving shape annotation");
                    }
                    finally
                    {
                        _toolHandler.ResetDrawingState();
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

                // 结束选中标注的拖拽操作
                if (_currentToolMode == AnnotationToolMode.Select &&
                    (_selectionManager.SelectionState == SelectionInteractionState.Moving ||
                     _selectionManager.SelectionState == SelectionInteractionState.ResizingTopLeft ||
                     _selectionManager.SelectionState == SelectionInteractionState.ResizingTopRight ||
                     _selectionManager.SelectionState == SelectionInteractionState.ResizingBottomLeft ||
                     _selectionManager.SelectionState == SelectionInteractionState.ResizingBottomRight))
                {
                    _selectionManager.SelectionState = SelectionInteractionState.Idle;
                    _form.PictureBoxPdf.Cursor = Cursors.SizeAll;

                    if (_selectionManager.SelectedHighlight != null)
                    {
                        // 持久化高亮位置变化
                        var highlightService = _form.HighlightService;
                        if (highlightService != null && !string.IsNullOrEmpty(_form.CurrentPdfPath))
                        {
                            highlightService.UpdateHighlight(_form.CurrentPdfPath, _selectionManager.SelectedHighlight);
                        }
                    }
                    else
                    {
                        _form.Presenter?.SaveAnnotationForCurrentPage();
                    }

                    // 重新加载标注位图，使调整后的位置立即生效
                    LoadAnnotationsForCurrentPage();
                    _form.PictureBoxPdf.Invalidate();
                }

                // 橡皮擦模式：区域擦除完成
                if (_selectionManager.IsEraserDragging)
                {
                    _selectionManager.IsEraserDragging = false;
                    _form.PictureBoxPdf.Cursor = Cursors.Default;

                    var dragRect = AnnotationSelectionManager.GetNormalizedDragRect(_selectionManager.EraserDragStart, _selectionManager.EraserDragEnd);
                    bool isClick = dragRect.Width < 5 && dragRect.Height < 5;

                    if (isClick)
                    {
                        // 点击行为：删除单条笔划（原行为）
                        _selectionManager.HandleEraserModeClick(_selectionManager.EraserDragStart);
                    }
                    else
                    {
                        // 区域擦除：删除区域内所有笔划
                        var imgPoint1 = ClientToImage(_selectionManager.EraserDragStart);
                        var imgPoint2 = ClientToImage(_selectionManager.EraserDragEnd);
                        var eraserRect = new RectangleF(
                            Math.Min(imgPoint1.X, imgPoint2.X),
                            Math.Min(imgPoint1.Y, imgPoint2.Y),
                            Math.Abs(imgPoint2.X - imgPoint1.X),
                            Math.Abs(imgPoint2.Y - imgPoint1.Y));

                        _logger.LogInformation("Eraser area: {Rect}", eraserRect);

                        var strokes = _selectionManager.GetCurrentPageStrokes();
                        int deletedCount = 0;

                        for (int i = strokes.Count - 1; i >= 0; i--)
                        {
                            if (_selectionManager.StrokeIntersectsRect(strokes[i], eraserRect))
                            {
                                PushStrokeToUndoStack(strokes[i]);
                                _form.Presenter.RemoveStrokeAtCurrentPage(i);
                                deletedCount++;
                            }
                        }

                        _logger.LogInformation("Eraser area deleted {Count} strokes", deletedCount);

                        LoadAnnotationsForCurrentPage();
                        _form.PictureBoxPdf.Invalidate();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MouseUp");
                _isSelecting = false;
                _toolHandler.IsDrawing = false;
                _toolHandler.IsDrawingShape = false;
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

        private void StartClearPendingHighlightTimer()
        {
            _clearPendingHighlightTimer?.Stop();
            _clearPendingHighlightTimer?.Dispose();

            _clearPendingHighlightTimer = new System.Windows.Forms.Timer();
            _clearPendingHighlightTimer.Interval = 100;
            _clearPendingHighlightTimer.Tick += ClearPendingHighlight_Tick;
            _clearPendingHighlightTimer.Start();
        }

        private void ClearPendingHighlight_Tick(object? sender, EventArgs e)
        {
            _clearPendingHighlightTimer?.Stop();
            _pendingHighlightRect = null;
            _form.PictureBoxPdf?.Invalidate();
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
            if (_zoomController.IsLocked) return;

            _isDragging = true;
            _longPressDragStarted = true;
            _dragStart = startLocation;
            _form.PictureBoxPdf.Cursor = Cursors.Hand;
        }

        private void EnsureAnnotationBitmap()
        {
            try
            {
                bool isSecondPage = _toolHandler.DrawingPageIndex > _form.CurrentPageIndex;
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

                if (isSecondPage)
                {
                    var bmp = _annotationLayerManager.SecondAnnotationBitmap;
                    var gfx = _annotationLayerManager.SecondAnnotationGraphics;
                    if (bmp != null)
                    {
                        if (bmp.Width != imgWidth || bmp.Height != imgHeight)
                            _annotationLayerManager.CleanupSecondAnnotationBitmap();
                    }
                    if (_annotationLayerManager.SecondAnnotationBitmap == null)
                        _annotationLayerManager.EnsureSecondAnnotationBitmap(imgWidth, imgHeight);
                }
                else
                {
                    _annotationLayerManager.EnsureAnnotationBitmap();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnsureAnnotationBitmap");
                _annotationLayerManager.CleanupAnnotationBitmap();
            }
        }

        public void CleanupAnnotationBitmap()
        {
            _annotationLayerManager.CleanupAnnotationBitmap();
        }

        /// <summary>仅清理第二页标注缓存（双页模式切换时使用）</summary>
        public void CleanupSecondAnnotationBitmap()
        {
            _annotationLayerManager.CleanupSecondAnnotationBitmap();
        }

        /// <summary>
        /// 清除当前 PDF 的所有笔划标注（包括内存缓存和持久化存储），用于清除旧版坐标污染数据
        /// </summary>
        public void ClearAllStrokes()
        {
            try
            {
                if (_form.Presenter == null || string.IsNullOrEmpty(_form.CurrentPdfPath))
                {
                    _logger.LogWarning("ClearAllStrokes: Presenter or PdfPath is null");
                    return;
                }

                _logger.LogInformation("清除所有笔划标注: {PdfPath}", _form.CurrentPdfPath);

                _form.Presenter.ClearAllAnnotations(_form.CurrentPdfPath);
                _selectionManager.ClearSelection();
                _annotationLayerManager.CleanupAnnotationBitmap();
                _toolHandler.CurrentStrokePoints?.Clear();
                _selectionManager.HoveredStroke = null;
                _selectionManager.HoveredStrokeIndex = -1;
                _annotationLayerManager.LoadAnnotationsForCurrentPage();
                _form.PictureBoxPdf?.Invalidate();

                _logger.LogInformation("所有笔划标注已清除");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清除所有笔划标注时出错");
            }
        }

        public void LoadAnnotationsForCurrentPage()
        {
            _annotationLayerManager.LoadAnnotationsForCurrentPage();
            _selectionManager.ClearSelection();
        }

        /// <summary>
        /// 用已加载好的标注位图直接设置当前标注图层，避免再次读磁盘。
        /// 当 annotationBitmap 为 null 时创建空的透明图层。
        /// 注意：仅刷新标注渲染缓存，不清除当前选中——该方法由异步标注加载回调调用，
        /// 同一页的异步加载发生在用户正在交互期间，清除选中会导致 Delete/垃圾桶删除失效。
        /// 翻页清选中由同步入口 SetCurrentPageIndex 负责。
        /// </summary>
        public void ApplyLoadedAnnotationBitmap(Bitmap? annotationBitmap)
        {
            _annotationLayerManager.ApplyLoadedAnnotationBitmap(annotationBitmap);
        }

        /// <summary>应用第二页标注位图（双页模式）</summary>
        public void ApplySecondLoadedAnnotationBitmap(Bitmap? annotationBitmap)
        {
            _annotationLayerManager.ApplySecondLoadedAnnotationBitmap(annotationBitmap);
        }

        public void DeleteSelectedStroke()
        {
            _selectionManager.DeleteSelectedStroke();
            if (_selectionManager.SelectedStrokeIndex < 0)
                LoadAnnotationsForCurrentPage();
            _form.PictureBoxPdf.Invalidate();
        }

        public void DeleteSelectedText()
        {
            _selectionManager.DeleteSelectedText();
            if (_selectionManager.SelectedTextIndex < 0)
                LoadAnnotationsForCurrentPage();
            _form.PictureBoxPdf.Invalidate();
        }

        /// <summary>
        /// 删除当前选中的标注（笔画或文本）。返回是否实际删除了某个标注。
        /// 供 Delete 键与垃圾桶按钮统一调用：无任何选中时不执行删除并返回 false。
        /// </summary>
        public bool DeleteSelectedAnnotation()
        {
            if (_selectionManager.SelectedStrokeIndex >= 0)
            {
                DeleteSelectedStroke();
                return true;
            }
            if (_selectionManager.SelectedTextIndex >= 0)
            {
                DeleteSelectedText();
                return true;
            }
            return false;
        }

        public void ClearSelection()
        {
            _selectionManager.ClearSelection();
            _form.PictureBoxPdf.Invalidate();
        }

        public void UpdateSelectedStrokeColor(Color color)
        {
            _selectionManager.UpdateSelectedStrokeColor(color);
        }

        public void UpdateSelectedStrokeThickness(float thickness)
        {
            _selectionManager.UpdateSelectedStrokeThickness(thickness);
        }

        public void DrawAnnotations(Graphics g, Rectangle imgRect, int pageIndex)
        {
            if (!_annotationLayerVisible) return;
            try
            {
                // 双页模式下，_annotationLayerManager.AnnotationBitmap 属于左页，_annotationLayerManager.SecondAnnotationBitmap 属于右页
                if (pageIndex < 0 || pageIndex == _form.CurrentPageIndex)
                {
                    if (_annotationLayerManager.AnnotationBitmap != null)
                    {
                        g.DrawImage(_annotationLayerManager.AnnotationBitmap, imgRect);
                    }
                }
                else if (pageIndex == _form.CurrentPageIndex + 1)
                {
                    if (_annotationLayerManager.SecondAnnotationBitmap != null)
                    {
                        g.DrawImage(_annotationLayerManager.SecondAnnotationBitmap, imgRect);
                    }
                }

                // 绘制自由笔划的实时预览
                if (_toolHandler.IsDrawing)
                {
                    Bitmap? srcImage = _form.CurrentPageImage;
                    if (pageIndex > _form.CurrentPageIndex && _form.SecondPageImage != null)
                        srcImage = _form.SecondPageImage;
                    _toolHandler.DrawStrokePreview(g, imgRect, pageIndex, srcImage, _currentToolMode);
                }

                // 绘制形状的实时预览
                if (_toolHandler.IsDrawingShape)
                {
                    Bitmap? srcImage = _form.CurrentPageImage;
                    if (pageIndex > _form.CurrentPageIndex && _form.SecondPageImage != null)
                        srcImage = _form.SecondPageImage;
                    _toolHandler.DrawShapePreview(g, imgRect, pageIndex, srcImage, _currentToolMode);
                }

                // 绘制选中标注的视觉反馈（选中框 + 手柄）
                _selectionManager.DrawSelectionVisual(g, imgRect, pageIndex);

                // 橡皮擦模式：绘制悬停笔划的红色高亮效果
                if (_currentToolMode == AnnotationToolMode.Eraser && _selectionManager.HoveredStroke != null && pageIndex >= 0 && pageIndex == _form.CurrentPageIndex)
                {
                    _selectionManager.DrawEraserHoverVisual(g, imgRect, pageIndex);
                }

                // 橡皮擦模式：绘制区域擦除选择矩形
                if (_currentToolMode == AnnotationToolMode.Eraser && _selectionManager.IsEraserDragging && pageIndex >= 0 && pageIndex == _form.CurrentPageIndex)
                {
                    var dragRect = AnnotationSelectionManager.GetNormalizedDragRect(_selectionManager.EraserDragStart, _selectionManager.EraserDragEnd);
                    if (dragRect.Width > 2 && dragRect.Height > 2)
                    {
                        using var fillBrush = new SolidBrush(Color.FromArgb(30, 255, 0, 0));
                        g.FillRectangle(fillBrush, dragRect);
                        using var borderPen = new Pen(Color.FromArgb(180, 255, 0, 0), 2f);
                        borderPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                        g.DrawRectangle(borderPen, dragRect);
                    }
                }

                // 激光笔模式：绘制红点 + 射线
                if (_currentToolMode == AnnotationToolMode.LaserPointer && _isLaserPointerActive && pageIndex >= 0 && pageIndex == _form.CurrentPageIndex)
                {
                    var pt = _laserPointerPosition;
                    // 射线：从红点向外延伸的淡红色线条
                    using var rayPen = new Pen(Color.FromArgb(60, 255, 50, 50), 1f);
                    rayPen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
                    g.DrawLine(rayPen, pt.X - 30, pt.Y, pt.X + 30, pt.Y);
                    g.DrawLine(rayPen, pt.X, pt.Y - 30, pt.X, pt.Y + 30);
                    // 外圈光晕
                    using var glowPen = new Pen(Color.FromArgb(100, 255, 50, 50), 2f);
                    g.DrawEllipse(glowPen, pt.X - 10, pt.Y - 10, 20, 20);
                    // 红点
                    using var dotBrush = new SolidBrush(Color.FromArgb(220, 255, 0, 0));
                    g.FillEllipse(dotBrush, pt.X - 4, pt.Y - 4, 8, 8);
                }

                // 聚光灯模式：鼠标周围高亮，其余区域变暗（实际在 DrawSpotlightOverlay 中绘制）
                // 这里保留空块以兼容双页模式下的绘制顺序
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error drawing annotations");
            }
        }

        /// <summary>
        /// 将当前页的标注图层渲染到指定的 Graphics 对象上（用于导出为图片）。
        /// </summary>
        public void DrawAnnotationsToGraphics(Graphics g, Rectangle destRect)
        {
            if (_annotationLayerManager.AnnotationBitmap != null)
            {
                g.DrawImage(_annotationLayerManager.AnnotationBitmap, destRect);
            }
        }

        /// <summary>
        /// 绘制聚光灯效果覆盖层（支持双页模式，覆盖整个视图区域）
        /// </summary>
        public void DrawSpotlightOverlay(Graphics g, Rectangle totalRect)
        {
            if (_currentToolMode != AnnotationToolMode.Spotlight || !_spotlightActive)
                return;

            var pt = _spotlightPosition;
            if (!totalRect.Contains(pt) && !totalRect.Contains(new Point(pt.X + 1, pt.Y + 1)))
                return;

            // 用半透明黑色覆盖整个视图区域
            using var dimBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0));
            var spotlightRadius = 120;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddRectangle(totalRect);
            path.AddEllipse(pt.X - spotlightRadius, pt.Y - spotlightRadius, spotlightRadius * 2, spotlightRadius * 2);
            g.FillPath(dimBrush, path);

            // 外圈高亮
            using var glowPen2 = new Pen(Color.FromArgb(120, 255, 255, 200), 3f);
            g.DrawEllipse(glowPen2, pt.X - spotlightRadius, pt.Y - spotlightRadius, spotlightRadius * 2, spotlightRadius * 2);

            // 内圈光晕
            using var innerGlow = new Pen(Color.FromArgb(60, 255, 255, 200), 1f);
            g.DrawEllipse(innerGlow, pt.X - spotlightRadius + 5, pt.Y - spotlightRadius + 5, (spotlightRadius - 5) * 2, (spotlightRadius - 5) * 2);
        }

        /// <summary>
        /// 获取当前鼠标位置对应的显示区域矩形（支持双页模式）
        /// </summary>
        private Rectangle GetCurrentDisplayRect(Point clientPoint)
        {
            if (_form.IsDualPage && _form.CurrentPageImage != null)
            {
                var (_, pageRect, _) = _form.GetPageAtPoint(clientPoint);
                if (pageRect.Width > 0 && pageRect.Height > 0)
                    return pageRect;
            }
            return _form.GetImageDisplayRect();
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
                var items = _strokeUndoStack.ToArray();
                _strokeUndoStack.Clear();
                for (int i = items.Length - 2; i >= 0; i--)
                    _strokeUndoStack.Push(items[i]);
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
                    _toolHandler?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing tool handler");
                }
            }

            _disposed = true;
        }
    }
}
