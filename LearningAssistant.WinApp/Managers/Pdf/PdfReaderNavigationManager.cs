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
        TemporaryPen,
        Mosaic,
        Strikethrough,
        Text,
        Eraser,
        LaserPointer,
        Checklist,
        ImageEmbed,
        Spotlight,
        Magnifier
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

        // 拖动中的预览脏区跟踪：只重绘「上一帧预览包围盒 ∪ 当前包围盒」，
        // 避免每次 MouseMove 触发整页位图 + 全部标注层的全控件重绘。
        private Rectangle _previewInvalidRect = Rectangle.Empty;
        private bool _hasPreviewInvalidRect = false;
        private Point _lastPreviewPoint = Point.Empty;
        private bool _hasLastPreviewPoint = false;
        private const int PreviewInvalidMargin = 8;

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
        // 放大镜：跟随鼠标的圆形透镜。直接从当前页位图截取光标附近区域放大绘制，
        // 不再弹独立窗口；移动时只重绘旧/新透镜包围盒，避免整页重绘影响性能。
        private bool _magnifierActive = false;
        private Point _magnifierPosition = Point.Empty;
        private bool _hasMagnifierPosition = false;
        private Point _lastMagnifierPoint = Point.Empty;
        private bool _hasLastMagnifierPoint = false;
        private const int MagnifierRadius = 110;
        private const float MagnifierZoom = 2f;
        private bool _favoriteToolbarActive = false;

        private readonly Stack<AnnotationStroke> _strokeUndoStack = new Stack<AnnotationStroke>();
        // 重做栈：撤销时把被移除的笔划实体留存，重做时按原坐标与样式写回；产生新笔划会清空它
        private readonly Stack<AnnotationStroke> _strokeRedoStack = new Stack<AnnotationStroke>();
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

        // 一次性工具模式：工具使用完后自动恢复到默认模式
        private bool _isOneTimeTool = false;
        private AnnotationToolMode _defaultToolMode = AnnotationToolMode.None;

        // 双击工具按钮锁定的“连续标注”：锁定后画完不自动回到选择，换工具或按 Esc 解除
        private bool _toolSticky = false;

        /// <summary>
        /// 一次标注真正完成并落盘后触发（画笔/形状/高亮各一次）。
        /// 是否自动回到选择模式由宿主按用户偏好决定，Manager 本身不读配置。
        /// </summary>
        public event EventHandler? AnnotationCommitted;

        // 临时笔模式：绘制的笔迹暂时显示后自动消失
        private List<PointF>? _temporaryPenPoints = null;
        private int _temporaryPenPageIndex = -1;
        private Color _temporaryPenColor = Color.Red;
        private System.Windows.Forms.Timer? _temporaryPenClearTimer;

        private System.Windows.Forms.Timer? _clearPendingHighlightTimer;

        /// <summary>
        /// 标注被选中时触发，携带(selectedStroke, selectedIndex, isDoubleClick)
        /// </summary>
        public event Action<AnnotationStroke, int, bool>? AnnotationSelected;

        /// <summary>
        /// 高亮被选中时触发，携带高亮对象，供外部回显已缓存的识别文本与译文
        /// </summary>
        public event Action<PdfHighlight>? HighlightSelected;

        /// <summary>双击空白处取消高亮选中后触发，宿主可切回高亮绘制状态继续划词</summary>
        public event Action? HighlightDeselectedByDoubleClick;

        /// <summary>当前选中的高亮（画布点击或列表选中）</summary>
        public PdfHighlight? SelectedHighlight => _selectionManager.SelectedHighlight;

        public int ZoomLevel => _zoomController.ZoomLevel;
        public bool IsLocked => _zoomController.IsLocked;
        public Rectangle? LastSelectionRect => _lastSelectionRect ?? _pendingHighlightRect;
        public Point ImageOffset => _zoomController.ImageOffset;
        public AnnotationToolMode CurrentToolMode => _currentToolMode;
        public Color PenColor => _toolHandler.PenColor;
        public Color TemporaryPenColor => _temporaryPenColor;
        public float PenWidth => _toolHandler.PenWidth;
        public bool IsDashed => _toolHandler.IsDashed;
        public string PenType => _toolHandler.PenType;
        public Color HighlightPreviewColor => _toolHandler.HighlightPreviewColor;
        public bool IsDrawing => _toolHandler.IsDrawing;
        public List<PointF>? CurrentStrokePoints => _toolHandler.CurrentStrokePoints;
        public AnnotationStroke? SelectedStroke => _selectionManager.SelectedStroke;
        public int SelectedStrokeIndex => _selectionManager.SelectedStrokeIndex;
        public AnnotationStroke? HoveredStroke => _selectionManager.HoveredStroke;
        public SelectionInteractionState SelectionState => _selectionManager.SelectionState;

        /// <summary>当前页标注层版本号，供异步加载回调校验是否已过期。</summary>
        public int AnnotationRevision => _annotationLayerManager.CurrentPageRevision;

        /// <summary>双页第二页标注层版本号。</summary>
        public int SecondAnnotationRevision => _annotationLayerManager.SecondPageRevision;

        /// <summary>
        /// 用户是否正在进行绘制或标注拖拽/缩放。
        /// 此期间不得替换页面位图，否则标注图层会被清理，导致当前笔画丢失。
        /// </summary>
        public bool IsUserInteractionActive
        {
            get
            {
                if (_toolHandler.IsDrawing || _toolHandler.IsDrawingShape) return true;
                var state = _selectionManager.SelectionState;
                return state == SelectionInteractionState.Moving
                    || state == SelectionInteractionState.ResizingTopLeft
                    || state == SelectionInteractionState.ResizingTopRight
                    || state == SelectionInteractionState.ResizingBottomLeft
                    || state == SelectionInteractionState.ResizingBottomRight;
            }
        }

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
        /// <summary>获取当前录音 Id，用于多次同页录音时精确定位回放</summary>
        public Func<string?>? GetCurrentAudioRecordingId { get; set; }
        /// <summary>标注写入后触发，用于刷新本页时间戳标注列表</summary>
        public Action? AnnotationAddedCallback { get; set; }

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
            _selectionManager.UpdateStrokeAtCurrentPage = (idx, stroke) =>
            {
                _form.Presenter?.UpdateStrokeAtCurrentPage(idx, stroke);
                _annotationLayerManager.IncrementCurrentPageRevision();
            };
            _selectionManager.RemoveStrokeAtCurrentPage = (idx) =>
            {
                _form.Presenter?.RemoveStrokeAtCurrentPage(idx);
                _annotationLayerManager.IncrementCurrentPageRevision();
            };
            _selectionManager.RemoveTextAtCurrentPage = (idx, text) =>
            {
                _form.Presenter?.RemoveTextAtCurrentPage(idx);
                _annotationLayerManager.IncrementCurrentPageRevision();
            };
            _selectionManager.UpdateTextAtCurrentPage = (idx, text) =>
            {
                _form.Presenter?.UpdateTextAtCurrentPage(idx, text);
                _annotationLayerManager.IncrementCurrentPageRevision();
            };
            _selectionManager.ReloadAnnotations = () => LoadAnnotationsForCurrentPage();
            _selectionManager.InvalidateView = () => { try { _form.PictureBoxPdf?.Invalidate(); } catch { } };
            _selectionManager.PushStrokeToUndoStack = PushStrokeToUndoStack;
            _selectionManager.EditTextCallback = (text, idx) => EditTextCallback?.Invoke(text, idx);
            _selectionManager.UpdateHighlightCallback = (highlight) => _form.HighlightService?.UpdateHighlight(_form.CurrentPdfPath, highlight);
            _selectionManager.HighlightSelected += (highlight) => HighlightSelected?.Invoke(highlight);
            _selectionManager.HighlightDeselectedByDoubleClick += () => HighlightDeselectedByDoubleClick?.Invoke();
        }

        private void InitializeToolHandlerDelegates()
        {
            _toolHandler.PushStrokeToUndoStack = PushStrokeToUndoStack;
            _toolHandler.GetCurrentAudioTimestampMs = () => GetCurrentAudioTimestampMs?.Invoke();
            _toolHandler.GetCurrentAudioRecordingId = () => GetCurrentAudioRecordingId?.Invoke();
            _toolHandler.SaveAnnotationForPage = (pageIndex) => _form.Presenter?.SaveAnnotationForPage(pageIndex);
            _toolHandler.AddAnnotationStroke = (pts, color, thickness, imgW, imgH, shapeType, pageIdx, strokeStyle, audioTs, audioRecordingId) =>
                _form.Presenter?.AddAnnotationStroke(pts, color, thickness, imgW, imgH, shapeType, pageIdx, strokeStyle, audioTs, audioRecordingId);
            _toolHandler.ClientToImage = ClientToImage;
        }

        public void SetToolMode(AnnotationToolMode mode, bool isOneTime = false)
        {
            // 换到另一个工具即解除双击锁定的连续标注；同一工具的重复设置保留锁定状态
            // （按钮双击会触发两次 Click，第二次仍是同一工具，不能把刚锁定的状态清掉）
            if (mode != _currentToolMode)
                _toolSticky = false;

            _currentToolMode = mode;
            _isOneTimeTool = isOneTime;
            _toolHandler.IsDrawing = false;
            _isSelecting = false;
            _toolHandler.CurrentStrokePoints = null;
            _isLaserPointerActive = (mode == AnnotationToolMode.LaserPointer);
            _spotlightActive = (mode == AnnotationToolMode.Spotlight);
            // 放大镜在切到该模式后即随鼠标出现；切走时清掉位置，避免下次进入残留旧透镜
            _magnifierActive = (mode == AnnotationToolMode.Magnifier);
            if (!_magnifierActive)
            {
                _magnifierPosition = Point.Empty;
                _hasMagnifierPosition = false;
                _hasLastMagnifierPoint = false;
            }
            _selectionManager.ClearSelection();
            ClearTemporaryPen();

            if (_form.PictureBoxPdf != null)
            {
                _form.PictureBoxPdf.Cursor = mode == AnnotationToolMode.Select ? Cursors.SizeAll : Cursors.Default;
            }

            _form.PictureBoxPdf.Invalidate();
        }

        /// <summary>
        /// 如果当前是一次性工具，完成操作后自动恢复到默认模式
        /// </summary>
        public void RevertIfOneTimeTool()
        {
            if (_isOneTimeTool)
            {
                _isOneTimeTool = false;
                SetToolMode(_defaultToolMode);
            }
        }

        /// <summary>
        /// 设置默认工具模式（不触发工具切换，仅记录默认值）
        /// </summary>
        public void SetDefaultToolMode(AnnotationToolMode mode)
        {
            _defaultToolMode = mode;
        }

        /// <summary>工具是否被双击锁定为连续标注（锁定后画完不自动回到选择模式）。</summary>
        public bool IsToolSticky => _toolSticky;

        /// <summary>锁定当前工具为连续标注；由工具按钮双击触发。</summary>
        public void LockToolForContinuousDrawing()
        {
            _toolSticky = true;
        }

        /// <summary>通知宿主：一次标注已完成（是否回到选择由宿主按偏好判断）。</summary>
        private void RaiseAnnotationCommitted()
        {
            try
            {
                AnnotationCommitted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying annotation committed");
            }
        }

        public void SetPenColor(Color color)
        {
            _toolHandler.SetPenColor(color);
        }

        public void SetTemporaryPenColor(Color color)
        {
            _temporaryPenColor = color;
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

        /// <summary>
        /// 设置高亮预览使用的颜色，使高亮预览与实际落笔的高亮颜色保持一致
        /// （此前形状预览里的高亮色是硬编码的，不随所选高亮颜色变化）。
        /// </summary>
        public void SetHighlightPreviewColor(Color color)
        {
            _toolHandler.HighlightPreviewColor = color;
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
                    ResetPreviewInvalidation();
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
                        case AnnotationToolMode.TemporaryPen:
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
                            EnsureAnnotationBitmap();
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
                        case AnnotationToolMode.Magnifier:
                            _logger.LogInformation("MouseDown Left: Magnifier at {X},{Y}", e.Location.X, e.Location.Y);
                            // 透镜本身跟随鼠标移动，点击只重新定位并刷新，不再弹出独立放大窗口
                            _magnifierActive = true;
                            UpdateMagnifierPosition(e.Location);
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

                    if (_currentToolMode == AnnotationToolMode.Pen || _currentToolMode == AnnotationToolMode.TemporaryPen || _currentToolMode == AnnotationToolMode.Strikethrough || _toolHandler.IsDrawing)
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

        /// <summary>
        /// 计算当前绘制预览（自由笔划/形状）在控件客户区中的包围盒。
        /// 无法确定（双页模式、跨页预览、缺少图像）时返回 null，调用方回退到全控件重绘。
        /// </summary>
        private Rectangle? GetPreviewClientRect()
        {
            // 双页模式的第二页预览暂不做局部重绘，保持行为安全
            if (_form.IsDualPage) return null;

            var srcImage = _form.CurrentPageImage;
            if (srcImage == null) return null;
            if (_toolHandler.DrawingPageIndex >= 0 && _toolHandler.DrawingPageIndex != _form.CurrentPageIndex) return null;

            var imgRect = _form.GetImageDisplayRect();
            if (imgRect.Width <= 0 || imgRect.Height <= 0) return null;

            float scaleX = (float)imgRect.Width / srcImage.Width;
            float scaleY = (float)imgRect.Height / srcImage.Height;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            if (_toolHandler.IsDrawing && _toolHandler.CurrentStrokePoints != null && _toolHandler.CurrentStrokePoints.Count > 0)
            {
                foreach (var pt in _toolHandler.CurrentStrokePoints)
                {
                    if (pt.X < minX) minX = pt.X;
                    if (pt.Y < minY) minY = pt.Y;
                    if (pt.X > maxX) maxX = pt.X;
                    if (pt.Y > maxY) maxY = pt.Y;
                }
            }
            else if (_toolHandler.IsDrawingShape && _toolHandler.ShapeStartPoint.HasValue && _toolHandler.ShapeEndPoint.HasValue)
            {
                var a = _toolHandler.ShapeStartPoint.Value;
                var b = _toolHandler.ShapeEndPoint.Value;
                minX = Math.Min(a.X, b.X);
                minY = Math.Min(a.Y, b.Y);
                maxX = Math.Max(a.X, b.X);
                maxY = Math.Max(a.Y, b.Y);
            }
            else
            {
                return null;
            }

            int left = (int)Math.Floor(minX * scaleX) + imgRect.X - PreviewInvalidMargin;
            int top = (int)Math.Floor(minY * scaleY) + imgRect.Y - PreviewInvalidMargin;
            int right = (int)Math.Ceiling(maxX * scaleX) + imgRect.X + PreviewInvalidMargin;
            int bottom = (int)Math.Ceiling(maxY * scaleY) + imgRect.Y + PreviewInvalidMargin;

            var rect = Rectangle.FromLTRB(left, top, right, bottom);
            rect.Intersect(_form.PictureBoxPdf.ClientRectangle);
            return rect;
        }

        /// <summary>
        /// 请求重绘当前绘制预览：优先只重绘「上一帧 ∪ 当前」预览包围盒，避免整页重绘。
        /// </summary>
        private void InvalidatePreview()
        {
            var current = GetPreviewClientRect();
            if (!current.HasValue || current.Value.Width <= 0 || current.Value.Height <= 0)
            {
                _hasPreviewInvalidRect = false;
                _form.PictureBoxPdf.Invalidate();
                return;
            }

            var dirty = current.Value;
            if (_hasPreviewInvalidRect)
                dirty = Rectangle.Union(dirty, _previewInvalidRect);

            _previewInvalidRect = current.Value;
            _hasPreviewInvalidRect = true;
            _form.PictureBoxPdf.Invalidate(dirty);
        }

        /// <summary>开始一次新的绘制时重置预览脏区跟踪</summary>
        private void ResetPreviewInvalidation()
        {
            _previewInvalidRect = Rectangle.Empty;
            _hasPreviewInvalidRect = false;
            _hasLastPreviewPoint = false;
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
                    // 同一位置的重复移动直接跳过，减少无意义的预览重绘
                    if (_hasLastPreviewPoint && e.Location == _lastPreviewPoint) return;
                    _lastPreviewPoint = e.Location;
                    _hasLastPreviewPoint = true;
                    var imgPt = ClientToImage(e.Location);
                    _toolHandler.AddStrokePoint(imgPt);
                    InvalidatePreview();
                    return;
                }

                if (_toolHandler.IsDrawingShape)
                {
                    _selectEnd = e.Location;
                    if (_hasLastPreviewPoint && e.Location == _lastPreviewPoint) return;
                    _lastPreviewPoint = e.Location;
                    _hasLastPreviewPoint = true;
                    var endPt = ClientToImage(e.Location);

                    _toolHandler.UpdateShapeEnd(endPt, _toolHandler.ShapeStartPoint, _currentToolMode, (Control.ModifierKeys & Keys.Shift) == Keys.Shift);
                    InvalidatePreview();
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

                // 放大镜模式：透镜随鼠标移动，只重绘旧/新透镜包围盒
                if (_currentToolMode == AnnotationToolMode.Magnifier && _magnifierActive)
                {
                    UpdateMagnifierPosition(e.Location);
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
                // 绘制结束后会全量重绘一次，这里清理预览脏区跟踪
                ResetPreviewInvalidation();

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
                    _form.HighlightManager?.RefreshHighlightList();
                    AnnotationAddedCallback?.Invoke();

                    // 临时笔模式：记录笔迹并启动定时清除
                    if (_currentToolMode == AnnotationToolMode.TemporaryPen)
                    {
                        StartTemporaryPenClear();
                    }

                    // 一次性工具使用完成后自动恢复默认模式；
                    // 普通工具交给宿主按用户偏好决定是否回到选择模式
                    if (_isOneTimeTool)
                        RevertIfOneTimeTool();
                    else
                        RaiseAnnotationCommitted();
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
                    _form.HighlightManager?.RefreshHighlightList();
                    AnnotationAddedCallback?.Invoke();

                    // 一次性工具：形状绘制完成后自动恢复默认模式；
                    // 普通形状同样交给宿主按用户偏好回到选择模式
                    if (_isOneTimeTool)
                        RevertIfOneTimeTool();
                    else
                        RaiseAnnotationCommitted();
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
                        // 高亮已添加：与画笔/形状一样按宿主偏好回到选择模式
                        RaiseAnnotationCommitted();
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
                        var selectedHighlight = _selectionManager.SelectedHighlight;

                        // 本次是否真的移动/缩放过：仅位置变化时才重新取词，避免单击也触发 OCR/翻译
                        bool boundsChanged = HasSelectedHighlightBoundsChanged(selectedHighlight);

                        // 持久化高亮位置变化
                        var highlightService = _form.HighlightService;
                        if (highlightService != null && !string.IsNullOrEmpty(_form.CurrentPdfPath))
                        {
                            highlightService.UpdateHighlight(_form.CurrentPdfPath, selectedHighlight);
                        }

                        // 重建高亮离屏图层，使拖动/缩放后的位置立即生效（双页时按页码选择图层）
                        _form.HighlightManager?.RefreshHighlightLayerForPage(selectedHighlight.PageIndex);

                        // 保持选中状态：若在此处走 LoadAnnotationsForCurrentPage 会清空选中，
                        // 选中框与缩放句柄随之消失，导致双击选中后无法继续调整大小。
                        _selectionManager.SelectedHighlightOriginalBounds = new RectangleF(
                            selectedHighlight.NormalizedX, selectedHighlight.NormalizedY,
                            selectedHighlight.NormalizedWidth, selectedHighlight.NormalizedHeight);

                        // 位置/大小变化后按新矩形重新取词（文本层优先，OCR 兜底），
                        // 更新高亮列表文本并触发翻译
                        if (boundsChanged)
                        {
                            _ = _form.HighlightManager?.ReextractHighlightTextAsync(selectedHighlight);
                        }
                    }
                    else
                    {
                        _form.Presenter?.SaveAnnotationForCurrentPage();
                        // 重新加载标注位图，使调整后的位置立即生效
                        LoadAnnotationsForCurrentPage();
                    }

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

        /// <summary>
        /// 判断当前选中高亮相对本次按下时的矩形是否发生移动/缩放。
        /// </summary>
        private bool HasSelectedHighlightBoundsChanged(PdfHighlight highlight)
        {
            var start = _selectionManager.SelectedHighlightInteractionStartBounds;
            if (start.Width <= 0 || start.Height <= 0) return false;

            const float epsilon = 0.002f;
            return Math.Abs(highlight.NormalizedX - start.X) > epsilon
                || Math.Abs(highlight.NormalizedY - start.Y) > epsilon
                || Math.Abs(highlight.NormalizedWidth - start.Width) > epsilon
                || Math.Abs(highlight.NormalizedHeight - start.Height) > epsilon;
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
            // 必须先保存待定状态：StopLongPressTimer 内部会把 _isLongPressPending 置为 false
            bool shouldStartDragging = _isLongPressPending;
            StopLongPressTimer();
            if (shouldStartDragging)
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
                // 所有页面标注均已清除，同时使第二页在途加载失效（第二页不会在此处重载）
                _annotationLayerManager.IncrementSecondPageRevision();
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
            _form.HighlightManager?.RefreshHighlightList();
        }

        public void DeleteSelectedText()
        {
            _selectionManager.DeleteSelectedText();
            if (_selectionManager.SelectedTextIndex < 0)
                LoadAnnotationsForCurrentPage();
            _form.PictureBoxPdf.Invalidate();
            _form.HighlightManager?.RefreshHighlightList();
        }

        /// <summary>
        /// 删除当前选中的标注（笔画或文本）。返回是否实际删除了某个标注。
        /// 供 Delete 键与垃圾桶按钮统一调用：无任何选中时不执行删除并返回 false。
        /// </summary>
        public bool DeleteSelectedAnnotation()
        {
            // 选中高亮 → 删除高亮
            if (_selectionManager.SelectedHighlightIndex >= 0 && _selectionManager.SelectedHighlight != null)
            {
                DeleteSelectedHighlight();
                return true;
            }
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

        private void DeleteSelectedHighlight()
        {
            if (_selectionManager.SelectedHighlight == null || _selectionManager.SelectedHighlightIndex < 0) return;

            var highlight = _selectionManager.SelectedHighlight;

            _selectionManager.SelectedHighlight = null;
            _selectionManager.SelectedHighlightIndex = -1;
            _selectionManager.SelectionState = SelectionInteractionState.None;

            // 删除动作统一走高亮管理器：会写入撤销栈（Delete 键删掉的高亮可被 Ctrl+Z 恢复），
            // 同时由管理器负责列表项移除与当前页图层刷新。
            var highlightManager = _form.HighlightManager;
            if (highlightManager != null)
            {
                highlightManager.RemoveHighlight(highlight);
                // 删除后同步列表条数与空状态（与原直接删除路径保持一致的外围刷新）
                highlightManager.RefreshHighlightList();
            }
            else
            {
                // 管理器尚未初始化时的兜底：保持原有直接删除行为
                _form.HighlightService?.RemoveHighlight(_form.CurrentPdfPath, highlight.Id);
                _form.PictureBoxPdf?.Invalidate();
            }
        }

        public void ClearSelection()
        {
            _selectionManager.ClearSelection();
            _form.PictureBoxPdf.Invalidate();
        }

        /// <summary>
        /// 按 Id 选中当前页的标注/高亮，供列表点击与新建高亮自动选中调用。
        /// <param name="switchToSelectMode">为 true 时切到选择模式；连续标注自动选中时传 false，保持当前绘制工具。</param>
        /// </summary>
        public bool SelectAnnotationById(string id, bool switchToSelectMode = true)
        {
            // 列表点击等场景需要切到选择模式；连续标注自动选中时保持当前绘制工具
            if (switchToSelectMode)
                SetToolMode(AnnotationToolMode.Select);

            _selectionManager.ClearSelection();

            // 1. 在笔画中查找
            var strokes = _selectionManager.GetCurrentPageStrokes();
            for (int i = 0; i < strokes.Count; i++)
            {
                if (strokes[i].Id == id)
                {
                    _selectionManager.SelectedStroke = strokes[i];
                    _selectionManager.SelectedStrokeIndex = i;
                    _selectionManager.SelectionState = SelectionInteractionState.Idle;
                    _form.PictureBoxPdf?.Invalidate();
                    return true;
                }
            }

            // 2. 在文字标注中查找
            var texts = _selectionManager.GetCurrentPageTexts();
            for (int i = 0; i < texts.Count; i++)
            {
                if (texts[i].Id == id)
                {
                    _selectionManager.SelectedText = texts[i];
                    _selectionManager.SelectedTextIndex = i;
                    _selectionManager.SelectionState = SelectionInteractionState.Idle;
                    _form.PictureBoxPdf?.Invalidate();
                    return true;
                }
            }

            // 3. 在高亮中查找
            var highlights = _selectionManager.GetCurrentPageHighlights();
            for (int i = 0; i < highlights.Count; i++)
            {
                if (highlights[i].Id == id)
                {
                    _selectionManager.SelectedHighlight = highlights[i];
                    _selectionManager.SelectedHighlightIndex = i;
                    _selectionManager.SelectionState = SelectionInteractionState.Idle;
                    HighlightSelected?.Invoke(highlights[i]);
                    _form.PictureBoxPdf?.Invalidate();
                    return true;
                }
            }

            return false;
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

                // 绘制临时笔迹（叠加层，会在2秒后自动消失）
                DrawTemporaryPen(g, imgRect, pageIndex);

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
            using var path = new System.Drawing.Drawing2D.GraphicsPath();
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
        /// 绘制跟随鼠标的放大镜透镜：以光标所在页的位图为源，裁成圆形并按倍率放大。
        /// 单页/双页都通过 <see cref="IPdfReaderFormAccess.GetPageAtPoint"/> 取当前页，
        /// 夜间模式下用颜色矩阵反色，与页面显示保持一致。
        /// </summary>
        public void DrawMagnifierOverlay(Graphics g, Rectangle totalRect)
        {
            if (_currentToolMode != AnnotationToolMode.Magnifier || !_magnifierActive || !_hasMagnifierPosition)
                return;

            var pt = _magnifierPosition;
            // 鼠标不在页面区域时不绘制，避免在空白处留下一个放大的空白圆
            if (!totalRect.Contains(pt))
                return;

            var (_, pageRect, pageImage) = _form.GetPageAtPoint(pt);
            if (pageImage == null || pageRect.Width <= 0 || pageRect.Height <= 0)
                return;

            // 双页模式下 totalRect 包含两页之间的间隙，GetPageAtPoint 对间隙点会回退返回左页；
            // 这里再校验光标是否真的落在该页矩形内，避免在间隙里画出越界的窄条
            if (!pageRect.Contains(pt))
                return;

            var lensRect = new Rectangle(
                pt.X - MagnifierRadius, pt.Y - MagnifierRadius,
                MagnifierRadius * 2, MagnifierRadius * 2);

            // 页面显示矩形 -> 原始位图坐标，反推透镜内应截取的源区域
            float scaleX = (float)pageImage.Width / pageRect.Width;
            float scaleY = (float)pageImage.Height / pageRect.Height;
            float srcRadiusX = MagnifierRadius / MagnifierZoom * scaleX;
            float srcRadiusY = MagnifierRadius / MagnifierZoom * scaleY;
            float cx = (pt.X - pageRect.X) * scaleX;
            float cy = (pt.Y - pageRect.Y) * scaleY;

            float rawX = cx - srcRadiusX;
            float rawY = cy - srcRadiusY;
            float rawWidth = srcRadiusX * 2;
            float rawHeight = srcRadiusY * 2;

            // 靠近页面边缘时源区域会越界：按比例缩小目标矩形，保证放大内容不变形
            float srcX = Math.Max(0, rawX);
            float srcY = Math.Max(0, rawY);
            float srcWidth = Math.Min(pageImage.Width, rawX + rawWidth) - srcX;
            float srcHeight = Math.Min(pageImage.Height, rawY + rawHeight) - srcY;
            if (rawWidth <= 0 || rawHeight <= 0 || srcWidth <= 0 || srcHeight <= 0)
                return;

            var imageRect = new RectangleF(
                lensRect.X + (srcX - rawX) / rawWidth * lensRect.Width,
                lensRect.Y + (srcY - rawY) / rawHeight * lensRect.Height,
                srcWidth / rawWidth * lensRect.Width,
                srcHeight / rawHeight * lensRect.Height);

            // 柔和阴影：让透镜浮在页面上，但不使用独立窗口的硬边框
            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, lensRect.X + 3, lensRect.Y + 5, lensRect.Width, lensRect.Height);
            }

            var state = g.Save();
            var interpolation = g.InterpolationMode;
            try
            {
                using var clipPath = new GraphicsPath();
                clipPath.AddEllipse(lensRect);
                g.SetClip(clipPath, CombineMode.Intersect);
                g.InterpolationMode = InterpolationMode.HighQualityBilinear;

                // 目标矩形取整：GDI+ 对应重载的目标是 Rectangle，源坐标可保持浮点精度
                var destRectInt = Rectangle.Round(imageRect);
                if (destRectInt.Width <= 0 || destRectInt.Height <= 0)
                    return;

                // 先铺页面底色：光标靠近页边时，越界部分保持页面底色而不是透出下层内容
                bool isNight = _form.IsNightMode;
                using (var backgroundBrush = new SolidBrush(isNight ? Color.FromArgb(20, 20, 20) : Color.White))
                {
                    g.FillEllipse(backgroundBrush, lensRect);
                }

                if (isNight)
                {
                    using var attributes = new System.Drawing.Imaging.ImageAttributes();
                    var matrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                    {
                        new float[] { -1, 0, 0, 0, 0 },
                        new float[] { 0, -1, 0, 0, 0 },
                        new float[] { 0, 0, -1, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 1, 1, 1, 0, 1 }
                    });
                    attributes.SetColorMatrix(matrix);
                    g.DrawImage(pageImage, destRectInt, srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel, attributes);
                }
                else
                {
                    g.DrawImage(pageImage, destRectInt, srcX, srcY, srcWidth, srcHeight, GraphicsUnit.Pixel);
                }
            }
            finally
            {
                g.Restore(state);
                g.InterpolationMode = interpolation;
            }

            // 细描边：只用于看清透镜边界，不遮挡内容
            using var ringPen = new Pen(Color.FromArgb(140, 70, 70, 70), 1.5f);
            g.DrawEllipse(ringPen, lensRect);
            using var innerRingPen = new Pen(Color.FromArgb(180, 255, 255, 255), 1f);
            g.DrawEllipse(innerRingPen, lensRect.X + 1, lensRect.Y + 1, lensRect.Width - 2, lensRect.Height - 2);
        }

        /// <summary>透镜包围盒：含描边与阴影偏移的余量，供局部重绘使用。</summary>
        private static Rectangle GetMagnifierBounds(Point center)
        {
            int half = MagnifierRadius + 6;
            return new Rectangle(center.X - half, center.Y - half, half * 2, half * 2);
        }

        /// <summary>更新透镜位置，并把旧/新位置包围盒标记为脏区，避免整页重绘。</summary>
        private void UpdateMagnifierPosition(Point position)
        {
            var pictureBox = _form.PictureBoxPdf;
            if (pictureBox == null) return;
            // 同一位置的重复移动直接跳过，减少无意义的重绘
            if (_hasLastMagnifierPoint && position == _lastMagnifierPoint) return;

            var oldBounds = _hasMagnifierPosition ? GetMagnifierBounds(_magnifierPosition) : Rectangle.Empty;
            _magnifierPosition = position;
            _hasMagnifierPosition = true;
            _lastMagnifierPoint = position;
            _hasLastMagnifierPoint = true;

            var newBounds = GetMagnifierBounds(position);
            var dirty = oldBounds.IsEmpty ? newBounds : Rectangle.Union(oldBounds, newBounds);
            if (dirty.Width > 0 && dirty.Height > 0)
                pictureBox.Invalidate(dirty);
        }

        /// <summary>收起透镜并擦除旧位置；鼠标离开画布或切换工具时调用。</summary>
        public void ClearMagnifier()
        {
            if (!_hasMagnifierPosition) return;

            var oldBounds = GetMagnifierBounds(_magnifierPosition);
            _magnifierPosition = Point.Empty;
            _hasMagnifierPosition = false;
            _hasLastMagnifierPoint = false;

            var pictureBox = _form.PictureBoxPdf;
            if (pictureBox != null && oldBounds.Width > 0 && oldBounds.Height > 0)
                pictureBox.Invalidate(oldBounds);
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
            // 产生新笔划后，之前的重做路径失效（标准撤销/重做语义）
            _strokeRedoStack.Clear();
            UndoActionRecorded?.Invoke(this, EventArgs.Empty);
        }

        public bool CanUndoStroke()
        {
            return _strokeUndoStack.Count > 0;
        }

        public bool CanRedoStroke()
        {
            return _strokeRedoStack.Count > 0;
        }

        /// <summary>清空撤销/重做历史（切换文档时调用，避免把上一本文档的笔迹重做到新文档）。</summary>
        public void ClearUndoRedoHistory()
        {
            _strokeUndoStack.Clear();
            _strokeRedoStack.Clear();
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
                    // 取回真正落盘的笔划（页面归一化坐标），供重做时原样写回
                    var removed = _form.Presenter.UndoAnnotationStroke();
                    if (removed != null)
                    {
                        removed.PageIndex = _form.CurrentPageIndex;
                        PushStrokeToRedoStack(removed);
                    }
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

        private void PushStrokeToRedoStack(AnnotationStroke stroke)
        {
            if (stroke == null) return;

            if (_strokeRedoStack.Count >= MaxUndoStackSize)
            {
                var items = _strokeRedoStack.ToArray();
                _strokeRedoStack.Clear();
                for (int i = items.Length - 2; i >= 0; i--)
                    _strokeRedoStack.Push(items[i]);
            }
            _strokeRedoStack.Push(stroke);
        }

        /// <summary>
        /// 重做最近撤销的笔划：按撤销时取回的实体写回原页，并重新放回撤销栈。
        /// 返回 true 表示成功；重做不应再清空重做栈，所以这里直接入栈而不走 PushStrokeToUndoStack。
        /// </summary>
        public bool RedoStroke()
        {
            if (_strokeRedoStack.Count == 0)
                return false;

            var stroke = _strokeRedoStack.Pop();

            try
            {
                int pageIndex = stroke.PageIndex >= 0 ? stroke.PageIndex : _form.CurrentPageIndex;
                if (_form.Presenter?.RestoreAnnotationStroke(stroke, pageIndex) != true)
                    return false;

                _strokeUndoStack.Push(stroke);

                CleanupAnnotationBitmap();
                LoadAnnotationsForCurrentPage();
                _form.PictureBoxPdf.Invalidate();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error redoing stroke");
                return false;
            }
        }

        #region 临时笔（TemporaryPen）支持

        /// <summary>
        /// 启动临时笔迹清除定时器（2秒后自动清除临时笔迹）
        /// </summary>
        private void StartTemporaryPenClear()
        {
            // 记录当前笔迹点用于临时渲染
            _temporaryPenPoints = _toolHandler.CurrentStrokePoints != null
                ? new List<PointF>(_toolHandler.CurrentStrokePoints)
                : null;
            _temporaryPenPageIndex = _form.CurrentPageIndex;

            // 启动定时器，2秒后清除临时笔迹
            if (_temporaryPenClearTimer == null)
            {
                _temporaryPenClearTimer = new System.Windows.Forms.Timer();
                _temporaryPenClearTimer.Tick += (s, e) =>
                {
                    ClearTemporaryPen();
                    _form.PictureBoxPdf.Invalidate();
                };
            }
            _temporaryPenClearTimer.Interval = 2000;
            _temporaryPenClearTimer.Start();
        }

        /// <summary>
        /// 清除临时笔迹
        /// </summary>
        private void ClearTemporaryPen()
        {
            if (_temporaryPenClearTimer != null)
            {
                _temporaryPenClearTimer.Stop();
            }
            _temporaryPenPoints = null;
            _temporaryPenPageIndex = -1;
        }

        /// <summary>
        /// 在图片上绘制临时笔迹（叠加层，不写入标注层）
        /// </summary>
        private void DrawTemporaryPen(Graphics g, Rectangle imgRect, int pageIndex)
        {
            if (_temporaryPenPoints == null || _temporaryPenPoints.Count < 2 || pageIndex != _temporaryPenPageIndex)
                return;

            var pen = _toolHandler.CreatePen(_temporaryPenColor, _toolHandler.PenWidth);
            if (pen == null) return;

            try
            {
                // 将图片坐标的笔迹点转换为屏幕坐标
                float scaleX = (float)imgRect.Width / _form.CurrentPageImage!.Width;
                float scaleY = (float)imgRect.Height / _form.CurrentPageImage.Height;

                var screenPoints = _temporaryPenPoints.Select(pt =>
                    new PointF(pt.X * scaleX + imgRect.X, pt.Y * scaleY + imgRect.Y)).ToArray();

                g.DrawLines(pen, screenPoints);
            }
            finally
            {
                pen.Dispose();
            }
        }

        #endregion

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

                try
                {
                    if (_temporaryPenClearTimer != null)
                    {
                        _temporaryPenClearTimer.Stop();
                        _temporaryPenClearTimer.Dispose();
                        _temporaryPenClearTimer = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing temporary pen timer");
                }
            }

            _disposed = true;
        }
    }
}
