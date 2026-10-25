using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.UI;
using LearningAssistant.Forms.UserControls.Common;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.Learning.Status;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Practice;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Practice;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace LearningAssistant.Forms.Pdf
{
    public partial class PdfReaderForm : Form, IPdfView, IPdfReaderFormAccess
    {

        #region 字段与状态

        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderForm> _logger;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private readonly Services.Learning.IPendingContentService? _pendingContentService;
        private readonly IHighlightService _highlightService;
        private readonly IAnnotationService? _annotationService;
        private readonly IEventBus? _eventBus;
        private readonly IPdfAiService? _pdfAiService;
        private readonly IPdfDocumentTextIndex? _documentTextIndex;
        private readonly Services.Learning.ISpacedRepetitionService? _spacedRepetitionService;
        private readonly Services.Learning.INoteService? _noteService;
        private readonly Services.Learning.IUserSessionService? _userSessionService;
        private readonly Services.Learning.IStudyEntryService? _studyEntryService;
        private readonly AppConfig? _appConfig;

        // 本次会话是否已确认把整份文档文本发送给 AI 服务，避免每次操作重复弹窗
        private bool _wholeDocumentAiConfirmed;

        private PdfReaderNightModeManager? _nightModeManager;
        private PdfReaderHighlightManager? _highlightManager;
        private PdfReaderNavigationManager? _navigationManager;
        private PdfReaderAudioManager? _audioManager;
        private PdfAudioTrackManager? _audioTrackManager;
        private PdfAudioPanel? _audioPanel;
        private PdfAudioPlayerBar? _audioPlayerBar;
        private Panel? _recordingHost;
        private bool _recordingServiceAvailable;
        // 阶段2：音频内容从顶层页签迁入右侧抽屉（配套音频 / 整页录音 / 翻译）
        private Panel? _audioDrawer;
        private Panel? _audioDrawerContent;
        private Label? _audioDrawerTitle;
        private Label? _audioDrawerSubtitle;
        private Button? _audioDrawerAudioButton;
        private Button? _audioDrawerRecordingButton;
        private Button? _audioDrawerTranslateButton;
        private Label? _audioUnavailableLabel;
        private Label? _recordingUnavailableLabel;
        private Button? _audioDrawerCloseButton;
        private Control? _audioDrawerReturnFocus;
        private AudioDrawerMode _audioDrawerMode;
        private PdfReadingSidebarPanel? _readingSidebar;
        private SidebarHeaderBar? _translateHeader;
        private PdfTranslatePanel? _translatePanel;
        private SidebarHeaderBar? _highlightHeader;
        private PdfHighlightPanel? _highlightPanel;
        private Panel? _highlightEmptyHost;
        private readonly IAudioRecorderService? _audioRecorderService;

        private readonly Pen _pen = new Pen(Color.Red, 4f);
        private bool _disposed = false;

        private enum AudioDrawerMode
        {
            Audio,
            Recording,
            Translate
        }

        private Panel? _ocrPanel;
        private PictureBox? _ocrPictureBox;
        private Button? _ocrCloseButton;

        private LoadingIndicator? _loadingIndicator;
        private bool _isTranslationEnabled = false;

        private ListBox? _listBoxHighlights;
        private Button? _buttonRemoveHighlight;
        private Button? _buttonBatchRemoveHighlight;
        private Button? _buttonEditHighlight;
        private RadioButton? _radioHighlightYellow;
        private RadioButton? _radioHighlightGreen;
        private RadioButton? _radioHighlightBlue;
        private RadioButton? _radioHighlightPink;
        private RadioButton? _radioHighlightOrange;

        private TabPage? _tabPageBookmarksAndHighlights;
        private TabPage? _tabPageAudio;

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
        private BufferedPictureBox _pictureBoxPdf;

        // 未打开 PDF 时的空状态引导面板
        private Panel? _panelEmptyState;
        private FlowLayoutPanel? _panelEmptyContent;
        private ContextMenuStrip _contextMenuPdf;
        private Label? _toastLabel;

        // 复用同一个提示标签，新提示到来时取消前一条的隐藏计时
        private CancellationTokenSource? _toastCts;
        private Panel _panelThumbnails;
        private FlowLayoutPanel _flowLayoutPanelThumbnails;
        private TabPage _tabPageTranslate;
        private TabPage _tabPageStudy;
        private Label? _studyHighlightCount;
        private Label? _studyLearningCount;
        private Label? _studySentenceCount;
        private Button? _buttonStudyAddHighlights;
        private Button? _buttonStudyFlashcards;
        private Button? _buttonStudyShadowing;
        private FlowLayoutPanel? _studyItemList;
        private PdfStudyPanel? _studyPanel;
        private static string? _lastSidebarTabName;
        private SyncedTextBox _textBoxOriginal;
        private SyncedTextBox _textBoxTranslation;
        private Button _buttonAddToLearningContent;
        private CheckBox _checkBoxAutoSpeak;
        private SpeedSelectorControl _speedSelector;
        private Panel _panelNavigation;
        private Button _buttonPrev;
        private Button _buttonPrevSpread;
        private TextBox _textBoxPage;
        private Label _labelPageCount;
        private Button _buttonNext;
        private Button _buttonNextSpread;
        private ProgressBar _progressBarPage;
        private Button _buttonNightMode;
        private Button _buttonTranslationToggle;
        private Button _buttonAskAi;
        private Button _buttonOpenFolder;
        private Label _labelZoom;
        private TrackBar _trackBarZoom;
        private BufferedTabControl _tabControlLeft;
        private TabPage _tabPageFiles;
        private Panel _panelLeftContainer;

        // 侧栏页签改为自绘「图标 + 名称」：悬停索引、图标字体与名称字体
        private int _sidebarHoveredTabIndex = -1;
        private Font? _sidebarTabFont;
        private Font? _sidebarTabLabelFont;
        // 悬停提示延迟显示，避免鼠标划过页签时提示框反复弹出/收回造成闪烁
        private System.Windows.Forms.Timer? _sidebarTipTimer;

        // 阶段5：页签使用习惯按文档记忆，另提供默认关闭的悬停切换
        private const string SidebarStateFileName = "pdf-sidebar-state.json";
        private static bool _sidebarHoverSwitch;
        private static bool _sidebarStateLoaded;
        private static Dictionary<string, string> _sidebarTabByDocument = new();
        private System.Windows.Forms.Timer? _sidebarHoverSwitchTimer;
        private int _sidebarHoverSwitchCandidate = -1;
        private ContextMenuStrip? _sidebarTabMenu;

        private Label _transitionLabel;
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
        private Button _buttonTemporaryPenMode;
        private Button _buttonMosaicMode;
        private Button _buttonStrikethroughMode;
        private Button _buttonCollapseNav;
        private Button _buttonTextMode;
        private Button _buttonUndoAnnotation;
        private Button _buttonClearAllAnnotations;
        private Button _buttonToggleAnnotationLayer;
        private Button _buttonExportAnnotationsAsImage;
        private Button _buttonEraserMode;
        private Button _buttonChecklistMode;
        private Button _buttonImageEmbedMode;
        private Button _buttonSpotlightMode;
        private Button _buttonMagnifierMode;
        private Button _buttonStrokeStyleDot;
        private Button _buttonStrokeStyleArrow;
        //private Button _buttonFavoriteToolbar;
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
        //private Button _buttonColorWhite;
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
        private Button _buttonHighlightColor0;
        private Button _buttonHighlightColor1;
        private Button _buttonHighlightColor2;
        private Button _buttonHighlightColor3;
        private Button _buttonHighlightColor4;

        /// <summary>「更多」按钮：承载低频工具，对应工具栏一级图标条的收口入口</summary>
        private Button _buttonMoreTools;

        /// <summary>高亮色圆形按钮（仅在「高亮」工具下显示，与标注色共用同一颜色面板）</summary>
        private Button[] _buttonHighlightColors = Array.Empty<Button>();

        private static readonly HighlightColor[] HighlightPalette =
        {
            HighlightColor.Yellow, HighlightColor.Green, HighlightColor.Blue,
            HighlightColor.Pink, HighlightColor.Orange
        };

        // 16色面板 + 更多颜色按钮
        private static readonly Color[] AnnotationColors = new[]
        {
            Color.Black,      // 0: 黑色
            //Color.White,      // 1: 白色
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
        private System.Windows.Forms.Timer? _pageTransitionTimer;
        private string _currentPdfPath = string.Empty;
        private SentenceShadowingForm? _shadowingForm;
        private int _currentPageIndex = 0;
        private string? _pendingSelectionId = null;
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

        /// <summary>
        /// 绘制/拖拽标注期间收到的页面位图。此时不能立即替换画面（会清理标注图层并丢失当前笔画），
        /// 待鼠标抬起后再应用，仅保留最新一帧。
        /// </summary>
        private Bitmap? _pendingDisplayImage;

        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;

        private System.Windows.Forms.Timer? _longPressTimer;

        private HighlightColor _currentHighlightColor = HighlightColor.Yellow;

        private bool _isNightMode = false;
        private bool _closeConfirmed = false;
        private bool _isDualPage = false;
        private bool _isFullscreen = false;
        private Bitmap? _highlightBitmap;
        private ToolStripMenuItem menuItemCopy;
        private ToolStripMenuItem menuItemSearch;
        private ToolStripMenuItem menuItemHighlight;
        private ToolStripMenuItem menuItemRectangle;
        private ToolStripMenuItem menuItemText;
        private ToolStripSeparator menuItemAnnotationSeparator;
        private ToolStripSeparator menuItemZoomSeparator;
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
        private CheckBox _checkBoxAiPolish;
        private bool _syncingTranslationScroll;
        private Panel panel1;
        private Panel pnlPage;
        private Panel panel2;
        private SplitContainer _splitContainerMain;

        private ContextMenuStrip moreMenu;
        // 绘制工具的二级选项（虚线/粗细/颜色/笔型）改为与「更多」一致的按需浮层，不再常驻占用工具栏高度
        private ToolStripDropDown _annotationOptionsDropDown;
        private ToolStripControlHost _annotationOptionsHostItem;
        private Panel _annotationOptionsHost;

        #endregion

        #region 构造、初始化与窗体生命周期

        /// <summary>仅供 Visual Studio 设计器实例化使用，运行时不走此构造。</summary>
        public PdfReaderForm()
        {
            InitializeComponent();
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<PdfReaderForm>.Instance;
        }

        public PdfReaderForm(ILogger<PdfReaderForm> logger, IAIPanelPopupService? aiPanelPopupService = null, Services.Learning.IPendingContentService? pendingContentService = null, IHighlightService? highlightService = null, IAnnotationService? annotationService = null, IEventBus? eventBus = null, IAudioRecorderService? audioRecorderService = null, IPdfAiService? pdfAiService = null, IPdfDocumentTextIndex? documentTextIndex = null, Services.Learning.ISpacedRepetitionService? spacedRepetitionService = null, Services.Learning.INoteService? noteService = null, Services.Learning.IUserSessionService? userSessionService = null, Services.Learning.IStudyEntryService? studyEntryService = null, AppConfig? appConfig = null)
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiPanelPopupService = aiPanelPopupService;
            _pendingContentService = pendingContentService;
            _highlightService = highlightService ?? new HighlightService();
            _annotationService = annotationService;
            _eventBus = eventBus;
            _audioRecorderService = audioRecorderService;
            _pdfAiService = pdfAiService;
            _documentTextIndex = documentTextIndex;
            _spacedRepetitionService = spacedRepetitionService;
            _noteService = noteService;
            _userSessionService = userSessionService;
            _studyEntryService = studyEntryService;
            _appConfig = appConfig;
            KeyPreview = true;
            Load += PdfReaderFormV2_Load;
            Resize += PdfReaderFormV2_Resize;
            KeyDown += PdfReaderFormV2_KeyDown;
            FormClosing += PdfReaderForm_FormClosing;

            InitializeFileContextMenu();
            InitializeManagers();
        }

        private void PdfReaderForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.UserClosing || _closeConfirmed)
                return;

            // 多窗口并存时本窗口可能不在前台：先提到最前，避免确认框被其它阅读器窗口挡住，
            // 让人以为窗口被禁用、关不掉
            Activate();
            BringToFront();

            var result = MessageBox.Show(
                this,
                "确定要关闭 PDF 阅读器吗？",
                "关闭确认",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }

            _closeConfirmed = true;
        }

        private void InitializeManagers()
        {
            _nightModeManager = new PdfReaderNightModeManager(_logger, this);
            _highlightManager = new PdfReaderHighlightManager(_logger, this, _highlightService, _annotationService, _eventBus);
            _navigationManager = new PdfReaderNavigationManager(_logger, this);

            // 订阅两个 Manager 的撤销动作入栈事件，统一记录到 _unifiedUndoStack，
            // 供工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
            _highlightManager.UndoActionRecorded += (s, e) => _unifiedUndoStack.Push(UndoActionKind.Highlight);
            _navigationManager.UndoActionRecorded += (s, e) => _unifiedUndoStack.Push(UndoActionKind.Stroke);

            _navigationManager.IsHighlightModeCallback = () => _highlightManager?.IsHighlightMode ?? true;
            _navigationManager.AddHighlightCallback = rect => _highlightManager?.AddHighlight(rect);
            _navigationManager.AddTextCallback = point => ShowTextAnnotationDialog(point);
            _navigationManager.ShowMagnifierCallback = point => ShowMagnifier(point);
            _navigationManager.UpdateHighlightLayerCallback = () => _highlightManager?.UpdateHighlightLayer();

            // 录音过程中创建的标注自动关联当前录音（时间戳 + 录音 Id），供「我的录音」页跳转回放
            _navigationManager.GetCurrentAudioTimestampMs = () => _audioManager?.GetCurrentTimestampMs();
            _navigationManager.GetCurrentAudioRecordingId = () => _audioManager?.GetCurrentRecordingId();
            _navigationManager.AnnotationAddedCallback = () => _audioManager?.RefreshTimestampList();

            // 标注选中事件：双击触发编辑，单击显示选中状态
            _navigationManager.AnnotationSelected += OnAnnotationSelected;

            // 高亮选中事件：回显该区域已缓存的识别文本与译文，避免重复 OCR/翻译
            _navigationManager.HighlightSelected += OnHighlightSelected;

            // 文字标注双击编辑回调
            _navigationManager.EditTextCallback = (text, index) =>
            {
                if (_currentPageImage == null) return;
                var item = new PdfAnnotationItem
                {
                    Id = text.Id,
                    Type = AnnotationType.Text,
                    PdfPath = _currentPdfPath,
                    PageIndex = _currentPageIndex,
                    Text = text.Content,
                    ColorArgb = text.ColorArgb,
                    FontSize = text.FontSize,
                    FontFamily = text.FontFamily,
                    NormalizedX = text.NormalizedX,
                    NormalizedY = text.NormalizedY
                };
                EditTextAnnotation(item);
            };

            if (_pictureBoxPdf != null)
            {
                _pictureBoxPdf.MouseDown += _navigationManager.MouseDown;
                _pictureBoxPdf.MouseMove += _navigationManager.MouseMove;
                _pictureBoxPdf.MouseUp += PictureBoxPdf_MouseUp;
            }

            // 阶段2：音频内容统一收进右侧抽屉，不再创建顶层「音频」三段式侧栏
            _recordingHost = new Panel { Dock = DockStyle.Fill, Visible = false };

            // 初始化配套音频跟踪管理器（与录音服务解耦，独立于录音可用性）
            try
            {
                var matcher = Program.ServiceProvider?.GetService(typeof(IPdfAudioMatcher)) as IPdfAudioMatcher;
                var scanner = Program.ServiceProvider?.GetService(typeof(IPdfAudioDirectoryScanner)) as IPdfAudioDirectoryScanner;
                var store = Program.ServiceProvider?.GetService(typeof(IPdfAudioProfileStore)) as IPdfAudioProfileStore;
                var subParser = Program.ServiceProvider?.GetService(typeof(ISubtitleParser)) as ISubtitleParser;
                if (matcher != null && scanner != null && store != null && subParser != null)
                {
                    _audioTrackManager = new PdfAudioTrackManager(_logger, matcher, scanner, store, subParser);
                    _audioTrackManager.PageSyncRequested += OnAudioPageSyncRequested;
                    // 音频档案加载/变更后按当前文档是否真的有音频来决定播放条显隐
                    _audioTrackManager.ProfileLoaded += OnAudioAvailabilityChanged;
                    _audioTrackManager.SettingsChanged += OnAudioAvailabilityChanged;
                    _audioTrackManager.TrackChanged += OnAudioTrackChangedForBar;

                    // AI 匹配只按需读取页面文本，使用独立的 PDF 服务实例避免干扰阅读器自己的文档句柄
                    var aiService = Program.ServiceProvider?.GetService(typeof(IAIService)) as IAIService;
                    var pdfTextLogger = Program.ServiceProvider?
                        .GetService(typeof(ILogger<PdfiumPdfService>)) as ILogger<PdfiumPdfService>;
                    var aiMatcher = aiService != null && pdfTextLogger != null
                        ? new PdfAudioAiMatcher(aiService, new PdfiumPdfService(pdfTextLogger), _logger)
                        : null;

                    _audioPanel = new PdfAudioPanel(_audioTrackManager, this, aiMatcher) { Dock = DockStyle.Fill };

                    // 底部播放器是配套音频的唯一播放入口，挂到画布底部、状态栏之上
                    // 默认隐藏：等新文档的音频档案加载完成并确认存在曲目后再显示，避免无音频时占用画布高度
                    _audioPlayerBar = new PdfAudioPlayerBar(_audioTrackManager, this, ShowAudioCompanion)
                    {
                        Visible = false
                    };
                    _panelPdf.Controls.Add(_audioPlayerBar);
                }
            }
            catch { /* 配套音频管理器初始化失败，不阻断 PDF 阅读 */ }

            // 初始化音频录制管理器（如果音频录制服务可用）
            var audioRecorderService = _audioRecorderService;
            if (audioRecorderService == null)
            {
                try { audioRecorderService = Program.ServiceProvider?.GetService(typeof(IAudioRecorderService)) as IAudioRecorderService; }
                catch { /* 录音服务不可用，跳过音频管理器初始化 */ }
            }
            if (audioRecorderService != null)
            {
                var pdfAiSummaryStore = Program.ServiceProvider?.GetService(typeof(IPdfAiSummaryStore)) as IPdfAiSummaryStore;
                _audioManager = new PdfReaderAudioManager(_logger, this, audioRecorderService,
                    _pdfAiService, pdfAiSummaryStore, _annotationService)
                { MountHost = _recordingHost };
                _audioManager.Initialize();
                _recordingServiceAvailable = true;
            }


            InitializeColorButtonLayout();
            InitializeHighlightColorSwatches();
            InitializeToolbarButtonHoverEffects();
            InitializeAnnotationToolStrip();

            // 画笔子类型按钮初始隐藏，仅在进入 Pen 模式后显示
            if (_buttonPenTypePencil != null) _buttonPenTypePencil.Visible = false;
            if (_buttonPenTypePen != null) _buttonPenTypePen.Visible = false;
            if (_buttonPenTypeMarker != null) _buttonPenTypeMarker.Visible = false;

            // 工具栏分组绘制分隔线
            _toolbarGroupTools.Paint += ToolbarGroupTools_Paint;


            // 工具栏按钮不参与 Tab 焦点切换，确保键盘快捷键直达 PDF 视图
            var tabStopButtons = new Control?[]
            {
                _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenMode, _buttonTemporaryPenMode, _buttonPenTypePencil, _buttonPenTypePen, _buttonPenTypeMarker,
                _buttonMosaicMode, _buttonTextMode, _buttonStrikethroughMode, _buttonEraserMode,
                _buttonUndoAnnotation, _buttonClearAllAnnotations, _buttonDashStyle,
                _buttonToggleAnnotationLayer, _buttonChecklistMode, _buttonImageEmbedMode,
                _buttonSpotlightMode, _buttonMagnifierMode, _buttonAskAi, _buttonTranslationToggle, _buttonCollapseNav,
                _buttonPrev, _buttonNext, _buttonZoomOut, _buttonZoomIn, _buttonResetView,
                _buttonRotate, _buttonLockView, _buttonNightMode, _buttonDualPage, _buttonFullscreen,
                _buttonAddToLearningContent
            };
            foreach (var ctrl in tabStopButtons)
            {
                if (ctrl is Button btn) btn.TabStop = false;
            }

            // 翻译 / 高亮 / 听读三个侧栏面板统一为同一套设计语言
            ApplySidebarRedesign();
        }

        #endregion

        #region 选择联动与工具栏视觉

        /// <summary>
        /// 高亮被选中（画布点击或列表选中）时，回显该高亮已缓存的识别文本与译文，
        /// 不再重新发起 OCR/翻译。
        /// </summary>
        private void OnHighlightSelected(PdfHighlight highlight)
        {
            if (highlight == null) return;

            // 没有缓存文本时保持现状，避免清空用户当前正在查看的内容
            if (string.IsNullOrWhiteSpace(highlight.Text)) return;

            _textBoxOriginal.Text = highlight.Text;
            // 译文同样判空：高亮尚未翻译时保留用户当前正在查看的译文
            if (!string.IsNullOrWhiteSpace(highlight.TranslationText))
                _textBoxTranslation.Text = highlight.TranslationText;
            UpdateStatusBar();
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

        #region 工具栏颜色常量

        private static readonly Color ToolbarHoverDay = Color.FromArgb(230, 230, 230);   // #e6e6e6
        private static readonly Color ToolbarHoverNight = Color.FromArgb(60, 60, 60);     // #3c3c3c
        private static readonly Color ToolbarLeaveDay = Color.Transparent;
        private static readonly Color ToolbarLeaveNight = Color.FromArgb(45, 45, 45);     // #2d2d2d
        private static readonly Color ToolbarPressedDay = Color.FromArgb(200, 200, 200);  // #c8c8c8
        private static readonly Color ToolbarPressedNight = Color.FromArgb(42, 42, 42);   // #2a2a2a

        // 二级选项改为浮层后，工具栏只需容纳单行工具（一级图标条 + 视图/页码组 + 加载指示）
        private const int ToolbarSingleRowHeight = 58;

        // 缩放区间：滑块与滚轮共用同一范围，避免两者钳位不一致
        private const int MinZoomPercent = 10;
        private const int MaxZoomPercent = 400;

        // 工具栏组间分隔线 X 坐标（由 LayoutToolbarGroups 在运行期计算，-1 表示不绘制）
        private int _toolbarViewSepX = -1;
        private int _toolbarPageSepX = -1;

        #endregion

        private Button?[] GetAllToolbarButtons()
        {
            return new[]
            {
                _buttonPrev, _buttonNext,
                _buttonZoomOut, _buttonZoomIn, _buttonResetView,
                _buttonRotate, _buttonLockView, _buttonNightMode,
                _buttonDualPage, _buttonFullscreen,
                _buttonEraserMode, _buttonHighlightMode, _buttonRectangleMode, _buttonEllipseMode,
                _buttonArrowMode, _buttonPenMode, _buttonTemporaryPenMode, _buttonPenTypePencil, _buttonPenTypePen, _buttonPenTypeMarker,
                _buttonMosaicMode, _buttonTextMode, _buttonStrikethroughMode,
                _buttonUndoAnnotation, _buttonClearAllAnnotations, _buttonDashStyle,
                _buttonToggleAnnotationLayer, _buttonChecklistMode, _buttonImageEmbedMode,
                _buttonSpotlightMode, _buttonMagnifierMode, _buttonAskAi, _buttonTranslationToggle, _buttonCollapseNav
            };
        }

        private void InitializeToolbarButtonHoverEffects()
        {
            foreach (var button in GetAllToolbarButtons())
            {
                if (button != null)
                {
                    button.MouseEnter += ToolbarButton_MouseEnter;
                    button.MouseLeave += ToolbarButton_MouseLeave;
                    button.MouseDown += ToolbarButton_MouseDown;
                    button.MouseUp += ToolbarButton_MouseUp;
                }
            }
        }

        private void ToolbarButton_MouseEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (button.FlatAppearance.BorderSize >= 2) return;
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                button.BackColor = isNightMode ? ToolbarHoverNight : ToolbarHoverDay;
            }
        }

        private void ToolbarButton_MouseLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (button.FlatAppearance.BorderSize >= 2) return;
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                button.BackColor = isNightMode ? ToolbarLeaveNight : ToolbarLeaveDay;
            }
        }

        private void ToolbarButton_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is Button button && e.Button == MouseButtons.Left)
            {
                if (button.FlatAppearance.BorderSize >= 2) return;
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                button.BackColor = isNightMode ? ToolbarPressedNight : ToolbarPressedDay;
            }
        }

        private void ToolbarButton_MouseUp(object? sender, MouseEventArgs e)
        {
            if (sender is Button button && e.Button == MouseButtons.Left)
            {
                if (button.FlatAppearance.BorderSize >= 2) return;
                bool isNightMode = _nightModeManager?.IsNightMode ?? false;
                button.BackColor = isNightMode ? ToolbarHoverNight : ToolbarHoverDay;
            }
        }

        #endregion

        #region 视图事件、TTS 与窗体加载

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
            set
            {
                _currentPageIndex = value;
                // 停留在「学习」Tab 时翻页需要同步刷新本页统计
                if (_tabControlLeft != null && _tabControlLeft.SelectedTab == _tabPageStudy)
                    UpdateStudyTab();
            }
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
        public TextBox? TextBoxTranslation => _textBoxTranslation;
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

        /// <summary>翻译后是否使用 AI 术语表润色</summary>
        public bool AiTranslationPolishEnabled => _checkBoxAiPolish?.Checked ?? false;

        public TabPage? TabPageBookmarksAndHighlights => _tabPageBookmarksAndHighlights;

        public ListBox? ListBoxHighlights => _listBoxHighlights;
        public Button? ButtonRemoveHighlight => _buttonRemoveHighlight;
        public Button? ButtonBatchRemoveHighlight => _buttonBatchRemoveHighlight;

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
        PdfReaderHighlightManager? IPdfReaderFormAccess.HighlightManager => _highlightManager;

        public TabPage? TabPageAudio => _tabPageAudio;
        public IAudioRecorderService? AudioRecorderService => _audioRecorderService;

        public Button? ButtonLanguage => null;


        public void OnSelectOcrClicked() => SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        public void OnTranslateClicked() => TranslateClicked?.Invoke(this, EventArgs.Empty);
        public void OnOcrPageClicked() => OcrPageClicked?.Invoke(this, EventArgs.Empty);

        #endregion

        private void PdfReaderFormV2_Load(object? sender, EventArgs e)
        {
            EnsureSidebarStateLoaded();
            _tabControlLeft.SelectedIndexChanged += TabControlLeft_SelectedIndexChanged;
            RestoreSidebarTabSelection();
            // 「图标 + 名称」页签：自绘 + 宽度自适应 + 悬停提示
            _tabControlLeft.DrawItem += TabControlLeft_DrawItem;
            _tabControlLeft.MouseMove += TabControlLeft_MouseMove;
            _tabControlLeft.MouseLeave += TabControlLeft_MouseLeave;
            _tabControlLeft.SizeChanged += (_, __) => UpdateSidebarTabItemSize();
            _sidebarTipTimer = new System.Windows.Forms.Timer { Interval = 320 };
            _sidebarTipTimer.Tick += (_, __) =>
            {
                _sidebarTipTimer?.Stop();
                ShowSidebarTabToolTip(_sidebarHoveredTabIndex);
            };
            _sidebarHoverSwitchTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _sidebarHoverSwitchTimer.Tick += SidebarHoverSwitchTimer_Tick;
            BuildSidebarTabContextMenu();
            _sidebarTabFont ??= new Font("Segoe UI Emoji", 12F);
            _sidebarTabLabelFont ??= new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);
            UpdateSidebarTabItemSize();
            // 页签已显示名称，无障碍名称补充完整分组说明
            foreach (TabPage page in _tabControlLeft.TabPages)
                page.AccessibleName = GetSidebarTabTitle(page);

            AdjustPanelPdfSize();
            InitializeEmptyState();
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
                // 抽屉打开时 Esc 优先关闭抽屉，避免误触退出全屏或切到其他模式
                if (e.KeyCode == Keys.Escape && _audioDrawer?.Visible == true && _audioDrawer.ContainsFocus)
                {
                    HideAudioDrawer();
                    e.Handled = e.SuppressKeyPress = true;
                    return;
                }

                // Ctrl+K 命令搜索：把常用入口聚合成一个键盘入口
                if (e.Control && e.KeyCode == Keys.K)
                {
                    ShowCommandPalette();
                    e.Handled = e.SuppressKeyPress = true;
                    return;
                }

                // 全局媒体键：Ctrl+Space 在任何位置都能切换播放
                if (e.Control && e.KeyCode == Keys.Space && _audioTrackManager != null)
                {
                    _audioTrackManager.TogglePlayback();
                    e.Handled = e.SuppressKeyPress = true;
                    return;
                }

                // Ctrl+1..4 直达侧栏四个工作页
                if (e.Control && e.KeyCode >= Keys.D1 && e.KeyCode <= Keys.D4)
                {
                    int tabIndex = e.KeyCode - Keys.D1;
                    if (tabIndex < _tabControlLeft.TabPages.Count)
                        _tabControlLeft.SelectedIndex = tabIndex;
                    e.Handled = e.SuppressKeyPress = true;
                    return;
                }

                // 输入框内不响应翻页/媒体快捷键，避免吞掉空格与方向键
                if (ActiveControl is TextBoxBase) return;

                // 焦点在播放器或音频侧栏时，空格/方向键按媒体惯例工作
                if (_audioTrackManager != null && IsMediaShortcutScope())
                {
                    switch (e.KeyCode)
                    {
                        case Keys.Space:
                            _audioTrackManager.TogglePlayback();
                            e.Handled = e.SuppressKeyPress = true;
                            return;
                        case Keys.Left:
                            SeekAudio(-5);
                            e.Handled = e.SuppressKeyPress = true;
                            return;
                        case Keys.Right:
                            SeekAudio(5);
                            e.Handled = e.SuppressKeyPress = true;
                            return;
                    }
                }

                // Shift+←/→ 在任意非输入位置快退/快进 30 秒
                if (e.Shift && _audioTrackManager != null && (e.KeyCode == Keys.Left || e.KeyCode == Keys.Right))
                {
                    SeekAudio(e.KeyCode == Keys.Left ? -30 : 30);
                    e.Handled = e.SuppressKeyPress = true;
                    return;
                }

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
                        _navigationManager?.DeleteSelectedAnnotation();
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
                        _pictureBoxPdf.Invalidate();
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
                    case Keys.Menu:
                        // Alt（单独按下）→ 切换到选择模式
                        if (!e.Control && !e.Shift)
                        {
                            SetAnnotationToolMode(AnnotationToolMode.Select);
                            e.Handled = true;
                            e.SuppressKeyPress = true;
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
                            PrintCurrentPdf();
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
                    case Keys.C:
                        SetAnnotationToolMode(AnnotationToolMode.Checklist);
                        e.Handled = true;
                        break;
                    case Keys.I:
                        SetAnnotationToolMode(AnnotationToolMode.ImageEmbed);
                        e.Handled = true;
                        break;
                    case Keys.K:
                        SetAnnotationToolMode(AnnotationToolMode.Spotlight);
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling keyboard shortcut");
            }
        }

        /// <summary>切歌同步页码：只移动正文，不反向触发音频切换。</summary>
        private void OnAudioPageSyncRequested(object? sender, int pageIndex)
        {
            if (IsDisposed) return;
            if (pageIndex < 0 || pageIndex >= (_presenter?.PageCount ?? 0)) return;
            NavigateToPage(pageIndex);
        }

        /// <summary>焦点是否在播放器或音频侧栏内；只有此范围才启用空格/方向键媒体快捷键。</summary>
        private bool IsMediaShortcutScope()
            => _audioPlayerBar?.ContainsFocus == true
               || _audioPanel?.ContainsFocus == true
               || _audioDrawer?.ContainsFocus == true;

        /// <summary>相对当前进度快进/快退指定秒数，并夹在 [0, 时长] 内。</summary>
        private void SeekAudio(double seconds)
        {
            if (_audioTrackManager == null) return;
            var player = _audioTrackManager.Playback.Player;
            if (player.Duration <= TimeSpan.Zero) return;

            var target = player.Position + TimeSpan.FromSeconds(seconds);
            if (target < TimeSpan.Zero) target = TimeSpan.Zero;
            if (target > player.Duration) target = player.Duration;
            _audioTrackManager.Playback.Seek(target);
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

                string fileName = string.Empty;
                if (!string.IsNullOrEmpty(_currentPdfPath))
                {
                    fileName = Path.GetFileName(_currentPdfPath);
                    if (fileName.Length > 30)
                    {
                        fileName = fileName.Substring(0, 27) + "...";
                    }
                }

                // 页码与阅读进度在上方工具栏和进度条已有呈现，此处只保留文件名与页码
                string pageInfo = $"第 {currentPage} 页 / 共 {totalPages} 页";
                _statusLabelLeft.Text = string.IsNullOrEmpty(fileName) ? pageInfo : $"{fileName}  ·  {pageInfo}";
            }
            if (_statusLabelRight != null)
            {
                string mode = GetCurrentToolModeText();
                string dualPage = _isDualPage ? "双页" : "单页";

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
                // 缩放值已显示在工具栏，状态栏只保留工具与页模式，避免同一信息出现三处
                _statusLabelRight.Text = $"{mode}  ·  {dualPage}{highlightInfo}";
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

        private void StartPageTransition(bool forward)
        {
            _navigationManager?.StartPageTransition(forward);
        }

        private void PageTransitionTimer_Tick(object? sender, EventArgs e)
        {
            _navigationManager?.OnPageTransitionTick();
        }

        #endregion

        #region 高亮、视图与状态栏

        private void RadioHighlightColor_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not RadioButton radio) return;

            radio.FlatAppearance.BorderSize = radio.Checked ? 2 : 0;
            radio.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);

            int colorIndex = GetHighlightColorIndex(radio);
            if (radio.Checked && colorIndex > 0)
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

        /// <summary>颜色按钮不在 Tag 中存索引（Tag 留给主题系统），改按引用映射。</summary>
        private int GetHighlightColorIndex(RadioButton radio)
        {
            if (ReferenceEquals(radio, _radioHighlightYellow)) return 1;
            if (ReferenceEquals(radio, _radioHighlightGreen)) return 2;
            if (ReferenceEquals(radio, _radioHighlightBlue)) return 3;
            if (ReferenceEquals(radio, _radioHighlightPink)) return 4;
            if (ReferenceEquals(radio, _radioHighlightOrange)) return 5;
            return 0;
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

        private void ListBoxHighlights_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is IPdfNavigatable item)
            {
                NavigateToItem(item);
            }
        }

        private void ListBoxHighlights_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem == null) return;

            string? id = null;
            int targetPageIndex = -1;

            switch (_listBoxHighlights.SelectedItem)
            {
                case PdfHighlight highlight:
                    id = highlight.Id;
                    targetPageIndex = highlight.PageIndex;
                    break;
                case PdfAnnotationItem annotation:
                    id = annotation.Id;
                    targetPageIndex = annotation.PageIndex;
                    break;
            }

            if (string.IsNullOrEmpty(id)) return;

            // 如果标注不在当前页，先导航到目标页，等页面渲染后再选中
            if (targetPageIndex >= 0 && targetPageIndex != _currentPageIndex)
            {
                _pendingSelectionId = id;
                _presenter?.RenderPage(targetPageIndex);
                return;
            }

            // 在当前页，直接选中
            _navigationManager?.SelectAnnotationById(id);
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

            // 只有 90°/270° 需要交换宽高，180° 画布尺寸保持不变
            bool swapDimensions = angle % 180 != 0;
            int targetWidth = swapDimensions ? bitmap.Height : bitmap.Width;
            int targetHeight = swapDimensions ? bitmap.Width : bitmap.Height;

            var rotated = new Bitmap(targetWidth, targetHeight);
            rotated.SetResolution(bitmap.HorizontalResolution, bitmap.VerticalResolution);

            using (var g = Graphics.FromImage(rotated))
            {
                g.TranslateTransform(rotated.Width / 2f, rotated.Height / 2f);
                g.RotateTransform(angle);
                g.TranslateTransform(-bitmap.Width / 2f, -bitmap.Height / 2f);
                g.DrawImage(bitmap, Point.Empty);
            }

            // 传入位图的生命周期由调用方管理，避免旋转失败后回退到已释放对象
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
            string currentFontFamily = string.IsNullOrEmpty(annotation.FontFamily) ? "Microsoft YaHei UI" : annotation.FontFamily;

            using var dialog = new TextAnnotationDialog(
                "编辑文字注解",
                annotation.Text,
                currentColor,
                annotation.FontSize,
                currentFontFamily);

            var result = dialog.ShowDialog(this);

            if (result == DialogResult.OK)
            {
                try
                {
                    _presenter?.UpdateTextAnnotation(
                        annotation,
                        dialog.InitialText,
                        dialog.SelectedColor.ToArgb(),
                        dialog.SelectedFontSize,
                        dialog.SelectedFontFamily);

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

        private void RefreshHighlightList()
        {
            _highlightManager?.RefreshHighlightList();
            UpdateHighlightEmptyState();
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

        #endregion

        #region 文档加载与页面渲染

        /// <summary>打开右侧音频抽屉的「配套音频」视图，供播放器空态/去绑定入口调用。</summary>
        public void ShowAudioCompanion()
        {
            ShowAudioDrawer(AudioDrawerMode.Audio);
            _audioPanel?.RefreshData();
        }

        private void OnAudioAvailabilityChanged(object? sender, EventArgs e)
            => UpdateAudioPlayerBarVisibility();

        private void OnAudioTrackChangedForBar(object? sender, AudioTrack track)
            => UpdateAudioPlayerBarVisibility();

        /// <summary>切换文档时先收起播放条，等新文档的音频档案加载完成后再决定是否显示。</summary>
        private void HideAudioPlayerBarForDocumentSwitch()
        {
            if (_audioPlayerBar == null) return;
            _audioPlayerBar.Visible = false;
        }

        /// <summary>
        /// 只有当前文档确实扫描到配套音频时才显示底部播放条；
        /// 无音频时保持隐藏，把 64px 高度还给画布。
        /// </summary>
        private void UpdateAudioPlayerBarVisibility()
        {
            if (_audioPlayerBar == null || _audioTrackManager == null || IsDisposed) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(UpdateAudioPlayerBarVisibility));
                return;
            }

            bool hasTracks = _audioTrackManager.Result?.Tracks.Count > 0;
            if (_audioPlayerBar.Visible == hasTracks) return;

            _audioPlayerBar.Visible = hasTracks;
            _panelPdf.PerformLayout();
            if (hasTracks) _audioPlayerBar.RefreshPage();
        }

        /// <summary>
        /// 设置当前PDF文件路径，并刷新相关UI状态
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="pageCount">文档页数；-1 表示从 Presenter 读取</param>
        public void SetCurrentPdfPath(string pdfPath, int pageCount = -1)
        {
            CleanupHighlightLayer();
            ClearThumbnails();
            _thumbnailsLoaded = false;
            bool documentChanged = !string.Equals(_currentPdfPath, pdfPath, StringComparison.OrdinalIgnoreCase);
            _currentPdfPath = pdfPath;
            // 新文档的音频档案是异步加载的：先收起播放条，避免上一本文档的曲目或空条残留
            HideAudioPlayerBarForDocumentSwitch();
            if (documentChanged)
                RestoreSidebarTabSelection();
            RefreshHighlightList();
            // 打开文档时渲染器尚未初始化，优先使用调用方传入的页数，避免按 0 页匹配导致配套音频不加载
            int resolvedPageCount = pageCount >= 0 ? pageCount : (_presenter?.PageCount ?? 0);
            _audioTrackManager?.LoadProfileAsync(pdfPath, resolvedPageCount)
                .ContinueWith(t => _logger.LogError(t.Exception, "加载PDF配套音频失败: {PdfPath}", pdfPath),
                    TaskContinuationOptions.OnlyOnFaulted);
            _audioPanel?.RefreshData();

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
            // 空文档或加载失败时 count 可能为 0，进度条 Minimum 为 1，Maximum 不得小于 Minimum
            _progressBarPage.Maximum = Math.Max(1, count);
            UpdateStatusBar();
        }

        public void SetCurrentPageIndex(int pageIndex)
        {
            bool isForward = pageIndex > _currentPageIndex;
            // 翻页（页码变化）时同步清除上一页的选中状态，避免选中残留到新页
            if (pageIndex != _currentPageIndex)
                _navigationManager?.ClearSelection();
            _currentPageIndex = pageIndex;
            _audioTrackManager?.OnPageChanged(pageIndex);
            _audioPanel?.OnCurrentPageChanged();
            _audioPlayerBar?.RefreshPage();
            _audioManager?.RefreshTimestampList();
            _textBoxPage.Text = (pageIndex + 1).ToString();
            // 页码可能超出进度条范围（如空文档或页码未同步），避免 Value 越界抛异常
            _progressBarPage.Value = Math.Clamp(pageIndex + 1, _progressBarPage.Minimum, _progressBarPage.Maximum);
            StartPageTransition(isForward);
            UpdateStatusBar();
        }

        public void SetPageText(int pageIndex, string text)
        {
        }

        /// <summary>
        /// 显示PDF页面图像
        /// 所有权约定：视图接管传入位图并负责释放，调用方不得再释放。
        /// </summary>
        /// <param name="image">要显示的Bitmap图像</param>
        public void DisplayImage(Bitmap image)
        {
            // 绘制/拖拽期间禁止换图：CleanupAnnotationBitmap 会使 FinalizeStroke 拿不到位图而丢弃当前笔画
            if (_navigationManager?.IsUserInteractionActive == true)
            {
                // 仅保留最新一帧，被替换的挂起位图由视图释放
                if (!ReferenceEquals(_pendingDisplayImage, image))
                {
                    SafeDisposeImage(ref _pendingDisplayImage);
                }
                _pendingDisplayImage = image;
                return;
            }

            // 交互已结束但挂起帧尚未应用时，新帧直接取代它，避免泄漏
            SafeDisposeImage(ref _pendingDisplayImage);
            // 页面成功显示后移除空状态引导
            if (_panelEmptyState != null && _panelEmptyState.Visible)
                _panelEmptyState.Visible = false;
            Bitmap bmp = image;
            Bitmap? rotatedImage = null;
            try
            {
                CleanupAnnotationBitmap();
                CleanupHighlightLayer();

                Bitmap imageToDisplay = bmp;

                if (_rotationAngle != 0)
                {
                    // 旋转结果单独持有，异常时仍可安全回退到原始位图
                    rotatedImage = RotateBitmap(bmp, _rotationAngle);
                    imageToDisplay = rotatedImage;
                }

                SafeDisposeImage(ref _currentPageImage);
                _currentPageImage = imageToDisplay;

                _pictureBoxPdf.Image = null;

                // 同步重建高亮图层（数据已在内存缓存中，无需磁盘IO）
                _highlightManager?.UpdateHighlightLayer();

                // 刷新高亮/标注列表（页面渲染后 PageCount 已就绪，确保标注项正确加载）
                RefreshHighlightList();

                // 处理跨页选中：用户在列表中点击了其他页的标注，导航到此页后执行选中
                if (_pendingSelectionId != null)
                {
                    string id = _pendingSelectionId;
                    _pendingSelectionId = null;
                    _navigationManager?.SelectAnnotationById(id);
                }

                _pictureBoxPdf.Invalidate();

                // 标注位图构建较重，放到后台线程
                int pageIndexAtDisplay = _currentPageIndex;
                string pdfPathAtDisplay = _currentPdfPath;
                _ = Task.Run(() => LoadAnnotationsAsync(pageIndexAtDisplay, pdfPathAtDisplay));

                // 旋转结果已提交到界面，原始未旋转位图不再需要
                if (rotatedImage != null)
                {
                    bmp.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisplayImage");

                // 回退到未旋转的原始位图；已挂到 _currentPageImage 上的旋转图一并释放，避免残留
                if (rotatedImage != null && !ReferenceEquals(_currentPageImage, rotatedImage))
                {
                    rotatedImage.Dispose();
                    rotatedImage = null;
                }

                if (!ReferenceEquals(_currentPageImage, bmp))
                {
                    SafeDisposeImage(ref _currentPageImage);
                }

                _currentPageImage = bmp;
                _pictureBoxPdf.Image = null;
                _pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
                _navigationManager?.LoadAnnotationsForCurrentPage();
            }
        }

        /// <summary>
        /// 绘制/拖拽结束后应用被挂起的页面位图。
        /// </summary>
        private void FlushPendingDisplayImage()
        {
            if (_pendingDisplayImage == null) return;
            if (_navigationManager?.IsUserInteractionActive == true) return;

            var pending = _pendingDisplayImage;
            _pendingDisplayImage = null;
            DisplayImage(pending);
        }

        private void PictureBoxPdf_MouseUp(object? sender, MouseEventArgs e)
        {
            _navigationManager?.MouseUp(sender, e);
            // MouseUp 内部会结束绘制/拖拽状态，此时再应用挂起的页面位图
            FlushPendingDisplayImage();
        }

        void IPdfReaderFormAccess.DisplayImage(Bitmap bmp)
        {
            // 直接转交，不再做 PNG 编码中转
            DisplayImage(bmp);
        }

        void IPdfReaderFormAccess.ReloadAnnotationLayer()
        {
            // 仅重载标注图层并刷新，不触发整页重新渲染
            _navigationManager?.LoadAnnotationsForCurrentPage();
            _pictureBoxPdf?.Invalidate();
        }

        private async Task LoadAnnotationsAsync(int pageIndex, string pdfPath)
        {
            try
            {
                int revisionAtLoad = _navigationManager?.AnnotationRevision ?? 0;

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

                if (IsDisposed || !IsHandleCreated || _pictureBoxPdf.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_currentPageIndex != pageIndex || _currentPdfPath != pdfPath)
                            return;
                        // 标注内容已变化（新增/撤销/删除/重载），丢弃这份过期图层
                        if ((_navigationManager?.AnnotationRevision ?? 0) != revisionAtLoad)
                            return;
                        if (_currentPageImage == null) return;

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
                int revisionAtLoad = _navigationManager?.SecondAnnotationRevision ?? 0;

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

                if (IsDisposed || !IsHandleCreated || _pictureBoxPdf.IsDisposed) return;

                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (_currentPageIndex + 1 != pageIndex || _currentPdfPath != pdfPath)
                            return;
                        // 第二页标注内容已变化，丢弃过期图层
                        if ((_navigationManager?.SecondAnnotationRevision ?? 0) != revisionAtLoad)
                            return;
                        if (_secondPageImage == null) return;

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
        /// 所有权约定：视图接管传入位图并负责释放，调用方不得再释放。
        /// </summary>
        /// <param name="image">第二页的Bitmap图像，null则清除</param>
        public void SetSecondPageImage(Bitmap? image)
        {
            Bitmap? bmp = image;
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
                // 未成功挂到 _secondPageImage 上的新位图由视图释放，避免泄漏
                if (bmp != null && !ReferenceEquals(_secondPageImage, bmp))
                {
                    bmp.Dispose();
                }
                _logger.LogError(ex, "Error in SetSecondPageImage");
            }
        }

        private async Task UpdateSecondHighlightLayerAsync(int pageIndex, string pdfPath, int imgW, int imgH)
        {
            try
            {
                if (_highlightService == null || string.IsNullOrEmpty(pdfPath)) return;

                var highlights = _highlightService.GetHighlightsForPage(pdfPath, pageIndex);

                if (IsDisposed || !IsHandleCreated || _pictureBoxPdf.IsDisposed) return;

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

        #endregion

        #region 提示、加载状态、夜间模式与缩略图

        public void ShowWarning(string message)
        {
            MessageBox.Show(this, message, WarningTitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(this, message, ErrorTitle, MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            MessageBox.Show(this, message, InfoTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(this, message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            return MessageBox.Show(this, message, title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
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
            _pictureBoxPdf.Invalidate();
        }

        public void ClearThumbnails()
        {
            _thumbnailDirectoryHeaders.Clear();
            if (_flowLayoutPanelThumbnails != null)
            {
                // 先取快照再释放：Dispose 会把控件从父集合中移除，直接遍历会抛 InvalidOperationException
                var thumbnails = _flowLayoutPanelThumbnails.Controls.Cast<Control>().ToArray();
                foreach (var control in thumbnails)
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

        #endregion

        #region 页面导航、文本、选区与 OCR 覆盖层

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
            PersistTranslationToSelectedHighlight(text);
        }

        /// <summary>
        /// 手动翻译完成后，若当前选中的高亮正是原文框中这段文字，
        /// 把译文回写到高亮实体，后续再次选中时直接回显。
        /// </summary>
        private void PersistTranslationToSelectedHighlight(string translation)
        {
            if (string.IsNullOrWhiteSpace(translation)) return;

            var highlight = _navigationManager?.SelectedHighlight;
            if (highlight == null || string.IsNullOrWhiteSpace(highlight.Text)) return;

            var originalText = _textBoxOriginal.Text?.Trim();
            if (!string.Equals(originalText, highlight.Text.Trim(), StringComparison.Ordinal)) return;
            if (string.Equals(highlight.TranslationText, translation, StringComparison.Ordinal)) return;

            highlight.TranslationText = translation;
            _highlightService?.UpdateHighlightContent(_currentPdfPath, highlight.Id, highlight.Text, translation);
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
            // 选区结果由导航管理器维护（PictureBox 客户区坐标），供 OCR/翻译按显示区域换算
            var rect = _navigationManager?.LastSelectionRect;
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
            if (_aiPanelPopupService == null)
            {
                MessageBox.Show(this, "AI面板服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selection = GetSelectedText();
            var context = string.IsNullOrWhiteSpace(selection)
                ? (_textBoxOriginal?.Text ?? string.Empty)
                : selection;

            var options = new PdfAiPanelOptions
            {
                CurrentPageIndex = _currentPageIndex,
                PageCount = _presenter?.PageCount ?? 0,
                ContextProvider = BuildPdfAiScopeContextAsync,
                QuestionHandler = AskPdfAiScopedAsync,
                PageNavigationHandler = NavigateToAiPageAsync
            };

            _aiPanelPopupService.ShowAIAbilityPanel(this, context, null, context, options);
        }

        #endregion

        #region PDF AI：上下文与摘要问答

        /// <summary>取翻译原文框中当前选中的文本</summary>
        private string GetSelectedText()
        {
            if (_textBoxOriginal == null || _textBoxOriginal.IsDisposed) return string.Empty;
            return _textBoxOriginal.SelectedText?.Trim() ?? string.Empty;
        }

        /// <summary>按 AI 面板选择的作用域构建带页码锚点的上下文</summary>
        private async Task<string> BuildPdfAiScopeContextAsync(PdfAiScope scope, int startPageNumber, int endPageNumber)
        {
            if (string.IsNullOrWhiteSpace(_currentPdfPath) || _documentTextIndex == null)
                return _textBoxOriginal?.Text ?? string.Empty;

            var documentName = Path.GetFileName(_currentPdfPath);

            // 选中内容作用域无需整份文档索引，直接用选中的文本，避免首次构建索引时等待
            if (scope == PdfAiScope.Selection)
            {
                var selection = GetSelectedText();
                if (!string.IsNullOrWhiteSpace(selection))
                    return $"文档：{documentName}\n\n[[p{_currentPageIndex + 1}]]\n{selection}\n\n";
            }

            try
            {
                var index = await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true);
                if (index == null)
                    return _textBoxOriginal?.Text ?? string.Empty;

                var result = PdfAiContextBuilder.Build(
                    index,
                    scope,
                    _currentPageIndex,
                    Math.Max(0, startPageNumber - 1),
                    Math.Max(0, endPageNumber - 1),
                    GetSelectedText());

                return $"文档：{documentName}\n\n{result.Context}";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "构建 PDF AI 上下文失败");
                return _textBoxOriginal?.Text ?? string.Empty;
            }
        }

        /// <summary>
        /// 确保当前 PDF 的文本索引可用：已有磁盘缓存直接复用，未命中时按需显示带取消的构建进度。
        /// </summary>
        private async Task<PdfTextIndexDocument?> EnsurePdfTextIndexAsync(bool showProgress)
        {
            if (string.IsNullOrWhiteSpace(_currentPdfPath) || _documentTextIndex == null)
                return null;

            var indexService = _documentTextIndex;
            var pdfPath = _currentPdfPath;

            // 缓存命中时不弹窗，避免每次 AI 操作都打断阅读
            var cached = indexService.TryLoadCached(pdfPath);
            if (cached != null) return cached;

            if (!showProgress)
            {
                try
                {
                    return await indexService.GetOrBuildAsync(pdfPath).ConfigureAwait(true);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "构建 PDF 文本索引失败");
                    return null;
                }
            }

            PdfTextIndexDocument? result = null;
            using var progressForm = new PdfTextIndexProgressForm();
            using var cancellation = new CancellationTokenSource();

            progressForm.CancelRequested += (_, _) => cancellation.Cancel();
            progressForm.Shown += async (_, _) =>
            {
                try
                {
                    var progress = new Progress<PdfTextIndexProgress>(progressForm.UpdateProgress);
                    result = await indexService
                        .GetOrBuildAsync(pdfPath, forceRebuild: false, progress, cancellation.Token)
                        .ConfigureAwait(true);
                }
                catch (OperationCanceledException)
                {
                    result = null;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "构建 PDF 文本索引失败");
                    result = null;
                }
                finally
                {
                    if (!progressForm.IsDisposed) progressForm.Close();
                }
            };

            progressForm.ShowDialog(this);
            return result;
        }

        /// <summary>
        /// 全文作用域的隐私确认：首次使用把整份文档文本发送给 AI 前提示，确认后本次会话不再重复询问。
        /// </summary>
        private bool EnsureWholeDocumentAiConsent(string operation)
        {
            if (_wholeDocumentAiConfirmed) return true;

            var result = MessageBox.Show(this,
                $"{operation}会把整份文档的文本（含 OCR 结果）发送给 AI 服务。\n\n" +
                "如果文档包含敏感信息，建议改用“当前页”或“选中内容”范围。\n\n是否继续？",
                "上传整份文档内容",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (result != DialogResult.Yes) return false;

            _wholeDocumentAiConfirmed = true;
            return true;
        }

        /// <summary>站内问答：按作用域调用 PDF AI 编排并返回可引用的页码</summary>
        private async Task<PdfAiResponse> AskPdfAiScopedAsync(
            PdfAiScope scope,
            int startPageNumber,
            int endPageNumber,
            string question,
            CancellationToken cancellationToken)
        {
            if (_pdfAiService == null)
                return PdfAiResponse.Fail("AI 服务不可用，请先完成 AI 配置");
            if (string.IsNullOrWhiteSpace(_currentPdfPath))
                return PdfAiResponse.Fail("当前没有打开的 PDF 文档");

            // 全文作用域会把整份文档文本发送给 AI，首次使用需用户显式确认
            if (scope == PdfAiScope.WholeDocument && !EnsureWholeDocumentAiConsent("全文问答"))
                return PdfAiResponse.Fail("已取消将整份文档内容发送给 AI");

            // 非选中内容作用域都依赖全文索引，先带进度地准备索引，避免调用时长时间无反馈
            if (scope != PdfAiScope.Selection)
            {
                var index = await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true);
                if (index == null)
                    return PdfAiResponse.Fail("未能建立文档文本索引，请重试或先完成 OCR");
            }

            return await _pdfAiService.AskScopedAsync(
                _currentPdfPath,
                question,
                scope,
                _currentPageIndex,
                Math.Max(0, startPageNumber - 1),
                Math.Max(0, endPageNumber - 1),
                GetSelectedText(),
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>点击答案中的页码后跳转（页码从 1 开始）</summary>
        private async Task NavigateToAiPageAsync(int pageNumber)
        {
            if (_presenter == null || _presenter.PageCount <= 0) return;

            int pageIndex = Math.Clamp(pageNumber - 1, 0, _presenter.PageCount - 1);
            await _presenter.RenderPage(pageIndex).ConfigureAwait(false);
        }

        /// <summary>总结当前页（走 A3 缓存，重复总结同一页可直接命中）</summary>
        private async Task SummarizeCurrentPageAsync()
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService.SummarizePagesAsync(_currentPdfPath, _currentPageIndex, _currentPageIndex)
                    .ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            ShowAiResponseDialog($"第 {_currentPageIndex + 1} 页 AI 总结", response, allowSave: true);
        }

        /// <summary>总结翻译页原文框中选中的内容</summary>
        private async Task SummarizeSelectionAsync()
        {
            var selection = GetSelectedText();
            if (string.IsNullOrWhiteSpace(selection))
            {
                MessageBox.Show(this, "请先在翻译面板的原文框中选中要总结的内容", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService.AskScopedAsync(
                    _currentPdfPath,
                    "请用要点总结下面选中的内容，并在需要时标注引用页码。",
                    PdfAiScope.Selection,
                    _currentPageIndex,
                    0,
                    0,
                    selection).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            ShowAiResponseDialog("选中内容 AI 总结", response, allowSave: true);
        }

        /// <summary>全文检索问答：关键词召回相关页后回答，答案页码可点击跳转</summary>
        private async Task AskDocumentByRetrievalAsync()
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var question = Microsoft.VisualBasic.Interaction.InputBox(
                "请输入要检索的问题：", "全文检索问答", string.Empty);
            if (string.IsNullOrWhiteSpace(question)) return;

            if (!EnsureWholeDocumentAiConsent("全文检索问答"))
                return;

            if (await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService.AskDocumentWithRetrievalAsync(
                    _currentPdfPath, question.Trim()).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            ShowAiResponseDialog("全文检索问答", response, allowSave: true);
        }

        /// <summary>生成整份文档的 AI 大纲，按章节与页码展示，可加入待处理内容</summary>
        private async Task GenerateDocumentOutlineAsync()
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_presenter == null || _presenter.PageCount <= 0 || string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!EnsureWholeDocumentAiConsent("AI 大纲生成")) return;

            if (await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pageCount = _presenter.PageCount;
            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService
                    .GenerateOutlineAsync(_currentPdfPath, 0, pageCount - 1)
                    .ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (response.Success && !string.IsNullOrWhiteSpace(response.Content))
            {
                var outline = FormatOutline(response.Content, pageCount);
                if (!string.IsNullOrWhiteSpace(outline))
                    response = PdfAiResponse.Ok(outline, response.ReferencedPages);
            }

            ShowAiResponseDialog("文档 AI 大纲", response, allowSave: false, allowAddToEditor: true);
        }

        /// <summary>把 AI 大纲 JSON 转成带页码的 Markdown 文本；格式异常时原样返回</summary>
        private static string FormatOutline(string content, int pageCount)
        {
            var json = StripCodeFence(content);
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Object) return json;

                var builder = new StringBuilder();
                var title = document.RootElement.TryGetProperty("title", out var titleElement)
                    ? (titleElement.GetString() ?? string.Empty).Trim()
                    : string.Empty;
                if (!string.IsNullOrWhiteSpace(title))
                    builder.AppendLine($"# {title}").AppendLine();

                if (!document.RootElement.TryGetProperty("sections", out var sections) ||
                    sections.ValueKind != JsonValueKind.Array)
                    return json;

                int index = 1;
                foreach (var section in sections.EnumerateArray())
                {
                    if (section.ValueKind != JsonValueKind.Object) continue;

                    var sectionTitle = section.TryGetProperty("title", out var titleNode)
                        ? (titleNode.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(sectionTitle))
                        sectionTitle = $"第 {index} 部分";

                    int pageNumber = section.TryGetProperty("page", out var pageNode)
                        && pageNode.TryGetInt32(out var parsedPage)
                            ? Math.Clamp(parsedPage, 0, Math.Max(0, pageCount))
                            : 0;
                    var pageSuffix = pageNumber > 0 ? $"（第 {pageNumber} 页）" : string.Empty;

                    builder.AppendLine($"## {index}. {sectionTitle}{pageSuffix}");
                    if (section.TryGetProperty("points", out var points) && points.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var point in points.EnumerateArray())
                        {
                            var text = (point.GetString() ?? string.Empty).Trim();
                            if (!string.IsNullOrWhiteSpace(text))
                                builder.AppendLine($"- {text}");
                        }
                    }
                    builder.AppendLine();
                    index++;
                }

                return builder.ToString().TrimEnd();
            }
            catch (JsonException)
            {
                return json;
            }
        }

        #endregion

        #region PDF AI：闪卡、笔记与标注整理

        /// <summary>生成指定页码范围的 AI 闪卡，预览勾选后加入闪卡复习队列</summary>
        private async Task GenerateFlashcardsAsync(PdfAiFlashcardOptions? presetOptions = null)
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_presenter == null || _presenter.PageCount <= 0 || string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_spacedRepetitionService == null)
            {
                MessageBox.Show(this, "复习服务不可用，无法加入闪卡复习", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PdfAiFlashcardOptions options;
            if (presetOptions != null)
            {
                options = presetOptions;
            }
            else
            {
                using var optionsDialog = new PdfAiFlashcardDialog(_presenter.PageCount);
                if (optionsDialog.ShowDialog(this) != DialogResult.OK) return;
                options = optionsDialog.Options;
            }
            // 全部页面生成等同全文上传，首次需要隐私确认
            if (options.StartPageIndex < 0 && !EnsureWholeDocumentAiConsent("AI 闪卡生成"))
                return;

            if (await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pageCount = _presenter.PageCount;
            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService
                    .GenerateFlashcardsAsync(_currentPdfPath, options.StartPageIndex, options.EndPageIndex)
                    .ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (!response.Success)
            {
                MessageBox.Show(this, response.ErrorMessage ?? "闪卡生成失败", "AI 闪卡",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cards = ParseFlashcards(response.Content, pageCount);
            if (cards.Count == 0)
            {
                MessageBox.Show(this, "AI 未返回有效的闪卡，请调整页码范围后重试", "AI 闪卡",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var documentName = Path.GetFileNameWithoutExtension(_currentPdfPath);
            using var previewDialog = new PdfAiFlashcardPreviewDialog(cards, documentName);
            if (previewDialog.ShowDialog(this) != DialogResult.OK) return;

            int imported = await ImportFlashcardsToReviewAsync(previewDialog.SelectedCards);
            ShowMessage(imported > 0
                ? $"已把 {imported} 张闪卡加入复习队列"
                : "未能加入复习队列，请检查复习服务",
                "AI 闪卡");
        }

        /// <summary>解析 AI 返回的闪卡 JSON，过滤缺少正面或背面的条目</summary>
        private static List<PdfAiFlashcardCard> ParseFlashcards(string content, int pageCount)
        {
            var cards = new List<PdfAiFlashcardCard>();
            if (string.IsNullOrWhiteSpace(content)) return cards;

            var json = StripCodeFence(content);
            try
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.ValueKind != JsonValueKind.Array) return cards;

                foreach (var element in document.RootElement.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.Object) continue;

                    var front = element.TryGetProperty("front", out var frontNode)
                        ? (frontNode.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    var back = element.TryGetProperty("back", out var backNode)
                        ? (backNode.GetString() ?? string.Empty).Trim()
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(front) || string.IsNullOrWhiteSpace(back)) continue;

                    int pageNumber = element.TryGetProperty("page", out var pageNode)
                        && pageNode.TryGetInt32(out var parsedPage)
                            ? Math.Clamp(parsedPage, 0, Math.Max(0, pageCount))
                            : 0;

                    cards.Add(new PdfAiFlashcardCard
                    {
                        Front = front,
                        Back = back,
                        PageNumber = pageNumber
                    });
                }
            }
            catch (JsonException)
            {
                cards.Clear();
            }

            return cards;
        }

        /// <summary>
        /// 把闪卡写入间隔重复复习队列，记录来源文档便于后续筛选；
        /// 同时双写统一学习项（P0-4），使闪卡在学习窗口按“来源=PDF 闪卡”可见。
        /// </summary>
        private async Task<int> ImportFlashcardsToReviewAsync(IReadOnlyList<PdfAiFlashcardCard> cards)
        {
            if (_spacedRepetitionService == null || cards == null || cards.Count == 0) return 0;

            var userId = _userSessionService?.CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                userId = Constants.DefaultUserId;

            var documentName = string.IsNullOrWhiteSpace(_currentPdfPath)
                ? string.Empty
                : Path.GetFileNameWithoutExtension(_currentPdfPath);

            int imported = 0;
            foreach (var card in cards)
            {
                try
                {
                    var item = _spacedRepetitionService.CreateNewItem(userId, card.Front, card.Back);
                    item.Category = "PDF闪卡";
                    if (!string.IsNullOrWhiteSpace(documentName))
                        item.Subject = documentName;
                    item.UpdatedAt = DateTime.Now;
                    _spacedRepetitionService.UpdateItem(item);

                    // 双写统一学习项：复习队列之外，学习窗口与统计也能看到该闪卡
                    if (_studyEntryService != null)
                    {
                        try
                        {
                            await _studyEntryService.AddAsync(new StudyEntryRequest
                            {
                                Text = card.Front,
                                Translation = string.IsNullOrWhiteSpace(card.Back) ? null : card.Back,
                                SourceType = "PdfAiFlashcard",
                                PdfPath = _currentPdfPath,
                                PageIndex = card.PageNumber > 0
                                    ? card.PageNumber - 1
                                    : _currentPageIndex
                            }).ConfigureAwait(true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "闪卡写入学习项失败: {Front}", card.Front);
                        }
                    }

                    imported++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "闪卡加入复习队列失败: {Front}", card.Front);
                }
            }

            return imported;
        }

        /// <summary>解释选中内容：带当前页上下文并走缓存，可添加到编辑器或保存为标注</summary>
        private async Task ExplainSelectionAsync()
        {
            if (_presenter == null)
            {
                MessageBox.Show(this, "PDF 服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selection = GetSelectedText();
            if (string.IsNullOrWhiteSpace(selection))
            {
                MessageBox.Show(this, "请先在翻译面板的原文框中选中要解释的内容", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentPdfPath) ||
                await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _presenter.ExplainSelectionWithContextAsync(selection).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            ShowAiResponseDialog("选中内容 AI 解释", response, allowSave: true, allowAddToEditor: true);
        }

        /// <summary>导出多页/全文 AI 学习笔记为 Markdown，复用批量导出的目录与重名处理</summary>
        private async Task ExportAiStudyNotesAsync()
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_presenter == null || _presenter.PageCount <= 0 || string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var documentName = Path.GetFileNameWithoutExtension(_currentPdfPath);
            using var optionsDialog = new PdfAiNotesExportDialog(_presenter.PageCount, documentName);
            if (optionsDialog.ShowDialog(this) != DialogResult.OK) return;

            var options = optionsDialog.Options;

            // 全部页面导出等同全文上传，首次需要隐私确认
            if (options.StartPageIndex < 0 && !EnsureWholeDocumentAiConsent("AI 学习笔记导出"))
                return;

            if (await EnsurePdfTextIndexAsync(showProgress: true).ConfigureAwait(true) == null)
            {
                MessageBox.Show(this, "未能建立文档文本索引，请重试或先完成 OCR", "提示",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService.GenerateStudyNotesAsync(
                    _currentPdfPath, options.StartPageIndex, options.EndPageIndex).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (!response.Success)
            {
                MessageBox.Show(this, response.ErrorMessage ?? "笔记生成失败", "AI 学习笔记导出",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var content = StripCodeFence(response.Content ?? string.Empty);
            var rangeText = options.StartPageIndex < 0
                ? $"全部页面（共 {_presenter.PageCount} 页）"
                : $"第 {options.StartPageIndex + 1}-{Math.Min(options.EndPageIndex, _presenter.PageCount - 1) + 1} 页";

            var builder = new StringBuilder();
            builder.AppendLine($"# {documentName} 学习笔记");
            builder.AppendLine();
            builder.AppendLine($"> 范围：{rangeText}　生成时间：{DateTime.Now:yyyy-MM-dd HH:mm}");
            builder.AppendLine();
            builder.AppendLine(content);
            builder.AppendLine();

            if (response.ReferencedPages.Count > 0)
            {
                builder.AppendLine("## 参考页");
                builder.AppendLine();
                foreach (var page in response.ReferencedPages.OrderBy(p => p))
                {
                    builder.AppendLine($"- 第 {page} 页");
                }
            }

            var outputPath = GetUniqueOutputPath(Path.Combine(
                options.OutputDirectory,
                SanitizeFileName(documentName) + "_学习笔记.md"));

            await File.WriteAllTextAsync(outputPath, builder.ToString(), new UTF8Encoding(false));

            // 同时写入笔记库，方便在笔记模块继续整理与复习
            bool noteSaved = false;
            if (options.SaveToNotes)
                noteSaved = SaveAiNotesToNoteLibrary(documentName, rangeText, content, response);

            var message = noteSaved
                ? $"学习笔记已导出并保存到笔记库：\n{outputPath}"
                : $"学习笔记已导出：\n{outputPath}";
            ShowMessage(message, "AI 学习笔记导出");
        }

        /// <summary>把 AI 学习笔记写入笔记库，并关联来源 PDF 与参考页</summary>
        private bool SaveAiNotesToNoteLibrary(
            string documentName, string rangeText, string content, PdfAiResponse response)
        {
            if (_noteService == null || string.IsNullOrWhiteSpace(content)) return false;

            var userId = _userSessionService?.CurrentUserId;
            if (string.IsNullOrWhiteSpace(userId))
                userId = Constants.DefaultUserId;

            try
            {
                var tags = new List<string> { "AI笔记", "PDF" };
                if (!string.IsNullOrWhiteSpace(documentName))
                    tags.Add(documentName);
                foreach (var page in response.ReferencedPages.OrderBy(p => p).Take(5))
                    tags.Add($"第{page}页");

                var note = new NoteItem
                {
                    UserId = userId,
                    Title = $"{documentName} 学习笔记（{rangeText}）",
                    Content = content,
                    Category = "PDF学习笔记",
                    Tags = tags,
                    RelatedType = "Pdf",
                    RelatedItemId = _currentPdfPath,
                    RelatedItemTitle = documentName,
                    Importance = 2,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Source = "AI"
                };

                _noteService.AddNote(userId, note);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI 学习笔记写入笔记库失败");
                return false;
            }
        }

        /// <summary>AI 整理标注：本页或全书，输出要点/待办/疑问三类清单（带页码与时间锚点）</summary>
        private async Task SummarizeAnnotationsAsync(bool wholeDocument)
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_annotationService == null || _presenter == null || string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pdfPath = _currentPdfPath;
            int pageCount = _presenter.PageCount;
            int currentPageIndex = _currentPageIndex;
            var annotationService = _annotationService;

            List<PdfAnnotationDigestItem> items;
            UseWaitCursor = true;
            try
            {
                items = await Task.Run(() => CollectAnnotationDigest(
                    annotationService, pdfPath, wholeDocument, currentPageIndex, pageCount)).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (items.Count == 0)
            {
                MessageBox.Show(this,
                    wholeDocument ? "全书中还没有可整理的标注" : "当前页还没有可整理的标注",
                    "AI 整理标注", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PdfAiResponse response;
            UseWaitCursor = true;
            try
            {
                response = await _pdfAiService.SummarizeAnnotationsAsync(pdfPath, items).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            ShowAiResponseDialog(wholeDocument ? "全书标注 AI 整理" : "本页标注 AI 整理", response, allowSave: false);
        }

        /// <summary>逐页采集标注摘要（仅页码、时间戳与文字，不含音频）</summary>
        private static List<PdfAnnotationDigestItem> CollectAnnotationDigest(
            IAnnotationService annotationService, string pdfPath, bool wholeDocument, int currentPageIndex, int pageCount)
        {
            var items = new List<PdfAnnotationDigestItem>();

            if (!wholeDocument)
            {
                items.AddRange(PdfAnnotationDigestBuilder.Collect(
                    annotationService.GetAnnotation(pdfPath, currentPageIndex), currentPageIndex));
                return items;
            }

            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                if (items.Count >= PdfAnnotationDigestBuilder.MaxItems) break;

                PdfAnnotation? annotation;
                try
                {
                    annotation = annotationService.GetAnnotation(pdfPath, pageIndex);
                }
                catch
                {
                    continue;
                }

                items.AddRange(PdfAnnotationDigestBuilder.Collect(annotation, pageIndex));
            }

            return items.Take(PdfAnnotationDigestBuilder.MaxItems).ToList();
        }

        /// <summary>AI 主题聚合全书标注并导出 Markdown 复习清单</summary>
        private async Task ExportAnnotationReviewListAsync()
        {
            if (_pdfAiService == null)
            {
                MessageBox.Show(this, "AI 服务不可用，请先完成 AI 配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_annotationService == null || _presenter == null || string.IsNullOrWhiteSpace(_currentPdfPath))
            {
                MessageBox.Show(this, "当前没有打开的 PDF 文档", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var pdfPath = _currentPdfPath;
            int pageCount = _presenter.PageCount;
            var annotationService = _annotationService;

            List<PdfAnnotationDigestItem> items;
            UseWaitCursor = true;
            try
            {
                items = await Task.Run(() => CollectAnnotationDigest(annotationService, pdfPath, true, -1, pageCount))
                    .ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (items.Count == 0)
            {
                MessageBox.Show(this, "全书中还没有可聚合的标注", "导出标注复习清单",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using var folderDialog = new FolderBrowserDialog
            {
                Description = "选择复习清单输出目录",
                SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            if (folderDialog.ShowDialog(this) != DialogResult.OK) return;

            PdfTopicClusterResult result;
            UseWaitCursor = true;
            try
            {
                result = await _pdfAiService.ClusterAnnotationsAsync(pdfPath, items).ConfigureAwait(true);
            }
            finally
            {
                UseWaitCursor = false;
            }

            if (!result.Success)
            {
                MessageBox.Show(this, result.ErrorMessage ?? "标注主题聚合失败", "导出标注复习清单",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var documentName = Path.GetFileNameWithoutExtension(pdfPath);
            var builder = new StringBuilder();
            builder.AppendLine($"# {documentName} 标注复习清单");
            builder.AppendLine();
            builder.AppendLine($"> 主题数：{result.Clusters.Count}　标注数：{result.Clusters.Sum(c => c.Items.Count)}　生成时间：{DateTime.Now:yyyy-MM-dd HH:mm}");
            builder.AppendLine();

            foreach (var cluster in result.Clusters)
            {
                builder.AppendLine($"## {cluster.Topic}");
                builder.AppendLine();
                foreach (var item in cluster.Items.OrderBy(i => i.PageIndex).ThenBy(i => i.TimestampMs ?? long.MaxValue))
                {
                    var anchor = item.TimestampMs.HasValue
                        ? $"P{item.PageIndex + 1} · {PdfAnnotationDigestBuilder.FormatTimestamp(item.TimestampMs.Value)}"
                        : $"P{item.PageIndex + 1}";
                    var text = string.IsNullOrWhiteSpace(item.Text) ? "（标注）" : item.Text.Replace('\r', ' ').Replace('\n', ' ');
                    builder.AppendLine($"- [{anchor}] {text}");
                }
                builder.AppendLine();
            }

            var outputPath = GetUniqueOutputPath(Path.Combine(
                folderDialog.SelectedPath,
                SanitizeFileName(documentName) + "_标注复习清单.md"));

            await File.WriteAllTextAsync(outputPath, builder.ToString(), new UTF8Encoding(false));
            ShowMessage($"复习清单已导出：\n{outputPath}", "导出标注复习清单");
        }

        /// <summary>去掉 AI 返回内容外层的 Markdown 代码围栏</summary>
        private static string StripCodeFence(string content)
        {
            var text = content.Trim();
            if (!text.StartsWith("```", StringComparison.Ordinal)) return text;

            int firstLineEnd = text.IndexOf('\n');
            if (firstLineEnd < 0) return text;

            text = text[(firstLineEnd + 1)..].TrimEnd();
            if (text.EndsWith("```", StringComparison.Ordinal))
            {
                text = text[..^3].TrimEnd();
            }
            return text;
        }

        /// <summary>展示 AI 结果窗口，可按需保存为当前页标注</summary>
        private void ShowAiResponseDialog(string title, PdfAiResponse response, bool allowSave,
            bool allowAddToEditor = false)
        {
            using var dialog = new PdfAiAnswerDialog(
                title,
                response,
                pageNumber => _ = NavigateToAiPageAsync(pageNumber),
                allowSave ? SaveAiSummaryAsAnnotationAsync : null,
                allowAddToEditor ? AddAiAnswerToEditor : null);
            dialog.ShowDialog(this);
        }

        /// <summary>把 AI 解释加入待处理内容，供内容编辑器直接取用</summary>
        private void AddAiAnswerToEditor(PdfAiResponse response)
        {
            var text = response.Content;
            if (string.IsNullOrWhiteSpace(text)) return;

            if (_pendingContentService == null)
            {
                MessageBox.Show(this, "待添加内容服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool isEnglish = string.Equals(GetCurrentLanguage(), "eng", StringComparison.OrdinalIgnoreCase);
            var language = isEnglish ? Constants.Language.English : Constants.Language.Chinese;
            var category = isEnglish ? Constants.SubCategory.EnglishComprehensive : Constants.SubCategory.ChineseComprehensive;
            _pendingContentService.Add(text.Trim(), language, category);

            MessageBox.Show(this, $"已添加到内容编辑器（{category}）", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>把 AI 结果保存为当前页文字标注（带 AI 标记与引用页码）</summary>
        private Task SaveAiSummaryAsAnnotationAsync(PdfAiResponse response)
        {
            if (_presenter == null || _currentPageImage == null || string.IsNullOrWhiteSpace(_currentPdfPath))
                return Task.CompletedTask;

            var content = response.Content ?? string.Empty;
            if (content.Length > 600)
            {
                content = content.Substring(0, 600) + "…";
            }

            var referencePages = response.ReferencedPages.Count > 0
                ? string.Join(",", response.ReferencedPages)
                : null;

            _presenter.AddAnnotationText(
                0.06f, 0.06f, content,
                Color.FromArgb(139, 92, 246).ToArgb(),
                11f,
                "Microsoft YaHei UI",
                _currentPageImage.Width, _currentPageImage.Height,
                null, null, true, referencePages);

            _navigationManager?.LoadAnnotationsForCurrentPage();
            RefreshHighlightList();
            _pictureBoxPdf?.Invalidate();

            return Task.CompletedTask;
        }

        #endregion

        #region 缩放、标注工具模式与工具条布局

        public int GetZoomLevel()
        {
            return _navigationManager?.ZoomLevel ?? _zoomLevel;
        }

        public void SetZoomLevel(int level)
        {
            int clampedLevel = Math.Max(MinZoomPercent, Math.Min(MaxZoomPercent, level));
            _navigationManager?.Zoom(clampedLevel);
            _trackBarZoom.Value = clampedLevel;
            _zoomLevel = clampedLevel;
            UpdateStatusBar();
        }

        public void ZoomIn()
        {
            int currentLevel = GetZoomLevel();
            int newLevel = Math.Min(MaxZoomPercent, currentLevel + 10);
            if (newLevel != currentLevel)
            {
                SetZoomLevel(newLevel);
            }
        }

        public void ZoomOut()
        {
            int currentLevel = GetZoomLevel();
            int newLevel = Math.Max(MinZoomPercent, currentLevel - 10);
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
        public event EventHandler? OcrPageClicked;
        public event EventHandler? ReTranslateClicked;
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

        private void ButtonTemporaryPenMode_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Pen");
            SetAnnotationToolMode(AnnotationToolMode.TemporaryPen, isOneTime: true);
        }

        private void ButtonStrikethroughMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Strikethrough);
        }

        private void ToolbarGroupTools_Paint(object? sender, PaintEventArgs e)
        {
            // 绘制按钮组间分隔线：X 坐标随重排结果动态变化，不再使用固定值
            bool isNightMode = _nightModeManager?.IsNightMode ?? false;
            Color sepColor = isNightMode ? Color.FromArgb(60, 60, 60) : Color.FromArgb(224, 224, 224);
            using var pen = new Pen(sepColor, 1);

            if (_toolbarViewSepX > 0)
                e.Graphics.DrawLine(pen, _toolbarViewSepX, 6, _toolbarViewSepX, 34);
            if (_toolbarPageSepX > 0)
                e.Graphics.DrawLine(pen, _toolbarPageSepX, 6, _toolbarPageSepX, 34);
        }

        private void ButtonTextMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Text);
        }

        private void SetAnnotationColor(Color color)
        {
            // 临时笔模式使用独立的颜色，不影响普通画笔颜色
            if (_navigationManager?.CurrentToolMode == AnnotationToolMode.TemporaryPen)
            {
                _navigationManager?.SetTemporaryPenColor(color);
                UpdateAnnotationColorSelection(color);
            }
            else
            {
                _navigationManager?.SetPenColor(color);
                UpdateAnnotationColorSelection(color);
            }
        }

        private void UpdateAnnotationColorSelection(Color color)
        {
            var allButtons = new[]
            {
                _buttonColorBlue, _buttonColorGreen, _buttonColorOrange, _buttonColorRed,
                _buttonColorBlack, //_buttonColorWhite,
                                   _buttonColorPurple, _buttonColorCyan,
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
                _buttonColorBlack, //_buttonColorWhite,
                                   _buttonColorBlue, _buttonColorGreen,
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

            int columns = 15;
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
        /// 初始化高亮色圆形按钮并放入标注颜色面板，
        /// 让「高亮」与「添加标注」共用同一个颜色入口，消除两处颜色选择不一致。
        /// </summary>
        private void InitializeHighlightColorSwatches()
        {
            if (_panelColor == null) return;

            _buttonHighlightColors = new[]
            {
                _buttonHighlightColor0, _buttonHighlightColor1, _buttonHighlightColor2,
                _buttonHighlightColor3, _buttonHighlightColor4
            };

            for (int i = 0; i < _buttonHighlightColors.Length && i < HighlightPalette.Length; i++)
            {
                var button = _buttonHighlightColors[i];
                var color = HighlightService.GetHighlightColor(HighlightPalette[i]).ToColor();
                button.BackColor = ToSwatchSurface(color);
                var paletteColor = HighlightPalette[i];
                button.Click += (s, e) => SelectHighlightColor(paletteColor);
                _toolTip.SetToolTip(button, $"高亮色：{paletteColor}");
                MakeButtonCircular(button);
            }
        }

        /// <summary>将带透明度的荧光色合成为不透明色，用于色板按钮显示</summary>
        private static Color ToSwatchSurface(Color color)
        {
            int alpha = color.A;
            if (alpha >= 255) return color;
            int Mix(int value) => (value * alpha + 255 * (255 - alpha)) / 255;
            return Color.FromArgb(Mix(color.R), Mix(color.G), Mix(color.B));
        }

        /// <summary>从颜色面板选择高亮颜色，并同步高亮标签页的单选项</summary>
        private void SelectHighlightColor(HighlightColor color)
        {
            _currentHighlightColor = color;
            if (_highlightManager != null) _highlightManager.CurrentHighlightColor = color;

            var radios = new[] { _radioHighlightYellow, _radioHighlightGreen, _radioHighlightBlue, _radioHighlightPink, _radioHighlightOrange };
            int index = Array.IndexOf(HighlightPalette, color);
            if (index >= 0 && index < radios.Length && radios[index] != null && !radios[index].Checked)
                radios[index].Checked = true;

            if (_navigationManager != null)
                _navigationManager.SetHighlightPreviewColor(HighlightService.GetHighlightColor(color).ToColor());

            UpdateHighlightColorSelection(color);
            _pictureBoxPdf?.Invalidate();
        }

        private void UpdateHighlightColorSelection(HighlightColor color)
        {
            if (_buttonHighlightColors == null) return;
            for (int i = 0; i < _buttonHighlightColors.Length && i < HighlightPalette.Length; i++)
            {
                var button = _buttonHighlightColors[i];
                if (button == null) continue;
                bool isMatch = HighlightPalette[i] == color;
                button.FlatAppearance.BorderSize = isMatch ? 3 : 1;
                button.FlatAppearance.BorderColor = isMatch
                    ? Color.FromArgb(64, 150, 255)
                    : Color.FromArgb(200, 200, 200);
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

        /// <summary>
        /// 参照微信截图工具栏重整标注工具条：一级图标条只保留高频工具，
        /// 低频工具收进「更多」下拉，并重新紧凑排布以消除原有的按钮位置重叠。
        /// </summary>
        private void InitializeAnnotationToolStrip()
        {
            if (_toolbarGroupTools == null) return;



            AddMoreMenuItem(moreMenu, "删除线", _buttonStrikethroughMode, () => ButtonStrikethroughMode_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "标注图层显隐", _buttonToggleAnnotationLayer, () => ButtonToggleAnnotationLayer_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "导出标注为图片", _buttonExportAnnotationsAsImage, () => ExportAnnotationsAsImage_Click(this, EventArgs.Empty));

            // 批量 OCR 导出属于文件级操作，放在「更多」中作为低频入口
            var batchOcrItem = new ToolStripMenuItem("批量OCR导出文本…");
            batchOcrItem.Image = CreateGlyphIcon("📄");
            batchOcrItem.Click += (s, e) => BatchOcrExport_Click();
            moreMenu.Items.Add(batchOcrItem);
            AddMoreMenuItem(moreMenu, "检查清单", _buttonChecklistMode, () => ButtonChecklistMode_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "插入图片", _buttonImageEmbedMode, () => ButtonImageEmbedMode_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "聚光灯", _buttonSpotlightMode, () => ButtonSpotlightMode_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "放大镜", _buttonMagnifierMode, () => ButtonMagnifierMode_Click(this, EventArgs.Empty));
            AddMoreMenuItem(moreMenu, "临时画笔", _buttonTemporaryPenMode, () => ButtonTemporaryPenMode_Click(this, EventArgs.Empty));


            // 低频工具按钮退出一级图标条
            HideFromToolStrip(_buttonStrikethroughMode);
            HideFromToolStrip(_buttonToggleAnnotationLayer);
            HideFromToolStrip(_buttonExportAnnotationsAsImage);
            HideFromToolStrip(_buttonChecklistMode);
            HideFromToolStrip(_buttonImageEmbedMode);
            HideFromToolStrip(_buttonSpotlightMode);
            HideFromToolStrip(_buttonMagnifierMode);
            HideFromToolStrip(_buttonTemporaryPenMode);


            _buttonMoreTools.BringToFront();

            // 二级选项改由浮层承载，工具栏回到单行高度，不再为其预留一行
            if (_panelNavigation != null) _panelNavigation.Height = ToolbarSingleRowHeight;
            InitializeAnnotationOptionsDropDown();

            // 窗口宽度变化时重算分组位置，避免固定坐标在窄窗口下互相压盖
            _toolbarGroupTools.Resize += (_, __) => LayoutAnnotationToolStrip();

            LayoutAnnotationToolStrip();
        }

        /// <summary>
        /// 把绘制工具的二级选项（虚线/粗细/颜色/笔型）改造成与「更多」一致的下拉浮层：
        /// 先把这些控件从工具栏摘除并移入宿主面板，宿主面板再由 ToolStripControlHost 承载，
        /// 只有选中带选项的工具时才按需弹出，因此不再占用工具栏高度。
        /// </summary>
        private void InitializeAnnotationOptionsDropDown()
        {
            if (_toolbarGroupTools == null || _panelAnnotationOptions == null) return;

            _annotationOptionsHost = new Panel
            {
                AutoSize = false,
                BackColor = Color.White
            };

            // 二级选项控件从工具栏摘除，改由浮层承载
            _toolbarGroupTools.Controls.Remove(_panelAnnotationOptions);
            if (panel2 != null) _toolbarGroupTools.Controls.Remove(panel2);
            _annotationOptionsHost.Controls.Add(_panelAnnotationOptions);
            if (panel2 != null) _annotationOptionsHost.Controls.Add(panel2);

            // 承载项与浮层都关闭自动尺寸，改由 LayoutAnnotationOptionsHost 精确指定，
            // 避免浮层复用时沿用上一次的测量结果导致尺寸不对
            _annotationOptionsHostItem = new ToolStripControlHost(_annotationOptionsHost)
            {
                AutoSize = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            _annotationOptionsDropDown = new ToolStripDropDown
            {
                AutoSize = false,
                DropShadowEnabled = true,
                Padding = Padding.Empty
            };
            _annotationOptionsDropDown.Items.Add(_annotationOptionsHostItem);
            components?.Add(_annotationOptionsDropDown);

            // 初始不展开，等选中带选项的工具时再弹出
            _panelAnnotationOptions.Visible = false;
            if (panel2 != null) panel2.Visible = false;
        }

        /// <summary>新增「更多」菜单项，图标取自对应工具按钮上的符号</summary>
        private static void AddMoreMenuItem(ContextMenuStrip menu, string text, Button? glyphSource, Action action)
        {
            var item = new ToolStripMenuItem(text);
            var glyph = glyphSource?.Text;
            if (!string.IsNullOrWhiteSpace(glyph))
            {
                item.Image = CreateGlyphIcon(glyph);
            }
            item.Click += (s, e) => action();
            menu.Items.Add(item);
        }

        /// <summary>把工具符号现场绘制成 16×16 菜单图标，避免额外引入图标资源文件</summary>
        private static Bitmap CreateGlyphIcon(string glyph)
        {
            const int size = 16;
            var bitmap = new Bitmap(size, size);
            using var g = Graphics.FromImage(bitmap);
            using var font = new Font("Segoe UI Emoji", 9F, FontStyle.Regular, GraphicsUnit.Point);
            TextRenderer.DrawText(g, glyph, font, new Rectangle(0, 0, size, size), Color.FromArgb(64, 64, 64),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            return bitmap;
        }

        private static void HideFromToolStrip(Button? button)
        {
            if (button != null) button.Visible = false;
        }

        /// <summary>按分组顺序重排一级图标条：绘制工具 → 编辑动作 → 更多 → 视图与智能</summary>
        private void LayoutAnnotationToolStrip()
        {
            const int y = 2;
            const int size = 28;
            const int gap = 4;
            const int groupGap = 12;
            int x = 5;

            x = PlaceInToolStrip(_buttonHighlightMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonRectangleMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonEllipseMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonArrowMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonPenMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonMosaicMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonTextMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonEraserMode, x, y, size, groupGap);

            x = PlaceInToolStrip(_buttonUndoAnnotation, x, y, size, groupGap);

            x = PlaceInToolStrip(_buttonMoreTools, x, y, size, groupGap);

            x = PlaceInToolStrip(_buttonNightMode, x, y, size, gap);
            x = PlaceInToolStrip(_buttonAskAi, x, y, size, gap);
            x = PlaceInToolStrip(_buttonTranslationToggle, x, y, size, gap);
            int toolsEndX = PlaceInToolStrip(_buttonClearAllAnnotations, x, y, size, gap);

            // 视图组、页码组的位置同样不能在设计器里写死，否则会与上面重排后的图标条相互压盖
            LayoutToolbarGroups(toolsEndX);
        }

        /// <summary>
        /// 按工具栏实际宽度排布右侧分组：加载指示 → 视图组 → 页码组（靠右）。
        /// 空间不足时依次收起页码进度条、缩放滑块，最后隐藏视图组，保证任何宽度下都不发生控件重叠。
        /// </summary>
        private void LayoutToolbarGroups(int toolsEndX)
        {
            if (_toolbarGroupTools == null || _toolbarGroupView == null || pnlPage == null) return;

            int available = _toolbarGroupTools.ClientSize.Width;
            if (available <= 0) return;

            const int edge = 6;
            const int gap = 12;
            // 双页模式多两个跨页按钮，页码组需要更宽
            int fullPageWidth = _isDualPage ? 396 : 343;
            int compactPageWidth = _isDualPage ? 262 : 207;
            const int fullViewWidth = 376;
            const int compactViewWidth = 230;

            int loadingLeft = toolsEndX + 2;
            if (_loadingIndicator != null)
                _loadingIndicator.Location = new Point(loadingLeft, 1);

            int viewX = loadingLeft + (_loadingIndicator?.Width ?? 0) + 6;

            // 逐级降级：完整体 → 收起页码进度条 → 收起缩放滑块 → 隐藏视图组，任何宽度下都不重叠
            bool showProgress = available >= viewX + fullViewWidth + gap + fullPageWidth + edge;
            bool fullView = available >= viewX + fullViewWidth + gap + compactPageWidth + edge;
            bool showViewGroup = available >= viewX + compactViewWidth + gap + compactPageWidth + edge;

            ApplyViewGroupCompactLayout(!fullView);

            int pageWidth = showProgress ? fullPageWidth : compactPageWidth;
            int pageX = available - pageWidth - edge;
            if (!showViewGroup)
                pageX = Math.Max(toolsEndX + gap, pageX);

            _toolbarGroupView.Location = new Point(viewX, 0);
            _toolbarGroupView.Visible = showViewGroup;
            if (_progressBarPage != null) _progressBarPage.Visible = showProgress;
            pnlPage.Width = pageWidth;
            pnlPage.Location = new Point(pageX, 0);
            ApplyPageGroupLayout();

            _toolbarViewSepX = showViewGroup ? viewX - 6 : -1;
            _toolbarPageSepX = pageX - 6;
            _toolbarGroupTools.Invalidate();
        }

        /// <summary>
        /// 页码组的单页/双页两套排布。双页模式下插入「上一跨页/下一跨页」两个按钮，
        /// 其余单页按钮和页码输入框右移，单页模式保持原有位置不变。
        /// </summary>
        private void ApplyPageGroupLayout()
        {
            if (pnlPage == null) return;

            bool dual = _isDualPage;
            if (_buttonPrevSpread != null) _buttonPrevSpread.Visible = dual;
            if (_buttonNextSpread != null) _buttonNextSpread.Visible = dual;

            if (dual)
            {
                if (_buttonPrevSpread != null) _buttonPrevSpread.Location = new Point(78, 3);
                if (_textBoxPage != null) _textBoxPage.Location = new Point(110, 7);
                if (_labelPageCount != null) _labelPageCount.Location = new Point(154, 11);
                if (_buttonNextSpread != null) _buttonNextSpread.Location = new Point(192, 3);
                if (_buttonNext != null) _buttonNext.Location = new Point(226, 3);
                if (_progressBarPage != null) _progressBarPage.Location = new Point(264, 7);
            }
            else
            {
                if (_textBoxPage != null) _textBoxPage.Location = new Point(74, 7);
                if (_labelPageCount != null) _labelPageCount.Location = new Point(120, 11);
                if (_buttonNext != null) _buttonNext.Location = new Point(171, 3);
                if (_progressBarPage != null) _progressBarPage.Location = new Point(211, 7);
            }
        }

        /// <summary>
        /// 视图组的紧凑/完整两套排布：紧凑时隐藏缩放滑块与百分比，只保留按钮，节省横向空间。
        /// </summary>
        private void ApplyViewGroupCompactLayout(bool compact)
        {
            if (_toolbarGroupView == null) return;

            if (_trackBarZoom != null) _trackBarZoom.Visible = !compact;
            if (_labelZoom != null) _labelZoom.Visible = !compact;

            PlaceViewGroupButton(_buttonZoomOut, 5);
            PlaceViewGroupButton(_buttonZoomIn, compact ? 37 : 185);
            PlaceViewGroupButton(_buttonLockView, compact ? 69 : 217);
            PlaceViewGroupButton(_buttonFullscreen, compact ? 101 : 249);
            PlaceViewGroupButton(_buttonDualPage, compact ? 133 : 281);
            PlaceViewGroupButton(_buttonResetView, compact ? 165 : 313);
            PlaceViewGroupButton(_buttonRotate, compact ? 197 : 345);

            if (!compact)
            {
                if (_trackBarZoom != null) _trackBarZoom.Location = new Point(37, 4);
                if (_labelZoom != null) _labelZoom.Location = new Point(141, 12);
            }

            _toolbarGroupView.Width = compact ? 230 : 376;
        }

        private static void PlaceViewGroupButton(Button? button, int x)
        {
            if (button != null) button.Location = new Point(x, 4);
        }

        // 注意：不能在这里判断 control.Visible —— 构造函数阶段窗体尚未显示，
        // Control.Visible 会沿父链返回 false，导致重排被整体跳过。
        private static int PlaceInToolStrip(Control? control, int x, int y, int size, int gap)
        {
            if (control == null) return x;
            control.Location = new Point(x, y);
            control.Size = new Size(size, 31);
            return x + size + gap;
        }

        /// <summary>当前工具需要的选项分组：颜色 / 粗细 / 虚实线</summary>
        private static (bool color, bool thickness, bool dash) GetOptionGroups(AnnotationToolMode mode)
        {
            switch (mode)
            {
                // 形状类：颜色 + 粗细 + 虚实线
                case AnnotationToolMode.Rectangle:
                case AnnotationToolMode.Ellipse:
                case AnnotationToolMode.Arrow:
                case AnnotationToolMode.Strikethrough:
                    return (true, true, true);
                // 手绘 / 高亮：颜色 + 粗细
                case AnnotationToolMode.Pen:
                case AnnotationToolMode.TemporaryPen:
                case AnnotationToolMode.Highlight:
                    return (true, true, false);
                // 文字：仅颜色
                case AnnotationToolMode.Text:
                    return (true, false, false);
                default:
                    return (false, false, false);
            }
        }

        /// <summary>
        /// 工具切换后更新二级选项浮层：只放当前工具需要的分组，并按可见顺序紧凑重排，
        /// 避免出现「控件还在但被隐藏」造成的空洞与错位；无选项的工具则收起浮层。
        /// </summary>
        private void UpdateAnnotationOptions(AnnotationToolMode mode)
        {
            if (_panelAnnotationOptions == null) return;

            var groups = GetOptionGroups(mode);
            bool showOptions = groups.color || groups.thickness || groups.dash;
            // 画笔笔型（铅笔/钢笔/马克笔）属于画笔的上下文选项，随画笔一起进浮层
            bool showPenTypes = mode == AnnotationToolMode.Pen || mode == AnnotationToolMode.TemporaryPen;

            if (!showOptions && !showPenTypes)
            {
                HideAnnotationOptionsDropDown();
                return;
            }

            _panelAnnotationOptions.Visible = showOptions;

            if (showOptions)
            {
                int x = 3;

                if (_buttonDashStyle != null)
                {
                    _buttonDashStyle.Visible = groups.dash;
                    if (groups.dash)
                    {
                        _buttonDashStyle.Location = new Point(x, 3);
                        x += _buttonDashStyle.Width + 9;
                    }
                }

                if (_trackBarThickness != null)
                {
                    _trackBarThickness.Visible = groups.thickness;
                    if (_labelThicknessValue != null) _labelThicknessValue.Visible = groups.thickness;
                    if (groups.thickness)
                    {
                        _trackBarThickness.Location = new Point(x, 5);
                        x += _trackBarThickness.Width + 4;
                        if (_labelThicknessValue != null)
                        {
                            _labelThicknessValue.Location = new Point(x, 9);
                            x += _labelThicknessValue.Width + 3;
                        }
                    }
                }

                LayoutColorPanel(groups.color, mode == AnnotationToolMode.Highlight);
                if (_panelColor != null)
                {
                    _panelColor.Location = new Point(x, 6);
                    _panelAnnotationOptions.Width = x + _panelColor.Width + 3;
                }

                SyncAnnotationOptionsToManager();
            }

            if (panel2 != null) panel2.Visible = showPenTypes;

            LayoutAnnotationOptionsHost(showOptions, showPenTypes);
            ShowAnnotationOptionsDropDown(mode);
        }

        /// <summary>按当前需要的分组调整浮层尺寸：选项面板在左，画笔笔型面板紧随其后</summary>
        private void LayoutAnnotationOptionsHost(bool showOptions, bool showPenTypes)
        {
            if (_annotationOptionsHost == null || _panelAnnotationOptions == null) return;

            const int pad = 6;
            const int gap = 12;
            int optionsWidth = showOptions ? _panelAnnotationOptions.Width : 0;
            int optionsHeight = showOptions ? _panelAnnotationOptions.Height : 0;
            int penWidth = showPenTypes && panel2 != null ? panel2.Width : 0;
            int penHeight = showPenTypes && panel2 != null ? panel2.Height : 0;
            int contentHeight = Math.Max(optionsHeight, penHeight);
            int spacing = showOptions && showPenTypes ? gap : 0;

            _panelAnnotationOptions.Location = new Point(pad, pad);
            if (panel2 != null && showPenTypes)
                panel2.Location = new Point(pad + optionsWidth + spacing, pad + (contentHeight - penHeight) / 2);

            // 浮层尺寸＝内容尺寸，三处同步指定以确保每次展开都由当前工具决定大小
            var size = new Size(pad * 2 + optionsWidth + spacing + penWidth, pad * 2 + contentHeight);
            _annotationOptionsHost.Size = size;
            if (_annotationOptionsHostItem != null) _annotationOptionsHostItem.Size = size;
            if (_annotationOptionsDropDown != null) _annotationOptionsDropDown.Size = size;
        }

        /// <summary>二级选项浮层的锚点按钮：优先锚定对应工具，一级图标条上不占位的工具锚定「更多」入口</summary>
        private Control? ResolveAnnotationOptionsAnchor(AnnotationToolMode mode)
        {
            switch (mode)
            {
                case AnnotationToolMode.Highlight: return _buttonHighlightMode;
                case AnnotationToolMode.Rectangle: return _buttonRectangleMode;
                case AnnotationToolMode.Ellipse: return _buttonEllipseMode;
                case AnnotationToolMode.Arrow: return _buttonArrowMode;
                case AnnotationToolMode.Pen: return _buttonPenMode;
                case AnnotationToolMode.Text: return _buttonTextMode;
                case AnnotationToolMode.TemporaryPen:
                case AnnotationToolMode.Strikethrough:
                    return _buttonMoreTools;
                default: return null;
            }
        }

        /// <summary>选中带选项的工具时，把二级选项浮层贴在该工具按钮下方弹出（与「更多」一致）</summary>
        private void ShowAnnotationOptionsDropDown(AnnotationToolMode mode)
        {
            if (_annotationOptionsDropDown == null) return;

            var anchor = ResolveAnnotationOptionsAnchor(mode);
            // 窗体或锚点尚未创建句柄时不弹出（例如构造函数阶段），避免误弹浮层
            if (anchor == null || !IsHandleCreated || !anchor.IsHandleCreated)
            {
                HideAnnotationOptionsDropDown();
                return;
            }

            if (_annotationOptionsDropDown.Visible)
                _annotationOptionsDropDown.Close();

            _annotationOptionsDropDown.Show(anchor, new Point(0, anchor.Height));
        }

        private void HideAnnotationOptionsDropDown()
        {
            if (_annotationOptionsDropDown != null && _annotationOptionsDropDown.Visible)
                _annotationOptionsDropDown.Close();
        }

        /// <summary>
        /// 颜色面板模式切换：高亮使用半透明荧光色，其余工具使用实色标注色。
        /// 使「高亮」与「添加标注」共用同一套颜色入口，避免两处颜色选择不一致。
        /// </summary>
        private void LayoutColorPanel(bool visible, bool highlightMode)
        {
            if (_panelColor == null) return;
            _panelColor.Visible = visible;

            foreach (var button in GetSolidColorButtons())
            {
                if (button != null) button.Visible = visible && !highlightMode;
            }
            if (_buttonColorMore != null) _buttonColorMore.Visible = visible && !highlightMode;

            if (_buttonHighlightColors != null)
            {
                foreach (var button in _buttonHighlightColors)
                {
                    if (button != null) button.Visible = visible && highlightMode;
                }
            }

            if (!visible) return;

            if (highlightMode)
            {
                // 进入高亮工具时同步预览色，确保预览与实际落笔颜色一致
                if (_navigationManager != null)
                    _navigationManager.SetHighlightPreviewColor(HighlightService.GetHighlightColor(_currentHighlightColor).ToColor());
                UpdateHighlightColorSelection(_currentHighlightColor);
            }
            else
            {
                Color current = _navigationManager?.CurrentToolMode == AnnotationToolMode.TemporaryPen
                    ? (_navigationManager?.TemporaryPenColor ?? Color.RoyalBlue)
                    : (_navigationManager?.PenColor ?? Color.RoyalBlue);
                UpdateAnnotationColorSelection(current);
            }
        }

        private Button[] GetSolidColorButtons() => new[]
        {
            _buttonColorBlack, _buttonColorBlue, _buttonColorGreen, _buttonColorOrange, _buttonColorRed,
            _buttonColorPurple, _buttonColorCyan, _buttonColorTeal, _buttonColorPink, _buttonColorBrown,
            _buttonColorGray, _buttonColorYellow, _buttonColorLime, _buttonColorVioletRed, _buttonColorDodgerBlue
        };

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

        #endregion

        #region 撤销、导出、批量 OCR 与标注辅助工具

        private async void ButtonUndoAnnotation_Click(object? sender, EventArgs e)
        {
            await UndoLastActionAsync();
            RefreshHighlightList();
        }
        private async void ButtonMoreTools_Click(object? sender, EventArgs e)
        {
            moreMenu.Show(_buttonMoreTools, new Point(0, _buttonMoreTools.Height));
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

            // 有选中标注时优先删除单个选中标注；无选中时才清除所有标注
            if (_navigationManager != null && _navigationManager.DeleteSelectedAnnotation())
                return;

            if (!ShowConfirm("确定要清除当前PDF的所有笔划标注吗？此操作不可撤销。", "清空确认"))
                return;

            _navigationManager?.ClearAllStrokes();
            UpdateStatusBar();
        }

        private void ButtonToggleAnnotationLayer_Click(object? sender, EventArgs e)
        {
            if (_navigationManager == null) return;
            _navigationManager.AnnotationLayerVisible = !_navigationManager.AnnotationLayerVisible;
            _buttonToggleAnnotationLayer.BackColor = _navigationManager.AnnotationLayerVisible ? Color.White : Color.FromArgb(230, 244, 255);
        }

        private void ExportAnnotationsAsImage_Click(object? sender, EventArgs e)
        {
            if (_navigationManager == null || _currentPageImage == null) return;
            using var saveDialog = new SaveFileDialog();
            saveDialog.Filter = "PNG图片|*.png";
            saveDialog.FileName = $"标注_{Path.GetFileNameWithoutExtension(_currentPdfPath)}_第{_currentPageIndex + 1}页.png";
            if (saveDialog.ShowDialog(this) == DialogResult.OK)
            {
                using var bmp = new Bitmap(_currentPageImage.Width, _currentPageImage.Height);
                using var g = Graphics.FromImage(bmp);
                g.Clear(Color.Transparent);
                _navigationManager.DrawAnnotationsToGraphics(g, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save(saveDialog.FileName, System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        #region 批量 OCR 导出

        /// <summary>
        /// 批量 OCR 导出入口：多选 PDF → 设置页码/目录/格式 → 后台逐页提取文本或 OCR → 输出 md/txt。
        /// </summary>
        private void BatchOcrExport_Click()
        {
            using var openDialog = new OpenFileDialog
            {
                Title = "选择要导出的 PDF 文件",
                Filter = "PDF 文件|*.pdf",
                Multiselect = true
            };
            if (openDialog.ShowDialog(this) != DialogResult.OK || openDialog.FileNames.Length == 0)
                return;

            using var optionsDialog = new BatchOcrExportDialog(openDialog.FileNames.Length);
            if (optionsDialog.ShowDialog(this) != DialogResult.OK)
                return;

            RunBatchOcrExport(openDialog.FileNames, optionsDialog.Options);
        }

        /// <summary>执行批量导出：进度窗口模态显示，逐文件串行处理，支持取消</summary>
        private void RunBatchOcrExport(string[] files, BatchOcrExportOptions options)
        {
            var pdfService = Program.ServiceProvider?.GetService(typeof(IPdfService)) as IPdfService;
            if (pdfService == null)
            {
                ShowWarning("PDF服务不可用，无法执行批量导出");
                return;
            }

            var ocrService = Program.ServiceProvider?.GetService(typeof(IPdfOcrService)) as IPdfOcrService;
            int successCount = 0;
            var failures = new List<string>();

            using var progressForm = new BatchOcrExportProgressForm(files.Length);
            progressForm.Shown += async (s, e) =>
            {
                try
                {
                    for (int i = 0; i < files.Length; i++)
                    {
                        if (progressForm.IsCancellationRequested) break;

                        var file = files[i];
                        progressForm.SetStatus($"正在处理 {Path.GetFileName(file)}（{i + 1}/{files.Length}）");
                        try
                        {
                            await ExportPdfTextAsync(file, options, pdfService, ocrService, progressForm);
                            successCount++;
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "批量OCR导出失败: {File}", file);
                            failures.Add($"{Path.GetFileName(file)}：{ex.Message}");
                        }
                    }
                }
                finally
                {
                    progressForm.Close();
                }
            };

            progressForm.ShowDialog(this);

            if (failures.Count == 0)
            {
                var message = progressForm.IsCancellationRequested
                    ? $"批量导出已取消，已完成 {successCount} 个文件"
                    : $"批量导出完成，共处理 {successCount} 个文件";
                ShowToast(message, 3500);
            }
            else
            {
                ShowMessage($"批量导出结束：成功 {successCount} 个，失败 {failures.Count} 个。\n\n{string.Join("\n", failures)}",
                    "批量OCR导出");
            }
        }

        /// <summary>导出单个 PDF：有文本层直接提取，无文本层才渲染并 OCR，最后写入 md/txt</summary>
        private async Task ExportPdfTextAsync(string pdfPath, BatchOcrExportOptions options, IPdfService pdfService,
            IPdfOcrService? ocrService, BatchOcrExportProgressForm progressForm)
        {
            int pageCount = pdfService.GetPageCount(pdfPath);
            if (pageCount <= 0)
                throw new InvalidOperationException("无法读取文档页数");

            int startPage = options.StartPageIndex < 0 ? 0 : Math.Min(options.StartPageIndex, pageCount - 1);
            int endPage = options.EndPageIndex < 0 ? pageCount - 1 : Math.Min(options.EndPageIndex, pageCount - 1);
            if (endPage < startPage)
                throw new InvalidOperationException($"页码范围超出文档页数（共 {pageCount} 页）");

            var baseName = Path.GetFileNameWithoutExtension(pdfPath);
            var pageTexts = new List<string>(endPage - startPage + 1);

            IPdfService? renderService = null;
            bool renderServiceLoaded = false;
            try
            {
                int totalPages = endPage - startPage + 1;
                for (int pageIndex = startPage; pageIndex <= endPage; pageIndex++)
                {
                    if (progressForm.IsCancellationRequested)
                        throw new OperationCanceledException();

                    progressForm.SetPageProgress(pageIndex - startPage + 1, totalPages);

                    string? pageText = null;
                    try
                    {
                        pageText = pdfService.ExtractText(pdfPath, pageIndex + 1);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "提取文本层失败: {File} 第 {Page} 页", pdfPath, pageIndex + 1);
                    }

                    if (string.IsNullOrWhiteSpace(pageText))
                    {
                        // 文本层为空（扫描件）→ 渲染当前页并 OCR
                        if (ocrService == null || !ocrService.IsAvailable)
                        {
                            pageText = "（OCR服务不可用，无法识别本页文字）";
                        }
                        else
                        {
                            renderService ??= new PdfiumPdfService(
                                Microsoft.Extensions.Logging.Abstractions.NullLogger<PdfiumPdfService>.Instance);
                            if (!renderServiceLoaded)
                            {
                                renderService.Load(pdfPath);
                                renderServiceLoaded = true;
                            }
                            pageText = await OcrRenderedPageAsync(renderService, ocrService, pageIndex);
                        }
                    }

                    pageTexts.Add(string.IsNullOrWhiteSpace(pageText) ? "（本页未识别到文字）" : pageText.Trim());
                }
            }
            finally
            {
                renderService?.Dispose();
            }

            // AI 智能整理为可选增强：关闭时输出与原有逻辑完全一致，失败时逐页回退原始文本
            if (options.UseAiCleanup && pageTexts.Count > 0)
            {
                var aiService = Program.ServiceProvider?.GetService(typeof(IAIService)) as IAIService;
                if (aiService != null)
                {
                    progressForm.SetStatus($"AI 正在整理 {Path.GetFileName(pdfPath)}…");
                    var cleaner = new PdfBatchTextCleaner(aiService);
                    var cleaned = await cleaner.CleanAsync(baseName, pageTexts, startPage);
                    for (int i = 0; i < pageTexts.Count && i < cleaned.Pages.Count; i++)
                    {
                        // AI 返回空页时保留原始文本，避免丢失已识别内容
                        if (!string.IsNullOrWhiteSpace(cleaned.Pages[i]))
                        {
                            pageTexts[i] = cleaned.Pages[i];
                        }
                    }
                }
            }

            var builder = new StringBuilder();
            if (options.Markdown)
            {
                builder.AppendLine($"# {baseName}");
                builder.AppendLine();
            }

            for (int i = 0; i < pageTexts.Count; i++)
            {
                if (options.Markdown)
                {
                    builder.AppendLine($"## 第 {startPage + i + 1} 页");
                    builder.AppendLine();
                    builder.AppendLine(pageTexts[i]);
                    builder.AppendLine();
                }
                else
                {
                    builder.AppendLine($"===== 第 {startPage + i + 1} 页 =====");
                    builder.AppendLine(pageTexts[i]);
                    builder.AppendLine();
                }
            }

            var extension = options.Markdown ? ".md" : ".txt";
            var outputPath = GetUniqueOutputPath(Path.Combine(options.OutputDirectory, SanitizeFileName(baseName) + extension));
            await File.WriteAllTextAsync(outputPath, builder.ToString(), new UTF8Encoding(false));
        }

        /// <summary>把页面渲染为位图后交给 OCR 识别，位图与内存流销毁前完成编码</summary>
        private static async Task<string?> OcrRenderedPageAsync(IPdfService pdfService, IPdfOcrService ocrService, int pageIndex)
        {
            var pageSize = pdfService.GetPageSize(pageIndex);
            const int targetWidth = 1600;
            int targetHeight = pageSize.Width > 0
                ? Math.Max(1, (int)Math.Round(targetWidth * pageSize.Height / pageSize.Width))
                : 2000;

            using var bitmap = pdfService.RenderPageBitmap(pageIndex, targetWidth, targetHeight);
            if (bitmap == null) return null;

            using var memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            // 批量导出同样启用自动语言检测，跨文件中英混排时无需手动切换语言
            return await ocrService.RecognizeTextWithAutoLanguageAsync(memoryStream.ToArray());
        }

        /// <summary>去掉文件名中的非法字符，避免导出失败</summary>
        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "未命名";
            var invalidChars = Path.GetInvalidFileNameChars();
            var chars = name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray();
            return new string(chars).Trim();
        }

        /// <summary>目标文件已存在时追加序号，避免覆盖已有导出结果</summary>
        private static string GetUniqueOutputPath(string path)
        {
            if (!File.Exists(path)) return path;

            var directory = Path.GetDirectoryName(path) ?? string.Empty;
            var baseName = Path.GetFileNameWithoutExtension(path);
            var extension = Path.GetExtension(path);
            for (int i = 2; i < 1000; i++)
            {
                var candidate = Path.Combine(directory, $"{baseName}_{i}{extension}");
                if (!File.Exists(candidate)) return candidate;
            }

            return Path.Combine(directory, $"{baseName}_{DateTime.Now:yyyyMMddHHmmss}{extension}");
        }

        #endregion

        private void ButtonChecklistMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Checklist);
        }

        private void ButtonImageEmbedMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.ImageEmbed);
        }

        private void ButtonSpotlightMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Spotlight);
        }

        private void ButtonMagnifierMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Magnifier);
        }

        private void ButtonStrokeStyleDot_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetStrokeStyle("DotLine");
            _buttonStrokeStyleDot.BackColor = Color.FromArgb(230, 244, 255);
            _buttonStrokeStyleArrow.BackColor = Color.White;
        }

        private void ButtonStrokeStyleArrow_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetStrokeStyle("ArrowLine");
            _buttonStrokeStyleArrow.BackColor = Color.FromArgb(230, 244, 255);
            _buttonStrokeStyleDot.BackColor = Color.White;
        }

        private void ButtonFavoriteToolbar_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ToggleFavoriteToolbar();
        }

        private void ButtonSelectMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Select);
        }

        private void ButtonEraserMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Eraser);
        }

        private void SetAnnotationToolMode(AnnotationToolMode mode, bool isOneTime = false)
        {
            _navigationManager?.SetToolMode(mode, isOneTime);

            // 画笔子类型按钮在 Pen 和 TemporaryPen 模式下可见
            // （需先于二级选项浮层布局，确保浮层展开时笔型按钮已经就位）
            bool isPenMode = mode == AnnotationToolMode.Pen || mode == AnnotationToolMode.TemporaryPen;
            if (_buttonPenTypePencil != null) _buttonPenTypePencil.Visible = isPenMode;
            if (_buttonPenTypePen != null) _buttonPenTypePen.Visible = isPenMode;
            if (_buttonPenTypeMarker != null) _buttonPenTypeMarker.Visible = isPenMode;

            // 二级选项：按需在对应工具下方弹出浮层，并重新紧凑排布分组
            UpdateAnnotationOptions(mode);

            UpdateToolButtonState(_buttonEraserMode, mode == AnnotationToolMode.Eraser);
            UpdateToolButtonState(_buttonHighlightMode, mode == AnnotationToolMode.Highlight);
            UpdateToolButtonState(_buttonRectangleMode, mode == AnnotationToolMode.Rectangle);
            UpdateToolButtonState(_buttonEllipseMode, mode == AnnotationToolMode.Ellipse);
            UpdateToolButtonState(_buttonArrowMode, mode == AnnotationToolMode.Arrow);
            UpdateToolButtonState(_buttonMosaicMode, mode == AnnotationToolMode.Mosaic);
            UpdateToolButtonState(_buttonTextMode, mode == AnnotationToolMode.Text);
            UpdateToolButtonState(_buttonPenMode, mode == AnnotationToolMode.Pen);
            UpdateToolButtonState(_buttonTemporaryPenMode, mode == AnnotationToolMode.TemporaryPen);
            UpdateToolButtonState(_buttonStrikethroughMode, mode == AnnotationToolMode.Strikethrough);
            UpdateToolButtonState(_buttonSpotlightMode, mode == AnnotationToolMode.Spotlight);
            UpdateToolButtonState(_buttonMagnifierMode, mode == AnnotationToolMode.Magnifier);
            UpdatePenTypeButtonState();

            if (_highlightManager != null)
            {
                _highlightManager.IsHighlightMode = mode == AnnotationToolMode.Highlight;
            }
            _isHighlightMode = mode == AnnotationToolMode.Highlight;

            // 进入临时笔模式时，同步颜色选择高亮显示为临时笔颜色
            if (mode == AnnotationToolMode.TemporaryPen && _navigationManager != null)
            {
                UpdateAnnotationColorSelection(_navigationManager.TemporaryPenColor);
            }

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

            if (result == DialogResult.OK && _currentPageImage != null && !string.IsNullOrEmpty(_currentPdfPath))
            {
                try
                {
                    var imgRect = ToRectangle(GetImageDisplayRect());
                    float relX = (float)(location.X - imgRect.X) / imgRect.Width;
                    float relY = (float)(location.Y - imgRect.Y) / imgRect.Height;

                    relX = Math.Clamp(relX, 0, 1);
                    relY = Math.Clamp(relY, 0, 1);

                    _presenter?.AddAnnotationText(
                        relX, relY, dialog.InitialText,
                        dialog.SelectedColor.ToArgb(),
                        dialog.SelectedFontSize,
                        "Microsoft YaHei UI",
                        _currentPageImage.Width, _currentPageImage.Height,
                        _audioManager?.GetCurrentTimestampMs(),
                        _audioManager?.GetCurrentRecordingId());

                    _navigationManager?.LoadAnnotationsForCurrentPage();
                    RefreshHighlightList();
                    _audioManager?.RefreshTimestampList();
                    _pictureBoxPdf.Invalidate();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding text annotation");
                }
            }
        }

        private void ShowMagnifier(Point location)
        {
            if (_currentPageImage == null) return;

            var imgRect = ToRectangle(GetImageDisplayRect());
            float relX = (float)(location.X - imgRect.X) / imgRect.Width;
            float relY = (float)(location.Y - imgRect.Y) / imgRect.Height;

            relX = Math.Clamp(relX, 0, 1);
            relY = Math.Clamp(relY, 0, 1);

            int imgX = (int)(relX * _currentPageImage.Width);
            int imgY = (int)(relY * _currentPageImage.Height);

            var magnifier = new MagnifierForm(_currentPageImage, new Point(imgX, imgY));
            magnifier.Location = new Point(
                Math.Max(0, Math.Min(location.X - magnifier.Width / 2, Screen.PrimaryScreen.WorkingArea.Width - magnifier.Width)),
                Math.Max(0, Math.Min(location.Y + 30, Screen.PrimaryScreen.WorkingArea.Height - magnifier.Height)));
            magnifier.Show(this);
        }

        #endregion

        #region 翻页、视图模式与页面绘制

        private void ButtonDualPage_Click(object? sender, EventArgs e)
        {
            _isDualPage = !_isDualPage;
            if (_buttonDualPage != null)
            {
                _buttonDualPage.BackColor = _isDualPage ? Color.FromArgb(230, 244, 255) : Color.White;
                _buttonDualPage.FlatAppearance.BorderColor = _isDualPage ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            UpdateStatusBar();
            // 双页模式下页码组需要额外容纳跨页按钮，重新排布工具栏
            LayoutAnnotationToolStrip();
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
            // 统一走 Presenter 翻页入口，与方向键/滚轮行为一致（双页模式同样按 1 页步进）
            _presenter?.PreviousPage();
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            _presenter?.NextPage();
        }

        private void ButtonPrevSpread_Click(object? sender, EventArgs e)
        {
            StepSpread(-2);
        }

        private void ButtonNextSpread_Click(object? sender, EventArgs e)
        {
            StepSpread(2);
        }

        /// <summary>双页模式按跨页步进（两页），单页按钮仍保持一页步进</summary>
        private void StepSpread(int delta)
        {
            if (!_isDualPage || _presenter == null) return;
            int target = _currentPageIndex + delta;
            if (target < 0 || target >= _presenter.PageCount) return;
            _ = _presenter.RenderPage(target);
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
            _pictureBoxPdf.Invalidate();
        }

        private void UpdateToolbarButtonColorsForNightMode()
        {
            bool isNightMode = _nightModeManager?.IsNightMode ?? false;

            foreach (var button in GetAllToolbarButtons())
            {
                if (button != null)
                {
                    if (button.FlatAppearance.BorderSize >= 2) continue;
                    button.BackColor = isNightMode ? ToolbarLeaveNight : ToolbarLeaveDay;
                }
            }

            // 折叠按钮文字颜色
            if (_buttonCollapseNav != null)
            {
                _buttonCollapseNav.ForeColor = isNightMode ? Color.FromArgb(200, 200, 200) : Color.FromArgb(80, 80, 80);
            }

            // 高亮颜色单选按钮夜间模式
            var radioButtons = new[] { _radioHighlightYellow, _radioHighlightGreen, _radioHighlightBlue, _radioHighlightPink, _radioHighlightOrange };
            foreach (var radio in radioButtons)
            {
                if (radio != null)
                {
                    radio.FlatAppearance.BorderColor = isNightMode ? Color.FromArgb(80, 80, 80) : Color.FromArgb(200, 200, 200);
                }
            }

            // 自绘页签需要显式重绘，否则主题切换后仍保留旧底色
            _tabControlLeft?.Invalidate();
        }

        private void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            var menu = new ContextMenuStrip();

            menu.Items.Add("打开 AI 面板", null, (_, _) => RaiseAiQuestionAsked());
            menu.Items.Add("总结当前页", null, async (_, _) => await SummarizeCurrentPageAsync());
            menu.Items.Add("总结选中内容", null, async (_, _) => await SummarizeSelectionAsync());
            menu.Items.Add("解释选中内容", null, async (_, _) => await ExplainSelectionAsync());
            menu.Items.Add("全文检索问答", null, async (_, _) => await AskDocumentByRetrievalAsync());
            menu.Items.Add("生成文档大纲…", null, async (_, _) => await GenerateDocumentOutlineAsync());
            menu.Items.Add("生成闪卡…", null, async (_, _) => await GenerateFlashcardsAsync());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("导出 AI 学习笔记…", null, async (_, _) => await ExportAiStudyNotesAsync());
            menu.Items.Add("整理本页标注", null, async (_, _) => await SummarizeAnnotationsAsync(wholeDocument: false));
            menu.Items.Add("整理全书标注", null, async (_, _) => await SummarizeAnnotationsAsync(wholeDocument: true));
            menu.Items.Add("导出标注复习清单…", null, async (_, _) => await ExportAnnotationReviewListAsync());

            if (sender is Control anchor)
            {
                menu.Show(anchor, new Point(0, anchor.Height));
            }
            else
            {
                menu.Show(Cursor.Position);
            }
        }

        private void ButtonTranslationToggle_Click(object? sender, EventArgs e)
        {
            _isTranslationEnabled = !_isTranslationEnabled;
            if (_buttonTranslationToggle != null)
            {
                _buttonTranslationToggle.BackColor = _isTranslationEnabled ? Color.FromArgb(230, 244, 255) : Color.Transparent;
                _buttonTranslationToggle.FlatAppearance.BorderColor = _isTranslationEnabled ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            TranslateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OcrCloseButton_Click(object? sender, EventArgs e)
        {
            HideOcrOverlay();
        }

        private void PictureBoxPdf_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                ClearBackground(e.Graphics, e.ClipRectangle);
                DrawPageImage(e.Graphics);
                DrawSelectionRect(e.Graphics);

                if (!string.IsNullOrEmpty(_currentPdfPath) && _currentPageImage != null)
                {
                    if (_navigationManager?.AnnotationLayerVisible ?? true)
                    {
                        DrawHighlightsFromLayer(e.Graphics);
                    }
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


        private void ClearBackground(Graphics g, Rectangle clip)
        {
            bool isNight = _nightModeManager?.IsNightMode ?? _isNightMode;
            // 只填充本次需要重绘的区域，使局部 Invalidate 生效（原先 g.Clear 会清空整个控件，
            // 导致拖动绘制时无法做脏区重绘）
            if (clip.Width <= 0 || clip.Height <= 0) return;
            using var brush = new SolidBrush(isNight ? Color.FromArgb(20, 20, 20) : Color.White);
            g.FillRectangle(brush, clip);
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
                var rect = ToRectangle(GetImageDisplayRect());
                // 夜间反色只在显示层做一次，渲染缓存与导出/OCR 始终使用原色位图
                if (IsNightModeActive)
                {
                    DrawInvertedImage(g, _currentPageImage, rect);
                }
                else
                {
                    g.DrawImage(_currentPageImage, rect);
                }
            }
        }

        /// <summary>
        /// 当前是否处于夜间模式（以夜间模式管理器为准，兼容快捷键切换）。
        /// </summary>
        private bool IsNightModeActive => _nightModeManager?.IsNightMode ?? _isNightMode;

        private static void DrawInvertedImage(Graphics g, Image image, Rectangle rect)
        {
            using var ia = new System.Drawing.Imaging.ImageAttributes();
            var cm = new System.Drawing.Imaging.ColorMatrix(new float[][]
            {
                new float[] { -1, 0, 0, 0, 0 },
                new float[] { 0, -1, 0, 0, 0 },
                new float[] { 0, 0, -1, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 1, 1, 1, 0, 1 }
            });
            ia.SetColorMatrix(cm);
            g.DrawImage(image, rect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, ia);
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
                // 聚光灯覆盖层绘制在双页之上
                var totalRect = Rectangle.Union(leftRect, rightRect);
                _navigationManager.DrawSpotlightOverlay(g, totalRect);
            }
            else
            {
                var singleRect = ToRectangle(GetImageDisplayRect());
                _navigationManager.DrawAnnotations(g, singleRect, _currentPageIndex);
                // 聚光灯覆盖层绘制在单页之上
                _navigationManager.DrawSpotlightOverlay(g, singleRect);
            }
        }

        private void DrawDualPageLayout(Graphics g)
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                var (leftRect, rightRect) = GetDualPageRects();
                bool isNight = IsNightModeActive;

                // 夜间反色只在显示层做一次，双页两页都需反色
                if (isNight)
                {
                    DrawInvertedImage(g, _currentPageImage, leftRect);

                    if (_secondPageImage != null)
                    {
                        DrawInvertedImage(g, _secondPageImage, rightRect);
                    }
                }
                else
                {
                    g.DrawImage(_currentPageImage, leftRect);

                    if (_secondPageImage != null)
                    {
                        g.DrawImage(_secondPageImage, rightRect);
                    }
                }

                if (_secondPageImage == null)
                {
                    using var brush = new SolidBrush(isNight ? Color.FromArgb(20, 20, 20) : Color.White);
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

        #endregion

        #region 右键菜单、搜索、打印与画笔/缩放

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
            using var searchDialog = new PdfSearchDialog();
            searchDialog.FindButton.Click += ButtonFind_Click;
            // 指定 owner，避免进程级模态禁用其它已打开的阅读器窗口
            searchDialog.ShowDialog(this);
        }

        private void PrintCurrentPdf()
        {
            // 打印走 PdfiumViewer 原始渲染，不包含当前界面的旋转角度与高亮/标注，先行提示避免误打印
            if (_rotationAngle != 0 || HasHighlights())
            {
                var warnings = new List<string>();
                if (_rotationAngle != 0)
                    warnings.Add($"当前页面已旋转 {_rotationAngle}°");
                if (HasHighlights())
                    warnings.Add("存在高亮/标注");

                var result = MessageBox.Show(
                    this,
                    $"打印输出的将是 PDF 原始内容，不会包含：{string.Join("、", warnings)}。\n\n是否仍要继续打印？",
                    "打印提示", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result != DialogResult.Yes)
                    return;
            }

            _presenter?.PrintPdf();
        }

        private bool HasHighlights()
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || _highlightService == null)
                return false;

            try
            {
                var list = _highlightService.GetHighlights(_currentPdfPath);
                return list != null && list.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        private async void ButtonFind_Click(object? sender, EventArgs e)
        {
            if ((sender as Button)?.FindForm() is not PdfSearchDialog dialog) return;
            if (string.IsNullOrWhiteSpace(dialog.SearchText)) return;

            await (_presenter?.SearchTextAsync(dialog.SearchText, dialog.CaseSensitive, dialog.WholeWord) ?? Task.CompletedTask);
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
            // 笔型是画笔的上下文选项，切换笔型不再改变当前工具模式
            _navigationManager?.SetPenType("Pencil");
            UpdatePenTypeButtonState();
        }

        private void ButtonPenTypePen_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Pen");
            UpdatePenTypeButtonState();
        }

        private void ButtonPenTypeMarker_Click(object? sender, EventArgs e)
        {
            _navigationManager?.SetPenType("Marker");
            UpdatePenTypeButtonState();
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
            colorDialog.Color = _navigationManager?.CurrentToolMode == AnnotationToolMode.TemporaryPen
                ? (_navigationManager?.TemporaryPenColor ?? Color.Red)
                : (_navigationManager?.PenColor ?? Color.Black);
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
                if (saveDialog.ShowDialog(this) == DialogResult.OK)
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
            // 统一钳位，避免滑块与滚轮使用不同区间导致数值回跳
            int clampedLevel = Math.Max(MinZoomPercent, Math.Min(MaxZoomPercent, level));
            _navigationManager?.Zoom(clampedLevel);
            _trackBarZoom.Value = clampedLevel;
            _labelZoom.Text = $"{clampedLevel}%";
            UpdateStatusBar();
        }

        private void FitToWidth()
        {
            if (_currentPageImage != null)
            {
                float scale = (float)_pictureBoxPdf.ClientSize.Width / _currentPageImage.Width;
                int zoomLevel = (int)(scale * 100);
                SetZoom(Math.Max(MinZoomPercent, Math.Min(MaxZoomPercent, zoomLevel)));
            }
        }

        private void FitToHeight()
        {
            if (_currentPageImage != null)
            {
                float scale = (float)_pictureBoxPdf.ClientSize.Height / _currentPageImage.Height;
                int zoomLevel = (int)(scale * 100);
                SetZoom(Math.Max(MinZoomPercent, Math.Min(MaxZoomPercent, zoomLevel)));
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
                SetZoom(Math.Max(MinZoomPercent, Math.Min(MaxZoomPercent, zoomLevel)));
            }
        }

        #endregion

        #region 文件树与文件操作

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

        #endregion

        #region 翻译、朗读与学习入口

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

        private async void ButtonAddToLearningContent_Click(object? sender, EventArgs e)
        {
            if (_appConfig?.AppSettings.EnableUnifiedStudyEntry == false)
            {
                MessageBox.Show(this, "统一学习入口未启用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_studyEntryService == null)
            {
                MessageBox.Show(this, "学习入口服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var text = ResolveStudyText();
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show(this, "请先选中或识别一段文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var request = new Services.Learning.StudyEntryRequest
                {
                    Text = text,
                    Translation = _textBoxTranslation?.Text,
                    SourceType = "PdfHighlight",
                    PdfPath = _currentPdfPath,
                    PageIndex = _currentPageIndex
                };

                var result = await _studyEntryService.AddAsync(request);
                MessageBox.Show(
                    this,
                    result.AlreadyExists ? "已在学习库中" : "已加入学习",
                    "提示",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "添加到学习库失败");
                MessageBox.Show(this, $"加入学习失败：{ex.Message}", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>按“选区 → 原文框全文”的优先级解析待加入学习的文本。</summary>
        private string ResolveStudyText()
        {
            var selected = GetSelectedText();
            if (!string.IsNullOrWhiteSpace(selected)) return selected;

            return _textBoxOriginal?.Text?.Trim() ?? string.Empty;
        }

        #endregion

        #region 侧栏构建（阅读 / 翻译 / 标注 / 音频 / 学习）

        #region 侧栏统一视觉重构

        /// <summary>把翻译 / 高亮 / 听读三个侧栏面板统一到同一套卡片式设计语言。</summary>
        private void ApplySidebarRedesign()
        {
            // 阶段2：音频不再是顶层页签，三个子视图统一收进右侧抽屉
            if (_tabPageAudio != null && _tabControlLeft.TabPages.Contains(_tabPageAudio))
                _tabControlLeft.TabPages.Remove(_tabPageAudio);
            // 翻译面板改为由学习页入口在右侧抽屉弹出，不再占用顶层页签
            if (_tabPageTranslate != null && _tabControlLeft.TabPages.Contains(_tabPageTranslate))
                _tabControlLeft.TabPages.Remove(_tabPageTranslate);

            BuildReadingSidebar();
            BuildTranslateSidebar();
            BuildHighlightSidebar();
            BuildAudioDrawer();
            BuildStudySidebar();

            // 夜间状态按窗口记录，避免新建窗口覆盖其他阅读窗口的主题
            PdfSidebarTheme.SetWindowDark(this, _nightModeManager?.IsNightMode ?? false);
            PdfSidebarTheme.ApplyRecursive(_tabControlLeft);
        }

        /// <summary>抽屉与页签共用同一套主题令牌；夜间模式切换时由管理器统一调用。</summary>
        public void ApplySidebarTheme()
        {
            if (_audioDrawer != null)
                PdfSidebarTheme.ApplyRecursive(_audioDrawer);
        }

        /// <summary>
        /// 「学习」Tab（P3）：汇总本页高亮/已加入学习项/可跟读句子，并提供一键动作。
        /// </summary>
        private void BuildStudySidebar()
        {
            if (_tabPageStudy == null) return;

            _tabPageStudy.Controls.Clear();

            _studyPanel = new PdfStudyPanel();
            _studyPanel.Dock = DockStyle.Fill;
            _studyPanel.Margin = Padding.Empty;
            _studyPanel.Header.ActionButton!.Click += (_, _) => UpdateStudyTab();
            _tabPageStudy.Controls.Add(_studyPanel);

            _studyHighlightCount = _studyPanel.HighlightCountLabel;
            _studyLearningCount = _studyPanel.LearningCountLabel;
            _studySentenceCount = _studyPanel.SentenceCountLabel;

            _buttonStudyAddHighlights = _studyPanel.AddHighlightsButton;
            _buttonStudyAddHighlights.Click += ButtonStudyAddHighlights_Click;
            _buttonStudyFlashcards = _studyPanel.FlashcardsButton;
            _buttonStudyFlashcards.Click += ButtonStudyFlashcards_Click;
            _buttonStudyShadowing = _studyPanel.ShadowingButton;
            _buttonStudyShadowing.Click += ButtonStudyShadowing_Click;

            // 音频与跟读收口到学习页：配套音频/整页录音进入右侧抽屉
            _studyPanel.AudioOpenButton.Click += (_, _) => ShowAudioCompanion();
            _studyPanel.RecordingButton.Click += (_, _) => ShowAudioDrawer(AudioDrawerMode.Recording);
            // 翻译面板不再占顶层页签，改为从学习页弹出同一个右侧抽屉
            _studyPanel.TranslateButton.Click += (_, _) => ShowAudioDrawer(AudioDrawerMode.Translate);

            // 「AI 与文件工具」分组入口，复用现有 AI 菜单能力，避免功能藏在右键菜单里
            _studyPanel.SummaryButton.Click += async (_, _) => await SummarizeCurrentPageAsync();
            _studyPanel.AskButton.Click += async (_, _) => await AskDocumentByRetrievalAsync();
            _studyPanel.OutlineButton.Click += async (_, _) => await GenerateDocumentOutlineAsync();
            _studyPanel.NotesButton.Click += async (_, _) => await ExportAiStudyNotesAsync();
            _studyPanel.BatchOcrButton.Click += (_, _) => BatchOcrExport_Click();

            _studyItemList = _studyPanel.ItemList;
            _studyItemList.ClientSizeChanged += (_, _) => ResizeStudyItemRows();
        }

        /// <summary>
        /// 刷新「学习」Tab 的计数与按钮可用状态。数据量很小，直接同步读取个人学习库。
        /// </summary>
        private void UpdateStudyTab()
        {
            if (_tabPageStudy == null || _studyHighlightCount == null) return;

            int highlightCount = 0;
            if (!string.IsNullOrWhiteSpace(_currentPdfPath) && _highlightService != null && _currentPageIndex >= 0)
            {
                try
                {
                    highlightCount = _highlightService
                        .GetHighlightsForPage(_currentPdfPath, _currentPageIndex)
                        .Count(h => !string.IsNullOrWhiteSpace(h.Text));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "统计本页高亮失败");
                }
            }

            int learningCount = 0;
            IReadOnlyList<LearningItem> pageItems = Array.Empty<LearningItem>();
            try
            {
                var studyEntryService = Program.GetService<IStudyEntryService>();
                if (studyEntryService != null && !string.IsNullOrWhiteSpace(_currentPdfPath) && _currentPageIndex >= 0)
                {
                    pageItems = studyEntryService.GetByPdfPage(_currentPdfPath, _currentPageIndex);
                    learningCount = pageItems.Count;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "统计本页已加入学习项失败");
            }

            int sentenceCount = CountReadableSentences(_textBoxOriginal?.Text);

            _studyHighlightCount.Text = $"🖍 本页高亮：{highlightCount} 条";
            _studyLearningCount.Text = $"📚 已加入学习：{learningCount} 项";
            _studySentenceCount.Text = $"🎙 可跟读句子：{sentenceCount} 句";

            if (_buttonStudyAddHighlights != null)
                _buttonStudyAddHighlights.Enabled = highlightCount > 0;
            if (_buttonStudyShadowing != null)
                _buttonStudyShadowing.Enabled = sentenceCount > 0;

            RefreshStudyItemList(pageItems);
        }

        /// <summary>重建「本页学习项」列表：内容 + 状态 + 最近练习（P3-2 / P4-3）。</summary>
        private void RefreshStudyItemList(IReadOnlyList<LearningItem> items)
        {
            if (_studyItemList == null) return;

            _studyItemList.SuspendLayout();
            try
            {
                _studyItemList.Controls.Clear();

                if (items.Count == 0)
                {
                    var empty = new Label();
                    empty.AutoSize = false;
                    empty.Width = Math.Max(120, _studyItemList.ClientSize.Width - 6);
                    empty.Height = Math.Max(24, Font.Height + 12);
                    empty.Font = new Font("Microsoft YaHei UI", 9F);
                    empty.ForeColor = PdfSidebarTheme.Muted;
                    empty.Margin = new Padding(2, 4, 0, 0);
                    empty.Tag = "muted";
                    empty.Text = "本页暂无学习项，可先加入高亮或生成闪卡";
                    _studyItemList.Controls.Add(empty);
                    return;
                }

                IReadOnlyDictionary<string, PracticeRecord> latest =
                    new Dictionary<string, PracticeRecord>();
                try
                {
                    var practice = Program.GetService<IPracticeRecordService>();
                    var userId = _userSessionService?.CurrentUserId;
                    if (practice != null && !string.IsNullOrWhiteSpace(userId))
                        latest = practice.GetLatestByItemIds(userId, items.Select(i => i.Id));
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "读取本页学习项练习记录失败");
                }

                foreach (var item in items)
                    _studyItemList.Controls.Add(CreateStudyItemRow(item, latest));

                ResizeStudyItemRows();
            }
            finally
            {
                _studyItemList.ResumeLayout();
            }
        }

        /// <summary>让「本页学习项」的行跟随卡片宽度拉伸，避免固定宽度在窄侧栏下溢出。</summary>
        private void ResizeStudyItemRows()
        {
            if (_studyItemList == null || _studyItemList.IsDisposed) return;
            int width = Math.Max(120, _studyItemList.ClientSize.Width - 6);
            foreach (Control child in _studyItemList.Controls)
            {
                if (child.Width != width) child.Width = width;
            }
        }

        private Control CreateStudyItemRow(
            LearningItem item, IReadOnlyDictionary<string, PracticeRecord> latest)
        {
            int rowWidth = Math.Max(120, (_studyItemList?.ClientSize.Width ?? 272) - 6);
            var contentFont = new Font("Microsoft YaHei UI", 9.5F);
            var metaFont = new Font("Microsoft YaHei UI", 8.5F);
            int contentHeight = Math.Max(26, contentFont.Height + 10);
            int metaHeight = Math.Max(18, metaFont.Height + 6);

            var row = new Panel();
            row.Size = new Size(rowWidth, contentHeight + metaHeight + 4);
            row.BackColor = PdfSidebarTheme.Surface;
            row.Margin = new Padding(2, 0, 2, 4);
            row.Cursor = Cursors.Hand;

            var content = new Label();
            content.AutoSize = false;
            content.Location = new Point(2, 2);
            content.Size = new Size(Math.Max(40, rowWidth - 4), contentHeight);
            content.Font = contentFont;
            content.ForeColor = PdfSidebarTheme.Text;
            content.TextAlign = ContentAlignment.MiddleLeft;
            content.AutoEllipsis = true;
            content.Text = item.MainContent;

            var meta = new Label();
            meta.AutoSize = false;
            meta.Location = new Point(2, content.Bottom);
            meta.Size = new Size(Math.Max(40, rowWidth - 4), metaHeight);
            meta.Font = metaFont;
            meta.ForeColor = PdfSidebarTheme.Muted;
            meta.TextAlign = ContentAlignment.MiddleLeft;
            meta.AutoEllipsis = true;
            meta.Tag = "muted";
            meta.Text = BuildStudyItemMeta(item, latest);

            void Jump(object? sender, EventArgs e) => _ = JumpToStudyItemAsync(item);
            row.DoubleClick += Jump;
            content.DoubleClick += Jump;
            meta.DoubleClick += Jump;

            // 行宽变化时同步子标签宽度，保持文字左对齐且不溢出
            void LayoutRow()
            {
                int innerWidth = Math.Max(40, row.Width - 4);
                if (content.Width != innerWidth)
                {
                    content.Width = innerWidth;
                    meta.Width = innerWidth;
                }
                meta.Top = content.Bottom;
                int desiredHeight = content.Height + meta.Height + 4;
                if (row.Height != desiredHeight) row.Height = desiredHeight;
            }

            row.Resize += (_, _) => LayoutRow();
            row.Paint += (_, pe) =>
            {
                var palette = PdfSidebarTheme.PaletteFor(row);
                using var pen = new Pen(palette.Border);
                pe.Graphics.DrawLine(pen, 2, row.Height - 1, row.Width - 2, row.Height - 1);
            };

            row.Controls.Add(content);
            row.Controls.Add(meta);
            return row;
        }

        private static string BuildStudyItemMeta(
            LearningItem item, IReadOnlyDictionary<string, PracticeRecord> latest)
        {
            var status = GetLearningStatusText(item.Status);

            if (latest.TryGetValue(item.Id, out var record))
                return $"{status} · 最近练习 {record.CreatedAt:MM-dd HH:mm} · {record.Score:0}分";

            if (item.LastReviewedAt.HasValue)
                return $"{status} · 最近复习 {item.LastReviewedAt.Value:MM-dd HH:mm}";

            return $"{status} · 尚无练习";
        }

        private static string GetLearningStatusText(LearningStatus status) => status switch
        {
            LearningStatus.New => "新",
            LearningStatus.Learning => "学习中",
            LearningStatus.Known => "已认识",
            LearningStatus.Mastered => "已掌握",
            _ => "—"
        };

        /// <summary>双击学习项回跳到它的来源页（P4-3 回流入口）。</summary>
        private async Task JumpToStudyItemAsync(LearningItem item)
        {
            if (_presenter == null || _presenter.PageCount <= 0) return;

            int pageIndex = item.SourceRef is { PageIndex: >= 0 } anchor
                ? anchor.PageIndex
                : _currentPageIndex;
            pageIndex = Math.Clamp(pageIndex, 0, _presenter.PageCount - 1);

            try
            {
                await _presenter.RenderPage(pageIndex);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "学习项回跳失败");
            }
        }

        private static int CountReadableSentences(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            return text
                .Split(new[] { '.', '!', '?', '。', '！', '？', '\n', '\r', ';', '；' },
                    StringSplitOptions.RemoveEmptyEntries)
                .Count(part => part.Trim().Length >= 2);
        }

        private async void ButtonStudyAddHighlights_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentPdfPath) || _currentPageIndex < 0)
            {
                ShowToast("请先打开 PDF 并定位到需要转换的页面");
                return;
            }

            var studyEntryService = Program.GetService<IStudyEntryService>();
            if (studyEntryService == null)
            {
                ShowToast("学习服务不可用");
                return;
            }

            try
            {
                var highlights = _highlightService?
                    .GetHighlightsForPage(_currentPdfPath, _currentPageIndex)
                    .Where(h => !string.IsNullOrWhiteSpace(h.Text))
                    .ToList() ?? new List<LearningAssistant.Models.Pdf.PdfHighlight>();

                if (highlights.Count == 0)
                {
                    ShowToast("本页暂无高亮");
                    return;
                }

                if (_buttonStudyAddHighlights != null) _buttonStudyAddHighlights.Enabled = false;

                var requests = highlights.Select(h => new StudyEntryRequest
                {
                    Text = h.Text,
                    Translation = string.IsNullOrWhiteSpace(h.TranslationText) ? null : h.TranslationText,
                    SourceType = "PdfHighlight",
                    Importance = LearningItemClassification.ResolveImportanceFromHighlightColor(h.Color),
                    PdfPath = _currentPdfPath,
                    PageIndex = _currentPageIndex,
                    HighlightId = h.Id
                }).ToList();

                var results = await studyEntryService.AddRangeAsync(requests);
                var created = results.Count(r => r.Created);
                var existed = results.Count(r => r.AlreadyExists);

                ShowToast(existed > 0
                    ? $"已加入 {created} 条高亮（{existed} 条已存在）"
                    : $"已将 {created} 条高亮加入学习");

                UpdateStudyTab();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "本页高亮批量加入学习失败");
                ShowToast($"加入学习失败: {ex.Message}");
                if (_buttonStudyAddHighlights != null) _buttonStudyAddHighlights.Enabled = true;
            }
        }

        private async void ButtonStudyFlashcards_Click(object? sender, EventArgs e)
        {
            if (_currentPageIndex < 0)
            {
                ShowToast("请先打开 PDF");
                return;
            }

            await GenerateFlashcardsAsync(new PdfAiFlashcardOptions
            {
                StartPageIndex = _currentPageIndex,
                EndPageIndex = _currentPageIndex
            });
            UpdateStudyTab();
        }

        private void ButtonStudyShadowing_Click(object? sender, EventArgs e)
        {
            if (CountReadableSentences(_textBoxOriginal?.Text) == 0)
            {
                ShowToast("请先在翻译面板识别原文，再开始跟读");
                return;
            }

            OpenShadowingWindow();
        }

        /// <summary>翻译面板「句子跟读」：用当前页原文直接打开分句跟读窗口。</summary>
        private void ButtonTranslateShadowing_Click()
        {
            if (CountReadableSentences(_textBoxOriginal?.Text) == 0)
            {
                ShowToast("请先识别原文，再开始跟读");
                return;
            }

            OpenShadowingWindow();
        }

        private static void StyleSidebarButton(Button? button, string text, int width, int height = 30)
        {
            if (button == null) return;
            button.Text = text;
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = PdfSidebarTheme.Border;
            button.BackColor = PdfSidebarTheme.Surface;
            button.ForeColor = PdfSidebarTheme.Text;
            button.Font = new Font("Microsoft YaHei UI", 9F);
            button.Cursor = Cursors.Hand;
            button.Tag = "secondary";
        }

        /// <summary>
        /// 「目录」页：把原「文件」与「缩略图」两个页签合并为一个页，
        /// 由页头双图标切换视图，形成 目录 / 标注 / 学习 三组。
        /// </summary>
        private void BuildReadingSidebar()
        {
            if (_tabPageFiles == null) return;

            if (_readingSidebar == null) return;

            _readingSidebar.SelectedIndexChanged += (_, _) =>
            {
                if (_readingSidebar.SelectedIndex == 1 && !_thumbnailsLoaded && _presenter != null)
                {
                    _thumbnailsLoaded = true;
                    _presenter.GenerateThumbnails();
                }
            };

            // 页签图标；短名称与图标一起由自绘逻辑绘制
            _tabPageFiles.Text = "📖";
            _tabPageBookmarksAndHighlights.Text = "📑";
            _tabPageStudy.Text = "🤖";

            foreach (TabPage page in _tabControlLeft.TabPages)
                page.AccessibleName = GetSidebarTabTitle(page);
        }

        private void BuildTranslateSidebar()
        {
            _translatePanel = new PdfTranslatePanel
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            _translateHeader = _translatePanel.Header;
            _translatePanel.Header.ActionButton!.Click += (_, _) => ButtonTranslate_Click(null, EventArgs.Empty);
            _translatePanel.OcrButton.Click += (_, _) => OcrPageClicked?.Invoke(this, EventArgs.Empty);
            _translatePanel.ShadowingButton.Click += (_, _) => ButtonTranslateShadowing_Click();
            _translatePanel.OriginalCard.ActionButton!.Click += (_, _) => ButtonSpeakOriginal_Click(null, EventArgs.Empty);
            _translatePanel.TranslationCard.ActionButton!.Click += (_, _) => ButtonSpeakTranslation_Click(null, EventArgs.Empty);
            _translatePanel.GlossaryButton.Click += (_, _) => ShowTranslationGlossaryDialog();
            _translatePanel.ReTranslateButton.Click += (_, _) => ReTranslateClicked?.Invoke(this, EventArgs.Empty);

            _textBoxOriginal = _translatePanel.OriginalBox;
            _textBoxTranslation = _translatePanel.TranslationBox;

            _buttonAddToLearningContent = _translatePanel.AddToLearningButton;
            _buttonAddToLearningContent.Click += ButtonAddToLearningContent_Click;

            _checkBoxAutoTranslate = _translatePanel.AutoTranslateCheck;
            _checkBoxAutoSpeak = _translatePanel.AutoSpeakCheck;
            _checkBoxAiPolish = _translatePanel.AiPolishCheck;
            _speedSelector = _translatePanel.TtsSpeedSelector;

            SubscribeTranslationScrollSync();
            _textBoxOriginal.TextChanged += (_, _) => UpdateTranslateHeaderStatus();
            _textBoxTranslation.TextChanged += (_, _) => UpdateTranslateHeaderStatus();
            UpdateTranslateHeaderStatus();
        }

        private void UpdateTranslateHeaderStatus()
        {
            if (_translateHeader == null) return;
            int originalLength = _textBoxOriginal?.Text?.Length ?? 0;
            int translationLength = _textBoxTranslation?.Text?.Length ?? 0;
            string original = originalLength == 0 ? "未识别" : $"{originalLength} 字";
            string translation = translationLength == 0 ? "未翻译" : $"{translationLength} 字";
            _translateHeader.SetSubtitle($"第 {_currentPageIndex + 1} 页 · 原文 {original} · 译文 {translation}");
        }

        /// <summary>打开翻译术语表对话框：读取（必要时由 AI 构建）本文件术语表，保存后立即用于后续润色。</summary>
        private async void ShowTranslationGlossaryDialog()
        {
            var pdfPath = _currentPdfPath;
            if (string.IsNullOrEmpty(pdfPath) || !File.Exists(pdfPath))
            {
                ShowToast("请先打开 PDF 文档");
                return;
            }

            var service = Program.ServiceProvider?.GetService(typeof(IPdfTranslationGlossaryService))
                as IPdfTranslationGlossaryService;
            if (service == null)
            {
                ShowToast("术语表服务不可用");
                return;
            }

            try
            {
                var glossary = await service.GetOrBuildAsync(pdfPath);
                using var dialog = new PdfTranslationGlossaryDialog(
                    Path.GetFileName(pdfPath), glossary, _textBoxOriginal?.Text);
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                if (await service.SaveAsync(pdfPath, dialog.Entries))
                    ShowToast("术语表已保存");
                else
                    ShowToast("术语表保存失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开翻译术语表失败");
                ShowToast("读取术语表失败：" + ex.Message);
            }
        }

        private void BuildHighlightSidebar()
        {
            if (_tabPageBookmarksAndHighlights == null) return;

            _highlightPanel = new PdfHighlightPanel();
            _highlightPanel.Dock = DockStyle.Fill;
            _highlightHeader = _highlightPanel.Header;
            _highlightPanel.Header.ActionButton!.Click += (_, _) => ButtonExportHighlights_Click(null, EventArgs.Empty);

            _radioHighlightYellow = _highlightPanel.YellowButton;
            _radioHighlightGreen = _highlightPanel.GreenButton;
            _radioHighlightBlue = _highlightPanel.BlueButton;
            _radioHighlightPink = _highlightPanel.PinkButton;
            _radioHighlightOrange = _highlightPanel.OrangeButton;
            foreach (var radio in new[] { _highlightPanel.YellowButton, _highlightPanel.GreenButton, _highlightPanel.BlueButton, _highlightPanel.PinkButton, _highlightPanel.OrangeButton })
            {
                radio.CheckedChanged += RadioHighlightColor_CheckedChanged;
                radio.MouseEnter += RadioHighlightColor_MouseEnter;
                radio.MouseLeave += RadioHighlightColor_MouseLeave;
            }

            _listBoxHighlights = _highlightPanel.HighlightList;
            _listBoxHighlights.DrawItem += ListBoxHighlights_DrawItem;
            _listBoxHighlights.SelectedIndexChanged += ListBoxHighlights_SelectedIndexChanged;
            _listBoxHighlights.DoubleClick += ListBoxHighlights_DoubleClick;

            _highlightEmptyHost = SidebarUi.CreateEmptyState("📑", "还没有摘录\n选中文字后用高亮工具即可保存");
            _highlightEmptyHost.Visible = false;
            _highlightPanel.ListHost.Controls.Add(_highlightEmptyHost);

            _buttonRemoveHighlight = _highlightPanel.RemoveButton;
            _buttonEditHighlight = _highlightPanel.EditButton;
            _buttonBatchRemoveHighlight = _highlightPanel.BatchRemoveButton;
            StyleSidebarButton(_buttonRemoveHighlight, "删除", 72, 28);
            StyleSidebarButton(_buttonEditHighlight, "编辑", 72, 28);
            StyleSidebarButton(_buttonBatchRemoveHighlight, "批量删除", 92, 28);
            _buttonRemoveHighlight.Click += ButtonRemoveHighlight_Click;
            _buttonEditHighlight.Click += ButtonEditHighlight_Click;
            _buttonBatchRemoveHighlight.Click += ButtonBatchRemoveHighlight_Click;

            _tabPageBookmarksAndHighlights.Controls.Add(_highlightPanel);
            _highlightPanel.BringToFront();

            UpdateHighlightEmptyState();
        }

        private void UpdateHighlightEmptyState()
        {
            int count = _listBoxHighlights?.Items.Count ?? 0;
            _highlightHeader?.SetSubtitle(count == 0 ? "全书暂无摘录" : $"全书共 {count} 条 · 双击跳转");
            if (_highlightEmptyHost != null)
            {
                _highlightEmptyHost.Visible = count == 0;
                if (count == 0) _highlightEmptyHost.BringToFront();
            }
        }

        private void ListBoxHighlights_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (_listBoxHighlights == null || e.Index < 0 || e.Index >= _listBoxHighlights.Items.Count)
                return;

            object? item = _listBoxHighlights.Items[e.Index];
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            var bounds = e.Bounds;
            var palette = PdfSidebarTheme.PaletteFor(_listBoxHighlights);

            using (var background = new SolidBrush(selected ? palette.BrandSoft : palette.Surface))
                e.Graphics.FillRectangle(background, bounds);

            if (selected)
            {
                using var border = new Pen(PdfSidebarTheme.Brand);
                e.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }

            Color color = Color.FromArgb(253, 224, 71);
            string title;
            string meta;

            switch (item)
            {
                case PdfHighlight highlight:
                    color = MapHighlightColor(highlight.Color);
                    title = string.IsNullOrWhiteSpace(highlight.Text) ? highlight.Note : highlight.Text;
                    meta = $"第 {highlight.PageIndex + 1} 页 · {highlight.CreatedAt:MM-dd HH:mm}";
                    break;
                case PdfAnnotationItem annotation:
                    color = Color.FromArgb(annotation.ColorArgb);
                    title = string.IsNullOrWhiteSpace(annotation.Text) ? annotation.Type.ToString() : annotation.Text;
                    meta = $"第 {annotation.PageIndex + 1} 页 · {annotation.Type}";
                    break;
                default:
                    title = item?.ToString() ?? string.Empty;
                    meta = string.Empty;
                    break;
            }

            using (var strip = new SolidBrush(color))
                e.Graphics.FillRectangle(strip, bounds.X, bounds.Y + 6, 3, bounds.Height - 12);

            var titleRect = new Rectangle(bounds.X + 12, bounds.Y + 6, Math.Max(10, bounds.Width - 24), 30);
            TextRenderer.DrawText(e.Graphics, title, _listBoxHighlights.Font, titleRect,
                palette.Text,
                TextFormatFlags.WordBreak | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            var metaFont = new Font("Microsoft YaHei UI", 8F);
            var metaRect = new Rectangle(bounds.X + 12, bounds.Y + bounds.Height - 18, Math.Max(10, bounds.Width - 24), 16);
            TextRenderer.DrawText(e.Graphics, meta, metaFont, metaRect, palette.Muted,
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
            metaFont.Dispose();
        }

        private static Color MapHighlightColor(HighlightColor color)
        {
            return color switch
            {
                HighlightColor.Green => Color.FromArgb(74, 222, 128),
                HighlightColor.Blue => Color.FromArgb(96, 165, 250),
                HighlightColor.Pink => Color.FromArgb(244, 114, 182),
                HighlightColor.Orange => Color.FromArgb(251, 146, 60),
                _ => Color.FromArgb(253, 224, 71)
            };
        }

        /// <summary>
        /// 构建右侧侧栏抽屉：翻译 / 配套音频 / 整页录音共用一个内容区，
        /// 不再占用顶层页签，也不再使用「音频」内部的二级页签。
        /// </summary>
        private void BuildAudioDrawer()
        {
            if (_audioDrawer != null) return;

            _audioDrawer = new Panel
            {
                Visible = false,
                BackColor = PdfSidebarTheme.Background,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            _audioDrawer.Paint += (_, e) =>
            {
                var palette = PdfSidebarTheme.PaletteFor(_audioDrawer);
                using var pen = new Pen(palette.Border, 1);
                e.Graphics.DrawLine(pen, 0, 0, 0, _audioDrawer.Height);
                // 左侧渐隐，模拟抽屉浮起
                using var brush = new SolidBrush(Color.FromArgb(18, palette.Border));
                for (int i = 1; i <= 6; i++)
                    e.Graphics.FillRectangle(brush, i, 0, 1, _audioDrawer.Height);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = PdfSidebarTheme.Background,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            layout.Controls.Add(BuildAudioDrawerHeader(), 0, 0);
            layout.Controls.Add(BuildAudioDrawerContent(), 0, 1);
            _audioDrawer.Controls.Add(layout);

            Controls.Add(_audioDrawer);
            _tabControlLeft.SizeChanged += (_, _) => PositionAudioDrawer();
            UpdateAudioDrawerHeader();
            UpdateAudioDrawerModeStyles();
            PdfSidebarTheme.ApplyRecursive(_audioDrawer);
        }

        private Control BuildAudioDrawerHeader()
        {
            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = PdfSidebarTheme.Surface,
                Tag = "surface",
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
            header.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            var titleRow = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            _audioDrawerTitle = new Label
            {
                Text = "配套音频",
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 8)
            };
            _audioDrawerSubtitle = new Label
            {
                Text = string.Empty,
                Font = new Font("Microsoft YaHei UI", 8F),
                ForeColor = PdfSidebarTheme.Muted,
                Tag = "muted",
                AutoSize = true,
                Location = new Point(14, 29)
            };
            var closeButton = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI Symbol", 11F),
                Size = new Size(30, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = true,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Tag = "keep",
                AccessibleName = "关闭侧栏面板"
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.Click += (_, _) => HideAudioDrawer();
            _audioDrawerCloseButton = closeButton;
            titleRow.Resize += (_, _) =>
                closeButton.Location = new Point(
                    Math.Max(0, titleRow.Width - closeButton.Width - 10), 10);

            titleRow.Controls.Add(_audioDrawerTitle);
            titleRow.Controls.Add(_audioDrawerSubtitle);
            titleRow.Controls.Add(closeButton);

            var modeBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(10, 4, 10, 4)
            };
            for (int i = 0; i < 3; i++)
                modeBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            _audioDrawerTranslateButton = CreateAudioDrawerModeButton("翻译");
            _audioDrawerAudioButton = CreateAudioDrawerModeButton("配套音频");
            _audioDrawerRecordingButton = CreateAudioDrawerModeButton("整页录音");
            _audioDrawerTranslateButton.Click += (_, _) => ShowAudioDrawer(AudioDrawerMode.Translate);
            _audioDrawerAudioButton.Click += (_, _) => ShowAudioDrawer(AudioDrawerMode.Audio);
            _audioDrawerRecordingButton.Click += (_, _) => ShowAudioDrawer(AudioDrawerMode.Recording);
            _audioDrawerTranslateButton.Margin = new Padding(0, 0, 4, 0);
            _audioDrawerAudioButton.Margin = new Padding(2, 0, 2, 0);
            _audioDrawerRecordingButton.Margin = new Padding(4, 0, 0, 0);

            modeBar.Controls.Add(_audioDrawerTranslateButton, 0, 0);
            modeBar.Controls.Add(_audioDrawerAudioButton, 1, 0);
            modeBar.Controls.Add(_audioDrawerRecordingButton, 2, 0);

            header.Controls.Add(titleRow, 0, 0);
            header.Controls.Add(modeBar, 0, 1);
            return header;
        }

        private static Button CreateAudioDrawerModeButton(string text)
        {
            var button = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold),
                TabStop = true,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                Tag = "keep"
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private Control BuildAudioDrawerContent()
        {
            _audioDrawerContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = PdfSidebarTheme.Background,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            if (_audioPanel != null)
                _audioDrawerContent.Controls.Add(_audioPanel);
            else
            {
                _audioUnavailableLabel = CreateAudioDrawerEmptyLabel(
                    "当前版本未启用配套音频服务。");
                _audioDrawerContent.Controls.Add(_audioUnavailableLabel);
            }

            _audioDrawerContent.Controls.Add(_recordingHost!);
            if (!_recordingServiceAvailable)
            {
                _recordingUnavailableLabel = CreateAudioDrawerEmptyLabel(
                    "录音服务不可用，暂不能整页录音。");
                _recordingHost!.Controls.Add(_recordingUnavailableLabel);
            }

            if (_translatePanel != null)
            {
                _translatePanel.Visible = false;
                _audioDrawerContent.Controls.Add(_translatePanel);
            }

            return _audioDrawerContent;
        }

        private static Label CreateAudioDrawerEmptyLabel(string text)
            => new()
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = PdfSidebarTheme.Muted,
                Tag = "muted",
                Font = new Font("Microsoft YaHei UI", 9F),
                Padding = new Padding(24)
            };

        /// <summary>显示音频抽屉并切换到指定视图。</summary>
        private void ShowAudioDrawer(AudioDrawerMode mode)
        {
            if (_audioDrawer == null) BuildAudioDrawer();
            if (_audioDrawer == null) return;

            // 首次打开时记录触发控件，关闭后把焦点还回去，形成键盘闭环
            if (!_audioDrawer.Visible && ActiveControl != null && !_audioDrawer.Contains(ActiveControl))
                _audioDrawerReturnFocus = ActiveControl;

            SetAudioDrawerMode(mode);
            PositionAudioDrawer();
            _audioDrawer.Visible = true;
            _audioDrawer.BringToFront();
            _audioDrawerCloseButton?.Focus();
        }

        private void HideAudioDrawer()
        {
            if (_audioDrawer == null || !_audioDrawer.Visible) return;

            _audioDrawer.Visible = false;

            var target = _audioDrawerReturnFocus;
            _audioDrawerReturnFocus = null;
            if (target != null && !target.IsDisposed && target.Visible && target.CanFocus)
                target.Focus();
        }

        private void SetAudioDrawerMode(AudioDrawerMode mode)
        {
            _audioDrawerMode = mode;

            if (_audioPanel != null) _audioPanel.Visible = mode == AudioDrawerMode.Audio;
            if (_audioUnavailableLabel != null) _audioUnavailableLabel.Visible = mode == AudioDrawerMode.Audio;
            if (_recordingHost != null) _recordingHost.Visible = mode == AudioDrawerMode.Recording;
            if (_translatePanel != null) _translatePanel.Visible = mode == AudioDrawerMode.Translate;

            switch (mode)
            {
                case AudioDrawerMode.Audio:
                    _audioPanel?.RefreshData();
                    break;
                case AudioDrawerMode.Recording:
                    _audioManager?.RefreshTimestampList();
                    _audioManager?.RefreshSummaryList();
                    break;
                case AudioDrawerMode.Translate:
                    UpdateTranslateHeaderStatus();
                    break;
            }

            UpdateAudioDrawerHeader();
            UpdateAudioDrawerModeStyles();
        }

        private void UpdateAudioDrawerHeader()
        {
            if (_audioDrawerTitle == null) return;

            _audioDrawerTitle.Text = _audioDrawerMode switch
            {
                AudioDrawerMode.Recording => "整页录音",
                AudioDrawerMode.Translate => "翻译",
                _ => "配套音频"
            };

            string document = string.IsNullOrWhiteSpace(_currentPdfPath)
                ? "未打开文档"
                : Path.GetFileNameWithoutExtension(_currentPdfPath);
            _audioDrawerSubtitle!.Text = $"{document} · 第 {_currentPageIndex + 1} 页";
        }

        private void UpdateAudioDrawerModeStyles()
        {
            if (_audioDrawerAudioButton == null || _audioDrawerRecordingButton == null
                || _audioDrawerTranslateButton == null) return;

            var palette = PdfSidebarTheme.PaletteFor(this);
            StyleAudioDrawerModeButton(_audioDrawerTranslateButton, _audioDrawerMode == AudioDrawerMode.Translate, palette);
            StyleAudioDrawerModeButton(_audioDrawerAudioButton, _audioDrawerMode == AudioDrawerMode.Audio, palette);
            StyleAudioDrawerModeButton(_audioDrawerRecordingButton, _audioDrawerMode == AudioDrawerMode.Recording, palette);
        }

        private static void StyleAudioDrawerModeButton(Button button, bool selected, PdfSidebarPalette palette)
        {
            button.BackColor = selected ? palette.BrandSoft : palette.Surface;
            button.ForeColor = selected ? PdfSidebarTheme.Brand : palette.Muted;
            button.FlatAppearance.MouseOverBackColor = palette.Hover;
            button.FlatAppearance.MouseDownBackColor = palette.BrandSoft;
        }

        /// <summary>抽屉覆盖侧栏右侧、不遮挡 PDF 画布；跟随侧栏尺寸变化重新定位。</summary>
        private void PositionAudioDrawer()
        {
            if (_audioDrawer == null || _tabControlLeft == null || !IsHandleCreated) return;

            var sidebarScreen = _tabControlLeft.PointToScreen(Point.Empty);
            var formScreen = PointToScreen(Point.Empty);
            int width = Math.Min(420, Math.Max(320, _tabControlLeft.Width - 24));
            int x = sidebarScreen.X - formScreen.X + _tabControlLeft.Width - width;
            int y = sidebarScreen.Y - formScreen.Y;
            _audioDrawer.Bounds = new Rectangle(x, y, width, _tabControlLeft.Height);
        }

        private void OpenShadowingWindow()
        {
            if (string.IsNullOrEmpty(_currentPdfPath))
            {
                ShowWarning("请先打开 PDF 文档。");
                return;
            }

            string originalText = _textBoxOriginal.Text?.Trim() ?? string.Empty;
            if (originalText.Length == 0)
            {
                ShowWarning("请先识别或输入原文，再开始句子跟读。");
                return;
            }

            // 同一文档只保留一个跟读窗口，重复点击时激活已有窗口
            if (_shadowingForm != null && !_shadowingForm.IsDisposed)
            {
                if (_shadowingForm.WindowState == FormWindowState.Minimized)
                    _shadowingForm.WindowState = FormWindowState.Normal;
                _shadowingForm.Activate();
                return;
            }

            string documentTitle = Path.GetFileNameWithoutExtension(_currentPdfPath);
            _shadowingForm = new SentenceShadowingForm(
                _currentPdfPath, _currentPageIndex, originalText, documentTitle,
                _nightModeManager?.IsNightMode ?? false);
            _shadowingForm.FormClosed += (_, _) => _shadowingForm = null;
            _shadowingForm.Show(this);
        }

        #endregion

        private void ButtonOpenFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            // 指定 owner，避免进程级模态禁用其它已打开的阅读器窗口
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _presenter?.LoadFolder(dialog.SelectedPath);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                // 释放Bitmap资源
                SafeDisposeImage(ref _currentPageImage);
                SafeDisposeImage(ref _secondPageImage);
                SafeDisposeImage(ref _pendingDisplayImage);
                SafeDisposeImage(ref _annotationBitmap);
                SafeDisposeImage(ref _highlightBitmap);

                // 释放Graphics资源
                _annotationGraphics?.Dispose();

                // 释放Pen资源
                _pen?.Dispose();

                // 释放Manager资源
                _nightModeManager?.Dispose();
                _highlightManager?.Dispose();
                _navigationManager?.Dispose();
                _audioManager?.Dispose();
                _audioTrackManager?.Dispose();

                // 释放Timer资源
                _longPressTimer?.Stop();
                _longPressTimer?.Dispose();
                _pageTransitionTimer?.Stop();
                _pageTransitionTimer?.Dispose();
                _sidebarTipTimer?.Stop();
                _sidebarTipTimer?.Dispose();

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
        /// <returns>实际设置的图像</returns>
        private Bitmap? SafeReplaceImage(ref Bitmap? target, Bitmap? newImage)
        {
            if (target == newImage)
            {
                return newImage;
            }

            SafeDisposeImage(ref target);
            target = newImage;
            return newImage;
        }

        #endregion

        #region 缩放预设、语言、Toast 与拖放

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

        public string GetTranslationTargetLanguage()
        {
            return _translatePanel?.TargetLanguageCode ?? "auto";
        }

        public void ShowToast(string message, int duration = 2000)
        {
            if (_toastLabel == null || _toastLabel.IsDisposed) return;

            // 取消上一条提示的隐藏计时，避免连续操作时后一条提示被前一条提前关闭
            _toastCts?.Cancel();
            _toastCts?.Dispose();
            var cts = new CancellationTokenSource();
            _toastCts = cts;

            _toastLabel.Text = message;
            _toastLabel.Visible = true;

            // 以阅读画布中心定位，而不是整窗中心（侧栏会让提示视觉上偏左）
            Control? canvas = (_pictureBoxPdf != null && _pictureBoxPdf.IsHandleCreated && _pictureBoxPdf.Visible)
                ? _pictureBoxPdf
                : null;
            Point center = canvas != null
                ? PointToClient(canvas.PointToScreen(new Point(canvas.ClientSize.Width / 2, canvas.ClientSize.Height / 2)))
                : new Point(ClientSize.Width / 2, ClientSize.Height / 2);
            _toastLabel.Location = new Point(center.X - _toastLabel.Width / 2, center.Y - _toastLabel.Height / 2);
            _toastLabel.BringToFront();

            _ = HideToastAfterDelayAsync(cts.Token, duration);
        }

        private async Task HideToastAfterDelayAsync(CancellationToken token, int duration)
        {
            try
            {
                await Task.Delay(duration, token).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested || IsDisposed || !IsHandleCreated) return;

            try
            {
                BeginInvoke(() =>
                {
                    // 期间可能已有新提示复用同一标签，此时不能隐藏
                    if (token.IsCancellationRequested || _toastLabel == null || _toastLabel.IsDisposed) return;
                    _toastLabel.Visible = false;
                });
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
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

        #endregion

        #region 侧栏页签绘制与提示

        /// <summary>
        /// 左侧选项卡切换事件：切换到缩略图选项卡时按需加载缩略图
        /// </summary>
        private void TabControlLeft_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_tabControlLeft.SelectedTab == _tabPageStudy)
                UpdateStudyTab();
            else if (_tabControlLeft.SelectedTab == _tabPageBookmarksAndHighlights)
                UpdateHighlightEmptyState();

            if (_tabControlLeft.SelectedTab == null) return;

            _lastSidebarTabName = _tabControlLeft.SelectedTab.Name;
            if (string.IsNullOrEmpty(_currentPdfPath)) return;

            // 每本文档记住各自最后使用的页签，重新打开时不再统一回到「目录」
            if (_sidebarTabByDocument.TryGetValue(_currentPdfPath, out var saved) &&
                string.Equals(saved, _lastSidebarTabName, StringComparison.Ordinal))
                return;

            _sidebarTabByDocument[_currentPdfPath] = _lastSidebarTabName;
            SaveSidebarState();
        }

        /// <summary>恢复当前文档上次使用的侧栏页签；没有记录时回落到会话内页签或「目录」。</summary>
        private void RestoreSidebarTabSelection()
        {
            if (_tabControlLeft == null) return;

            string? target = null;
            if (!string.IsNullOrEmpty(_currentPdfPath))
                _sidebarTabByDocument.TryGetValue(_currentPdfPath, out target);
            target ??= _lastSidebarTabName;
            if (string.IsNullOrEmpty(target)) return;

            foreach (TabPage page in _tabControlLeft.TabPages)
            {
                if (!string.Equals(page.Name, target, StringComparison.Ordinal)) continue;
                _tabControlLeft.SelectedTab = page;
                return;
            }
        }

        private static string SidebarStatePath => Path.Combine(AppPaths.ConfigDir, SidebarStateFileName);

        private static void EnsureSidebarStateLoaded()
        {
            if (_sidebarStateLoaded) return;
            _sidebarStateLoaded = true;

            try
            {
                if (!File.Exists(SidebarStatePath)) return;

                var state = JsonSerializer.Deserialize<SidebarState>(File.ReadAllText(SidebarStatePath));
                if (state == null) return;

                _sidebarHoverSwitch = state.HoverSwitch;
                _sidebarTabByDocument = state.Tabs ?? new Dictionary<string, string>();
            }
            catch
            {
                // 状态文件损坏时退回默认值，不阻断阅读器启动
                _sidebarTabByDocument = new Dictionary<string, string>();
            }
        }

        private static void SaveSidebarState()
        {
            try
            {
                Directory.CreateDirectory(AppPaths.ConfigDir);
                var state = new SidebarState
                {
                    HoverSwitch = _sidebarHoverSwitch,
                    Tabs = _sidebarTabByDocument
                };
                File.WriteAllText(SidebarStatePath, JsonSerializer.Serialize(state));
            }
            catch
            {
                // 记录失败只影响下次恢复，不影响当前会话
            }
        }

        private sealed class SidebarState
        {
            public bool HoverSwitch { get; set; }
            public Dictionary<string, string>? Tabs { get; set; } = new();
        }

        /// <summary>右键页签条可开启「悬停 0.4 秒切换」，默认关闭，避免误触。</summary>
        private void BuildSidebarTabContextMenu()
        {
            _sidebarTabMenu = new ContextMenuStrip();
            var hoverItem = new ToolStripMenuItem("悬停 0.4 秒自动切换页签")
            {
                CheckOnClick = true,
                Checked = _sidebarHoverSwitch
            };
            hoverItem.CheckedChanged += (_, _) =>
            {
                _sidebarHoverSwitch = hoverItem.Checked;
                if (!_sidebarHoverSwitch)
                {
                    _sidebarHoverSwitchTimer?.Stop();
                    _sidebarHoverSwitchCandidate = -1;
                }
                SaveSidebarState();
            };
            _sidebarTabMenu.Items.Add(hoverItem);
            _tabControlLeft.ContextMenuStrip = _sidebarTabMenu;
        }

        private void SidebarHoverSwitchTimer_Tick(object? sender, EventArgs e)
        {
            _sidebarHoverSwitchTimer?.Stop();
            if (!_sidebarHoverSwitch || _tabControlLeft == null) return;

            int index = _sidebarHoverSwitchCandidate;
            if (index < 0 || index != _sidebarHoveredTabIndex) return;
            if (index >= _tabControlLeft.TabPages.Count) return;
            if (index == _tabControlLeft.SelectedIndex) return;

            _tabControlLeft.SelectedIndex = index;
        }

        /// <summary>Ctrl+K：把常用入口聚合成可搜索的命令面板。</summary>
        private void ShowCommandPalette()
        {
            var commands = new List<PdfCommandPaletteItem>
            {
                new("打开翻译面板", "在右侧弹出翻译与 OCR 面板", () => ShowAudioDrawer(AudioDrawerMode.Translate)),
                new("翻译本页", "识别并翻译当前页", () => { ShowAudioDrawer(AudioDrawerMode.Translate); OnTranslateClicked(); }),
                new("整页识别", "只识别当前页文字，不翻译", () => { ShowAudioDrawer(AudioDrawerMode.Translate); OnOcrPageClicked(); }),
                new("开始本页跟读", "打开句子跟读窗口", OpenShadowingWindow),
                new("配套音频", "在右侧抽屉查看配套音频", ShowAudioCompanion),
                new("整页录音", "在右侧抽屉为当前页录音", () => ShowAudioDrawer(AudioDrawerMode.Recording)),
                new("导出学习笔记", "导出当前文档的 AI 学习笔记", async () => await ExportAiStudyNotesAsync()),
                new("批量识别导出", "识别整本 PDF 并导出文本", BatchOcrExport_Click),
                new("切换夜间模式", "在浅色与夜间配色之间切换", () => _nightModeManager?.ToggleNightMode())
            };

            using var dialog = new PdfCommandPaletteDialog(commands);
            dialog.ShowDialog(this);
        }

        /// <summary>按侧栏实际宽度均分页签尺寸，确保任意 DPI 下都保持单行且不溢出。</summary>
        private void UpdateSidebarTabItemSize()
        {
            if (_tabControlLeft == null) return;
            int count = Math.Max(1, _tabControlLeft.TabPages.Count);
            int tabWidth = Math.Max(40, (_tabControlLeft.ClientSize.Width - 6) / count);
            // 高度需要同时容纳图标与名称，跟随字体高度避免高 DPI 下被裁切
            int tabHeight = Math.Max(38, (_sidebarTabFont?.Height ?? 16) + 18);
            var size = new Size(tabWidth, tabHeight);
            if (_tabControlLeft.ItemSize != size)
            {
                _tabControlLeft.ItemSize = size;
                // 页签高度变化后同步页内容区域，避免页内容上移被页签遮挡
                Rectangle display = _tabControlLeft.DisplayRectangle;
                foreach (TabPage page in _tabControlLeft.TabPages)
                    page.Bounds = display;
            }
        }

        /// <summary>页签的完整名称，用于悬停提示与无障碍说明。</summary>
        private static string GetSidebarTabTitle(TabPage? page)
        {
            if (page == null) return string.Empty;
            return page.Name switch
            {
                "_tabPageFiles" => "目录：文件、页面与缩略图",
                "_tabPageStudy" => "学习：AI 工具与学习整理",
                "_tabPageBookmarksAndHighlights" => "标注：高亮与标注列表",
                _ => page.Text
            };
        }

        /// <summary>页签上显示的短名称，与图标一起自绘。</summary>
        private static string GetSidebarTabLabel(TabPage? page)
        {
            if (page == null) return string.Empty;
            return page.Name switch
            {
                "_tabPageFiles" => "目录",
                "_tabPageStudy" => "学习",
                "_tabPageBookmarksAndHighlights" => "标注",
                _ => page.Text
            };
        }

        private void TabControlLeft_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (_tabControlLeft == null || e.Index < 0 || e.Index >= _tabControlLeft.TabPages.Count) return;

            bool isNightMode = _nightModeManager?.IsNightMode ?? false;
            bool isSelected = e.Index == _tabControlLeft.SelectedIndex;
            bool isHovered = e.Index == _sidebarHoveredTabIndex;

            Color baseColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
            Color selectedColor = isNightMode ? Color.FromArgb(51, 65, 85) : Color.FromArgb(238, 242, 255);
            Color hoverColor = isNightMode ? Color.FromArgb(45, 56, 70) : Color.FromArgb(248, 250, 252);
            Color borderColor = isNightMode ? Color.FromArgb(63, 63, 70) : Color.FromArgb(226, 232, 240);
            Color iconColor = isSelected
                ? (isNightMode ? Color.FromArgb(233, 213, 255) : Color.FromArgb(124, 58, 237))
                : (isNightMode ? Color.FromArgb(203, 213, 225) : Color.FromArgb(100, 116, 139));
            Color accentColor = isNightMode ? Color.FromArgb(167, 139, 250) : Color.FromArgb(139, 92, 246);

            using (var brush = new SolidBrush(isSelected ? selectedColor : isHovered ? hoverColor : baseColor))
                e.Graphics.FillRectangle(brush, e.Bounds);

            using (var borderPen = new Pen(borderColor, 1))
                e.Graphics.DrawLine(borderPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);

            if (isSelected)
            {
                using var accentBrush = new SolidBrush(accentColor);
                e.Graphics.FillRectangle(accentBrush, e.Bounds.Left + 8, e.Bounds.Bottom - 3, e.Bounds.Width - 16, 3);
            }

            string glyph = _tabControlLeft.TabPages[e.Index].Text;
            string label = GetSidebarTabLabel(_tabControlLeft.TabPages[e.Index]);
            _sidebarTabFont ??= new Font("Segoe UI Emoji", 12F);
            _sidebarTabLabelFont ??= new Font("Microsoft YaHei UI", 9.5F, FontStyle.Bold);

            Size glyphSize = TextRenderer.MeasureText(glyph, _sidebarTabFont, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            Size labelSize = TextRenderer.MeasureText(label, _sidebarTabLabelFont, Size.Empty,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
            const int gap = 6;
            int contentWidth = glyphSize.Width + gap + labelSize.Width;
            int startX = e.Bounds.Left + Math.Max(0, (e.Bounds.Width - contentWidth) / 2);
            var flags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                | TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;

            TextRenderer.DrawText(e.Graphics, glyph, _sidebarTabFont,
                new Rectangle(startX, e.Bounds.Top, glyphSize.Width, e.Bounds.Height), iconColor, flags);
            TextRenderer.DrawText(e.Graphics, label, _sidebarTabLabelFont,
                new Rectangle(startX + glyphSize.Width + gap, e.Bounds.Top, labelSize.Width + 1, e.Bounds.Height),
                iconColor, flags);

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(e.Bounds, -4, -4));
        }

        private void TabControlLeft_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_tabControlLeft == null) return;

            int hoveredIndex = -1;
            for (int i = 0; i < _tabControlLeft.TabPages.Count; i++)
            {
                if (_tabControlLeft.GetTabRect(i).Contains(e.Location))
                {
                    hoveredIndex = i;
                    break;
                }
            }

            if (hoveredIndex == _sidebarHoveredTabIndex) return;

            int previousIndex = _sidebarHoveredTabIndex;
            _sidebarHoveredTabIndex = hoveredIndex;
            // 只重绘变化的两个页签，避免整条页签反复重绘导致图标闪烁
            InvalidateSidebarTab(previousIndex);
            InvalidateSidebarTab(hoveredIndex);

            // 悬停提示延迟显示：鼠标快速划过多页签时只保留最后一次，避免提示框闪烁
            _sidebarTipTimer?.Stop();
            _toolTip?.Hide(_tabControlLeft);
            if (hoveredIndex >= 0)
                _sidebarTipTimer?.Start();

            // 可选增强：悬停稳定 0.4 秒后切换页签，默认关闭
            _sidebarHoverSwitchTimer?.Stop();
            if (_sidebarHoverSwitch && hoveredIndex >= 0 && hoveredIndex != _tabControlLeft.SelectedIndex)
            {
                _sidebarHoverSwitchCandidate = hoveredIndex;
                _sidebarHoverSwitchTimer?.Start();
            }
            else
            {
                _sidebarHoverSwitchCandidate = -1;
            }
        }

        private void TabControlLeft_MouseLeave(object? sender, EventArgs e)
        {
            if (_tabControlLeft == null) return;
            _sidebarTipTimer?.Stop();
            _sidebarHoverSwitchTimer?.Stop();
            _sidebarHoverSwitchCandidate = -1;
            if (_sidebarHoveredTabIndex < 0) return;
            int previousIndex = _sidebarHoveredTabIndex;
            _sidebarHoveredTabIndex = -1;
            InvalidateSidebarTab(previousIndex);
            _toolTip?.Hide(_tabControlLeft);
        }

        private void InvalidateSidebarTab(int index)
        {
            if (_tabControlLeft == null || index < 0 || index >= _tabControlLeft.TabPages.Count) return;
            Rectangle rect = _tabControlLeft.GetTabRect(index);
            if (rect.IsEmpty) return;
            rect.Inflate(1, 1);
            _tabControlLeft.Invalidate(rect);
        }

        private void ShowSidebarTabToolTip(int index)
        {
            if (_tabControlLeft == null || index < 0 || index >= _tabControlLeft.TabPages.Count) return;
            Rectangle rect = _tabControlLeft.GetTabRect(index);
            if (rect.IsEmpty) return;
            _toolTip?.Show(GetSidebarTabTitle(_tabControlLeft.TabPages[index]),
                _tabControlLeft, rect.Left, rect.Bottom + 2, 2500);
        }

        #endregion

        #region 空状态、拖放与滚动同步

        /// <summary>未打开 PDF 时的空状态引导：拖放提示 + 打开文件夹入口。</summary>
        private void InitializeEmptyState()
        {
            if (_panelPdf == null || _panelEmptyState != null) return;

            _panelEmptyState = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(248, 250, 252),
                Name = "_panelEmptyState"
            };

            _panelEmptyContent = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            var iconLabel = new Label
            {
                Text = "📄",
                Font = new Font("Segoe UI Emoji", 44F),
                AutoSize = true,
                ForeColor = Color.FromArgb(148, 163, 184),
                Margin = new Padding(0, 0, 0, 4)
            };

            var titleLabel = new Label
            {
                Text = "还没有打开 PDF",
                Font = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };

            var hintLabel = new Label
            {
                Text = "把 PDF 文件拖到这里，或从左侧文件夹中选择",
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = Color.FromArgb(100, 116, 139),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 18)
            };

            var openButton = new Button
            {
                Text = "打开文件夹",
                Size = new Size(132, 36),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(139, 92, 246),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 9.5F),
                Cursor = Cursors.Hand,
                Margin = new Padding(0)
            };
            openButton.FlatAppearance.BorderSize = 0;
            openButton.Click += (_, __) => ButtonOpenFolder_Click(this, EventArgs.Empty);
            openButton.MouseEnter += (_, __) => openButton.BackColor = Color.FromArgb(124, 58, 237);
            openButton.MouseLeave += (_, __) => openButton.BackColor = Color.FromArgb(139, 92, 246);

            _panelEmptyContent.Controls.Add(iconLabel);
            _panelEmptyContent.Controls.Add(titleLabel);
            _panelEmptyContent.Controls.Add(hintLabel);
            _panelEmptyContent.Controls.Add(openButton);

            _panelEmptyState.Controls.Add(_panelEmptyContent);
            _panelEmptyState.Resize += (_, __) => CenterEmptyState();

            _panelPdf.Controls.Add(_panelEmptyState);
            _panelEmptyState.BringToFront();
            CenterEmptyState();

            // 画布与空状态中的子控件都要接受拖放，否则拖到画布上无法触发打开
            EnablePdfFileDrop(_panelPdf);
            EnablePdfFileDrop(_pictureBoxPdf);
            EnablePdfFileDrop(_panelEmptyState);
            EnablePdfFileDrop(_panelEmptyContent);
            foreach (Control child in _panelEmptyContent.Controls)
                EnablePdfFileDrop(child);
        }

        private void CenterEmptyState()
        {
            if (_panelEmptyState == null || _panelEmptyContent == null) return;
            _panelEmptyContent.Location = new Point(
                Math.Max(0, (_panelEmptyState.ClientSize.Width - _panelEmptyContent.Width) / 2),
                Math.Max(0, (_panelEmptyState.ClientSize.Height - _panelEmptyContent.Height) / 2));
        }

        private void EnablePdfFileDrop(Control? control)
        {
            if (control == null) return;
            control.AllowDrop = true;
            control.DragEnter += PdfReaderFormV2_DragEnter;
            control.DragDrop += PdfReaderFormV2_DragDrop;
        }

        private void SubscribeTranslationScrollSync()
        {
            if (_textBoxOriginal is not SyncedTextBox originalBox) return;
            if (_textBoxTranslation is not SyncedTextBox translationBox) return;

            originalBox.VerticalScrolled += (_, _) => SyncTranslationScroll(_textBoxOriginal, _textBoxTranslation);
            translationBox.VerticalScrolled += (_, _) => SyncTranslationScroll(_textBoxTranslation, _textBoxOriginal);
        }

        /// <summary>把源文本框的纵向滚动位置同步到目标文本框，避免递归回环。</summary>
        private void SyncTranslationScroll(TextBox source, TextBox target)
        {
            if (_syncingTranslationScroll || source.IsDisposed || target.IsDisposed) return;
            if (!source.IsHandleCreated || !target.IsHandleCreated) return;

            // 段落级定位期间由面板接管滚动，跳过比例同步以免覆盖定位结果
            if (source is SyncedTextBox { SuppressScrollSync: true }) return;

            _syncingTranslationScroll = true;
            try
            {
                var position = default(POINT);
                SendMessage(source.Handle, EM_GETSCROLLPOS, IntPtr.Zero, ref position);
                SendMessage(target.Handle, EM_SETSCROLLPOS, IntPtr.Zero, ref position);
            }
            finally
            {
                _syncingTranslationScroll = false;
            }
        }

        #endregion

        #region Win32 互操作与内嵌控件

        private const int EM_GETSCROLLPOS = 0x0400 + 221;
        private const int EM_SETSCROLLPOS = 0x0400 + 222;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref POINT lParam);

        /// <summary>
        /// 开启双缓冲的 PictureBox，减少拖动绘制/缩放时的闪烁并降低重绘的感知延迟。
        /// </summary>
        private sealed class BufferedPictureBox : PictureBox
        {
            public BufferedPictureBox()
            {
                DoubleBuffered = true;
            }
        }

        #endregion

    }
}
