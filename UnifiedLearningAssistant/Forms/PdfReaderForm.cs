using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Services.Pdf;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Views.UI;

namespace UnifiedLearningAssistant.Forms
{
    public partial class PdfReaderForm : UserControl, IPdfView
    {
        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderForm> _logger;
        private readonly BookmarkService _bookmarkService;
        private readonly HighlightService _highlightService;
        private int _zoomLevel = 100;
        private bool _isSelecting = false;
        private bool _isDrawing = false;
        private Point _selectStart = Point.Empty;
        private Point _selectEnd = Point.Empty;
        private Rectangle? _lastSelectionRect = null;
        private readonly Pen _pen = new Pen(Color.Red, 4f);
        private Bitmap? _annotationBitmap;
        private Graphics? _annotationGraphics;
        private List<PointF>? _currentStrokePoints;
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

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;
        private LoadingIndicator? _loadingIndicator;
        private bool _isNightMode = false;

        private Panel? _bookmarkPanel;
        private ListBox? _listBoxBookmarks;
        private Button? _buttonAddBookmark;
        private Button? _buttonRemoveBookmark;
        private TextBox? _textBoxBookmarkTitle;

        private Panel? _highlightPanel;
        private ListBox? _listBoxHighlights;
        private Button? _buttonAddHighlight;
        private Button? _buttonRemoveHighlight;
        private ComboBox? _comboBoxHighlightColor;

        private TabPage? _tabPageBookmarks;
        private TabPage? _tabPageHighlights;
        private HighlightColor _currentHighlightColor = HighlightColor.Yellow;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;

        private Panel? _pageTransitionOverlay;
        private Timer? _pageTransitionTimer;
        private bool _isAnimating = false;

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
            InitializeBookmarkAndHighlightUI();
            InitializePageTransition();
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
            if (_tabPageBookmarks != null || tabControlLeft?.Contains(_tabPageBookmarks!) == true)
            {
                return;
            }

            _tabPageBookmarks = new TabPage();
            _tabPageBookmarks.Name = "tabPageBookmarks";
            _tabPageBookmarks.Text = "🔖 书签";
            _tabPageBookmarks.Size = new Size(335, 822);

            var bookmarkContainer = new Panel { Dock = DockStyle.Fill };
            _listBoxBookmarks = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 300,
                Font = new Font("Microsoft YaHei UI", 10F)
            };
            _listBoxBookmarks.DoubleClick += ListBoxBookmarks_DoubleClick;

            _textBoxBookmarkTitle = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 30,
                PlaceholderText = "输入书签名称...",
                Margin = new Padding(5)
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight
            };

            _buttonAddBookmark = new Button { Text = "➕ 添加书签", Width = 100, Margin = new Padding(5) };
            _buttonAddBookmark.Click += ButtonAddBookmark_Click;

            _buttonRemoveBookmark = new Button { Text = "🗑️ 删除书签", Width = 100, Margin = new Padding(5) };
            _buttonRemoveBookmark.Click += ButtonRemoveBookmark_Click;

            buttonPanel.Controls.Add(_buttonAddBookmark);
            buttonPanel.Controls.Add(_buttonRemoveBookmark);

            bookmarkContainer.Controls.Add(_listBoxBookmarks);
            bookmarkContainer.Controls.Add(buttonPanel);
            bookmarkContainer.Controls.Add(_textBoxBookmarkTitle!);

            _tabPageBookmarks.Controls.Add(bookmarkContainer);

            _tabPageHighlights = new TabPage();
            _tabPageHighlights.Name = "tabPageHighlights";
            _tabPageHighlights.Text = "🖍️ 高亮";
            _tabPageHighlights.Size = new Size(335, 822);

            var highlightContainer = new Panel { Dock = DockStyle.Fill };

            var colorPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight
            };

            var colorLabel = new Label { Text = "颜色:", AutoSize = true, Margin = new Padding(5) };
            _comboBoxHighlightColor = new ComboBox
            {
                Width = 100,
                Margin = new Padding(5)
            };
            _comboBoxHighlightColor.Items.AddRange(new[] { "黄色", "绿色", "蓝色", "粉色", "橙色" });
            _comboBoxHighlightColor.SelectedIndex = 0;
            _comboBoxHighlightColor.SelectedIndexChanged += ComboBoxHighlightColor_SelectedIndexChanged;

            colorPanel.Controls.Add(colorLabel);
            colorPanel.Controls.Add(_comboBoxHighlightColor);

            _listBoxHighlights = new ListBox
            {
                Dock = DockStyle.Top,
                Height = 250,
                Font = new Font("Microsoft YaHei UI", 10F)
            };
            _listBoxHighlights.DoubleClick += ListBoxHighlights_DoubleClick;

            var highlightButtonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.LeftToRight
            };

            _buttonAddHighlight = new Button { Text = "➕ 添加高亮", Width = 100, Margin = new Padding(5) };
            _buttonAddHighlight.Click += ButtonAddHighlight_Click;

            _buttonRemoveHighlight = new Button { Text = "🗑️ 删除高亮", Width = 100, Margin = new Padding(5) };
            _buttonRemoveHighlight.Click += ButtonRemoveHighlight_Click;

            highlightButtonPanel.Controls.Add(_buttonAddHighlight);
            highlightButtonPanel.Controls.Add(_buttonRemoveHighlight);

            highlightContainer.Controls.Add(_listBoxHighlights);
            highlightContainer.Controls.Add(highlightButtonPanel);
            highlightContainer.Controls.Add(colorPanel);

            _tabPageHighlights!.Controls.Add(highlightContainer);

            if (tabControlLeft != null)
            {
                tabControlLeft.Controls.Add(_tabPageBookmarks);
                tabControlLeft.Controls.Add(_tabPageHighlights);
            }
        }

        private void InitializePageTransition()
        {
            _pageTransitionOverlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var transitionLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "",
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Microsoft YaHei UI", 24F, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 100, 100, 100)
            };
            _pageTransitionOverlay.Controls.Add(transitionLabel);

            if (panelPdf != null)
            {
                panelPdf.Controls.Add(_pageTransitionOverlay);
                _pageTransitionOverlay.BringToFront();
            }

            _pageTransitionTimer = new Timer { Interval = 50 };
            _pageTransitionTimer.Tick += PageTransitionTimer_Tick;
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

        private void ComboBoxHighlightColor_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_comboBoxHighlightColor == null) return;
            _currentHighlightColor = (HighlightColor)_comboBoxHighlightColor.SelectedIndex;
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
                _presenter?.RenderPage(highlight.PageIndex);
            }
        }

        private void ButtonAddHighlight_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentPdfPath)) return;

            var imgRect = GetImageDisplayRect();
            float x = imgRect.X + imgRect.Width * 0.1f;
            float y = imgRect.Y + imgRect.Height * 0.1f;
            float width = imgRect.Width * 0.8f;
            float height = imgRect.Height * 0.8f;

            _highlightService.AddHighlight(_currentPdfPath, _currentPageIndex, x, y, width, height, "", _currentHighlightColor);
            RefreshHighlightList();
            pictureBoxPdf?.Invalidate();
        }

        private void ButtonRemoveHighlight_Click(object? sender, EventArgs e)
        {
            if (_listBoxHighlights?.SelectedItem is PdfHighlight highlight)
            {
                _highlightService.RemoveHighlight(_currentPdfPath, highlight.Id);
                RefreshHighlightList();
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
            var highlights = _highlightService.GetHighlights(_currentPdfPath);
            foreach (var highlight in highlights)
            {
                _listBoxHighlights.Items.Add(highlight);
            }
        }

        private void LoadHighlightsForCurrentPage()
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || pictureBoxPdf?.Image == null) return;

            var highlights = _highlightService.GetHighlightsForPage(_currentPdfPath, _currentPageIndex);
            pictureBoxPdf.Invalidate();
        }

        public void SetCurrentPdfPath(string pdfPath)
        {
            _currentPdfPath = pdfPath;
            _bookmarkService.ClearCache();
            _highlightService.ClearCacheForPdf(pdfPath);
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
            var old = pictureBoxPdf.Image;
            pictureBoxPdf.Image = bmp;
            old?.Dispose();
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

            var label = new Label();
            label.Text = (pageIndex + 1).ToString();
            label.Location = new Point(5, 120);
            label.Size = new Size(90, 15);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Microsoft YaHei UI", 8F);
            // 根据夜间模式设置文字颜色
            label.ForeColor = _isNightMode ? Color.White : Color.Black;

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(label);
            panel.Click += (s, e) =>
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
            return pictureBoxPdf.Image;
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
            if (pictureBoxPdf.Image == null)
                return pictureBoxPdf.ClientRectangle;

            var image = pictureBoxPdf.Image;
            var controlWidth = pictureBoxPdf.ClientSize.Width;
            var controlHeight = pictureBoxPdf.ClientSize.Height;

            float imageAspect = (float)image.Width / image.Height;
            float controlAspect = (float)controlWidth / controlHeight;

            int displayWidth, displayHeight, displayX, displayY;

            if (imageAspect > controlAspect)
            {
                // 图片更宽，水平填满，垂直居中
                displayWidth = controlWidth;
                displayHeight = (int)(controlWidth / imageAspect);
                displayX = 0;
                displayY = (controlHeight - displayHeight) / 2;
            }
            else
            {
                // 图片更高，垂直填满，水平居中
                displayHeight = controlHeight;
                displayWidth = (int)(controlHeight * imageAspect);
                displayY = 0;
                displayX = (controlWidth - displayWidth) / 2;
            }

            return new Rectangle(displayX, displayY, displayWidth, displayHeight);
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

        private void InitializeComponent()
        {
            splitContainer1 = new SplitContainer();
            panelPdf = new Panel();
            panelNavigation = new Panel();
            trackBarZoom = new TrackBar();
            labelZoom = new Label();
            _loadingIndicator = new LoadingIndicator();
            buttonLanguage = new Button();
            buttonNightMode = new Button();
            buttonNext = new Button();
            labelPageCount = new Label();
            textBoxPage = new TextBox();
            buttonPrev = new Button();
            pictureBoxPdf = new PictureBox();
            _ocrPanel = new Panel();
            _ocrPictureBox = new PictureBox();
            _ocrCloseButton = new Button();
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
            buttonOpenFolder = new Button();
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
            panelLeftContainer.SuspendLayout();
            tabControlLeft.SuspendLayout();
            tabPageThumbnails.SuspendLayout();
            panelThumbnails.SuspendLayout();
            tabPageTranslate.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            tabPageFiles.SuspendLayout();
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
            splitContainer1.Size = new Size(1396, 887);
            splitContainer1.SplitterDistance = 1049;
            splitContainer1.TabIndex = 5;
            // 
            // panelPdf
            // 
            panelPdf.Controls.Add(panelNavigation);
            panelPdf.Controls.Add(pictureBoxPdf);
            panelPdf.Controls.Add(_ocrPanel);
            panelPdf.Dock = DockStyle.Fill;
            panelPdf.Location = new Point(0, 0);
            panelPdf.Name = "panelPdf";
            panelPdf.Size = new Size(1049, 887);
            panelPdf.TabIndex = 1;
            // 
            // panelNavigation
            // 
            panelNavigation.Controls.Add(trackBarZoom);
            panelNavigation.Controls.Add(labelZoom);
            panelNavigation.Controls.Add(_loadingIndicator);
            panelNavigation.Controls.Add(buttonLanguage);
            panelNavigation.Controls.Add(buttonNightMode);
            panelNavigation.Controls.Add(buttonNext);
            panelNavigation.Controls.Add(labelPageCount);
            panelNavigation.Controls.Add(textBoxPage);
            panelNavigation.Controls.Add(buttonPrev);
            panelNavigation.Location = new Point(9, 12);
            panelNavigation.Name = "panelNavigation";
            panelNavigation.Size = new Size(488, 59);
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
            _loadingIndicator.Anchor = AnchorStyles.None;
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
            pictureBoxPdf.Size = new Size(1049, 887);
            pictureBoxPdf.SizeMode = PictureBoxSizeMode.Zoom;
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
            // panelLeftContainer
            // 
            panelLeftContainer.Controls.Add(tabControlLeft);
            panelLeftContainer.Controls.Add(buttonOpenFolder);
            panelLeftContainer.Dock = DockStyle.Fill;
            panelLeftContainer.Location = new Point(0, 0);
            panelLeftContainer.Name = "panelLeftContainer";
            panelLeftContainer.Size = new Size(343, 887);
            panelLeftContainer.TabIndex = 0;
            // 
            // tabControlLeft
            // 
            tabControlLeft.Controls.Add(tabPageThumbnails);
            tabControlLeft.Controls.Add(tabPageTranslate);
            tabControlLeft.Controls.Add(tabPageFiles);
            tabControlLeft.Dock = DockStyle.Fill;
            tabControlLeft.Location = new Point(0, 35);
            tabControlLeft.Name = "tabControlLeft";
            tabControlLeft.SelectedIndex = 0;
            tabControlLeft.Size = new Size(343, 852);
            tabControlLeft.TabIndex = 1;
            // 
            // tabPageThumbnails
            // 
            tabPageThumbnails.Controls.Add(panelThumbnails);
            tabPageThumbnails.Location = new Point(4, 26);
            tabPageThumbnails.Name = "tabPageThumbnails";
            tabPageThumbnails.Padding = new Padding(3);
            tabPageThumbnails.Size = new Size(335, 822);
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
            panelThumbnails.Size = new Size(329, 816);
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
            flowLayoutPanelThumbnails.Size = new Size(329, 816);
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
            tabPageTranslate.Size = new Size(335, 822);
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
            groupBoxProgress.Size = new Size(329, 523);
            groupBoxProgress.TabIndex = 24;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Location = new Point(9, 325);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ReadOnly = true;
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(314, 181);
            textBoxTranslation.TabIndex = 5;
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Location = new Point(9, 42);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(314, 195);
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
            groupBoxLanguage.Location = new Point(3, 532);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(329, 287);
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
            richTextBoxAiAnswer.Location = new Point(9, 120);
            richTextBoxAiAnswer.Name = "richTextBoxAiAnswer";
            richTextBoxAiAnswer.ReadOnly = true;
            richTextBoxAiAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAiAnswer.Size = new Size(314, 161);
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
            textBoxQuestion.Location = new Point(9, 51);
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.Size = new Size(305, 23);
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
            tabPageFiles.Size = new Size(335, 822);
            tabPageFiles.TabIndex = 0;
            tabPageFiles.Text = "📁 目录";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // treeViewFiles
            // 
            treeViewFiles.Dock = DockStyle.Fill;
            treeViewFiles.Location = new Point(3, 3);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(329, 816);
            treeViewFiles.TabIndex = 0;
            treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // buttonOpenFolder
            // 
            buttonOpenFolder.Dock = DockStyle.Top;
            buttonOpenFolder.Location = new Point(0, 0);
            buttonOpenFolder.Name = "buttonOpenFolder";
            buttonOpenFolder.Size = new Size(343, 35);
            buttonOpenFolder.TabIndex = 0;
            buttonOpenFolder.Text = "📁 选择文件夹";
            buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // PdfReaderForm
            // 
            Controls.Add(splitContainer1);
            Name = "PdfReaderForm";
            Size = new Size(1396, 887);
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

                int leftBoundary = panelLeftContainer?.Width ?? 0;
                int rightBoundary = ClientSize.Width - panelNavigation.Width;
                int toolWidth = tabControlLeft?.Width ?? 0;
                rightBoundary -= toolWidth;

                rightBoundary = Math.Max(leftBoundary, rightBoundary);

                _logger.LogInformation($"拖动调试 - LeftBoundary:{leftBoundary}, RightBoundary:{rightBoundary}, ToolWidth:{toolWidth}, ClientWidth:{ClientSize.Width}, PanelWidth:{panelNavigation.Width}");

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
            if (e.Button == MouseButtons.Left)
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
                    pictureBoxPdf.Invalidate();
                    return;
                }

                _isSelecting = true;
                _selectStart = e.Location;
                _selectEnd = e.Location;
                pictureBoxPdf.Invalidate();
            }
        }

        private void PictureBoxPdf_MouseMove(object? sender, MouseEventArgs e)
        {
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
            }
        }


        private void PictureBoxPdf_MouseUp(object? sender, MouseEventArgs e)
        {
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
                pictureBoxPdf.Invalidate();
                return;
            }
            if (_isSelecting)
            {
                _isSelecting = false;
                _selectEnd = e.Location;
                _lastSelectionRect = GetSelectionRectangle(_selectStart, _selectEnd);
                pictureBoxPdf.Invalidate();
                SelectOcrClicked?.Invoke(this, EventArgs.Empty);
            }
        }

        private void PictureBoxPdf_MouseWheel(object? sender, MouseEventArgs e)
        {
            if (ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0) _zoomLevel = Math.Min(400, _zoomLevel + 10);
                else _zoomLevel = Math.Max(10, _zoomLevel - 10);
                if (_presenter != null)
                {
                    var page = int.TryParse(textBoxPage.Text, out var p) ? p - 1 : 0;
                    int targetW = (int)(pictureBoxPdf.ClientSize.Width * _zoomLevel / 100.0);
                    int targetH = (int)(pictureBoxPdf.ClientSize.Height * _zoomLevel / 100.0);
                    try
                    {
                        var bmp = _presenter.RenderPageToBitmap(page, Math.Max(1, targetW), Math.Max(1, targetH));
                        if (bmp != null)
                        {
                            var old = pictureBoxPdf.Image;
                            pictureBoxPdf.Image = bmp;
                            old?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error rendering page during zoom");
                    }
                }
            }
            else
            {
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

        private void PictureBoxPdf_Paint(object? sender, PaintEventArgs e)
        {
            if (_isSelecting)
            {
                var rect = GetSelectionRectangle(_selectStart, _selectEnd);
                using var brush = new SolidBrush(Color.FromArgb(80, Color.Yellow));
                e.Graphics.FillRectangle(brush, rect);
                using var pen = new Pen(Color.Orange, 2);
                e.Graphics.DrawRectangle(pen, rect);
            }
            else if (_isDrawing)
            {
                using var pen = new Pen(Color.Red, 4f);
                e.Graphics.DrawLine(pen, _selectStart, _selectEnd);
            }

            if (!string.IsNullOrEmpty(_currentPdfPath) && pictureBoxPdf?.Image != null)
            {
                DrawHighlights(e.Graphics);
            }
        }

        private void DrawHighlights(Graphics g)
        {
            if (string.IsNullOrEmpty(_currentPdfPath) || pictureBoxPdf?.Image == null) return;

            var imgRect = GetImageDisplayRect();
            var highlights = _highlightService.GetHighlightsForPage(_currentPdfPath, _currentPageIndex);

            foreach (var highlight in highlights)
            {
                var color = HighlightService.GetHighlightColor(highlight.Color);
                using var brush = new SolidBrush(color);
                var rect = new RectangleF(
                    imgRect.X + highlight.X * imgRect.Width / (imgRect.Width > 0 ? imgRect.Width : 1),
                    imgRect.Y + highlight.Y * imgRect.Height / (imgRect.Height > 0 ? imgRect.Height : 1),
                    highlight.Width,
                    highlight.Height
                );
                g.FillRectangle(brush, rect);

                if (!string.IsNullOrEmpty(highlight.Note))
                {
                    using var font = new Font("Microsoft YaHei UI", 10F);
                    using var textBrush = new SolidBrush(Color.Black);
                    g.DrawString("📝", font, textBrush, rect.Location);
                }
            }
        }

        private void EnsureAnnotationBitmap()
        {
            if (pictureBoxPdf.Image == null)
                return;

            if (_annotationBitmap != null)
            {
                if (_annotationBitmap.Width != pictureBoxPdf.Image.Width ||
                    _annotationBitmap.Height != pictureBoxPdf.Image.Height)
                {
                    _annotationGraphics?.Dispose();
                    _annotationBitmap?.Dispose();
                    _annotationGraphics = null;
                    _annotationBitmap = null;
                }
            }

            if (_annotationBitmap == null)
            {
                _annotationBitmap = new Bitmap(pictureBoxPdf.Image.Width, pictureBoxPdf.Image.Height);
                _annotationGraphics = Graphics.FromImage(_annotationBitmap);
            }
        }

        private PointF ClientToImage(Point clientPt)
        {
            if (pictureBoxPdf.Image == null) return new PointF(clientPt.X, clientPt.Y);
            var scaleX = (float)pictureBoxPdf.Image.Width / pictureBoxPdf.ClientSize.Width;
            var scaleY = (float)pictureBoxPdf.Image.Height / pictureBoxPdf.ClientSize.Height;
            return new PointF(clientPt.X * scaleX, clientPt.Y * scaleY);
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
                _pen?.Dispose();
                _annotationGraphics?.Dispose();
                _annotationBitmap?.Dispose();

                if (pictureBoxPdf.Image != null)
                {
                    pictureBoxPdf.Image.Dispose();
                    pictureBoxPdf.Image = null;
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }


    }
}
