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

        private TabPage? _tabPageBookmarksAndHighlights;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private bool _isImageMode = false;

        private Panel? _pageTransitionOverlay;
        private System.Windows.Forms.Timer? _pageTransitionTimer;

        private Bitmap? _currentPageImage;
        private Bitmap? _secondPageImage;

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
        private bool _isDualPage = false;
        private bool _isFullscreen = false;
        private Bitmap? _highlightBitmap;

        private SplitContainer splitContainerMain;
        private Button? buttonSpeakOriginal;
        private GroupBox? groupBoxProgress;

        private Panel? _statusBar;
        private Label? _statusLabelLeft;
        private Label? _statusLabelRight;

        private Panel? _toolbarGroupNav;
        private Panel? _toolbarGroupView;
        private Panel? _toolbarGroupMode;
        private Panel? _toolbarGroupTools;

        private Button? buttonZoomIn;
        private Button? buttonZoomOut;
        private Button? buttonDualPage;
        private Button? buttonFullscreen;
        private Button? buttonHighlightMode;
        private Button? buttonPenMode;
        private Button? buttonTextMode;

        public PdfReaderFormV2(ILogger<PdfReaderFormV2> logger, IAIPanelPopupService? aiPanelPopupService = null, Services.Learning.IPendingContentService? pendingContentService = null, IHighlightService? highlightService = null, IBookmarkService? bookmarkService = null)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aiPanelPopupService = aiPanelPopupService;
            _pendingContentService = pendingContentService;
            _highlightService = highlightService ?? new HighlightService();
            _bookmarkService = bookmarkService ?? new BookmarkService();
            KeyPreview = true;
            Load += PdfReaderFormV2_Load;
            Resize += PdfReaderFormV2_Resize;
            KeyDown += PdfReaderFormV2_KeyDown;

            InitializeManagers();
        }

        private void InitializeManagers()
        {
            _nightModeManager = new PdfReaderNightModeManager(_logger, this);
            _highlightManager = new PdfReaderHighlightManager(_logger, this, _highlightService);
            _bookmarkManager = new PdfReaderBookmarkManager(_logger, this, _bookmarkService);
            _navigationManager = new PdfReaderNavigationManager(_logger, this);

            _navigationManager.IsHighlightModeCallback = () => _highlightManager?.IsHighlightMode ?? true;
            _navigationManager.AddHighlightCallback = rect => _highlightManager?.AddHighlight(rect);
            _navigationManager.AddTextCallback = point => ShowTextAnnotationDialog(point);

            if (pictureBoxPdf != null)
            {
                pictureBoxPdf.MouseDown += _navigationManager.MouseDown;
                pictureBoxPdf.MouseMove += _navigationManager.MouseMove;
                pictureBoxPdf.MouseUp += _navigationManager.MouseUp;
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
        public bool IsTranslationEnabled
        {
            get => _isTranslationEnabled;
            set => _isTranslationEnabled = value;
        }

        public bool IsDualPage => _isDualPage;

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

        public Form Form => this;

        public Button? ButtonLanguage => throw new NotImplementedException();

        public Bitmap? SecondPageImage { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void OnSelectOcrClicked() => SelectOcrClicked?.Invoke(this, EventArgs.Empty);
        public void OnTranslateClicked() => TranslateClicked?.Invoke(this, EventArgs.Empty);

        #endregion

        private void PdfReaderFormV2_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
            _presenter?.LoadLastSessionAndRestore();
            UpdateStatusBar();
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
                    case Keys.F:
                        ToggleFullscreen();
                        e.Handled = true;
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
                    if (panelNavigation != null)
                        panelNavigation.Visible = false;
                    if (_statusBar != null)
                        _statusBar.Visible = false;
                    _isFullscreen = true;
                }
                else
                {
                    FormBorderStyle = FormBorderStyle.Sizable;
                    WindowState = FormWindowState.Normal;
                    if (panelNavigation != null)
                        panelNavigation.Visible = true;
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
            pictureBoxPdf.Invalidate();
        }

        private void UpdateStatusBar()
        {
            if (_statusLabelLeft != null)
            {
                int totalPages = 0;
                if (labelPageCount != null && !string.IsNullOrEmpty(labelPageCount.Text))
                {
                    var parts = labelPageCount.Text.Replace("/", "").Trim();
                    int.TryParse(parts, out totalPages);
                }
                int currentPage = _currentPageIndex + 1;
                double progress = totalPages > 0 ? (double)currentPage / totalPages * 100 : 0;
                _statusLabelLeft.Text = $"第 {currentPage} 页 / 共 {totalPages} 页 · {progress:F0}%";
            }
            if (_statusLabelRight != null)
            {
                string mode = _isHighlightMode ? "高亮" : "选择";
                string dualPage = _isDualPage ? "双页" : "单页";
                _statusLabelRight.Text = $"缩放: {_zoomLevel}% · {mode}模式 · {dualPage}";
            }
        }

        private void InitializeBookmarkAndHighlightUI()
        {
            bool needInitialize = false;

            if (_tabPageBookmarksAndHighlights == null)
            {
                needInitialize = true;
            }
            else if (tabControlLeft != null && !tabControlLeft.TabPages.Contains(_tabPageBookmarksAndHighlights))
            {
                needInitialize = true;
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
                if (tabControlLeft != null)
                {
                    if (_tabPageBookmarksAndHighlights != null && tabControlLeft.TabPages.Contains(_tabPageBookmarksAndHighlights))
                    {
                        tabControlLeft.TabPages.Remove(_tabPageBookmarksAndHighlights);
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
                pictureBoxPdf?.Invalidate();
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
            UpdateStatusBar();
        }

        private void ButtonResetView_Click(object? sender, EventArgs e)
        {
            _navigationManager?.ResetZoom();
            UpdateStatusBar();
        }

        private void TrackBarZoom_Scroll(object? sender, EventArgs e)
        {
            _navigationManager?.Zoom(trackBarZoom.Value);
            _zoomLevel = trackBarZoom.Value;
            UpdateStatusBar();
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
            _presenter?.ExportHighlightsToExcel();
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
            Text = $"学习助手 - {Path.GetFileName(pdfPath)}";
        }

        public void SetPresenter(PdfPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.SetView(this);
        }

        public void SetFileList(IEnumerable<string> files)
        {
            treeViewFiles.Nodes.Clear();
            foreach (var file in files)
            {
                treeViewFiles.Nodes.Add(file);
            }
        }

        public void SetImageList(IEnumerable<string> imageFiles)
        {
            treeViewFiles.Nodes.Clear();
            foreach (var file in imageFiles)
            {
                treeViewFiles.Nodes.Add(Path.GetFileName(file));
            }
        }

        public void SetPageCount(int count)
        {
            labelPageCount.Text = $"/ {count}";
            UpdateStatusBar();
        }

        public void SetCurrentPageIndex(int pageIndex)
        {
            bool isForward = pageIndex > _currentPageIndex;
            _currentPageIndex = pageIndex;
            textBoxPage.Text = (pageIndex + 1).ToString();
            StartPageTransition(isForward);
            LoadHighlightsForCurrentPage();
            UpdateStatusBar();
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
                Bitmap? oldImageToDispose = null;
                if (_nightModeManager?.IsNightMode ?? false)
                {
                    imageToDisplay = new Bitmap(_nightModeManager.InvertImage(bmp));
                    if (_currentPageImage != null && _currentPageImage != bmp)
                    {
                        oldImageToDispose = _currentPageImage;
                    }
                }

                var old = _currentPageImage;
                _currentPageImage = imageToDisplay;

                pictureBoxPdf.Image = null;

                if (old != null && old != bmp && old != imageToDisplay)
                {
                    oldImageToDispose = old;
                }

                if (oldImageToDispose != null)
                {
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        try
                        {
                            oldImageToDispose?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to dispose old image");
                        }
                    }, TaskScheduler.Default);
                }

                pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
                _navigationManager?.LoadAnnotationsForCurrentPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DisplayImage");
                _currentPageImage = bmp;
                pictureBoxPdf.Image = null;
                pictureBoxPdf.Invalidate();
                LoadHighlightsForCurrentPage();
                _navigationManager?.LoadAnnotationsForCurrentPage();
            }
        }

        public void SetSecondPageImage(Bitmap? bmp)
        {
            try
            {
                if (_secondPageImage != null && _secondPageImage != bmp)
                {
                    var old = _secondPageImage;
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        try
                        {
                            old?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to dispose second page image");
                        }
                    }, TaskScheduler.Default);
                }

                if (bmp != null && (_nightModeManager?.IsNightMode ?? false))
                {
                    _secondPageImage = new Bitmap(_nightModeManager.InvertImage(bmp));
                }
                else
                {
                    _secondPageImage = bmp;
                }

                pictureBoxPdf.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SetSecondPageImage");
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
            if (flowLayoutPanelThumbnails == null || thumbnail == null) return;

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
            label.Location = new Point(5, 130);
            label.Size = new Size(100, 18);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Microsoft YaHei UI", 9F);
            label.ForeColor = Color.FromArgb(102, 102, 102);
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
                        panel.BackColor = Color.FromArgb(230, 244, 255);
                        panel.BorderStyle = BorderStyle.None;
                        panel.Padding = new Padding(2);
                        using (var g = panel.CreateGraphics())
                        {
                            using (var pen = new Pen(Color.FromArgb(64, 150, 255), 2))
                            {
                                g.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                            }
                        }
                        panel.BringToFront();
                    }
                    else
                    {
                        _nightModeManager?.UpdateThumbnailPanelColor(panel);
                        panel.BorderStyle = BorderStyle.None;
                        panel.Padding = new Padding(0);
                        panel.Invalidate();
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
            return treeViewFiles.SelectedNode?.Text ?? string.Empty;
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
                    displayWidth = controlWidth;
                    displayHeight = (int)(controlWidth / imageAspect);
                }
                else
                {
                    displayHeight = controlHeight;
                    displayWidth = (int)(controlHeight * imageAspect);
                }

                float scale = _navigationManager != null ? _navigationManager.ZoomLevel / 100.0f : _zoomLevel / 100.0f;
                displayWidth = (int)(displayWidth * scale);
                displayHeight = (int)(displayHeight * scale);

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

                    int maxWidth = panelPdf.ClientSize.Width - 100;
                    int maxHeight = panelPdf.ClientSize.Height - 100;

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
                var context = textBoxOriginal?.Text ?? string.Empty;
                _aiPanelPopupService.ShowAIAbilityPanel(this, context, null, context);
            }
            else
            {
                MessageBox.Show("AI面板服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            if (trackBarZoom.Value < trackBarZoom.Maximum)
            {
                trackBarZoom.Value = Math.Min(trackBarZoom.Value + 10, trackBarZoom.Maximum);
                TrackBarZoom_Scroll(sender, e);
            }
        }

        private void ButtonZoomOut_Click(object? sender, EventArgs e)
        {
            if (trackBarZoom.Value > trackBarZoom.Minimum)
            {
                trackBarZoom.Value = Math.Max(trackBarZoom.Value - 10, trackBarZoom.Minimum);
                TrackBarZoom_Scroll(sender, e);
            }
        }

        private void ButtonHighlightMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Highlight);
        }

        private void ButtonPenMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Pen);
        }

        private void ButtonTextMode_Click(object? sender, EventArgs e)
        {
            SetAnnotationToolMode(AnnotationToolMode.Text);
        }

        private void SetAnnotationToolMode(AnnotationToolMode mode)
        {
            _navigationManager?.SetToolMode(mode);

            if (buttonHighlightMode != null)
            {
                buttonHighlightMode.BackColor = mode == AnnotationToolMode.Highlight ? Color.FromArgb(230, 244, 255) : Color.White;
                buttonHighlightMode.FlatAppearance.BorderColor = mode == AnnotationToolMode.Highlight ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            if (buttonPenMode != null)
            {
                buttonPenMode.BackColor = mode == AnnotationToolMode.Pen ? Color.FromArgb(230, 244, 255) : Color.White;
                buttonPenMode.FlatAppearance.BorderColor = mode == AnnotationToolMode.Pen ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            if (buttonTextMode != null)
            {
                buttonTextMode.BackColor = mode == AnnotationToolMode.Text ? Color.FromArgb(230, 244, 255) : Color.White;
                buttonTextMode.FlatAppearance.BorderColor = mode == AnnotationToolMode.Text ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }

            if (_highlightManager != null)
            {
                _highlightManager.IsHighlightMode = mode == AnnotationToolMode.Highlight;
            }
            _isHighlightMode = mode == AnnotationToolMode.Highlight;

            UpdateStatusBar();
        }

        private void ShowTextAnnotationDialog(Point location)
        {
            using var form = new Form();
            form.Text = "添加文字注解";
            form.Size = new Size(400, 200);
            form.StartPosition = FormStartPosition.CenterParent;
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.MaximizeBox = false;
            form.MinimizeBox = false;

            var label = new Label();
            label.Text = "请输入文字内容：";
            label.Location = new Point(20, 20);
            label.Size = new Size(150, 20);

            var textBox = new TextBox();
            textBox.Location = new Point(20, 45);
            textBox.Size = new Size(340, 100);
            textBox.Multiline = true;
            textBox.ScrollBars = ScrollBars.Vertical;

            var okButton = new Button();
            okButton.Text = "确定";
            okButton.Location = new Point(220, 130);
            okButton.Size = new Size(70, 30);
            okButton.DialogResult = DialogResult.OK;

            var cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new Point(300, 130);
            cancelButton.Size = new Size(70, 30);
            cancelButton.DialogResult = DialogResult.Cancel;

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrWhiteSpace(textBox.Text))
            {
                try
                {
                    if (_currentPageImage != null && !string.IsNullOrEmpty(_currentPdfPath))
                    {
                        var imgRect = GetImageDisplayRect();
                        float relX = (float)(location.X - imgRect.X) / imgRect.Width;
                        float relY = (float)(location.Y - imgRect.Y) / imgRect.Height;

                        relX = Math.Max(0, Math.Min(1, relX));
                        relY = Math.Max(0, Math.Min(1, relY));

                        var color = Color.Red;
                        _presenter?.AddAnnotationText(relX, relY, textBox.Text, color.ToArgb(), 16f, "Microsoft YaHei UI", _currentPageImage.Width, _currentPageImage.Height);
                        pictureBoxPdf.Invalidate();
                    }
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
            if (buttonDualPage != null)
            {
                buttonDualPage.BackColor = _isDualPage ? Color.FromArgb(230, 244, 255) : Color.White;
                buttonDualPage.FlatAppearance.BorderColor = _isDualPage ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
            }
            UpdateStatusBar();
            if (_isDualPage)
            {
                _presenter?.RenderPage(_currentPageIndex);
            }
            pictureBoxPdf.Invalidate();
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
                if (int.TryParse(textBoxPage.Text, out int page))
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
        }

        private void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            RaiseAiQuestionAsked();
        }

        private void ButtonTranslationToggle_Click(object? sender, EventArgs e)
        {
            _isTranslationEnabled = !_isTranslationEnabled;
            if (buttonTranslationToggle != null)
            {
                buttonTranslationToggle.BackColor = _isTranslationEnabled ? Color.FromArgb(230, 244, 255) : Color.White;
                buttonTranslationToggle.FlatAppearance.BorderColor = _isTranslationEnabled ? Color.FromArgb(64, 150, 255) : Color.FromArgb(217, 217, 217);
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
            if (_isNavPanelDragging && panelNavigation != null)
            {
                int x = panelNavigation.Left + (e.X - _navPanelStartPoint.X);
                int y = panelNavigation.Top + (e.Y - _navPanelStartPoint.Y);
                panelNavigation.Location = new Point(
                    Math.Max(0, Math.Min(x, panelPdf.Width - panelNavigation.Width)),
                    Math.Max(0, Math.Min(y, panelPdf.Height - panelNavigation.Height))
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
                bool isNight = _nightModeManager?.IsNightMode ?? _isNightMode;
                if (isNight)
                {
                    e.Graphics.Clear(Color.FromArgb(20, 20, 20));
                }
                else
                {
                    e.Graphics.Clear(Color.White);
                }

                if (_currentPageImage != null)
                {
                    if (_isDualPage)
                    {
                        DrawDualPageLayout(e.Graphics);
                    }
                    else
                    {
                        var imgRect = GetImageDisplayRect();
                        e.Graphics.DrawImage(_currentPageImage, imgRect);
                    }
                }

                if (_navigationManager != null && _navigationManager.LastSelectionRect.HasValue)
                {
                    var isHighlightMode = _navigationManager.IsHighlightModeCallback?.Invoke() ?? true;
                    var rect = _navigationManager.LastSelectionRect.Value;

                    if (isHighlightMode)
                    {
                        var color = HighlightService.GetHighlightColor(_highlightManager?.CurrentHighlightColor ?? _currentHighlightColor);
                        using var brush = new SolidBrush(Color.FromArgb(color.A, color.R, color.G, color.B));
                        e.Graphics.FillRectangle(brush, rect);
                        using var pen = new Pen(Color.FromArgb(color.A + 50, color.R, color.G, color.B), 2);
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                    else
                    {
                        using var brush = new SolidBrush(Color.FromArgb(80, Color.Yellow));
                        e.Graphics.FillRectangle(brush, rect);
                        using var pen = new Pen(Color.Orange, 2);
                        e.Graphics.DrawRectangle(pen, rect);
                    }
                }

                if (!string.IsNullOrEmpty(_currentPdfPath) && _currentPageImage != null)
                {
                    DrawHighlightsFromLayer(e.Graphics);
                }

                if (_navigationManager != null && _currentPageImage != null)
                {
                    var imgRect = GetImageDisplayRect();
                    _navigationManager.DrawAnnotations(e.Graphics, imgRect);
                }
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

        private void DrawDualPageLayout(Graphics g)
        {
            try
            {
                if (_currentPageImage == null)
                    return;

                int imgWidth = _currentPageImage.Width;
                int imgHeight = _currentPageImage.Height;

                float fitScale = Math.Min(
                    (float)pictureBoxPdf.ClientSize.Width / (imgWidth * 2),
                    (float)pictureBoxPdf.ClientSize.Height / imgHeight);

                float zoomScale = _navigationManager != null ? _navigationManager.ZoomLevel / 100.0f : _zoomLevel / 100.0f;
                float totalScale = fitScale * zoomScale;

                int scaledWidth = (int)(imgWidth * totalScale);
                int scaledHeight = (int)(imgHeight * totalScale);

                int totalWidth = scaledWidth * 2;
                var imageOffset = _navigationManager != null ? _navigationManager.ImageOffset : _imageOffset;
                int offsetX = (pictureBoxPdf.ClientSize.Width - totalWidth) / 2 + imageOffset.X;
                int offsetY = (pictureBoxPdf.ClientSize.Height - scaledHeight) / 2 + imageOffset.Y;

                var leftRect = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
                var rightRect = new Rectangle(offsetX + scaledWidth, offsetY, scaledWidth, scaledHeight);

                g.DrawImage(_currentPageImage, leftRect);

                if (_secondPageImage != null)
                {
                    g.DrawImage(_secondPageImage, rightRect);
                }
                else if (_currentPageImage != null)
                {
                    using var brush = new SolidBrush(Color.White);
                    g.FillRectangle(brush, rightRect);
                }
            }
            catch (ObjectDisposedException)
            {
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
                (float)pictureBoxPdf.ClientSize.Width / (imgWidth * 2),
                (float)pictureBoxPdf.ClientSize.Height / imgHeight);

            float zoomScale = _navigationManager != null ? _navigationManager.ZoomLevel / 100.0f : _zoomLevel / 100.0f;
            float totalScale = fitScale * zoomScale;

            int scaledWidth = (int)(imgWidth * totalScale);
            int scaledHeight = (int)(imgHeight * totalScale);

            int totalWidth = scaledWidth * 2;
            var imageOffset = _navigationManager != null ? _navigationManager.ImageOffset : _imageOffset;
            int offsetX = (pictureBoxPdf.ClientSize.Width - totalWidth) / 2 + imageOffset.X;
            int offsetY = (pictureBoxPdf.ClientSize.Height - scaledHeight) / 2 + imageOffset.Y;

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

        public async void ResetZoom()
        {
            _navigationManager?.ResetZoom();
            UpdateStatusBar();
        }

        private void _loadingIndicator_Click(object? sender, EventArgs e)
        {
        }

        private void TreeViewFiles_AfterSelect(object? sender, TreeViewEventArgs e)
        {
            FileSelected?.Invoke(this, EventArgs.Empty);
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
            components = new Container();
            splitContainerMain = new SplitContainer();
            panelPdf = new Panel();
            panelNavigation = new Panel();
            _toolbarGroupNav = new Panel();
            buttonPrev = new Button();
            textBoxPage = new TextBox();
            labelPageCount = new Label();
            buttonNext = new Button();
            _toolbarGroupView = new Panel();
            buttonZoomOut = new Button();
            trackBarZoom = new TrackBar();
            labelZoom = new Label();
            buttonZoomIn = new Button();
            _buttonResetView = new Button();
            _buttonLockView = new Button();
            _toolbarGroupMode = new Panel();
            buttonNightMode = new Button();
            buttonDualPage = new Button();
            buttonFullscreen = new Button();
            _toolbarGroupTools = new Panel();
            buttonHighlightMode = new Button();
            buttonAskAi = new Button();
            buttonPenMode = new Button();
            buttonTextMode = new Button();
            _loadingIndicator = new LoadingIndicator();
            _statusBar = new Panel();
            _statusLabelLeft = new Label();
            _statusLabelRight = new Label();
            pictureBoxPdf = new PictureBox();
            _ocrPanel = new Panel();
            _ocrPictureBox = new PictureBox();
            _ocrCloseButton = new Button();
            _pageTransitionOverlay = new Panel();
            transitionLabel = new Label();
            panelLeftContainer = new Panel();
            tabControlLeft = new TabControl();
            tabPageFiles = new TabPage();
            treeViewFiles = new TreeView();
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
            _buttonRemoveHighlight = new Button();
            buttonUndoHighlight = new Button();
            _buttonBatchRemoveHighlight = new Button();
            _buttonExportHighlights = new Button();
            _groupBoxBookmarks = new GroupBox();
            _listBoxBookmarks = new ListBox();
            _textBoxBookmarkTitle = new TextBox();
            buttonPanel = new FlowLayoutPanel();
            _buttonAddBookmark = new Button();
            _buttonRemoveBookmark = new Button();
            buttonOpenFolder = new Button();
            buttonTranslationToggle = new Button();
            _pageTransitionTimer = new System.Windows.Forms.Timer(components);

            ((ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            panelPdf.SuspendLayout();
            panelNavigation.SuspendLayout();
            _toolbarGroupNav.SuspendLayout();
            _toolbarGroupView.SuspendLayout();
            ((ISupportInitialize)trackBarZoom).BeginInit();
            _toolbarGroupMode.SuspendLayout();
            _toolbarGroupTools.SuspendLayout();
            _statusBar.SuspendLayout();
            ((ISupportInitialize)pictureBoxPdf).BeginInit();
            _ocrPanel.SuspendLayout();
            ((ISupportInitialize)_ocrPictureBox).BeginInit();
            _pageTransitionOverlay.SuspendLayout();
            panelLeftContainer.SuspendLayout();
            tabControlLeft.SuspendLayout();
            tabPageFiles.SuspendLayout();
            tabPageThumbnails.SuspendLayout();
            panelThumbnails.SuspendLayout();
            tabPageTranslate.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            _tabPageBookmarksAndHighlights.SuspendLayout();
            _groupBoxHighlights.SuspendLayout();
            groupBoxHighlightColor.SuspendLayout();
            highlightButtonPanel.SuspendLayout();
            _groupBoxBookmarks.SuspendLayout();
            buttonPanel.SuspendLayout();
            SuspendLayout();
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel2;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(panelPdf);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelLeftContainer);
            splitContainerMain.Size = new Size(1400, 900);
            splitContainerMain.SplitterDistance = 1051;
            // 
            // panelPdf
            // 
            panelPdf.BackColor = Color.FromArgb(240, 240, 240);
            panelPdf.Controls.Add(panelNavigation);
            panelPdf.Controls.Add(_statusBar);
            panelPdf.Controls.Add(pictureBoxPdf);
            panelPdf.Controls.Add(_ocrPanel);
            panelPdf.Controls.Add(_pageTransitionOverlay);
            panelPdf.Dock = DockStyle.Fill;
            panelPdf.Location = new Point(0, 0);
            panelPdf.Name = "panelPdf";
            panelPdf.Size = new Size(1051, 900);
            panelPdf.TabIndex = 1;
            // 
            // panelNavigation
            // 
            panelNavigation.BackColor = Color.White;
            panelNavigation.Controls.Add(_toolbarGroupNav);
            panelNavigation.Controls.Add(_toolbarGroupView);
            panelNavigation.Controls.Add(_toolbarGroupMode);
            panelNavigation.Controls.Add(_toolbarGroupTools);
            panelNavigation.Controls.Add(_loadingIndicator);
            panelNavigation.Dock = DockStyle.Top;
            panelNavigation.Location = new Point(0, 0);
            panelNavigation.Name = "panelNavigation";
            panelNavigation.Padding = new Padding(8);
            panelNavigation.Size = new Size(1051, 52);
            panelNavigation.TabIndex = 3;
            panelNavigation.MouseDown += PanelNavigation_MouseDown;
            panelNavigation.MouseMove += PanelNavigation_MouseMove;
            panelNavigation.MouseUp += PanelNavigation_MouseUp;
            // 
            // _toolbarGroupNav
            // 
            _toolbarGroupNav.Controls.Add(buttonPrev);
            _toolbarGroupNav.Controls.Add(textBoxPage);
            _toolbarGroupNav.Controls.Add(labelPageCount);
            _toolbarGroupNav.Controls.Add(buttonNext);
            _toolbarGroupNav.Dock = DockStyle.Left;
            _toolbarGroupNav.Location = new Point(578, 8);
            _toolbarGroupNav.Name = "_toolbarGroupNav";
            _toolbarGroupNav.Size = new Size(150, 36);
            _toolbarGroupNav.TabIndex = 0;
            // 
            // buttonPrev
            // 
            buttonPrev.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonPrev.FlatStyle = FlatStyle.Flat;
            buttonPrev.Font = new Font("Microsoft YaHei UI", 10F);
            buttonPrev.Location = new Point(0, 2);
            buttonPrev.Name = "buttonPrev";
            buttonPrev.Size = new Size(32, 32);
            buttonPrev.TabIndex = 0;
            buttonPrev.Text = "◀";
            buttonPrev.UseVisualStyleBackColor = false;
            buttonPrev.Click += ButtonPrev_Click;
            // 
            // textBoxPage
            // 
            textBoxPage.BorderStyle = BorderStyle.FixedSingle;
            textBoxPage.Font = new Font("Microsoft YaHei UI", 10F);
            textBoxPage.Location = new Point(38, 5);
            textBoxPage.Name = "textBoxPage";
            textBoxPage.Size = new Size(40, 24);
            textBoxPage.TabIndex = 1;
            textBoxPage.Text = "1";
            textBoxPage.TextAlign = HorizontalAlignment.Center;
            textBoxPage.KeyDown += TextBoxPage_KeyDown;
            // 
            // labelPageCount
            // 
            labelPageCount.AutoSize = true;
            labelPageCount.Font = new Font("Microsoft YaHei UI", 10F);
            labelPageCount.ForeColor = Color.FromArgb(102, 102, 102);
            labelPageCount.Location = new Point(83, 8);
            labelPageCount.Name = "labelPageCount";
            labelPageCount.Size = new Size(27, 20);
            labelPageCount.TabIndex = 2;
            labelPageCount.Text = "/ 1";
            // 
            // buttonNext
            // 
            buttonNext.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonNext.FlatStyle = FlatStyle.Flat;
            buttonNext.Font = new Font("Microsoft YaHei UI", 10F);
            buttonNext.Location = new Point(115, 2);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(32, 32);
            buttonNext.TabIndex = 3;
            buttonNext.Text = "▶";
            buttonNext.UseVisualStyleBackColor = false;
            buttonNext.Click += ButtonNext_Click;
            // 
            // _toolbarGroupView
            // 
            _toolbarGroupView.Controls.Add(buttonZoomOut);
            _toolbarGroupView.Controls.Add(trackBarZoom);
            _toolbarGroupView.Controls.Add(labelZoom);
            _toolbarGroupView.Controls.Add(buttonZoomIn);
            _toolbarGroupView.Controls.Add(_buttonResetView);
            _toolbarGroupView.Controls.Add(_buttonLockView);
            _toolbarGroupView.Dock = DockStyle.Left;
            _toolbarGroupView.Location = new Point(288, 8);
            _toolbarGroupView.Name = "_toolbarGroupView";
            _toolbarGroupView.Size = new Size(290, 36);
            _toolbarGroupView.TabIndex = 1;
            // 
            // buttonZoomOut
            // 
            buttonZoomOut.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonZoomOut.FlatStyle = FlatStyle.Flat;
            buttonZoomOut.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonZoomOut.Location = new Point(10, 2);
            buttonZoomOut.Name = "buttonZoomOut";
            buttonZoomOut.Size = new Size(28, 32);
            buttonZoomOut.TabIndex = 0;
            buttonZoomOut.Text = "−";
            buttonZoomOut.UseVisualStyleBackColor = false;
            buttonZoomOut.Click += ButtonZoomOut_Click;
            // 
            // trackBarZoom
            // 
            trackBarZoom.Location = new Point(44, 4);
            trackBarZoom.Maximum = 200;
            trackBarZoom.Minimum = 50;
            trackBarZoom.Name = "trackBarZoom";
            trackBarZoom.Size = new Size(100, 45);
            trackBarZoom.TabIndex = 1;
            trackBarZoom.Value = 100;
            trackBarZoom.Scroll += TrackBarZoom_Scroll;
            // 
            // labelZoom
            // 
            labelZoom.AutoSize = true;
            labelZoom.Font = new Font("Microsoft YaHei UI", 9F);
            labelZoom.ForeColor = Color.FromArgb(102, 102, 102);
            labelZoom.Location = new Point(148, 8);
            labelZoom.Name = "labelZoom";
            labelZoom.Size = new Size(40, 17);
            labelZoom.TabIndex = 2;
            labelZoom.Text = "100%";
            // 
            // buttonZoomIn
            // 
            buttonZoomIn.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonZoomIn.FlatStyle = FlatStyle.Flat;
            buttonZoomIn.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonZoomIn.Location = new Point(192, 2);
            buttonZoomIn.Name = "buttonZoomIn";
            buttonZoomIn.Size = new Size(28, 32);
            buttonZoomIn.TabIndex = 3;
            buttonZoomIn.Text = "+";
            buttonZoomIn.UseVisualStyleBackColor = false;
            buttonZoomIn.Click += ButtonZoomIn_Click;
            // 
            // _buttonResetView
            // 
            _buttonResetView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonResetView.FlatStyle = FlatStyle.Flat;
            _buttonResetView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonResetView.Location = new Point(226, 2);
            _buttonResetView.Name = "_buttonResetView";
            _buttonResetView.Size = new Size(28, 32);
            _buttonResetView.TabIndex = 4;
            _buttonResetView.Text = "↺";
            _buttonResetView.UseVisualStyleBackColor = false;
            _buttonResetView.Click += ButtonResetView_Click;
            // 
            // _buttonLockView
            // 
            _buttonLockView.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonLockView.FlatStyle = FlatStyle.Flat;
            _buttonLockView.Font = new Font("Microsoft YaHei UI", 10F);
            _buttonLockView.Location = new Point(258, 2);
            _buttonLockView.Name = "_buttonLockView";
            _buttonLockView.Size = new Size(28, 32);
            _buttonLockView.TabIndex = 5;
            _buttonLockView.Text = "🔓";
            _buttonLockView.UseVisualStyleBackColor = false;
            _buttonLockView.Click += ButtonLockView_Click;
            // 
            // _toolbarGroupMode
            // 
            _toolbarGroupMode.Controls.Add(buttonNightMode);
            _toolbarGroupMode.Controls.Add(buttonDualPage);
            _toolbarGroupMode.Controls.Add(buttonFullscreen);
            _toolbarGroupMode.Dock = DockStyle.Left;
            _toolbarGroupMode.Location = new Point(158, 8);
            _toolbarGroupMode.Name = "_toolbarGroupMode";
            _toolbarGroupMode.Size = new Size(130, 36);
            _toolbarGroupMode.TabIndex = 2;
            // 
            // buttonNightMode
            // 
            buttonNightMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonNightMode.FlatStyle = FlatStyle.Flat;
            buttonNightMode.Font = new Font("Microsoft YaHei UI", 12F);
            buttonNightMode.Location = new Point(10, 2);
            buttonNightMode.Name = "buttonNightMode";
            buttonNightMode.Size = new Size(32, 32);
            buttonNightMode.TabIndex = 0;
            buttonNightMode.Text = "🌙";
            buttonNightMode.UseVisualStyleBackColor = false;
            buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // buttonDualPage
            // 
            buttonDualPage.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonDualPage.FlatStyle = FlatStyle.Flat;
            buttonDualPage.Font = new Font("Microsoft YaHei UI", 10F);
            buttonDualPage.Location = new Point(48, 2);
            buttonDualPage.Name = "buttonDualPage";
            buttonDualPage.Size = new Size(32, 32);
            buttonDualPage.TabIndex = 1;
            buttonDualPage.Text = "📖";
            buttonDualPage.UseVisualStyleBackColor = false;
            buttonDualPage.Click += ButtonDualPage_Click;
            // 
            // buttonFullscreen
            // 
            buttonFullscreen.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonFullscreen.FlatStyle = FlatStyle.Flat;
            buttonFullscreen.Font = new Font("Microsoft YaHei UI", 10F);
            buttonFullscreen.Location = new Point(86, 2);
            buttonFullscreen.Name = "buttonFullscreen";
            buttonFullscreen.Size = new Size(32, 32);
            buttonFullscreen.TabIndex = 2;
            buttonFullscreen.Text = "⛶";
            buttonFullscreen.UseVisualStyleBackColor = false;
            buttonFullscreen.Click += ButtonFullscreen_Click;
            // 
            // _toolbarGroupTools
            // 
            _toolbarGroupTools.Controls.Add(buttonHighlightMode);
            _toolbarGroupTools.Controls.Add(buttonPenMode);
            _toolbarGroupTools.Controls.Add(buttonTextMode);
            _toolbarGroupTools.Controls.Add(buttonAskAi);
            _toolbarGroupTools.Controls.Add(buttonOpenFolder);
            _toolbarGroupTools.Dock = DockStyle.Left;
            _toolbarGroupTools.Location = new Point(8, 8);
            _toolbarGroupTools.Name = "_toolbarGroupTools";
            _toolbarGroupTools.Size = new Size(220, 36);
            _toolbarGroupTools.TabIndex = 3;
            // 
            // buttonHighlightMode
            // 
            buttonHighlightMode.BackColor = Color.FromArgb(230, 244, 255);
            buttonHighlightMode.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            buttonHighlightMode.FlatStyle = FlatStyle.Flat;
            buttonHighlightMode.Font = new Font("Microsoft YaHei UI", 10F);
            buttonHighlightMode.Location = new Point(10, 2);
            buttonHighlightMode.Name = "buttonHighlightMode";
            buttonHighlightMode.Size = new Size(32, 32);
            buttonHighlightMode.TabIndex = 0;
            buttonHighlightMode.Text = "🖍️";
            buttonHighlightMode.UseVisualStyleBackColor = false;
            buttonHighlightMode.Click += ButtonHighlightMode_Click;
            // 
            // buttonPenMode
            // 
            buttonPenMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonPenMode.FlatStyle = FlatStyle.Flat;
            buttonPenMode.Font = new Font("Microsoft YaHei UI", 10F);
            buttonPenMode.Location = new Point(50, 2);
            buttonPenMode.Name = "buttonPenMode";
            buttonPenMode.Size = new Size(32, 32);
            buttonPenMode.TabIndex = 3;
            buttonPenMode.Text = "✏️";
            buttonPenMode.UseVisualStyleBackColor = false;
            buttonPenMode.Click += ButtonPenMode_Click;
            // 
            // buttonTextMode
            // 
            buttonTextMode.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonTextMode.FlatStyle = FlatStyle.Flat;
            buttonTextMode.Font = new Font("Microsoft YaHei UI", 10F);
            buttonTextMode.Location = new Point(90, 2);
            buttonTextMode.Name = "buttonTextMode";
            buttonTextMode.Size = new Size(32, 32);
            buttonTextMode.TabIndex = 4;
            buttonTextMode.Text = "📝";
            buttonTextMode.UseVisualStyleBackColor = false;
            buttonTextMode.Click += ButtonTextMode_Click;
            // 
            // buttonAskAi
            // 
            buttonAskAi.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonAskAi.FlatStyle = FlatStyle.Flat;
            buttonAskAi.Font = new Font("Microsoft YaHei UI", 10F);
            buttonAskAi.Location = new Point(130, 2);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(32, 32);
            buttonAskAi.TabIndex = 1;
            buttonAskAi.Text = "🤖";
            buttonAskAi.UseVisualStyleBackColor = false;
            buttonAskAi.Click += ButtonAskAi_Click;
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
            _statusBar.Size = new Size(1051, 24);
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
            _statusLabelRight.Location = new Point(901, 4);
            _statusLabelRight.Name = "_statusLabelRight";
            _statusLabelRight.Size = new Size(130, 17);
            _statusLabelRight.TabIndex = 1;
            _statusLabelRight.Text = "缩放: 100% · 高亮模式";
            _statusLabelRight.TextAlign = ContentAlignment.MiddleRight;
            // 
            // pictureBoxPdf
            // 
            pictureBoxPdf.Dock = DockStyle.Fill;
            pictureBoxPdf.Location = new Point(0, 0);
            pictureBoxPdf.Name = "pictureBoxPdf";
            pictureBoxPdf.Size = new Size(1051, 900);
            pictureBoxPdf.TabIndex = 1;
            pictureBoxPdf.TabStop = false;
            pictureBoxPdf.Paint += PictureBoxPdf_Paint;
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
            _pageTransitionOverlay.Controls.Add(transitionLabel);
            _pageTransitionOverlay.Dock = DockStyle.Fill;
            _pageTransitionOverlay.Location = new Point(0, 0);
            _pageTransitionOverlay.Name = "_pageTransitionOverlay";
            _pageTransitionOverlay.Size = new Size(1051, 900);
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
            transitionLabel.Size = new Size(1051, 900);
            transitionLabel.TabIndex = 0;
            transitionLabel.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panelLeftContainer
            // 
            panelLeftContainer.BackColor = Color.White;
            panelLeftContainer.Controls.Add(tabControlLeft);
            panelLeftContainer.Dock = DockStyle.Fill;
            panelLeftContainer.Location = new Point(0, 0);
            panelLeftContainer.Name = "panelLeftContainer";
            panelLeftContainer.Size = new Size(345, 900);
            panelLeftContainer.TabIndex = 0;
            // 
            // tabControlLeft
            // 
            tabControlLeft.Controls.Add(tabPageFiles);
            tabControlLeft.Controls.Add(tabPageThumbnails);
            tabControlLeft.Controls.Add(tabPageTranslate);
            tabControlLeft.Controls.Add(_tabPageBookmarksAndHighlights);
            tabControlLeft.Dock = DockStyle.Fill;
            tabControlLeft.Font = new Font("Microsoft YaHei UI", 9F);
            tabControlLeft.Location = new Point(0, 0);
            tabControlLeft.Name = "tabControlLeft";
            tabControlLeft.SelectedIndex = 0;
            tabControlLeft.Size = new Size(345, 900);
            tabControlLeft.TabIndex = 0;
            // 
            // tabPageFiles
            // 
            tabPageFiles.Controls.Add(treeViewFiles);
            tabPageFiles.Location = new Point(4, 26);
            tabPageFiles.Name = "tabPageFiles";
            tabPageFiles.Padding = new Padding(3);
            tabPageFiles.Size = new Size(337, 834);
            tabPageFiles.TabIndex = 2;
            tabPageFiles.Text = "📁 文件";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // treeViewFiles
            // 
            treeViewFiles.BorderStyle = BorderStyle.None;
            treeViewFiles.Dock = DockStyle.Fill;
            treeViewFiles.Location = new Point(3, 3);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(331, 828);
            treeViewFiles.TabIndex = 0;
            treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // tabPageThumbnails
            // 
            tabPageThumbnails.Controls.Add(panelThumbnails);
            tabPageThumbnails.Location = new Point(4, 26);
            tabPageThumbnails.Name = "tabPageThumbnails";
            tabPageThumbnails.Padding = new Padding(3);
            tabPageThumbnails.Size = new Size(338, 834);
            tabPageThumbnails.TabIndex = 1;
            tabPageThumbnails.Text = "🖼️ 缩略图";
            tabPageThumbnails.UseVisualStyleBackColor = true;
            // 
            // panelThumbnails
            // 
            panelThumbnails.AutoScroll = true;
            panelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            panelThumbnails.Controls.Add(flowLayoutPanelThumbnails);
            panelThumbnails.Dock = DockStyle.Fill;
            panelThumbnails.Location = new Point(3, 3);
            panelThumbnails.Name = "panelThumbnails";
            panelThumbnails.Size = new Size(332, 828);
            panelThumbnails.TabIndex = 0;
            // 
            // flowLayoutPanelThumbnails
            // 
            flowLayoutPanelThumbnails.AutoScroll = true;
            flowLayoutPanelThumbnails.BackColor = Color.FromArgb(245, 245, 245);
            flowLayoutPanelThumbnails.Dock = DockStyle.Fill;
            flowLayoutPanelThumbnails.Location = new Point(0, 0);
            flowLayoutPanelThumbnails.Name = "flowLayoutPanelThumbnails";
            flowLayoutPanelThumbnails.Size = new Size(332, 828);
            flowLayoutPanelThumbnails.TabIndex = 0;
            // 
            // tabPageTranslate
            // 
            tabPageTranslate.Controls.Add(groupBoxProgress);
            tabPageTranslate.Location = new Point(4, 26);
            tabPageTranslate.Name = "tabPageTranslate";
            tabPageTranslate.Padding = new Padding(3);
            tabPageTranslate.Size = new Size(338, 834);
            tabPageTranslate.TabIndex = 0;
            tabPageTranslate.Text = "🌐 翻译";
            tabPageTranslate.UseVisualStyleBackColor = true;
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
            groupBoxProgress.Dock = DockStyle.Fill;
            groupBoxProgress.Font = new Font("Microsoft YaHei UI", 9F);
            groupBoxProgress.Location = new Point(3, 3);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Size = new Size(332, 828);
            groupBoxProgress.TabIndex = 0;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "OCR / 翻译";
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxTranslation.BorderStyle = BorderStyle.FixedSingle;
            textBoxTranslation.Location = new Point(10, 320);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(310, 350);
            textBoxTranslation.TabIndex = 7;
            // 
            // buttonSpeakTranslation
            // 
            buttonSpeakTranslation.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonSpeakTranslation.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonSpeakTranslation.FlatStyle = FlatStyle.Flat;
            buttonSpeakTranslation.Location = new Point(10, 680);
            buttonSpeakTranslation.Name = "buttonSpeakTranslation";
            buttonSpeakTranslation.Size = new Size(75, 28);
            buttonSpeakTranslation.TabIndex = 6;
            buttonSpeakTranslation.Text = "🔊 朗读";
            buttonSpeakTranslation.UseVisualStyleBackColor = false;
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // buttonAddToLearningContent
            // 
            buttonAddToLearningContent.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonAddToLearningContent.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonAddToLearningContent.FlatStyle = FlatStyle.Flat;
            buttonAddToLearningContent.Location = new Point(180, 680);
            buttonAddToLearningContent.Name = "buttonAddToLearningContent";
            buttonAddToLearningContent.Size = new Size(140, 28);
            buttonAddToLearningContent.TabIndex = 5;
            buttonAddToLearningContent.Text = "➕ 添加到学习";
            buttonAddToLearningContent.UseVisualStyleBackColor = false;
            buttonAddToLearningContent.Click += ButtonAddToLearningContent_Click;
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOriginal.BorderStyle = BorderStyle.FixedSingle;
            textBoxOriginal.Location = new Point(10, 50);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(310, 220);
            textBoxOriginal.TabIndex = 4;
            // 
            // buttonSpeakOriginal
            // 
            buttonSpeakOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSpeakOriginal.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonSpeakOriginal.FlatStyle = FlatStyle.Flat;
            buttonSpeakOriginal.Location = new Point(245, 20);
            buttonSpeakOriginal.Name = "buttonSpeakOriginal";
            buttonSpeakOriginal.Size = new Size(75, 25);
            buttonSpeakOriginal.TabIndex = 3;
            buttonSpeakOriginal.Text = "🔊 朗读";
            buttonSpeakOriginal.UseVisualStyleBackColor = false;
            buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;
            // 
            // labelTranslation
            // 
            labelTranslation.AutoSize = true;
            labelTranslation.Location = new Point(10, 295);
            labelTranslation.Name = "labelTranslation";
            labelTranslation.Size = new Size(44, 17);
            labelTranslation.TabIndex = 2;
            labelTranslation.Text = "翻译：";
            // 
            // labelOriginal
            // 
            labelOriginal.AutoSize = true;
            labelOriginal.Location = new Point(10, 25);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(44, 17);
            labelOriginal.TabIndex = 1;
            labelOriginal.Text = "原文：";
            // 
            // buttonTranslate
            // 
            buttonTranslate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonTranslate.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonTranslate.FlatStyle = FlatStyle.Flat;
            buttonTranslate.Location = new Point(160, 275);
            buttonTranslate.Name = "buttonTranslate";
            buttonTranslate.Size = new Size(160, 30);
            buttonTranslate.TabIndex = 0;
            buttonTranslate.Text = "🌐 翻译选中内容";
            buttonTranslate.UseVisualStyleBackColor = false;
            buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // _tabPageBookmarksAndHighlights
            // 
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxHighlights);
            _tabPageBookmarksAndHighlights.Controls.Add(_groupBoxBookmarks);
            _tabPageBookmarksAndHighlights.Location = new Point(4, 26);
            _tabPageBookmarksAndHighlights.Name = "_tabPageBookmarksAndHighlights";
            _tabPageBookmarksAndHighlights.Padding = new Padding(3);
            _tabPageBookmarksAndHighlights.Size = new Size(338, 834);
            _tabPageBookmarksAndHighlights.TabIndex = 3;
            _tabPageBookmarksAndHighlights.Text = "📑 书签/高亮";
            _tabPageBookmarksAndHighlights.UseVisualStyleBackColor = true;
            // 
            // _groupBoxHighlights
            // 
            _groupBoxHighlights.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _groupBoxHighlights.Controls.Add(groupBoxHighlightColor);
            _groupBoxHighlights.Controls.Add(_listBoxHighlights);
            _groupBoxHighlights.Controls.Add(highlightButtonPanel);
            _groupBoxHighlights.Location = new Point(10, 10);
            _groupBoxHighlights.Name = "_groupBoxHighlights";
            _groupBoxHighlights.Size = new Size(318, 380);
            _groupBoxHighlights.TabIndex = 0;
            _groupBoxHighlights.TabStop = false;
            _groupBoxHighlights.Text = "高亮";
            // 
            // groupBoxHighlightColor
            // 
            groupBoxHighlightColor.Controls.Add(radioHighlightYellow);
            groupBoxHighlightColor.Controls.Add(radioHighlightGreen);
            groupBoxHighlightColor.Controls.Add(radioHighlightBlue);
            groupBoxHighlightColor.Controls.Add(radioHighlightPink);
            groupBoxHighlightColor.Controls.Add(radioHighlightOrange);
            groupBoxHighlightColor.Location = new Point(10, 25);
            groupBoxHighlightColor.Name = "groupBoxHighlightColor";
            groupBoxHighlightColor.Size = new Size(298, 75);
            groupBoxHighlightColor.TabIndex = 0;
            groupBoxHighlightColor.TabStop = false;
            groupBoxHighlightColor.Text = "颜色";
            // 
            // radioHighlightYellow
            // 
            radioHighlightYellow.Appearance = Appearance.Button;
            radioHighlightYellow.BackColor = Color.Yellow;
            radioHighlightYellow.Checked = true;
            radioHighlightYellow.FlatAppearance.BorderColor = Color.FromArgb(64, 150, 255);
            radioHighlightYellow.FlatAppearance.BorderSize = 2;
            radioHighlightYellow.FlatAppearance.CheckedBackColor = Color.Yellow;
            radioHighlightYellow.FlatStyle = FlatStyle.Flat;
            radioHighlightYellow.Location = new Point(15, 20);
            radioHighlightYellow.Name = "radioHighlightYellow";
            radioHighlightYellow.Size = new Size(36, 28);
            radioHighlightYellow.TabIndex = 0;
            radioHighlightYellow.TabStop = true;
            radioHighlightYellow.Tag = 1;
            radioHighlightYellow.UseVisualStyleBackColor = false;
            radioHighlightYellow.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightGreen
            // 
            radioHighlightGreen.Appearance = Appearance.Button;
            radioHighlightGreen.BackColor = Color.LimeGreen;
            radioHighlightGreen.FlatAppearance.BorderSize = 0;
            radioHighlightGreen.FlatAppearance.CheckedBackColor = Color.LimeGreen;
            radioHighlightGreen.FlatStyle = FlatStyle.Flat;
            radioHighlightGreen.Location = new Point(60, 20);
            radioHighlightGreen.Name = "radioHighlightGreen";
            radioHighlightGreen.Size = new Size(36, 28);
            radioHighlightGreen.TabIndex = 1;
            radioHighlightGreen.Tag = 2;
            radioHighlightGreen.UseVisualStyleBackColor = false;
            radioHighlightGreen.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightBlue
            // 
            radioHighlightBlue.Appearance = Appearance.Button;
            radioHighlightBlue.BackColor = Color.LightBlue;
            radioHighlightBlue.FlatAppearance.BorderSize = 0;
            radioHighlightBlue.FlatAppearance.CheckedBackColor = Color.LightBlue;
            radioHighlightBlue.FlatStyle = FlatStyle.Flat;
            radioHighlightBlue.Location = new Point(105, 20);
            radioHighlightBlue.Name = "radioHighlightBlue";
            radioHighlightBlue.Size = new Size(36, 28);
            radioHighlightBlue.TabIndex = 2;
            radioHighlightBlue.Tag = 3;
            radioHighlightBlue.UseVisualStyleBackColor = false;
            radioHighlightBlue.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightPink
            // 
            radioHighlightPink.Appearance = Appearance.Button;
            radioHighlightPink.BackColor = Color.Pink;
            radioHighlightPink.FlatAppearance.BorderSize = 0;
            radioHighlightPink.FlatAppearance.CheckedBackColor = Color.Pink;
            radioHighlightPink.FlatStyle = FlatStyle.Flat;
            radioHighlightPink.Location = new Point(150, 20);
            radioHighlightPink.Name = "radioHighlightPink";
            radioHighlightPink.Size = new Size(36, 28);
            radioHighlightPink.TabIndex = 3;
            radioHighlightPink.Tag = 4;
            radioHighlightPink.UseVisualStyleBackColor = false;
            radioHighlightPink.CheckedChanged += RadioHighlightColor_CheckedChanged;
            // 
            // radioHighlightOrange
            // 
            radioHighlightOrange.Appearance = Appearance.Button;
            radioHighlightOrange.BackColor = Color.Orange;
            radioHighlightOrange.FlatAppearance.BorderSize = 0;
            radioHighlightOrange.FlatAppearance.CheckedBackColor = Color.Orange;
            radioHighlightOrange.FlatStyle = FlatStyle.Flat;
            radioHighlightOrange.Location = new Point(195, 20);
            radioHighlightOrange.Name = "radioHighlightOrange";
            radioHighlightOrange.Size = new Size(36, 28);
            radioHighlightOrange.TabIndex = 4;
            radioHighlightOrange.Tag = 5;
            radioHighlightOrange.UseVisualStyleBackColor = false;
            radioHighlightOrange.CheckedChanged += RadioHighlightColor_CheckedChanged;
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
            // highlightButtonPanel
            // 
            highlightButtonPanel.Controls.Add(_buttonRemoveHighlight);
            highlightButtonPanel.Controls.Add(buttonUndoHighlight);
            highlightButtonPanel.Controls.Add(_buttonBatchRemoveHighlight);
            highlightButtonPanel.Controls.Add(_buttonExportHighlights);
            highlightButtonPanel.Location = new Point(10, 315);
            highlightButtonPanel.Name = "highlightButtonPanel";
            highlightButtonPanel.Size = new Size(298, 50);
            highlightButtonPanel.TabIndex = 2;
            // 
            // _buttonRemoveHighlight
            // 
            _buttonRemoveHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonRemoveHighlight.FlatStyle = FlatStyle.Flat;
            _buttonRemoveHighlight.Location = new Point(3, 3);
            _buttonRemoveHighlight.Name = "_buttonRemoveHighlight";
            _buttonRemoveHighlight.Size = new Size(60, 28);
            _buttonRemoveHighlight.TabIndex = 2;
            _buttonRemoveHighlight.Text = "删除";
            _buttonRemoveHighlight.UseVisualStyleBackColor = false;
            _buttonRemoveHighlight.Click += ButtonRemoveHighlight_Click;
            // 
            // buttonUndoHighlight
            // 
            buttonUndoHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonUndoHighlight.FlatStyle = FlatStyle.Flat;
            buttonUndoHighlight.Location = new Point(69, 3);
            buttonUndoHighlight.Name = "buttonUndoHighlight";
            buttonUndoHighlight.Size = new Size(60, 28);
            buttonUndoHighlight.TabIndex = 1;
            buttonUndoHighlight.Text = "撤销";
            buttonUndoHighlight.UseVisualStyleBackColor = false;
            buttonUndoHighlight.Click += ButtonUndoHighlight_Click;
            // 
            // _buttonBatchRemoveHighlight
            // 
            _buttonBatchRemoveHighlight.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonBatchRemoveHighlight.FlatStyle = FlatStyle.Flat;
            _buttonBatchRemoveHighlight.Location = new Point(135, 3);
            _buttonBatchRemoveHighlight.Name = "_buttonBatchRemoveHighlight";
            _buttonBatchRemoveHighlight.Size = new Size(70, 28);
            _buttonBatchRemoveHighlight.TabIndex = 0;
            _buttonBatchRemoveHighlight.Text = "批量删除";
            _buttonBatchRemoveHighlight.UseVisualStyleBackColor = false;
            _buttonBatchRemoveHighlight.Click += ButtonBatchRemoveHighlight_Click;
            // 
            // _buttonExportHighlights
            // 
            _buttonExportHighlights.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            _buttonExportHighlights.FlatStyle = FlatStyle.Flat;
            _buttonExportHighlights.Location = new Point(211, 3);
            _buttonExportHighlights.Name = "_buttonExportHighlights";
            _buttonExportHighlights.Size = new Size(75, 28);
            _buttonExportHighlights.TabIndex = 3;
            _buttonExportHighlights.Text = "导出";
            _buttonExportHighlights.UseVisualStyleBackColor = false;
            _buttonExportHighlights.Click += ButtonExportHighlights_Click;
            // 
            // _groupBoxBookmarks
            // 
            _groupBoxBookmarks.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _groupBoxBookmarks.Controls.Add(_listBoxBookmarks);
            _groupBoxBookmarks.Controls.Add(_textBoxBookmarkTitle);
            _groupBoxBookmarks.Controls.Add(buttonPanel);
            _groupBoxBookmarks.Location = new Point(10, 400);
            _groupBoxBookmarks.Name = "_groupBoxBookmarks";
            _groupBoxBookmarks.Size = new Size(318, 380);
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
            // buttonPanel
            // 
            buttonPanel.Controls.Add(_buttonAddBookmark);
            buttonPanel.Controls.Add(_buttonRemoveBookmark);
            buttonPanel.Location = new Point(216, 18);
            buttonPanel.Name = "buttonPanel";
            buttonPanel.Size = new Size(92, 30);
            buttonPanel.TabIndex = 2;
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
            // buttonOpenFolder
            // 
            buttonOpenFolder.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonOpenFolder.FlatStyle = FlatStyle.Flat;
            buttonOpenFolder.Font = new Font("Microsoft YaHei UI", 10F);
            buttonOpenFolder.Location = new Point(170, 2);
            buttonOpenFolder.Name = "buttonOpenFolder";
            buttonOpenFolder.Size = new Size(32, 32);
            buttonOpenFolder.TabIndex = 5;
            buttonOpenFolder.Text = "📂";
            buttonOpenFolder.UseVisualStyleBackColor = false;
            buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // buttonTranslationToggle
            // 
            buttonTranslationToggle.FlatAppearance.BorderColor = Color.FromArgb(217, 217, 217);
            buttonTranslationToggle.FlatStyle = FlatStyle.Flat;
            buttonTranslationToggle.Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold);
            buttonTranslationToggle.Location = new Point(48, 2);
            buttonTranslationToggle.Name = "buttonTranslationToggle";
            buttonTranslationToggle.Size = new Size(32, 32);
            buttonTranslationToggle.TabIndex = 1;
            buttonTranslationToggle.Text = "译";
            buttonTranslationToggle.UseVisualStyleBackColor = false;
            buttonTranslationToggle.Click += ButtonTranslationToggle_Click;
            // 

            // 
            // PdfReaderFormV2
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1400, 900);
            Controls.Add(splitContainerMain);
            Name = "PdfReaderFormV2";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "学习助手 - PDF 阅读器 (优化版)";
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            panelPdf.ResumeLayout(false);
            panelNavigation.ResumeLayout(false);
            _toolbarGroupNav.ResumeLayout(false);
            _toolbarGroupNav.PerformLayout();
            _toolbarGroupView.ResumeLayout(false);
            _toolbarGroupView.PerformLayout();
            ((ISupportInitialize)trackBarZoom).EndInit();
            _toolbarGroupMode.ResumeLayout(false);
            _toolbarGroupTools.ResumeLayout(false);
            _statusBar.ResumeLayout(false);
            _statusBar.PerformLayout();
            ((ISupportInitialize)pictureBoxPdf).EndInit();
            _ocrPanel.ResumeLayout(false);
            ((ISupportInitialize)_ocrPictureBox).EndInit();
            _pageTransitionOverlay.ResumeLayout(false);
            panelLeftContainer.ResumeLayout(false);
            tabControlLeft.ResumeLayout(false);
            tabPageFiles.ResumeLayout(false);
            tabPageThumbnails.ResumeLayout(false);
            panelThumbnails.ResumeLayout(false);
            tabPageTranslate.ResumeLayout(false);
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
            _tabPageBookmarksAndHighlights.ResumeLayout(false);
            _groupBoxHighlights.ResumeLayout(false);
            groupBoxHighlightColor.ResumeLayout(false);
            highlightButtonPanel.ResumeLayout(false);
            _groupBoxBookmarks.ResumeLayout(false);
            _groupBoxBookmarks.PerformLayout();
            buttonPanel.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;
                _pen.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        public void SetCurrentLanguage(string language)
        {
            throw new NotImplementedException();
        }

        public void UpdateLanguageButtonText(string text)
        {
            throw new NotImplementedException();
        }

        public string GetCurrentLanguage()
        {
            throw new NotImplementedException();
        }
    }
}
