using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{

    public partial class PdfReaderForm : Form, IPdfView, IPdfReaderFormAccess
    {
        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderForm> _logger;
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
        private Button? _buttonExportHighlights;
        private TextBox? _textBoxFilter;
        private List<string> _allFiles = new List<string>();

        private TabPage? _tabPageBookmarksAndHighlights;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private bool _isImageMode = false;

        private Panel? _pageTransitionOverlay;
        private System.Windows.Forms.Timer? _pageTransitionTimer;

        private Bitmap? _currentPageImage;

        private Button? _buttonLockView;
        private Button? _buttonResetView;

        private int _zoomLevel = 100;
        private Point _imageOffset = Point.Empty;
        private Rectangle? _lastSelectionRect;

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;

        private bool _isLongPressPending = false;
        private Point _longPressStartLocation = Point.Empty;
        private bool _longPressDragStarted = false;

        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTime_ms = 200;
        private const int DoubleClickDistance = 5;
        private bool _isDoubleClickPending = false;

        private bool _isSelecting = false;
        private bool _isDrawing = false;
        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private List<PointF>? _currentStrokePoints;

        private bool _isHighlightMode = true;
        private bool _isLocked = false;
        private bool _isDragging = false;
        private Point _dragStart = Point.Empty;

        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;

        private System.Windows.Forms.Timer? _longPressTimer;

        private HighlightColor _currentHighlightColor = HighlightColor.Yellow;

        private bool _isNightMode = false;
        private Bitmap? _highlightBitmap;

        private SplitContainer splitContainer1;
        private Button? buttonSpeakOriginal;
        private GroupBox? groupBoxProgress;
        private GroupBox? groupBox1;
        private GroupBox? groupBox2;
        private string _currentLanguage = "eng";

        public PdfReaderForm(ILogger<PdfReaderForm> logger, IAIPanelPopupService? aiPanelPopupService = null, Services.Learning.IPendingContentService? pendingContentService = null, IHighlightService? highlightService = null, IBookmarkService? bookmarkService = null, IAnnotationService? annotationService = null, IEventBus? eventBus = null)
        {
            InitializeComponent();
            DoubleBuffered = true;
            SetDoubleBuffered(panelPdf);
            SetDoubleBuffered(pictureBoxPdf);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiPanelPopupService = aiPanelPopupService;
            _pendingContentService = pendingContentService;
            _highlightService = highlightService ?? new HighlightService();
            _bookmarkService = bookmarkService ?? new BookmarkService();
            _annotationService = annotationService;
            _eventBus = eventBus;
            Load += PdfReaderForm_Load;
            Resize += PdfReaderForm_Resize;
            KeyDown += PdfReaderForm_KeyDown;

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

            // 在 _navigationManager 初始化后绑定鼠标事件
            pictureBoxPdf.MouseDown += _navigationManager.MouseDown;
            pictureBoxPdf.MouseMove += _navigationManager.MouseMove;
            pictureBoxPdf.MouseUp += _navigationManager.MouseUp;
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
        public bool IsTranslationEnabled
        {
            get => _isTranslationEnabled;
            set => _isTranslationEnabled = value;
        }

        public PictureBox PictureBoxPdf => pictureBoxPdf;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PdfPresenter? Presenter
        {
            get => _presenter;
            set => _presenter = value;
        }
        public TextBox TextBoxOriginal => textBoxOriginal;
        public TextBox TextBoxPage => textBoxPage;
        public Label LabelZoom => labelZoom;
        public TrackBar TrackBarZoom => trackBarZoom;

        public Button? ButtonNightMode => buttonNightMode;
        public Button? ButtonLanguage => buttonLanguage;
        public Button? ButtonAskAi => buttonAskAi;

        public TabPage? TabPageTranslate => tabPageTranslate;
        public GroupBox? GroupBoxProgress => groupBoxProgress;
        public TextBox? TextBoxTranslation => textBoxTranslation;
        public Label? LabelOriginal => labelOriginal;
        public Label? LabelTranslation => labelTranslation;
        public Button? ButtonTranslate => buttonTranslate;
        public Button? ButtonSpeakOriginal => buttonSpeakOriginal;
        public Button? ButtonSpeakTranslation => buttonSpeakTranslation;

        public TabPage? TabPageBookmarksAndHighlights => _tabPageBookmarksAndHighlights;
        public GroupBox? GroupBoxBookmarks => _groupBoxBookmarks;
        public ListBox? ListBoxBookmarks => _listBoxBookmarks;
        public TextBox? TextBoxBookmarkTitle => _textBoxBookmarkTitle;
        public Button? ButtonAddBookmark => _buttonAddBookmark;
        public Button? ButtonRemoveBookmark => _buttonRemoveBookmark;

        public GroupBox? GroupBoxHighlights => _groupBoxHighlights;
        public ListBox? ListBoxHighlights => _listBoxHighlights;
        public GroupBox? GroupBoxHighlightColor => groupBoxHighlightColor;
        public Button? ButtonRemoveHighlight => _buttonRemoveHighlight;
        public Button? ButtonBatchRemoveHighlight => _buttonBatchRemoveHighlight;
        public Button? ButtonExportHighlights => _buttonExportHighlights;
        public Button? ButtonUndoHighlight => buttonUndoHighlight;

        public Panel? PanelPdf => panelPdf;
        public Panel? PanelNavigation => panelNavigation;
        public Panel? PanelLeftContainer => panelLeftContainer;
        public TreeView? TreeViewFiles => treeViewFiles;
        public TabControl? TabControlLeft => tabControlLeft;
        public Panel? PanelThumbnails => panelThumbnails;
        public FlowLayoutPanel? FlowLayoutPanelThumbnails => flowLayoutPanelThumbnails;

        public Panel? PageTransitionOverlay => _pageTransitionOverlay;
        public System.Windows.Forms.Timer? PageTransitionTimer => _pageTransitionTimer;
        public Button? ButtonLockView => _buttonLockView;

        public Pen Pen => _pen;

        #endregion


        private void PdfReaderForm_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();

            // 加载完成后通知 presenter 加载上次会话
            _presenter?.LoadLastSessionAndRestore();
        }

        private void PdfReaderForm_KeyDown(object? sender, KeyEventArgs e)
        {
            try
            {
                if ((ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)
                    {
                        _navigationManager?.Zoom(_navigationManager.ZoomLevel + 10);
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)
                    {
                        _navigationManager?.Zoom(_navigationManager.ZoomLevel - 10);
                        e.Handled = true;
                        return;
                    }
                    if (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)
                    {
                        _navigationManager?.ResetZoom();
                        e.Handled = true;
                        return;
                    }
                }

                switch (e.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Up:
                    case Keys.PageUp:
                        _presenter?.PreviousPage();
                        e.Handled = true;
                        break;
                    case Keys.Right:
                    case Keys.Down:
                    case Keys.PageDown:
                        _presenter?.NextPage();
                        e.Handled = true;
                        break;
                    case Keys.Home:
                        _presenter?.RenderPage(0);
                        e.Handled = true;
                        break;
                    case Keys.End:
                        if (_presenter != null && _presenter.PageCount > 0)
                        {
                            _presenter.RenderPage(_presenter.PageCount - 1);
                        }
                        e.Handled = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PdfReaderForm_KeyDown");
            }
        }

        private void PdfReaderForm_Resize(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
        }

        private void AdjustPanelPdfSize()
        {
            pictureBoxPdf.Invalidate();
        }

        private void InitializeBookmarkAndHighlightUI()
        {
            // 检查合并后的标签页是否已正确添加到 tabControlLeft
            bool needInitialize = false;

            if (_tabPageBookmarksAndHighlights == null)
            {
                needInitialize = true;
            }
            else if (tabControlLeft != null && !tabControlLeft.TabPages.Contains(_tabPageBookmarksAndHighlights))
            {
                // _tabPageBookmarksAndHighlights 存在但没有添加到 tabControlLeft，需要重新初始化
                needInitialize = true;
                // 先清理旧的
                try
                {
                    CleanupOldTabPages();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up old tab pages");
                }
            }

            if (!needInitialize)
            {
                return;
            }
        }


        private void CleanupOldTabPages()
        {
            try
            {
                // 移除旧的书签和高亮标签页
                if (tabControlLeft != null)
                {
                    if (_tabPageBookmarksAndHighlights != null && tabControlLeft.TabPages.Contains(_tabPageBookmarksAndHighlights))
                    {
                        tabControlLeft.TabPages.Remove(_tabPageBookmarksAndHighlights);
                    }
                }

                // 清理旧的控件引用
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
            if (sender is RadioButton radio && radio.Checked && radio.Tag is int colorIndex)
            {
                _highlightManager!.CurrentHighlightColor = (HighlightColor)colorIndex;
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
            if (_listBoxHighlights?.SelectedItem is PdfHighlight highlight)
            {
                if (highlight.PdfPath != _currentPdfPath)
                {
                    if (_presenter != null)
                    {
                        string extension = Path.GetExtension(highlight.PdfPath).ToLower();
                        if (extension == ".pdf")
                            _presenter.LoadPdf(highlight.PdfPath);
                        else
                        {
                            _presenter.LoadPdf(Path.GetFileName(highlight.PdfPath));
                        }
                    }
                }
                else
                {
                    _presenter?.RenderPage(highlight.PageIndex);
                }
            }
        }

        private void ButtonLockView_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ToggleLockView();
        }

        private void ButtonResetView_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ResetZoom();
        }

        private void TrackBarZoom_Scroll(object? sender, EventArgs e)
        {
            _navigationManager?.Zoom(trackBarZoom.Value);
        }

        private void ButtonRemoveHighlight_Click(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is PdfHighlight highlight)
            {
                _highlightManager?.RemoveHighlight(highlight);
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



        public void SetCurrentPdfPath(string pdfPath)
        {
            CleanupHighlightLayer();
            ClearThumbnails();
            _currentPdfPath = pdfPath;
            _bookmarkManager?.ClearCache();

            InitializeBookmarkAndHighlightUI();

            RefreshBookmarkList();
            RefreshHighlightList();

            LoadHighlightsForCurrentPage();
        }

        public void SetPresenter(PdfPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.SetView(this);
        }


        public void SetFileList(IEnumerable<string> files)
        {
            _allFiles = files.ToList();
            _textBoxFilter?.Clear();
            UpdateFileListDisplay();
        }

        private void UpdateFileListDisplay()
        {
            treeViewFiles.Nodes.Clear();
            string filter = _textBoxFilter?.Text?.Trim() ?? string.Empty;
            
            var filteredFiles = string.IsNullOrEmpty(filter)
                ? _allFiles
                : _allFiles.Where(f => Path.GetFileName(f).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
            
            foreach (var file in filteredFiles)
            {
                var node = treeViewFiles.Nodes.Add(Path.GetFileName(file));
                node.Tag = file;
            }
            
            if (!string.IsNullOrEmpty(_currentPdfPath))
            {
                var currentNode = treeViewFiles.Nodes.Cast<TreeNode>()
                    .FirstOrDefault(n => n.Tag is string tag && tag == _currentPdfPath);
                if (currentNode != null)
                {
                    treeViewFiles.SelectedNode = currentNode;
                    currentNode.EnsureVisible();
                }
            }
        }

        public void SetImageList(IEnumerable<string> imageFiles)
        {
            _allFiles = imageFiles.ToList();
            _textBoxFilter?.Clear();
            UpdateFileListDisplay();
        }

        public void SetPageCount(int count)
        {
            labelPageCount.Text = $"/ {count}";
        }

        public void SetCurrentPageIndex(int pageIndex)
        {
            bool isForward = pageIndex > _currentPageIndex;
            _currentPageIndex = pageIndex;
            textBoxPage.Text = (pageIndex + 1).ToString();
            StartPageTransition(isForward);
            LoadHighlightsForCurrentPage();
        }

        public void SetPageText(int pageIndex, string text)
        {
        }

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

                var old = _currentPageImage;
                _currentPageImage = imageToDisplay;

                pictureBoxPdf.Image = null;

                if (old != null && old != bmp && old != imageToDisplay)
                {
                    try
                    {
                        old.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to dispose old image");
                    }
                }

                pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisplayImage");
                _currentPageImage = bmp;
                pictureBoxPdf.Image = null;
                pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
            }
        }

        public void ShowWarning(string message)
        {
            MessageBox.Show(message, "警告", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }




        // 新增功能：中等级 - UI响应性改进，加载状态管理
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
            MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowMessage(string message, string title)
        {
            MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ShowLoading(string message)
        {
            SetLoadingState(true);
            // 如果需要显示加载提示，可以在这里添加
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

        private void ToggleLanguage()
        {
            if (_currentLanguage == "eng")
            {
                _currentLanguage = "chi_sim";
                if (buttonLanguage != null)
                {
                    buttonLanguage.Text = "中";
                }
            }
            else
            {
                _currentLanguage = "eng";
                if (buttonLanguage != null)
                {
                    buttonLanguage.Text = "EN";
                }
            }
        }

        public void SetCurrentLanguage(string language)
        {
            _currentLanguage = language;
            if (buttonLanguage != null)
            {
                buttonLanguage.Text = language == "eng" ? "EN" : "中";
            }
        }

        public void UpdateLanguageButtonText(string text)
        {
            if (buttonLanguage != null)
            {
                buttonLanguage.Text = text;
            }
        }

        public string GetCurrentLanguage()
        {
            return _currentLanguage;
        }

        // 新增功能：中等级 - PDF页面缩略图
        public void ClearThumbnails()
        {
            if (flowLayoutPanelThumbnails != null)
            {
                foreach (Control control in flowLayoutPanelThumbnails.Controls)
                {
                    control.Dispose();
                }
                flowLayoutPanelThumbnails.Controls.Clear();
            }
        }

        public void AddThumbnail(int pageIndex, Image thumbnail)
        {
            AddThumbnail(pageIndex, thumbnail, string.Empty);
        }

        public void AddThumbnail(int pageIndex, Image thumbnail, string directoryPath)
        {
            // V1 不支持目录分组，忽略 directoryPath 参数
            if (flowLayoutPanelThumbnails == null || thumbnail == null) return;

            var panel = new Panel();
            panel.Size = new Size(100, 140);
            panel.Margin = new Padding(5);
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Tag = pageIndex;

            _nightModeManager?.UpdateThumbnailPanelColor(panel);

            Image displayImage = thumbnail;
            if (_nightModeManager?.IsNightMode ?? false)
            {
                displayImage = _nightModeManager.InvertImage(thumbnail);
            }

            var pictureBox = new PictureBox();
            pictureBox.Image = displayImage;
            pictureBox.Size = new Size(90, 115);
            pictureBox.Location = new Point(5, 5);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Tag = pageIndex;
            pictureBox.Click += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };
            pictureBox.DoubleClick += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };

            var label = new Label();
            label.Text = (pageIndex + 1).ToString();
            label.Location = new Point(5, 120);
            label.Size = new Size(90, 15);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Microsoft YaHei UI", 8F);
            _nightModeManager?.UpdateThumbnailLabelColor(label);
            label.Tag = pageIndex;
            label.Click += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };
            label.DoubleClick += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(label);
            panel.Click += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };
            panel.DoubleClick += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    NavigateToPage(idx);
                }
            };

            flowLayoutPanelThumbnails.Controls.Add(panel);
        }

        public void HighlightThumbnail(int pageIndex)
        {
            if (flowLayoutPanelThumbnails == null) return;

            foreach (Control control in flowLayoutPanelThumbnails.Controls)
            {
                if (control is Panel panel)
                {
                    if (panel.Tag is int idx && idx == pageIndex)
                    {
                        panel.BackColor = Color.FromArgb(100, 150, 250);
                        panel.BorderStyle = BorderStyle.Fixed3D;
                        panel.BringToFront();
                    }
                    else
                    {
                        _nightModeManager?.UpdateThumbnailPanelColor(panel);
                        panel.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
            }
        }

        private void NavigateToPage(int pageIndex)
        {
            _presenter?.RenderPage(pageIndex);
        }

        public string GetSelectedFile()
        {
            return treeViewFiles.SelectedNode?.Tag as string ?? treeViewFiles.SelectedNode?.Text ?? string.Empty;
        }

        public string GetPageText()
        {
            return textBoxPage.Text;
        }



        public string GetTranslationText()
        {
            return textBoxTranslation.Text;
        }

        public string GetOriginalText()
        {
            return textBoxOriginal.Text;
        }

        public void SetTranslationText(string text)
        {
            textBoxTranslation.Text = text;
        }

        public void SetOriginalText(string text)
        {
            textBoxOriginal.Text = text;
        }

        public void SetOcrResultText(string text)
        {
            textBoxOriginal.Text = text;
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
            return pictureBoxPdf.ClientRectangle;
        }

        public Rectangle GetImageDisplayRect()
        {
            try
            {
                if (_currentPageImage == null)
                    return pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;

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
                    return pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
                }

                var controlWidth = pictureBoxPdf.ClientSize.Width;
                var controlHeight = pictureBoxPdf.ClientSize.Height;

                float imageAspect = (float)imgWidth / imgHeight;
                float controlAspect = (float)controlWidth / controlHeight;

                int displayWidth, displayHeight, displayX, displayY;

                if (imageAspect > controlAspect)
                {
                    // 图片更宽，水平填满，垂直居中
                    displayWidth = controlWidth;
                    displayHeight = (int)(controlWidth / imageAspect);
                }
                else
                {
                    // 图片更高，垂直填满，水平居中
                    displayHeight = controlHeight;
                    displayWidth = (int)(controlHeight * imageAspect);
                }

                // 使用 _navigationManager 的缩放状态
                float scale = _navigationManager != null ? _navigationManager.ZoomLevel / 100.0f : _zoomLevel / 100.0f;
                displayWidth = (int)(displayWidth * scale);
                displayHeight = (int)(displayHeight * scale);

                // 计算居中位置（考虑拖动偏移）
                var imageOffset = _navigationManager != null ? _navigationManager.ImageOffset : _imageOffset;
                displayX = (controlWidth - displayWidth) / 2 + imageOffset.X;
                displayY = (controlHeight - displayHeight) / 2 + imageOffset.Y;

                return new Rectangle(displayX, displayY, displayWidth, displayHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetImageDisplayRect");
                return pictureBoxPdf?.ClientRectangle ?? Rectangle.Empty;
            }
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
                    // 按图片实际尺寸调整面板大小，添加边框和关闭按钮空间
                    int panelWidth = image.Width + 8;
                    int panelHeight = image.Height + 32; // 26 for close button area

                    // 限制最小宽度至少能放下关闭按钮
                    if (panelWidth < 50)
                    {
                        panelWidth = 50;
                    }

                    // 限制最大尺寸，防止过大
                    int maxWidth = panelPdf.ClientSize.Width - 100;
                    int maxHeight = panelPdf.ClientSize.Height - 100;

                    if (panelWidth > maxWidth)
                    {
                        // 等比例缩放
                        double scale = (double)maxWidth / panelWidth;
                        panelWidth = maxWidth;
                        panelHeight = (int)(panelHeight * scale);
                    }

                    if (panelHeight > maxHeight)
                    {
                        // 等比例缩放
                        double scale = (double)maxHeight / panelHeight;
                        panelHeight = maxHeight;
                        panelWidth = (int)(panelWidth * scale);
                    }

                    _ocrPanel.Size = new Size(panelWidth, panelHeight);
                    // 重新定位关闭按钮和图片框
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

        public event EventHandler? FileSelected;
        public event EventHandler? PageChanged;
        public event EventHandler? OcrSelectionComplete;
        public event EventHandler? AiQuestionAsked;
        public event EventHandler? AddToLearningList;
        public event EventHandler<AddToEditorEventArgs>? AddToEditor;
        public void RaiseAddToEditor(string text, string language)
        {
            AddToEditor?.Invoke(this, new AddToEditorEventArgs { Text = text, Language = language });
        }
        public event EventHandler? SpeakTranslation;
        public event EventHandler<string>? SpeakText;
        public event EventHandler<string>? AskAiWithText;
        public event EventHandler? SelectOcrClicked;
        public event EventHandler? TranslateClicked;
        public event EventHandler? ToggleNightMode;
        public event EventHandler? ToggleTranslation;
        public event EventHandler? LanguageChanged;
        public event EventHandler? SpeakAnswer;
        public event EventHandler? SpeakOriginal;

        #region IPdfReaderFormAccess Implementation

        public Form Form => this;

        public void OnSelectOcrClicked() => SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        public void OnTranslateClicked() => TranslateClicked?.Invoke(this, EventArgs.Empty);

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TreeView treeViewFiles;
        private Panel panelPdf;
        private PictureBox pictureBoxPdf;
        private Panel panelThumbnails;
        private FlowLayoutPanel flowLayoutPanelThumbnails;
        private TabPage tabPageTranslate;
        private TextBox textBoxOriginal;
        private Label labelOriginal;
        private Label labelTranslation;
        private TextBox textBoxTranslation;
        private Button buttonTranslate;
        private Button buttonSpeakTranslation;
        private Button buttonAddToLearningContent;
        private Panel panelNavigation;
        private Button buttonPrev;
        private TextBox textBoxPage;
        private Label labelPageCount;
        private Button buttonNext;
        private Button buttonNightMode;
        private Button buttonTranslationToggle;
        private Button buttonLanguage;
        private Button buttonAskAi;
        private Button buttonOpenFolder;
        private Label labelZoom;
        private TrackBar trackBarZoom;
        private TabControl tabControlLeft;
        private TabPage tabPageFiles;
        private TabPage tabPageThumbnails;
        private Panel panelLeftContainer;



        private Panel bookmarkContainer;
        private FlowLayoutPanel buttonPanel;
        private Panel highlightContainer;
        private FlowLayoutPanel highlightButtonPanel;
        private Button buttonUndoHighlight;

        private GroupBox groupBoxHighlightColor;
        private RadioButton radioHighlightYellow;
        private RadioButton radioHighlightGreen;
        private RadioButton radioHighlightBlue;
        private RadioButton radioHighlightPink;
        private RadioButton radioHighlightOrange;


        private Label transitionLabel;


        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            splitContainer1 = new SplitContainer();
            panelPdf = new Panel();
            panelNavigation = new Panel();
            trackBarZoom = new TrackBar();
            labelZoom = new Label();
            _loadingIndicator = new LoadingIndicator();
            buttonLanguage = new Button();
            buttonAskAi = new Button();
            buttonNightMode = new Button();
            buttonTranslationToggle = new Button();
            _buttonLockView = new Button();
            _buttonResetView = new Button();
            buttonNext = new Button();
            labelPageCount = new Label();
            textBoxPage = new TextBox();
            buttonPrev = new Button();
            pictureBoxPdf = new PictureBox();
            _ocrPanel = new Panel();
            _ocrPictureBox = new PictureBox();
            _ocrCloseButton = new Button();
            _pageTransitionOverlay = new Panel();
            transitionLabel = new Label();
            panelLeftContainer = new Panel();
            tabControlLeft = new TabControl();
            tabPageThumbnails = new TabPage();
            panelThumbnails = new Panel();
            flowLayoutPanelThumbnails = new FlowLayoutPanel();
            tabPageTranslate = new TabPage();
            groupBoxProgress = new GroupBox();
            textBoxTranslation = new TextBox();
            buttonSpeakTranslation = new Button();
            buttonAddToLearningContent = new Button();
            textBoxOriginal = new TextBox();
            buttonSpeakOriginal = new Button();
            labelTranslation = new Label();
            labelOriginal = new Label();
            buttonTranslate = new Button();
            tabPageFiles = new TabPage();
            treeViewFiles = new TreeView();
            _tabPageBookmarksAndHighlights = new TabPage();
            _groupBoxHighlights = new GroupBox();
            groupBoxHighlightColor = new GroupBox();
            radioHighlightYellow = new RadioButton();
            radioHighlightGreen = new RadioButton();
            radioHighlightBlue = new RadioButton();
            radioHighlightPink = new RadioButton();
            radioHighlightOrange = new RadioButton();
            _listBoxHighlights = new ListBox();
            highlightButtonPanel = new FlowLayoutPanel();
            _buttonBatchRemoveHighlight = new Button();
            buttonUndoHighlight = new Button();
            _buttonRemoveHighlight = new Button();
            _buttonExportHighlights = new Button();
            _groupBoxBookmarks = new GroupBox();
            _listBoxBookmarks = new ListBox();
            _textBoxBookmarkTitle = new TextBox();
            buttonPanel = new FlowLayoutPanel();
            _buttonRemoveBookmark = new Button();
            _buttonAddBookmark = new Button();
            buttonOpenFolder = new Button();
            _pageTransitionTimer = new System.Windows.Forms.Timer(components);
            groupBox1 = new GroupBox();
            groupBox2 = new GroupBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panelPdf.SuspendLayout();
            panelNavigation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).BeginInit();
            _ocrPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)_ocrPictureBox).BeginInit();
            _pageTransitionOverlay.SuspendLayout();
            panelLeftContainer.SuspendLayout();
            tabControlLeft.SuspendLayout();
            tabPageThumbnails.SuspendLayout();
            panelThumbnails.SuspendLayout();
            tabPageTranslate.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            tabPageFiles.SuspendLayout();
            _tabPageBookmarksAndHighlights.SuspendLayout();
            _groupBoxHighlights.SuspendLayout();
            groupBoxHighlightColor.SuspendLayout();
            _groupBoxBookmarks.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panelPdf);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(panelLeftContainer);
            splitContainer1.Size = new Size(1380, 848);
            splitContainer1.SplitterDistance = 1036;
            splitContainer1.TabIndex = 5;
            // 
            // panelPdf
            // 
            panelPdf.Controls.Add(panelNavigation);
            panelPdf.Controls.Add(pictureBoxPdf);
            panelPdf.Controls.Add(_ocrPanel);
            panelPdf.Controls.Add(_pageTransitionOverlay);
            panelPdf.Dock = DockStyle.Fill;
            panelPdf.Location = new Point(0, 0);
            panelPdf.Name = "panelPdf";
            panelPdf.Size = new Size(1036, 848);
            panelPdf.TabIndex = 1;
            // 
            // panelNavigation
            // 
            panelNavigation.Controls.Add(trackBarZoom);
            panelNavigation.Controls.Add(labelZoom);
            panelNavigation.Controls.Add(_loadingIndicator);
            panelNavigation.Controls.Add(buttonLanguage);
            panelNavigation.Controls.Add(buttonAskAi);
            panelNavigation.Controls.Add(buttonNightMode);
            panelNavigation.Controls.Add(buttonTranslationToggle);
            panelNavigation.Controls.Add(_buttonLockView);
            panelNavigation.Controls.Add(_buttonResetView);
            panelNavigation.Controls.Add(buttonNext);
            panelNavigation.Controls.Add(labelPageCount);
            panelNavigation.Controls.Add(textBoxPage);
            panelNavigation.Controls.Add(buttonPrev);
            panelNavigation.Location = new Point(9, 12);
            panelNavigation.Name = "panelNavigation";
            panelNavigation.Size = new Size(1024, 59);
            panelNavigation.TabIndex = 3;
            panelNavigation.MouseDown += PanelNavigation_MouseDown;
            panelNavigation.MouseMove += PanelNavigation_MouseMove;
            panelNavigation.MouseUp += PanelNavigation_MouseUp;
            // 
            // trackBarZoom
            // 
            trackBarZoom.Location = new Point(149, 6);
            trackBarZoom.Maximum = 200;
            trackBarZoom.Minimum = 50;
            trackBarZoom.Name = "trackBarZoom";
            trackBarZoom.Size = new Size(150, 45);
            trackBarZoom.TabIndex = 8;
            trackBarZoom.Value = 100;
            trackBarZoom.Scroll += TrackBarZoom_Scroll;
            // 
            // labelZoom
            // 
            labelZoom.Location = new Point(309, 18);
            labelZoom.Name = "labelZoom";
            labelZoom.Size = new Size(45, 20);
            labelZoom.TabIndex = 7;
            labelZoom.Text = "100%";
            // 
            // _loadingIndicator
            // 
            _loadingIndicator.BackColor = Color.FromArgb(245, 245, 245);
            _loadingIndicator.Location = new Point(616, 11);
            _loadingIndicator.Name = "_loadingIndicator";
            _loadingIndicator.Size = new Size(38, 35);
            _loadingIndicator.TabIndex = 2;
            _loadingIndicator.Visible = false;
            _loadingIndicator.Click += _loadingIndicator_Click;
            // 
            // buttonLanguage
            // 
            buttonLanguage.BackColor = Color.White;
            buttonLanguage.FlatStyle = FlatStyle.Flat;
            buttonLanguage.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonLanguage.Location = new Point(364, 11);
            buttonLanguage.Name = "buttonLanguage";
            buttonLanguage.Size = new Size(50, 35);
            buttonLanguage.TabIndex = 9;
            buttonLanguage.Text = "eng";
            buttonLanguage.UseVisualStyleBackColor = false;
            buttonLanguage.Click += ButtonLanguage_Click;
            // 
            // buttonAskAi
            // 
            buttonAskAi.BackColor = Color.LightGray;
            buttonAskAi.FlatStyle = FlatStyle.Flat;
            buttonAskAi.Font = new Font("Microsoft YaHei UI", 9F);
            buttonAskAi.ForeColor = Color.Black;
            buttonAskAi.Location = new Point(572, 11);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(38, 35);
            buttonAskAi.TabIndex = 10;
            buttonAskAi.Text = "🤖 ";
            buttonAskAi.UseVisualStyleBackColor = false;
            buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // buttonNightMode
            // 
            buttonNightMode.BackColor = Color.White;
            buttonNightMode.FlatStyle = FlatStyle.Flat;
            buttonNightMode.Font = new Font("Microsoft YaHei UI", 12F);
            buttonNightMode.Location = new Point(415, 11);
            buttonNightMode.Name = "buttonNightMode";
            buttonNightMode.Size = new Size(38, 35);
            buttonNightMode.TabIndex = 6;
            buttonNightMode.Text = "🌙";
            buttonNightMode.UseVisualStyleBackColor = false;
            buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // buttonTranslationToggle
            // 
            buttonTranslationToggle.BackColor = Color.LightGray;
            buttonTranslationToggle.FlatStyle = FlatStyle.Flat;
            buttonTranslationToggle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            buttonTranslationToggle.Location = new Point(493, 11);
            buttonTranslationToggle.Name = "buttonTranslationToggle";
            buttonTranslationToggle.Size = new Size(38, 35);
            buttonTranslationToggle.TabIndex = 12;
            buttonTranslationToggle.Text = "译";
            buttonTranslationToggle.UseVisualStyleBackColor = false;
            buttonTranslationToggle.Click += ButtonTranslationToggle_Click;
            // 
            // _buttonLockView
            // 
            _buttonLockView.BackColor = Color.White;
            _buttonLockView.FlatStyle = FlatStyle.Flat;
            _buttonLockView.Location = new Point(454, 11);
            _buttonLockView.Name = "_buttonLockView";
            _buttonLockView.Size = new Size(38, 35);
            _buttonLockView.TabIndex = 10;
            _buttonLockView.Text = "🔓";
            _buttonLockView.UseVisualStyleBackColor = false;
            _buttonLockView.Click += ButtonLockView_Click;
            // 
            // _buttonResetView
            // 
            _buttonResetView.BackColor = Color.White;
            _buttonResetView.FlatStyle = FlatStyle.Flat;
            _buttonResetView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonResetView.Location = new Point(532, 11);
            _buttonResetView.Name = "_buttonResetView";
            _buttonResetView.Size = new Size(38, 35);
            _buttonResetView.TabIndex = 11;
            _buttonResetView.Text = "↺";
            _buttonResetView.UseVisualStyleBackColor = false;
            _buttonResetView.Click += ButtonResetView_Click;
            // 
            // buttonNext
            // 
            buttonNext.Location = new Point(108, 14);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(35, 28);
            buttonNext.TabIndex = 4;
            buttonNext.Text = "▶";
            buttonNext.Click += ButtonNext_Click;
            // 
            // labelPageCount
            // 
            labelPageCount.Location = new Point(73, 18);
            labelPageCount.Name = "labelPageCount";
            labelPageCount.Size = new Size(38, 20);
            labelPageCount.TabIndex = 3;
            labelPageCount.Text = "/ 1";
            // 
            // textBoxPage
            // 
            textBoxPage.Location = new Point(38, 16);
            textBoxPage.Name = "textBoxPage";
            textBoxPage.Size = new Size(30, 23);
            textBoxPage.TabIndex = 2;
            textBoxPage.Text = "1";
            textBoxPage.KeyDown += TextBoxPage_KeyDown;
            // 
            // buttonPrev
            // 
            buttonPrev.Location = new Point(3, 14);
            buttonPrev.Name = "buttonPrev";
            buttonPrev.Size = new Size(30, 28);
            buttonPrev.TabIndex = 1;
            buttonPrev.Text = "◀";
            buttonPrev.Click += ButtonPrev_Click;
            // 
            // pictureBoxPdf
            // 
            pictureBoxPdf.Dock = DockStyle.Fill;
            pictureBoxPdf.Location = new Point(0, 0);
            pictureBoxPdf.Name = "pictureBoxPdf";
            pictureBoxPdf.Size = new Size(1036, 848);
            pictureBoxPdf.TabIndex = 1;
            pictureBoxPdf.TabStop = false;
            pictureBoxPdf.Paint += PictureBoxPdf_Paint;
            // 鼠标事件在 InitializeManagers() 之后绑定，因为 _navigationManager 需要先初始化
            pictureBoxPdf.MouseWheel += PictureBoxPdf_MouseWheel;
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
            _ocrPictureBox.Location = new Point(2, 24);
            _ocrPictureBox.Name = "_ocrPictureBox";
            _ocrPictureBox.Size = new Size(192, 170);
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
            _pageTransitionOverlay.Controls.Add(transitionLabel);
            _pageTransitionOverlay.Dock = DockStyle.Fill;
            _pageTransitionOverlay.Location = new Point(0, 0);
            _pageTransitionOverlay.Name = "_pageTransitionOverlay";
            _pageTransitionOverlay.Size = new Size(1036, 848);
            _pageTransitionOverlay.TabIndex = 0;
            _pageTransitionOverlay.Visible = false;
            // 
            // transitionLabel
            // 
            transitionLabel.Dock = DockStyle.Fill;
            transitionLabel.Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold);
            transitionLabel.ForeColor = Color.FromArgb(200, 100, 100, 100);
            transitionLabel.Location = new Point(0, 0);
            transitionLabel.Name = "transitionLabel";
            transitionLabel.Size = new Size(1036, 848);
            transitionLabel.TabIndex = 0;
            transitionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelLeftContainer
            // 
            panelLeftContainer.Controls.Add(tabControlLeft);
            panelLeftContainer.Controls.Add(buttonOpenFolder);
            panelLeftContainer.Dock = DockStyle.Fill;
            panelLeftContainer.Location = new Point(0, 0);
            panelLeftContainer.Name = "panelLeftContainer";
            panelLeftContainer.Size = new Size(340, 848);
            panelLeftContainer.TabIndex = 0;
            // 
            // tabControlLeft
            // 
            tabControlLeft.Controls.Add(tabPageThumbnails);
            tabControlLeft.Controls.Add(tabPageTranslate);
            tabControlLeft.Controls.Add(tabPageFiles);
            tabControlLeft.Controls.Add(_tabPageBookmarksAndHighlights);
            tabControlLeft.Dock = DockStyle.Fill;
            tabControlLeft.Location = new Point(0, 35);
            tabControlLeft.Name = "tabControlLeft";
            tabControlLeft.SelectedIndex = 0;
            tabControlLeft.Size = new Size(340, 813);
            tabControlLeft.TabIndex = 1;
            // 
            // tabPageThumbnails
            // 
            tabPageThumbnails.Controls.Add(panelThumbnails);
            tabPageThumbnails.Location = new Point(4, 26);
            tabPageThumbnails.Name = "tabPageThumbnails";
            tabPageThumbnails.Padding = new Padding(3);
            tabPageThumbnails.Size = new Size(332, 783);
            tabPageThumbnails.TabIndex = 1;
            tabPageThumbnails.Text = "🖼️ 缩略图";
            tabPageThumbnails.UseVisualStyleBackColor = true;
            // 
            // panelThumbnails
            // 
            panelThumbnails.AutoScroll = true;
            panelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
            panelThumbnails.Controls.Add(flowLayoutPanelThumbnails);
            panelThumbnails.Dock = DockStyle.Fill;
            panelThumbnails.Location = new Point(3, 3);
            panelThumbnails.Name = "panelThumbnails";
            panelThumbnails.Size = new Size(326, 777);
            panelThumbnails.TabIndex = 0;
            // 
            // flowLayoutPanelThumbnails
            // 
            flowLayoutPanelThumbnails.AutoScroll = true;
            flowLayoutPanelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
            flowLayoutPanelThumbnails.Dock = DockStyle.Fill;
            flowLayoutPanelThumbnails.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelThumbnails.Location = new Point(0, 0);
            flowLayoutPanelThumbnails.Name = "flowLayoutPanelThumbnails";
            flowLayoutPanelThumbnails.Size = new Size(326, 777);
            flowLayoutPanelThumbnails.TabIndex = 0;
            flowLayoutPanelThumbnails.WrapContents = false;
            // 
            // tabPageTranslate
            // 
            tabPageTranslate.Controls.Add(groupBoxProgress);
            tabPageTranslate.Location = new Point(4, 26);
            tabPageTranslate.Name = "tabPageTranslate";
            tabPageTranslate.Padding = new Padding(3);
            tabPageTranslate.Size = new Size(332, 783);
            tabPageTranslate.TabIndex = 1;
            tabPageTranslate.Text = "📚翻译结果";
            // 
            // groupBoxProgress
            // 
            groupBoxProgress.Controls.Add(textBoxTranslation);
            groupBoxProgress.Controls.Add(buttonSpeakTranslation);
            groupBoxProgress.Controls.Add(buttonAddToLearningContent);
            groupBoxProgress.Controls.Add(textBoxOriginal);
            groupBoxProgress.Controls.Add(buttonSpeakOriginal);
            groupBoxProgress.Controls.Add(labelTranslation);
            groupBoxProgress.Controls.Add(labelOriginal);
            groupBoxProgress.Controls.Add(buttonTranslate);
            groupBoxProgress.Dock = DockStyle.Top;
            groupBoxProgress.Location = new Point(3, 3);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Size = new Size(326, 477);
            groupBoxProgress.TabIndex = 24;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxTranslation.Location = new Point(6, 266);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ReadOnly = true;
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(311, 155);
            textBoxTranslation.TabIndex = 5;
            // 
            // buttonSpeakTranslation
            // 
            buttonSpeakTranslation.Location = new Point(235, 427);
            buttonSpeakTranslation.Name = "buttonSpeakTranslation";
            buttonSpeakTranslation.Size = new Size(85, 34);
            buttonSpeakTranslation.TabIndex = 4;
            buttonSpeakTranslation.Text = "🔊朗读译文";
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // buttonAddToLearningContent
            // 
            buttonAddToLearningContent.Location = new Point(208, 203);
            buttonAddToLearningContent.Name = "buttonAddToLearningContent";
            buttonAddToLearningContent.Size = new Size(100, 34);
            buttonAddToLearningContent.TabIndex = 6;
            buttonAddToLearningContent.Text = "📝添加到学习";
            buttonAddToLearningContent.Click += ButtonAddToLearningContent_Click;
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOriginal.Location = new Point(6, 42);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(311, 155);
            textBoxOriginal.TabIndex = 1;
            // 
            // buttonSpeakOriginal
            // 
            buttonSpeakOriginal.Location = new Point(112, 203);
            buttonSpeakOriginal.Name = "buttonSpeakOriginal";
            buttonSpeakOriginal.Size = new Size(85, 34);
            buttonSpeakOriginal.TabIndex = 23;
            buttonSpeakOriginal.Text = "🔊朗读原文";
            buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;
            // 
            // labelTranslation
            // 
            labelTranslation.Location = new Point(6, 243);
            labelTranslation.Name = "labelTranslation";
            labelTranslation.Size = new Size(311, 20);
            labelTranslation.TabIndex = 2;
            labelTranslation.Text = "译文:";
            // 
            // labelOriginal
            // 
            labelOriginal.Location = new Point(6, 17);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(311, 20);
            labelOriginal.TabIndex = 0;
            labelOriginal.Text = "原文:";
            // 
            // buttonTranslate
            // 
            buttonTranslate.Location = new Point(16, 203);
            buttonTranslate.Name = "buttonTranslate";
            buttonTranslate.Size = new Size(85, 34);
            buttonTranslate.TabIndex = 3;
            buttonTranslate.Text = "📚翻译";
            buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // tabPageFiles
            // 
            tabPageFiles.Controls.Add(_textBoxFilter);
            tabPageFiles.Controls.Add(treeViewFiles);
            tabPageFiles.Location = new Point(4, 26);
            tabPageFiles.Name = "tabPageFiles";
            tabPageFiles.Padding = new Padding(3);
            tabPageFiles.Size = new Size(332, 783);
            tabPageFiles.TabIndex = 0;
            tabPageFiles.Text = "📁 目录";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // _textBoxFilter
            // 
            _textBoxFilter = new TextBox();
            _textBoxFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _textBoxFilter.Location = new Point(3, 3);
            _textBoxFilter.Name = "_textBoxFilter";
            _textBoxFilter.Size = new Size(326, 23);
            _textBoxFilter.TabIndex = 1;
            _textBoxFilter.PlaceholderText = "🔍 搜索文件...";
            _textBoxFilter.TextChanged += TextBoxFilter_TextChanged;
            // 
            // treeViewFiles
            // 
            treeViewFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            treeViewFiles.Location = new Point(3, 32);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(326, 748);
            treeViewFiles.TabIndex = 0;
            treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // _tabPageBookmarksAndHighlights
            // 
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxHighlights);
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxBookmarks);
            _tabPageBookmarksAndHighlights.Location = new Point(4, 26);
            _tabPageBookmarksAndHighlights.Name = "_tabPageBookmarksAndHighlights";
            _tabPageBookmarksAndHighlights.Size = new Size(332, 783);
            _tabPageBookmarksAndHighlights.TabIndex = 2;
            _tabPageBookmarksAndHighlights.Text = "🔖 书签 & 高亮";
            // 
            // _groupBoxHighlights
            // 
            _groupBoxHighlights.Controls.Add(groupBoxHighlightColor);
            _groupBoxHighlights.Controls.Add(_listBoxHighlights);
            _groupBoxHighlights.Controls.Add(highlightButtonPanel);
            _groupBoxHighlights.Controls.Add(_buttonBatchRemoveHighlight);
            _groupBoxHighlights.Controls.Add(buttonUndoHighlight);
            _groupBoxHighlights.Controls.Add(_buttonRemoveHighlight);
            _groupBoxHighlights.Controls.Add(_buttonExportHighlights);
            _groupBoxHighlights.Dock = DockStyle.Fill;
            _groupBoxHighlights.Location = new Point(0, 0);
            _groupBoxHighlights.Name = "_groupBoxHighlights";
            _groupBoxHighlights.Size = new Size(332, 418);
            _groupBoxHighlights.TabIndex = 1;
            _groupBoxHighlights.TabStop = false;
            _groupBoxHighlights.Text = "🖍️ 高亮";
            // 
            // groupBoxHighlightColor
            // 
            groupBoxHighlightColor.Controls.Add(radioHighlightYellow);
            groupBoxHighlightColor.Controls.Add(radioHighlightGreen);
            groupBoxHighlightColor.Controls.Add(radioHighlightBlue);
            groupBoxHighlightColor.Controls.Add(radioHighlightPink);
            groupBoxHighlightColor.Controls.Add(radioHighlightOrange);
            groupBoxHighlightColor.Dock = DockStyle.Bottom;
            groupBoxHighlightColor.Location = new Point(3, 353);
            groupBoxHighlightColor.Name = "groupBoxHighlightColor";
            groupBoxHighlightColor.Size = new Size(326, 62);
            groupBoxHighlightColor.TabIndex = 13;
            groupBoxHighlightColor.TabStop = false;
            groupBoxHighlightColor.Text = "🎨";
            // 
            // radioHighlightYellow
            // 
            radioHighlightYellow.Appearance = Appearance.Button;
            radioHighlightYellow.BackColor = Color.Yellow;
            radioHighlightYellow.Checked = true;
            radioHighlightYellow.FlatStyle = FlatStyle.Flat;
            radioHighlightYellow.Location = new Point(5, 19);
            radioHighlightYellow.Name = "radioHighlightYellow";
            radioHighlightYellow.Size = new Size(50, 30);
            radioHighlightYellow.TabIndex = 0;
            radioHighlightYellow.TabStop = true;
            radioHighlightYellow.Tag = 1;
            radioHighlightYellow.UseVisualStyleBackColor = false;
            radioHighlightYellow.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightGreen
            // 
            radioHighlightGreen.Appearance = Appearance.Button;
            radioHighlightGreen.BackColor = Color.LightGreen;
            radioHighlightGreen.FlatStyle = FlatStyle.Flat;
            radioHighlightGreen.Location = new Point(61, 19);
            radioHighlightGreen.Name = "radioHighlightGreen";
            radioHighlightGreen.Size = new Size(50, 30);
            radioHighlightGreen.TabIndex = 1;
            radioHighlightGreen.TabStop = true;
            radioHighlightGreen.Tag = 2;
            radioHighlightGreen.UseVisualStyleBackColor = false;
            radioHighlightGreen.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightBlue
            // 
            radioHighlightBlue.Appearance = Appearance.Button;
            radioHighlightBlue.BackColor = Color.LightBlue;
            radioHighlightBlue.FlatStyle = FlatStyle.Flat;
            radioHighlightBlue.Location = new Point(117, 19);
            radioHighlightBlue.Name = "radioHighlightBlue";
            radioHighlightBlue.Size = new Size(50, 30);
            radioHighlightBlue.TabIndex = 2;
            radioHighlightBlue.TabStop = true;
            radioHighlightBlue.Tag = 3;
            radioHighlightBlue.UseVisualStyleBackColor = false;
            radioHighlightBlue.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightPink
            // 
            radioHighlightPink.Appearance = Appearance.Button;
            radioHighlightPink.BackColor = Color.Pink;
            radioHighlightPink.FlatStyle = FlatStyle.Flat;
            radioHighlightPink.Location = new Point(173, 19);
            radioHighlightPink.Name = "radioHighlightPink";
            radioHighlightPink.Size = new Size(50, 30);
            radioHighlightPink.TabIndex = 3;
            radioHighlightPink.TabStop = true;
            radioHighlightPink.Tag = 4;
            radioHighlightPink.UseVisualStyleBackColor = false;
            radioHighlightPink.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightOrange
            // 
            radioHighlightOrange.Appearance = Appearance.Button;
            radioHighlightOrange.BackColor = Color.Orange;
            radioHighlightOrange.FlatStyle = FlatStyle.Flat;
            radioHighlightOrange.Location = new Point(229, 19);
            radioHighlightOrange.Name = "radioHighlightOrange";
            radioHighlightOrange.Size = new Size(50, 30);
            radioHighlightOrange.TabIndex = 4;
            radioHighlightOrange.TabStop = true;
            radioHighlightOrange.Tag = 5;
            radioHighlightOrange.UseVisualStyleBackColor = false;
            radioHighlightOrange.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // _listBoxHighlights
            // 
            _listBoxHighlights.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _listBoxHighlights.Font = new Font("Microsoft YaHei UI", 10F);
            _listBoxHighlights.Location = new Point(3, 138);
            _listBoxHighlights.Name = "_listBoxHighlights";
            _listBoxHighlights.Size = new Size(326, 213);
            _listBoxHighlights.TabIndex = 2;
            _listBoxHighlights.DoubleClick += ListBoxHighlights_DoubleClick;
            // 
            // highlightButtonPanel
            // 
            highlightButtonPanel.AutoSize = true;
            highlightButtonPanel.Location = new Point(3, 19);
            highlightButtonPanel.Name = "highlightButtonPanel";
            highlightButtonPanel.Size = new Size(326, 0);
            highlightButtonPanel.TabIndex = 1;
            // 
            // _buttonBatchRemoveHighlight
            // 
            _buttonBatchRemoveHighlight.Location = new Point(157, 19);
            _buttonBatchRemoveHighlight.Margin = new Padding(5);
            _buttonBatchRemoveHighlight.Name = "_buttonBatchRemoveHighlight";
            _buttonBatchRemoveHighlight.Size = new Size(132, 35);
            _buttonBatchRemoveHighlight.TabIndex = 11;
            _buttonBatchRemoveHighlight.Text = "🗑️ 批量删除";
            _buttonBatchRemoveHighlight.Click += ButtonBatchRemoveHighlight_Click;
            // 
            // buttonUndoHighlight
            // 
            buttonUndoHighlight.Location = new Point(13, 64);
            buttonUndoHighlight.Margin = new Padding(5);
            buttonUndoHighlight.Name = "buttonUndoHighlight";
            buttonUndoHighlight.Size = new Size(132, 35);
            buttonUndoHighlight.TabIndex = 12;
            buttonUndoHighlight.Text = "↩️ 撤销";
            buttonUndoHighlight.Click += ButtonUndoHighlight_Click;
            // 
            // _buttonRemoveHighlight
            // 
            _buttonRemoveHighlight.Location = new Point(13, 19);
            _buttonRemoveHighlight.Margin = new Padding(5);
            _buttonRemoveHighlight.Name = "_buttonRemoveHighlight";
            _buttonRemoveHighlight.Size = new Size(132, 35);
            _buttonRemoveHighlight.TabIndex = 10;
            _buttonRemoveHighlight.Text = "🗑️ 删除高亮";
            _buttonRemoveHighlight.Click += ButtonRemoveHighlight_Click;
            // 
            // _buttonExportHighlights
            // 
            _buttonExportHighlights.Location = new Point(157, 64);
            _buttonExportHighlights.Margin = new Padding(5);
            _buttonExportHighlights.Name = "_buttonExportHighlights";
            _buttonExportHighlights.Size = new Size(132, 35);
            _buttonExportHighlights.TabIndex = 14;
            _buttonExportHighlights.Text = "📊 导出高亮到Excel";
            _buttonExportHighlights.Click += ButtonExportHighlights_Click;
            // 
            // _groupBoxBookmarks
            // 
            _groupBoxBookmarks.Controls.Add(_listBoxBookmarks);
            _groupBoxBookmarks.Controls.Add(_textBoxBookmarkTitle);
            _groupBoxBookmarks.Controls.Add(buttonPanel);
            _groupBoxBookmarks.Dock = DockStyle.Bottom;
            _groupBoxBookmarks.Location = new Point(0, 418);
            _groupBoxBookmarks.Name = "_groupBoxBookmarks";
            _groupBoxBookmarks.Size = new Size(332, 365);
            _groupBoxBookmarks.TabIndex = 0;
            _groupBoxBookmarks.TabStop = false;
            _groupBoxBookmarks.Text = "🔖 书签";
            // 
            // _listBoxBookmarks
            // 
            _listBoxBookmarks.Dock = DockStyle.Fill;
            _listBoxBookmarks.Font = new Font("Microsoft YaHei UI", 10F);
            _listBoxBookmarks.Location = new Point(3, 86);
            _listBoxBookmarks.Name = "_listBoxBookmarks";
            _listBoxBookmarks.Size = new Size(326, 276);
            _listBoxBookmarks.TabIndex = 0;
            _listBoxBookmarks.DoubleClick += ListBoxBookmarks_DoubleClick;
            // 
            // _textBoxBookmarkTitle
            // 
            _textBoxBookmarkTitle.Dock = DockStyle.Top;
            _textBoxBookmarkTitle.Location = new Point(3, 63);
            _textBoxBookmarkTitle.Margin = new Padding(5);
            _textBoxBookmarkTitle.Name = "_textBoxBookmarkTitle";
            _textBoxBookmarkTitle.PlaceholderText = "输入书签名称...";
            _textBoxBookmarkTitle.Size = new Size(326, 23);
            _textBoxBookmarkTitle.TabIndex = 1;
            // 
            // buttonPanel
            // 
            buttonPanel.Controls.Add(_buttonRemoveBookmark);
            buttonPanel.Controls.Add(_buttonAddBookmark);
            buttonPanel.Dock = DockStyle.Top;
            buttonPanel.Location = new Point(3, 19);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(326, 44);
            buttonPanel.TabIndex = 2;
            // 
            // _buttonRemoveBookmark
            // 
            _buttonRemoveBookmark.Location = new Point(5, 5);
            _buttonRemoveBookmark.Margin = new Padding(5);
            _buttonRemoveBookmark.Name = "_buttonRemoveBookmark";
            _buttonRemoveBookmark.Size = new Size(100, 36);
            _buttonRemoveBookmark.TabIndex = 1;
            _buttonRemoveBookmark.Text = "🗑️ 删除书签";
            _buttonRemoveBookmark.Click += ButtonRemoveBookmark_Click;
            // 
            // _buttonAddBookmark
            // 
            _buttonAddBookmark.Location = new Point(115, 5);
            _buttonAddBookmark.Margin = new Padding(5);
            _buttonAddBookmark.Name = "_buttonAddBookmark";
            _buttonAddBookmark.Size = new Size(100, 36);
            _buttonAddBookmark.TabIndex = 0;
            _buttonAddBookmark.Text = "➕ 添加书签";
            _buttonAddBookmark.Click += ButtonAddBookmark_Click;
            // 
            // buttonOpenFolder
            // 
            buttonOpenFolder.Dock = DockStyle.Top;
            buttonOpenFolder.Location = new Point(0, 0);
            buttonOpenFolder.Name = "buttonOpenFolder";
            buttonOpenFolder.Size = new Size(340, 35);
            buttonOpenFolder.TabIndex = 0;
            buttonOpenFolder.Text = "📁 选择文件夹";
            buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // _pageTransitionTimer
            // 
            _pageTransitionTimer.Interval = 50;
            _pageTransitionTimer.Tick += PageTransitionTimer_Tick;
            // 
            // groupBox1
            // 
            groupBox1.Location = new Point(5, 321);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(332, 138);
            groupBox1.TabIndex = 25;
            groupBox1.TabStop = false;
            groupBox1.Text = "书签";
            // 
            // groupBox2
            // 
            groupBox2.Location = new Point(15, 481);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(332, 138);
            groupBox2.TabIndex = 25;
            groupBox2.TabStop = false;
            groupBox2.Text = "书签";
            // 
            // PdfReaderForm
            // 
            ClientSize = new Size(1380, 848);
            Controls.Add(splitContainer1);
            Name = "PdfReaderForm";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panelPdf.ResumeLayout(false);
            panelNavigation.ResumeLayout(false);
            panelNavigation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).EndInit();
            _ocrPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_ocrPictureBox).EndInit();
            _pageTransitionOverlay.ResumeLayout(false);
            panelLeftContainer.ResumeLayout(false);
            tabControlLeft.ResumeLayout(false);
            tabPageThumbnails.ResumeLayout(false);
            panelThumbnails.ResumeLayout(false);
            tabPageTranslate.ResumeLayout(false);
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
            tabPageFiles.ResumeLayout(false);
            _tabPageBookmarksAndHighlights.ResumeLayout(false);
            _groupBoxHighlights.ResumeLayout(false);
            _groupBoxHighlights.PerformLayout();
            groupBoxHighlightColor.ResumeLayout(false);
            _groupBoxBookmarks.ResumeLayout(false);
            _groupBoxBookmarks.PerformLayout();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);

        }

        private void SetupNavigationPanelChildEvents()
        {
            foreach (Control ctrl in panelNavigation.Controls)
            {
                ctrl.MouseDown += PanelNavigationChild_MouseDown;
                ctrl.MouseMove += PanelNavigationChild_MouseMove;
                ctrl.MouseUp += PanelNavigationChild_MouseUp;
            }
        }

        private void PanelNavigationChild_MouseDown(object? sender, MouseEventArgs e)
        {
            if (sender is Control ctrl)
            {
                Point relativePoint = ctrl.PointToScreen(e.Location);
                PanelNavigation_MouseDown(panelNavigation, new MouseEventArgs(e.Button, e.Clicks, relativePoint.X, relativePoint.Y, e.Delta));
            }
        }

        private void PanelNavigationChild_MouseMove(object? sender, MouseEventArgs e)
        {
            if (sender is Control ctrl)
            {
                Point relativePoint = ctrl.PointToScreen(e.Location);
                PanelNavigation_MouseMove(panelNavigation, new MouseEventArgs(e.Button, e.Clicks, relativePoint.X, relativePoint.Y, e.Delta));
            }
        }

        private void PanelNavigationChild_MouseUp(object? sender, MouseEventArgs e)
        {
            if (sender is Control ctrl)
            {
                Point relativePoint = ctrl.PointToScreen(e.Location);
                PanelNavigation_MouseUp(panelNavigation, new MouseEventArgs(e.Button, e.Clicks, relativePoint.X, relativePoint.Y, e.Delta));
            }
        }

        #endregion

        #region Navigation Panel Drag

        private void PanelNavigation_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _isNavPanelDragging = true;
                _navPanelStartPoint = PointToScreen(e.Location);
                panelNavigation.Cursor = Cursors.SizeAll;
                panelNavigation.Capture = true;
            }
        }

        private void PanelNavigation_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_isNavPanelDragging)
            {
                Point currentScreenPoint = PointToScreen(e.Location);
                int deltaX = currentScreenPoint.X - _navPanelStartPoint.X;
                int deltaY = currentScreenPoint.Y - _navPanelStartPoint.Y;

                int newX = panelNavigation.Left + deltaX;
                int newY = panelNavigation.Top + deltaY;

                int leftBoundary = 0;
                int rightBoundary = ClientSize.Width - panelNavigation.Width;

                newX = Math.Max(leftBoundary, Math.Min(newX, rightBoundary));
                newY = Math.Max(0, Math.Min(newY, ClientSize.Height - panelNavigation.Height));

                panelNavigation.Location = new Point(newX, newY);
                _navPanelStartPoint = currentScreenPoint;
            }
        }

        private void PanelNavigation_MouseUp(object? sender, MouseEventArgs e)
        {
            if (_isNavPanelDragging)
            {
                _isNavPanelDragging = false;
                panelNavigation.Cursor = Cursors.Default;
                panelNavigation.Capture = false;
            }
        }

        #endregion

        #region OCR Panel Drag





        private void OcrCloseButton_Click(object? sender, EventArgs e)
        {
            HideOcrOverlay();
        }

        #endregion

        #region Event Handlers

        private void ButtonOpenFolder_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _presenter.LoadFolder(dialog.SelectedPath);
            }
        }

        private void TreeViewFiles_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            FileSelected?.Invoke(this, EventArgs.Empty);
        }

        private void TextBoxFilter_TextChanged(object? sender, EventArgs e)
        {
            UpdateFileListDisplay();
        }

        private void PictureBoxPdf_MouseDown(object? sender, MouseEventArgs e)
        {
            try
            {
                // 先停止可能运行的长按计时器
                StopLongPressTimer();

                if (e.Button == MouseButtons.Left)
                {
                    // 左键：长按拖动，短按可能用于其他功能
                    _isLongPressPending = true;
                    _longPressStartLocation = e.Location;
                    _longPressDragStarted = false;

                    // 启动长按计时器
                    StartLongPressTimer();
                    return;
                }

                if (e.Button == MouseButtons.Right)
                {
                    var now = DateTime.Now;
                    var timeDiff = (now - _lastClickTime).TotalMilliseconds;
                    var distance = Math.Sqrt(Math.Pow(e.Location.X - _lastClickLocation.X, 2) + Math.Pow(e.Location.Y - _lastClickLocation.Y, 2));

                    if (timeDiff < DoubleClickTime_ms && distance < DoubleClickDistance)
                    {
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

                    if (_isDrawing || (ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        try
                        {
                            _pen.Color = Color.Red;
                            _pen.Width = 4f;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error initializing drawing pen");
                        }

                        _isDrawing = true;
                        EnsureAnnotationBitmap();
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        var imgPt = ClientToImage(e.Location);
                        _currentStrokePoints = new List<PointF>() { imgPt };
                        try
                        {
                            pictureBoxPdf.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error invalidating pictureBox on MouseDown");
                        }
                        return;
                    }

                    // 右键用于高亮选择
                    if (_isHighlightMode)
                    {
                        _isSelecting = true;
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        try
                        {
                            pictureBoxPdf.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error invalidating pictureBox on highlight start");
                        }
                    }
                    else
                    {
                        _isSelecting = true;
                        _selectStart = e.Location;
                        _selectEnd = e.Location;
                        try
                        {
                            pictureBoxPdf.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error invalidating pictureBox on MouseDown (selecting)");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_MouseDown");
                // 重置状态，避免死锁
                _isSelecting = false;
                _isDrawing = false;
                StopLongPressTimer();
            }
        }

        private void PictureBoxPdf_MouseMove(object? sender, MouseEventArgs e)
        {
            try
            {
                // 如果锁定状态，不允许拖动
                if (_isLocked)
                    return;

                // 长按检测：如果在长按等待期间移动超过一定距离，取消长按
                if (_isLongPressPending && !_longPressDragStarted)
                {
                    var distance = Math.Sqrt(
                        Math.Pow(e.Location.X - _longPressStartLocation.X, 2) +
                        Math.Pow(e.Location.Y - _longPressStartLocation.Y, 2)
                    );

                    // 如果移动距离超过阈值，立即开始拖动
                    if (distance > DoubleClickDistance)
                    {
                        StopLongPressTimer();
                        StartDragging(e.Location);
                        return;
                    }
                }

                var ctrlDown = (ModifierKeys & Keys.Control) == Keys.Control;
                var leftDown = (Control.MouseButtons & MouseButtons.Left) == MouseButtons.Left;
                if (_isDrawing || (ctrlDown && leftDown))
                {
                    _selectEnd = e.Location;
                    pictureBoxPdf.Invalidate();
                    return;
                }

                if (_isSelecting)
                {
                    _selectEnd = e.Location;
                    pictureBoxPdf.Invalidate();
                    return;
                }

                if (_isDragging || _longPressDragStarted)
                {
                    var deltaX = e.Location.X - _dragStart.X;
                    var deltaY = e.Location.Y - _dragStart.Y;
                    _imageOffset = new Point(_imageOffset.X + deltaX, _imageOffset.Y + deltaY);
                    _dragStart = e.Location;
                    pictureBoxPdf.Invalidate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_MouseMove");
            }
        }


        private void PictureBoxPdf_MouseUp(object? sender, MouseEventArgs e)
        {
            try
            {
                // 清理长按状态
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
                        if (_annotationBitmap != null)
                        {
                            var ip1 = ClientToImage(_selectStart);
                            var ip2 = ClientToImage(_selectEnd);
                            _annotationGraphics!.SmoothingMode = SmoothingMode.AntiAlias;
                            _annotationGraphics.DrawLine(_pen, ip1, ip2);
                            _presenter.SaveAnnotationForCurrentPage((Bitmap)_annotationBitmap.Clone());
                            var imgW = _annotationBitmap.Width;
                            var imgH = _annotationBitmap.Height;
                            var pts = new List<float>() { ip1.X / imgW, ip1.Y / imgH, ip2.X / imgW, ip2.Y / imgH };
                            _presenter.AddAnnotationStroke(pts.ToArray(), _pen.Color.ToArgb(), _pen.Width, imgW, imgH);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving annotation");
                    }
                    finally { _currentStrokePoints = null; }
                    try
                    {
                        pictureBoxPdf.Invalidate();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error invalidating after drawing");
                    }
                    return;
                }


                if (_isDragging || _longPressDragStarted)
                {
                    _isDragging = false;
                    _longPressDragStarted = false;
                    pictureBoxPdf.Cursor = Cursors.Default;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_MouseUp");
                _isSelecting = false;
                _isDrawing = false;
                _isDragging = false;
                _longPressDragStarted = false;
                StopLongPressTimer();
            }
        }

        // 长按计时器相关方法
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

        private void LongPressTimer_Tick(object? sender, EventArgs e)
        {
            StopLongPressTimer();
            if (_isLongPressPending)
            {
                // 长按超时，开始拖动
                StartDragging(_longPressStartLocation);
            }
        }

        private void StartDragging(Point startLocation)
        {
            if (_isLocked)
                return;

            _isDragging = true;
            _longPressDragStarted = true;
            _dragStart = startLocation;
            pictureBoxPdf.Cursor = Cursors.Hand;
        }



        private async Task<string> GetOcrTextFromSelectionAsync(Rectangle selectionRect, Bitmap currentPageImage)
        {
            if (currentPageImage == null || _presenter == null) return string.Empty;

            // 检查OCR服务是否可用
            if (!_presenter.IsOcrAvailable())
            {
                _logger.LogWarning("OCR service is not available, skipping text recognition");
                return string.Empty;
            }

            var imgRect = GetImageDisplayRect();
            if (imgRect.Width <= 0 || imgRect.Height <= 0) return string.Empty;

            // 计算选择区域在原始图像中的实际坐标
            float scaleX = (float)currentPageImage.Width / imgRect.Width;
            float scaleY = (float)currentPageImage.Height / imgRect.Height;

            float actualX = (selectionRect.X - imgRect.X) * scaleX;
            float actualY = (selectionRect.Y - imgRect.Y) * scaleY;
            float actualWidth = selectionRect.Width * scaleX;
            float actualHeight = selectionRect.Height * scaleY;

            // 确保裁剪区域在图像范围内
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
                var result = await _presenter.OcrBitmapAsync(cropped);
                return result ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OCR recognition failed");
                return string.Empty;
            }
        }

        private void PictureBoxPdf_MouseWheel(object? sender, MouseEventArgs e)
        {
            try
            {
                if (e.Delta != 0)
                {
                    // 使用 _navigationManager 处理鼠标滚轮事件
                    _navigationManager?.ZoomByMouseWheel(e.Delta, (ModifierKeys & Keys.Control) == Keys.Control);
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
        }

        private void PictureBoxPdf_Paint(object? sender, PaintEventArgs e)
        {
            try
            {
                // 先填充背景色
                if (_isNightMode)
                {
                    e.Graphics.Clear(Color.FromArgb(20, 20, 20));
                }
                else
                {
                    e.Graphics.Clear(Color.White);
                }

                // 完全自己绘制图片，使用私有字段 _currentPageImage
                if (_currentPageImage != null)
                {
                    var imgRect = GetImageDisplayRect();
                    e.Graphics.DrawImage(_currentPageImage, imgRect);

                    // 绘制注释（包括高亮选择框和其他图形）
                    _navigationManager?.DrawAnnotations(e.Graphics, imgRect);
                }

                if (!string.IsNullOrEmpty(_currentPdfPath) && _currentPageImage != null)
                {
                    DrawHighlightsFromLayer(e.Graphics);
                }
            }
            catch (ObjectDisposedException ex)
            {
                _logger.LogWarning(ex, "Object disposed during Paint event");
                // 不显示红叉的方法是不抛出异常，让控件继续
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_Paint");
                // 捕获所有异常，防止红叉出现
            }
        }

        private void DrawHighlightsFromLayer(Graphics g)
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                // 使用 _highlightManager 的高亮位图
                if (_highlightManager != null)
                {
                    _highlightManager.DrawHighlightsFromLayer(g);
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

        private void EnsureAnnotationBitmap()
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                // 安全地获取图像尺寸
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

        private PointF ClientToImage(Point clientPt)
        {
            try
            {
                if (_currentPageImage == null)
                    return new PointF(clientPt.X, clientPt.Y);

                int imgWidth, imgHeight;
                try
                {
                    imgWidth = _currentPageImage.Width;
                    imgHeight = _currentPageImage.Height;
                }
                catch (ObjectDisposedException)
                {
                    return new PointF(clientPt.X, clientPt.Y);
                }

                var scaleX = (float)imgWidth / pictureBoxPdf.ClientSize.Width;
                var scaleY = (float)imgHeight / pictureBoxPdf.ClientSize.Height;
                return new PointF(clientPt.X * scaleX, clientPt.Y * scaleY);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ClientToImage");
                return new PointF(clientPt.X, clientPt.Y);
            }
        }

        private Rectangle GetSelectionRectangle(Point start, Point end)
        {
            return new Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(end.X - start.X),
                Math.Abs(end.Y - start.Y)
            );
        }

        private void ButtonSelectOcr_Click(object? sender, EventArgs e)
        {
            SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonTranslate_Click(object? sender, EventArgs e)
        {
            if (_presenter != null && !_presenter.IsTranslationServiceAvailable())
            {
                MessageBox.Show("翻译服务不可用，请检查百度翻译API配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            TranslateClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSpeakOriginal_Click(object? sender, EventArgs e)
        {
            var text = textBoxOriginal?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("请先输入或选择要朗读的文本", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_presenter != null && !_presenter.IsTTSServiceAvailable())
            {
                MessageBox.Show("朗读服务不可用，请检查TTS配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SpeakOriginal?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSpeakTranslation_Click(object? sender, EventArgs e)
        {
            var text = textBoxTranslation?.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("请先进行翻译", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (_presenter != null && !_presenter.IsTTSServiceAvailable())
            {
                MessageBox.Show("朗读服务不可用，请检查TTS配置", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            SpeakTranslation?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            AiQuestionAsked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAddToLearning_Click(object? sender, EventArgs e)
        {
            AddToLearningList?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonOpenAI_Click(object? sender, EventArgs e)
        {
            RaiseAiQuestionAsked();
        }

        public void RaiseAiQuestionAsked()
        {
            if (_aiPanelPopupService != null)
            {
                // 获取原文作为上下文
                var context = textBoxOriginal?.Text ?? string.Empty;
                _aiPanelPopupService.ShowAIAbilityPanel(this, context, null, context);
            }
            else
            {
                MessageBox.Show("AI面板服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ButtonPrev_Click(object? sender, EventArgs e)
        {
            if (_presenter == null) return;
            _presenter.PreviousPage();
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            if (_presenter == null) return;
            _presenter.NextPage();
        }

        private void ButtonNightMode_Click(object? sender, EventArgs e)
        {
            NightMode();
            ToggleNightMode?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonTranslationToggle_Click(object? sender, EventArgs e)
        {
            TranslationToggle();
            ToggleTranslation?.Invoke(this, EventArgs.Empty);
        }

        private void TranslationToggle()
        {
            _isTranslationEnabled = !_isTranslationEnabled;
            if (buttonTranslationToggle != null)
            {
                buttonTranslationToggle.Text = _isTranslationEnabled ? "译" : "译";
                buttonTranslationToggle.BackColor = _isTranslationEnabled ? Color.LightGreen : Color.LightGray;
            }
        }

        private void ButtonLanguage_Click(object? sender, EventArgs e)
        {
            ToggleLanguage();
            LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void TextBoxPage_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                PageChanged?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                try
                {
                    CleanupAnnotationBitmap();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up annotation bitmap");
                }

                try
                {
                    CleanupHighlightLayer();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error cleaning up highlight layer");
                }

                try
                {
                    if (_currentPageImage != null)
                    {
                        _currentPageImage.Dispose();
                        _currentPageImage = null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing main image");
                }

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

                try
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing components");
                }

                try
                {
                    _nightModeManager?.Dispose();
                    _highlightManager?.Dispose();
                    _bookmarkManager?.Dispose();
                    _navigationManager?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing managers");
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        public void SetImageMode(bool isImageMode)
        {
            _isImageMode = isImageMode;
        }

        public bool GetImageMode()
        {
            return _isImageMode;
        }

        private void buttonAddToLearning_Click_1(object sender, EventArgs e)
        {

        }

        private void ButtonAddToLearningContent_Click(object? sender, EventArgs e)
        {
            try
            {
                var originalText = textBoxOriginal?.Text ?? string.Empty;
                var translationText = textBoxTranslation?.Text ?? string.Empty;

                if (string.IsNullOrWhiteSpace(originalText))
                {
                    ShowWarning("请先输入或选择要学习的文本内容");
                    return;
                }

                // 判断语言和内容类型
                var language = DetectLanguage(originalText);
                var contentType = DetectContentType(originalText, language);

                // 使用AI生成学习内容模板
                _ = GenerateAndAddLearningContentAsync(originalText, translationText, language, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ButtonAddToLearningContent_Click");
                ShowError("添加到学习内容失败: " + ex.Message);
            }
        }

        private string DetectLanguage(string text)
        {
            // 简单的语言检测：检查是否包含中文字符
            if (System.Text.RegularExpressions.Regex.IsMatch(text, @"[\u4e00-\u9fa5]"))
            {
                return Constants.Language.Chinese;
            }
            return Constants.Language.English;
        }

        private string DetectContentType(string text, string language)
        {
            text = text.Trim();

            // 根据文本长度和特征判断内容类型
            if (language == Constants.Language.Chinese)
            {
                // 中文内容类型判断
                if (text.Length == 1)
                    return "字";
                else if (text.Length <= 4 && !text.Contains(" "))
                    return "成语";
                else if (text.Length <= 10)
                    return "短语";
                else if (text.Contains("\n") || text.Length > 50)
                    return "句子";
                else
                    return "短语";
            }
            else
            {
                // 英文内容类型判断
                var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 1)
                    return "单词";
                else if (words.Length <= 3)
                    return "短语";
                else if (text.Contains("\n") || text.Length > 50)
                    return "句子";
                else
                    return "短语";
            }
        }

        private async Task GenerateAndAddLearningContentAsync(string originalText, string translationText, string language, string contentType)
        {
            try
            {
                SetLoadingState(true);

                // 构建AI提示词，生成学习内容模板
                var prompt = BuildLearningContentPrompt(originalText, translationText, language, contentType);

                // 使用AI服务生成内容
                var aiGeneratedContent = await _presenter?.GenerateAiContentAsync(prompt) ?? string.Empty;

                // 合成最终的学习内容JSON
                var learningContent = BuildLearningContentJson(originalText, translationText, language, contentType, aiGeneratedContent);

                // 使用PendingContentService保存内容
                _pendingContentService?.Add(learningContent, language, GetCategoryFromContentType(contentType, language));

                // 触发添加到编辑器事件（供其他监听器使用）
                RaiseAddToEditor(learningContent, language);

                ShowMessage($"已添加到学习内容\n语言: {language}\n类型: {contentType}\n\n请到内容编辑页面查看");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating learning content");
                ShowError("生成学习内容失败: " + ex.Message);
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private string GetCategoryFromContentType(string contentType, string language)
        {
            if (language == Constants.Language.Chinese)
            {
                return contentType switch
                {
                    "识字" => Constants.SubCategory.ChineseCharacter,
                    "成语" => Constants.SubCategory.ChineseIdiom,
                    "短语" => Constants.SubCategory.ChinesePhrase,
                    "诗词" => Constants.SubCategory.ChinesePoem,
                    _ => Constants.SubCategory.ChineseComprehensive
                };
            }
            else
            {
                return contentType switch
                {
                    "英语单词" => Constants.SubCategory.EnglishWord,
                    "英语短语" => Constants.SubCategory.EnglishPhrase,
                    "英语句子" => Constants.SubCategory.EnglishSentence,
                    _ => Constants.SubCategory.EnglishComprehensive
                };
            }
        }

        private string BuildLearningContentPrompt(string originalText, string translationText, string language, string contentType)
        {
            var languageName = language == Constants.Language.Chinese ? "中文" : "英语";

            return $@"请为以下{languageName}{contentType}生成学习内容模板，包括：
1. 释义/翻译（如果已有翻译则优化）
2. 例句（1-2个）
3. 相关知识点或记忆技巧

原文: {originalText}
译文: {(string.IsNullOrEmpty(translationText) ? "请提供翻译" : translationText)}

请以JSON格式返回，格式如下：
{{""meaning"": ""释义"", ""example"": ""例句"", ""tips"": ""记忆技巧""}}";
        }

        private string BuildLearningContentJson(string originalText, string translationText, string language, string contentType, string aiGeneratedContent)
        {
            // 解析AI生成的内容
            string meaning = translationText;
            string example = "";
            string tips = "";

            try
            {
                if (!string.IsNullOrEmpty(aiGeneratedContent))
                {
                    // 尝试解析JSON
                    var jsonStart = aiGeneratedContent.IndexOf('{');
                    var jsonEnd = aiGeneratedContent.LastIndexOf('}');
                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        var jsonStr = aiGeneratedContent.Substring(jsonStart, jsonEnd - jsonStart + 1);
                        var jsonObj = Newtonsoft.Json.Linq.JObject.Parse(jsonStr);
                        meaning = jsonObj["meaning"]?.ToString() ?? translationText;
                        example = jsonObj["example"]?.ToString() ?? "";
                        tips = jsonObj["tips"]?.ToString() ?? "";
                    }
                }
            }
            catch
            {
                // 如果解析失败，使用原始翻译
            }

            // 根据内容类型构建不同的JSON结构
            if (language == Constants.Language.Chinese)
            {
                if (contentType == "字")
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Character = originalText,
                        Meaning = meaning,
                        Example = example,
                        Tips = tips,
                        StrokeOrder = ""
                    });
                }
                else if (contentType == "成语")
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Idiom = originalText,
                        Meaning = meaning,
                        Example = example,
                        Origin = tips
                    });
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Phrase = originalText,
                        Meaning = meaning,
                        Example = example,
                        Tips = tips
                    });
                }
            }
            else
            {
                if (contentType == "单词")
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Word = originalText,
                        Meaning = meaning,
                        Example = example,
                        Tips = tips,
                        SyllableBreakdown = ""
                    });
                }
                else
                {
                    return Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        Phrase = originalText,
                        Meaning = meaning,
                        Example = example,
                        Tips = tips
                    });
                }
            }
        }

        private void _loadingIndicator_Click(object sender, EventArgs e)
        {

        }

        private static void SetDoubleBuffered(Control control)
        {
            if (control == null) return;
            control.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            control.UpdateStyles();
        }

        public void SetSecondPageImage(Bitmap? bmp)
        {
            throw new NotImplementedException();
        }
    }
}
