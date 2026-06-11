using LearningAssistant.Common;
using LearningAssistant.Models;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Presenters;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Views;
using LearningAssistant.Views.UI;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{

    public partial class PdfReaderForm : Form, IPdfView
    {
        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderForm> _logger;
        private readonly BookmarkService _bookmarkService;
        private readonly HighlightService _highlightService;
        private int _zoomLevel = 100;
        private bool _isSelecting = false;
        private bool _isDrawing = false;
        private bool _isDragging = false;
        private bool _isLocked = false;
        private Button? _buttonLockView;
        private Button? _buttonResetView;
        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private Point _dragStart = Point.Empty;
        private Point _imageOffset = Point.Empty;
        private Rectangle? _lastSelectionRect = null;
        private readonly Pen _pen = new Pen(Color.Red, 4f);
        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;
        private List<PointF>? _currentStrokePoints;
        private Bitmap? _highlightBitmap;
        private Graphics? _highlightGraphics;
        private readonly Stack<HighlightUndoAction> _highlightUndoStack = new Stack<HighlightUndoAction>();
        private bool _disposed = false;

        private Panel? _ocrPanel;
        private PictureBox? _ocrPictureBox;
        private Button? _ocrCloseButton;
        private bool _isOcrPanelDragging = false;
        private Point _ocrPanelStartPoint = Point.Empty;
        private bool _isDoubleClickPending = false;
        private DateTime _lastClickTime = DateTime.MinValue;
        private Point _lastClickLocation = Point.Empty;
        private const int DoubleClickTime_ms = 200;
        private const int DoubleClickDistance = 5;

        // 长按拖动相关
        private System.Windows.Forms.Timer? _longPressTimer;
        private bool _isLongPressPending = false;
        private Point _longPressStartLocation = Point.Empty;
        private const int LongPressTime_ms = 300; // 长按时间阈值
        private bool _longPressDragStarted = false;

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;
        private LoadingIndicator? _loadingIndicator;
        private bool _isNightMode = false;

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
        private HighlightColor _currentHighlightColor = HighlightColor.Yellow;
        private bool _isHighlightMode = true;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private bool _isImageMode = false;

        private Panel? _pageTransitionOverlay;
        private System.Windows.Forms.Timer? _pageTransitionTimer;
        private bool _isAnimating = false;
        private Bitmap? _currentPageImage; // 保存当前页面图像，避免 PictureBox 自动绘制

        private SplitContainer splitContainer1;
        private GroupBox groupBoxLanguage;
        private Label labelQuestion;
        private RichTextBox richTextBoxAiAnswer;
        private Button buttonSpeakAnswer;
        private TextBox textBoxQuestion;
        private Button buttonAddToLearning;
        private Button buttonAskAi;
        private Button buttonSpeakOriginal;
        private GroupBox groupBoxProgress;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private string _currentLanguage = "eng";

        public PdfReaderForm(ILogger<PdfReaderForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bookmarkService = new BookmarkService();
            _highlightService = new HighlightService();
            Load += PdfReaderForm_Load;
            Resize += PdfReaderForm_Resize;
            KeyDown += PdfReaderForm_KeyDown;

            // 初始化长按计时器
            _longPressTimer = new System.Windows.Forms.Timer();
            _longPressTimer.Interval = LongPressTime_ms;
            _longPressTimer.Tick += LongPressTimer_Tick;
        }


        private void PdfReaderForm_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();

            // 加载完成后通知 presenter 加载上次会话
            _presenter?.LoadLastSessionAndRestore();
        }

        private void PdfReaderForm_KeyDown(object? sender, KeyEventArgs e)
        {
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
        private int _transitionStep = 0;
        private bool _transitionFadeOut = false;

        private void StartPageTransition(bool forward)
        {
            if (_isAnimating || _pageTransitionOverlay == null) return;

            _isAnimating = true;
            _transitionStep = 0;
            _transitionFadeOut = true;
            _pageTransitionOverlay.Visible = true;
            _pageTransitionOverlay.BackColor = Color.White;
            _pageTransitionOverlay.BackColor = Color.FromArgb(255, 255, 255);

            if (_pageTransitionTimer != null)
            {
                _pageTransitionTimer.Start();
            }
        }

        private void PageTransitionTimer_Tick(object? sender, EventArgs e)
        {
            if (_pageTransitionOverlay == null || !_isAnimating) return;

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
                _pageTransitionOverlay.BackColor = Color.FromArgb(alpha, 255, 255, 255);
            }
            else
            {
                int alpha = _transitionStep * 25;
                if (alpha >= 255)
                {
                    alpha = 255;
                    _pageTransitionTimer?.Stop();
                    _isAnimating = false;
                    _pageTransitionOverlay.Visible = false;
                    return;
                }
                _pageTransitionOverlay.BackColor = Color.FromArgb(alpha, 255, 255, 255);
            }
        }

        private void RadioHighlightColor_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked && radio.Tag is int colorIndex)
            {
                _currentHighlightColor = (HighlightColor)colorIndex;
            }
        }

        private void ListBoxBookmarks_DoubleClick(object? sender, EventArgs e)
        {
            if (_listBoxBookmarks?.SelectedItem is PdfBookmark bookmark)
            {
                _presenter?.RenderPage(bookmark.PageIndex);
            }
        }

        private void ButtonAddBookmark_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPdfPath)) return;

            var title = _textBoxBookmarkTitle?.Text ?? $"第 {_currentPageIndex + 1} 页";
            _bookmarkService.AddBookmark(_currentPdfPath, _currentPageIndex, title);
            RefreshBookmarkList();
            _textBoxBookmarkTitle!.Text = string.Empty;
        }

        private void ButtonRemoveBookmark_Click(object? sender, EventArgs e)
        {
            if (_listBoxBookmarks?.SelectedItem is PdfBookmark bookmark)
            {
                _bookmarkService.RemoveBookmark(_currentPdfPath, bookmark.PageIndex, bookmark.Title);
                RefreshBookmarkList();
            }
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
            _isLocked = !_isLocked;
            if (_buttonLockView != null)
            {
                _buttonLockView.Text = _isLocked ? "🔒" : "🔓";
                _buttonLockView.BackColor = _isLocked ? Color.LightSalmon : Color.White;
            }

            if (_isLocked)
            {
                trackBarZoom.Enabled = false;
            }
            else
            {
                trackBarZoom.Enabled = true;
            }
        }

        private void ButtonResetView_Click(object? sender, EventArgs e)
        {
            _zoomLevel = 100;
            _imageOffset = Point.Empty;
            trackBarZoom.Value = 100;
            labelZoom.Text = "100%";
            ResetZoom();
        }

        private void ButtonRemoveHighlight_Click(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is PdfHighlight highlight)
            {
                _highlightUndoStack.Push(new HighlightUndoAction
                {
                    ActionType = HighlightActionType.Remove,
                    Highlight = highlight
                });
                _highlightService.RemoveHighlight(_currentPdfPath, highlight.Id);
                RefreshHighlightList();
                UpdateHighlightLayer();
                pictureBoxPdf?.Invalidate();
            }
        }

        private void ButtonExportHighlights_Click(object? sender, EventArgs e)
        {
            _presenter?.ExportHighlightsToExcel();
        }

        private void ButtonBatchRemoveHighlight_Click(object? sender, EventArgs e)
        {
            var highlights = _highlightService.GetHighlights(_currentPdfPath);
            if (highlights == null || highlights.Count == 0)
            {
                MessageBox.Show("当前文档没有高亮可删除", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show($"确定要删除所有 {highlights.Count} 个高亮吗？", "确认删除",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                foreach (var highlight in highlights)
                {
                    _highlightUndoStack.Push(new HighlightUndoAction
                    {
                        ActionType = HighlightActionType.Remove,
                        Highlight = highlight
                    });
                    _highlightService.RemoveHighlight(_currentPdfPath, highlight.Id);
                }

                RefreshHighlightList();
                UpdateHighlightLayer();
                pictureBoxPdf?.Invalidate();

                MessageBox.Show($"已成功删除 {highlights.Count} 个高亮", "删除完成",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ButtonUndoHighlight_Click(object? sender, EventArgs e)
        {
            if (_highlightUndoStack.Count > 0)
            {
                var lastAction = _highlightUndoStack.Pop();
                if (lastAction.ActionType == HighlightActionType.Add)
                {
                    _highlightService.RemoveHighlight(_currentPdfPath, lastAction.Highlight.Id);
                }
                else if (lastAction.ActionType == HighlightActionType.Remove)
                {
                    _highlightService.AddHighlight(
                        _currentPdfPath,
                        lastAction.Highlight.PageIndex,
                        lastAction.Highlight.NormalizedX > 0 ? lastAction.Highlight.NormalizedX : lastAction.Highlight.X,
                        lastAction.Highlight.NormalizedY > 0 ? lastAction.Highlight.NormalizedY : lastAction.Highlight.Y,
                        lastAction.Highlight.NormalizedWidth > 0 ? lastAction.Highlight.NormalizedWidth : lastAction.Highlight.Width,
                        lastAction.Highlight.NormalizedHeight > 0 ? lastAction.Highlight.NormalizedHeight : lastAction.Highlight.Height,
                        lastAction.Highlight.Text,
                        lastAction.Highlight.Color
                    );
                }
                RefreshHighlightList();
                UpdateHighlightLayer();
                pictureBoxPdf?.Invalidate();
            }
        }

        private void RefreshBookmarkList()
        {
            if (_listBoxBookmarks == null || string.IsNullOrEmpty(_currentPdfPath)) return;

            _listBoxBookmarks.Items.Clear();
            var bookmarks = _bookmarkService.GetBookmarks(_currentPdfPath);
            foreach (var bookmark in bookmarks)
            {
                _listBoxBookmarks.Items.Add(bookmark);
            }
        }

        private void RefreshHighlightList()
        {
            if (_listBoxHighlights == null || string.IsNullOrEmpty(_currentPdfPath)) return;

            _listBoxHighlights.Items.Clear();
            // 获取整个目录的高亮
            var folderPath = Path.GetDirectoryName(_currentPdfPath) ?? "";
            var highlights = _highlightService.GetHighlightsForFolder(folderPath);
            foreach (var highlight in highlights)
            {
                _listBoxHighlights.Items.Add(highlight);
            }
        }

        private void LoadHighlightsForCurrentPage()
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || _currentPageImage == null) return;

            var highlights = _highlightService.GetHighlightsForPage(_currentPdfPath, _currentPageIndex);
            UpdateHighlightLayer();
            pictureBoxPdf.Invalidate();
        }

        private void UpdateHighlightLayer()
        {
            try
            {
                if (_currentPageImage == null)
                {
                    CleanupHighlightLayer();
                    return;
                }

                var imgRect = GetImageDisplayRect();
                if (imgRect.Width <= 0 || imgRect.Height <= 0) return;

                int imgWidth = imgRect.Width;
                int imgHeight = imgRect.Height;

                bool needsRecreate = false;
                if (_highlightBitmap != null)
                {
                    try
                    {
                        if (_highlightBitmap.Width != imgWidth || _highlightBitmap.Height != imgHeight)
                        {
                            needsRecreate = true;
                            CleanupHighlightLayer();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        needsRecreate = true;
                        CleanupHighlightLayer();
                    }
                }

                if (_highlightBitmap == null || needsRecreate)
                {
                    _highlightBitmap = new Bitmap(imgWidth, imgHeight);
                    _highlightGraphics = Graphics.FromImage(_highlightBitmap);
                    _highlightGraphics.Clear(Color.Transparent);
                }

                _highlightGraphics!.Clear(Color.Transparent);

                var highlights = _highlightService.GetHighlightsForPage(_currentPdfPath, _currentPageIndex);
                foreach (var highlight in highlights)
                {
                    var color = HighlightService.GetHighlightColor(highlight.Color);

                    // 使用归一化坐标，兼容旧格式
                    float x, y, width, height;
                    if (highlight.NormalizedWidth > 0)
                    {
                        x = highlight.NormalizedX * imgWidth;
                        y = highlight.NormalizedY * imgHeight;
                        width = highlight.NormalizedWidth * imgWidth;
                        height = highlight.NormalizedHeight * imgHeight;
                    }
                    else
                    {
                        x = highlight.X;
                        y = highlight.Y;
                        width = highlight.Width;
                        height = highlight.Height;
                    }

                    var rect = new RectangleF(x, y, width, height);

                    // 确保矩形有效
                    if (rect.Width <= 0 || rect.Height <= 0 || rect.X < 0 || rect.Y < 0)
                    {
                        continue;
                    }

                    // 使用渐变画笔，更美观
                    using var gradientBrush = new LinearGradientBrush(
                        rect,
                        Color.FromArgb(color.A, color.R, color.G, color.B),
                        Color.FromArgb(color.A - 30, color.R, color.G, color.B),
                        LinearGradientMode.ForwardDiagonal);

                    _highlightGraphics.FillRectangle(gradientBrush, rect);

                    // 绘制边界
                    using var pen = new Pen(Color.FromArgb(color.A + 50, color.R, color.G, color.B), 1.5f);
                    _highlightGraphics.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);

                    if (!string.IsNullOrEmpty(highlight.Note))
                    {
                        using var font = new Font("Microsoft YaHei UI", 10F);
                        using var textBrush = new SolidBrush(Color.Black);
                        _highlightGraphics.DrawString("📝", font, textBrush, rect.Location);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateHighlightLayer");
                CleanupHighlightLayer();
            }
        }

        private void CleanupHighlightLayer()
        {
            try
            {
                _highlightGraphics?.Dispose();
                _highlightBitmap?.Dispose();
                _highlightGraphics = null;
                _highlightBitmap = null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error cleaning up highlight layer");
            }
        }

        private void CleanupAnnotationBitmap()
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



        public void SetCurrentPdfPath(string pdfPath)
        {
            CleanupHighlightLayer();
            ClearThumbnails();
            _currentPdfPath = pdfPath;
            _bookmarkService.ClearCache();
            _highlightService.ClearCacheForPdf(pdfPath);

            InitializeBookmarkAndHighlightUI();

            RefreshBookmarkList();
            RefreshHighlightList();
        }

        public void SetPresenter(PdfPresenter presenter)
        {
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _presenter.SetView(this);
        }

        #region IPdfView Implementation

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
                // 首先清理相关的注释位图，因为它们依赖于原图像尺寸
                CleanupAnnotationBitmap();
                CleanupHighlightLayer();

                var old = _currentPageImage;
                _currentPageImage = bmp;

                // 确保 pictureBoxPdf.Image 为 null，避免自动绘制
                pictureBoxPdf.Image = null;

                // 延迟释放旧图像，避免竞态条件
                if (old != null && old != bmp)
                {
                    Task.Delay(100).ContinueWith(_ =>
                    {
                        try
                        {
                            old.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to dispose old image");
                        }
                    }, TaskScheduler.Default);
                }

                // 触发重绘
                pictureBoxPdf.Invalidate();

                // 重新加载当前页面的高亮
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


        public void UpdateAiAnswer(string answer)
        {
            MarkdownParser.ParseMarkdownToRichTextBox(richTextBoxAiAnswer, answer);
        }

        public void SetQuestionInput(string text)
        {
            textBoxQuestion.Text = text;
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

        // 新增功能：低优先级 - 夜间模式切换
        public void NightMode()
        {
            _isNightMode = !_isNightMode;
            ApplyNightMode();
            if (buttonNightMode != null)
            {
                buttonNightMode.Text = _isNightMode ? "☀️" : "🌙";
            }
        }

        private void ApplyNightMode()
        {
            if (_isNightMode)
            {
                // 夜间模式 - 深色背景
                this.BackColor = Color.FromArgb(30, 30, 30);
                panelPdf.BackColor = Color.FromArgb(20, 20, 20);
                panelNavigation.BackColor = Color.FromArgb(45, 45, 45);
                treeViewFiles.BackColor = Color.FromArgb(40, 40, 40);
                treeViewFiles.ForeColor = Color.White;
                tabControlLeft.BackColor = Color.FromArgb(40, 40, 40);
                // 缩略图面板夜间模式
                panelThumbnails.BackColor = Color.FromArgb(40, 40, 40);
                flowLayoutPanelThumbnails.BackColor = Color.FromArgb(40, 40, 40);
                // 更新夜间模式按钮背景色
                if (buttonNightMode != null)
                {
                    buttonNightMode.BackColor = Color.FromArgb(45, 45, 45);
                }
                // 更新语言切换按钮背景色
                if (buttonLanguage != null)
                {
                    buttonLanguage.BackColor = Color.FromArgb(45, 45, 45);
                    buttonLanguage.ForeColor = Color.White;
                }
                // 右侧tab页夜间模式 - 翻译结果页面
                ApplyNightModeToTabPageTranslate(true);
                // 右侧tab页夜间模式 - 书签和高亮页面
                ApplyNightModeToBookmarksAndHighlights(true);
            }
            else
            {
                // 日间模式 - 浅色背景
                this.BackColor = Color.FromArgb(240, 240, 240);
                panelPdf.BackColor = Color.White;
                panelNavigation.BackColor = Color.FromArgb(240, 240, 240);
                treeViewFiles.BackColor = Color.White;
                treeViewFiles.ForeColor = Color.Black;
                tabControlLeft.BackColor = Color.White;
                // 缩略图面板日间模式
                panelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
                flowLayoutPanelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
                // 更新夜间模式按钮背景色
                if (buttonNightMode != null)
                {
                    buttonNightMode.BackColor = Color.White;
                }
                // 更新语言切换按钮背景色
                if (buttonLanguage != null)
                {
                    buttonLanguage.BackColor = Color.White;
                    buttonLanguage.ForeColor = Color.Black;
                }
                // 右侧tab页日间模式 - 翻译结果页面
                ApplyNightModeToTabPageTranslate(false);
                // 右侧tab页日间模式 - 书签和高亮页面
                ApplyNightModeToBookmarksAndHighlights(false);
            }

            // 更新缩略图背景色
            UpdateThumbnailsBackground();

            // 重新渲染当前页面以应用反色（如果需要）
            if (_presenter != null)
            {
                // 请求 Presenter 重新渲染当前页（Presenter 公开了异步渲染方法）
                _ = _presenter.RenderAndDisplayCurrentPageAsync();
            }
        }

        private void UpdateThumbnailsBackground()
        {
            if (flowLayoutPanelThumbnails == null) return;

            foreach (Control control in flowLayoutPanelThumbnails.Controls)
            {
                if (control is Panel panel)
                {
                    if (_isNightMode)
                    {
                        panel.BackColor = Color.FromArgb(45, 45, 45);
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label label)
                            {
                                label.ForeColor = Color.White;
                            }
                        }
                    }
                    else
                    {
                        panel.BackColor = Color.White;
                        foreach (Control child in panel.Controls)
                        {
                            if (child is Label label)
                            {
                                label.ForeColor = Color.Black;
                            }
                        }
                    }
                }
            }
        }

        private void ApplyNightModeToTabPageTranslate(bool isNightMode)
        {
            // 翻译结果页面的控件
            if (tabPageTranslate != null)
            {
                tabPageTranslate.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
            }

            // groupBoxProgress - 学习统计摘要
            if (groupBoxProgress != null)
            {
                groupBoxProgress.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
                groupBoxProgress.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // textBoxOriginal - 原文文本框
            if (textBoxOriginal != null)
            {
                textBoxOriginal.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                textBoxOriginal.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // textBoxTranslation - 译文文本框
            if (textBoxTranslation != null)
            {
                textBoxTranslation.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                textBoxTranslation.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // labelOriginal, labelTranslation - 标签
            if (labelOriginal != null)
            {
                labelOriginal.ForeColor = isNightMode ? Color.White : Color.Black;
            }
            if (labelTranslation != null)
            {
                labelTranslation.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 翻译相关按钮 - 夜间模式设置深色背景，日间模式恢复默认颜色
            if (buttonTranslate != null)
            {
                if (isNightMode)
                {
                    buttonTranslate.BackColor = Color.FromArgb(45, 45, 45);
                    buttonTranslate.ForeColor = Color.White;
                }
                else
                {
                    buttonTranslate.BackColor = SystemColors.Control;
                    buttonTranslate.ForeColor = SystemColors.ControlText;
                }
            }
            if (buttonSpeakOriginal != null)
            {
                if (isNightMode)
                {
                    buttonSpeakOriginal.BackColor = Color.FromArgb(45, 45, 45);
                    buttonSpeakOriginal.ForeColor = Color.White;
                }
                else
                {
                    buttonSpeakOriginal.BackColor = SystemColors.Control;
                    buttonSpeakOriginal.ForeColor = SystemColors.ControlText;
                }
            }
            if (buttonSpeakTranslation != null)
            {
                if (isNightMode)
                {
                    buttonSpeakTranslation.BackColor = Color.FromArgb(45, 45, 45);
                    buttonSpeakTranslation.ForeColor = Color.White;
                }
                else
                {
                    buttonSpeakTranslation.BackColor = SystemColors.Control;
                    buttonSpeakTranslation.ForeColor = SystemColors.ControlText;
                }
            }

            // groupBoxLanguage - AI提问区域
            if (groupBoxLanguage != null)
            {
                groupBoxLanguage.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
                groupBoxLanguage.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // textBoxQuestion - 问题输入框
            if (textBoxQuestion != null)
            {
                textBoxQuestion.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                textBoxQuestion.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // richTextBoxAiAnswer - AI回答框
            if (richTextBoxAiAnswer != null)
            {
                richTextBoxAiAnswer.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                richTextBoxAiAnswer.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // labelQuestion - 问题标签
            if (labelQuestion != null)
            {
                labelQuestion.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // AI相关按钮 - 夜间模式设置深色背景，日间模式恢复默认颜色
            if (buttonAskAi != null)
            {
                if (isNightMode)
                {
                    buttonAskAi.BackColor = Color.FromArgb(45, 45, 45);
                    buttonAskAi.ForeColor = Color.White;
                }
                else
                {
                    buttonAskAi.BackColor = SystemColors.Control;
                    buttonAskAi.ForeColor = SystemColors.ControlText;
                }
            }
            if (buttonAddToLearning != null)
            {
                if (isNightMode)
                {
                    buttonAddToLearning.BackColor = Color.FromArgb(45, 45, 45);
                    buttonAddToLearning.ForeColor = Color.White;
                }
                else
                {
                    buttonAddToLearning.BackColor = SystemColors.Control;
                    buttonAddToLearning.ForeColor = SystemColors.ControlText;
                }
            }
            if (buttonSpeakAnswer != null)
            {
                if (isNightMode)
                {
                    buttonSpeakAnswer.BackColor = Color.FromArgb(45, 45, 45);
                    buttonSpeakAnswer.ForeColor = Color.White;
                }
                else
                {
                    buttonSpeakAnswer.BackColor = SystemColors.Control;
                    buttonSpeakAnswer.ForeColor = SystemColors.ControlText;
                }
            }
        }

        private void ApplyNightModeToBookmarksAndHighlights(bool isNightMode)
        {
            // 书签和高亮页面
            if (_tabPageBookmarksAndHighlights != null)
            {
                _tabPageBookmarksAndHighlights.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
            }

            // 书签区域
            if (_groupBoxBookmarks != null)
            {
                _groupBoxBookmarks.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
                _groupBoxBookmarks.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 书签列表
            if (_listBoxBookmarks != null)
            {
                _listBoxBookmarks.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                _listBoxBookmarks.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 书签标题输入框
            if (_textBoxBookmarkTitle != null)
            {
                _textBoxBookmarkTitle.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                _textBoxBookmarkTitle.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 书签按钮 - 夜间模式设置深色背景，日间模式恢复默认颜色
            if (_buttonAddBookmark != null)
            {
                if (isNightMode)
                {
                    _buttonAddBookmark.BackColor = Color.FromArgb(45, 45, 45);
                    _buttonAddBookmark.ForeColor = Color.White;
                }
                else
                {
                    _buttonAddBookmark.BackColor = SystemColors.Control;
                    _buttonAddBookmark.ForeColor = SystemColors.ControlText;
                }
            }
            if (_buttonRemoveBookmark != null)
            {
                if (isNightMode)
                {
                    _buttonRemoveBookmark.BackColor = Color.FromArgb(45, 45, 45);
                    _buttonRemoveBookmark.ForeColor = Color.White;
                }
                else
                {
                    _buttonRemoveBookmark.BackColor = SystemColors.Control;
                    _buttonRemoveBookmark.ForeColor = SystemColors.ControlText;
                }
            }

            // 高亮区域
            if (_groupBoxHighlights != null)
            {
                _groupBoxHighlights.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
                _groupBoxHighlights.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 高亮列表
            if (_listBoxHighlights != null)
            {
                _listBoxHighlights.BackColor = isNightMode ? Color.FromArgb(30, 30, 30) : Color.White;
                _listBoxHighlights.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 高亮颜色选择区域
            if (groupBoxHighlightColor != null)
            {
                groupBoxHighlightColor.BackColor = isNightMode ? Color.FromArgb(40, 40, 40) : Color.White;
                groupBoxHighlightColor.ForeColor = isNightMode ? Color.White : Color.Black;
            }

            // 高亮按钮 - 夜间模式设置深色背景，日间模式恢复默认颜色
            if (_buttonRemoveHighlight != null)
            {
                if (isNightMode)
                {
                    _buttonRemoveHighlight.BackColor = Color.FromArgb(45, 45, 45);
                    _buttonRemoveHighlight.ForeColor = Color.White;
                }
                else
                {
                    _buttonRemoveHighlight.BackColor = SystemColors.Control;
                    _buttonRemoveHighlight.ForeColor = SystemColors.ControlText;
                }
            }
            if (_buttonBatchRemoveHighlight != null)
            {
                if (isNightMode)
                {
                    _buttonBatchRemoveHighlight.BackColor = Color.FromArgb(45, 45, 45);
                    _buttonBatchRemoveHighlight.ForeColor = Color.White;
                }
                else
                {
                    _buttonBatchRemoveHighlight.BackColor = SystemColors.Control;
                    _buttonBatchRemoveHighlight.ForeColor = SystemColors.ControlText;
                }
            }
            if (_buttonExportHighlights != null)
            {
                if (isNightMode)
                {
                    _buttonExportHighlights.BackColor = Color.FromArgb(45, 45, 45);
                    _buttonExportHighlights.ForeColor = Color.White;
                }
                else
                {
                    _buttonExportHighlights.BackColor = SystemColors.Control;
                    _buttonExportHighlights.ForeColor = SystemColors.ControlText;
                }
            }
            if (buttonUndoHighlight != null)
            {
                if (isNightMode)
                {
                    buttonUndoHighlight.BackColor = Color.FromArgb(45, 45, 45);
                    buttonUndoHighlight.ForeColor = Color.White;
                }
                else
                {
                    buttonUndoHighlight.BackColor = SystemColors.Control;
                    buttonUndoHighlight.ForeColor = SystemColors.ControlText;
                }
            }
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
            if (flowLayoutPanelThumbnails == null || thumbnail == null) return;

            var panel = new Panel();
            panel.Size = new Size(100, 140);
            panel.Margin = new Padding(5);
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Tag = pageIndex;

            // 根据夜间模式设置背景色
            panel.BackColor = _isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;

            // 如果是夜间模式，对缩略图进行反色处理
            Image displayImage = thumbnail;
            if (_isNightMode)
            {
                displayImage = InvertImage(thumbnail);
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
            // 根据夜间模式设置文字颜色
            label.ForeColor = _isNightMode ? Color.White : Color.Black;
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

        private Image InvertImage(Image image)
        {
            Bitmap bitmap = new Bitmap(image);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
            int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;
            IntPtr ptr = data.Scan0;
            int bytes = Math.Abs(data.Stride) * bitmap.Height;
            byte[] rgbValues = new byte[bytes];
            System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

            for (int i = 0; i < rgbValues.Length; i += bytesPerPixel)
            {
                if (bytesPerPixel >= 3)
                {
                    rgbValues[i] = (byte)(255 - rgbValues[i]);
                    rgbValues[i + 1] = (byte)(255 - rgbValues[i + 1]);
                    rgbValues[i + 2] = (byte)(255 - rgbValues[i + 2]);
                }
            }

            System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
            bitmap.UnlockBits(data);
            return bitmap;
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
                        panel.BackColor = _isNightMode ? Color.FromArgb(45, 45, 45) : Color.White;
                        panel.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
            }
        }

        // 新增功能：中等级 - 私有方法用于翻页
        private void NavigateToPage(int pageIndex)
        {
            // 直接调用 presenter 的渲染方法
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

        public string GetQuestionText()
        {
            return textBoxQuestion.Text;
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

        public string GetAiAnswerText()
        {
            return richTextBoxAiAnswer.Text;
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

                // 应用缩放级别
                float scale = _zoomLevel / 100.0f;
                displayWidth = (int)(displayWidth * scale);
                displayHeight = (int)(displayHeight * scale);

                // 计算居中位置（考虑拖动偏移）
                displayX = (controlWidth - displayWidth) / 2 + _imageOffset.X;
                displayY = (controlHeight - displayHeight) / 2 + _imageOffset.Y;

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
        public event EventHandler<Views.AddToEditorEventArgs>? AddToEditor;
        public void RaiseAddToEditor(string text, string language)
        {
            AddToEditor?.Invoke(this, new Views.AddToEditorEventArgs { Text = text, Language = language });
        }
        public event EventHandler? SpeakTranslation;
        public event EventHandler<string>? SpeakText;
        public event EventHandler<string>? AskAiWithText;
        public event EventHandler? SelectOcrClicked;
        public event EventHandler? TranslateClicked;
        public event EventHandler? ToggleNightMode;
        public event EventHandler? LanguageChanged;
        public event EventHandler? SpeakAnswer;
        public event EventHandler? SpeakOriginal;

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
        private Panel panelNavigation;
        private Button buttonPrev;
        private TextBox textBoxPage;
        private Label labelPageCount;
        private Button buttonNext;
        private Button buttonNightMode;
        private Button buttonLanguage;
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
            buttonNightMode = new Button();
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
            textBoxOriginal = new TextBox();
            buttonSpeakOriginal = new Button();
            labelTranslation = new Label();
            labelOriginal = new Label();
            buttonSpeakTranslation = new Button();
            buttonTranslate = new Button();
            groupBoxLanguage = new GroupBox();
            labelQuestion = new Label();
            richTextBoxAiAnswer = new RichTextBox();
            buttonSpeakAnswer = new Button();
            textBoxQuestion = new TextBox();
            buttonAddToLearning = new Button();
            buttonAskAi = new Button();
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
            groupBoxLanguage.SuspendLayout();
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
            panelNavigation.Controls.Add(buttonNightMode);
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
            _loadingIndicator.BackColor = SystemColors.MenuHighlight;
            _loadingIndicator.IsLoading = false;
            _loadingIndicator.Location = new Point(447, 11);
            _loadingIndicator.Name = "_loadingIndicator";
            _loadingIndicator.Size = new Size(38, 35);
            _loadingIndicator.TabIndex = 2;
            _loadingIndicator.Visible = false;
            // 
            // buttonLanguage
            // 
            buttonLanguage.BackColor = Color.White;
            buttonLanguage.FlatStyle = FlatStyle.Flat;
            buttonLanguage.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonLanguage.Location = new Point(364, 11);
            buttonLanguage.Name = "buttonLanguage";
            buttonLanguage.Size = new Size(45, 35);
            buttonLanguage.TabIndex = 9;
            buttonLanguage.Text = "eng";
            buttonLanguage.UseVisualStyleBackColor = false;
            buttonLanguage.Click += ButtonLanguage_Click;
            // 
            // buttonNightMode
            // 
            buttonNightMode.BackColor = Color.White;
            buttonNightMode.FlatStyle = FlatStyle.Flat;
            buttonNightMode.Font = new Font("Microsoft YaHei UI", 12F);
            buttonNightMode.Location = new Point(410, 11);
            buttonNightMode.Name = "buttonNightMode";
            buttonNightMode.Size = new Size(35, 35);
            buttonNightMode.TabIndex = 6;
            buttonNightMode.Text = "🌙";
            buttonNightMode.UseVisualStyleBackColor = false;
            buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // _buttonLockView
            // 
            _buttonLockView.BackColor = Color.White;
            _buttonLockView.FlatStyle = FlatStyle.Flat;
            _buttonLockView.Location = new Point(410, 11);
            _buttonLockView.Name = "_buttonLockView";
            _buttonLockView.Size = new Size(35, 35);
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
            _buttonResetView.Location = new Point(495, 11);
            _buttonResetView.Name = "_buttonResetView";
            _buttonResetView.Size = new Size(35, 35);
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
            pictureBoxPdf.MouseDown += PictureBoxPdf_MouseDown;
            pictureBoxPdf.MouseMove += PictureBoxPdf_MouseMove;
            pictureBoxPdf.MouseUp += PictureBoxPdf_MouseUp;
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
            tabPageTranslate.Controls.Add(groupBoxLanguage);
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
            groupBoxProgress.Controls.Add(textBoxOriginal);
            groupBoxProgress.Controls.Add(buttonSpeakOriginal);
            groupBoxProgress.Controls.Add(labelTranslation);
            groupBoxProgress.Controls.Add(labelOriginal);
            groupBoxProgress.Controls.Add(buttonSpeakTranslation);
            groupBoxProgress.Controls.Add(buttonTranslate);
            groupBoxProgress.Dock = DockStyle.Top;
            groupBoxProgress.Location = new Point(3, 3);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Size = new Size(326, 523);
            groupBoxProgress.TabIndex = 24;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxTranslation.Location = new Point(9, 325);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ReadOnly = true;
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(311, 181);
            textBoxTranslation.TabIndex = 5;
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOriginal.Location = new Point(9, 42);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(311, 195);
            textBoxOriginal.TabIndex = 1;
            // 
            // buttonSpeakOriginal
            // 
            buttonSpeakOriginal.Location = new Point(122, 250);
            buttonSpeakOriginal.Name = "buttonSpeakOriginal";
            buttonSpeakOriginal.Size = new Size(85, 34);
            buttonSpeakOriginal.TabIndex = 23;
            buttonSpeakOriginal.Text = "🔊朗读原文";
            buttonSpeakOriginal.Click += ButtonSpeakOriginal_Click;
            // 
            // labelTranslation
            // 
            labelTranslation.Location = new Point(9, 302);
            labelTranslation.Name = "labelTranslation";
            labelTranslation.Size = new Size(260, 20);
            labelTranslation.TabIndex = 2;
            labelTranslation.Text = "译文:";
            // 
            // labelOriginal
            // 
            labelOriginal.Location = new Point(9, 17);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(260, 20);
            labelOriginal.TabIndex = 0;
            labelOriginal.Text = "原文:";
            // 
            // buttonSpeakTranslation
            // 
            buttonSpeakTranslation.Location = new Point(229, 250);
            buttonSpeakTranslation.Name = "buttonSpeakTranslation";
            buttonSpeakTranslation.Size = new Size(85, 34);
            buttonSpeakTranslation.TabIndex = 4;
            buttonSpeakTranslation.Text = "🔊朗读译文";
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // buttonTranslate
            // 
            buttonTranslate.Location = new Point(9, 250);
            buttonTranslate.Name = "buttonTranslate";
            buttonTranslate.Size = new Size(85, 34);
            buttonTranslate.TabIndex = 3;
            buttonTranslate.Text = "📚翻译";
            buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // groupBoxLanguage
            // 
            groupBoxLanguage.Controls.Add(labelQuestion);
            groupBoxLanguage.Controls.Add(richTextBoxAiAnswer);
            groupBoxLanguage.Controls.Add(buttonSpeakAnswer);
            groupBoxLanguage.Controls.Add(textBoxQuestion);
            groupBoxLanguage.Controls.Add(buttonAddToLearning);
            groupBoxLanguage.Controls.Add(buttonAskAi);
            groupBoxLanguage.Dock = DockStyle.Bottom;
            groupBoxLanguage.Location = new Point(3, 493);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(326, 287);
            groupBoxLanguage.TabIndex = 22;
            groupBoxLanguage.TabStop = false;
            groupBoxLanguage.Text = "🤖 AI提问";
            // 
            // labelQuestion
            // 
            labelQuestion.Location = new Point(9, 28);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(305, 20);
            labelQuestion.TabIndex = 0;
            labelQuestion.Text = "问题:";
            // 
            // richTextBoxAiAnswer
            // 
            richTextBoxAiAnswer.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxAiAnswer.Location = new Point(9, 120);
            richTextBoxAiAnswer.Name = "richTextBoxAiAnswer";
            richTextBoxAiAnswer.ReadOnly = true;
            richTextBoxAiAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAiAnswer.Size = new Size(311, 161);
            richTextBoxAiAnswer.TabIndex = 5;
            richTextBoxAiAnswer.Text = "";
            // 
            // buttonSpeakAnswer
            // 
            buttonSpeakAnswer.Location = new Point(219, 81);
            buttonSpeakAnswer.Name = "buttonSpeakAnswer";
            buttonSpeakAnswer.Size = new Size(85, 34);
            buttonSpeakAnswer.TabIndex = 4;
            buttonSpeakAnswer.Text = "🔊 朗读原文";
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxQuestion.Location = new Point(9, 51);
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.Size = new Size(302, 23);
            textBoxQuestion.TabIndex = 1;
            // 
            // buttonAddToLearning
            // 
            buttonAddToLearning.Location = new Point(9, 81);
            buttonAddToLearning.Name = "buttonAddToLearning";
            buttonAddToLearning.Size = new Size(85, 34);
            buttonAddToLearning.TabIndex = 3;
            buttonAddToLearning.Text = "📝 加生词本";
            // 
            // buttonAskAi
            // 
            buttonAskAi.Location = new Point(112, 81);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(85, 34);
            buttonAskAi.TabIndex = 2;
            buttonAskAi.Text = "🤖 AI提问";
            // 
            // tabPageFiles
            // 
            tabPageFiles.Controls.Add(treeViewFiles);
            tabPageFiles.Location = new Point(4, 26);
            tabPageFiles.Name = "tabPageFiles";
            tabPageFiles.Padding = new Padding(3);
            tabPageFiles.Size = new Size(332, 783);
            tabPageFiles.TabIndex = 0;
            tabPageFiles.Text = "📁 目录";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // treeViewFiles
            // 
            treeViewFiles.Dock = DockStyle.Fill;
            treeViewFiles.Location = new Point(3, 3);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(326, 777);
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
            groupBoxLanguage.ResumeLayout(false);
            groupBoxLanguage.PerformLayout();
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
                if (_isSelecting)
                {
                    _isSelecting = false;
                    _selectEnd = e.Location;
                    _lastSelectionRect = GetSelectionRectangle(_selectStart, _selectEnd);

                    if (_isHighlightMode && _lastSelectionRect.HasValue)
                    {
                        _ = AddHighlightFromSelectionAsync(_lastSelectionRect.Value);
                    }
                    else
                    {
                        try
                        {
                            pictureBoxPdf.Invalidate();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error invalidating after selection");
                        }
                        SelectOcrClicked?.Invoke(this, EventArgs.Empty);
                    }
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
            _isDragging = true;
            _longPressDragStarted = true;
            _dragStart = startLocation;
            pictureBoxPdf.Cursor = Cursors.Hand;
        }

        private async Task AddHighlightFromSelectionAsync(Rectangle selectionRect)
        {
            try
            {
                // 捕获共享状态到局部变量，避免异步操作中的竞态条件
                var currentPageImage = _currentPageImage;
                var currentPdfPath = _currentPdfPath;
                var currentPageIndex = _currentPageIndex;
                var currentHighlightColor = _currentHighlightColor;

                if (currentPageImage == null || string.IsNullOrEmpty(currentPdfPath)) return;

                var imgRect = GetImageDisplayRect();
                if (imgRect.Width <= 0 || imgRect.Height <= 0) return;

                // 计算选择区域在显示矩形中的相对位置
                float x = Math.Max(0, selectionRect.X - imgRect.X);
                float y = Math.Max(0, selectionRect.Y - imgRect.Y);
                float width = Math.Min(selectionRect.Width, imgRect.Right - selectionRect.X);
                float height = Math.Min(selectionRect.Height, imgRect.Bottom - selectionRect.Y);

                // 直接使用显示矩形尺寸进行归一化，与UpdateHighlightLayer保持一致
                var normalizedRect = new RectangleF(
                    x / imgRect.Width,
                    y / imgRect.Height,
                    width / imgRect.Width,
                    height / imgRect.Height
                );

                // 确保矩形有效
                if (normalizedRect.Width < 0.01f || normalizedRect.Height < 0.01f) return;

                // 使用OCR识别选中区域的文字
                string ocrText = await GetOcrTextFromSelectionAsync(selectionRect, currentPageImage);

                // 添加高亮（包含OCR识别的文字）
                var highlight = new PdfHighlight
                {
                    PdfPath = currentPdfPath,
                    PageIndex = currentPageIndex,
                    NormalizedX = normalizedRect.X,
                    NormalizedY = normalizedRect.Y,
                    NormalizedWidth = normalizedRect.Width,
                    NormalizedHeight = normalizedRect.Height,
                    Text = ocrText,
                    Color = currentHighlightColor,
                    CreatedAt = DateTime.Now
                };

                _highlightUndoStack.Push(new HighlightUndoAction
                {
                    ActionType = HighlightActionType.Add,
                    Highlight = highlight
                });
                _highlightService.AddHighlight(
                    currentPdfPath,
                    currentPageIndex,
                    normalizedRect.X,
                    normalizedRect.Y,
                    normalizedRect.Width,
                    normalizedRect.Height,
                    ocrText,
                    currentHighlightColor
                );

                RefreshHighlightList();
                UpdateHighlightLayer();
                pictureBoxPdf.Invalidate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding highlight from selection");
            }
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
                    if ((ModifierKeys & Keys.Control) == Keys.Control)
                    {
                        // Ctrl + 滚轮：缩放
                        if (e.Delta > 0) _zoomLevel = Math.Min(400, _zoomLevel + 10);
                        else _zoomLevel = Math.Max(10, _zoomLevel - 10);
                        
                        // 异步渲染，避免阻塞 UI 线程
                        Task.Run(async () =>
                        {
                            try
                            {
                                var page = int.TryParse(textBoxPage.Text, out var p) ? p - 1 : 0;
                                int targetW = (int)(pictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
                                int targetH = (int)(pictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
                                var bmp = await _presenter!.RenderPageAsync(page, Math.Max(1, targetW), Math.Max(1, targetH));
                                if (bmp != null)
                                {
                                    BeginInvoke(() => DisplayImage(bmp));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error rendering page during zoom");
                            }
                        });
                    }
                    else
                    {
                        // 普通滚轮：翻页
                        if (_presenter != null)
                        {
                            if (e.Delta < 0)
                            {
                                _presenter.NextPage();
                            }
                            else
                            {
                                _presenter.PreviousPage();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PictureBoxPdf_MouseWheel");
            }
        }

        public async void ResetZoom()
        {
            _zoomLevel = 100;
            _imageOffset = Point.Empty;
            if (_presenter != null)
            {
                var page = int.TryParse(textBoxPage.Text, out var p) ? p - 1 : 0;
                try
                {
                    var bmp = await _presenter.RenderPageAsync(page, pictureBoxPdf.ClientSize.Width, pictureBoxPdf.ClientSize.Height);
                    if (bmp != null)
                    {
                        DisplayImage(bmp);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error resetting zoom");
                }
            }
            pictureBoxPdf.Invalidate();
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
                }

                if (_isSelecting)
                {
                    var rect = GetSelectionRectangle(_selectStart, _selectEnd);

                    if (_isHighlightMode)
                    {
                        var color = HighlightService.GetHighlightColor(_currentHighlightColor);
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
                else if (_isDrawing)
                {
                    using var pen = new Pen(Color.Red, 4f);
                    e.Graphics.DrawLine(pen, _selectStart, _selectEnd);
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
                if (_highlightBitmap == null || _currentPageImage == null)
                    return;

                var imgRect = GetImageDisplayRect();

                // _highlightBitmap已经是imgRect尺寸，直接绘制到imgRect位置
                g.DrawImage(_highlightBitmap, imgRect.Location);
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

        private void ButtonAskAi_Click(object? sender, EventArgs e)
        {
            AiQuestionAsked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAddToLearning_Click(object? sender, EventArgs e)
        {
            AddToLearningList?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSpeakAnswer_Click(object? sender, EventArgs e)
        {
            SpeakAnswer?.Invoke(this, EventArgs.Empty);
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
                    _pen?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing pen");
                }

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
    }
}
