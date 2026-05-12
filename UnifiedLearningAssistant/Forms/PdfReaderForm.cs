using System.Drawing.Drawing2D;
using Microsoft.Extensions.Logging;
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

        public PdfReaderForm(ILogger<PdfReaderForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Load += PdfReaderForm_Load;
            Resize += PdfReaderForm_Resize;
        }

        private void PdfReaderForm_Load(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
            // 加载完成后通知 presenter 加载上次会话
            _presenter?.LoadLastSessionAndRestore();
        }

        private void PdfReaderForm_Resize(object? sender, EventArgs e)
        {
            AdjustPanelPdfSize();
        }

        private void AdjustPanelPdfSize()
        {
            panelPdf.Height = ClientSize.Height;
            panelPdf.Width = ClientSize.Width - treeViewFiles.Width - tabControlTools.Width;
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
            if (flowLayoutPanelThumbnails == null) return;

            var panel = new Panel();
            panel.Size = new Size(100, 140);
            panel.Margin = new Padding(5);
            panel.BackColor = Color.White;
            panel.BorderStyle = BorderStyle.FixedSingle;
            panel.Tag = pageIndex;

            var pictureBox = new PictureBox();
            pictureBox.Image = thumbnail;
            pictureBox.Size = new Size(90, 115);
            pictureBox.Location = new Point(5, 5);
            pictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox.Tag = pageIndex;
            pictureBox.Click += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    RenderPage(idx);
                }
            };

            var label = new Label();
            label.Text = (pageIndex + 1).ToString();
            label.Location = new Point(5, 120);
            label.Size = new Size(90, 15);
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Font = new Font("Microsoft YaHei UI", 8F);

            panel.Controls.Add(pictureBox);
            panel.Controls.Add(label);
            panel.Click += (s, e) =>
            {
                if (s is Control c && c.Tag is int idx)
                {
                    RenderPage(idx);
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
                        panel.BackColor = Color.White;
                        panel.BorderStyle = BorderStyle.FixedSingle;
                    }
                }
            }
        }

        // 新增功能：中等级 - 私有方法用于触发页码更改事件
        private void RenderPage(int pageIndex)
        {
            PageChanged?.Invoke(this, EventArgs.Empty);
        }

        public string GetSelectedFile()
        {
            return treeViewFiles.SelectedNode?.Text ?? string.Empty;
        }

        public string GetPageText()
        {
            return textBoxPage.Text;
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

        public event EventHandler? FileSelected;
        public event EventHandler? PageChanged;
        public event EventHandler? OcrSelectionComplete;
        public event EventHandler? AiQuestionAsked;
        public event EventHandler? AddWordToLearningList;
        public event EventHandler? SpeakTranslation;
        public event EventHandler? SelectOcrClicked;
        public event EventHandler? TranslateClicked;

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TreeView treeViewFiles;
        private Panel panelPdf;
        private PictureBox pictureBoxPdf;
        // 新增功能：中等级 - PDF页面缩略图侧边栏
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
        private Panel panelNavigation;
        private Button buttonPrev;
        private TextBox textBoxPage;
        private Label labelPageCount;
        private Button buttonNext;
        private Button buttonOpenFolder;
        private Label labelZoom;
        private TrackBar trackBarZoom;
        private Label labelDrag;

        private void InitializeComponent()
        {
            treeViewFiles = new TreeView();
            panelPdf = new Panel();
            pictureBoxPdf = new PictureBox();
            // 新增功能：中等级 - PDF页面缩略图侧边栏
            panelThumbnails = new Panel();
            flowLayoutPanelThumbnails = new FlowLayoutPanel();
            tabControlTools = new TabControl();
            // 新增功能：中等级 - 加载指示器初始化
            _loadingIndicator = new LoadingIndicator();
            tabPageOcr = new TabPage();
            buttonSelectOcr = new Button();
            textBoxOcrResult = new TextBox();
            labelOcr = new Label();
            tabPageTranslate = new TabPage();
            buttonSpeakTranslation = new Button();
            buttonTranslate = new Button();
            textBoxTranslation = new TextBox();
            labelTranslation = new Label();
            textBoxOriginal = new TextBox();
            labelOriginal = new Label();
            tabPageAi = new TabPage();
            buttonSpeakAnswer = new Button();
            buttonAddToLearning = new Button();
            richTextBoxAiAnswer = new RichTextBox();
            buttonAskAi = new Button();
            textBoxQuestion = new TextBox();
            panelNavigation = new Panel();
            labelDrag = new Label();
            trackBarZoom = new TrackBar();
            labelZoom = new Label();
            buttonNext = new Button();
            labelPageCount = new Label();
            textBoxPage = new TextBox();
            buttonPrev = new Button();
            buttonOpenFolder = new Button();
            panelPdf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).BeginInit();
            tabControlTools.SuspendLayout();
            tabPageOcr.SuspendLayout();
            tabPageTranslate.SuspendLayout();
            tabPageAi.SuspendLayout();
            panelNavigation.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).BeginInit();
            SuspendLayout();
            // 
            // treeViewFiles
            // 
            treeViewFiles.Dock = DockStyle.Left;
            treeViewFiles.Location = new Point(0, 0);
            treeViewFiles.Name = "treeViewFiles";
            treeViewFiles.Size = new Size(150, 600);
            treeViewFiles.TabIndex = 0;
            treeViewFiles.AfterSelect += TreeViewFiles_AfterSelect;
            // 
            // panelThumbnails
            // 
            // 新增功能：中等级 - PDF页面缩略图侧边栏
            panelThumbnails.Dock = DockStyle.Left;
            panelThumbnails.Width = 120;
            panelThumbnails.BackColor = Color.FromArgb(240, 240, 240);
            panelThumbnails.AutoScroll = true;
            panelThumbnails.Controls.Add(flowLayoutPanelThumbnails);
            // 
            // flowLayoutPanelThumbnails
            // 
            flowLayoutPanelThumbnails.Dock = DockStyle.Fill;
            flowLayoutPanelThumbnails.AutoScroll = true;
            flowLayoutPanelThumbnails.FlowDirection = FlowDirection.TopDown;
            flowLayoutPanelThumbnails.WrapContents = false;
            // 
            // panelPdf
            // 
            panelPdf.Controls.Add(pictureBoxPdf);
            // 新增功能：中等级 - 添加加载指示器到panelPdf
            _loadingIndicator.Name = "loadingIndicator";
            _loadingIndicator.Size = new Size(60, 60);
            _loadingIndicator.Location = new Point(235, 270);
            _loadingIndicator.Anchor = AnchorStyles.None;
            _loadingIndicator.IsLoading = false;
            _loadingIndicator.Visible = false;
            panelPdf.Controls.Add(_loadingIndicator);
            panelPdf.Location = new Point(270, 0); // treeViewFiles(150) + panelThumbnails(120)
            panelPdf.Name = "panelPdf";
            panelPdf.Size = new Size(530, 600);
            panelPdf.TabIndex = 1;
            // 
            // pictureBoxPdf
            // 
            pictureBoxPdf.Dock = DockStyle.Fill;
            pictureBoxPdf.Location = new Point(0, 0);
            pictureBoxPdf.Name = "pictureBoxPdf";
            pictureBoxPdf.Size = new Size(600, 600);
            pictureBoxPdf.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxPdf.TabIndex = 0;
            pictureBoxPdf.TabStop = false;
            pictureBoxPdf.Paint += PictureBoxPdf_Paint;
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
            tabControlTools.Dock = DockStyle.Right;
            tabControlTools.Location = new Point(800, 0);
            tabControlTools.Name = "tabControlTools";
            tabControlTools.SelectedIndex = 0;
            tabControlTools.Size = new Size(300, 600);
            tabControlTools.TabIndex = 2;
            // 
            // tabPageOcr
            // 
            tabPageOcr.Controls.Add(buttonSelectOcr);
            tabPageOcr.Controls.Add(textBoxOcrResult);
            tabPageOcr.Controls.Add(labelOcr);
            tabPageOcr.Location = new Point(4, 26);
            tabPageOcr.Name = "tabPageOcr";
            tabPageOcr.Padding = new Padding(3);
            tabPageOcr.Size = new Size(292, 570);
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
            // textBoxOcrResult
            // 
            textBoxOcrResult.Location = new Point(15, 70);
            textBoxOcrResult.Multiline = true;
            textBoxOcrResult.Name = "textBoxOcrResult";
            textBoxOcrResult.ReadOnly = true;
            textBoxOcrResult.ScrollBars = ScrollBars.Vertical;
            textBoxOcrResult.Size = new Size(260, 100);
            textBoxOcrResult.TabIndex = 1;
            // 
            // labelOcr
            // 
            labelOcr.Location = new Point(15, 50);
            labelOcr.Name = "labelOcr";
            labelOcr.Size = new Size(260, 20);
            labelOcr.TabIndex = 0;
            labelOcr.Text = "识别结果:";
            // 
            // tabPageTranslate
            // 
            tabPageTranslate.Controls.Add(buttonSpeakTranslation);
            tabPageTranslate.Controls.Add(buttonTranslate);
            tabPageTranslate.Controls.Add(textBoxTranslation);
            tabPageTranslate.Controls.Add(labelTranslation);
            tabPageTranslate.Controls.Add(textBoxOriginal);
            tabPageTranslate.Controls.Add(labelOriginal);
            tabPageTranslate.Location = new Point(4, 26);
            tabPageTranslate.Name = "tabPageTranslate";
            tabPageTranslate.Padding = new Padding(3);
            tabPageTranslate.Size = new Size(292, 570);
            tabPageTranslate.TabIndex = 1;
            tabPageTranslate.Text = "翻译结果";
            // 
            // buttonSpeakTranslation
            // 
            buttonSpeakTranslation.Location = new Point(145, 140);
            buttonSpeakTranslation.Name = "buttonSpeakTranslation";
            buttonSpeakTranslation.Size = new Size(130, 30);
            buttonSpeakTranslation.TabIndex = 5;
            buttonSpeakTranslation.Text = "朗读译文";
            buttonSpeakTranslation.Click += ButtonSpeakTranslation_Click;
            // 
            // buttonTranslate
            // 
            buttonTranslate.Location = new Point(15, 140);
            buttonTranslate.Name = "buttonTranslate";
            buttonTranslate.Size = new Size(120, 30);
            buttonTranslate.TabIndex = 4;
            buttonTranslate.Text = "翻译";
            buttonTranslate.Click += ButtonTranslate_Click;
            // 
            // textBoxTranslation
            // 
            textBoxTranslation.Location = new Point(15, 95);
            textBoxTranslation.Multiline = true;
            textBoxTranslation.Name = "textBoxTranslation";
            textBoxTranslation.ReadOnly = true;
            textBoxTranslation.ScrollBars = ScrollBars.Vertical;
            textBoxTranslation.Size = new Size(260, 40);
            textBoxTranslation.TabIndex = 3;
            // 
            // labelTranslation
            // 
            labelTranslation.Location = new Point(15, 75);
            labelTranslation.Name = "labelTranslation";
            labelTranslation.Size = new Size(260, 20);
            labelTranslation.TabIndex = 2;
            labelTranslation.Text = "译文:";
            // 
            // textBoxOriginal
            // 
            textBoxOriginal.Location = new Point(15, 35);
            textBoxOriginal.Multiline = true;
            textBoxOriginal.Name = "textBoxOriginal";
            textBoxOriginal.ScrollBars = ScrollBars.Vertical;
            textBoxOriginal.Size = new Size(260, 35);
            textBoxOriginal.TabIndex = 1;
            // 
            // labelOriginal
            // 
            labelOriginal.Location = new Point(15, 15);
            labelOriginal.Name = "labelOriginal";
            labelOriginal.Size = new Size(260, 20);
            labelOriginal.TabIndex = 0;
            labelOriginal.Text = "原文:";
            // 
            // tabPageAi
            // 
            tabPageAi.Controls.Add(buttonSpeakAnswer);
            tabPageAi.Controls.Add(buttonAddToLearning);
            tabPageAi.Controls.Add(richTextBoxAiAnswer);
            tabPageAi.Controls.Add(buttonAskAi);
            tabPageAi.Controls.Add(textBoxQuestion);
            tabPageAi.Location = new Point(4, 26);
            tabPageAi.Name = "tabPageAi";
            tabPageAi.Padding = new Padding(3);
            tabPageAi.Size = new Size(292, 570);
            tabPageAi.TabIndex = 2;
            tabPageAi.Text = "AI提问";
            // 
            // buttonSpeakAnswer
            // 
            buttonSpeakAnswer.Location = new Point(145, 80);
            buttonSpeakAnswer.Name = "buttonSpeakAnswer";
            buttonSpeakAnswer.Size = new Size(130, 30);
            buttonSpeakAnswer.TabIndex = 4;
            buttonSpeakAnswer.Text = "朗读答案";
            // 
            // buttonAddToLearning
            // 
            buttonAddToLearning.Location = new Point(15, 80);
            buttonAddToLearning.Name = "buttonAddToLearning";
            buttonAddToLearning.Size = new Size(120, 30);
            buttonAddToLearning.TabIndex = 3;
            buttonAddToLearning.Text = "添加到生词本";
            buttonAddToLearning.Click += ButtonAddToLearning_Click;
            // 
            // richTextBoxAiAnswer
            // 
            richTextBoxAiAnswer.Location = new Point(15, 120);
            richTextBoxAiAnswer.Name = "richTextBoxAiAnswer";
            richTextBoxAiAnswer.ReadOnly = true;
            richTextBoxAiAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
            richTextBoxAiAnswer.Size = new Size(260, 440);
            richTextBoxAiAnswer.TabIndex = 2;
            richTextBoxAiAnswer.Text = "";
            // 
            // buttonAskAi
            // 
            buttonAskAi.Location = new Point(200, 45);
            buttonAskAi.Name = "buttonAskAi";
            buttonAskAi.Size = new Size(75, 30);
            buttonAskAi.TabIndex = 1;
            buttonAskAi.Text = "向AI提问";
            buttonAskAi.Click += ButtonAskAi_Click;
            // 
            // textBoxQuestion
            // 
            textBoxQuestion.Location = new Point(15, 15);
            textBoxQuestion.Name = "textBoxQuestion";
            textBoxQuestion.Size = new Size(260, 23);
            textBoxQuestion.TabIndex = 0;
            // 
            // panelNavigation
            // 
            panelNavigation.Controls.Add(labelDrag);
            panelNavigation.Controls.Add(trackBarZoom);
            panelNavigation.Controls.Add(labelZoom);
            panelNavigation.Controls.Add(buttonNext);
            panelNavigation.Controls.Add(labelPageCount);
            panelNavigation.Controls.Add(textBoxPage);
            panelNavigation.Controls.Add(buttonPrev);
            panelNavigation.Controls.Add(buttonOpenFolder);
            panelNavigation.Location = new Point(280, 10);
            panelNavigation.Name = "panelNavigation";
            panelNavigation.Size = new Size(480, 50);
            panelNavigation.TabIndex = 3;
            panelNavigation.MouseDown += PanelNavigation_MouseDown;
            panelNavigation.MouseMove += PanelNavigation_MouseMove;
            panelNavigation.MouseUp += PanelNavigation_MouseUp;
            // 
            // labelDrag
            // 
            labelDrag.AutoSize = true;
            labelDrag.BackColor = Color.IndianRed;
            labelDrag.Cursor = Cursors.SizeAll;
            labelDrag.Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 134);
            labelDrag.Location = new Point(522, 10);
            labelDrag.Name = "labelDrag";
            labelDrag.Size = new Size(25, 22);
            labelDrag.TabIndex = 8;
            labelDrag.Text = "↔";
            // 
            // trackBarZoom
            // 
            trackBarZoom.Location = new Point(320, 3);
            trackBarZoom.Maximum = 200;
            trackBarZoom.Minimum = 50;
            trackBarZoom.Name = "trackBarZoom";
            trackBarZoom.Size = new Size(150, 45);
            trackBarZoom.TabIndex = 6;
            trackBarZoom.Value = 100;
            // 
            // labelZoom
            // 
            labelZoom.Location = new Point(475, 18);
            labelZoom.Name = "labelZoom";
            labelZoom.Size = new Size(30, 20);
            labelZoom.TabIndex = 5;
            labelZoom.Text = "100%";
            // 
            // buttonNext
            // 
            buttonNext.Location = new Point(260, 12);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(30, 25);
            buttonNext.TabIndex = 4;
            buttonNext.Text = "▶";
            buttonNext.Click += ButtonNext_Click;
            // 
            // labelPageCount
            // 
            labelPageCount.Location = new Point(230, 15);
            labelPageCount.Name = "labelPageCount";
            labelPageCount.Size = new Size(25, 20);
            labelPageCount.TabIndex = 3;
            labelPageCount.Text = "/ 1";
            // 
            // textBoxPage
            // 
            textBoxPage.Location = new Point(200, 12);
            textBoxPage.Name = "textBoxPage";
            textBoxPage.Size = new Size(25, 23);
            textBoxPage.TabIndex = 2;
            textBoxPage.Text = "1";
            // 
            // buttonPrev
            // 
            buttonPrev.Location = new Point(170, 12);
            buttonPrev.Name = "buttonPrev";
            buttonPrev.Size = new Size(25, 25);
            buttonPrev.TabIndex = 1;
            buttonPrev.Text = "◀";
            buttonPrev.Click += ButtonPrev_Click;
            // 
            // buttonOpenFolder
            // 
            buttonOpenFolder.Location = new Point(15, 10);
            buttonOpenFolder.Name = "buttonOpenFolder";
            buttonOpenFolder.Size = new Size(140, 30);
            buttonOpenFolder.TabIndex = 0;
            buttonOpenFolder.Text = "📁 选择文件夹";
            buttonOpenFolder.Click += ButtonOpenFolder_Click;
            // 
            // PdfReaderForm
            // 
            Controls.Add(panelNavigation);
            Controls.Add(tabControlTools);
            // 新增功能：中等级 - 添加缩略图侧边栏
            Controls.Add(panelPdf);
            Controls.Add(panelThumbnails);
            Controls.Add(treeViewFiles);
            Name = "PdfReaderForm";
            Size = new Size(1100, 600);
            panelPdf.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxPdf).EndInit();
            tabControlTools.ResumeLayout(false);
            tabPageOcr.ResumeLayout(false);
            tabPageOcr.PerformLayout();
            tabPageTranslate.ResumeLayout(false);
            tabPageTranslate.PerformLayout();
            tabPageAi.ResumeLayout(false);
            tabPageAi.PerformLayout();
            panelNavigation.ResumeLayout(false);
            panelNavigation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarZoom).EndInit();
            ResumeLayout(false);

            SetupNavigationPanelChildEvents();
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

                newX = Math.Max(treeViewFiles.Width, Math.Min(newX, ClientSize.Width - panelNavigation.Width));
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
                _presenter.RememberCurrentPageForCurrentFile(currentPage - 2);
            }
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            if (_presenter == null) return;
            var currentPage = int.TryParse(textBoxPage.Text, out var p) ? p : 1;
            if (currentPage < _presenter.PageCount)
            {
                _presenter.RenderPage(currentPage);
                _presenter.RememberCurrentPageForCurrentFile(currentPage);
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

    public class TranslationDialog : Form
    {
        public TranslationDialog(string original, string translation)
        {
            InitializeComponent(original, translation);
        }

        private void InitializeComponent(string original, string translation)
        {
            var labelOriginal = new Label { Text = "原文:", Location = new Point(20, 20), Size = new Size(260, 20) };
            var textBoxOriginal = new TextBox { Text = original, Location = new Point(20, 40), Size = new Size(340, 60), Multiline = true, ReadOnly = true };
            var labelTranslation = new Label { Text = "译文:", Location = new Point(20, 110), Size = new Size(260, 20) };
            var textBoxTranslation = new TextBox { Text = translation, Location = new Point(20, 130), Size = new Size(340, 60), Multiline = true, ReadOnly = true };
            var buttonClose = new Button { Text = "关闭", Location = new Point(150, 200), Size = new Size(80, 30) };
            buttonClose.Click += (s, e) => Close();

            Controls.Add(labelOriginal);
            Controls.Add(textBoxOriginal);
            Controls.Add(labelTranslation);
            Controls.Add(textBoxTranslation);
            Controls.Add(buttonClose);

            ClientSize = new Size(380, 250);
            Text = "翻译结果";
        }
    }
}
