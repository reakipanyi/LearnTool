using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Models.Config;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LearningAssistant.Forms.Pdf
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
        // 文件树右键菜单：重命名/删除文件或文件夹
        private ContextMenuStrip? _contextMenuFiles;
        private ToolStripMenuItem? _menuItemRename;
        private ToolStripMenuItem? _menuItemDelete;
        // 当前根目录（加载文件夹的共同父目录），禁止删除/重命名根目录本身
        private string _rootFolderPath = string.Empty;
        // 右键菜单操作目标：路径与是否文件夹
        private string? _contextMenuTargetPath;
        private bool _contextMenuTargetIsFolder;
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
        private SpeedSelectorControl _speedSelector;
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
        private TabPage _tabPageAnnotationSummary;
        private ListView _listViewAnnotationSummary;
        private Panel _panelLeftContainer;

        private Panel _bookmarkContainer;
        private FlowLayoutPanel _buttonPanel;
        private Panel _highlightContainer;
        private FlowLayoutPanel _highlightButtonPanel;

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
        private Button _buttonEraserMode;
        private Panel _panelAnnotationOptions;
        private TrackBar _trackBarThickness;
        private Label _labelThicknessValue;
        private Button _buttonDashStyle;
        private Button _buttonPenTypePencil;
        private Button _buttonPenTypePen;
        private Button _buttonPenTypeMarker;
        private Panel _panelColor;
        private Button _buttonColorBlue;
        private Button _buttonColorGreen;
        private Button _buttonColorOrange;
        private Button _buttonColorRed;
        private Button _buttonColorBlack;
        private Button _buttonColorWhite;
        private Button _buttonColorPurple;
        private Button _buttonColorCyan;
        private Button _buttonColorTeal;
        private Button _buttonColorPink;
        private Button _buttonColorBrown;
        private Button _buttonColorGray;
        private Button _buttonColorYellow;
        private Button _buttonColorLime;
        private Button _buttonColorVioletRed;
        private Button _buttonColorDodgerBlue;
        private Button _buttonColorMore;

        // 16色面板 + 更多颜色按钮
        private static readonly Color[] AnnotationColors = new[]
        {
            Color.Black,      // 0: 黑色
            Color.White,      // 1: 白色
            Color.RoyalBlue,  // 2: 蓝色
            Color.LimeGreen,  // 3: 绿色
            Color.Orange,     // 4: 橙色
            Color.Red,        // 5: 红色
            Color.Purple,     // 6: 紫色
            Color.Cyan,       // 7: 青色
            Color.Teal,       // 8: 蓝绿色
            Color.HotPink,    // 9: 粉色
            Color.SaddleBrown,// 10: 棕色
            Color.Gray,       // 11: 灰色
            Color.Gold,       // 12: 金色
            Color.Lime,       // 13: 亮绿
            Color.MediumVioletRed, // 14: 紫红
            Color.DodgerBlue  // 15: 亮蓝
        };
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

        /// <summary>
        /// 统一撤销栈：按时间顺序记录最近发生的操作类型（画笔或高亮）。
        /// 工具栏撤销按钮据此智能撤销"最近一次操作"，
        /// 取代原先书签面板中只针对高亮的撤销按钮。
        /// </summary>
        private readonly Stack<UndoActionKind> _unifiedUndoStack = new Stack<UndoActionKind>();

        /// <summary>
        /// 标记当前文档的缩略图是否已加载，切换到缩略图选项卡时按需加载
        /// </summary>
        private bool _thumbnailsLoaded = false;


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
        private const string AppTitle = "PDF阅读工具";
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

            InitializeFileContextMenu();
            InitializeManagers();
        }

        private void InitializeManagers()
        {
            _nightModeManager = new PdfReaderNightModeManager(_logger, this);
            _highlightManager = new PdfReaderHighlightManager(_logger, this, _highlightService, _annotationService, _eventBus);
            _bookmarkManager = new PdfReaderBookmarkManager(_logger, this, _bookmarkService);
            _navigationManager = new PdfReaderNavigationManager(_logger, this);

            // 订阅两个 Manager 的撤销动作入栈事件，统一记录到 _unifiedUndoStack，
            // 供工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
            _highlightManager.UndoActionRecorded += (s, e) => _unifiedUndoStack.Push(UndoActionKind.Highlight);
            _navigationManager.UndoActionRecorded += (s, e) => _unifiedUndoStack.Push(UndoActionKind.Stroke);

            _navigationManager.IsHighlightModeCallback = () => _highlightManager?.IsHighlightMode ?? true;
            _navigationManager.AddHighlightCallback = rect => _highlightManager?.AddHighlight(rect);
            _navigationManager.AddTextCallback = point => ShowTextAnnotationDialog(point);
            _navigationManager.UpdateHighlightLayerCallback = () => _highlightManager?.UpdateHighlightLayer();

            // 标注选中事件：双击触发编辑，单击显示选中状态
            _navigationManager.AnnotationSelected += OnAnnotationSelected;

            if (_pictureBoxPdf != null)
            {
                _pictureBoxPdf.MouseDown += _navigationManager.MouseDown;
                _pictureBoxPdf.MouseMove += _navigationManager.MouseMove;
                _pictureBoxPdf.MouseUp += _navigationManager.MouseUp;
            }

            InitializeHighlightColorButtonEvents();
            InitializeColorButtonLayout();
            InitializeToolbarButtonHoverEffects();
        }

        private void OnAnnotationSelected(AnnotationStroke stroke, int index, bool isDoubleClick)
        {
            if (isDoubleClick)
            {
                // 双击标注 -> 打开编辑
                string shapeType = stroke.ShapeType ?? string.Empty;
                if (shapeType == "Text")
                {
                    // 查找对应的文字标注并编辑
                    var texts = _presenter?.GetCurrentPageTexts().ToList();
                    if (texts != null)
                    {
                        foreach (var text in texts)
                        {
                            // 匹配位置相近的文字标注
                            if (stroke.Points.Length >= 4)
                            {
                                float nx = stroke.Points[0];
                                float ny = stroke.Points[1];
                                if (Math.Abs(text.NormalizedX - nx) < 0.01 && Math.Abs(text.NormalizedY - ny) < 0.01)
                                {
                                    var item = new PdfAnnotationItem
                                    {
                                        Id = text.Id,
                                        Type = AnnotationType.Text,
                                        PdfPath = _currentPdfPath,
                                        PageIndex = _currentPageIndex,
                                        Text = text.Content,
                                        ColorArgb = text.ColorArgb,
                                        FontSize = text.FontSize,
                                        NormalizedX = text.NormalizedX,
                                        NormalizedY = text.NormalizedY
                                    };
                                    EditTextAnnotation(item);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // 形状标注 -> 打开属性编辑面板
                    ShowAnnotationPropertiesPanel(stroke);
                }
            }
            else
            {
                // 单击选中 -> 更新UI状态，显示选中信息
                UpdateStatusBar();
                // 如果颜色面板可见，同步选中标注的颜色
                SyncSelectionColorToPanel(stroke);
            }
        }

        private void ShowAnnotationPropertiesPanel(AnnotationStroke stroke)
        {
            if (stroke == null) return;

            // 使用现有的颜色面板来显示当前选中标注的颜色
            var currentColor = Color.FromArgb(stroke.ColorArgb);
            SetAnnotationColor(currentColor);
            SetAnnotationThicknessFromSlider();
            if (_trackBarThickness != null)
                _trackBarThickness.Value = (int)Math.Round(Math.Max(1, Math.Min(20, stroke.Thickness)));

            ShowToast($"已选中标注: {stroke.ShapeType ?? "画笔"} 颜色: #{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2} 粗细: {stroke.Thickness:F0}");
        }

        private void SyncSelectionColorToPanel(AnnotationStroke stroke)
        {
            if (stroke == null) return;
            var color = Color.FromArgb(stroke.ColorArgb);
            // 在颜色面板中高亮对应的颜色
            UpdateAnnotationColorSelection(color);
        }

        private void InitializeToolbarButtonHoverEffects()
        {
            var toolbarButtons = new[]
            {
                _buttonPrev, _buttonNext,
                _buttonZoomOut, _buttonZoomIn, _buttonResetView,
                _buttonRotate, _buttonLockView, _buttonNightMode,
                _buttonDualPage, _buttonFullscreen,
                _buttonSelectMode, _buttonEraserMode, _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenTypePencil, _buttonPenTypePen, _buttonPenTypeMarker, _buttonMosaicMode, _buttonTextMode,
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

        public void ApplyTtsConfig(TtsConfig ttsConfig)
        {
            if (_speedSelector != null)
            {
                _speedSelector.TtsConfig = ttsConfig;
            }
        }
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
        public SpeedSelectorControl? SpeedSelector => _speedSelector;

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

        IHighlightService? IPdfReaderFormAccess.HighlightService => _highlightService;

        public Button? ButtonLanguage => null;


        public void OnSelectOcrClicked() => SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        public void OnTranslateClicked() => TranslateClicked?.Invoke(this, EventArgs.Empty);

        #endregion

        private void PdfReaderFormV2_Load(object? sender, EventArgs e)
        {
            _tabControlLeft.SelectedIndexChanged += TabControlLeft_SelectedIndexChanged;

            AdjustPanelPdfSize();
            _presenter?.LoadLastSessionAndRestore();
            UpdateStatusBar();
            UpdateAnnotationColorSelection(Color.Black);
            if (_trackBarThickness != null)
            {
                _trackBarThickness.Value = 3;
                _labelThicknessValue.Text = "3px";
            }
            _navigationManager?.SetPenWidth(3f);
            UpdateDashStyleButtonState();
            UpdatePenTypeButtonState();
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
                    case Keys.E:
                        SetAnnotationToolMode(AnnotationToolMode.Eraser);
                        e.Handled = true;
                        break;
                    case Keys.R:
                        SetAnnotationToolMode(AnnotationToolMode.Rectangle);
                        e.Handled = true;
                        break;
                    case Keys.L:
                        if (e.Shift)
                        {
                            SetAnnotationToolMode(AnnotationToolMode.LaserPointer);
                            e.Handled = true;
                        }
                        else
                        {
                            SetAnnotationToolMode(AnnotationToolMode.Ellipse);
                            e.Handled = true;
                        }
                        break;
                    case Keys.A:
                        SetAnnotationToolMode(AnnotationToolMode.Arrow);
                        e.Handled = true;
                        break;
                    case Keys.M:
                        SetAnnotationToolMode(AnnotationToolMode.Mosaic);
                        e.Handled = true;
                        break;
                    case Keys.T:
                        if (!e.Control)
                        {
                            SetAnnotationToolMode(AnnotationToolMode.Text);
                            e.Handled = true;
                        }
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
                        else
                        {
                            SetAnnotationToolMode(AnnotationToolMode.Select);
                            e.Handled = true;
                        }
                        break;
                    case Keys.P:
                        if (e.Control)
                        {
                            _presenter?.PrintPdf();
                            e.Handled = true;
                        }
                        else
                        {
                            SetAnnotationToolMode(AnnotationToolMode.Pen);
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
            _thumbnailsLoaded = false;
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
            // 基于全量文件列表计算根目录，作为右键"删除/重命名"的刷新基准（禁止操作根目录本身）
            _rootFolderPath = _allFiles.Count > 0 ? FindCommonRootDirectory(_allFiles) : string.Empty;
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

            // 添加"最近打开"节点
            if (string.IsNullOrEmpty(filter) && _presenter != null)
            {
                var recentFiles = _presenter.GetRecentFiles();
                if (recentFiles.Count > 0)
                {
                    var recentNode = new TreeNode("最近打开") { Tag = "__RECENT_HEADER__" };
                    recentNode.NodeFont = new Font(_treeViewFiles.Font, FontStyle.Bold);
                    _treeViewFiles.Nodes.Add(recentNode);

                    foreach (var recentFile in recentFiles)
                    {
                        var fileNode = new TreeNode(Path.GetFileName(recentFile))
                        {
                            Tag = recentFile
                        };
                        recentNode.Nodes.Add(fileNode);
                    }

                    recentNode.Expand();
                }
            }

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
                                // 存储文件夹完整路径，供右键菜单重命名/删除使用
                                folderNode.Tag = currentPath;
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
            UpdateStatusBar();
        }

        public void SetPageText(int pageIndex, string text)
        {
        }

        /// <summary>
        /// 显示PDF页面图像
        /// </summary>
        /// <param name="bmp">要显示的Bitmap图像</param>
        public void DisplayImage(byte[] imageData)
        {
            Bitmap bmp = new Bitmap(new MemoryStream(imageData));
            try
            {
                CleanupAnnotationBitmap();
                CleanupHighlightLayer();

                Bitmap imageToDisplay = bmp;

                if (_rotationAngle != 0)
                {
                    imageToDisplay = RotateBitmap(imageToDisplay, _rotationAngle);
                }

                SafeDisposeImage(ref _currentPageImage);
                _currentPageImage = imageToDisplay;

                _pictureBoxPdf.Image = null;

                // 同步重建高亮图层（数据已在内存缓存中，无需磁盘IO）
                _highlightManager?.UpdateHighlightLayer();

                _pictureBoxPdf.Invalidate();

                // 标注位图构建较重，放到后台线程
                int pageIndexAtDisplay = _currentPageIndex;
                string pdfPathAtDisplay = _currentPdfPath;
                _ = Task.Run(() => LoadAnnotationsAsync(pageIndexAtDisplay, pdfPathAtDisplay));
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

        void IPdfReaderFormAccess.DisplayImage(Bitmap bmp)
        {
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            DisplayImage(ms.ToArray());
        }

        private async Task LoadAnnotationsAsync(int pageIndex, string pdfPath)
        {
            try
            {
                int imgW = 0, imgH = 0;
                try
                {
                    if (_currentPageImage != null)
                    {
                        imgW = _currentPageImage.Width;
                        imgH = _currentPageImage.Height;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                Bitmap? annotationBmp = null;
                if (_presenter != null && !string.IsNullOrEmpty(pdfPath) && imgW > 0 && imgH > 0)
                {
                    var annotationBytes = _presenter.LoadAnnotationForPage(pdfPath, pageIndex, imgW, imgH);
                    if (annotationBytes != null)
                        annotationBmp = new Bitmap(new MemoryStream(annotationBytes));
                }

                if (IsDisposed || _pictureBoxPdf.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    if (_currentPageIndex != pageIndex || _currentPdfPath != pdfPath)
                        return;
                    if (_currentPageImage == null) return;

                    try
                    {
                        _navigationManager?.ApplyLoadedAnnotationBitmap(annotationBmp);
                        _pictureBoxPdf.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying async-loaded annotations");
                    }
                    finally
                    {
                        annotationBmp?.Dispose();
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in async annotation load");
            }
        }

        /// <summary>
        /// 后台加载双页模式第二页的标注图层（笔触）
        /// </summary>
        private async Task LoadSecondAnnotationsAsync(int pageIndex, string pdfPath)
        {
            try
            {
                int imgW = 0, imgH = 0;
                try
                {
                    if (_secondPageImage != null)
                    {
                        imgW = _secondPageImage.Width;
                        imgH = _secondPageImage.Height;
                    }
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                Bitmap? annotationBmp = null;
                if (_presenter != null && !string.IsNullOrEmpty(pdfPath) && imgW > 0 && imgH > 0)
                {
                    var annotationBytes = _presenter.LoadAnnotationForPage(pdfPath, pageIndex, imgW, imgH);
                    if (annotationBytes != null)
                        annotationBmp = new Bitmap(new MemoryStream(annotationBytes));
                }

                if (IsDisposed || _pictureBoxPdf.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    if (_currentPageIndex + 1 != pageIndex || _currentPdfPath != pdfPath)
                        return;
                    if (_secondPageImage == null) return;

                    try
                    {
                        _navigationManager?.ApplySecondLoadedAnnotationBitmap(annotationBmp);
                        _pictureBoxPdf.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying second page annotations");
                    }
                    finally
                    {
                        annotationBmp?.Dispose();
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in async second annotation load");
            }
        }

        /// <summary>
        /// 设置双页模式下第二页的图像
        /// </summary>
        /// <param name="bmp">第二页的Bitmap图像，null则清除</param>
        public void SetSecondPageImage(byte[]? imageData)
        {
            Bitmap? bmp = imageData != null ? new Bitmap(new MemoryStream(imageData)) : null;
            try
            {
                SafeReplaceImage(ref _secondPageImage, bmp);

                // 清理第二页标注缓存
                _navigationManager?.CleanupSecondAnnotationBitmap();

                if (_highlightManager != null)
                {
                    if (bmp == null)
                    {
                        _highlightManager.CleanupSecondHighlightLayer();
                    }
                    else
                    {
                        int pageIndexAtSet = _currentPageIndex + 1;
                        string pdfPathAtSet = _currentPdfPath;
                        int imgW = bmp.Width;
                        int imgH = bmp.Height;
                        _ = Task.Run(() => UpdateSecondHighlightLayerAsync(pageIndexAtSet, pdfPathAtSet, imgW, imgH));

                        // 同时加载第二页的标注（笔触）
                        _ = Task.Run(() => LoadSecondAnnotationsAsync(pageIndexAtSet, pdfPathAtSet));
                    }
                }

                _pictureBoxPdf.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetSecondPageImage");
            }
        }

        private async Task UpdateSecondHighlightLayerAsync(int pageIndex, string pdfPath, int imgW, int imgH)
        {
            try
            {
                if (_highlightService == null || string.IsNullOrEmpty(pdfPath)) return;

                var highlights = _highlightService.GetHighlightsForPage(pdfPath, pageIndex);

                if (IsDisposed || _pictureBoxPdf.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    if (_currentPageIndex + 1 != pageIndex || _currentPdfPath != pdfPath) return;
                    if (_secondPageImage == null) return;

                    try
                    {
                        _highlightManager?.UpdateSecondHighlightLayer(pageIndex, _secondPageImage);
                        _pictureBoxPdf.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error applying second highlight layer");
                    }
                }));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error in async second highlight load");
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

        public void AddThumbnail(int pageIndex, byte[] thumbnailData)
        {
            // PDF 模式：directoryPath 为空，保持原有行为不分组
            AddThumbnail(pageIndex, thumbnailData, string.Empty);
        }

        public void AddThumbnail(int pageIndex, byte[] thumbnailData, string directoryPath)
        {
            Image thumbnail = new Bitmap(new MemoryStream(thumbnailData));
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

        public byte[]? GetCurrentImage()
        {
            return BitmapToBytes(_currentPageImage);
        }

        private static byte[]? BitmapToBytes(Bitmap? bmp)
        {
            if (bmp == null) return null;
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        public RectInt? GetSelectionRect()
        {
            var rect = _lastSelectionRect;
            return rect.HasValue ? new RectInt(rect.Value.X, rect.Value.Y, rect.Value.Width, rect.Value.Height) : null;
        }

        public RectInt GetDisplayRect()
        {
            var rect = _pictureBoxPdf.ClientRectangle;
            return new RectInt(rect.X, rect.Y, rect.Width, rect.Height);
        }

        /// <summary>
        /// 获取图片在PictureBox中的显示矩形（考虑缩放和偏移）
        /// </summary>
        /// <returns>图片显示区域的矩形坐标</returns>
        public RectInt GetImageDisplayRect()
        {
            try
            {
                if (_currentPageImage == null)
                {
                    var r = _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
                    return new RectInt(r.X, r.Y, r.Width, r.Height);
                }

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
                    var r = _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
                    return new RectInt(r.X, r.Y, r.Width, r.Height);
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

                return new RectInt(displayX, displayY, displayWidth, displayHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetImageDisplayRect");
                var r = _pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
                return new RectInt(r.X, r.Y, r.Width, r.Height);
            }
        }

        Rectangle IPdfReaderFormAccess.GetImageDisplayRect() => ToRectangle(GetImageDisplayRect());

        private static Rectangle ToRectangle(RectInt rect) => new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);

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

            var imgRect = ToRectangle(GetImageDisplayRect());
            return (_currentPageIndex, imgRect, _currentPageImage);
        }

        public void ShowOcrOverlay(byte[]? imageData)
        {
            Bitmap? image = imageData != null ? new Bitmap(new MemoryStream(imageData)) : null;
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
            _navigationManager?.SetPenType("Pen");
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
            var allButtons = new[]
            {
                _buttonColorBlue, _buttonColorGreen, _buttonColorOrange, _buttonColorRed,
                _buttonColorBlack, _buttonColorWhite, _buttonColorPurple, _buttonColorCyan,
                _buttonColorTeal, _buttonColorPink, _buttonColorBrown, _buttonColorGray,
                _buttonColorYellow, _buttonColorLime, _buttonColorVioletRed, _buttonColorDodgerBlue
            };

            for (int i = 0; i < allButtons.Length && i < AnnotationColors.Length; i++)
            {
                if (allButtons[i] != null)
                {
                    bool isMatch = color.ToArgb() == AnnotationColors[i].ToArgb();
                    // 选中态：显示品牌色外圈，模拟参考图片中的选中环效果
                    allButtons[i].FlatAppearance.BorderSize = isMatch ? 3 : 0;
                    allButtons[i].FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
                }
            }
        }

        /// <summary>
        /// 初始化颜色按钮布局（移出 InitializeComponent 以支持窗体设计器预览）
        /// </summary>
        private void InitializeColorButtonLayout()
        {
            var colorButtons = new[] {
                _buttonColorBlack, _buttonColorWhite, _buttonColorBlue, _buttonColorGreen,
                _buttonColorOrange, _buttonColorRed, _buttonColorPurple, _buttonColorCyan,
                _buttonColorTeal, _buttonColorPink, _buttonColorBrown, _buttonColorGray,
                _buttonColorYellow, _buttonColorLime, _buttonColorVioletRed, _buttonColorDodgerBlue
            };
            var colorHandlers = new EventHandler[] {
                ButtonColorBlack_Click, ButtonColorWhite_Click, ButtonColorBlue_Click, ButtonColorGreen_Click,
                ButtonColorOrange_Click, ButtonColorRed_Click, ButtonColorPurple_Click, ButtonColorCyan_Click,
                ButtonColorTeal_Click, ButtonColorPink_Click, ButtonColorBrown_Click, ButtonColorGray_Click,
                ButtonColorYellow_Click, ButtonColorLime_Click, ButtonColorVioletRed_Click, ButtonColorDodgerBlue_Click
            };

            const int columns = 8;
            for (int i = 0; i < colorButtons.Length; i++)
            {
                var btn = colorButtons[i];
                btn.BackColor = AnnotationColors[i];
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;
                int row = i / columns;
                int col = i % columns;
                btn.Location = new Point(2 + col * 28, 4 + row * 26);
                btn.Size = new Size(22, 22);
                btn.Name = $"_buttonColor{i}";
                btn.TabIndex = i;
                btn.UseVisualStyleBackColor = false;
                btn.Click += colorHandlers[i];
                MakeButtonCircular(btn);
                if (AnnotationColors[i] == Color.White)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                    btn.FlatAppearance.BorderSize = 1;
                }
            }
        }

        /// <summary>
        /// 将按钮设置为圆形 Region
        /// </summary>
        private static void MakeButtonCircular(Button button)
        {
            if (button == null || button.Width <= 0 || button.Height <= 0) return;
            try
            {
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddEllipse(0, 0, button.Width, button.Height);
                button.Region = new Region(path);
                path.Dispose();
            }
            catch
            {
                // 忽略 Region 设置失败
            }
        }

        private void SetAnnotationThicknessFromSlider()
        {
            if (_trackBarThickness != null)
            {
                float width = _trackBarThickness.Value;
                _navigationManager?.SetPenWidth(width);
                if (_labelThicknessValue != null)
                    _labelThicknessValue.Text = $"{width}px";
            }
        }

        private void ShowAnnotationOptions(bool show)
        {
            if (_panelAnnotationOptions != null)
                _panelAnnotationOptions.Visible = show;
        }

        private void SyncAnnotationOptionsToManager()
        {
            if (_navigationManager == null) return;

            // 同步虚线状态
            UpdateDashStyleButtonState();

            // 同步画笔类型状态
            UpdatePenTypeButtonState();

            // 同步粗细滑块和标签
            if (_trackBarThickness != null && _labelThicknessValue != null)
            {
                int width = (int)Math.Round(_navigationManager.PenWidth);
                width = Math.Max(1, Math.Min(20, width));
                _trackBarThickness.Value = width;
                _labelThicknessValue.Text = $"{width}px";
            }
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

        private async void ButtonUndoAnnotation_Click(object? sender, EventArgs e)
        {
            await UndoLastActionAsync();
            RefreshHighlightList();
        }

        /// <summary>
        /// 按时间顺序智能撤销最近一次操作。
        /// 弹出统一撤销栈顶的操作类型，根据类型调用对应 Manager 的撤销方法：
        /// - <see cref="UndoActionKind.Stroke"/>：撤销画笔/标注笔画
        /// - <see cref="UndoActionKind.Highlight"/>：撤销高亮添加/删除
        /// 若对应 Manager 内部栈已空（与统一栈不同步的边界情况），
        /// 自动跳过该记录并继续尝试下一个，保证用户感知一致。
        /// </summary>
        private async Task UndoLastActionAsync()
        {
            while (_unifiedUndoStack.Count > 0)
            {
                var kind = _unifiedUndoStack.Pop();

                if (kind == UndoActionKind.Stroke)
                {
                    if (_navigationManager != null && _navigationManager.CanUndoStroke())
                    {
                        _navigationManager.UndoStroke();
                        return;
                    }
                    // 内部画笔栈已空，跳过此记录继续尝试下一个
                }
                else if (kind == UndoActionKind.Highlight)
                {
                    if (_highlightManager != null && _highlightManager.CanUndoHighlight())
                    {
                        await _highlightManager.UndoHighlightAsync();
                        return;
                    }
                    // 内部高亮栈已空，跳过此记录继续尝试下一个
                }
            }

            _logger?.LogInformation("UndoLastAction: 统一撤销栈为空，无可撤销的操作");
            ShowMessage("没有可撤销的操作", "提示");
        }

        private void ButtonClearAllAnnotations_Click(object? sender, EventArgs e)
        {
            if (_presenter == null || string.IsNullOrEmpty(_currentPdfPath)) return;

            if (!ShowConfirm("确定要清除当前PDF的所有笔划标注吗？此操作不可撤销。", "清空确认"))
                return;

            _navigationManager?.ClearAllStrokes();
            _ = _presenter.RenderAndDisplayCurrentPageAsync();
            UpdateStatusBar();
        }

        private void ButtonSelectMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Select);
        }

        private void ButtonEraserMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Eraser);
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

            if (showOptions)
            {
                SyncAnnotationOptionsToManager();

                // 根据模式显示/隐藏第一排的画笔类型按钮
                bool isPenMode = mode == AnnotationToolMode.Pen;

                // 根据模式显示/隐藏第二排的控件
                bool isShapeMode = mode == AnnotationToolMode.Rectangle || mode == AnnotationToolMode.Ellipse;
                // 虚线实线切换：仅矩形/椭圆需要
                if (_buttonDashStyle != null)
                    _buttonDashStyle.Visible = isShapeMode;
            }

            UpdateToolButtonState(_buttonSelectMode, mode == AnnotationToolMode.Select);
            UpdateToolButtonState(_buttonEraserMode, mode == AnnotationToolMode.Eraser);
            UpdateToolButtonState(_buttonHighlightMode, mode == AnnotationToolMode.Highlight);
            UpdateToolButtonState(_buttonRectangleMode, mode == AnnotationToolMode.Rectangle);
            UpdateToolButtonState(_buttonEllipseMode, mode == AnnotationToolMode.Ellipse);
            UpdateToolButtonState(_buttonArrowMode, mode == AnnotationToolMode.Arrow);
            UpdateToolButtonState(_buttonMosaicMode, mode == AnnotationToolMode.Mosaic);
            UpdateToolButtonState(_buttonTextMode, mode == AnnotationToolMode.Text);
            UpdatePenTypeButtonState();

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
            if (isActive)
            {
                // 选中态：浅蓝背景 + 品牌色边框，类似参考图片的 active 风格
                button.BackColor = Color.FromArgb(230, 244, 255);
                button.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
                button.FlatAppearance.BorderSize = 2;
                button.Font = new Font(button.Font, FontStyle.Bold);
            }
            else
            {
                // 非选中态：透明背景 + 浅灰边框
                button.BackColor = Color.Transparent;
                button.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
                button.FlatAppearance.BorderSize = 1;
                button.Font = new Font(button.Font, FontStyle.Regular);
            }
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
                    var imgRect = ToRectangle(GetImageDisplayRect());
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
                _buttonSelectMode, _buttonEraserMode, _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenTypePencil, _buttonPenTypePen, _buttonPenTypeMarker, _buttonMosaicMode, _buttonTextMode,
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
                g.DrawImage(_currentPageImage, ToRectangle(GetImageDisplayRect()));
            }
        }

        private void DrawSelectionRect(Graphics g)
        {
            if (_navigationManager == null || !_navigationManager.LastSelectionRect.HasValue) return;

            var rect = _navigationManager.LastSelectionRect.Value;
            var isHighlightMode = _navigationManager.IsHighlightModeCallback?.Invoke() ?? true;

            if (isHighlightMode)
            {
                var color = HighlightService.GetHighlightColor(_highlightManager?.CurrentHighlightColor ?? _currentHighlightColor).ToColor();
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
                _navigationManager.DrawAnnotations(g, ToRectangle(GetImageDisplayRect()));
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
                            _highlightManager.DrawSecondHighlightsFromLayer(
                                g,
                                rightRect,
                                secondPageIndex);
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

        private void TrackBarThickness_Scroll(object? sender, EventArgs e)
        {
            if (_trackBarThickness != null)
            {
                float width = _trackBarThickness.Value;
                _navigationManager?.SetPenWidth(width);
                if (_labelThicknessValue != null)
                    _labelThicknessValue.Text = $"{width}px";
            }
        }

        private void ButtonDashStyle_Click(object? sender, EventArgs e)
        {
            if (_navigationManager == null) return;
            bool newDashed = !_navigationManager.IsDashed;
            _navigationManager.SetDashStyle(newDashed);
            UpdateDashStyleButtonState();
        }

        private void UpdateDashStyleButtonState()
        {
            if (_buttonDashStyle == null || _navigationManager == null) return;
            bool isDashed = _navigationManager.IsDashed;
            _buttonDashStyle.Text = isDashed ? "┅" : "—";
            _buttonDashStyle.FlatAppearance.BorderColor = isDashed ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            _buttonDashStyle.FlatAppearance.BorderSize = isDashed ? 2 : 1;
            _buttonDashStyle.BackColor = isDashed ? Color.FromArgb(230, 244, 255) : Color.Transparent;
            _toolTip.SetToolTip(_buttonDashStyle, isDashed ? "虚线 (点击切换实线)" : "实线 (点击切换虚线)");
        }

        private void UpdatePenTypeButtonState()
        {
            if (_navigationManager == null) return;
            string penType = _navigationManager.PenType;
            UpdateToolButtonState(_buttonPenTypePencil, penType == "Pencil");
            UpdateToolButtonState(_buttonPenTypePen, penType == "Pen");
            UpdateToolButtonState(_buttonPenTypeMarker, penType == "Marker");
        }

        private void ButtonPenTypePencil_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Pencil");
            SetAnnotationToolMode(AnnotationToolMode.Pen);
        }

        private void ButtonPenTypePen_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Pen");
            SetAnnotationToolMode(AnnotationToolMode.Pen);
        }

        private void ButtonPenTypeMarker_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Marker");
            SetAnnotationToolMode(AnnotationToolMode.Pen);
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

        private void ButtonColorPurple_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Purple);
        }

        private void ButtonColorCyan_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Cyan);
        }

        private void ButtonColorTeal_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Teal);
        }

        private void ButtonColorPink_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.HotPink);
        }

        private void ButtonColorBrown_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.SaddleBrown);
        }

        private void ButtonColorGray_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Gray);
        }

        private void ButtonColorYellow_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Gold);
        }

        private void ButtonColorLime_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.Lime);
        }

        private void ButtonColorVioletRed_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.MediumVioletRed);
        }

        private void ButtonColorDodgerBlue_Click(object? sender, EventArgs e)
        {
            SetAnnotationColor(Color.DodgerBlue);
        }

        private void ButtonColorMore_Click(object? sender, EventArgs e)
        {
            using var colorDialog = new ColorDialog();
            colorDialog.Color = _navigationManager?.PenColor ?? Color.Black;
            colorDialog.FullOpen = true;
            if (colorDialog.ShowDialog(this) == DialogResult.OK)
            {
                SetAnnotationColor(colorDialog.Color);
            }
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
            // 文件夹节点不触发 PDF 加载（其 Tag 为目录路径，非 PDF 文件）
            if (e.Node != null && string.Equals(e.Node.ImageKey, "Folder", StringComparison.Ordinal))
                return;

            FileSelected?.Invoke(this, EventArgs.Empty);
        }

        #region 文件树右键菜单（重命名/删除）

        /// <summary>
        /// 初始化文件树右键菜单：包含"重命名"和"删除"两项。
        /// </summary>
        private void InitializeFileContextMenu()
        {
            _contextMenuFiles = new ContextMenuStrip();
            _menuItemRename = new ToolStripMenuItem("重命名");
            _menuItemDelete = new ToolStripMenuItem("删除");
            _menuItemRename.Click += MenuItemRename_Click;
            _menuItemDelete.Click += MenuItemDelete_Click;
            _contextMenuFiles.Items.AddRange(new ToolStripItem[] { _menuItemRename, _menuItemDelete });

            // 右键选中节点并弹出菜单
            _treeViewFiles.NodeMouseClick += TreeViewFiles_NodeMouseClick;
        }

        /// <summary>
        /// 右键点击文件树节点：选中目标节点、记录路径与类型、弹出菜单。
        /// 根目录本身禁止操作（不弹菜单）。
        /// </summary>
        private void TreeViewFiles_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            if (_contextMenuFiles == null) return;

            // 仅当节点携带路径 Tag 时才视为可操作目标
            if (e.Node?.Tag is not string targetPath || string.IsNullOrEmpty(targetPath))
                return;

            // 禁止操作根目录本身（避免误删整个加载的文件夹）
            if (!string.IsNullOrEmpty(_rootFolderPath) &&
                string.Equals(targetPath, _rootFolderPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _treeViewFiles.SelectedNode = e.Node;
            _contextMenuTargetPath = targetPath;
            // 文件夹节点 ImageKey == "Folder"；文件节点无此 ImageKey
            _contextMenuTargetIsFolder = string.Equals(e.Node.ImageKey, "Folder", StringComparison.Ordinal);

            // 更新菜单文案
            _menuItemRename!.Text = _contextMenuTargetIsFolder ? "重命名文件夹" : "重命名文件";
            _menuItemDelete!.Text = _contextMenuTargetIsFolder ? "删除文件夹" : "删除文件";

            _contextMenuFiles.Show(_treeViewFiles, e.Location);
        }

        /// <summary>
        /// 重命名当前右键目标（文件或文件夹）。
        /// 文件保留原扩展名；文件夹直接使用输入名。新名与旧名相同或为空则取消。
        /// </summary>
        private void MenuItemRename_Click(object? sender, EventArgs e)
        {
            var targetPath = _contextMenuTargetPath;
            var isFolder = _contextMenuTargetIsFolder;
            _contextMenuTargetPath = null;

            if (string.IsNullOrEmpty(targetPath)) return;
            if (!PathExists(targetPath, isFolder))
            {
                MessageBox.Show(this, "目标已不存在，可能已被移动或删除。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshFileListAfterFsChange();
                return;
            }

            string oldName = Path.GetFileName(targetPath);
            string parentDir = Path.GetDirectoryName(targetPath) ?? string.Empty;
            string oldExt = isFolder ? string.Empty : Path.GetExtension(targetPath);

            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"请输入新的{GetTargetTypeName(isFolder)}名称：",
                "重命名",
                oldName);

            if (string.IsNullOrWhiteSpace(input)) return;
            string newName = input.Trim();

            // 文件：若用户未带扩展名则自动补原扩展名
            if (!isFolder && !string.IsNullOrEmpty(oldExt) &&
                !newName.EndsWith(oldExt, StringComparison.OrdinalIgnoreCase))
            {
                newName = newName + oldExt;
            }

            if (string.Equals(newName, oldName, StringComparison.Ordinal))
            {
                return; // 名字未变化
            }

            string newPath = Path.Combine(parentDir, newName);
            if (PathExists(newPath, isFolder) || (!isFolder && File.Exists(newPath)) || (isFolder && Directory.Exists(newPath)))
            {
                MessageBox.Show(this, $"已存在同名{GetTargetTypeName(isFolder)}，请使用其他名称。", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 若目标包含当前打开的 PDF（文件本身或其所在文件夹被重命名），先释放文件句柄避免"文件被占用"
                if (ShouldReleaseCurrentDocument(targetPath, isFolder))
                {
                    _presenter?.CloseCurrentDocument();
                    var prevImg = _pictureBoxPdf.Image;
                    _pictureBoxPdf.Image = null;
                    prevImg?.Dispose();
                    ClearThumbnails();
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                    GC.WaitForPendingFinalizers();
                }

                if (isFolder)
                    MoveFolderWithRetry(targetPath, newPath);
                else
                    MoveFileWithRetry(targetPath, newPath);

                // 若重命名的是当前打开的 PDF，同步更新引用
                if (!isFolder && !string.IsNullOrEmpty(_currentPdfPath) &&
                    string.Equals(_currentPdfPath, targetPath, StringComparison.OrdinalIgnoreCase))
                {
                    _currentPdfPath = newPath;
                }

                // 若重命名的是当前 PDF 所在文件夹，把 _currentPdfPath 前缀替换为新路径
                if (isFolder && !string.IsNullOrEmpty(_currentPdfPath) &&
                    _currentPdfPath.StartsWith(targetPath + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    _currentPdfPath = newPath + _currentPdfPath.Substring(targetPath.Length);
                }

                _logger?.LogInformation("Renamed {Type} \"{Old}\" -> \"{New}\"", isFolder ? "folder" : "file", targetPath, newPath);
                RefreshFileListAfterFsChange();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to rename \"{Path}\"", targetPath);
                MessageBox.Show(this, $"重命名失败：{ex.Message}", "重命名", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 删除当前右键目标（文件或文件夹）。文件夹递归删除，需二次确认。
        /// </summary>
        private void MenuItemDelete_Click(object? sender, EventArgs e)
        {
            var targetPath = _contextMenuTargetPath;
            var isFolder = _contextMenuTargetIsFolder;
            _contextMenuTargetPath = null;

            if (string.IsNullOrEmpty(targetPath)) return;
            if (!PathExists(targetPath, isFolder))
            {
                MessageBox.Show(this, "目标已不存在，可能已被删除。", "删除", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshFileListAfterFsChange();
                return;
            }

            string prompt = isFolder
                ? $"确定要删除文件夹 \"{Path.GetFileName(targetPath)}\" 及其所有内容吗？\n\n该操作不可恢复！"
                : $"确定要删除文件 \"{Path.GetFileName(targetPath)}\" 吗？\n\n该操作不可恢复！";
            if (MessageBox.Show(this, prompt, "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                // 若目标包含当前打开的 PDF（文件本身或其所在文件夹被删除），先释放文件句柄避免"文件被占用"
                if (ShouldReleaseCurrentDocument(targetPath, isFolder))
                {
                    _presenter?.CloseCurrentDocument();
                    // 显式 dispose 当前页面位图，避免 Image 关联的 SafeFileHandle 延迟回收仍锁定文件
                    var prevImg = _pictureBoxPdf.Image;
                    _pictureBoxPdf.Image = null;
                    prevImg?.Dispose();
                    ClearThumbnails();
                    // 强制回收并等待终结器执行，彻底释放 PDFium/图片可能残留的非托管句柄
                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                    GC.WaitForPendingFinalizers();
                }

                if (isFolder)
                    DeleteFolderWithRetry(targetPath);
                else
                    DeleteFileWithRetry(targetPath);

                // 若删除的是当前打开的 PDF（或其所在文件夹），清空引用并提示
                if (ShouldReleaseCurrentDocument(targetPath, isFolder))
                {
                    _currentPdfPath = string.Empty;
                    MessageBox.Show(this, "当前打开的文件已被删除，请从左侧列表选择其他文件。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                _logger?.LogInformation("Deleted {Type} \"{Path}\"", isFolder ? "folder" : "file", targetPath);
                RefreshFileListAfterFsChange();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to delete \"{Path}\"", targetPath);
                MessageBox.Show(this, $"删除失败：{ex.Message}", "删除", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 文件系统变更后重新扫描根目录刷新文件树。
        /// </summary>
        private void RefreshFileListAfterFsChange()
        {
            if (string.IsNullOrEmpty(_rootFolderPath) || !Directory.Exists(_rootFolderPath))
            {
                // 根目录已不存在（极端情况），清空列表
                _allFiles.Clear();
                _rootFolderPath = string.Empty;
                UpdateFileListDisplay();
                return;
            }
            _presenter?.LoadFolder(_rootFolderPath);
        }

        private static bool PathExists(string path, bool isFolder)
            => isFolder ? Directory.Exists(path) : File.Exists(path);

        private static string GetTargetTypeName(bool isFolder) => isFolder ? "文件夹" : "文件";

        // 删除/重命名的重试次数与间隔（毫秒）。"只读属性+句柄延迟释放"通常前3次内可成功。
        private const int FsRetryCount = 4;
        private static readonly int[] FsRetryDelaysMs = new[] { 50, 100, 200, 300 };

        /// <summary>
        /// 可靠删除文件：先清只读属性 + 最多 4 次指数级重试。
        /// 处理：浏览器/网盘同步/资源管理器预览等常见只读/临时占用场景。
        /// </summary>
        private static void DeleteFileWithRetry(string filePath)
        {
            Exception? lastEx = null;
            for (int attempt = 0; attempt < FsRetryCount; attempt++)
            {
                try
                {
                    if (!File.Exists(filePath)) return;
                    // 先去除 ReadOnly / Archive / System / Encrypted / Offline 等任何影响写入的属性，置 Normal
                    File.SetAttributes(filePath, FileAttributes.Normal);
                    File.Delete(filePath);
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    lastEx = ex;
                    if (attempt < FsRetryCount - 1)
                        Thread.Sleep(FsRetryDelaysMs[attempt]);
                }
            }
            throw new AggregateException($"文件删除失败，可能仍被占用或权限不足：{filePath}", lastEx);
        }

        /// <summary>
        /// 可靠删除文件夹：递归清内部文件只读属性 + 整体重试。
        /// 先遍历所有内部文件清属性，再删根目录；每一轮整体失败再重试。
        /// </summary>
        private static void DeleteFolderWithRetry(string folderPath)
        {
            Exception? lastEx = null;
            for (int attempt = 0; attempt < FsRetryCount; attempt++)
            {
                try
                {
                    if (!Directory.Exists(folderPath)) return;
                    // 递归清除内部所有文件/子文件夹的只读等属性
                    foreach (var file in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
                    {
                        try { File.SetAttributes(file, FileAttributes.Normal); }
                        catch { /* 个别文件清属性失败不阻止继续 */ }
                    }
                    Directory.Delete(folderPath, recursive: true);
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    lastEx = ex;
                    if (attempt < FsRetryCount - 1)
                    {
                        // 再追加一次 GC 回收，应对句柄延迟释放
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(FsRetryDelaysMs[attempt]);
                    }
                }
            }
            throw new AggregateException($"文件夹删除失败，内部可能仍有文件被占用或权限不足：{folderPath}", lastEx);
        }

        /// <summary>
        /// 可靠重命名文件：先清只读属性 + 重试。File.Move 对只读文件也会抛 UnauthorizedAccessException。
        /// </summary>
        private static void MoveFileWithRetry(string oldPath, string newPath)
        {
            Exception? lastEx = null;
            for (int attempt = 0; attempt < FsRetryCount; attempt++)
            {
                try
                {
                    if (!File.Exists(oldPath))
                        throw new FileNotFoundException("源文件不存在，可能已被删除或移动。", oldPath);
                    File.SetAttributes(oldPath, FileAttributes.Normal);
                    File.Move(oldPath, newPath);
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    lastEx = ex;
                    if (attempt < FsRetryCount - 1)
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(FsRetryDelaysMs[attempt]);
                    }
                }
            }
            throw new AggregateException($"文件重命名失败，可能仍被占用或权限不足：{oldPath}", lastEx);
        }

        /// <summary>
        /// 可靠重命名文件夹：重试整体 Directory.Move；重命名不涉及子文件属性，核心是句柄占用重试。
        /// </summary>
        private static void MoveFolderWithRetry(string oldPath, string newPath)
        {
            Exception? lastEx = null;
            for (int attempt = 0; attempt < FsRetryCount; attempt++)
            {
                try
                {
                    if (!Directory.Exists(oldPath))
                        throw new DirectoryNotFoundException($"源文件夹不存在：{oldPath}");
                    Directory.Move(oldPath, newPath);
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException || ex is IOException)
                {
                    lastEx = ex;
                    if (attempt < FsRetryCount - 1)
                    {
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(FsRetryDelaysMs[attempt]);
                    }
                }
            }
            throw new AggregateException($"文件夹重命名失败，内部可能仍有文件被占用或权限不足：{oldPath}", lastEx);
        }

        /// <summary>
        /// 判断目标（文件或文件夹）是否包含当前打开的 PDF，决定删除/重命名前是否需要释放文件句柄。
        /// 文件：路径等于当前 PDF；文件夹：当前 PDF 位于该文件夹下（含分隔符，避免"abc"误匹配"abcd"）。
        /// </summary>
        private bool ShouldReleaseCurrentDocument(string targetPath, bool isFolder)
        {
            if (string.IsNullOrEmpty(_currentPdfPath)) return false;
            if (string.IsNullOrEmpty(targetPath)) return false;

            if (!isFolder)
            {
                return string.Equals(_currentPdfPath, targetPath, StringComparison.OrdinalIgnoreCase);
            }

            // 文件夹：当前 PDF 路径以"目标文件夹 + 分隔符"开头
            string prefix = targetPath.EndsWith(Path.DirectorySeparatorChar) || targetPath.EndsWith(Path.AltDirectorySeparatorChar)
                ? targetPath
                : targetPath + Path.DirectorySeparatorChar;
            return _currentPdfPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

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
            _buttonEraserMode = new Button();
            _buttonHighlightMode = new Button();
            _buttonRectangleMode = new Button();
            _buttonEllipseMode = new Button();
            _buttonArrowMode = new Button();
            _buttonPenTypePencil = new Button();
            _buttonPenTypePen = new Button();
            _buttonPenTypeMarker = new Button();
            _buttonMosaicMode = new Button();
            _buttonTextMode = new Button();
            _buttonUndoAnnotation = new Button();
            _buttonAskAi = new Button();
            _buttonOpenFolder = new Button();
            _panelAnnotationOptions = new Panel();
            _buttonDashStyle = new Button();
            _trackBarThickness = new TrackBar();
            _labelThicknessValue = new Label();
            _panelColor = new Panel();
            _buttonColorBlack = new Button();
            _buttonColorWhite = new Button();
            _buttonColorBlue = new Button();
            _buttonColorGreen = new Button();
            _buttonColorOrange = new Button();
            _buttonColorRed = new Button();
            _buttonColorPurple = new Button();
            _buttonColorCyan = new Button();
            _buttonColorTeal = new Button();
            _buttonColorPink = new Button();
            _buttonColorBrown = new Button();
            _buttonColorGray = new Button();
            _buttonColorYellow = new Button();
            _buttonColorLime = new Button();
            _buttonColorVioletRed = new Button();
            _buttonColorDodgerBlue = new Button();
            _buttonColorMore = new Button();
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
            _checkBoxAutoTranslate = new CheckBox();
            _checkBoxAutoSpeak = new CheckBox();
            _speedSelector = new SpeedSelectorControl();
            _labelTranslation = new Label();
            _labelOriginal = new Label();
            _buttonTranslate = new Button();
            _tabPageBookmarksAndHighlights = new TabPage();
            _tabPageAnnotationSummary = new TabPage();
            _listViewAnnotationSummary = new ListView();
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
            _buttonBatchRemoveHighlight = new Button();
            _buttonExportHighlights = new Button();
            _groupBoxBookmarks = new GroupBox();
            _listBoxBookmarks = new ListBox();
            _textBoxBookmarkTitle = new TextBox();
            _buttonPanel = new FlowLayoutPanel();
            _buttonAddBookmark = new Button();
            _buttonRemoveBookmark = new Button();
            _buttonPenMode = new Button();
            _toolTip = new ToolTip(components);
            _buttonStrikethroughMode = new Button();
            _toastLabel = new Label();
            _buttonTranslationToggle = new Button();
            _pageTransitionTimer = new System.Windows.Forms.Timer(components);
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
            ((ISupportInitialize)_trackBarThickness).BeginInit();
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
            _panelNavigation.Size = new Size(1127, 112);
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
            _toolbarGroupNav.Location = new Point(1057, 8);
            _toolbarGroupNav.Name = "_toolbarGroupNav";
            _toolbarGroupNav.Size = new Size(280, 96);
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
            _toolbarGroupView.Location = new Point(736, 8);
            _toolbarGroupView.Name = "_toolbarGroupView";
            _toolbarGroupView.Size = new Size(321, 96);
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
            _toolbarGroupMode.Controls.Add(_buttonOpenFolder);
            _toolbarGroupMode.Controls.Add(_buttonAskAi);
            _toolbarGroupMode.Dock = DockStyle.Left;
            _toolbarGroupMode.Location = new Point(534, 8);
            _toolbarGroupMode.Name = "_toolbarGroupMode";
            _toolbarGroupMode.Size = new Size(202, 96);
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
            _toolbarGroupTools.Controls.Add(_buttonEraserMode);
            _toolbarGroupTools.Controls.Add(_buttonHighlightMode);
            _toolbarGroupTools.Controls.Add(_buttonRectangleMode);
            _toolbarGroupTools.Controls.Add(_buttonClearAllAnnotations);
            _toolbarGroupTools.Controls.Add(_buttonEllipseMode);
            _toolbarGroupTools.Controls.Add(_buttonArrowMode);
            _toolbarGroupTools.Controls.Add(_buttonPenTypePencil);
            _toolbarGroupTools.Controls.Add(_buttonPenTypePen);
            _toolbarGroupTools.Controls.Add(_buttonPenTypeMarker);
            _toolbarGroupTools.Controls.Add(_buttonMosaicMode);
            _toolbarGroupTools.Controls.Add(_buttonTextMode);
            _toolbarGroupTools.Controls.Add(_buttonUndoAnnotation);
            _toolbarGroupTools.Controls.Add(_panelAnnotationOptions);
            _toolbarGroupTools.Dock = DockStyle.Left;
            _toolbarGroupTools.Location = new Point(8, 8);
            _toolbarGroupTools.Name = "_toolbarGroupTools";
            _toolbarGroupTools.Size = new Size(526, 96);
            _toolbarGroupTools.TabIndex = 3;
            // 
            // _buttonSelectMode
            // 
            _buttonSelectMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSelectMode.FlatStyle = FlatStyle.Flat;
            _buttonSelectMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonSelectMode.Location = new Point(10, 2);
            _buttonSelectMode.Name = "_buttonSelectMode";
            _buttonSelectMode.Size = new Size(28, 28);
            _buttonSelectMode.TabIndex = 0;
            _buttonSelectMode.Text = "👆";
            _toolTip.SetToolTip(_buttonSelectMode, "选择模式（点击选中标注后拖拽移动/拖拽手柄调整大小）");
            _buttonSelectMode.UseVisualStyleBackColor = false;
            _buttonSelectMode.Click += ButtonSelectMode_Click;
            // 
            // _buttonEraserMode
            // 
            _buttonEraserMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonEraserMode.FlatStyle = FlatStyle.Flat;
            _buttonEraserMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonEraserMode.Location = new Point(362, 2);
            _buttonEraserMode.Name = "_buttonEraserMode";
            _buttonEraserMode.Size = new Size(28, 28);
            _buttonEraserMode.TabIndex = 19;
            _buttonEraserMode.Text = "\U0001f9fd";
            _toolTip.SetToolTip(_buttonEraserMode, "橡皮擦");
            _buttonEraserMode.UseVisualStyleBackColor = false;
            _buttonEraserMode.Click += ButtonEraserMode_Click;
            // 
            // _buttonHighlightMode
            // 
            _buttonHighlightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonHighlightMode.FlatStyle = FlatStyle.Flat;
            _buttonHighlightMode.Font = new Font("Microsoft YaHei UI", 11F);
            _buttonHighlightMode.Location = new Point(42, 2);
            _buttonHighlightMode.Name = "_buttonHighlightMode";
            _buttonHighlightMode.Size = new Size(28, 28);
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
            _buttonRectangleMode.Location = new Point(74, 2);
            _buttonRectangleMode.Name = "_buttonRectangleMode";
            _buttonRectangleMode.Size = new Size(28, 28);
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
            _buttonEllipseMode.Location = new Point(106, 2);
            _buttonEllipseMode.Name = "_buttonEllipseMode";
            _buttonEllipseMode.Size = new Size(28, 28);
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
            _buttonArrowMode.Location = new Point(138, 2);
            _buttonArrowMode.Name = "_buttonArrowMode";
            _buttonArrowMode.Size = new Size(28, 28);
            _buttonArrowMode.TabIndex = 3;
            _buttonArrowMode.Text = "━";
            _buttonArrowMode.UseVisualStyleBackColor = false;
            _buttonArrowMode.Click += ButtonArrowMode_Click;
            // 
            // _buttonPenTypePencil
            // 
            _buttonPenTypePencil.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypePencil.FlatStyle = FlatStyle.Flat;
            _buttonPenTypePencil.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPenTypePencil.Location = new Point(170, 2);
            _buttonPenTypePencil.Name = "_buttonPenTypePencil";
            _buttonPenTypePencil.Size = new Size(28, 28);
            _buttonPenTypePencil.TabIndex = 15;
            _buttonPenTypePencil.Text = "✎";
            _toolTip.SetToolTip(_buttonPenTypePencil, "铅笔");
            _buttonPenTypePencil.UseVisualStyleBackColor = false;
            _buttonPenTypePencil.Click += ButtonPenTypePencil_Click;
            // 
            // _buttonPenTypePen
            // 
            _buttonPenTypePen.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypePen.FlatStyle = FlatStyle.Flat;
            _buttonPenTypePen.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            _buttonPenTypePen.Location = new Point(202, 2);
            _buttonPenTypePen.Name = "_buttonPenTypePen";
            _buttonPenTypePen.Size = new Size(28, 28);
            _buttonPenTypePen.TabIndex = 16;
            _buttonPenTypePen.Text = "✒";
            _toolTip.SetToolTip(_buttonPenTypePen, "水笔");
            _buttonPenTypePen.UseVisualStyleBackColor = false;
            _buttonPenTypePen.Click += ButtonPenTypePen_Click;
            // 
            // _buttonPenTypeMarker
            // 
            _buttonPenTypeMarker.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonPenTypeMarker.FlatStyle = FlatStyle.Flat;
            _buttonPenTypeMarker.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonPenTypeMarker.Location = new Point(234, 2);
            _buttonPenTypeMarker.Name = "_buttonPenTypeMarker";
            _buttonPenTypeMarker.Size = new Size(28, 28);
            _buttonPenTypeMarker.TabIndex = 17;
            _buttonPenTypeMarker.Text = "▮";
            _toolTip.SetToolTip(_buttonPenTypeMarker, "马克笔");
            _buttonPenTypeMarker.UseVisualStyleBackColor = false;
            _buttonPenTypeMarker.Click += ButtonPenTypeMarker_Click;
            // 
            // _buttonMosaicMode
            // 
            _buttonMosaicMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonMosaicMode.FlatStyle = FlatStyle.Flat;
            _buttonMosaicMode.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonMosaicMode.Location = new Point(298, 2);
            _buttonMosaicMode.Name = "_buttonMosaicMode";
            _buttonMosaicMode.Size = new Size(28, 28);
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
            _buttonTextMode.Location = new Point(330, 2);
            _buttonTextMode.Name = "_buttonTextMode";
            _buttonTextMode.Size = new Size(28, 28);
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
            _buttonUndoAnnotation.Location = new Point(394, 2);
            _buttonUndoAnnotation.Name = "_buttonUndoAnnotation";
            _buttonUndoAnnotation.Size = new Size(28, 28);
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
            _buttonAskAi.Location = new Point(124, 2);
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
            _buttonOpenFolder.Location = new Point(156, 2);
            _buttonOpenFolder.Name = "_buttonOpenFolder";
            _buttonOpenFolder.Size = new Size(32, 32);
            _buttonOpenFolder.TabIndex = 9;
            _buttonOpenFolder.Text = "📂";
            _buttonOpenFolder.UseVisualStyleBackColor = false;
            _buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // _panelAnnotationOptions
            // 
            _panelAnnotationOptions.Controls.Add(_buttonDashStyle);
            _panelAnnotationOptions.Controls.Add(_trackBarThickness);
            _panelAnnotationOptions.Controls.Add(_labelThicknessValue);
            _panelAnnotationOptions.Controls.Add(_panelColor);
            _panelAnnotationOptions.Location = new Point(50, 38);
            _panelAnnotationOptions.Name = "_panelAnnotationOptions";
            _panelAnnotationOptions.Size = new Size(580, 56);
            _panelAnnotationOptions.TabIndex = 9;
            _panelAnnotationOptions.Visible = false;
            // 
            // _buttonDashStyle
            // 
            _buttonDashStyle.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonDashStyle.FlatStyle = FlatStyle.Flat;
            _buttonDashStyle.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonDashStyle.Location = new Point(4, 16);
            _buttonDashStyle.Name = "_buttonDashStyle";
            _buttonDashStyle.Size = new Size(32, 24);
            _buttonDashStyle.TabIndex = 0;
            _buttonDashStyle.Text = "—";
            _toolTip.SetToolTip(_buttonDashStyle, "切换实线/虚线");
            _buttonDashStyle.UseVisualStyleBackColor = false;
            _buttonDashStyle.Click += ButtonDashStyle_Click;
            // 
            // _trackBarThickness
            // 
            _trackBarThickness.AutoSize = false;
            _trackBarThickness.Location = new Point(44, 6);
            _trackBarThickness.Maximum = 20;
            _trackBarThickness.Minimum = 1;
            _trackBarThickness.Name = "_trackBarThickness";
            _trackBarThickness.Size = new Size(100, 28);
            _trackBarThickness.TabIndex = 4;
            _trackBarThickness.TickStyle = TickStyle.None;
            _trackBarThickness.Value = 3;
            _trackBarThickness.Scroll += TrackBarThickness_Scroll;
            // 
            // _labelThicknessValue
            // 
            _labelThicknessValue.AutoSize = true;
            _labelThicknessValue.Font = new Font("Microsoft YaHei UI", 9F);
            _labelThicknessValue.ForeColor = Color.FromArgb(102, 102, 102);
            _labelThicknessValue.Location = new Point(148, 16);
            _labelThicknessValue.Name = "_labelThicknessValue";
            _labelThicknessValue.Size = new Size(29, 17);
            _labelThicknessValue.TabIndex = 5;
            _labelThicknessValue.Text = "3px";
            // 
            // _panelColor
            // 
            _panelColor.Controls.Add(_buttonColorBlack);
            _panelColor.Controls.Add(_buttonColorWhite);
            _panelColor.Controls.Add(_buttonColorBlue);
            _panelColor.Controls.Add(_buttonColorGreen);
            _panelColor.Controls.Add(_buttonColorOrange);
            _panelColor.Controls.Add(_buttonColorRed);
            _panelColor.Controls.Add(_buttonColorPurple);
            _panelColor.Controls.Add(_buttonColorCyan);
            _panelColor.Controls.Add(_buttonColorTeal);
            _panelColor.Controls.Add(_buttonColorPink);
            _panelColor.Controls.Add(_buttonColorBrown);
            _panelColor.Controls.Add(_buttonColorGray);
            _panelColor.Controls.Add(_buttonColorYellow);
            _panelColor.Controls.Add(_buttonColorLime);
            _panelColor.Controls.Add(_buttonColorVioletRed);
            _panelColor.Controls.Add(_buttonColorDodgerBlue);
            _panelColor.Controls.Add(_buttonColorMore);
            _panelColor.Location = new Point(186, 0);
            _panelColor.Name = "_panelColor";
            _panelColor.Size = new Size(248, 56);
            _panelColor.TabIndex = 1;
            // 
            // _buttonColorBlack
            // 
            _buttonColorBlack.Location = new Point(0, 0);
            _buttonColorBlack.Name = "_buttonColorBlack";
            _buttonColorBlack.Size = new Size(75, 23);
            _buttonColorBlack.TabIndex = 0;
            // 
            // _buttonColorWhite
            // 
            _buttonColorWhite.Location = new Point(0, 0);
            _buttonColorWhite.Name = "_buttonColorWhite";
            _buttonColorWhite.Size = new Size(75, 23);
            _buttonColorWhite.TabIndex = 1;
            // 
            // _buttonColorBlue
            // 
            _buttonColorBlue.Location = new Point(0, 0);
            _buttonColorBlue.Name = "_buttonColorBlue";
            _buttonColorBlue.Size = new Size(75, 23);
            _buttonColorBlue.TabIndex = 2;
            // 
            // _buttonColorGreen
            // 
            _buttonColorGreen.Location = new Point(0, 0);
            _buttonColorGreen.Name = "_buttonColorGreen";
            _buttonColorGreen.Size = new Size(75, 23);
            _buttonColorGreen.TabIndex = 3;
            // 
            // _buttonColorOrange
            // 
            _buttonColorOrange.Location = new Point(0, 0);
            _buttonColorOrange.Name = "_buttonColorOrange";
            _buttonColorOrange.Size = new Size(75, 23);
            _buttonColorOrange.TabIndex = 4;
            // 
            // _buttonColorRed
            // 
            _buttonColorRed.Location = new Point(0, 0);
            _buttonColorRed.Name = "_buttonColorRed";
            _buttonColorRed.Size = new Size(75, 23);
            _buttonColorRed.TabIndex = 5;
            // 
            // _buttonColorPurple
            // 
            _buttonColorPurple.Location = new Point(0, 0);
            _buttonColorPurple.Name = "_buttonColorPurple";
            _buttonColorPurple.Size = new Size(75, 23);
            _buttonColorPurple.TabIndex = 6;
            // 
            // _buttonColorCyan
            // 
            _buttonColorCyan.Location = new Point(0, 0);
            _buttonColorCyan.Name = "_buttonColorCyan";
            _buttonColorCyan.Size = new Size(75, 23);
            _buttonColorCyan.TabIndex = 7;
            // 
            // _buttonColorTeal
            // 
            _buttonColorTeal.Location = new Point(0, 0);
            _buttonColorTeal.Name = "_buttonColorTeal";
            _buttonColorTeal.Size = new Size(75, 23);
            _buttonColorTeal.TabIndex = 8;
            // 
            // _buttonColorPink
            // 
            _buttonColorPink.Location = new Point(0, 0);
            _buttonColorPink.Name = "_buttonColorPink";
            _buttonColorPink.Size = new Size(75, 23);
            _buttonColorPink.TabIndex = 9;
            // 
            // _buttonColorBrown
            // 
            _buttonColorBrown.Location = new Point(0, 0);
            _buttonColorBrown.Name = "_buttonColorBrown";
            _buttonColorBrown.Size = new Size(75, 23);
            _buttonColorBrown.TabIndex = 10;
            // 
            // _buttonColorGray
            // 
            _buttonColorGray.Location = new Point(0, 0);
            _buttonColorGray.Name = "_buttonColorGray";
            _buttonColorGray.Size = new Size(75, 23);
            _buttonColorGray.TabIndex = 11;
            // 
            // _buttonColorYellow
            // 
            _buttonColorYellow.Location = new Point(0, 0);
            _buttonColorYellow.Name = "_buttonColorYellow";
            _buttonColorYellow.Size = new Size(75, 23);
            _buttonColorYellow.TabIndex = 12;
            // 
            // _buttonColorLime
            // 
            _buttonColorLime.Location = new Point(0, 0);
            _buttonColorLime.Name = "_buttonColorLime";
            _buttonColorLime.Size = new Size(75, 23);
            _buttonColorLime.TabIndex = 13;
            // 
            // _buttonColorVioletRed
            // 
            _buttonColorVioletRed.Location = new Point(0, 0);
            _buttonColorVioletRed.Name = "_buttonColorVioletRed";
            _buttonColorVioletRed.Size = new Size(75, 23);
            _buttonColorVioletRed.TabIndex = 14;
            // 
            // _buttonColorDodgerBlue
            // 
            _buttonColorDodgerBlue.Location = new Point(0, 0);
            _buttonColorDodgerBlue.Name = "_buttonColorDodgerBlue";
            _buttonColorDodgerBlue.Size = new Size(75, 23);
            _buttonColorDodgerBlue.TabIndex = 15;
            // 
            // _buttonColorMore
            // 
            _buttonColorMore.BackColor = Color.FromArgb(240, 240, 240);
            _buttonColorMore.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            _buttonColorMore.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 220, 220);
            _buttonColorMore.FlatStyle = FlatStyle.Flat;
            _buttonColorMore.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _buttonColorMore.Location = new Point(2, 4);
            _buttonColorMore.Name = "_buttonColorMore";
            _buttonColorMore.Size = new Size(22, 22);
            _buttonColorMore.TabIndex = 14;
            _buttonColorMore.Text = "+";
            _buttonColorMore.UseVisualStyleBackColor = false;
            _buttonColorMore.Click += ButtonColorMore_Click;
            // 
            // _buttonClearAllAnnotations
            // 
            _buttonClearAllAnnotations.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonClearAllAnnotations.FlatStyle = FlatStyle.Flat;
            _buttonClearAllAnnotations.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonClearAllAnnotations.Location = new Point(428, 1);
            _buttonClearAllAnnotations.Name = "_buttonClearAllAnnotations";
            _buttonClearAllAnnotations.Size = new Size(28, 28);
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
            _tabControlLeft.Controls.Add(_tabPageAnnotationSummary);
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
            _groupBoxProgress.Controls.Add(_speedSelector);
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
            _textBoxOriginal.Location = new Point(10, 91);
            _textBoxOriginal.Multiline = true;
            _textBoxOriginal.Name = "_textBoxOriginal";
            _textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            _textBoxOriginal.Size = new Size(294, 179);
            _textBoxOriginal.TabIndex = 4;
            // 
            // _buttonSpeakOriginal
            // 
            _buttonSpeakOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _buttonSpeakOriginal.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonSpeakOriginal.FlatStyle = FlatStyle.Flat;
            _buttonSpeakOriginal.Location = new Point(57, 20);
            _buttonSpeakOriginal.Name = "_buttonSpeakOriginal";
            _buttonSpeakOriginal.Size = new Size(75, 25);
            _buttonSpeakOriginal.TabIndex = 3;
            _buttonSpeakOriginal.Text = "🔊 朗读";
            _buttonSpeakOriginal.UseVisualStyleBackColor = false;
            _buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;
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
            // _checkBoxAutoSpeak
            // 
            _checkBoxAutoSpeak.AutoSize = true;
            _checkBoxAutoSpeak.Checked = true;
            _checkBoxAutoSpeak.CheckState = CheckState.Checked;
            _checkBoxAutoSpeak.Location = new Point(25, 56);
            _checkBoxAutoSpeak.Name = "_checkBoxAutoSpeak";
            _checkBoxAutoSpeak.Size = new Size(107, 21);
            _checkBoxAutoSpeak.TabIndex = 8;
            _checkBoxAutoSpeak.Text = "🔊 识别后朗读";
            _checkBoxAutoSpeak.UseVisualStyleBackColor = true;
            // 
            // _speedSelector
            // 
            _speedSelector.BackColor = Color.Transparent;
            _speedSelector.Location = new Point(138, 45);
            _speedSelector.Name = "_speedSelector";
            _speedSelector.Size = new Size(135, 32);
            _speedSelector.TabIndex = 9;
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
            // _tabPageAnnotationSummary
            // 
            _tabPageAnnotationSummary.Controls.Add(_listViewAnnotationSummary);
            _tabPageAnnotationSummary.Location = new Point(4, 26);
            _tabPageAnnotationSummary.Name = "_tabPageAnnotationSummary";
            _tabPageAnnotationSummary.Padding = new Padding(3);
            _tabPageAnnotationSummary.Size = new Size(322, 870);
            _tabPageAnnotationSummary.TabIndex = 4;
            _tabPageAnnotationSummary.Text = "📝 标注摘要";
            _tabPageAnnotationSummary.UseVisualStyleBackColor = true;

            // 
            // _listViewAnnotationSummary
            // 
            _listViewAnnotationSummary.Dock = DockStyle.Fill;
            _listViewAnnotationSummary.Font = new Font("Microsoft YaHei UI", 9F);
            _listViewAnnotationSummary.FullRowSelect = true;
            _listViewAnnotationSummary.GridLines = true;
            _listViewAnnotationSummary.HeaderStyle = ColumnHeaderStyle.Nonclickable;
            _listViewAnnotationSummary.HideSelection = false;
            _listViewAnnotationSummary.Location = new Point(3, 3);
            _listViewAnnotationSummary.Name = "_listViewAnnotationSummary";
            _listViewAnnotationSummary.Size = new Size(316, 864);
            _listViewAnnotationSummary.TabIndex = 0;
            _listViewAnnotationSummary.UseCompatibleStateImageBehavior = false;
            _listViewAnnotationSummary.View = View.Details;
            _listViewAnnotationSummary.MultiSelect = false;
            _listViewAnnotationSummary.Columns.Add("页面", 46, HorizontalAlignment.Left);
            _listViewAnnotationSummary.Columns.Add("类型", 46, HorizontalAlignment.Left);
            _listViewAnnotationSummary.Columns.Add("内容", 200, HorizontalAlignment.Left);
            _listViewAnnotationSummary.DoubleClick += ListViewAnnotationSummary_DoubleClick;
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
            // _buttonBatchRemoveHighlight
            // 
            _buttonBatchRemoveHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonBatchRemoveHighlight.FlatStyle = FlatStyle.Flat;
            _buttonBatchRemoveHighlight.Location = new Point(125, 3);
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
            _buttonExportHighlights.Location = new Point(186, 3);
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
            // _buttonPenMode
            // 
            _buttonPenMode.Location = new Point(0, 0);
            _buttonPenMode.Name = "_buttonPenMode";
            _buttonPenMode.Size = new Size(75, 23);
            _buttonPenMode.TabIndex = 0;
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
            _panelAnnotationOptions.PerformLayout();
            ((ISupportInitialize)_trackBarThickness).EndInit();
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
            _tabPageAnnotationSummary.ResumeLayout(false);
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

        /// <summary>
        /// 左侧选项卡切换事件：切换到缩略图选项卡时按需加载缩略图
        /// </summary>
        private void TabControlLeft_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tabControlLeft.SelectedTab == _tabPageThumbnails)
            {
                if (!_thumbnailsLoaded && _presenter != null)
                {
                    _thumbnailsLoaded = true;
                    _presenter.GenerateThumbnails();
                }
            }
            else if (_tabControlLeft.SelectedTab == _tabPageAnnotationSummary)
            {
                PopulateAnnotationSummary();
            }
        }

        private void ListViewAnnotationSummary_DoubleClick(object? sender, EventArgs e)
        {
            if (_listViewAnnotationSummary.SelectedItems.Count == 0) return;

            var item = _listViewAnnotationSummary.SelectedItems[0];
            if (item.Tag is int pageIndex && pageIndex >= 0 && pageIndex < (_presenter?.PageCount ?? 0))
            {
                _presenter?.RenderPage(pageIndex);
            }
        }

        private void PopulateAnnotationSummary()
        {
            try
            {
                _listViewAnnotationSummary.Items.Clear();

                if (_presenter == null || string.IsNullOrEmpty(_currentPdfPath))
                    return;

                int pageCount = _presenter.PageCount;
                int totalAnnotations = 0;

                for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                {
                    // 获取笔划
                    var strokes = _presenter.GetCurrentPageStrokesForPage(pageIndex);
                    foreach (var stroke in strokes)
                    {
                        var typeText = stroke.ShapeType switch
                        {
                            "Rectangle" => "矩形",
                            "Ellipse" => "椭圆",
                            "Arrow" => "箭头",
                            "Mosaic" => "马赛克",
                            "Pen" => "画笔",
                            _ => stroke.ShapeType ?? "未知"
                        };
                        var item = new ListViewItem(new[] { $"第{pageIndex + 1}页", typeText, "" });
                        item.Tag = pageIndex;
                        _listViewAnnotationSummary.Items.Add(item);
                        totalAnnotations++;
                    }

                    // 获取高亮
                    var highlights = _highlightService?.GetHighlightsForPage(_currentPdfPath, pageIndex);
                    if (highlights != null)
                    {
                        foreach (var hl in highlights)
                        {
                            var text = string.IsNullOrEmpty(hl.Text) ? "" : hl.Text;
                            var colorName = hl.Color switch
                            {
                                HighlightColor.Yellow => "黄",
                                HighlightColor.Green => "绿",
                                HighlightColor.Blue => "蓝",
                                HighlightColor.Pink => "粉",
                                HighlightColor.Orange => "橙",
                                HighlightColor.Red => "红",
                                _ => "黄"
                            };
                            var item = new ListViewItem(new[] { $"第{pageIndex + 1}页", $"高亮({colorName})", text });
                            item.Tag = pageIndex;
                            _listViewAnnotationSummary.Items.Add(item);
                            totalAnnotations++;
                        }
                    }
                }

                _tabPageAnnotationSummary.Text = totalAnnotations > 0
                    ? $"📝 标注({totalAnnotations})"
                    : "📝 标注摘要";
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "填充标注摘要失败");
            }
        }
    }
}
