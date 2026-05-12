using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Views;
using UnifiedLearningAssistant.Views.UI;

namespace UnifiedLearningAssistant.Forms
{
    public partial class PdfReaderForm : UserControl, IPdfView
    {
        private PdfPresenter? _presenter;
        private readonly ILogger<PdfReaderForm> _logger;
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

        private bool _isNavPanelDragging = false;
        private Point _navPanelStartPoint = Point.Empty;
        // 新增功能：中等级 - UI响应性改进，添加加载指示器
        private LoadingIndicator? _loadingIndicator;
        // 新增功能：低优先级 - PDF搜索和高亮
        private Panel? _searchPanel;
        private TextBox? _searchTextBox;
        private Label? _searchResultLabel;
        private Button? _searchNextButton;
        private Button? _searchPrevButton;
        private Button? _searchCloseButton;
        private SplitContainer? _splitContainer;
        private string _currentSearchText = "";
        private List<int> _searchResults = new List<int>();
        private int _currentSearchIndex = -1;
        // 新增功能：低优先级 - 夜间模式
        private bool _isNightMode = false;
        private SplitContainer splitContainer1;

        // 新增功能：OCR语言切换
        private string _currentLanguage = "eng";

        public PdfReaderForm(ILogger<PdfReaderForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Load += PdfReaderForm_Load;
            Resize += PdfReaderForm_Resize;
            KeyDown += PdfReaderForm_KeyDown;
        }

        private void PdfReaderForm_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();

            // 加载完成后通知 presenter 加载上次会话
            _presenter?.LoadLastSessionAndRestore();
        }

        private void PdfReaderForm_KeyDown(object? sender, KeyEventArgs e)
        {
            // 新增功能：低优先级 - Ctrl+F 快捷键打开搜索面板
            if (e.Control && e.KeyCode == Keys.F)
            {
                ToggleSearchPanel?.Invoke(this, EventArgs.Empty);
                SetSearchPanelVisible(true);
                e.Handled = true;
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
            textBoxPage.Text = (pageIndex + 1).ToString();
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

        public void ShowTranslationDialog(string original, string translation, string grammar)
        {
            var dialog = new TranslationDialog(original, translation);
            dialog.AddToLearningList += (s, e) =>
            {
                textBoxQuestion.Text = original;
                AddWordToLearningList?.Invoke(this, EventArgs.Empty);
            };
            dialog.AskAi += (s, text) =>
            {
                textBoxQuestion.Text = text;
                AiQuestionAsked?.Invoke(this, EventArgs.Empty);
            };
            dialog.SpeakText += (s, text) =>
            {
                SpeakTranslation?.Invoke(this, EventArgs.Empty);
            };
            dialog.ShowDialog();
        }

        public void UpdateAiAnswer(string answer)
        {
            richTextBoxAiAnswer.Text = answer;
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

        // 新增功能：低优先级 - PDF搜索和高亮
        public void UpdateSearchResultCount(int count)
        {
            if (_searchResultLabel != null)
            {
                _searchResultLabel.Text = count > 0 ? $"找到 {count} 处" : "未找到";
            }
        }

        public void SetSearchPanelVisible(bool visible)
        {
            if (_searchPanel != null)
            {
                _searchPanel.Visible = visible;
                if (visible && _searchTextBox != null)
                {
                    _searchTextBox.Focus();
                }
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
                tabControlTools.BackColor = Color.FromArgb(40, 40, 40);
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
                tabControlTools.BackColor = Color.White;
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
                displayWidth = controlWidth;
                displayHeight = (int)(controlWidth / imageAspect);
                displayX = 0;
                displayY = 0;
            }
            else
            {
                displayHeight = controlHeight;
                displayWidth = (int)(controlHeight * imageAspect);
                displayY = 0;
                displayX = 0;
            }

            return new Rectangle(displayX, displayY, displayWidth, displayHeight);
        }

        public event EventHandler? FileSelected;
        public event EventHandler? PageChanged;
        public event EventHandler? OcrSelectionComplete;
        public event EventHandler? AiQuestionAsked;
        public event EventHandler? AddWordToLearningList;
        public event EventHandler? SpeakTranslation;
        public event EventHandler? SelectOcrClicked;
        public event EventHandler? TranslateClicked;
        public event EventHandler<string>? SearchTextChanged;
        public event EventHandler? SearchNext;
        public event EventHandler? SearchPrevious;
        public event EventHandler? ToggleSearchPanel;
        public event EventHandler? ToggleNightMode;
        public event EventHandler? LanguageChanged;

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TreeView treeViewFiles;
        private Panel panelPdf;
        private PictureBox pictureBoxPdf;
        private Panel panelThumbnails;
        private FlowLayoutPanel flowLayoutPanelThumbnails;
        private TabControl tabControlTools;
        private TabPage tabPageOcr;
        private Button buttonSelectOcr;
        private TextBox textBoxOcrResult;
        private Label labelOcr;
        private TabPage tabPageTranslate;
        private TextBox textBoxOriginal;
        private Label labelOriginal;
        private Label labelTranslation;
        private TextBox textBoxTranslation;
        private Button buttonTranslate;
        private Button buttonSpeakTranslation;
        private TabPage tabPageAi;
        private TextBox textBoxQuestion;
        private Button buttonAskAi;
        private RichTextBox richTextBoxAiAnswer;
        private Button buttonAddToLearning;
        private Button buttonSpeakAnswer;
        private Label labelQuestion;
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
            _splitContainer = new SplitContainer();
            panelLeftContainer = new Panel();
            tabControlLeft = new TabControl();
            tabPageFiles = new TabPage();
            treeViewFiles = new TreeView();
            tabPageThumbnails = new TabPage();
            panelThumbnails = new Panel();
            flowLayoutPanelThumbnails = new FlowLayoutPanel();
            buttonOpenFolder = new Button();
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
            _searchPanel = new Panel();
            _searchCloseButton = new Button();
            _searchNextButton = new Button();
            _searchTextBox = new TextBox();
            _searchPrevButton = new Button();
            _searchResultLabel = new Label();
            pictureBoxPdf = new PictureBox();
            tabControlTools = new TabControl();
            tabPageOcr = new TabPage();
            buttonSelectOcr = new Button();
            labelOcr = new Label();
            textBoxOcrResult = new TextBox();
            tabPageTranslate = new TabPage();
            labelOriginal = new Label();
            textBoxOriginal = new TextBox();
            labelTranslation = new Label();
            buttonTranslate = new Button();
            buttonSpeakTranslation = new Button();
            textBoxTranslation = new TextBox();
            tabPageAi = new TabPage();
            labelQuestion = new Label();
            textBoxQuestion = new TextBox();
            buttonAskAi = new Button();
            buttonAddToLearning = new Button();
            buttonSpeakAnswer = new Button();
            richTextBoxAiAnswer = new RichTextBox();
            ((System.ComponentModel.ISupportInitialize)_splitContainer).BeginInit();
            _splitContainer.Panel1.SuspendLayout();
            _splitContainer.Panel2.SuspendLayout();
            _splitContainer.SuspendLayout();
            panelLeftContainer.SuspendLayout();
            tabControlLeft.SuspendLayout();
            tabPageFiles.SuspendLayout();
            tabPageThumbnails.SuspendLayout();
            panelThumbnails.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            panelPdf.SuspendLayout();
            panelNavigation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).BeginInit();
            _searchPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).BeginInit();
            tabControlTools.SuspendLayout();
            tabPageOcr.SuspendLayout();
            tabPageTranslate.SuspendLayout();
            tabPageAi.SuspendLayout();
            SuspendLayout();
            // 
            // _splitContainer
            // 
            _splitContainer.Dock = DockStyle.Fill;
            _splitContainer.Location = new Point(0, 0);
            _splitContainer.Name = "_splitContainer";
            // 
            // _splitContainer.Panel1
            // 
            _splitContainer.Panel1.Controls.Add(panelLeftContainer);
            // 
            // _splitContainer.Panel2
            // 
            _splitContainer.Panel2.Controls.Add(splitContainer1);
            _splitContainer.Size = new Size(1396, 887);
            _splitContainer.SplitterDistance = 257;
            _splitContainer.TabIndex = 5;
            // 
            // panelLeftContainer
            // 
            panelLeftContainer.Controls.Add(tabControlLeft);
            panelLeftContainer.Controls.Add(buttonOpenFolder);
            panelLeftContainer.Dock = DockStyle.Fill;
            panelLeftContainer.Location = new Point(0, 0);
            panelLeftContainer.Name = "panelLeftContainer";
            panelLeftContainer.Size = new Size(257, 887);
            panelLeftContainer.TabIndex = 0;
            // 
            // tabControlLeft
            // 
            tabControlLeft.Controls.Add(tabPageFiles);
            tabControlLeft.Controls.Add(tabPageThumbnails);
            tabControlLeft.Dock = DockStyle.Fill;
            tabControlLeft.Location = new Point(0, 35);
            tabControlLeft.Name = "tabControlLeft";
            tabControlLeft.SelectedIndex = 0;
            tabControlLeft.Size = new Size(257, 852);
            tabControlLeft.TabIndex = 1;
            // 
            // tabPageFiles
            // 
            tabPageFiles.Controls.Add(treeViewFiles);
            tabPageFiles.Location = new Point(4, 26);
            tabPageFiles.Name = "tabPageFiles";
            tabPageFiles.Padding = new Padding(3);
            tabPageFiles.Size = new Size(249, 822);
            tabPageFiles.TabIndex = 0;
            tabPageFiles.Text = "📁 目录";
            tabPageFiles.UseVisualStyleBackColor = true;
            // 
            // treeViewFiles
            // 
            treeViewFiles.Dock = DockStyle.Fill;
            treeViewFiles.Location = new Point(3, 3);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(243, 816);
            treeViewFiles.TabIndex = 0;
            treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // tabPageThumbnails
            // 
            tabPageThumbnails.Controls.Add(panelThumbnails);
            tabPageThumbnails.Location = new Point(4, 26);
            tabPageThumbnails.Name = "tabPageThumbnails";
            tabPageThumbnails.Padding = new Padding(3);
            tabPageThumbnails.Size = new Size(249, 822);
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
            panelThumbnails.Size = new Size(243, 816);
            panelThumbnails.TabIndex = 0;
            // 
            // flowLayoutPanelThumbnails
            // 
            flowLayoutPanelThumbnails.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            flowLayoutPanelThumbnails.AutoScroll = true;
            flowLayoutPanelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
            flowLayoutPanelThumbnails.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelThumbnails.Location = new Point(0, 0);
            flowLayoutPanelThumbnails.Name = "flowLayoutPanelThumbnails";
            flowLayoutPanelThumbnails.Size = new Size(170, 783);
            flowLayoutPanelThumbnails.TabIndex = 0;
            flowLayoutPanelThumbnails.WrapContents = false;
            // 
            // buttonOpenFolder
            // 
            buttonOpenFolder.Dock = DockStyle.Top;
            buttonOpenFolder.Location = new Point(0, 0);
            buttonOpenFolder.Name = "buttonOpenFolder";
            buttonOpenFolder.Size = new Size(257, 35);
            buttonOpenFolder.TabIndex = 0;
            buttonOpenFolder.Text = "📁 选择文件夹";
            buttonOpenFolder.Click += ButtonOpenFolder_Click;
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
            splitContainer1.Panel2.Controls.Add(tabControlTools);
            splitContainer1.Size = new Size(1135, 887);
            splitContainer1.SplitterDistance = 854;
            splitContainer1.TabIndex = 5;
            // 
            // panelPdf
            // 
            panelPdf.Controls.Add(panelNavigation);
            panelPdf.Controls.Add(_searchPanel);
            panelPdf.Controls.Add(pictureBoxPdf);
            panelPdf.Dock = DockStyle.Fill;
            panelPdf.Location = new Point(0, 0);
            panelPdf.Name = "panelPdf";
            panelPdf.Size = new Size(854, 887);
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
            panelNavigation.Location = new Point(0, 50);
            panelNavigation.Name = "panelNavigation";
            panelNavigation.Size = new Size(546, 63);
            panelNavigation.TabIndex = 3;
            panelNavigation.MouseDown += PanelNavigation_MouseDown;
            panelNavigation.MouseMove += PanelNavigation_MouseMove;
            panelNavigation.MouseUp += PanelNavigation_MouseUp;
            // 
            // trackBarZoom
            // 
            trackBarZoom.Location = new Point(165, 5);
            trackBarZoom.Maximum = 200;
            trackBarZoom.Minimum = 50;
            trackBarZoom.Name = "trackBarZoom";
            trackBarZoom.Size = new Size(150, 45);
            trackBarZoom.TabIndex = 8;
            trackBarZoom.Value = 100;
            // 
            // labelZoom
            // 
            labelZoom.Location = new Point(325, 17);
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
            _loadingIndicator.Location = new Point(467, 10);
            _loadingIndicator.Name = "_loadingIndicator";
            _loadingIndicator.Size = new Size(58, 35);
            _loadingIndicator.TabIndex = 2;
            _loadingIndicator.Visible = false;
            // 
            // buttonLanguage
            // 
            buttonLanguage.BackColor = Color.White;
            buttonLanguage.FlatStyle = FlatStyle.Flat;
            buttonLanguage.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            buttonLanguage.Location = new Point(380, 10);
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
            buttonNightMode.Location = new Point(426, 10);
            buttonNightMode.Name = "buttonNightMode";
            buttonNightMode.Size = new Size(35, 35);
            buttonNightMode.TabIndex = 6;
            buttonNightMode.Text = "🌙";
            buttonNightMode.UseVisualStyleBackColor = false;
            buttonNightMode.Click += ButtonNightMode_Click;
            // 
            // buttonNext
            // 
            buttonNext.Location = new Point(124, 13);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(35, 28);
            buttonNext.TabIndex = 4;
            buttonNext.Text = "▶";
            buttonNext.Click += ButtonNext_Click;
            // 
            // labelPageCount
            // 
            labelPageCount.Location = new Point(89, 17);
            labelPageCount.Name = "labelPageCount";
            labelPageCount.Size = new Size(38, 20);
            labelPageCount.TabIndex = 3;
            labelPageCount.Text = "/ 1";
            // 
            // textBoxPage
            // 
            textBoxPage.Location = new Point(54, 16);
            textBoxPage.Name = "textBoxPage";
            textBoxPage.Size = new Size(30, 23);
            textBoxPage.TabIndex = 2;
            textBoxPage.Text = "1";
            // 
            // buttonPrev
            // 
            buttonPrev.Location = new Point(19, 13);
            buttonPrev.Name = "buttonPrev";
            buttonPrev.Size = new Size(30, 28);
            buttonPrev.TabIndex = 1;
            buttonPrev.Text = "◀";
            buttonPrev.Click += ButtonPrev_Click;
            // 
            // _searchPanel
            // 
            _searchPanel.BackColor = Color.White;
            _searchPanel.BorderStyle = BorderStyle.FixedSingle;
            _searchPanel.Controls.Add(_searchCloseButton);
            _searchPanel.Controls.Add(_searchNextButton);
            _searchPanel.Controls.Add(_searchTextBox);
            _searchPanel.Controls.Add(_searchPrevButton);
            _searchPanel.Controls.Add(_searchResultLabel);
            _searchPanel.Location = new Point(0, 0);
            _searchPanel.Name = "_searchPanel";
            _searchPanel.Size = new Size(608, 45);
            _searchPanel.TabIndex = 0;
            // 
            // _searchCloseButton
            // 
            _searchCloseButton.BackColor = Color.White;
            _searchCloseButton.FlatStyle = FlatStyle.Flat;
            _searchCloseButton.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            _searchCloseButton.Location = new Point(552, 6);
            _searchCloseButton.Name = "_searchCloseButton";
            _searchCloseButton.Size = new Size(25, 28);
            _searchCloseButton.TabIndex = 4;
            _searchCloseButton.Text = "×";
            _searchCloseButton.UseVisualStyleBackColor = false;
            _searchCloseButton.Click += SearchCloseButton_Click;
            // 
            // _searchNextButton
            // 
            _searchNextButton.Font = new Font("Microsoft YaHei UI", 8F);
            _searchNextButton.Location = new Point(466, 4);
            _searchNextButton.Name = "_searchNextButton";
            _searchNextButton.Size = new Size(80, 30);
            _searchNextButton.TabIndex = 3;
            _searchNextButton.Text = "下一页";
            _searchNextButton.Click += SearchNextButton_Click;
            // 
            // _searchTextBox
            // 
            _searchTextBox.Font = new Font("Microsoft YaHei UI", 9F);
            _searchTextBox.Location = new Point(13, 11);
            _searchTextBox.Name = "_searchTextBox";
            _searchTextBox.PlaceholderText = "输入搜索内容（Ctrl+F）";
            _searchTextBox.Size = new Size(269, 23);
            _searchTextBox.TabIndex = 0;
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            _searchTextBox.KeyDown += SearchTextBox_KeyDown;
            // 
            // _searchPrevButton
            // 
            _searchPrevButton.Font = new Font("Microsoft YaHei UI", 8F);
            _searchPrevButton.Location = new Point(376, 4);
            _searchPrevButton.Name = "_searchPrevButton";
            _searchPrevButton.Size = new Size(80, 30);
            _searchPrevButton.TabIndex = 2;
            _searchPrevButton.Text = "上一页";
            _searchPrevButton.Click += SearchPrevButton_Click;
            // 
            // _searchResultLabel
            // 
            _searchResultLabel.Font = new Font("Microsoft YaHei UI", 8F);
            _searchResultLabel.Location = new Point(288, 13);
            _searchResultLabel.Name = "_searchResultLabel";
            _searchResultLabel.Size = new Size(80, 20);
            _searchResultLabel.TabIndex = 1;
            _searchResultLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // pictureBoxPdf
            // 
            pictureBoxPdf.Dock = DockStyle.Fill;
            pictureBoxPdf.Location = new Point(0, 0);
            pictureBoxPdf.Name = "pictureBoxPdf";
            pictureBoxPdf.Size = new Size(854, 887);
            pictureBoxPdf.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPdf.TabIndex = 1;
            pictureBoxPdf.TabStop = false;
            pictureBoxPdf.Paint += PictureBoxPdf_Paint;
            pictureBoxPdf.MouseDoubleClick += PictureBoxPdf_MouseDoubleClick;
            pictureBoxPdf.MouseDown += PictureBoxPdf_MouseDown;
            pictureBoxPdf.MouseMove += PictureBoxPdf_MouseMove;
            pictureBoxPdf.MouseUp += PictureBoxPdf_MouseUp;
            pictureBoxPdf.MouseWheel += PictureBoxPdf_MouseWheel;
            // 
            // tabControlTools
            // 
            tabControlTools.Controls.Add(tabPageOcr);
            tabControlTools.Controls.Add(tabPageTranslate);
            tabControlTools.Controls.Add(tabPageAi);
            tabControlTools.Dock = DockStyle.Fill;
            tabControlTools.Location = new Point(0, 0);
            tabControlTools.Name = "tabControlTools";
            tabControlTools.SelectedIndex = 0;
            tabControlTools.Size = new Size(277, 887);
            tabControlTools.TabIndex = 4;
            // 
            // tabPageOcr
            // 
            tabPageOcr.Controls.Add(buttonSelectOcr);
            tabPageOcr.Controls.Add(labelOcr);
            tabPageOcr.Controls.Add(textBoxOcrResult);
            tabPageOcr.Location = new Point(4, 26);
            tabPageOcr.Name = "tabPageOcr";
            tabPageOcr.Padding = new Padding(3);
            tabPageOcr.Size = new Size(269, 857);
            tabPageOcr.TabIndex = 0;
            tabPageOcr.Text = "OCR划词";
            // 
            // buttonSelectOcr
            // 
            buttonSelectOcr.Location = new Point(15, 15);
            buttonSelectOcr.Name = "buttonSelectOcr";
            buttonSelectOcr.Size = new Size(260, 35);
            buttonSelectOcr.TabIndex = 2;
            buttonSelectOcr.Text = "框选区域开始识别";
            buttonSelectOcr.Click += ButtonSelectOcr_Click;
            // 
            // labelOcr
            // 
            labelOcr.Location = new Point(15, 60);
            labelOcr.Name = "labelOcr";
            labelOcr.Size = new Size(260, 20);
            labelOcr.TabIndex = 0;
            labelOcr.Text = "识别结果:";
            // 
            // textBoxOcrResult
            // 
            textBoxOcrResult.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxOcrResult.Location = new Point(15, 85);
            textBoxOcrResult.Multiline = true;
            textBoxOcrResult.Name = "textBoxOcrResult";
            textBoxOcrResult.ReadOnly = true;
            textBoxOcrResult.ScrollBars = ScrollBars.Vertical;
            textBoxOcrResult.Size = new Size(237, 756);
            textBoxOcrResult.TabIndex = 1;
            // 
            // tabPageTranslate
            // 
            tabPageTranslate.Controls.Add(labelOriginal);
            tabPageTranslate.Controls.Add(textBoxOriginal);
            tabPageTranslate.Controls.Add(labelTranslation);
            tabPageTranslate.Controls.Add(buttonTranslate);
            tabPageTranslate.Controls.Add(buttonSpeakTranslation);
            tabPageTranslate.Controls.Add(textBoxTranslation);
            tabPageTranslate.Location = new Point(4, 26);
            tabPageTranslate.Name = "tabPageTranslate";
            tabPageTranslate.Padding = new Padding(3);
            tabPageTranslate.Size = new Size(269, 857);
            tabPageTranslate.TabIndex = 1;
            tabPageTranslate.Text = "翻译结果";
            // 
            // labelOriginal
            // 
            labelOriginal.Location = new Point(15, 15);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(260, 20);
            labelOriginal.TabIndex = 0;
            labelOriginal.Text = "原文:";
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Location = new Point(15, 40);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(260, 70);
            textBoxOriginal.TabIndex = 1;
            // 
            // labelTranslation
            // 
            labelTranslation.Location = new Point(15, 120);
            labelTranslation.Name = "labelTranslation";
            labelTranslation.Size = new Size(260, 20);
            labelTranslation.TabIndex = 2;
            labelTranslation.Text = "译文:";
            // 
            // buttonTranslate
            // 
            buttonTranslate.Location = new Point(15, 150);
            buttonTranslate.Name = "buttonTranslate";
            buttonTranslate.Size = new Size(120, 30);
            buttonTranslate.TabIndex = 3;
            buttonTranslate.Text = "翻译";
            buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // buttonSpeakTranslation
            // 
            buttonSpeakTranslation.Location = new Point(145, 150);
            buttonSpeakTranslation.Name = "buttonSpeakTranslation";
            buttonSpeakTranslation.Size = new Size(130, 30);
            buttonSpeakTranslation.TabIndex = 4;
            buttonSpeakTranslation.Text = "朗读译文";
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxTranslation.Location = new Point(15, 190);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ReadOnly = true;
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(237, 651);
            textBoxTranslation.TabIndex = 5;
            // 
            // tabPageAi
            // 
            tabPageAi.Controls.Add(labelQuestion);
            tabPageAi.Controls.Add(textBoxQuestion);
            tabPageAi.Controls.Add(buttonAskAi);
            tabPageAi.Controls.Add(buttonAddToLearning);
            tabPageAi.Controls.Add(buttonSpeakAnswer);
            tabPageAi.Controls.Add(richTextBoxAiAnswer);
            tabPageAi.Location = new Point(4, 26);
            tabPageAi.Name = "tabPageAi";
            tabPageAi.Padding = new Padding(3);
            tabPageAi.Size = new Size(269, 857);
            tabPageAi.TabIndex = 2;
            tabPageAi.Text = "AI提问";
            // 
            // labelQuestion
            // 
            labelQuestion.Location = new Point(15, 15);
            labelQuestion.Name = "labelQuestion";
            labelQuestion.Size = new Size(260, 20);
            labelQuestion.TabIndex = 0;
            labelQuestion.Text = "问题:";
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.Location = new Point(15, 40);
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.Size = new Size(260, 23);
            textBoxQuestion.TabIndex = 1;
            // 
            // buttonAskAi
            // 
            buttonAskAi.Location = new Point(165, 70);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(110, 30);
            buttonAskAi.TabIndex = 2;
            buttonAskAi.Text = "向AI提问";
            buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // buttonAddToLearning
            // 
            buttonAddToLearning.Location = new Point(15, 70);
            buttonAddToLearning.Name = "buttonAddToLearning";
            buttonAddToLearning.Size = new Size(135, 30);
            buttonAddToLearning.TabIndex = 3;
            buttonAddToLearning.Text = "添加到生词本";
            buttonAddToLearning.Click += ButtonAddToLearning_Click;
            // 
            // buttonSpeakAnswer
            // 
            buttonSpeakAnswer.Location = new Point(165, 105);
            buttonSpeakAnswer.Name = "buttonSpeakAnswer";
            buttonSpeakAnswer.Size = new Size(110, 30);
            buttonSpeakAnswer.TabIndex = 4;
            buttonSpeakAnswer.Text = "朗读答案";
            // 
            // richTextBoxAiAnswer
            // 
            richTextBoxAiAnswer.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            richTextBoxAiAnswer.Location = new Point(15, 145);
            richTextBoxAiAnswer.Name = "richTextBoxAiAnswer";
            richTextBoxAiAnswer.ReadOnly = true;
            richTextBoxAiAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAiAnswer.Size = new Size(237, 696);
            richTextBoxAiAnswer.TabIndex = 5;
            richTextBoxAiAnswer.Text = "";
            // 
            // PdfReaderForm
            // 
            Controls.Add(_splitContainer);
            Name = "PdfReaderForm";
            Size = new Size(1396, 887);
            _splitContainer.Panel1.ResumeLayout(false);
            _splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)_splitContainer).EndInit();
            _splitContainer.ResumeLayout(false);
            panelLeftContainer.ResumeLayout(false);
            tabControlLeft.ResumeLayout(false);
            tabPageFiles.ResumeLayout(false);
            tabPageThumbnails.ResumeLayout(false);
            panelThumbnails.ResumeLayout(false);
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            panelPdf.ResumeLayout(false);
            panelNavigation.ResumeLayout(false);
            panelNavigation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).EndInit();
            _searchPanel.ResumeLayout(false);
            _searchPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).EndInit();
            tabControlTools.ResumeLayout(false);
            tabPageOcr.ResumeLayout(false);
            tabPageOcr.PerformLayout();
            tabPageTranslate.ResumeLayout(false);
            tabPageTranslate.PerformLayout();
            tabPageAi.ResumeLayout(false);
            tabPageAi.PerformLayout();
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
                int rightBoundary = ClientSize.Width - panelNavigation.Width - (tabControlTools?.Width ?? 0);

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

        private void PictureBoxPdf_MouseDoubleClick(object? sender, MouseEventArgs e)
        {
            try
            {
                if (_presenter == null)
                {
                    _logger.LogWarning("双击识别失败: _presenter 为 null");
                    ShowWarning("演示器未初始化");
                    return;
                }

                if (pictureBoxPdf.Image == null)
                {
                    _logger.LogWarning("双击识别失败: pictureBoxPdf.Image 为 null");
                    ShowWarning("没有可识别的图像");
                    return;
                }

                var img = pictureBoxPdf.Image as Bitmap;
                if (img == null)
                {
                    _logger.LogWarning("双击识别失败: 图像不是 Bitmap 类型");
                    ShowWarning("图像格式不支持");
                    return;
                }

                if (img.Width == 0 || img.Height == 0)
                {
                    _logger.LogWarning("双击识别失败: 图像尺寸无效");
                    ShowWarning("图像尺寸无效");
                    return;
                }

                var a = async () => { await OcrFullImageAsync(img); };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "双击识别失败");
                ShowError("双击识别失败: " + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private async Task OcrFullImageAsync(Bitmap img)
        {
            if (_presenter == null)
                return;

            try
            {
                _logger.LogInformation($"开始OCR识别，图像尺寸: {img.Width} x {img.Height}");

                var recognizedText = await _presenter.OcrBitmapAsync(img);

                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    _logger.LogInformation($"OCR识别成功，识别到 {recognizedText.Length} 个字符");
                    textBoxOcrResult.Text = recognizedText;
                    textBoxOriginal.Text = recognizedText;
                    tabControlTools.SelectedTab = tabControlTools.TabPages["tabPageOcr"];
                }
                else
                {
                    _logger.LogInformation("OCR识别完成，未识别到文字");
                    ShowWarning("未识别到文字");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR识别失败");
                ShowError("OCR识别失败: " + ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private void PictureBoxPdf_MouseUp(object? sender, MouseEventArgs e)
        {
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
            AddWordToLearningList?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonPrev_Click(object? sender, EventArgs e)
        {
            if (_presenter == null) return;
            var currentPage = int.TryParse(textBoxPage.Text, out var p) ? p : 1;
            if (currentPage > 1)
            {
                _presenter.RenderPage(currentPage - 2);
            }
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            if (_presenter == null) return;
            var currentPage = int.TryParse(textBoxPage.Text, out var p) ? p : 1;
            if (currentPage < _presenter.PageCount)
            {
                _presenter.RenderPage(currentPage);
            }
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

        private void SearchTextBox_TextChanged(object? sender, EventArgs e)
        {
            _currentSearchText = _searchTextBox?.Text ?? "";
            SearchTextChanged?.Invoke(this, _currentSearchText);
        }

        private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if ((e.Modifiers & Keys.Shift) == Keys.Shift)
                {
                    SearchPrevious?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    SearchNext?.Invoke(this, EventArgs.Empty);
                }
                e.Handled = true;
            }
        }

        private void SearchPrevButton_Click(object? sender, EventArgs e)
        {
            SearchPrevious?.Invoke(this, EventArgs.Empty);
        }

        private void SearchNextButton_Click(object? sender, EventArgs e)
        {
            SearchNext?.Invoke(this, EventArgs.Empty);
        }

        private void SearchCloseButton_Click(object? sender, EventArgs e)
        {
            SetSearchPanelVisible(false);
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
