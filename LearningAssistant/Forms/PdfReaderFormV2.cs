using LearningAssistant.Common.Events;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class PdfReaderFormV2 : Form, IPdfView, IPdfReaderFormAccess
    {
        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderFormV2> _logger;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private readonly Services.Learning.IPendingContentService? _pendingContentService;
        private readonly IHighlightService _highlightService;
        private readonly IBookmarkService _bookmarkService;
        private readonly IAnnotationService? _annotationService;
        private readonly IEventBus? _eventBus;

        private PdfReaderNightModeManager? _nightModeManager;
        private PdfReaderHighlightManager? _highlightManager;
        private PdfReaderBookmarkManager? _bookmarkManager;
        private PdfReaderNavigationManager? _navigationManager;

        private readonly Pen _pen = new Pen(Color.Red, 4f);
        private bool _disposed = false;

        private Panel? _ocrPanel;
        private PictureBox? _ocrPictureBox;
        private Button? _ocrCloseButton;

        private LoadingIndicator? _loadingIndicator;
        private bool _isTranslationEnabled = false;

        private GroupBox? _groupBoxBookmarks;
        private ListBox? _listBoxBookmarks;
        private Button? _buttonAddBookmark;
        private Button? _buttonRemoveBookmark;
        private TextBox? _textBoxBookmarkTitle;

        private GroupBox? _groupBoxHighlights;
        private ListBox? _listBoxHighlights;
        private Button? _buttonRemoveHighlight;
        private Button? _buttonBatchRemoveHighlight;
        private Button? _buttonEditHighlight;
        private Button? _buttonExportHighlights;

        private TabPage? _tabPageBookmarksAndHighlights;

        private System.ComponentModel.IContainer components = null;
        private TreeView _treeViewFiles;
        private Panel _panelPdf;
        private PictureBox _pictureBoxPdf;
        private ContextMenuStrip _contextMenuPdf;
        private Label? _toastLabel;
        private Panel _panelThumbnails;
        private FlowLayoutPanel _flowLayoutPanelThumbnails;
        private TabPage _tabPageTranslate;
        private TextBox _textBoxOriginal;
        private Label _labelOriginal;
        private Label _labelTranslation;
        private TextBox _textBoxTranslation;
        private Button _buttonTranslate;
        private Button _buttonSpeakTranslation;
        private Button _buttonAddToLearningContent;
        private CheckBox _checkBoxAutoSpeak;
        private Panel _panelNavigation;
        private Button _buttonPrev;
        private TextBox _textBoxPage;
        private Label _labelPageCount;
        private Button _buttonNext;
        private ProgressBar _progressBarPage;
        private Button _buttonNightMode;
        private Button _buttonTranslationToggle;
        private Button _buttonAskAi;
        private Button _buttonOpenFolder;
        private Label _labelZoom;
        private TrackBar _trackBarZoom;
        private TabControl _tabControlLeft;
        private TabPage _tabPageFiles;
        private TabPage _tabPageThumbnails;
        private Panel _panelLeftContainer;

        private Panel _bookmarkContainer;
        private FlowLayoutPanel _buttonPanel;
        private Panel _highlightContainer;
        private FlowLayoutPanel _highlightButtonPanel;
        private Button _buttonUndoHighlight;

        private GroupBox _groupBoxHighlightColor;
        private RadioButton _radioHighlightYellow;
        private RadioButton _radioHighlightGreen;
        private RadioButton _radioHighlightBlue;
        private RadioButton _radioHighlightPink;
        private RadioButton _radioHighlightOrange;

        private Label _transitionLabel;

        private Panel _toolbarGroupNav;
        private Panel _toolbarGroupView;
        //private ToolStrip _toolbarGroupViewZoom;
        private Panel _toolbarGroupMode;
        private Panel _toolbarGroupTools;
        private Button _buttonZoomOut;
        private Button _buttonZoomIn;
        private Button _buttonResetView;
        private Button _buttonRotate;
        private Button _buttonLockView;
        //private ToolStripDropDownButton? _buttonZoomPreset;
        private Button _buttonDualPage;
        private Button _buttonFullscreen;
        private Button _buttonHighlightMode;
        private Button _buttonRectangleMode;
        private Button _buttonEllipseMode;
        private Button _buttonArrowMode;
        private Button _buttonPenMode;
        private Button _buttonMosaicMode;
        private Button _buttonStrikethroughMode;
        private Button _buttonTextMode;
        private Button _buttonUndoAnnotation;
        private Button _buttonClearAllAnnotations;
        private Button _buttonSelectMode;
        private Panel _panelAnnotationOptions;
        private Panel _panelThickness;
        private Button _buttonThickness1;
        private Button _buttonThickness2;
        private Button _buttonThickness3;
        private Panel _panelColor;
        private Button _buttonColorBlue;
        private Button _buttonColorGreen;
        private Button _buttonColorOrange;
        private Button _buttonColorRed;
        private Button _buttonColorBlack;
        private Button _buttonColorWhite;
        private Panel? _statusBar;
        private Label? _statusLabelLeft;
        private Label? _statusLabelRight;
        private Panel? _pageTransitionOverlay;
        private GroupBox? _groupBoxProgress;
        private Button? _buttonSpeakOriginal;
        private System.Windows.Forms.Timer? _pageTransitionTimer;
        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private bool _isImageMode = false;
        private string _currentLanguage = "chi_sim";

        // 图片模式下，缩略图按目录分组展示时，记录已创建的目录分组标题
        private readonly Dictionary<string, Label> _thumbnailDirectoryHeaders = new Dictionary<string, Label>(StringComparer.OrdinalIgnoreCase);


        private Bitmap? _currentPageImage;
        private Bitmap? _secondPageImage;



        private int _zoomLevel = 100;
        private Point _imageOffset = Point.Empty;

        private int CurrentZoomLevel => _navigationManager?.ZoomLevel ?? _zoomLevel;
        private Point CurrentImageOffset => _navigationManager?.ImageOffset ?? _imageOffset;
        private Rectangle? _lastSelectionRect;

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;

        private bool _isLongPressPending = false;
        private Point _longPressStartLocation = Point.Empty;
        private bool _longPressDragStarted = false;

        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTimeMs = 200;
        private const int DoubleClickDistance = 5;
        private const string AppTitle = "学习助手";
        private const string WarningTitle = "警告";
        private const string ErrorTitle = "错误";
        private const string InfoTitle = "提示";
        private bool _isDoubleClickPending = false;

        private bool _isSelecting = false;
        private bool _isDrawing = false;
        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private List<PointF>? _currentStrokePoints;
        private int _rotationAngle = 0;

        private bool _isHighlightMode = true;
        private bool _isLocked = false;
        private bool _isDragging = false;
        private Point _dragStart = Point.Empty;

        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;

        private System.Windows.Forms.Timer? _longPressTimer;

        private HighlightColor _currentHighlightColor = HighlightColor.Yellow;

        private bool _isNightMode = false;
        private bool _isDualPage = false;
        private bool _isFullscreen = false;
        private Bitmap? _highlightBitmap;
        private ToolStripMenuItem menuItemCopy;
        private ToolStripMenuItem menuItemSearch;
        private ToolStripMenuItem menuItemHighlight;
        private ToolStripMenuItem menuItemRectangle;
        private ToolStripMenuItem menuItemText;
        private ToolStripMenuItem menuItemZoomIn;
        private ToolStripMenuItem menuItemZoomOut;
        private ToolStripMenuItem menuItemResetZoom;
        private ToolStripMenuItem menuItemExport;
        private ToolStripSeparator menuItemRotationSeparator;
        private ToolStripMenuItem menuItemRotateLeft;
        private ToolStripMenuItem menuItemRotateRight;
        private ToolStripMenuItem menuItemResetRotation;
        private ToolTip _toolTip;
        private TextBox _textBoxFilter;
        private List<string> _allFiles = new List<string>();
        private CheckBox _checkBoxAutoTranslate;
        private SplitContainer _splitContainerMain;


        public PdfReaderFormV2(ILogger<PdfReaderFormV2> logger, IAIPanelPopupService? aiPanelPopupService = null, Services.Learning.IPendingContentService? pendingContentService = null, IHighlightService? highlightService = null, IBookmarkService? bookmarkService = null, IAnnotationService? annotationService = null, IEventBus? eventBus = null)
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiPanelPopupService = aiPanelPopupService;
            _pendingContentService = pendingContentService;
            _highlightService = highlightService ?? new HighlightService();
            _bookmarkService = bookmarkService ?? new BookmarkService();
            _annotationService = annotationService;
            _eventBus = eventBus;
            KeyPreview = true;
            Load += PdfReaderFormV2_Load;
            Resize += PdfReaderFormV2_Resize;
            KeyDown += PdfReaderFormV2_KeyDown;

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            _nightModeManager = new PdfReaderNightModeManager(_logger, this);
            _highlightManager = new PdfReaderHighlightManager(_logger, this, _highlightService, _annotationService, _eventBus);
            _bookmarkManager = new PdfReaderBookmarkManager(_logger, this, _bookmarkService);
            _navigationManager = new PdfReaderNavigationManager(_logger, this);

            _navigationManager.IsHighlightModeCallback = () => _highlightManager?.IsHighlightMode ?? true;
            _navigationManager.AddHighlightCallback = rect => _highlightManager?.AddHighlight(rect);
            _navigationManager.AddTextCallback = point => ShowTextAnnotationDialog(point);

            if (_pictureBoxPdf != null)
            {
                _pictureBoxPdf.MouseDown += _navigationManager.MouseDown;
                _pictureBoxPdf.MouseMove += _navigationManager.MouseMove;
                _pictureBoxPdf.MouseUp += _navigationManager.MouseUp;
            }

            InitializeHighlightColorButtonEvents();
            InitializeToolbarButtonHoverEffects();
        }

        private void InitializeToolbarButtonHoverEffects()
        {
            var toolbarButtons = new[]
            {
                _buttonPrev, _buttonNext,
                _buttonZoomOut, _buttonZoomIn, _buttonResetView,
                _buttonRotate, _buttonLockView, _buttonNightMode,
                _buttonDualPage, _buttonFullscreen,
                _buttonSelectMode, _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenMode, _buttonMosaicMode, _buttonTextMode,
                _buttonUndoAnnotation, _buttonClearAllAnnotations, _buttonAskAi
            };

            foreach (var button in toolbarButtons)
            {
                if (button != null)
                {
                    button.MouseEnter += ToolbarButton_MouseEnter;
                    button.MouseLeave += ToolbarButton_MouseLeave;
                }
            }
        }

        private void ToolbarButton_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                if (isNightMode)
                {
                    button.BackColor = Color.FromArgb(60, 60, 60);
                }
                else
                {
                    button.BackColor = Color.FromArgb(230, 240, 255);
                }
            }
        }

        private void ToolbarButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                if (isNightMode)
                {
                    button.BackColor = Color.FromArgb(45, 45, 45);
                }
                else
                {
                    button.BackColor = Color.White;
                }
            }
        }

        private void InitializeHighlightColorButtonEvents()
        {
            var radioButtons = new[] { _radioHighlightYellow, _radioHighlightGreen, _radioHighlightBlue, _radioHighlightPink, _radioHighlightOrange };
            foreach (var radio in radioButtons)
            {
                if (radio != null)
                {
                    radio.MouseEnter += RadioHighlightColor_MouseEnter;
                    radio.MouseLeave += RadioHighlightColor_MouseLeave;
                }
            }
        }

        #region IPdfReaderFormAccess Implementation

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentPdfPath
        {
            get => _currentPdfPath;
            set => _currentPdfPath = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentPageIndex
        {
            get => _currentPageIndex;
            set => _currentPageIndex = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap? CurrentPageImage
        {
            get => _currentPageImage;
            set => _currentPageImage = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Bitmap? SecondPageImage
        {
            get => _secondPageImage;
            set => _secondPageImage = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsTranslationEnabled
        {
            get => _isTranslationEnabled;
            set => _isTranslationEnabled = value;
        }

        public bool IsDualPage => _isDualPage;
        public bool IsNightMode => _nightModeManager?.IsNightMode ?? false;

        public PictureBox PictureBoxPdf => _pictureBoxPdf;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PdfPresenter? Presenter
        {
            get => _presenter;
            set => _presenter = value;
        }
        public TextBox TextBoxOriginal => _textBoxOriginal;
        public TextBox TextBoxPage => _textBoxPage;
        public Label LabelZoom => _labelZoom;
        public TrackBar TrackBarZoom => _trackBarZoom;

        public Button? ButtonNightMode => _buttonNightMode;
        public Button? ButtonAskAi => _buttonAskAi;

        public TabPage? TabPageTranslate => _tabPageTranslate;
        public GroupBox? GroupBoxProgress => _groupBoxProgress;
        public TextBox? TextBoxTranslation => _textBoxTranslation;
        public Label? LabelOriginal => _labelOriginal;
        public Label? LabelTranslation => _labelTranslation;
        public Button? ButtonTranslate => _buttonTranslate;
        public Button? ButtonSpeakOriginal => _buttonSpeakOriginal;
        public Button? ButtonSpeakTranslation => _buttonSpeakTranslation;

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool AutoSpeakAfterOcr
        {
            get => _checkBoxAutoSpeak?.Checked ?? false;
            set => _checkBoxAutoSpeak.Checked = value;
        }

        [System.ComponentModel.Browsable(false)]
        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public bool AutoTranslateAfterOcr
        {
            get => _checkBoxAutoTranslate?.Checked ?? false;
            set => _checkBoxAutoTranslate.Checked = value;
        }

        public TabPage? TabPageBookmarksAndHighlights => _tabPageBookmarksAndHighlights;
        public GroupBox? GroupBoxBookmarks => _groupBoxBookmarks;
        public ListBox? ListBoxBookmarks => _listBoxBookmarks;
        public TextBox? TextBoxBookmarkTitle => _textBoxBookmarkTitle;
        public Button? ButtonAddBookmark => _buttonAddBookmark;
        public Button? ButtonRemoveBookmark => _buttonRemoveBookmark;

        public GroupBox? GroupBoxHighlights => _groupBoxHighlights;
        public ListBox? ListBoxHighlights => _listBoxHighlights;
        public GroupBox? GroupBoxHighlightColor => _groupBoxHighlightColor;
        public Button? ButtonRemoveHighlight => _buttonRemoveHighlight;
        public Button? ButtonBatchRemoveHighlight => _buttonBatchRemoveHighlight;
        public Button? ButtonExportHighlights => _buttonExportHighlights;
        public Button? ButtonUndoHighlight => _buttonUndoHighlight;

        public Panel? PanelPdf => _panelPdf;
        public Panel? PanelNavigation => _panelNavigation;
        public Panel? PanelLeftContainer => _panelLeftContainer;
        public TreeView? TreeViewFiles => _treeViewFiles;
        public TabControl? TabControlLeft => _tabControlLeft;
        public Panel? PanelThumbnails => _panelThumbnails;
        public FlowLayoutPanel? FlowLayoutPanelThumbnails => _flowLayoutPanelThumbnails;

        public Panel? PageTransitionOverlay => _pageTransitionOverlay;
        public System.Windows.Forms.Timer? PageTransitionTimer => _pageTransitionTimer;
        public Button? ButtonLockView => _buttonLockView;

        public Panel? StatusBar => _statusBar;
        public Label? StatusLabelLeft => _statusLabelLeft;
        public Label? StatusLabelRight => _statusLabelRight;

        public Pen Pen => _pen;

        public Form Form => this;

        public Button? ButtonLanguage => null;


        public void OnSelectOcrClicked() => SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        public void OnTranslateClicked() => TranslateClicked?.Invoke(this, EventArgs.Empty);

        #endregion

        private void PdfReaderFormV2_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
            _presenter?.LoadLastSessionAndRestore();
            UpdateStatusBar();
            UpdateAnnotationColorSelection(Color.Black);
            UpdateAnnotationThicknessSelection(2);
        }

        private void PdfReaderFormV2_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                    case Keys.PageUp:
                        _navigationManager?.PreviousPage();
                        e.Handled = true;
                        break;
                    case Keys.Right:
                    case Keys.PageDown:
                    case Keys.Space:
                        _navigationManager?.NextPage();
                        e.Handled = true;
                        break;
                    case Keys.Home:
                        if (_presenter != null && _presenter.PageCount > 0)
                        {
                            _presenter.RenderPage(0);
                        }
                        e.Handled = true;
                        break;
                    case Keys.End:
                        if (_presenter != null && _presenter.PageCount > 0)
                        {
                            _presenter.RenderPage(_presenter.PageCount - 1);
                        }
                        e.Handled = true;
                        break;
                    case Keys.Oemplus:
                    case Keys.Add:
                        if (e.Control)
                        {
                            ButtonZoomIn_Click(this, EventArgs.Empty);
                            e.Handled = true;
                        }
                        break;
                    case Keys.OemMinus:
                    case Keys.Subtract:
                        if (e.Control)
                        {
                            ButtonZoomOut_Click(this, EventArgs.Empty);
                            e.Handled = true;
                        }
                        break;
                    case Keys.D0:
                        if (e.Control)
                        {
                            _navigationManager?.ResetZoom();
                            UpdateStatusBar();
                            e.Handled = true;
                        }
                        break;
                    case Keys.Delete:
                        _navigationManager?.DeleteSelectedStroke();
                        e.Handled = true;
                        break;
                    case Keys.F:
                        if (e.Control)
                        {
                            ShowSearchDialog();
                            e.Handled = true;
                        }
                        else
                        {
                            ToggleFullscreen();
                            e.Handled = true;
                        }
                        break;
                    case Keys.N:
                        _nightModeManager?.ToggleNightMode();
                        UpdateStatusBar();
                        e.Handled = true;
                        break;
                    case Keys.Escape:
                        if (_isFullscreen)
                        {
                            ToggleFullscreen();
                            e.Handled = true;
                        }
                        else if (WindowState == FormWindowState.Maximized)
                        {
                            WindowState = FormWindowState.Normal;
                            e.Handled = true;
                        }
                        break;
                    case Keys.H:
                        ButtonHighlightMode_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.T:
                        ButtonTranslationToggle_Click(this, EventArgs.Empty);
                        e.Handled = true;
                        break;
                    case Keys.O:
                        if (e.Control)
                        {
                            ButtonOpenFolder_Click(this, EventArgs.Empty);
                            e.Handled = true;
                        }
                        break;
                    case Keys.Z:
                        if (e.Control && !e.Shift)
                        {
                            _buttonUndoAnnotation?.PerformClick();
                            e.Handled = true;
                        }
                        break;
                    case Keys.Y:
                        if (e.Control)
                        {
                            _presenter?.UndoAnnotationStroke();
                            e.Handled = true;
                        }
                        break;
                    case Keys.S:
                        if (e.Control)
                        {
                            _presenter?.ExportHighlights();
                            e.Handled = true;
                        }
                        break;
                    case Keys.P:
                        if (e.Control)
                        {
                            _presenter?.PrintPdf();
                            e.Handled = true;
                        }
                        break;
                    case Keys.B:
                        if (e.Control)
                        {
                            ButtonDualPage_Click(this, EventArgs.Empty);
                            e.Handled = true;
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard shortcut");
            }
        }

        private void ToggleFullscreen()
        {
            try
            {
                if (!_isFullscreen)
                {
                    FormBorderStyle = FormBorderStyle.None;
                    WindowState = FormWindowState.Maximized;
                    if (_panelNavigation != null)
                        _panelNavigation.Visible = false;
                    if (_statusBar != null)
                        _statusBar.Visible = false;
                    _isFullscreen = true;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                    if (_panelNavigation != null)
                        _panelNavigation.Visible = true;
                    if (_statusBar != null)
                        _statusBar.Visible = true;
                    _isFullscreen = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling fullscreen");
            }
        }

        private void PdfReaderFormV2_Resize(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
        }

        private void AdjustPanelPdfSize()
        {
            _pictureBoxPdf.Invalidate();
        }

        private void UpdateStatusBar()
        {
            if (_statusLabelLeft != null)
            {
                int totalPages = 0;
                if (_labelPageCount != null && !string.IsNullOrEmpty(_labelPageCount.Text))
                {
                    var parts = _labelPageCount.Text.Replace("/", "").Trim();
                    int.TryParse(parts, out totalPages);
                }
                int currentPage = _currentPageIndex + 1;
                double progress = totalPages > 0 ? (double)currentPage / totalPages * 100 : 0;

                string fileName = string.Empty;
                if (!string.IsNullOrEmpty(_currentPdfPath))
                {
                    fileName = Path.GetFileName(_currentPdfPath);
                    if (fileName.Length > 30)
                    {
                        fileName = fileName.Substring(0, 27) + "...";
                    }
                }

                if (!string.IsNullOrEmpty(fileName))
                {
                    _statusLabelLeft.Text = $"{fileName}  ·  第 {currentPage} 页 / 共 {totalPages} 页  ·  阅读进度 {progress:F0}%";
                }
                else
                {
                    _statusLabelLeft.Text = $"第 {currentPage} 页 / 共 {totalPages} 页  ·  阅读进度 {progress:F0}%";
                }
            }
            if (_statusLabelRight != null)
            {
                string mode = GetCurrentToolModeText();
                string dualPage = _isDualPage ? "双页" : "单页";
                string zoom = $"{CurrentZoomLevel}%";

                int highlightCount = 0;
                try
                {
                    if (!string.IsNullOrEmpty(_currentPdfPath) && _highlightService != null)
                    {
                        highlightCount = _highlightService.GetHighlights(_currentPdfPath)?.Count ?? 0;
                    }
                }
                catch { }

                string highlightInfo = highlightCount > 0 ? $"  ·  标注 {highlightCount}" : string.Empty;
                _statusLabelRight.Text = $"缩放 {zoom}  ·  {mode}  ·  {dualPage}{highlightInfo}";
            }
        }

        private string GetCurrentToolModeText()
        {
            if (_navigationManager == null) return "选择模式";

            var toolMode = _navigationManager.CurrentToolMode;
            return toolMode switch
            {
                AnnotationToolMode.Highlight => "高亮模式",
                AnnotationToolMode.Rectangle => "矩形标注",
                AnnotationToolMode.Ellipse => "椭圆标注",
                AnnotationToolMode.Arrow => "箭头标注",
                AnnotationToolMode.Pen => "画笔模式",
                AnnotationToolMode.Mosaic => "马赛克",
                AnnotationToolMode.Text => "文字注解",
                _ => "选择模式"
            };
        }

        private void CleanupOldTabPages()
        {
            try
            {
                if (_tabControlLeft != null)
                {
                    if (_tabPageBookmarksAndHighlights != null && _tabControlLeft.TabPages.Contains(_tabPageBookmarksAndHighlights))
                    {
                        _tabControlLeft.TabPages.Remove(_tabPageBookmarksAndHighlights);
                    }
                }

                _groupBoxBookmarks?.Dispose();
                _listBoxBookmarks?.Dispose();
                _buttonAddBookmark?.Dispose();
                _buttonRemoveBookmark?.Dispose();
                _textBoxBookmarkTitle?.Dispose();
                _groupBoxHighlights?.Dispose();
                _listBoxHighlights?.Dispose();
                _buttonRemoveHighlight?.Dispose();
                _buttonBatchRemoveHighlight?.Dispose();

                _groupBoxBookmarks = null;
                _listBoxBookmarks = null;
                _buttonAddBookmark = null;
                _buttonRemoveBookmark = null;
                _textBoxBookmarkTitle = null;
                _groupBoxHighlights = null;
                _listBoxHighlights = null;
                _buttonRemoveHighlight = null;
                _buttonBatchRemoveHighlight = null;
                _tabPageBookmarksAndHighlights = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in CleanupOldTabPages");
            }
        }

        private void StartPageTransition(bool forward)
        {
            _navigationManager?.StartPageTransition(forward);
        }

        private void PageTransitionTimer_Tick(object? sender, EventArgs e)
        {
            _navigationManager?.PageTransitionTimer_Tick();
        }

        private void RadioHighlightColor_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadioButton radio) return;

            radio.FlatAppearance.BorderSize = radio.Checked ? 2 : 0;
            radio.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);

            if (radio.Checked && radio.Tag is int colorIndex)
            {
                var color = (HighlightColor)colorIndex;
                _currentHighlightColor = color;
                if (_highlightManager != null)
                {
                    _highlightManager.CurrentHighlightColor = color;
                }
                _pictureBoxPdf?.Invalidate();
            }
        }

        private void RadioHighlightColor_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
            {
                radio.FlatAppearance.BorderSize = 1;
                radio.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);
            }
        }

        private void RadioHighlightColor_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && !radio.Checked)
            {
                radio.FlatAppearance.BorderSize = 0;
            }
        }

        private void ListBoxBookmarks_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBoxBookmarks?.SelectedItem is PdfBookmark bookmark)
            {
                _bookmarkManager?.NavigateToBookmark(bookmark);
            }
        }

        private void ButtonAddBookmark_Click(object? sender, EventArgs e)
        {
            _bookmarkManager?.AddBookmark();
        }

        private void ButtonRemoveBookmark_Click(object? sender, EventArgs e)
        {
            _bookmarkManager?.RemoveBookmark();
        }

        private void ListBoxHighlights_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is IPdfNavigatable item)
            {
                NavigateToItem(item);
            }
        }

        private void NavigateToItem(IPdfNavigatable item)
        {
            if (item.PdfPath != _currentPdfPath)
            {
                if (_presenter != null)
                {
                    string extension = Path.GetExtension(item.PdfPath).ToLower();
                    if (extension == ".pdf")
                        _presenter.LoadPdf(item.PdfPath);
                    else
                    {
                        _presenter.LoadPdf(Path.GetFileName(item.PdfPath));
                    }
                }
            }
            else
            {
                _presenter?.RenderPage(item.PageIndex);
            }
        }

        private void ButtonLockView_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ToggleLockView();
            UpdateStatusBar();
        }

        private void ButtonResetView_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ResetZoom();
            UpdateStatusBar();
        }

        private void ButtonRotate_Click(object? sender, EventArgs e)
        {
            _rotationAngle = (_rotationAngle + 90) % 360;
            _presenter?.RenderPage(_currentPageIndex);
            ShowToast($"已旋转 {_rotationAngle}°");
        }

        private void ButtonRotate_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _rotationAngle = (_rotationAngle + 270) % 360;
                _presenter?.RenderPage(_currentPageIndex);
                ShowToast($"已旋转 {_rotationAngle}°");
            }
        }

        private void MenuItemRotateLeft_Click(object? sender, EventArgs e)
        {
            _rotationAngle = (_rotationAngle + 270) % 360;
            _presenter?.RenderPage(_currentPageIndex);
            ShowToast($"已旋转 {_rotationAngle}°");
        }

        private void MenuItemRotateRight_Click(object? sender, EventArgs e)
        {
            _rotationAngle = (_rotationAngle + 90) % 360;
            _presenter?.RenderPage(_currentPageIndex);
            ShowToast($"已旋转 {_rotationAngle}°");
        }

        private void MenuItemResetRotation_Click(object? sender, EventArgs e)
        {
            _rotationAngle = 0;
            _presenter?.RenderPage(_currentPageIndex);
            ShowToast("已重置旋转");
        }

        private Bitmap RotateBitmap(Bitmap bitmap, int angle)
        {
            if (angle == 0) return bitmap;

            var rotated = new Bitmap(bitmap.Height, bitmap.Width);
            rotated.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (var g = Graphics.FromImage(rotated))
            {
                g.TranslateTransform(rotated.Width / 2f, rotated.Height / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-bitmap.Width / 2f, -bitmap.Height / 2f);
                g.DrawImage(bitmap, Point.Empty);
            }

            bitmap.Dispose();
            return rotated;
        }

        private void TrackBarZoom_Scroll(object? sender, EventArgs e)
        {
            _navigationManager?.Zoom(_trackBarZoom.Value);
            _zoomLevel = _trackBarZoom.Value;
            UpdateStatusBar();
        }

        private void ButtonRemoveHighlight_Click(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is PdfHighlight highlight)
            {
                _highlightManager?.RemoveHighlight(highlight);
            }
            else if (_listBoxHighlights?.SelectedItem is PdfAnnotationItem annotation)
            {
                _presenter?.RemoveAnnotation(annotation);
                RefreshHighlightList();
                _navigationManager?.LoadAnnotationsForCurrentPage();
                _pictureBoxPdf.Invalidate();
            }
        }

        private void ButtonEditHighlight_Click(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is PdfAnnotationItem annotation && annotation.Type == AnnotationType.Text)
            {
                EditTextAnnotation(annotation);
            }
            else
            {
                ShowMessage("请选择要编辑的文字标注", InfoTitle);
            }
        }

        private void EditTextAnnotation(PdfAnnotationItem annotation)
        {
            Color currentColor = Color.FromArgb(annotation.ColorArgb);

            using var dialog = new TextAnnotationDialog(
                "编辑文字注解",
                annotation.Text,
                currentColor,
                annotation.FontSize);

            var result = dialog.ShowDialog(this);

            if (result.Confirmed)
            {
                try
                {
                    _presenter?.UpdateTextAnnotation(
                        annotation,
                        result.Text,
                        result.SelectedColor.ToArgb(),
                        result.FontSize,
                        "Microsoft YaHei UI");

                    RefreshHighlightList();
                    _navigationManager?.LoadAnnotationsForCurrentPage();
                    _pictureBoxPdf.Invalidate();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error editing text annotation");
                }
            }
        }

        private void ButtonExportHighlights_Click(object? sender, EventArgs e)
        {
            _ = _presenter?.ExportHighlightsToExcelAsync();
        }

        private void ButtonBatchRemoveHighlight_Click(object? sender, EventArgs e)
        {
            _highlightManager?.BatchRemoveHighlights();
        }

        private void ButtonUndoHighlight_Click(object? sender, EventArgs e)
        {
            _highlightManager?.UndoHighlight();
        }

        private void RefreshBookmarkList()
        {
            _bookmarkManager?.RefreshBookmarkList();
        }

        private void RefreshHighlightList()
        {
            _highlightManager?.RefreshHighlightList();
        }

        private void LoadHighlightsForCurrentPage()
        {
            _highlightManager?.LoadHighlightsForCurrentPage();
        }

        private void UpdateHighlightLayer()
        {
            _highlightManager?.UpdateHighlightLayer();
        }

        private void CleanupHighlightLayer()
        {
            _highlightManager?.CleanupHighlightLayer();
        }

        private void CleanupAnnotationBitmap()
        {
            _navigationManager?.CleanupAnnotationBitmap();
        }

        /// <summary>
        /// 设置当前PDF文件路径，并刷新相关UI状态
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        public void SetCurrentPdfPath(string pdfPath)
        {
            CleanupHighlightLayer();
            ClearThumbnails();
            _currentPdfPath = pdfPath;
            _bookmarkManager?.ClearCache();

            RefreshBookmarkList();
            RefreshHighlightList();

            LoadHighlightsForCurrentPage();
            Text = $"{AppTitle} - {Path.GetFileName(pdfPath)}";
        }

        /// <summary>
        /// 设置Presenter并建立视图关联
        /// </summary>
        /// <param name="presenter">PDF Presenter实例</param>
        /// <exception cref="ArgumentNullException">当presenter为null时抛出</exception>
        public void SetPresenter(PdfPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.SetView(this);
        }

        public void SetFileList(IEnumerable<string> files)
        {
            _allFiles = files.ToList();
            _textBoxFilter.Clear();
            UpdateFileListDisplay();
        }

        private void UpdateFileListDisplay()
        {
            _treeViewFiles.Nodes.Clear();
            string filter = _textBoxFilter?.Text?.Trim() ?? string.Empty;

            var filteredFiles = string.IsNullOrEmpty(filter)
                ? _allFiles
                : _allFiles.Where(f => Path.GetFileName(f).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

            if (filteredFiles.Any())
            {
                string commonRoot = FindCommonRootDirectory(filteredFiles);
                BuildTreeViewHierarchy(_treeViewFiles.Nodes, filteredFiles, commonRoot);
            }

            if (!string.IsNullOrEmpty(_currentPdfPath))
            {
                var currentNode = FindNodeByTag(_treeViewFiles.Nodes, _currentPdfPath);
                if (currentNode != null)
                {
                    _treeViewFiles.SelectedNode = currentNode;
                    currentNode.EnsureVisible();
                }
            }
        }

        private static string FindCommonRootDirectory(IEnumerable<string> files)
        {
            var paths = files.Select(f => Path.GetDirectoryName(f) ?? string.Empty).Distinct().ToList();
            if (paths.Count == 0) return string.Empty;
            if (paths.Count == 1) return paths[0];

            string commonRoot = paths[0];
            foreach (var path in paths.Skip(1))
            {
                while (!path.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                {
                    commonRoot = Path.GetDirectoryName(commonRoot) ?? string.Empty;
                    if (string.IsNullOrEmpty(commonRoot)) break;
                }
            }
            return commonRoot;
        }

        private static void BuildTreeViewHierarchy(TreeNodeCollection nodes, IEnumerable<string> files, string commonRoot)
        {
            var folderNodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                string fileName = Path.GetFileName(file);
                string dirPath = Path.GetDirectoryName(file) ?? string.Empty;

                TreeNodeCollection targetNodes = nodes;

                if (!string.IsNullOrEmpty(commonRoot) &&
                    dirPath.StartsWith(commonRoot, StringComparison.OrdinalIgnoreCase))
                {
                    string relativeDir = dirPath.Substring(commonRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (!string.IsNullOrEmpty(relativeDir))
                    {
                        string[] parts = relativeDir.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                        string currentPath = commonRoot;

                        foreach (var part in parts)
                        {
                            currentPath = Path.Combine(currentPath, part);

                            if (!folderNodes.TryGetValue(currentPath, out var folderNode))
                            {
                                folderNode = targetNodes.Add(part);
                                folderNode.ImageKey = "Folder";
                                folderNode.SelectedImageKey = "Folder";
                                folderNodes[currentPath] = folderNode;
                            }
                            targetNodes = folderNode.Nodes;
                        }
                    }
                }

                var fileNode = targetNodes.Add(fileName);
                fileNode.Tag = file;
            }
        }

        private static TreeNode? FindNodeByTag(TreeNodeCollection nodes, string tag)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Tag is string s && s == tag)
                    return node;

                var found = FindNodeByTag(node.Nodes, tag);
                if (found != null)
                    return found;
            }
            return null;
        }

        public void SetImageList(IEnumerable<string> imageFiles)
        {
            _allFiles = imageFiles.ToList();
            _textBoxFilter.Clear();
            UpdateFileListDisplay();
        }

        public void SetPageCount(int count)
        {
            _labelPageCount.Text = $"/ {count}";
            _progressBarPage.Maximum = count;
            UpdateStatusBar();
        }

        public void SetCurrentPageIndex(int pageIndex)
        {
            bool isForward = pageIndex > _currentPageIndex;
            _currentPageIndex = pageIndex;
            _textBoxPage.Text = (pageIndex + 1).ToString();
            _progressBarPage.Value = pageIndex + 1;
            StartPageTransition(isForward);
            LoadHighlightsForCurrentPage();
            UpdateStatusBar();
        }

        public void SetPageText(int pageIndex, string text)
        {
        }

        /// <summary>
        /// 显示PDF页面图像
        /// </summary>
        /// <param name="bmp">要显示的Bitmap图像</param>
        public void DisplayImage(Bitmap bmp)
        {
            try
            {
                CleanupAnnotationBitmap();
                CleanupHighlightLayer();

                Bitmap imageToDisplay = bmp;
                if (_nightModeManager?.IsNightMode ?? false)
                {
                    imageToDisplay = new Bitmap(_nightModeManager.InvertImage(bmp));
                }

                if (_rotationAngle != 0)
                {
                    imageToDisplay = RotateBitmap(imageToDisplay, _rotationAngle);
                }

                SafeDisposeImage(ref _currentPageImage);
                _currentPageImage = imageToDisplay;

                _pictureBoxPdf.Image = null;
                _pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
                _navigationManager?.LoadAnnotationsForCurrentPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisplayImage");
                SafeDisposeImage(ref _currentPageImage);
                _currentPageImage = bmp;
                _pictureBoxPdf.Image = null;
                _pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
                _navigationManager?.LoadAnnotationsForCurrentPage();
            }
        }

        /// <summary>
        /// 设置双页模式下第二页的图像
        /// </summary>
        /// <param name="bmp">第二页的Bitmap图像，null则清除</param>
        public void SetSecondPageImage(Bitmap? bmp)
        {
            try
            {
                SafeReplaceImage(ref _secondPageImage, bmp);
                _pictureBoxPdf.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetSecondPageImage");
            }
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, WarningTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.Visible = isLoading;
                _loadingIndicator.IsLoading = isLoading;
                if (isLoading)
                {
                    _loadingIndicator.BringToFront();
                }
            }
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message, InfoTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowLoading(string message)
        {
            SetLoadingState(true);
        }

        public void HideLoading()
        {
            SetLoadingState(false);
        }

        public bool ShowConfirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }

        public string? ShowSaveFileDialog(string defaultFileName, string filter)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.FileName = defaultFileName;
                saveFileDialog.Filter = filter;
                saveFileDialog.Title = "保存文件";

                if (saveFileDialog.ShowDialog(this) == DialogResult.OK)
                {
                    return saveFileDialog.FileName;
                }
                return null;
            }
        }

        public void NightMode()
        {
            _nightModeManager?.ToggleNightMode();
        }

        public void ClearThumbnails()
        {
            _thumbnailDirectoryHeaders.Clear();
            if (_flowLayoutPanelThumbnails != null)
            {
                foreach (Control control in _flowLayoutPanelThumbnails.Controls)
                {
                    control.Dispose();
                }
                _flowLayoutPanelThumbnails.Controls.Clear();
            }
        }

        public void AddThumbnail(int pageIndex, Image thumbnail)
        {
            // PDF 模式：directoryPath 为空，保持原有行为不分组
            AddThumbnail(pageIndex, thumbnail, string.Empty);
        }

        public void AddThumbnail(int pageIndex, Image thumbnail, string directoryPath)
        {
            if (_flowLayoutPanelThumbnails == null || thumbnail == null) return;

            // 图片模式下，按目录分组：每个新目录前插入一个分组标题
            if (!string.IsNullOrEmpty(directoryPath))
            {
                if (!_thumbnailDirectoryHeaders.TryGetValue(directoryPath, out _))
                {
                    var header = CreateThumbnailDirectoryHeader(directoryPath);
                    _flowLayoutPanelThumbnails.Controls.Add(header);
                    _thumbnailDirectoryHeaders[directoryPath] = header;
                }
            }

            var panel = new Panel();
            panel.Size = new Size(110, 150);
            panel.Margin = new Padding(6);
            panel.BorderStyle = BorderStyle.None;
            panel.Tag = pageIndex;
            panel.BackColor = Color.White;

            _nightModeManager?.UpdateThumbnailPanelColor(panel);

            Image displayImage = thumbnail;
            if (_nightModeManager?.IsNightMode ?? false)
            {
                displayImage = _nightModeManager.InvertImage(thumbnail);
            }

            var pictureBox = new PictureBox();
            pictureBox.Image = displayImage;
            pictureBox.Size = new Size(100, 125);
            pictureBox.Location = new Point(5, 5);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Tag = pageIndex;
            pictureBox.Click += Thumbnail_Click;
            pictureBox.DoubleClick += Thumbnail_Click;

            var label = new Label();
            label.Text = (pageIndex + 1).ToString();
            label.Location = new Point(5, 130);
            label.Size = new Size(100, 18);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Microsoft YaHei UI", 9F);
            label.ForeColor = Color.FromArgb(102, 102, 102);
            _nightModeManager?.UpdateThumbnailLabelColor(label);
            label.Tag = pageIndex;
            label.Click += Thumbnail_Click;
            label.DoubleClick += Thumbnail_Click;

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(label);
            panel.Click += Thumbnail_Click;
            panel.DoubleClick += Thumbnail_Click;

            _flowLayoutPanelThumbnails.Controls.Add(panel);
        }

        /// <summary>
        /// 创建图片模式缩略图分组的目录标题。
        /// 宽度占满一行，确保下一个缩略图自动换行到新行。
        /// </summary>
        private Label CreateThumbnailDirectoryHeader(string directoryPath)
        {
            var header = new Label();
            header.Text = "📁 " + Path.GetFileName(directoryPath);
            header.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            header.TextAlign = ContentAlignment.MiddleLeft;
            header.Padding = new Padding(8, 4, 8, 4);
            header.Margin = new Padding(6, 6, 6, 2);
            header.Height = 28;
            header.AutoSize = false;
            // 标记为目录分组标题，便于夜间模式管理器识别
            header.Tag = "DirectoryHeader";

            bool isNightMode = _nightModeManager?.IsNightMode ?? _isNightMode;
            header.BackColor = isNightMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(230, 230, 230);
            header.ForeColor = isNightMode ? Color.White : Color.FromArgb(64, 64, 64);

            // 宽度设为面板内容区宽度，强制下一个缩略图换行
            int panelWidth = _flowLayoutPanelThumbnails?.ClientSize.Width ?? 0;
            if (panelWidth <= 0) panelWidth = 280;
            header.Width = Math.Max(200, panelWidth - 12);

            return header;
        }

        public void HighlightThumbnail(int pageIndex)
        {
            if (_flowLayoutPanelThumbnails == null) return;

            foreach (Control control in _flowLayoutPanelThumbnails.Controls)
            {
                if (control is Panel panel)
                {
                    if (panel.Tag is int idx && idx == pageIndex)
                    {
                        bool isNightMode = _nightModeManager?.IsNightMode ?? _isNightMode;
                        panel.BackColor = isNightMode ? Color.FromArgb(60, 80, 120) : Color.FromArgb(230, 244, 255);
                        panel.BorderStyle = BorderStyle.None;
                        panel.Padding = new Padding(2);
                        panel.Paint -= ThumbnailPanel_Paint;
                        panel.Paint += ThumbnailPanel_Paint;
                        panel.Invalidate();
                        panel.BringToFront();

                        try
                        {
                            if (_flowLayoutPanelThumbnails.IsHandleCreated)
                            {
                                _flowLayoutPanelThumbnails.BeginInvoke(() =>
                                {
                                    try
                                    {
                                        _flowLayoutPanelThumbnails.ScrollControlIntoView(panel);
                                    }
                                    catch { }
                                });
                            }
                        }
                        catch { }
                    }
                    else
                    {
                        panel.Padding = new Padding(0);
                        panel.Paint -= ThumbnailPanel_Paint;
                        _nightModeManager?.UpdateThumbnailPanelColor(panel);
                        panel.BorderStyle = BorderStyle.None;
                        panel.Invalidate();
                    }
                }
            }
        }

        private void ThumbnailPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is Panel panel)
            {
                bool isNightMode = _nightModeManager?.IsNightMode ?? _isNightMode;
                Color borderColor = isNightMode ? Color.FromArgb(100, 150, 220) : Color.FromArgb(64, 150, 255);
                using var pen = new Pen(borderColor, 2);
                e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
            }
        }

        private void Thumbnail_Click(object? sender, EventArgs e)
        {
            if (sender is Control c && c.Tag is int idx)
            {
                NavigateToPage(idx);
            }
        }

        private void NavigateToPage(int pageIndex)
        {
            _presenter?.RenderPage(pageIndex);
        }

        public string GetSelectedFile()
        {
            return _treeViewFiles.SelectedNode?.Tag as string ?? string.Empty;
        }

        public string GetPageText()
        {
            return _textBoxPage.Text;
        }

        public string GetTranslationText()
        {
            return _textBoxTranslation.Text;
        }

        public string GetOriginalText()
        {
            return _textBoxOriginal.Text;
        }

        public void SetTranslationText(string text)
        {
            _textBoxTranslation.Text = text;
        }

        public void SetOriginalText(string text)
        {
            _textBoxOriginal.Text = text;
        }

        public void SetOcrResultText(string text)
        {
            _textBoxOriginal.Text = text;
        }

        public Image? GetCurrentImage()
        {
            return _currentPageImage;
        }

        public Rectangle? GetSelectionRect()
        {
            return _lastSelectionRect;
        }

        public Rectangle GetDisplayRect()
        {
            return _pictureBoxPdf.ClientRectangle;
        }

        /// <summary>
        /// 获取图片在PictureBox中的显示矩形（考虑缩放和偏移）
        /// </summary>
        /// <returns>图片显示区域的矩形坐标</returns>
        public Rectangle GetImageDisplayRect()
        {
            try
            {
                if (_currentPageImage == null)
                    return _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;

                var image = _currentPageImage;
                int imgWidth, imgHeight;
                try
                {
                    imgWidth = image.Width;
                    imgHeight = image.Height;
                }
                catch (ObjectDisposedException)
                {
                    _logger.LogWarning("Image disposed in GetImageDisplayRect");
                    return _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
                }

                var controlWidth = _pictureBoxPdf.ClientSize.Width;
                var controlHeight = _pictureBoxPdf.ClientSize.Height;

                float imageAspect = (float)imgWidth / imgHeight;
                float controlAspect = (float)controlWidth / controlHeight;

                int displayWidth, displayHeight, displayX, displayY;

                if (imageAspect > controlAspect)
                {
                    displayWidth = controlWidth;
                    displayHeight = (int)(controlWidth / imageAspect);
                }
                else
                {
                    displayHeight = controlHeight;
                    displayWidth = (int)(controlHeight * imageAspect);
                }

                float scale = CurrentZoomLevel / 100.0f;
                displayWidth = (int)(displayWidth * scale);
                displayHeight = (int)(displayHeight * scale);

                var offset = CurrentImageOffset;
                displayX = (controlWidth - displayWidth) / 2 + offset.X;
                displayY = (controlHeight - displayHeight) / 2 + offset.Y;

                return new Rectangle(displayX, displayY, displayWidth, displayHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetImageDisplayRect");
                return _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
            }
        }

        public (int pageIndex, Rectangle pageRect, Bitmap? pageImage) GetPageAtPoint(Point clientPoint)
        {
            if (_isDualPage && _currentPageImage != null)
            {
                var (leftRect, rightRect) = GetDualPageRects();

                if (clientPoint.X >= leftRect.X && clientPoint.X <= leftRect.Right &&
                    clientPoint.Y >= leftRect.Y && clientPoint.Y <= leftRect.Bottom)
                {
                    return (_currentPageIndex, leftRect, _currentPageImage);
                }

                if (_secondPageImage != null &&
                    clientPoint.X >= rightRect.X && clientPoint.X <= rightRect.Right &&
                    clientPoint.Y >= rightRect.Y && clientPoint.Y <= rightRect.Bottom)
                {
                    return (_currentPageIndex + 1, rightRect, _secondPageImage);
                }
            }

            var imgRect = GetImageDisplayRect();
            return (_currentPageIndex, imgRect, _currentPageImage);
        }

        public void ShowOcrOverlay(Bitmap? image)
        {
            if (_ocrPanel != null && _ocrPictureBox != null && _ocrCloseButton != null)
            {
                if (_ocrPictureBox.Image != null)
                {
                    _ocrPictureBox.Image.Dispose();
                }
                _ocrPictureBox.Image = image;
                _ocrPanel.Visible = image != null;
                if (image != null)
                {
                    int panelWidth = image.Width + 8;
                    int panelHeight = image.Height + 32;

                    if (panelWidth < 50)
                    {
                        panelWidth = 50;
                    }

                    int maxWidth = _panelPdf.ClientSize.Width - 100;
                    int maxHeight = _panelPdf.ClientSize.Height - 100;

                    if (panelWidth > maxWidth)
                    {
                        double scale = (double)maxWidth / panelWidth;
                        panelWidth = maxWidth;
                        panelHeight = (int)(panelHeight * scale);
                    }

                    if (panelHeight > maxHeight)
                    {
                        double scale = (double)maxHeight / panelHeight;
                        panelHeight = maxHeight;
                        panelWidth = (int)(panelWidth * scale);
                    }

                    _ocrPanel.Size = new Size(panelWidth, panelHeight);
                    _ocrCloseButton.Location = new Point(panelWidth - 28, 2);
                    _ocrPictureBox.Location = new Point(2, 26);
                    _ocrPictureBox.Size = new Size(panelWidth - 4, panelHeight - 28);
                    _ocrPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
                    _ocrPanel.BringToFront();
                }
            }
        }

        public void HideOcrOverlay()
        {
            if (_ocrPanel != null && _ocrPictureBox != null)
            {
                if (_ocrPictureBox.Image != null)
                {
                    _ocrPictureBox.Image.Dispose();
                    _ocrPictureBox.Image = null;
                }
                _ocrPanel.Visible = false;
            }
        }

        public void SetImageMode(bool isImageMode)
        {
            _isImageMode = isImageMode;
        }

        public bool GetImageMode()
        {
            return _isImageMode;
        }

        public void RaiseAddToEditor(string text, string language)
        {
            AddToEditor?.Invoke(this, new AddToEditorEventArgs { Text = text, Language = language });
        }

        public void RaiseAiQuestionAsked()
        {
            if (_aiPanelPopupService != null)
            {
                var context = _textBoxOriginal?.Text ?? string.Empty;
                _aiPanelPopupService.ShowAIAbilityPanel(this, context, null, context);
            }
            else
            {
                MessageBox.Show("AI面板服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public int GetZoomLevel()
        {
            return _navigationManager?.ZoomLevel ?? _zoomLevel;
        }

        public void SetZoomLevel(int level)
        {
            int clampedLevel = Math.Max(50, Math.Min(200, level));
            _navigationManager?.Zoom(clampedLevel);
            _trackBarZoom.Value = clampedLevel;
            _zoomLevel = clampedLevel;
            UpdateStatusBar();
        }

        public void ZoomIn()
        {
            int currentLevel = GetZoomLevel();
            int newLevel = Math.Min(200, currentLevel + 10);
            if (newLevel != currentLevel)
            {
                SetZoomLevel(newLevel);
            }
        }

        public void ZoomOut()
        {
            int currentLevel = GetZoomLevel();
            int newLevel = Math.Max(50, currentLevel - 10);
            if (newLevel != currentLevel)
            {
                SetZoomLevel(newLevel);
            }
        }


        public event EventHandler? FileSelected;
        public event EventHandler? PageChanged;
        public event EventHandler? OcrSelectionComplete;
        public event EventHandler? AiQuestionAsked;
        public event EventHandler? AddToLearningList;
        public event EventHandler<AddToEditorEventArgs>? AddToEditor;
        public event EventHandler? SpeakTranslation;
        public event EventHandler<string>? SpeakText;
        public event EventHandler<string>? AskAiWithText;
        public event EventHandler? SelectOcrClicked;
        public event EventHandler? TranslateClicked;
        public event EventHandler? ToggleNightMode;
        public event EventHandler? ToggleTranslation;
        public event EventHandler? SpeakAnswer;
        public event EventHandler? SpeakOriginal;
        public event EventHandler? LanguageChanged;

        private void ButtonZoomIn_Click(object? sender, EventArgs e)
        {
            if (_trackBarZoom.Value < _trackBarZoom.Maximum)
            {
                _trackBarZoom.Value = Math.Min(_trackBarZoom.Value + 10, _trackBarZoom.Maximum);
                TrackBarZoom_Scroll(sender, e);
            }
        }

        private void ButtonZoomOut_Click(object? sender, EventArgs e)
        {
            if (_trackBarZoom.Value > _trackBarZoom.Minimum)
            {
                _trackBarZoom.Value = Math.Max(_trackBarZoom.Value - 10, _trackBarZoom.Minimum);
                TrackBarZoom_Scroll(sender, e);
            }
        }

        private void ButtonHighlightMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Highlight);
        }

        private void ButtonRectangleMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Rectangle);
        }

        private void ButtonPenMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Pen);
        }

        private void ButtonStrikethroughMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Strikethrough);
        }

        private void ButtonTextMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Text);
        }

        private void SetAnnotationColor(Color color)
        {
            _navigationManager?.SetPenColor(color);
            UpdateAnnotationColorSelection(color);
        }

        private void UpdateAnnotationColorSelection(Color color)
        {
            if (_buttonColorBlue != null) _buttonColorBlue.FlatAppearance.BorderSize = color == Color.RoyalBlue ? 2 : 0;
            if (_buttonColorGreen != null) _buttonColorGreen.FlatAppearance.BorderSize = color == Color.LimeGreen ? 2 : 0;
            if (_buttonColorOrange != null) _buttonColorOrange.FlatAppearance.BorderSize = color == Color.Orange ? 2 : 0;
            if (_buttonColorRed != null) _buttonColorRed.FlatAppearance.BorderSize = color == Color.Red ? 2 : 0;
            if (_buttonColorBlack != null) _buttonColorBlack.FlatAppearance.BorderSize = color == Color.Black ? 2 : 0;
            if (_buttonColorWhite != null) _buttonColorWhite.FlatAppearance.BorderSize = color == Color.White ? 2 : 0;
        }

        private void SetAnnotationThickness(int level)
        {
            float[] widths = { 2f, 4f, 6f };
            float width = widths[Math.Clamp(level - 1, 0, widths.Length - 1)];
            _navigationManager?.SetPenWidth(width);
            UpdateAnnotationThicknessSelection(level);
        }

        private void UpdateAnnotationThicknessSelection(int level)
        {
            if (_buttonThickness1 != null)
            {
                _buttonThickness1.FlatAppearance.BorderSize = level == 1 ? 2 : 0;
                _buttonThickness1.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            }
            if (_buttonThickness2 != null)
            {
                _buttonThickness2.FlatAppearance.BorderSize = level == 2 ? 2 : 0;
                _buttonThickness2.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            }
            if (_buttonThickness3 != null)
            {
                _buttonThickness3.FlatAppearance.BorderSize = level == 3 ? 2 : 0;
                _buttonThickness3.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            }
        }

        private void ShowAnnotationOptions(bool show)
        {
            if (_panelAnnotationOptions != null)
                _panelAnnotationOptions.Visible = show;
        }

        private void ButtonEllipseMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Ellipse);
        }

        private void ButtonArrowMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Arrow);
        }

        private void ButtonMosaicMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Mosaic);
        }

        private void ButtonUndoAnnotation_Click(object? sender, EventArgs e)
        {
            _navigationManager?.UndoStroke();
            RefreshHighlightList();
        }

        private void ButtonClearAllAnnotations_Click(object? sender, EventArgs e)
        {
            if (_presenter == null || string.IsNullOrEmpty(_currentPdfPath)) return;

            if (!ShowConfirm("确定要清除当前PDF的所有涂改痕迹吗？此操作不可撤销。", "清屏确认"))
                return;

            _presenter.ClearAllAnnotations(_currentPdfPath);
            _ = _presenter.RenderAndDisplayCurrentPageAsync();
            UpdateStatusBar();
        }

        private void ButtonSelectMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Select);
        }

        private void SetAnnotationToolMode(AnnotationToolMode mode)
        {
            _navigationManager?.SetToolMode(mode);

            bool showOptions = mode == AnnotationToolMode.Rectangle ||
                             mode == AnnotationToolMode.Ellipse ||
                             mode == AnnotationToolMode.Arrow ||
                             mode == AnnotationToolMode.Pen ||
                             mode == AnnotationToolMode.Mosaic;
            ShowAnnotationOptions(showOptions);

            UpdateToolButtonState(_buttonSelectMode, mode == AnnotationToolMode.Select);
            UpdateToolButtonState(_buttonHighlightMode, mode == AnnotationToolMode.Highlight);
            UpdateToolButtonState(_buttonRectangleMode, mode == AnnotationToolMode.Rectangle);
            UpdateToolButtonState(_buttonEllipseMode, mode == AnnotationToolMode.Ellipse);
            UpdateToolButtonState(_buttonArrowMode, mode == AnnotationToolMode.Arrow);
            UpdateToolButtonState(_buttonPenMode, mode == AnnotationToolMode.Pen);
            UpdateToolButtonState(_buttonMosaicMode, mode == AnnotationToolMode.Mosaic);
            UpdateToolButtonState(_buttonTextMode, mode == AnnotationToolMode.Text);

            if (_highlightManager != null)
            {
                _highlightManager.IsHighlightMode = mode == AnnotationToolMode.Highlight;
            }
            _isHighlightMode = mode == AnnotationToolMode.Highlight;

            UpdateStatusBar();
        }

        private void UpdateToolButtonState(Button? button, bool isActive)
        {
            if (button == null) return;
            button.BackColor = isActive ? Color.FromArgb(230, 244, 255) : Color.Transparent;
            button.FlatAppearance.BorderColor = isActive ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
        }

        private void ConfigureButtonAppearance(Button button, bool isToolbarButton, Color backColor, int borderSize, Color borderColor)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = borderSize;
            button.FlatAppearance.BorderColor = borderColor;
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(200, 220, 255);
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(230, 244, 255);
            button.BackColor = backColor;
            button.UseVisualStyleBackColor = false;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular);

            if (isToolbarButton)
            {
                button.Size = new Size(36, 36);
                button.TextAlign = ContentAlignment.MiddleCenter;
            }
        }

        private void ApplyButtonStyle(Button button, bool isToolbarButton = false)
        {
            ConfigureButtonAppearance(button, isToolbarButton, Color.Transparent, 0, Color.FromArgb(217, 217, 217));
        }

        private void ApplyRoundedStyle(Button button, int radius = 4)
        {
            ConfigureButtonAppearance(button, false, Color.White, 1, Color.FromArgb(217, 217, 217));

            button.Paint += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(0, 0, radius, radius, 180, 90);
                    path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
                    path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
                    path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path);
                }
            };
        }

        private void ShowTextAnnotationDialog(Point location)
        {
            using var dialog = new TextAnnotationDialog("添加文字注解");
            var result = dialog.ShowDialog(this);

            if (result.Confirmed && _currentPageImage != null && !string.IsNullOrEmpty(_currentPdfPath))
            {
                try
                {
                    var imgRect = GetImageDisplayRect();
                    float relX = (float)(location.X - imgRect.X) / imgRect.Width;
                    float relY = (float)(location.Y - imgRect.Y) / imgRect.Height;

                    relX = Math.Clamp(relX, 0, 1);
                    relY = Math.Clamp(relY, 0, 1);

                    _presenter?.AddAnnotationText(
                        relX, relY, result.Text,
                        result.SelectedColor.ToArgb(),
                        result.FontSize,
                        "Microsoft YaHei UI",
                        _currentPageImage.Width, _currentPageImage.Height);

                    _navigationManager?.LoadAnnotationsForCurrentPage();
                    RefreshHighlightList();
                    _pictureBoxPdf.Invalidate();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding text annotation");
                }
            }
        }

        private void ButtonDualPage_Click(object? sender, EventArgs e)
        {
            _isDualPage = !_isDualPage;
            if (_buttonDualPage != null)
            {
                _buttonDualPage.BackColor = _isDualPage ? Color.FromArgb(230, 244, 255) : Color.White;
                _buttonDualPage.FlatAppearance.BorderColor = _isDualPage ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            UpdateStatusBar();
            if (_isDualPage)
            {
                _presenter?.RenderPage(_currentPageIndex);
            }
            _pictureBoxPdf.Invalidate();
        }

        private void ButtonFullscreen_Click(object? sender, EventArgs e)
        {
            ToggleFullscreen();
        }

        private void ButtonPrev_Click(object? sender, EventArgs e)
        {
            if (_currentPageIndex > 0)
            {
                _presenter?.RenderPage(_currentPageIndex - 1);
            }
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            _presenter?.RenderPage(_currentPageIndex + 1);
        }

        private void TextBoxPage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (int.TryParse(_textBoxPage.Text, out int page))
                {
                    _presenter?.RenderPage(page - 1);
                }
            }
        }

        private void ButtonNightMode_Click(object? sender, EventArgs e)
        {
            _nightModeManager?.ToggleNightMode();
            ToggleNightMode?.Invoke(this, EventArgs.Empty);
            UpdateStatusBar();
            UpdateToolbarButtonColorsForNightMode();
        }

        private void UpdateToolbarButtonColorsForNightMode()
        {
            bool isNightMode = _nightModeManager?.IsNightMode ?? false;
            var toolbarButtons = new[]
            {
                _buttonPrev, _buttonNext,
                _buttonZoomOut, _buttonZoomIn, _buttonResetView,
                _buttonRotate, _buttonLockView, _buttonNightMode,
                _buttonDualPage, _buttonFullscreen,
                _buttonSelectMode, _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenMode, _buttonMosaicMode, _buttonTextMode,
                _buttonUndoAnnotation, _buttonClearAllAnnotations
            };

            foreach (var button in toolbarButtons)
            {
                if (button != null)
                {
                    button.BackColor = isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
                }
            }
        }

        private void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            RaiseAiQuestionAsked();
        }

        private void ButtonTranslationToggle_Click(object? sender, EventArgs e)
        {
            _isTranslationEnabled = !_isTranslationEnabled;
            if (_buttonTranslationToggle != null)
            {
                _buttonTranslationToggle.BackColor = _isTranslationEnabled ? Color.FromArgb(230, 244, 255) : Color.White;
                _buttonTranslationToggle.FlatAppearance.BorderColor = _isTranslationEnabled ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            TranslateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OcrCloseButton_Click(object? sender, EventArgs e)
        {
            HideOcrOverlay();
        }

        private void PanelNavigation_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isNavPanelDragging = true;
                _navPanelStartPoint = new Point(e.X, e.Y);
            }
        }

        private void PanelNavigation_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isNavPanelDragging && _panelNavigation != null)
            {
                int x = _panelNavigation.Left + (e.X - _navPanelStartPoint.X);
                int y = _panelNavigation.Top + (e.Y - _navPanelStartPoint.Y);
                _panelNavigation.Location = new Point(
                    Math.Max(0, Math.Min(x, _panelPdf.Width - _panelNavigation.Width)),
                    Math.Max(0, Math.Min(y, _panelPdf.Height - _panelNavigation.Height))
                );
            }
        }

        private void PanelNavigation_MouseUp(object? sender, MouseEventArgs e)
        {
            _isNavPanelDragging = false;
        }

        private void PictureBoxPdf_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                ClearBackground(e.Graphics);
                DrawPageImage(e.Graphics);
                DrawSelectionRect(e.Graphics);

                if (!string.IsNullOrEmpty(_currentPdfPath) && _currentPageImage != null)
                {
                    DrawHighlightsFromLayer(e.Graphics);
                }

                DrawAnnotations(e.Graphics);
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Object disposed during Paint event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_Paint");
            }
        }

        private void ClearBackground(Graphics g)
        {
            bool isNight = _nightModeManager?.IsNightMode ?? _isNightMode;
            g.Clear(isNight ? Color.FromArgb(20, 20, 20) : Color.White);
        }

        private void DrawPageImage(Graphics g)
        {
            if (_currentPageImage == null) return;

            if (_isDualPage)
            {
                DrawDualPageLayout(g);
            }
            else
            {
                g.DrawImage(_currentPageImage, GetImageDisplayRect());
            }
        }

        private void DrawSelectionRect(Graphics g)
        {
            if (_navigationManager == null || !_navigationManager.LastSelectionRect.HasValue) return;

            var rect = _navigationManager.LastSelectionRect.Value;
            var isHighlightMode = _navigationManager.IsHighlightModeCallback?.Invoke() ?? true;

            if (isHighlightMode)
            {
                var color = HighlightService.GetHighlightColor(_highlightManager?.CurrentHighlightColor ?? _currentHighlightColor);
                using var brush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                using var pen = new Pen(Color.FromArgb(color.A + 50, color.R, color.G, color.B), 2);
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }
            else
            {
                using var brush = new SolidBrush(Color.FromArgb(80, Color.Yellow));
                using var pen = new Pen(Color.Orange, 2);
                g.FillRectangle(brush, rect);
                g.DrawRectangle(pen, rect);
            }
        }

        private void DrawAnnotations(Graphics g)
        {
            if (_navigationManager == null || _currentPageImage == null) return;

            if (_isDualPage)
            {
                var (leftRect, rightRect) = GetDualPageRects();
                _navigationManager.DrawAnnotations(g, leftRect, _currentPageIndex);
                if (_secondPageImage != null)
                {
                    _navigationManager.DrawAnnotations(g, rightRect, _currentPageIndex + 1);
                }
            }
            else
            {
                _navigationManager.DrawAnnotations(g, GetImageDisplayRect());
            }
        }

        private void DrawDualPageLayout(Graphics g)
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                var (leftRect, rightRect) = GetDualPageRects();

                g.DrawImage(_currentPageImage, leftRect);

                if (_secondPageImage != null)
                {
                    g.DrawImage(_secondPageImage, rightRect);
                }
                else
                {
                    using var brush = new SolidBrush(Color.White);
                    g.FillRectangle(brush, rightRect);
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogDebug(ex, "Object disposed during DrawDualPageLayout");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DrawDualPageLayout");
            }
        }

        private void DrawHighlightsFromLayer(Graphics g)
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                if (_highlightManager != null)
                {
                    if (_isDualPage)
                    {
                        var (leftRect, rightRect) = GetDualPageRects();

                        _highlightManager.DrawHighlightsForPage(
                            g,
                            _currentPageIndex,
                            leftRect,
                            _currentPageImage.Width,
                            _currentPageImage.Height);

                        if (_secondPageImage != null)
                        {
                            int secondPageIndex = _currentPageIndex + 1;
                            _highlightManager.DrawHighlightsForPage(
                                g,
                                secondPageIndex,
                                rightRect,
                                _secondPageImage.Width,
                                _secondPageImage.Height);
                        }
                    }
                    else
                    {
                        _highlightManager.DrawHighlightsFromLayer(g);
                    }
                }
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

        private (Rectangle leftRect, Rectangle rightRect) GetDualPageRects()
        {
            int imgWidth = _currentPageImage!.Width;
            int imgHeight = _currentPageImage.Height;

            float fitScale = Math.Min(
                (float)_pictureBoxPdf.ClientSize.Width / (imgWidth * 2),
                (float)_pictureBoxPdf.ClientSize.Height / imgHeight);

            float zoomScale = CurrentZoomLevel / 100.0f;
            float totalScale = fitScale * zoomScale;

            int scaledWidth = (int)(imgWidth * totalScale);
            int scaledHeight = (int)(imgHeight * totalScale);

            int totalWidth = scaledWidth * 2;
            var offset = CurrentImageOffset;
            int offsetX = (_pictureBoxPdf.ClientSize.Width - totalWidth) / 2 + offset.X;
            int offsetY = (_pictureBoxPdf.ClientSize.Height - scaledHeight) / 2 + offset.Y;

            var leftRect = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
            var rightRect = new Rectangle(offsetX + scaledWidth, offsetY, scaledWidth, scaledHeight);

            return (leftRect, rightRect);
        }

        private void EnsureAnnotationBitmap()
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                int imgWidth, imgHeight;
                try
                {
                    imgWidth = _currentPageImage.Width;
                    imgHeight = _currentPageImage.Height;
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

        private void MenuItemCopy_Click(object? sender, EventArgs e)
        {
            if (_currentPageImage != null)
            {
                Clipboard.SetImage(_currentPageImage);
            }
        }

        private void MenuItemSearch_Click(object? sender, EventArgs e)
        {
            ShowSearchDialog();
        }

        private void ShowSearchDialog()
        {
            using var searchForm = new Form();
            searchForm.Text = "查找";
            searchForm.Size = new Size(400, 150);
            searchForm.StartPosition = FormStartPosition.CenterParent;
            searchForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            searchForm.MaximizeBox = false;
            searchForm.MinimizeBox = false;

            var label = new Label();
            label.Text = "查找内容:";
            label.Location = new Point(15, 20);
            label.AutoSize = true;
            searchForm.Controls.Add(label);

            var textBox = new TextBox();
            textBox.Location = new Point(85, 18);
            textBox.Size = new Size(200, 24);
            textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            searchForm.Controls.Add(textBox);

            var buttonFind = new Button();
            buttonFind.Text = "查找";
            buttonFind.Location = new Point(295, 15);
            buttonFind.Size = new Size(80, 30);
            buttonFind.Click += ButtonFind_Click;
            searchForm.Controls.Add(buttonFind);

            var checkCase = new CheckBox();
            checkCase.Text = "区分大小写";
            checkCase.Location = new Point(15, 55);
            checkCase.AutoSize = true;
            searchForm.Controls.Add(checkCase);

            var checkWhole = new CheckBox();
            checkWhole.Text = "全字匹配";
            checkWhole.Location = new Point(120, 55);
            checkWhole.AutoSize = true;
            searchForm.Controls.Add(checkWhole);

            searchForm.ShowDialog();
        }

        private void ButtonFind_Click(object? sender, EventArgs e)
        {
            var button = sender as Button;
            if (button?.Parent is not Form searchForm) return;

            var textBox = searchForm.Controls.OfType<TextBox>().FirstOrDefault();
            if (textBox != null && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                _presenter?.SearchText(textBox.Text);
            }
        }

        private void ButtonThickness1_Click(object? sender, EventArgs e)
        {
            SetAnnotationThickness(1);
        }

        private void ButtonThickness2_Click(object? sender, EventArgs e)
        {
            SetAnnotationThickness(2);
        }

        private void ButtonThickness3_Click(object? sender, EventArgs e)
        {
            SetAnnotationThickness(3);
        }

        private void ButtonColorBlue_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.RoyalBlue);
        }

        private void ButtonColorGreen_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.LimeGreen);
        }

        private void ButtonColorOrange_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Orange);
        }

        private void ButtonColorRed_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Red);
        }

        private void ButtonColorBlack_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Black);
        }

        private void ButtonColorWhite_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.White);
        }

        private void MenuItemHighlight_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Highlight);
        }

        private void MenuItemRectangle_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Rectangle);
        }

        private void MenuItemText_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Text);
        }

        private void MenuItemZoomIn_Click(object? sender, EventArgs e)
        {
            _presenter?.ZoomIn();
        }

        private void MenuItemZoomOut_Click(object? sender, EventArgs e)
        {
            _presenter?.ZoomOut();
        }

        private void MenuItemResetZoom_Click(object? sender, EventArgs e)
        {
            ResetZoom();
        }

        private void MenuItemExport_Click(object? sender, EventArgs e)
        {
            if (_currentPageImage != null)
            {
                using var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "PNG图片|*.png|JPEG图片|*.jpg|BMP图片|*.bmp";
                saveDialog.DefaultExt = "png";
                saveDialog.Title = "导出当前页";
                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        _currentPageImage.Save(saveDialog.FileName);
                        ShowMessage("导出成功");
                    }
                    catch (IOException ex)
                    {
                        _logger.LogError(ex, "IO error exporting page");
                        ShowError("导出失败: 文件访问错误");
                    }
                    catch (OutOfMemoryException ex)
                    {
                        _logger.LogError(ex, "Out of memory exporting page");
                        ShowError("导出失败: 内存不足");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error exporting page");
                        ShowError("导出失败: " + ex.Message);
                    }
                }
            }
        }

        private void PictureBoxPdf_MouseWheel(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Delta != 0)
                {
                    _navigationManager?.ZoomByMouseWheel(e.Delta, (ModifierKeys & Keys.Control) == Keys.Control);
                    _zoomLevel = _navigationManager?.ZoomLevel ?? _zoomLevel;
                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_MouseWheel");
            }
        }

        public void ResetZoom()
        {
            _navigationManager?.ResetZoom();
            UpdateStatusBar();
        }

        private void SetZoom(int level)
        {
            _navigationManager?.Zoom(level);
            _trackBarZoom.Value = level;
            _labelZoom.Text = $"{level}%";
            UpdateStatusBar();
        }

        private void FitToWidth()
        {
            if (_currentPageImage != null)
            {
                float scale = (float)_pictureBoxPdf.ClientSize.Width / _currentPageImage.Width;
                int zoomLevel = (int)(scale * 100);
                SetZoom(Math.Max(50, Math.Min(200, zoomLevel)));
            }
        }

        private void FitToHeight()
        {
            if (_currentPageImage != null)
            {
                float scale = (float)_pictureBoxPdf.ClientSize.Height / _currentPageImage.Height;
                int zoomLevel = (int)(scale * 100);
                SetZoom(Math.Max(50, Math.Min(200, zoomLevel)));
            }
        }

        private void FitToPage()
        {
            if (_currentPageImage != null)
            {
                float scale = Math.Min(
                    (float)_pictureBoxPdf.ClientSize.Width / _currentPageImage.Width,
                    (float)_pictureBoxPdf.ClientSize.Height / _currentPageImage.Height);
                int zoomLevel = (int)(scale * 100);
                SetZoom(Math.Max(50, Math.Min(200, zoomLevel)));
            }
        }

        private void _loadingIndicator_Click(object? sender, EventArgs e)
        {
        }

        private void TreeViewFiles_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            FileSelected?.Invoke(this, EventArgs.Empty);
        }

        private void TextBoxFilter_TextChanged(object? sender, EventArgs e)
        {
            UpdateFileListDisplay();
        }

        private void ButtonTranslate_Click(object? sender, EventArgs e)
        {
            TranslateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSpeakOriginal_Click(object? sender, EventArgs e)
        {
            SpeakOriginal?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSpeakTranslation_Click(object? sender, EventArgs e)
        {
            SpeakTranslation?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAddToLearningContent_Click(object? sender, EventArgs e)
        {
            AddToLearningList?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonOpenFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _presenter?.LoadFolder(dialog.SelectedPath);
            }
        }

        #region WinForms Designer Generated Code


        private void InitializeComponent()
        {
            components = new Container();
            _splitContainerMain = new SplitContainer();
            _panelPdf = new Panel();
            _panelNavigation = new Panel();
            _toolbarGroupNav = new Panel();
            _buttonPrev = new Button();
            _textBoxPage = new TextBox();
            _labelPageCount = new Label();
            _buttonNext = new Button();
            _progressBarPage = new ProgressBar();
            _toolbarGroupView = new Panel();
            _buttonZoomOut = new Button();
            _trackBarZoom = new TrackBar();
            _labelZoom = new Label();
            _buttonZoomIn = new Button();
            _buttonResetView = new Button();
            _buttonRotate = new Button();
            _buttonLockView = new Button();
            _toolbarGroupMode = new Panel();
            _buttonNightMode = new Button();
            _buttonDualPage = new Button();
            _buttonFullscreen = new Button();
            _toolbarGroupTools = new Panel();
            _buttonSelectMode = new Button();
            _buttonHighlightMode = new Button();
            _buttonRectangleMode = new Button();
            _buttonEllipseMode = new Button();
            _buttonArrowMode = new Button();
            _buttonPenMode = new Button();
            _buttonMosaicMode = new Button();
            _buttonTextMode = new Button();
            _buttonUndoAnnotation = new Button();
            _buttonAskAi = new Button();
            _buttonOpenFolder = new Button();
            _panelAnnotationOptions = new Panel();
            _panelThickness = new Panel();
            _buttonThickness1 = new Button();
            _buttonThickness2 = new Button();
            _buttonThickness3 = new Button();
            _panelColor = new Panel();
            _buttonColorBlue = new Button();
            _buttonColorGreen = new Button();
            _buttonColorOrange = new Button();
            _buttonColorRed = new Button();
            _buttonColorBlack = new Button();
            _buttonColorWhite = new Button();
            _buttonClearAllAnnotations = new Button();
            _loadingIndicator = new LoadingIndicator();
            _statusBar = new Panel();
            _statusLabelLeft = new Label();
            _statusLabelRight = new Label();
            _pictureBoxPdf = new PictureBox();
            _contextMenuPdf = new ContextMenuStrip(components);
            menuItemCopy = new ToolStripMenuItem();
            menuItemSearch = new ToolStripMenuItem();
            menuItemHighlight = new ToolStripMenuItem();
            menuItemRectangle = new ToolStripMenuItem();
            menuItemText = new ToolStripMenuItem();
            menuItemZoomIn = new ToolStripMenuItem();
            menuItemZoomOut = new ToolStripMenuItem();
            menuItemResetZoom = new ToolStripMenuItem();
            menuItemExport = new ToolStripMenuItem();
            menuItemRotationSeparator = new ToolStripSeparator();
            menuItemRotateLeft = new ToolStripMenuItem();
            menuItemRotateRight = new ToolStripMenuItem();
            menuItemResetRotation = new ToolStripMenuItem();
            _ocrPanel = new Panel();
            _ocrPictureBox = new PictureBox();
            _ocrCloseButton = new Button();
            _pageTransitionOverlay = new Panel();
            _transitionLabel = new Label();
            _panelLeftContainer = new Panel();
            _tabControlLeft = new TabControl();
            _tabPageFiles = new TabPage();
            _treeViewFiles = new TreeView();
            _textBoxFilter = new TextBox();
            _tabPageThumbnails = new TabPage();
            _panelThumbnails = new Panel();
            _flowLayoutPanelThumbnails = new FlowLayoutPanel();
            _tabPageTranslate = new TabPage();
            _groupBoxProgress = new GroupBox();
            _textBoxTranslation = new TextBox();
            _buttonSpeakTranslation = new Button();
            _buttonAddToLearningContent = new Button();
            _textBoxOriginal = new TextBox();
            _buttonSpeakOriginal = new Button();
            _checkBoxAutoSpeak = new CheckBox();
            _labelTranslation = new Label();
            _labelOriginal = new Label();
            _buttonTranslate = new Button();
            _tabPageBookmarksAndHighlights = new TabPage();
            _groupBoxHighlights = new GroupBox();
            _groupBoxHighlightColor = new GroupBox();
            _radioHighlightYellow = new RadioButton();
            _radioHighlightGreen = new RadioButton();
            _radioHighlightBlue = new RadioButton();
            _radioHighlightPink = new RadioButton();
            _radioHighlightOrange = new RadioButton();
            _listBoxHighlights = new ListBox();
            _highlightButtonPanel = new FlowLayoutPanel();
            _buttonRemoveHighlight = new Button();
            _buttonEditHighlight = new Button();
            _buttonUndoHighlight = new Button();
            _buttonBatchRemoveHighlight = new Button();
            _buttonExportHighlights = new Button();
            _groupBoxBookmarks = new GroupBox();
            _listBoxBookmarks = new ListBox();
            _textBoxBookmarkTitle = new TextBox();
            _buttonPanel = new FlowLayoutPanel();
            _buttonAddBookmark = new Button();
            _buttonRemoveBookmark = new Button();
            _toolTip = new ToolTip(components);
            _buttonStrikethroughMode = new Button();
            _toastLabel = new Label();
            _buttonTranslationToggle = new Button();
            _pageTransitionTimer = new System.Windows.Forms.Timer(components);
            _checkBoxAutoTranslate = new CheckBox();
            ((ISupportInitialize)_splitContainerMain).BeginInit();
            _splitContainerMain.Panel1.SuspendLayout();
            _splitContainerMain.Panel2.SuspendLayout();
            _splitContainerMain.SuspendLayout();
            _panelPdf.SuspendLayout();
            _panelNavigation.SuspendLayout();
            _toolbarGroupNav.SuspendLayout();
            _toolbarGroupView.SuspendLayout();
            ((ISupportInitialize)_trackBarZoom).BeginInit();
            _toolbarGroupMode.SuspendLayout();
            _toolbarGroupTools.SuspendLayout();
            _panelAnnotationOptions.SuspendLayout();
            _panelThickness.SuspendLayout();
            _panelColor.SuspendLayout();
            _statusBar.SuspendLayout();
            ((ISupportInitialize)_pictureBoxPdf).BeginInit();
            _contextMenuPdf.SuspendLayout();
            _ocrPanel.SuspendLayout();
            ((ISupportInitialize)_ocrPictureBox).BeginInit();
            _pageTransitionOverlay.SuspendLayout();
            _panelLeftContainer.SuspendLayout();
            _tabControlLeft.SuspendLayout();
            _tabPageFiles.SuspendLayout();
            _tabPageThumbnails.SuspendLayout();
            _panelThumbnails.SuspendLayout();
            _tabPageTranslate.SuspendLayout();
            _groupBoxProgress.SuspendLayout();
            _tabPageBookmarksAndHighlights.SuspendLayout();
            _groupBoxHighlights.SuspendLayout();
            _groupBoxHighlightColor.SuspendLayout();
            _highlightButtonPanel.SuspendLayout();
            _groupBoxBookmarks.SuspendLayout();
            _buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _splitContainerMain
            // 
            _splitContainerMain.Dock = DockStyle.Fill;
            _splitContainerMain.FixedPanel = FixedPanel.Panel2;
            _splitContainerMain.Location = new Point(0, 0);
            _splitContainerMain.Name = "_splitContainerMain";
            // 
            // _splitContainerMain.Panel1
            // 
            _splitContainerMain.Panel1.Controls.Add(_panelPdf);
            // 
            // _splitContainerMain.Panel2
            // 
            _splitContainerMain.Panel2.Controls.Add(_panelLeftContainer);
            _splitContainerMain.Size = new Size(1461, 900);
            _splitContainerMain.SplitterDistance = 1127;
            _splitContainerMain.TabIndex = 0;
            // 
            // _panelPdf
            // 
            _panelPdf.BackColor = Color.FromArgb(240, 240, 240);
            _panelPdf.Controls.Add(_panelNavigation);
            _panelPdf.Controls.Add(_statusBar);
            _panelPdf.Controls.Add(_pictureBoxPdf);
            _panelPdf.Controls.Add(_ocrPanel);
            _panelPdf.Controls.Add(_pageTransitionOverlay);
            _panelPdf.Dock = DockStyle.Fill;
            _panelPdf.Location = new Point(0, 0);
            _panelPdf.Name = "_panelPdf";
            _panelPdf.Size = new Size(1127, 900);
            _panelPdf.TabIndex = 1;
            // 
            // _panelNavigation
            // 
            _panelNavigation.BackColor = Color.White;
            _panelNavigation.Controls.Add(_toolbarGroupNav);
            _panelNavigation.Controls.Add(_toolbarGroupView);
            _panelNavigation.Controls.Add(_toolbarGroupMode);
            _panelNavigation.Controls.Add(_toolbarGroupTools);
            _panelNavigation.Controls.Add(_loadingIndicator);
            _panelNavigation.Dock = DockStyle.Top;
            _panelNavigation.Location = new Point(0, 0);
            _panelNavigation.Name = "_panelNavigation";
            _panelNavigation.Padding = new Padding(8);
            _panelNavigation.Size = new Size(1127, 80);
            _panelNavigation.TabIndex = 3;
            _panelNavigation.MouseDown += PanelNavigation_MouseDown;
            _panelNavigation.MouseMove += PanelNavigation_MouseMove;
            _panelNavigation.MouseUp += PanelNavigation_MouseUp;
            // 
            // _toolbarGroupNav
            // 
            _toolbarGroupNav.Controls.Add(_buttonPrev);
            _toolbarGroupNav.Controls.Add(_textBoxPage);
            _toolbarGroupNav.Controls.Add(_labelPageCount);
            _toolbarGroupNav.Controls.Add(_buttonNext);
            _toolbarGroupNav.Controls.Add(_progressBarPage);
            _toolbarGroupNav.Dock = DockStyle.Left;
            _toolbarGroupNav.Location = new Point(919, 8);
            _toolbarGroupNav.Name = "_toolbarGroupNav";
            _toolbarGroupNav.Size = new Size(280, 64);
            _toolbarGroupNav.TabIndex = 0;
            // 
            // _buttonPrev
            // 
            _buttonPrev.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPrev.FlatStyle = FlatStyle.Flat;
            _buttonPrev.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPrev.Location = new Point(0, 2);
            _buttonPrev.Name = "_buttonPrev";
            _buttonPrev.Size = new Size(32, 32);
            _buttonPrev.TabIndex = 0;
            _buttonPrev.Text = "◀";
            _buttonPrev.UseVisualStyleBackColor = false;
            _buttonPrev.Click += ButtonPrev_Click;
            // 
            // _textBoxPage
            // 
            _textBoxPage.BorderStyle = BorderStyle.FixedSingle;
            _textBoxPage.Font = new Font("Microsoft YaHei UI", 10F);
            _textBoxPage.Location = new Point(38, 5);
            _textBoxPage.Name = "_textBoxPage";
            _textBoxPage.Size = new Size(40, 24);
            _textBoxPage.TabIndex = 1;
            _textBoxPage.Text = "1";
            _textBoxPage.TextAlign = HorizontalAlignment.Center;
            _textBoxPage.KeyDown += TextBoxPage_KeyDown;
            // 
            // _labelPageCount
            // 
            _labelPageCount.AutoSize = true;
            _labelPageCount.Font = new Font("Microsoft YaHei UI", 10F);
            _labelPageCount.ForeColor = Color.FromArgb(102, 102, 102);
            _labelPageCount.Location = new Point(83, 8);
            _labelPageCount.Name = "_labelPageCount";
            _labelPageCount.Size = new Size(27, 20);
            _labelPageCount.TabIndex = 2;
            _labelPageCount.Text = "/ 1";
            // 
            // _buttonNext
            // 
            _buttonNext.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonNext.FlatStyle = FlatStyle.Flat;
            _buttonNext.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonNext.Location = new Point(115, 2);
            _buttonNext.Name = "_buttonNext";
            _buttonNext.Size = new Size(32, 32);
            _buttonNext.TabIndex = 3;
            _buttonNext.Text = "▶";
            _buttonNext.UseVisualStyleBackColor = false;
            _buttonNext.Click += ButtonNext_Click;
            // 
            // _progressBarPage
            // 
            _progressBarPage.Location = new Point(155, 5);
            _progressBarPage.Maximum = 1;
            _progressBarPage.Minimum = 1;
            _progressBarPage.Name = "_progressBarPage";
            _progressBarPage.Size = new Size(120, 24);
            _progressBarPage.Style = ProgressBarStyle.Continuous;
            _progressBarPage.TabIndex = 4;
            _progressBarPage.Value = 1;
            // 
            // _toolbarGroupView
            // 
            _toolbarGroupView.Controls.Add(_buttonZoomOut);
            _toolbarGroupView.Controls.Add(_trackBarZoom);
            _toolbarGroupView.Controls.Add(_labelZoom);
            _toolbarGroupView.Controls.Add(_buttonZoomIn);
            _toolbarGroupView.Controls.Add(_buttonResetView);
            _toolbarGroupView.Controls.Add(_buttonRotate);
            _toolbarGroupView.Controls.Add(_buttonLockView);
            _toolbarGroupView.Dock = DockStyle.Left;
            _toolbarGroupView.Location = new Point(598, 8);
            _toolbarGroupView.Name = "_toolbarGroupView";
            _toolbarGroupView.Size = new Size(321, 64);
            _toolbarGroupView.TabIndex = 1;
            // 
            // _buttonZoomOut
            // 
            _buttonZoomOut.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonZoomOut.FlatStyle = FlatStyle.Flat;
            _buttonZoomOut.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonZoomOut.Location = new Point(5, 2);
            _buttonZoomOut.Name = "_buttonZoomOut";
            _buttonZoomOut.Size = new Size(28, 32);
            _buttonZoomOut.TabIndex = 0;
            _buttonZoomOut.Text = "−";
            _buttonZoomOut.UseVisualStyleBackColor = false;
            _buttonZoomOut.Click += ButtonZoomOut_Click;
            // 
            // _trackBarZoom
            // 
            _trackBarZoom.Location = new Point(39, 4);
            _trackBarZoom.Maximum = 200;
            _trackBarZoom.Minimum = 50;
            _trackBarZoom.Name = "_trackBarZoom";
            _trackBarZoom.Size = new Size(100, 45);
            _trackBarZoom.TabIndex = 1;
            _trackBarZoom.Value = 100;
            _trackBarZoom.Scroll += TrackBarZoom_Scroll;
            // 
            // _labelZoom
            // 
            _labelZoom.AutoSize = true;
            _labelZoom.Font = new Font("Microsoft YaHei UI", 9F);
            _labelZoom.ForeColor = Color.FromArgb(102, 102, 102);
            _labelZoom.Location = new Point(143, 8);
            _labelZoom.Name = "_labelZoom";
            _labelZoom.Size = new Size(40, 17);
            _labelZoom.TabIndex = 2;
            _labelZoom.Text = "100%";
            // 
            // _buttonZoomIn
            // 
            _buttonZoomIn.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonZoomIn.FlatStyle = FlatStyle.Flat;
            _buttonZoomIn.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonZoomIn.Location = new Point(187, 2);
            _buttonZoomIn.Name = "_buttonZoomIn";
            _buttonZoomIn.Size = new Size(28, 32);
            _buttonZoomIn.TabIndex = 3;
            _buttonZoomIn.Text = "+";
            _buttonZoomIn.UseVisualStyleBackColor = false;
            _buttonZoomIn.Click += ButtonZoomIn_Click;
            // 
            // _buttonResetView
            // 
            _buttonResetView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonResetView.FlatStyle = FlatStyle.Flat;
            _buttonResetView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonResetView.Location = new Point(221, 2);
            _buttonResetView.Name = "_buttonResetView";
            _buttonResetView.Size = new Size(28, 32);
            _buttonResetView.TabIndex = 4;
            _buttonResetView.Text = "↺";
            _buttonResetView.UseVisualStyleBackColor = false;
            _buttonResetView.Click += ButtonResetView_Click;
            // 
            // _buttonRotate
            // 
            _buttonRotate.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRotate.FlatStyle = FlatStyle.Flat;
            _buttonRotate.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonRotate.Location = new Point(253, 2);
            _buttonRotate.Name = "_buttonRotate";
            _buttonRotate.Size = new Size(28, 32);
            _buttonRotate.TabIndex = 5;
            _buttonRotate.Text = "↻";
            _toolTip.SetToolTip(_buttonRotate, "顺时针旋转90°（右键逆时针旋转）");
            _buttonRotate.UseVisualStyleBackColor = false;
            _buttonRotate.Click += ButtonRotate_Click;
            _buttonRotate.MouseDown += ButtonRotate_MouseDown;
            // 
            // _buttonLockView
            // 
            _buttonLockView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonLockView.FlatStyle = FlatStyle.Flat;
            _buttonLockView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonLockView.Location = new Point(285, 2);
            _buttonLockView.Name = "_buttonLockView";
            _buttonLockView.Size = new Size(28, 32);
            _buttonLockView.TabIndex = 6;
            _buttonLockView.Text = "🔓";
            _buttonLockView.UseVisualStyleBackColor = false;
            _buttonLockView.Click += ButtonLockView_Click;
            // 
            // _toolbarGroupMode
            // 
            _toolbarGroupMode.Controls.Add(_buttonNightMode);
            _toolbarGroupMode.Controls.Add(_buttonDualPage);
            _toolbarGroupMode.Controls.Add(_buttonFullscreen);
            _toolbarGroupMode.Dock = DockStyle.Left;
            _toolbarGroupMode.Location = new Point(468, 8);
            _toolbarGroupMode.Name = "_toolbarGroupMode";
            _toolbarGroupMode.Size = new Size(130, 64);
            _toolbarGroupMode.TabIndex = 2;
            // 
            // _buttonNightMode
            // 
            _buttonNightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonNightMode.FlatStyle = FlatStyle.Flat;
            _buttonNightMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonNightMode.Location = new Point(10, 2);
            _buttonNightMode.Name = "_buttonNightMode";
            _buttonNightMode.Size = new Size(32, 32);
            _buttonNightMode.TabIndex = 0;
            _buttonNightMode.Text = "🌙";
            _buttonNightMode.UseVisualStyleBackColor = false;
            _buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // _buttonDualPage
            // 
            _buttonDualPage.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonDualPage.FlatStyle = FlatStyle.Flat;
            _buttonDualPage.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonDualPage.Location = new Point(48, 2);
            _buttonDualPage.Name = "_buttonDualPage";
            _buttonDualPage.Size = new Size(32, 32);
            _buttonDualPage.TabIndex = 1;
            _buttonDualPage.Text = "📖";
            _buttonDualPage.UseVisualStyleBackColor = false;
            _buttonDualPage.Click += ButtonDualPage_Click;
            // 
            // _buttonFullscreen
            // 
            _buttonFullscreen.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonFullscreen.FlatStyle = FlatStyle.Flat;
            _buttonFullscreen.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonFullscreen.Location = new Point(86, 2);
            _buttonFullscreen.Name = "_buttonFullscreen";
            _buttonFullscreen.Size = new Size(32, 32);
            _buttonFullscreen.TabIndex = 2;
            _buttonFullscreen.Text = "⛶";
            _buttonFullscreen.UseVisualStyleBackColor = false;
            _buttonFullscreen.Click += ButtonFullscreen_Click;
            // 
            // _toolbarGroupTools
            // 
            _toolbarGroupTools.Controls.Add(_buttonSelectMode);
            _toolbarGroupTools.Controls.Add(_buttonHighlightMode);
            _toolbarGroupTools.Controls.Add(_buttonRectangleMode);
            _toolbarGroupTools.Controls.Add(_buttonEllipseMode);
            _toolbarGroupTools.Controls.Add(_buttonArrowMode);
            _toolbarGroupTools.Controls.Add(_buttonPenMode);
            _toolbarGroupTools.Controls.Add(_buttonMosaicMode);
            _toolbarGroupTools.Controls.Add(_buttonTextMode);
            _toolbarGroupTools.Controls.Add(_buttonUndoAnnotation);
            _toolbarGroupTools.Controls.Add(_buttonAskAi);
            _toolbarGroupTools.Controls.Add(_buttonOpenFolder);
            _toolbarGroupTools.Controls.Add(_panelAnnotationOptions);
            _toolbarGroupTools.Controls.Add(_buttonClearAllAnnotations);
            _toolbarGroupTools.Dock = DockStyle.Left;
            _toolbarGroupTools.Location = new Point(8, 8);
            _toolbarGroupTools.Name = "_toolbarGroupTools";
            _toolbarGroupTools.Size = new Size(460, 64);
            _toolbarGroupTools.TabIndex = 3;
            // 
            // _buttonSelectMode
            // 
            _buttonSelectMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSelectMode.FlatStyle = FlatStyle.Flat;
            _buttonSelectMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonSelectMode.Location = new Point(10, 2);
            _buttonSelectMode.Name = "_buttonSelectMode";
            _buttonSelectMode.Size = new Size(32, 32);
            _buttonSelectMode.TabIndex = 0;
            _buttonSelectMode.Text = "👆";
            _buttonSelectMode.UseVisualStyleBackColor = false;
            _buttonSelectMode.Click += ButtonSelectMode_Click;
            // 
            // _buttonHighlightMode
            // 
            _buttonHighlightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonHighlightMode.FlatStyle = FlatStyle.Flat;
            _buttonHighlightMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonHighlightMode.Location = new Point(48, 2);
            _buttonHighlightMode.Name = "_buttonHighlightMode";
            _buttonHighlightMode.Size = new Size(32, 32);
            _buttonHighlightMode.TabIndex = 1;
            _buttonHighlightMode.Text = "⭐";
            _buttonHighlightMode.UseVisualStyleBackColor = false;
            _buttonHighlightMode.Click += ButtonHighlightMode_Click;
            // 
            // _buttonRectangleMode
            // 
            _buttonRectangleMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRectangleMode.FlatStyle = FlatStyle.Flat;
            _buttonRectangleMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonRectangleMode.Location = new Point(86, 2);
            _buttonRectangleMode.Name = "_buttonRectangleMode";
            _buttonRectangleMode.Size = new Size(32, 32);
            _buttonRectangleMode.TabIndex = 1;
            _buttonRectangleMode.Text = "▢";
            _buttonRectangleMode.UseVisualStyleBackColor = false;
            _buttonRectangleMode.Click += ButtonRectangleMode_Click;
            // 
            // _buttonEllipseMode
            // 
            _buttonEllipseMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonEllipseMode.FlatStyle = FlatStyle.Flat;
            _buttonEllipseMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonEllipseMode.Location = new Point(124, 2);
            _buttonEllipseMode.Name = "_buttonEllipseMode";
            _buttonEllipseMode.Size = new Size(32, 32);
            _buttonEllipseMode.TabIndex = 2;
            _buttonEllipseMode.Text = "◯";
            _buttonEllipseMode.UseVisualStyleBackColor = false;
            _buttonEllipseMode.Click += ButtonEllipseMode_Click;
            // 
            // _buttonArrowMode
            // 
            _buttonArrowMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonArrowMode.FlatStyle = FlatStyle.Flat;
            _buttonArrowMode.Font = new Font("Microsoft YaHei UI", 12F);
            _buttonArrowMode.Location = new Point(162, 2);
            _buttonArrowMode.Name = "_buttonArrowMode";
            _buttonArrowMode.Size = new Size(32, 32);
            _buttonArrowMode.TabIndex = 3;
            _buttonArrowMode.Text = "━";
            _buttonArrowMode.UseVisualStyleBackColor = false;
            _buttonArrowMode.Click += ButtonArrowMode_Click;
            // 
            // _buttonPenMode
            // 
            _buttonPenMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenMode.FlatStyle = FlatStyle.Flat;
            _buttonPenMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonPenMode.Location = new Point(200, 2);
            _buttonPenMode.Name = "_buttonPenMode";
            _buttonPenMode.Size = new Size(32, 32);
            _buttonPenMode.TabIndex = 4;
            _buttonPenMode.Text = "✎";
            _buttonPenMode.UseVisualStyleBackColor = false;
            _buttonPenMode.Click += ButtonPenMode_Click;
            // 
            // _buttonMosaicMode
            // 
            _buttonMosaicMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonMosaicMode.FlatStyle = FlatStyle.Flat;
            _buttonMosaicMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonMosaicMode.Location = new Point(238, 2);
            _buttonMosaicMode.Name = "_buttonMosaicMode";
            _buttonMosaicMode.Size = new Size(32, 32);
            _buttonMosaicMode.TabIndex = 5;
            _buttonMosaicMode.Text = "▦";
            _buttonMosaicMode.UseVisualStyleBackColor = false;
            _buttonMosaicMode.Click += ButtonMosaicMode_Click;
            // 
            // _buttonTextMode
            // 
            _buttonTextMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTextMode.FlatStyle = FlatStyle.Flat;
            _buttonTextMode.Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
            _buttonTextMode.Location = new Point(276, 2);
            _buttonTextMode.Name = "_buttonTextMode";
            _buttonTextMode.Size = new Size(32, 32);
            _buttonTextMode.TabIndex = 6;
            _buttonTextMode.Text = "T";
            _buttonTextMode.UseVisualStyleBackColor = false;
            _buttonTextMode.Click += ButtonTextMode_Click;
            // 
            // _buttonUndoAnnotation
            // 
            _buttonUndoAnnotation.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonUndoAnnotation.FlatStyle = FlatStyle.Flat;
            _buttonUndoAnnotation.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonUndoAnnotation.Location = new Point(314, 2);
            _buttonUndoAnnotation.Name = "_buttonUndoAnnotation";
            _buttonUndoAnnotation.Size = new Size(32, 32);
            _buttonUndoAnnotation.TabIndex = 7;
            _buttonUndoAnnotation.Text = "↩";
            _buttonUndoAnnotation.UseVisualStyleBackColor = false;
            _buttonUndoAnnotation.Click += ButtonUndoAnnotation_Click;
            // 
            // _buttonAskAi
            // 
            _buttonAskAi.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonAskAi.FlatStyle = FlatStyle.Flat;
            _buttonAskAi.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonAskAi.Location = new Point(390, 2);
            _buttonAskAi.Name = "_buttonAskAi";
            _buttonAskAi.Size = new Size(32, 32);
            _buttonAskAi.TabIndex = 9;
            _buttonAskAi.Text = "🤖";
            _buttonAskAi.UseVisualStyleBackColor = false;
            _buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // _buttonOpenFolder
            // 
            _buttonOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonOpenFolder.FlatStyle = FlatStyle.Flat;
            _buttonOpenFolder.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonOpenFolder.Location = new Point(428, 2);
            _buttonOpenFolder.Name = "_buttonOpenFolder";
            _buttonOpenFolder.Size = new Size(32, 32);
            _buttonOpenFolder.TabIndex = 9;
            _buttonOpenFolder.Text = "📂";
            _buttonOpenFolder.UseVisualStyleBackColor = false;
            _buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // _panelAnnotationOptions
            // 
            _panelAnnotationOptions.Controls.Add(_panelThickness);
            _panelAnnotationOptions.Controls.Add(_panelColor);
            _panelAnnotationOptions.Location = new Point(50, 38);
            _panelAnnotationOptions.Name = "_panelAnnotationOptions";
            _panelAnnotationOptions.Size = new Size(260, 28);
            _panelAnnotationOptions.TabIndex = 9;
            _panelAnnotationOptions.Visible = false;
            // 
            // _panelThickness
            // 
            _panelThickness.Controls.Add(_buttonThickness1);
            _panelThickness.Controls.Add(_buttonThickness2);
            _panelThickness.Controls.Add(_buttonThickness3);
            _panelThickness.Location = new Point(0, 4);
            _panelThickness.Name = "_panelThickness";
            _panelThickness.Size = new Size(72, 22);
            _panelThickness.TabIndex = 0;
            // 
            // _buttonThickness1
            // 
            _buttonThickness1.BackColor = Color.Black;
            _buttonThickness1.FlatAppearance.BorderSize = 0;
            _buttonThickness1.FlatStyle = FlatStyle.Flat;
            _buttonThickness1.Location = new Point(2, 8);
            _buttonThickness1.Name = "_buttonThickness1";
            _buttonThickness1.Size = new Size(18, 4);
            _buttonThickness1.TabIndex = 0;
            _buttonThickness1.UseVisualStyleBackColor = false;
            _buttonThickness1.Click += ButtonThickness1_Click;
            // 
            // _buttonThickness2
            // 
            _buttonThickness2.BackColor = Color.Black;
            _buttonThickness2.FlatAppearance.BorderSize = 0;
            _buttonThickness2.FlatStyle = FlatStyle.Flat;
            _buttonThickness2.Location = new Point(26, 5);
            _buttonThickness2.Name = "_buttonThickness2";
            _buttonThickness2.Size = new Size(18, 10);
            _buttonThickness2.TabIndex = 1;
            _buttonThickness2.UseVisualStyleBackColor = false;
            _buttonThickness2.Click += ButtonThickness2_Click;
            // 
            // _buttonThickness3
            // 
            _buttonThickness3.BackColor = Color.Black;
            _buttonThickness3.FlatAppearance.BorderSize = 0;
            _buttonThickness3.FlatStyle = FlatStyle.Flat;
            _buttonThickness3.Location = new Point(50, 2);
            _buttonThickness3.Name = "_buttonThickness3";
            _buttonThickness3.Size = new Size(18, 16);
            _buttonThickness3.TabIndex = 2;
            _buttonThickness3.UseVisualStyleBackColor = false;
            _buttonThickness3.Click += ButtonThickness3_Click;
            // 
            // _panelColor
            // 
            _panelColor.Controls.Add(_buttonColorBlue);
            _panelColor.Controls.Add(_buttonColorGreen);
            _panelColor.Controls.Add(_buttonColorOrange);
            _panelColor.Controls.Add(_buttonColorRed);
            _panelColor.Controls.Add(_buttonColorBlack);
            _panelColor.Controls.Add(_buttonColorWhite);
            _panelColor.Location = new Point(80, 0);
            _panelColor.Name = "_panelColor";
            _panelColor.Size = new Size(170, 28);
            _panelColor.TabIndex = 1;
            // 
            // _buttonColorBlue
            // 
            _buttonColorBlue.BackColor = Color.RoyalBlue;
            _buttonColorBlue.FlatStyle = FlatStyle.Flat;
            _buttonColorBlue.Location = new Point(2, 4);
            _buttonColorBlue.Name = "_buttonColorBlue";
            _buttonColorBlue.Size = new Size(20, 20);
            _buttonColorBlue.TabIndex = 0;
            _buttonColorBlue.UseVisualStyleBackColor = false;
            _buttonColorBlue.Click += ButtonColorBlue_Click;
            // 
            // _buttonColorGreen
            // 
            _buttonColorGreen.BackColor = Color.LimeGreen;
            _buttonColorGreen.FlatStyle = FlatStyle.Flat;
            _buttonColorGreen.Location = new Point(28, 4);
            _buttonColorGreen.Name = "_buttonColorGreen";
            _buttonColorGreen.Size = new Size(20, 20);
            _buttonColorGreen.TabIndex = 1;
            _buttonColorGreen.UseVisualStyleBackColor = false;
            _buttonColorGreen.Click += ButtonColorGreen_Click;
            // 
            // _buttonColorOrange
            // 
            _buttonColorOrange.BackColor = Color.Orange;
            _buttonColorOrange.FlatStyle = FlatStyle.Flat;
            _buttonColorOrange.Location = new Point(54, 4);
            _buttonColorOrange.Name = "_buttonColorOrange";
            _buttonColorOrange.Size = new Size(20, 20);
            _buttonColorOrange.TabIndex = 2;
            _buttonColorOrange.UseVisualStyleBackColor = false;
            _buttonColorOrange.Click += ButtonColorOrange_Click;
            // 
            // _buttonColorRed
            // 
            _buttonColorRed.BackColor = Color.Red;
            _buttonColorRed.FlatStyle = FlatStyle.Flat;
            _buttonColorRed.Location = new Point(80, 4);
            _buttonColorRed.Name = "_buttonColorRed";
            _buttonColorRed.Size = new Size(20, 20);
            _buttonColorRed.TabIndex = 3;
            _buttonColorRed.UseVisualStyleBackColor = false;
            _buttonColorRed.Click += ButtonColorRed_Click;
            // 
            // _buttonColorBlack
            // 
            _buttonColorBlack.BackColor = Color.Black;
            _buttonColorBlack.FlatStyle = FlatStyle.Flat;
            _buttonColorBlack.Location = new Point(106, 4);
            _buttonColorBlack.Name = "_buttonColorBlack";
            _buttonColorBlack.Size = new Size(20, 20);
            _buttonColorBlack.TabIndex = 4;
            _buttonColorBlack.UseVisualStyleBackColor = false;
            _buttonColorBlack.Click += ButtonColorBlack_Click;
            // 
            // _buttonColorWhite
            // 
            _buttonColorWhite.BackColor = Color.White;
            _buttonColorWhite.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonColorWhite.FlatStyle = FlatStyle.Flat;
            _buttonColorWhite.Location = new Point(132, 4);
            _buttonColorWhite.Name = "_buttonColorWhite";
            _buttonColorWhite.Size = new Size(20, 20);
            _buttonColorWhite.TabIndex = 5;
            _buttonColorWhite.UseVisualStyleBackColor = false;
            _buttonColorWhite.Click += ButtonColorWhite_Click;
            // 
            // _buttonClearAllAnnotations
            // 
            _buttonClearAllAnnotations.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonClearAllAnnotations.FlatStyle = FlatStyle.Flat;
            _buttonClearAllAnnotations.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonClearAllAnnotations.Location = new Point(352, 2);
            _buttonClearAllAnnotations.Name = "_buttonClearAllAnnotations";
            _buttonClearAllAnnotations.Size = new Size(32, 32);
            _buttonClearAllAnnotations.TabIndex = 8;
            _buttonClearAllAnnotations.Text = "🗑";
            _buttonClearAllAnnotations.UseVisualStyleBackColor = false;
            _buttonClearAllAnnotations.Click += ButtonClearAllAnnotations_Click;
            // 
            // _loadingIndicator
            // 
            _loadingIndicator.BackColor = Color.FromArgb(245, 245, 245);
            _loadingIndicator.ForeColor = Color.FromArgb(66, 133, 244);
            _loadingIndicator.Location = new Point(980, 8);
            _loadingIndicator.Name = "_loadingIndicator";
            _loadingIndicator.Size = new Size(38, 36);
            _loadingIndicator.TabIndex = 5;
            _loadingIndicator.Visible = false;
            _loadingIndicator.Click += _loadingIndicator_Click;
            // 
            // _statusBar
            // 
            _statusBar.BackColor = Color.White;
            _statusBar.Controls.Add(_statusLabelLeft);
            _statusBar.Controls.Add(_statusLabelRight);
            _statusBar.Dock = DockStyle.Bottom;
            _statusBar.Location = new Point(0, 876);
            _statusBar.Name = "_statusBar";
            _statusBar.Size = new Size(1127, 24);
            _statusBar.TabIndex = 4;
            // 
            // _statusLabelLeft
            // 
            _statusLabelLeft.AutoSize = true;
            _statusLabelLeft.Font = new Font("Microsoft YaHei UI", 9F);
            _statusLabelLeft.ForeColor = Color.FromArgb(153, 153, 153);
            _statusLabelLeft.Location = new Point(12, 4);
            _statusLabelLeft.Name = "_statusLabelLeft";
            _statusLabelLeft.Size = new Size(142, 17);
            _statusLabelLeft.TabIndex = 0;
            _statusLabelLeft.Text = "第 1 页 / 共 1 页 · 100%";
            // 
            // _statusLabelRight
            // 
            _statusLabelRight.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _statusLabelRight.AutoSize = true;
            _statusLabelRight.Font = new Font("Microsoft YaHei UI", 9F);
            _statusLabelRight.ForeColor = Color.FromArgb(153, 153, 153);
            _statusLabelRight.Location = new Point(977, 4);
            _statusLabelRight.Name = "_statusLabelRight";
            _statusLabelRight.Size = new Size(130, 17);
            _statusLabelRight.TabIndex = 1;
            _statusLabelRight.Text = "缩放: 100% · 高亮模式";
            _statusLabelRight.TextAlign = ContentAlignment.MiddleRight;
            // 
            // _pictureBoxPdf
            // 
            _pictureBoxPdf.ContextMenuStrip = _contextMenuPdf;
            _pictureBoxPdf.Dock = DockStyle.Fill;
            _pictureBoxPdf.Location = new Point(0, 0);
            _pictureBoxPdf.Name = "_pictureBoxPdf";
            _pictureBoxPdf.Size = new Size(1127, 900);
            _pictureBoxPdf.TabIndex = 1;
            _pictureBoxPdf.TabStop = false;
            _pictureBoxPdf.Paint += PictureBoxPdf_Paint;
            _pictureBoxPdf.MouseWheel += PictureBoxPdf_MouseWheel;
            // 
            // _contextMenuPdf
            // 
            _contextMenuPdf.Items.AddRange(new ToolStripItem[] { menuItemCopy, menuItemSearch, menuItemHighlight, menuItemRectangle, menuItemText, menuItemZoomIn, menuItemZoomOut, menuItemResetZoom, menuItemExport, menuItemRotationSeparator, menuItemRotateLeft, menuItemRotateRight, menuItemResetRotation });
            _contextMenuPdf.Name = "_contextMenuPdf";
            _contextMenuPdf.Size = new Size(144, 274);
            // 
            // menuItemCopy
            // 
            menuItemCopy.Name = "menuItemCopy";
            menuItemCopy.Size = new Size(143, 22);
            menuItemCopy.Text = "复制";
            menuItemCopy.Click += MenuItemCopy_Click;
            // 
            // menuItemSearch
            // 
            menuItemSearch.Name = "menuItemSearch";
            menuItemSearch.Size = new Size(143, 22);
            menuItemSearch.Text = "搜索";
            menuItemSearch.Click += MenuItemSearch_Click;
            // 
            // menuItemHighlight
            // 
            menuItemHighlight.Name = "menuItemHighlight";
            menuItemHighlight.Size = new Size(143, 22);
            menuItemHighlight.Text = "高亮标注";
            menuItemHighlight.Click += MenuItemHighlight_Click;
            // 
            // menuItemRectangle
            // 
            menuItemRectangle.Name = "menuItemRectangle";
            menuItemRectangle.Size = new Size(143, 22);
            menuItemRectangle.Text = "矩形标注";
            menuItemRectangle.Click += MenuItemRectangle_Click;
            // 
            // menuItemText
            // 
            menuItemText.Name = "menuItemText";
            menuItemText.Size = new Size(143, 22);
            menuItemText.Text = "文字注解";
            menuItemText.Click += MenuItemText_Click;
            // 
            // menuItemZoomIn
            // 
            menuItemZoomIn.Name = "menuItemZoomIn";
            menuItemZoomIn.Size = new Size(143, 22);
            menuItemZoomIn.Text = "放大";
            menuItemZoomIn.Click += MenuItemZoomIn_Click;
            // 
            // menuItemZoomOut
            // 
            menuItemZoomOut.Name = "menuItemZoomOut";
            menuItemZoomOut.Size = new Size(143, 22);
            menuItemZoomOut.Text = "缩小";
            menuItemZoomOut.Click += MenuItemZoomOut_Click;
            // 
            // menuItemResetZoom
            // 
            menuItemResetZoom.Name = "menuItemResetZoom";
            menuItemResetZoom.Size = new Size(143, 22);
            menuItemResetZoom.Text = "重置缩放";
            menuItemResetZoom.Click += MenuItemResetZoom_Click;
            // 
            // menuItemExport
            // 
            menuItemExport.Name = "menuItemExport";
            menuItemExport.Size = new Size(143, 22);
            menuItemExport.Text = "导出当前页";
            menuItemExport.Click += MenuItemExport_Click;
            // 
            // menuItemRotationSeparator
            // 
            menuItemRotationSeparator.Name = "menuItemRotationSeparator";
            menuItemRotationSeparator.Size = new Size(140, 6);
            // 
            // menuItemRotateLeft
            // 
            menuItemRotateLeft.Name = "menuItemRotateLeft";
            menuItemRotateLeft.Size = new Size(143, 22);
            menuItemRotateLeft.Text = "向左旋转90°";
            menuItemRotateLeft.Click += MenuItemRotateLeft_Click;
            // 
            // menuItemRotateRight
            // 
            menuItemRotateRight.Name = "menuItemRotateRight";
            menuItemRotateRight.Size = new Size(143, 22);
            menuItemRotateRight.Text = "向右旋转90°";
            menuItemRotateRight.Click += MenuItemRotateRight_Click;
            // 
            // menuItemResetRotation
            // 
            menuItemResetRotation.Name = "menuItemResetRotation";
            menuItemResetRotation.Size = new Size(143, 22);
            menuItemResetRotation.Text = "重置旋转";
            menuItemResetRotation.Click += MenuItemResetRotation_Click;
            // 
            // _ocrPanel
            // 
            _ocrPanel.BackColor = Color.LightGray;
            _ocrPanel.BorderStyle = BorderStyle.FixedSingle;
            _ocrPanel.Controls.Add(_ocrPictureBox);
            _ocrPanel.Controls.Add(_ocrCloseButton);
            _ocrPanel.Location = new Point(100, 150);
            _ocrPanel.Name = "_ocrPanel";
            _ocrPanel.Size = new Size(200, 150);
            _ocrPanel.TabIndex = 2;
            _ocrPanel.Visible = false;
            // 
            // _ocrPictureBox
            // 
            _ocrPictureBox.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _ocrPictureBox.Location = new Point(2, 26);
            _ocrPictureBox.Name = "_ocrPictureBox";
            _ocrPictureBox.Size = new Size(192, 120);
            _ocrPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            _ocrPictureBox.TabIndex = 0;
            _ocrPictureBox.TabStop = false;
            // 
            // _ocrCloseButton
            // 
            _ocrCloseButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _ocrCloseButton.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
            _ocrCloseButton.Location = new Point(168, 2);
            _ocrCloseButton.Name = "_ocrCloseButton";
            _ocrCloseButton.Size = new Size(24, 20);
            _ocrCloseButton.TabIndex = 1;
            _ocrCloseButton.Text = "×";
            _ocrCloseButton.UseVisualStyleBackColor = true;
            _ocrCloseButton.Click += OcrCloseButton_Click;
            // 
            // _pageTransitionOverlay
            // 
            _pageTransitionOverlay.BackColor = Color.White;
            _pageTransitionOverlay.Controls.Add(_transitionLabel);
            _pageTransitionOverlay.Dock = DockStyle.Fill;
            _pageTransitionOverlay.Location = new Point(0, 0);
            _pageTransitionOverlay.Name = "_pageTransitionOverlay";
            _pageTransitionOverlay.Size = new Size(1127, 900);
            _pageTransitionOverlay.TabIndex = 0;
            _pageTransitionOverlay.Visible = false;
            // 
            // _transitionLabel
            // 
            _transitionLabel.Dock = DockStyle.Fill;
            _transitionLabel.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold);
            _transitionLabel.ForeColor = Color.FromArgb(200, 100, 100, 100);
            _transitionLabel.Location = new Point(0, 0);
            _transitionLabel.Name = "_transitionLabel";
            _transitionLabel.Size = new Size(1127, 900);
            _transitionLabel.TabIndex = 0;
            _transitionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // _panelLeftContainer
            // 
            _panelLeftContainer.BackColor = Color.White;
            _panelLeftContainer.Controls.Add(_tabControlLeft);
            _panelLeftContainer.Dock = DockStyle.Fill;
            _panelLeftContainer.Location = new Point(0, 0);
            _panelLeftContainer.Name = "_panelLeftContainer";
            _panelLeftContainer.Size = new Size(330, 900);
            _panelLeftContainer.TabIndex = 0;
            // 
            // _tabControlLeft
            // 
            _tabControlLeft.Controls.Add(_tabPageFiles);
            _tabControlLeft.Controls.Add(_tabPageThumbnails);
            _tabControlLeft.Controls.Add(_tabPageTranslate);
            _tabControlLeft.Controls.Add(_tabPageBookmarksAndHighlights);
            _tabControlLeft.Dock = DockStyle.Fill;
            _tabControlLeft.Font = new Font("Microsoft YaHei UI", 9F);
            _tabControlLeft.Location = new Point(0, 0);
            _tabControlLeft.Name = "_tabControlLeft";
            _tabControlLeft.SelectedIndex = 0;
            _tabControlLeft.Size = new Size(330, 900);
            _tabControlLeft.TabIndex = 0;
            // 
            // _tabPageFiles
            // 
            _tabPageFiles.Controls.Add(_treeViewFiles);
            _tabPageFiles.Controls.Add(_textBoxFilter);
            _tabPageFiles.Location = new Point(4, 26);
            _tabPageFiles.Name = "_tabPageFiles";
            _tabPageFiles.Padding = new Padding(3);
            _tabPageFiles.Size = new Size(322, 870);
            _tabPageFiles.TabIndex = 2;
            _tabPageFiles.Text = "📁 文件";
            _tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // _treeViewFiles
            // 
            _treeViewFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _treeViewFiles.BorderStyle = BorderStyle.None;
            _treeViewFiles.Location = new Point(3, 29);
            _treeViewFiles.Name = "_treeViewFiles";
            _treeViewFiles.Size = new Size(316, 838);
            _treeViewFiles.TabIndex = 0;
            _treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // _textBoxFilter
            // 
            _textBoxFilter.BorderStyle = BorderStyle.FixedSingle;
            _textBoxFilter.Dock = DockStyle.Top;
            _textBoxFilter.Font = new Font("Microsoft YaHei UI", 10F);
            _textBoxFilter.Location = new Point(3, 3);
            _textBoxFilter.Name = "_textBoxFilter";
            _textBoxFilter.PlaceholderText = "🔍 搜索文件...";
            _textBoxFilter.Size = new Size(316, 24);
            _textBoxFilter.TabIndex = 1;
            _textBoxFilter.TextChanged += TextBoxFilter_TextChanged;
            // 
            // _tabPageThumbnails
            // 
            _tabPageThumbnails.Controls.Add(_panelThumbnails);
            _tabPageThumbnails.Location = new Point(4, 26);
            _tabPageThumbnails.Name = "_tabPageThumbnails";
            _tabPageThumbnails.Padding = new Padding(3);
            _tabPageThumbnails.Size = new Size(322, 870);
            _tabPageThumbnails.TabIndex = 1;
            _tabPageThumbnails.Text = "🖼️ 缩略图";
            _tabPageThumbnails.UseVisualStyleBackColor = true;
            // 
            // _panelThumbnails
            // 
            _panelThumbnails.AutoScroll = true;
            _panelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            _panelThumbnails.Controls.Add(_flowLayoutPanelThumbnails);
            _panelThumbnails.Dock = DockStyle.Fill;
            _panelThumbnails.Location = new Point(3, 3);
            _panelThumbnails.Name = "_panelThumbnails";
            _panelThumbnails.Size = new Size(316, 864);
            _panelThumbnails.TabIndex = 0;
            // 
            // _flowLayoutPanelThumbnails
            // 
            _flowLayoutPanelThumbnails.AutoScroll = true;
            _flowLayoutPanelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            _flowLayoutPanelThumbnails.Dock = DockStyle.Fill;
            _flowLayoutPanelThumbnails.Location = new Point(0, 0);
            _flowLayoutPanelThumbnails.Name = "_flowLayoutPanelThumbnails";
            _flowLayoutPanelThumbnails.Size = new Size(316, 864);
            _flowLayoutPanelThumbnails.TabIndex = 0;
            // 
            // _tabPageTranslate
            // 
            _tabPageTranslate.Controls.Add(_groupBoxProgress);
            _tabPageTranslate.Location = new Point(4, 26);
            _tabPageTranslate.Name = "_tabPageTranslate";
            _tabPageTranslate.Padding = new Padding(3);
            _tabPageTranslate.Size = new Size(322, 870);
            _tabPageTranslate.TabIndex = 0;
            _tabPageTranslate.Text = "🌐 翻译";
            _tabPageTranslate.UseVisualStyleBackColor = true;
            // 
            // _groupBoxProgress
            // 
            _groupBoxProgress.Controls.Add(_textBoxTranslation);
            _groupBoxProgress.Controls.Add(_buttonSpeakTranslation);
            _groupBoxProgress.Controls.Add(_buttonAddToLearningContent);
            _groupBoxProgress.Controls.Add(_textBoxOriginal);
            _groupBoxProgress.Controls.Add(_buttonSpeakOriginal);
            _groupBoxProgress.Controls.Add(_checkBoxAutoTranslate);
            _groupBoxProgress.Controls.Add(_checkBoxAutoSpeak);
            _groupBoxProgress.Controls.Add(_labelTranslation);
            _groupBoxProgress.Controls.Add(_labelOriginal);
            _groupBoxProgress.Controls.Add(_buttonTranslate);
            _groupBoxProgress.Dock = DockStyle.Fill;
            _groupBoxProgress.Font = new Font("Microsoft YaHei UI", 9F);
            _groupBoxProgress.Location = new Point(3, 3);
            _groupBoxProgress.Name = "_groupBoxProgress";
            _groupBoxProgress.Size = new Size(316, 864);
            _groupBoxProgress.TabIndex = 0;
            _groupBoxProgress.TabStop = false;
            _groupBoxProgress.Text = "OCR / 翻译";
            // 
            // _textBoxTranslation
            // 
            _textBoxTranslation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxTranslation.BorderStyle = BorderStyle.FixedSingle;
            _textBoxTranslation.Location = new Point(10, 320);
            _textBoxTranslation.Multiline = true;
            _textBoxTranslation.Name = "_textBoxTranslation";
            _textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            _textBoxTranslation.Size = new Size(294, 386);
            _textBoxTranslation.TabIndex = 7;
            // 
            // _buttonSpeakTranslation
            // 
            _buttonSpeakTranslation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            _buttonSpeakTranslation.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSpeakTranslation.FlatStyle = FlatStyle.Flat;
            _buttonSpeakTranslation.Location = new Point(10, 716);
            _buttonSpeakTranslation.Name = "_buttonSpeakTranslation";
            _buttonSpeakTranslation.Size = new Size(75, 28);
            _buttonSpeakTranslation.TabIndex = 6;
            _buttonSpeakTranslation.Text = "🔊 朗读";
            _buttonSpeakTranslation.UseVisualStyleBackColor = false;
            _buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // _buttonAddToLearningContent
            // 
            _buttonAddToLearningContent.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            _buttonAddToLearningContent.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonAddToLearningContent.FlatStyle = FlatStyle.Flat;
            _buttonAddToLearningContent.Location = new Point(164, 716);
            _buttonAddToLearningContent.Name = "_buttonAddToLearningContent";
            _buttonAddToLearningContent.Size = new Size(140, 28);
            _buttonAddToLearningContent.TabIndex = 5;
            _buttonAddToLearningContent.Text = "➕ 添加到学习";
            _buttonAddToLearningContent.UseVisualStyleBackColor = false;
            _buttonAddToLearningContent.Click += ButtonAddToLearningContent_Click;
            // 
            // _textBoxOriginal
            // 
            _textBoxOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxOriginal.BorderStyle = BorderStyle.FixedSingle;
            _textBoxOriginal.Location = new Point(10, 50);
            _textBoxOriginal.Multiline = true;
            _textBoxOriginal.Name = "_textBoxOriginal";
            _textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            _textBoxOriginal.Size = new Size(294, 220);
            _textBoxOriginal.TabIndex = 4;
            // 
            // _buttonSpeakOriginal
            // 
            _buttonSpeakOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _buttonSpeakOriginal.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSpeakOriginal.FlatStyle = FlatStyle.Flat;
            _buttonSpeakOriginal.Location = new Point(229, 20);
            _buttonSpeakOriginal.Name = "_buttonSpeakOriginal";
            _buttonSpeakOriginal.Size = new Size(75, 25);
            _buttonSpeakOriginal.TabIndex = 3;
            _buttonSpeakOriginal.Text = "🔊 朗读";
            _buttonSpeakOriginal.UseVisualStyleBackColor = false;
            _buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;
            // 
            // _checkBoxAutoSpeak
            // 
            _checkBoxAutoSpeak.AutoSize = true;
            _checkBoxAutoSpeak.Checked = true;
            _checkBoxAutoSpeak.CheckState = CheckState.Checked;
            _checkBoxAutoSpeak.Location = new Point(102, 22);
            _checkBoxAutoSpeak.Name = "_checkBoxAutoSpeak";
            _checkBoxAutoSpeak.Size = new Size(107, 21);
            _checkBoxAutoSpeak.TabIndex = 8;
            _checkBoxAutoSpeak.Text = "🔊 识别后朗读";
            _checkBoxAutoSpeak.UseVisualStyleBackColor = true;
            // 
            // _labelTranslation
            // 
            _labelTranslation.AutoSize = true;
            _labelTranslation.Location = new Point(10, 284);
            _labelTranslation.Name = "_labelTranslation";
            _labelTranslation.Size = new Size(44, 17);
            _labelTranslation.TabIndex = 2;
            _labelTranslation.Text = "翻译：";
            // 
            // _labelOriginal
            // 
            _labelOriginal.AutoSize = true;
            _labelOriginal.Location = new Point(10, 24);
            _labelOriginal.Name = "_labelOriginal";
            _labelOriginal.Size = new Size(44, 17);
            _labelOriginal.TabIndex = 1;
            _labelOriginal.Text = "原文：";
            // 
            // _buttonTranslate
            // 
            _buttonTranslate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _buttonTranslate.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTranslate.FlatStyle = FlatStyle.Flat;
            _buttonTranslate.Location = new Point(229, 277);
            _buttonTranslate.Name = "_buttonTranslate";
            _buttonTranslate.Size = new Size(70, 30);
            _buttonTranslate.TabIndex = 0;
            _buttonTranslate.Text = "🌐 翻译";
            _buttonTranslate.UseVisualStyleBackColor = false;
            _buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // _tabPageBookmarksAndHighlights
            // 
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxHighlights);
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxBookmarks);
            _tabPageBookmarksAndHighlights.Location = new Point(4, 26);
            _tabPageBookmarksAndHighlights.Name = "_tabPageBookmarksAndHighlights";
            _tabPageBookmarksAndHighlights.Padding = new Padding(3);
            _tabPageBookmarksAndHighlights.Size = new Size(322, 870);
            _tabPageBookmarksAndHighlights.TabIndex = 3;
            _tabPageBookmarksAndHighlights.Text = "📑 书签/高亮";
            _tabPageBookmarksAndHighlights.UseVisualStyleBackColor = true;
            // 
            // _groupBoxHighlights
            // 
            _groupBoxHighlights.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _groupBoxHighlights.Controls.Add(_groupBoxHighlightColor);
            _groupBoxHighlights.Controls.Add(_listBoxHighlights);
            _groupBoxHighlights.Controls.Add(_highlightButtonPanel);
            _groupBoxHighlights.Location = new Point(4, 5);
            _groupBoxHighlights.Name = "_groupBoxHighlights";
            _groupBoxHighlights.Size = new Size(315, 380);
            _groupBoxHighlights.TabIndex = 0;
            _groupBoxHighlights.TabStop = false;
            _groupBoxHighlights.Text = "高亮";
            // 
            // _groupBoxHighlightColor
            // 
            _groupBoxHighlightColor.Controls.Add(_radioHighlightYellow);
            _groupBoxHighlightColor.Controls.Add(_radioHighlightGreen);
            _groupBoxHighlightColor.Controls.Add(_radioHighlightBlue);
            _groupBoxHighlightColor.Controls.Add(_radioHighlightPink);
            _groupBoxHighlightColor.Controls.Add(_radioHighlightOrange);
            _groupBoxHighlightColor.Location = new Point(10, 22);
            _groupBoxHighlightColor.Name = "_groupBoxHighlightColor";
            _groupBoxHighlightColor.Size = new Size(298, 56);
            _groupBoxHighlightColor.TabIndex = 0;
            _groupBoxHighlightColor.TabStop = false;
            _groupBoxHighlightColor.Text = "颜色";
            // 
            // _radioHighlightYellow
            // 
            _radioHighlightYellow.Appearance = Appearance.Button;
            _radioHighlightYellow.BackColor = Color.Yellow;
            _radioHighlightYellow.Checked = true;
            _radioHighlightYellow.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            _radioHighlightYellow.FlatAppearance.BorderSize = 2;
            _radioHighlightYellow.FlatAppearance.CheckedBackColor = Color.Yellow;
            _radioHighlightYellow.FlatStyle = FlatStyle.Flat;
            _radioHighlightYellow.Location = new Point(15, 20);
            _radioHighlightYellow.Name = "_radioHighlightYellow";
            _radioHighlightYellow.Size = new Size(36, 28);
            _radioHighlightYellow.TabIndex = 0;
            _radioHighlightYellow.TabStop = true;
            _radioHighlightYellow.Tag = 1;
            _radioHighlightYellow.UseVisualStyleBackColor = false;
            _radioHighlightYellow.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _radioHighlightGreen
            // 
            _radioHighlightGreen.Appearance = Appearance.Button;
            _radioHighlightGreen.BackColor = Color.LimeGreen;
            _radioHighlightGreen.FlatAppearance.BorderSize = 0;
            _radioHighlightGreen.FlatAppearance.CheckedBackColor = Color.LimeGreen;
            _radioHighlightGreen.FlatStyle = FlatStyle.Flat;
            _radioHighlightGreen.Location = new Point(60, 20);
            _radioHighlightGreen.Name = "_radioHighlightGreen";
            _radioHighlightGreen.Size = new Size(36, 28);
            _radioHighlightGreen.TabIndex = 1;
            _radioHighlightGreen.Tag = 2;
            _radioHighlightGreen.UseVisualStyleBackColor = false;
            _radioHighlightGreen.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _radioHighlightBlue
            // 
            _radioHighlightBlue.Appearance = Appearance.Button;
            _radioHighlightBlue.BackColor = Color.LightBlue;
            _radioHighlightBlue.FlatAppearance.BorderSize = 0;
            _radioHighlightBlue.FlatAppearance.CheckedBackColor = Color.LightBlue;
            _radioHighlightBlue.FlatStyle = FlatStyle.Flat;
            _radioHighlightBlue.Location = new Point(105, 20);
            _radioHighlightBlue.Name = "_radioHighlightBlue";
            _radioHighlightBlue.Size = new Size(36, 28);
            _radioHighlightBlue.TabIndex = 2;
            _radioHighlightBlue.Tag = 3;
            _radioHighlightBlue.UseVisualStyleBackColor = false;
            _radioHighlightBlue.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _radioHighlightPink
            // 
            _radioHighlightPink.Appearance = Appearance.Button;
            _radioHighlightPink.BackColor = Color.Pink;
            _radioHighlightPink.FlatAppearance.BorderSize = 0;
            _radioHighlightPink.FlatAppearance.CheckedBackColor = Color.Pink;
            _radioHighlightPink.FlatStyle = FlatStyle.Flat;
            _radioHighlightPink.Location = new Point(150, 20);
            _radioHighlightPink.Name = "_radioHighlightPink";
            _radioHighlightPink.Size = new Size(36, 28);
            _radioHighlightPink.TabIndex = 3;
            _radioHighlightPink.Tag = 4;
            _radioHighlightPink.UseVisualStyleBackColor = false;
            _radioHighlightPink.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _radioHighlightOrange
            // 
            _radioHighlightOrange.Appearance = Appearance.Button;
            _radioHighlightOrange.BackColor = Color.Orange;
            _radioHighlightOrange.FlatAppearance.BorderSize = 0;
            _radioHighlightOrange.FlatAppearance.CheckedBackColor = Color.Orange;
            _radioHighlightOrange.FlatStyle = FlatStyle.Flat;
            _radioHighlightOrange.Location = new Point(195, 20);
            _radioHighlightOrange.Name = "_radioHighlightOrange";
            _radioHighlightOrange.Size = new Size(36, 28);
            _radioHighlightOrange.TabIndex = 4;
            _radioHighlightOrange.Tag = 5;
            _radioHighlightOrange.UseVisualStyleBackColor = false;
            _radioHighlightOrange.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _listBoxHighlights
            // 
            _listBoxHighlights.BorderStyle = BorderStyle.FixedSingle;
            _listBoxHighlights.FormattingEnabled = true;
            _listBoxHighlights.Location = new Point(10, 85);
            _listBoxHighlights.Name = "_listBoxHighlights";
            _listBoxHighlights.Size = new Size(298, 223);
            _listBoxHighlights.TabIndex = 1;
            _listBoxHighlights.DoubleClick += ListBoxHighlights_DoubleClick;
            // 
            // _highlightButtonPanel
            // 
            _highlightButtonPanel.Controls.Add(_buttonRemoveHighlight);
            _highlightButtonPanel.Controls.Add(_buttonEditHighlight);
            _highlightButtonPanel.Controls.Add(_buttonUndoHighlight);
            _highlightButtonPanel.Controls.Add(_buttonBatchRemoveHighlight);
            _highlightButtonPanel.Controls.Add(_buttonExportHighlights);
            _highlightButtonPanel.Location = new Point(6, 315);
            _highlightButtonPanel.Name = "_highlightButtonPanel";
            _highlightButtonPanel.Size = new Size(302, 50);
            _highlightButtonPanel.TabIndex = 2;
            // 
            // _buttonRemoveHighlight
            // 
            _buttonRemoveHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRemoveHighlight.FlatStyle = FlatStyle.Flat;
            _buttonRemoveHighlight.Location = new Point(3, 3);
            _buttonRemoveHighlight.Name = "_buttonRemoveHighlight";
            _buttonRemoveHighlight.Size = new Size(55, 28);
            _buttonRemoveHighlight.TabIndex = 2;
            _buttonRemoveHighlight.Text = "删除";
            _buttonRemoveHighlight.UseVisualStyleBackColor = false;
            _buttonRemoveHighlight.Click += ButtonRemoveHighlight_Click;
            // 
            // _buttonEditHighlight
            // 
            _buttonEditHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonEditHighlight.FlatStyle = FlatStyle.Flat;
            _buttonEditHighlight.Location = new Point(64, 3);
            _buttonEditHighlight.Name = "_buttonEditHighlight";
            _buttonEditHighlight.Size = new Size(55, 28);
            _buttonEditHighlight.TabIndex = 3;
            _buttonEditHighlight.Text = "编辑";
            _buttonEditHighlight.UseVisualStyleBackColor = false;
            _buttonEditHighlight.Click += ButtonEditHighlight_Click;
            // 
            // _buttonUndoHighlight
            // 
            _buttonUndoHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonUndoHighlight.FlatStyle = FlatStyle.Flat;
            _buttonUndoHighlight.Location = new Point(125, 3);
            _buttonUndoHighlight.Name = "_buttonUndoHighlight";
            _buttonUndoHighlight.Size = new Size(55, 28);
            _buttonUndoHighlight.TabIndex = 1;
            _buttonUndoHighlight.Text = "撤销";
            _buttonUndoHighlight.UseVisualStyleBackColor = false;
            _buttonUndoHighlight.Click += ButtonUndoHighlight_Click;
            // 
            // _buttonBatchRemoveHighlight
            // 
            _buttonBatchRemoveHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonBatchRemoveHighlight.FlatStyle = FlatStyle.Flat;
            _buttonBatchRemoveHighlight.Location = new Point(186, 3);
            _buttonBatchRemoveHighlight.Name = "_buttonBatchRemoveHighlight";
            _buttonBatchRemoveHighlight.Size = new Size(55, 28);
            _buttonBatchRemoveHighlight.TabIndex = 0;
            _buttonBatchRemoveHighlight.Text = "批量删";
            _buttonBatchRemoveHighlight.UseVisualStyleBackColor = false;
            _buttonBatchRemoveHighlight.Click += ButtonBatchRemoveHighlight_Click;
            // 
            // _buttonExportHighlights
            // 
            _buttonExportHighlights.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonExportHighlights.FlatStyle = FlatStyle.Flat;
            _buttonExportHighlights.Location = new Point(247, 3);
            _buttonExportHighlights.Name = "_buttonExportHighlights";
            _buttonExportHighlights.Size = new Size(50, 28);
            _buttonExportHighlights.TabIndex = 4;
            _buttonExportHighlights.Text = "导出";
            _buttonExportHighlights.UseVisualStyleBackColor = false;
            _buttonExportHighlights.Click += ButtonExportHighlights_Click;
            // 
            // _groupBoxBookmarks
            // 
            _groupBoxBookmarks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _groupBoxBookmarks.Controls.Add(_listBoxBookmarks);
            _groupBoxBookmarks.Controls.Add(_textBoxBookmarkTitle);
            _groupBoxBookmarks.Controls.Add(_buttonPanel);
            _groupBoxBookmarks.Location = new Point(4, 388);
            _groupBoxBookmarks.Name = "_groupBoxBookmarks";
            _groupBoxBookmarks.Size = new Size(315, 328);
            _groupBoxBookmarks.TabIndex = 1;
            _groupBoxBookmarks.TabStop = false;
            _groupBoxBookmarks.Text = "书签";
            // 
            // _listBoxBookmarks
            // 
            _listBoxBookmarks.BorderStyle = BorderStyle.FixedSingle;
            _listBoxBookmarks.FormattingEnabled = true;
            _listBoxBookmarks.Location = new Point(10, 50);
            _listBoxBookmarks.Name = "_listBoxBookmarks";
            _listBoxBookmarks.Size = new Size(298, 274);
            _listBoxBookmarks.TabIndex = 0;
            _listBoxBookmarks.DoubleClick += ListBoxBookmarks_DoubleClick;
            // 
            // _textBoxBookmarkTitle
            // 
            _textBoxBookmarkTitle.BorderStyle = BorderStyle.FixedSingle;
            _textBoxBookmarkTitle.Location = new Point(10, 20);
            _textBoxBookmarkTitle.Name = "_textBoxBookmarkTitle";
            _textBoxBookmarkTitle.Size = new Size(200, 23);
            _textBoxBookmarkTitle.TabIndex = 1;
            // 
            // _buttonPanel
            // 
            _buttonPanel.Controls.Add(_buttonAddBookmark);
            _buttonPanel.Controls.Add(_buttonRemoveBookmark);
            _buttonPanel.Location = new Point(216, 16);
            _buttonPanel.Name = "_buttonPanel";
            _buttonPanel.Size = new Size(92, 30);
            _buttonPanel.TabIndex = 2;
            // 
            // _buttonAddBookmark
            // 
            _buttonAddBookmark.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonAddBookmark.FlatStyle = FlatStyle.Flat;
            _buttonAddBookmark.Location = new Point(3, 3);
            _buttonAddBookmark.Name = "_buttonAddBookmark";
            _buttonAddBookmark.Size = new Size(40, 28);
            _buttonAddBookmark.TabIndex = 1;
            _buttonAddBookmark.Text = "添加";
            _buttonAddBookmark.UseVisualStyleBackColor = false;
            _buttonAddBookmark.Click += ButtonAddBookmark_Click;
            // 
            // _buttonRemoveBookmark
            // 
            _buttonRemoveBookmark.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRemoveBookmark.FlatStyle = FlatStyle.Flat;
            _buttonRemoveBookmark.Location = new Point(49, 3);
            _buttonRemoveBookmark.Name = "_buttonRemoveBookmark";
            _buttonRemoveBookmark.Size = new Size(40, 28);
            _buttonRemoveBookmark.TabIndex = 0;
            _buttonRemoveBookmark.Text = "删除";
            _buttonRemoveBookmark.UseVisualStyleBackColor = false;
            _buttonRemoveBookmark.Click += ButtonRemoveBookmark_Click;
            // 
            // _buttonStrikethroughMode
            // 
            _buttonStrikethroughMode.Location = new Point(0, 0);
            _buttonStrikethroughMode.Name = "_buttonStrikethroughMode";
            _buttonStrikethroughMode.Size = new Size(75, 23);
            _buttonStrikethroughMode.TabIndex = 0;
            // 
            // _toastLabel
            // 
            _toastLabel.AutoSize = true;
            _toastLabel.BackColor = Color.FromArgb(60, 60, 60);
            _toastLabel.Font = new Font("Microsoft YaHei UI", 10F);
            _toastLabel.ForeColor = Color.White;
            _toastLabel.Location = new Point(284, 261);
            _toastLabel.Name = "_toastLabel";
            _toastLabel.Padding = new Padding(15, 8, 15, 8);
            _toastLabel.Size = new Size(30, 36);
            _toastLabel.TabIndex = 2;
            _toastLabel.TextAlign = ContentAlignment.MiddleCenter;
            _toastLabel.Visible = false;
            // 
            // _buttonTranslationToggle
            // 
            _buttonTranslationToggle.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonTranslationToggle.FlatStyle = FlatStyle.Flat;
            _buttonTranslationToggle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _buttonTranslationToggle.Location = new Point(48, 2);
            _buttonTranslationToggle.Name = "_buttonTranslationToggle";
            _buttonTranslationToggle.Size = new Size(32, 32);
            _buttonTranslationToggle.TabIndex = 1;
            _buttonTranslationToggle.Text = "译";
            _buttonTranslationToggle.UseVisualStyleBackColor = false;
            _buttonTranslationToggle.Click += ButtonTranslationToggle_Click;
            // 
            // _checkBoxAutoTranslate
            // 
            _checkBoxAutoTranslate.AutoSize = true;
            _checkBoxAutoTranslate.Location = new Point(102, 282);
            _checkBoxAutoTranslate.Name = "_checkBoxAutoTranslate";
            _checkBoxAutoTranslate.Size = new Size(107, 21);
            _checkBoxAutoTranslate.TabIndex = 8;
            _checkBoxAutoTranslate.Text = "🌐 识别后翻译";
            _checkBoxAutoTranslate.UseVisualStyleBackColor = true;
            // 
            // PdfReaderFormV2
            // 
            AllowDrop = true;
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1461, 900);
            Controls.Add(_splitContainerMain);
            Controls.Add(_toastLabel);
            Name = "PdfReaderFormV2";
            StartPosition = FormStartPosition.CenterScreen;
            DragDrop += PdfReaderFormV2_DragDrop;
            DragEnter += PdfReaderFormV2_DragEnter;
            _splitContainerMain.Panel1.ResumeLayout(false);
            _splitContainerMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)_splitContainerMain).EndInit();
            _splitContainerMain.ResumeLayout(false);
            _panelPdf.ResumeLayout(false);
            _panelNavigation.ResumeLayout(false);
            _toolbarGroupNav.ResumeLayout(false);
            _toolbarGroupNav.PerformLayout();
            _toolbarGroupView.ResumeLayout(false);
            _toolbarGroupView.PerformLayout();
            ((ISupportInitialize)_trackBarZoom).EndInit();
            _toolbarGroupMode.ResumeLayout(false);
            _toolbarGroupTools.ResumeLayout(false);
            _panelAnnotationOptions.ResumeLayout(false);
            _panelThickness.ResumeLayout(false);
            _panelColor.ResumeLayout(false);
            _statusBar.ResumeLayout(false);
            _statusBar.PerformLayout();
            ((ISupportInitialize)_pictureBoxPdf).EndInit();
            _contextMenuPdf.ResumeLayout(false);
            _ocrPanel.ResumeLayout(false);
            ((ISupportInitialize)_ocrPictureBox).EndInit();
            _pageTransitionOverlay.ResumeLayout(false);
            _panelLeftContainer.ResumeLayout(false);
            _tabControlLeft.ResumeLayout(false);
            _tabPageFiles.ResumeLayout(false);
            _tabPageFiles.PerformLayout();
            _tabPageThumbnails.ResumeLayout(false);
            _panelThumbnails.ResumeLayout(false);
            _tabPageTranslate.ResumeLayout(false);
            _groupBoxProgress.ResumeLayout(false);
            _groupBoxProgress.PerformLayout();
            _tabPageBookmarksAndHighlights.ResumeLayout(false);
            _groupBoxHighlights.ResumeLayout(false);
            _groupBoxHighlightColor.ResumeLayout(false);
            _highlightButtonPanel.ResumeLayout(false);
            _groupBoxBookmarks.ResumeLayout(false);
            _groupBoxBookmarks.PerformLayout();
            _buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }


        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // 释放Bitmap资源
                SafeDisposeImage(ref _currentPageImage);
                SafeDisposeImage(ref _secondPageImage);
                SafeDisposeImage(ref _annotationBitmap);
                SafeDisposeImage(ref _highlightBitmap);

                // 释放Graphics资源
                _annotationGraphics?.Dispose();

                // 释放Pen资源
                _pen?.Dispose();

                // 释放Manager资源
                _nightModeManager?.Dispose();
                _highlightManager?.Dispose();
                _bookmarkManager?.Dispose();
                _navigationManager?.Dispose();

                // 释放Timer资源
                _longPressTimer?.Stop();
                _longPressTimer?.Dispose();
                _pageTransitionTimer?.Stop();
                _pageTransitionTimer?.Dispose();

                // 释放容器资源
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// 安全释放Bitmap资源
        /// </summary>
        private static void SafeDisposeImage(ref Bitmap? bitmap)
        {
            if (bitmap != null)
            {
                try
                {
                    bitmap.Dispose();
                }
                catch { /* 忽略释放失败 */ }
                bitmap = null;
            }
        }

        /// <summary>
        /// 安全替换Bitmap图像，自动处理夜间模式转换和旧图像释放
        /// </summary>
        /// <param name="target">目标Bitmap引用</param>
        /// <param name="newImage">新图像</param>
        /// <returns>实际设置的图像（可能经过夜间模式转换）</returns>
        private Bitmap? SafeReplaceImage(ref Bitmap? target, Bitmap? newImage)
        {
            if (target == newImage)
            {
                return newImage;
            }

            if (newImage != null && (_nightModeManager?.IsNightMode ?? false))
            {
                try
                {
                    var inverted = new Bitmap(_nightModeManager.InvertImage(newImage));
                    SafeDisposeImage(ref target);
                    target = inverted;
                    return inverted;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invert image for night mode");
                }
            }

            SafeDisposeImage(ref target);
            target = newImage;
            return newImage;
        }

        #region 缩放预设菜单事件处理

        private void ZoomPreset50_Click(object? sender, EventArgs e) => SetZoom(50);
        private void ZoomPreset75_Click(object? sender, EventArgs e) => SetZoom(75);
        private void ZoomPreset100_Click(object? sender, EventArgs e) => SetZoom(100);
        private void ZoomPreset125_Click(object? sender, EventArgs e) => SetZoom(125);
        private void ZoomPreset150_Click(object? sender, EventArgs e) => SetZoom(150);
        private void ZoomPreset200_Click(object? sender, EventArgs e) => SetZoom(200);
        private void ZoomPresetFitWidth_Click(object? sender, EventArgs e) => FitToWidth();
        private void ZoomPresetFitHeight_Click(object? sender, EventArgs e) => FitToHeight();
        private void ZoomPresetFitPage_Click(object? sender, EventArgs e) => FitToPage();

        #endregion

        public void SetCurrentLanguage(string language)
        {
            _currentLanguage = language;
            if (ButtonLanguage != null)
            {
                ButtonLanguage.Text = language == "eng" ? "EN" : "中";
            }
        }

        public void UpdateLanguageButtonText(string text)
        {
            if (ButtonLanguage != null)
            {
                ButtonLanguage.Text = text;
            }
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        public void ShowToast(string message, int duration = 2000)
        {
            if (_toastLabel == null) return;

            _toastLabel.Text = message;
            _toastLabel.Visible = true;
            _toastLabel.Location = new Point(
                ClientSize.Width / 2 - _toastLabel.Width / 2,
                ClientSize.Height / 2 - _toastLabel.Height / 2);
            _toastLabel.BringToFront();

            Task.Delay(duration).ContinueWith(_ =>
            {
                if (_toastLabel != null && !_toastLabel.IsDisposed)
                {
                    try
                    {
                        if (InvokeRequired)
                        {
                            Invoke(() => _toastLabel.Visible = false);
                        }
                        else
                        {
                            _toastLabel.Visible = false;
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }, TaskScheduler.Default);
        }

        private void PdfReaderFormV2_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Any(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                {
                    e.Effect = DragDropEffects.Copy;
                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }

        private void PdfReaderFormV2_DragDrop(object? sender, DragEventArgs e)
        {
            try
            {
                if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                {
                    var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                    if (files != null && files.Length > 0)
                    {
                        var pdfFile = files.FirstOrDefault(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
                        if (!string.IsNullOrEmpty(pdfFile))
                        {
                            _presenter?.LoadPdf(pdfFile);
                            ShowToast($"已打开: {Path.GetFileName(pdfFile)}");
                        }
                        else
                        {
                            ShowToast("请拖拽PDF文件");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling drag drop");
                ShowError("打开文件失败: " + ex.Message);
            }
        }
    }
}
