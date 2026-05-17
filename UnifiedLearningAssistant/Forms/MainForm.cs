using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Presenters;
using UnifiedLearningAssistant.Services;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class MainForm : Form, IMainView
    {
        private readonly MainPresenter _presenter;
        private readonly IPdfView _pdfView;
        private readonly IWindowManager _windowManager;
        private readonly AppConfig _appConfig;
        private bool _isDisposed = false;

        public MainForm(MainPresenter presenter, IPdfView pdfView, IWindowManager windowManager, AppConfig appConfig)
        {
            InitializeComponent();
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _pdfView = pdfView ?? throw new ArgumentNullException(nameof(pdfView));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));

            this.EnableHighDpi();
            Load += MainForm_Load;
        }




        private void MainForm_Load(object? sender, EventArgs e)
        {
            _presenter.SetView(this);
            _presenter.Initialize();

            _presenter.OnStartLearning += Presenter_OnStartLearning;
            _presenter.OnOpenSettings += Presenter_OnOpenSettings;
            _presenter.OnOpenEditor += Presenter_OnOpenEditor;
            _presenter.OnOpenStatistics += Presenter_OnOpenStatistics;

            // 监听PDF的添加到编辑器事件
            _pdfView.AddToEditor += PdfView_OnAddToEditor;

            if (_pdfView is UserControl uc)
            {
                if (_pdfView is PdfReaderForm form)
                {
                    pdfReaderForm = form;
                }
                uc.Dock = DockStyle.Fill;
                tabPagePdf.Controls.Add(uc);
                uc.Show();
            }
            else
            {
                throw new InvalidOperationException("IPdfView 未实现为 UserControl 类型。");
            }

            ApplyColorScheme();
            AddButtonAnimations();
            ApplyFontSize();
        }

        private void ApplyFontSize()
        {
            int fontSize = _appConfig.AppSettings.DefaultFontSize;
            var defaultFont = new Font("Microsoft YaHei UI", fontSize);

            foreach (Control control in panelMain.Controls)
            {
                ApplyFontToControl(control, defaultFont);
            }

            labelStreakDays.Font = new Font("Microsoft YaHei UI", fontSize, FontStyle.Bold);
            toolStripStatusLabel.Font = defaultFont;
        }

        private void ApplyFontToControl(Control control, Font font)
        {
            control.Font = font;

            foreach (Control child in control.Controls)
            {
                ApplyFontToControl(child, font);
            }
        }

        private void PdfView_OnAddToEditor(object? sender, Views.AddToEditorEventArgs e)
        {
            _windowManager.OpenEditorWindowWithContext(e.Text, e.Language, null);
        }

        private void ApplyColorScheme()
        {
            BackColor = Color.FromArgb(250, 245, 235);

            ConfigureButton(buttonStartLearning, Color.FromArgb(76, 175, 80), "🚀 开始学习");
            ConfigureButton(buttonContinueLearning, Color.FromArgb(33, 150, 243), "📚 继续学习");
            ConfigureButton(buttonOpenEditor, Color.FromArgb(156, 39, 176), "📝 模板编辑");
            ConfigureButton(buttonOpenPdfReader, Color.FromArgb(0, 188, 212), "📖 PDF阅读");
            ConfigureButton(buttonOpenStatistics, Color.FromArgb(255, 152, 0), "📊 学习统计");
            ConfigureButton(buttonExportErrorBook, Color.FromArgb(244, 67, 54), "❌ 导出错题本");

            tabPageLearning.Text = "📖 双语学习";
            tabPagePdf.Text = "📄 PDF阅读助手";
        }

        private void ConfigureButton(Button button, Color backColor, string text)
        {
            button.BackColor = backColor;
            button.ForeColor = Color.White;
            button.Font = new Font(button.Font, FontStyle.Bold);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.Text = text;
        }

        private void AddButtonAnimations()
        {
            AddHoverEffect(buttonStartLearning, Color.FromArgb(76, 175, 80));
            AddHoverEffect(buttonContinueLearning, Color.FromArgb(33, 150, 243));
            AddHoverEffect(buttonOpenEditor, Color.FromArgb(156, 39, 176));
            AddHoverEffect(buttonOpenPdfReader, Color.FromArgb(0, 188, 212));
            AddHoverEffect(buttonOpenStatistics, Color.FromArgb(255, 152, 0));
            AddHoverEffect(buttonExportErrorBook, Color.FromArgb(244, 67, 54));
        }

        private void AddHoverEffect(Button button, Color originalColor)
        {
            Color hoverColor = Color.FromArgb(
                Math.Max(0, originalColor.R - 30),
                Math.Max(0, originalColor.G - 30),
                Math.Max(0, originalColor.B - 30)
            );

            button.MouseEnter += (s, e) =>
            {
                button.BackColor = hoverColor;
            };

            button.MouseLeave += (s, e) =>
            {
                button.BackColor = originalColor;
            };
        }

        // 新增功能：PDF生词本联动 - 设置PDF Presenter
        public void SetPdfPresenter(PdfPresenter pdfPresenter)
        {
            _presenter.SetPdfPresenter(pdfPresenter);
        }

        private async void Presenter_OnStartLearning(object? sender, Presenters.LearningStartEventArgs e)
        {
            await _windowManager.OpenLearningWindowAsync(e.UserId, e.Language, e.SubCategory, e.WordBankFile, e.Mode, e.SortOrder);
        }

        private void Presenter_OnOpenSettings(object? sender, EventArgs e)
        {
            _windowManager.OpenSettingsWindow();
        }

        private void Presenter_OnOpenEditor(object? sender, EventArgs e)
        {
            _windowManager.OpenEditorWindow();
        }

        private void Presenter_OnOpenStatistics(object? sender, EventArgs e)
        {
            _windowManager.OpenStatisticsWindow();
        }

        #region IMainView Implementation

        public string SelectedUser
        {
            get => comboBoxUser.Text;
            set => comboBoxUser.Text = value;
        }

        public string SelectedLanguage
        {
            get => radioChinese.Checked ? Constants.Language.Chinese : Constants.Language.English;
            set
            {
                radioChinese.Checked = value == Constants.Language.Chinese;
                radioEnglish.Checked = value == Constants.Language.English;
            }
        }

        public string SelectedSubCategory
        {
            get => comboBoxSubCategory.Text;
            set => comboBoxSubCategory.Text = value;
        }

        public string SelectedMode
        {
            get => radioStudyMode.Checked ? Constants.LearningMode.Study : Constants.LearningMode.Quick;
            set
            {
                radioStudyMode.Checked = value == Constants.LearningMode.Study;
                radioQuickMode.Checked = value == Constants.LearningMode.Quick;
            }
        }

        public string SelectedWordBankFile
        {
            get => comboBoxWordBank.Text;
            set => comboBoxWordBank.Text = value;
        }

        public string ProgressSummary
        {
            get => textBoxProgress.Text;
            set => textBoxProgress.Text = value;
        }

        public string SelectedSortOrder
        {
            get => comboBoxSortOrder.Text;
            set => comboBoxSortOrder.Text = value;
        }

        public string StatusText
        {
            get => toolStripStatusLabel.Text;
            set => toolStripStatusLabel.Text = value;
        }

        public event EventHandler? UserChanged;
        public event EventHandler? LanguageChanged;
        public event EventHandler? SubCategoryChanged;
        public event EventHandler? ModeChanged;
        public event EventHandler? WordBankChanged;
        public event EventHandler? StartLearningClicked;
        public event EventHandler? ContinueLearningClicked;
        public event EventHandler? OpenSettingsClicked;
        public event EventHandler? OpenEditorClicked;
        public event EventHandler? OpenStatisticsClicked;
        // 新增功能：错题本导出 - 导出事件
        public event EventHandler? ExportErrorBookClicked;
        public event EventHandler? SortOrderChanged;
        public event EventHandler? TabChanged;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void RefreshUserList(IEnumerable<string> users)
        {
            comboBoxUser.Items.Clear();
            foreach (var user in users)
            {
                comboBoxUser.Items.Add(user);
            }
        }

        public void RefreshSubCategories(IEnumerable<string> subCats)
        {
            comboBoxSubCategory.Items.Clear();
            foreach (var cat in subCats)
            {
                comboBoxSubCategory.Items.Add(cat);
            }
        }

        public void RefreshWordBankFiles(IEnumerable<string> files)
        {
            comboBoxWordBank.Items.Clear();
            foreach (var file in files)
            {
                comboBoxWordBank.Items.Add(file);
            }
        }

        public void SetTabPage(string tabName)
        {
            if (tabName == tabPageLearning.Text || tabName == "双语学习")
            {
                tabControl1.SelectedTab = tabPageLearning;
            }
            else if (tabName == tabPagePdf.Text || tabName == "PDF" || tabName == "PDF阅读助手")
            {
                tabControl1.SelectedTab = tabPagePdf;
            }
        }

        public void UpdateStatus(string status)
        {
            StatusText = status;
        }

        public void UpdateStreakInfo(int consecutiveDays, string studyTimeSummary)
        {
            if (labelStreakDays != null)
            {
                labelStreakDays.Text = $"连续 {consecutiveDays} 天";
                if (consecutiveDays >= 7)
                {
                    labelStreakDays.Text = $"🔥 连续 {consecutiveDays} 天";
                }
            }

            if (panelStreakInfo != null)
            {
                if (consecutiveDays >= 30)
                {
                    panelStreakInfo.BackColor = Color.FromArgb(255, 245, 230);
                    panelStreakInfo.BorderStyle = BorderStyle.Fixed3D;
                }
                else if (consecutiveDays >= 7)
                {
                    panelStreakInfo.BackColor = Color.FromArgb(255, 248, 240);
                    panelStreakInfo.BorderStyle = BorderStyle.FixedSingle;
                }
            }
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private TabControl tabControl1;
        private TabPage tabPageLearning;
        private Panel panelMain;
        private GroupBox groupBoxUser;
        private ComboBox comboBoxUser;
        private Label labelUser;
        private GroupBox groupBoxLearning;
        private GroupBox groupBoxLanguage;
        private RadioButton radioChinese;
        private RadioButton radioEnglish;
        private Label labelLanguage;
        private GroupBox groupBoxMode;
        private Label labelMode;
        private RadioButton radioStudyMode;
        private RadioButton radioQuickMode;
        private Label labelSubCategory;
        private ComboBox comboBoxSubCategory;
        private Label labelWordBank;
        private ComboBox comboBoxWordBank;
        private Button buttonStartLearning;
        private Button buttonContinueLearning;
        private Button buttonSettings;
        private Button buttonOpenEditor;
        private Button buttonOpenPdfReader;
        private Button buttonOpenStatistics;
        // 新增功能：错题本导出 - 导出按钮
        private Button buttonExportErrorBook;
        private PdfReaderForm? pdfReaderForm;
        private GroupBox groupBoxProgress;
        private TextBox textBoxProgress;
        private Label labelSortOrder;
        private ComboBox comboBoxSortOrder;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItemFile;
        private ToolStripMenuItem toolStripMenuItemNewUser;
        private ToolStripMenuItem toolStripMenuItemExit;
        private ToolStripMenuItem toolStripMenuItemSettings;
        private ToolStripMenuItem toolStripMenuItemHelp;
        private ToolStripStatusLabel toolStripStatusLabel;
        private TabPage tabPagePdf;
        private StatusStrip statusStrip1;
        private Panel panelStreakInfo;
        private Label labelStreakDays;
        private Label labelStreakIcon;

        private void InitializeComponent()
        {
            tabControl1 = new TabControl();
            tabPageLearning = new TabPage();
            panelMain = new Panel();
            groupBoxProgress = new GroupBox();
            textBoxProgress = new TextBox();
            buttonOpenStatistics = new Button();
            buttonOpenPdfReader = new Button();
            buttonOpenEditor = new Button();
            buttonSettings = new Button();
            buttonContinueLearning = new Button();
            buttonStartLearning = new Button();
            groupBoxLearning = new GroupBox();
            groupBoxLanguage = new GroupBox();
            labelLanguage = new Label();
            radioChinese = new RadioButton();
            radioEnglish = new RadioButton();
            groupBoxMode = new GroupBox();
            labelMode = new Label();
            radioStudyMode = new RadioButton();
            radioQuickMode = new RadioButton();
            labelSubCategory = new Label();
            comboBoxSubCategory = new ComboBox();
            labelWordBank = new Label();
            comboBoxWordBank = new ComboBox();
            labelSortOrder = new Label();
            comboBoxSortOrder = new ComboBox();
            groupBoxUser = new GroupBox();
            comboBoxUser = new ComboBox();
            labelUser = new Label();
            buttonExportErrorBook = new Button();
            panelStreakInfo = new Panel();
            labelStreakIcon = new Label();
            labelStreakDays = new Label();
            tabPagePdf = new TabPage();
            menuStrip1 = new MenuStrip();
            toolStripMenuItemFile = new ToolStripMenuItem();
            toolStripMenuItemNewUser = new ToolStripMenuItem();
            toolStripMenuItemExit = new ToolStripMenuItem();
            toolStripMenuItemSettings = new ToolStripMenuItem();
            toolStripMenuItemHelp = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            tabControl1.SuspendLayout();
            tabPageLearning.SuspendLayout();
            panelMain.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            groupBoxLearning.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            groupBoxMode.SuspendLayout();
            groupBoxUser.SuspendLayout();
            panelStreakInfo.SuspendLayout();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPageLearning);
            tabControl1.Controls.Add(tabPagePdf);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 25);
            tabControl1.Margin = new Padding(4, 4, 4, 4);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1441, 879);
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
            // 
            // tabPageLearning
            // 
            tabPageLearning.Controls.Add(panelMain);
            tabPageLearning.Location = new Point(4, 30);
            tabPageLearning.Margin = new Padding(4, 4, 4, 4);
            tabPageLearning.Name = "tabPageLearning";
            tabPageLearning.Padding = new Padding(4, 4, 4, 4);
            tabPageLearning.Size = new Size(1433, 845);
            tabPageLearning.TabIndex = 0;
            tabPageLearning.Text = "双语学习";
            // 
            // panelMain
            // 
            panelMain.Controls.Add(groupBoxProgress);
            panelMain.Controls.Add(buttonOpenStatistics);
            panelMain.Controls.Add(buttonOpenPdfReader);
            panelMain.Controls.Add(buttonOpenEditor);
            panelMain.Controls.Add(buttonContinueLearning);
            panelMain.Controls.Add(buttonStartLearning);
            panelMain.Controls.Add(groupBoxLearning);
            panelMain.Controls.Add(groupBoxUser);
            panelMain.Controls.Add(buttonExportErrorBook);
            panelMain.Controls.Add(panelStreakInfo);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(4, 4);
            panelMain.Margin = new Padding(4, 4, 4, 4);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(1425, 837);
            panelMain.TabIndex = 0;
            // 
            // groupBoxProgress
            // 
            groupBoxProgress.Controls.Add(textBoxProgress);
            groupBoxProgress.Location = new Point(886, 21);
            groupBoxProgress.Margin = new Padding(4, 4, 4, 4);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Padding = new Padding(4, 4, 4, 4);
            groupBoxProgress.Size = new Size(437, 256);
            groupBoxProgress.TabIndex = 12;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxProgress
            // 
            textBoxProgress.Location = new Point(21, 42);
            textBoxProgress.Margin = new Padding(4, 4, 4, 4);
            textBoxProgress.Multiline = true;
            textBoxProgress.Name = "textBoxProgress";
            textBoxProgress.ReadOnly = true;
            textBoxProgress.ScrollBars = ScrollBars.Vertical;
            textBoxProgress.Size = new Size(390, 181);
            textBoxProgress.TabIndex = 0;
            // 
            // buttonOpenStatistics
            // 
            buttonOpenStatistics.Location = new Point(886, 379);
            buttonOpenStatistics.Margin = new Padding(4, 4, 4, 4);
            buttonOpenStatistics.Name = "buttonOpenStatistics";
            buttonOpenStatistics.Size = new Size(214, 63);
            buttonOpenStatistics.TabIndex = 13;
            buttonOpenStatistics.Text = "📊 学习统计";
            buttonOpenStatistics.Click += ButtonOpenStatistics_Click;
            // 
            // buttonOpenPdfReader
            // 
            buttonOpenPdfReader.Location = new Point(1109, 313);
            buttonOpenPdfReader.Margin = new Padding(4, 4, 4, 4);
            buttonOpenPdfReader.Name = "buttonOpenPdfReader";
            buttonOpenPdfReader.Size = new Size(214, 63);
            buttonOpenPdfReader.TabIndex = 12;
            buttonOpenPdfReader.Text = "📖 PDF阅读";
            buttonOpenPdfReader.Click += ButtonOpenPdfReader_Click;
            // 
            // buttonOpenEditor
            // 
            buttonOpenEditor.Location = new Point(886, 313);
            buttonOpenEditor.Margin = new Padding(4, 4, 4, 4);
            buttonOpenEditor.Name = "buttonOpenEditor";
            buttonOpenEditor.Size = new Size(214, 63);
            buttonOpenEditor.TabIndex = 11;
            buttonOpenEditor.Text = "📝 模板编辑";
            buttonOpenEditor.Click += ButtonOpenEditor_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.Location = new Point(450, 161);
            buttonSettings.Margin = new Padding(4, 4, 4, 4);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(171, 49);
            buttonSettings.TabIndex = 16;
            buttonSettings.Text = "⚙️ 设置";
            buttonSettings.Visible = false;
            buttonSettings.Click += ButtonSettings_Click;
            // 
            // buttonContinueLearning
            // 
            buttonContinueLearning.Location = new Point(411, 427);
            buttonContinueLearning.Margin = new Padding(4, 4, 4, 4);
            buttonContinueLearning.Name = "buttonContinueLearning";
            buttonContinueLearning.Size = new Size(214, 63);
            buttonContinueLearning.TabIndex = 10;
            buttonContinueLearning.Text = "继续学习";
            buttonContinueLearning.Click += ButtonContinueLearning_Click;
            // 
            // buttonStartLearning
            // 
            buttonStartLearning.Location = new Point(154, 427);
            buttonStartLearning.Margin = new Padding(4, 4, 4, 4);
            buttonStartLearning.Name = "buttonStartLearning";
            buttonStartLearning.Size = new Size(214, 63);
            buttonStartLearning.TabIndex = 9;
            buttonStartLearning.Text = "开始学习";
            buttonStartLearning.Click += ButtonStartLearning_Click;
            // 
            // groupBoxLearning
            // 
            groupBoxLearning.Controls.Add(groupBoxLanguage);
            groupBoxLearning.Controls.Add(groupBoxMode);
            groupBoxLearning.Controls.Add(labelSubCategory);
            groupBoxLearning.Controls.Add(comboBoxSubCategory);
            groupBoxLearning.Controls.Add(buttonSettings);
            groupBoxLearning.Controls.Add(labelWordBank);
            groupBoxLearning.Controls.Add(comboBoxWordBank);
            groupBoxLearning.Controls.Add(labelSortOrder);
            groupBoxLearning.Controls.Add(comboBoxSortOrder);
            groupBoxLearning.Location = new Point(43, 140);
            groupBoxLearning.Margin = new Padding(4, 4, 4, 4);
            groupBoxLearning.Name = "groupBoxLearning";
            groupBoxLearning.Padding = new Padding(4, 4, 4, 4);
            groupBoxLearning.Size = new Size(800, 259);
            groupBoxLearning.TabIndex = 1;
            groupBoxLearning.TabStop = false;
            groupBoxLearning.Text = "快速开始";
            // 
            // groupBoxLanguage
            // 
            groupBoxLanguage.Controls.Add(labelLanguage);
            groupBoxLanguage.Controls.Add(radioChinese);
            groupBoxLanguage.Controls.Add(radioEnglish);
            groupBoxLanguage.Location = new Point(21, 31);
            groupBoxLanguage.Margin = new Padding(4, 4, 4, 4);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Padding = new Padding(4, 4, 4, 4);
            groupBoxLanguage.Size = new Size(357, 74);
            groupBoxLanguage.TabIndex = 0;
            groupBoxLanguage.TabStop = false;
            groupBoxLanguage.Text = "语言";
            // 
            // labelLanguage
            // 
            labelLanguage.Location = new Point(14, 27);
            labelLanguage.Margin = new Padding(4, 0, 4, 0);
            labelLanguage.Name = "labelLanguage";
            labelLanguage.Size = new Size(86, 28);
            labelLanguage.TabIndex = 0;
            labelLanguage.Text = "选择:";
            // 
            // radioChinese
            // 
            radioChinese.Location = new Point(114, 25);
            radioChinese.Margin = new Padding(4, 4, 4, 4);
            radioChinese.Name = "radioChinese";
            radioChinese.Size = new Size(114, 33);
            radioChinese.TabIndex = 1;
            radioChinese.Text = "中文";
            radioChinese.CheckedChanged += RadioChinese_CheckedChanged;
            // 
            // radioEnglish
            // 
            radioEnglish.Checked = true;
            radioEnglish.Location = new Point(243, 25);
            radioEnglish.Margin = new Padding(4, 4, 4, 4);
            radioEnglish.Name = "radioEnglish";
            radioEnglish.Size = new Size(114, 33);
            radioEnglish.TabIndex = 2;
            radioEnglish.TabStop = true;
            radioEnglish.Text = "英语";
            radioEnglish.CheckedChanged += RadioEnglish_CheckedChanged;
            // 
            // groupBoxMode
            // 
            groupBoxMode.Controls.Add(labelMode);
            groupBoxMode.Controls.Add(radioStudyMode);
            groupBoxMode.Controls.Add(radioQuickMode);
            groupBoxMode.Location = new Point(393, 31);
            groupBoxMode.Margin = new Padding(4, 4, 4, 4);
            groupBoxMode.Name = "groupBoxMode";
            groupBoxMode.Padding = new Padding(4, 4, 4, 4);
            groupBoxMode.Size = new Size(357, 74);
            groupBoxMode.TabIndex = 1;
            groupBoxMode.TabStop = false;
            groupBoxMode.Text = "模式";
            // 
            // labelMode
            // 
            labelMode.Location = new Point(14, 27);
            labelMode.Margin = new Padding(4, 0, 4, 0);
            labelMode.Name = "labelMode";
            labelMode.Size = new Size(86, 28);
            labelMode.TabIndex = 0;
            labelMode.Text = "选择:";
            // 
            // radioStudyMode
            // 
            radioStudyMode.Checked = true;
            radioStudyMode.Location = new Point(114, 25);
            radioStudyMode.Margin = new Padding(4, 4, 4, 4);
            radioStudyMode.Name = "radioStudyMode";
            radioStudyMode.Size = new Size(114, 33);
            radioStudyMode.TabIndex = 1;
            radioStudyMode.TabStop = true;
            radioStudyMode.Text = "学习模式";
            radioStudyMode.CheckedChanged += RadioStudyMode_CheckedChanged;
            // 
            // radioQuickMode
            // 
            radioQuickMode.Location = new Point(243, 25);
            radioQuickMode.Margin = new Padding(4, 4, 4, 4);
            radioQuickMode.Name = "radioQuickMode";
            radioQuickMode.Size = new Size(114, 33);
            radioQuickMode.TabIndex = 2;
            radioQuickMode.Text = "快速模式";
            radioQuickMode.CheckedChanged += RadioQuickMode_CheckedChanged;
            // 
            // labelSubCategory
            // 
            labelSubCategory.Location = new Point(21, 121);
            labelSubCategory.Margin = new Padding(4, 0, 4, 0);
            labelSubCategory.Name = "labelSubCategory";
            labelSubCategory.Size = new Size(143, 28);
            labelSubCategory.TabIndex = 2;
            labelSubCategory.Text = "学习类型:";
            // 
            // comboBoxSubCategory
            // 
            comboBoxSubCategory.FormattingEnabled = true;
            comboBoxSubCategory.Location = new Point(171, 119);
            comboBoxSubCategory.Margin = new Padding(4, 4, 4, 4);
            comboBoxSubCategory.Name = "comboBoxSubCategory";
            comboBoxSubCategory.Size = new Size(213, 29);
            comboBoxSubCategory.TabIndex = 2;
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            // 
            // labelWordBank
            // 
            labelWordBank.Location = new Point(21, 161);
            labelWordBank.Margin = new Padding(4, 0, 4, 0);
            labelWordBank.Name = "labelWordBank";
            labelWordBank.Size = new Size(143, 28);
            labelWordBank.TabIndex = 4;
            labelWordBank.Text = "词库文件:";
            // 
            // comboBoxWordBank
            // 
            comboBoxWordBank.FormattingEnabled = true;
            comboBoxWordBank.Location = new Point(171, 157);
            comboBoxWordBank.Margin = new Padding(4, 4, 4, 4);
            comboBoxWordBank.Name = "comboBoxWordBank";
            comboBoxWordBank.Size = new Size(213, 29);
            comboBoxWordBank.TabIndex = 5;
            comboBoxWordBank.SelectedIndexChanged += ComboBoxWordBank_SelectedIndexChanged;
            // 
            // labelSortOrder
            // 
            labelSortOrder.Location = new Point(21, 201);
            labelSortOrder.Margin = new Padding(4, 0, 4, 0);
            labelSortOrder.Name = "labelSortOrder";
            labelSortOrder.Size = new Size(143, 28);
            labelSortOrder.TabIndex = 6;
            labelSortOrder.Text = "排序方式:";
            // 
            // comboBoxSortOrder
            // 
            comboBoxSortOrder.FormattingEnabled = true;
            comboBoxSortOrder.Items.AddRange(new object[] { "顺序", "Random" });
            comboBoxSortOrder.Location = new Point(171, 198);
            comboBoxSortOrder.Margin = new Padding(4, 4, 4, 4);
            comboBoxSortOrder.Name = "comboBoxSortOrder";
            comboBoxSortOrder.Size = new Size(213, 29);
            comboBoxSortOrder.TabIndex = 8;
            comboBoxSortOrder.Text = "顺序";
            comboBoxSortOrder.SelectedIndexChanged += ComboBoxSortOrder_SelectedIndexChanged;
            // 
            // groupBoxUser
            // 
            groupBoxUser.Controls.Add(comboBoxUser);
            groupBoxUser.Controls.Add(labelUser);
            groupBoxUser.Location = new Point(43, 21);
            groupBoxUser.Margin = new Padding(4, 4, 4, 4);
            groupBoxUser.Name = "groupBoxUser";
            groupBoxUser.Padding = new Padding(4, 4, 4, 4);
            groupBoxUser.Size = new Size(357, 91);
            groupBoxUser.TabIndex = 0;
            groupBoxUser.TabStop = false;
            groupBoxUser.Text = "多玩家";
            // 
            // comboBoxUser
            // 
            comboBoxUser.FormattingEnabled = true;
            comboBoxUser.Location = new Point(114, 42);
            comboBoxUser.Margin = new Padding(4, 4, 4, 4);
            comboBoxUser.Name = "comboBoxUser";
            comboBoxUser.Size = new Size(213, 29);
            comboBoxUser.TabIndex = 1;
            comboBoxUser.SelectedIndexChanged += ComboBoxUser_SelectedIndexChanged;
            // 
            // labelUser
            // 
            labelUser.Location = new Point(29, 46);
            labelUser.Margin = new Padding(4, 0, 4, 0);
            labelUser.Name = "labelUser";
            labelUser.Size = new Size(71, 28);
            labelUser.TabIndex = 0;
            labelUser.Text = "玩家:";
            // 
            // buttonExportErrorBook
            // 
            buttonExportErrorBook.Location = new Point(1109, 380);
            buttonExportErrorBook.Margin = new Padding(4, 4, 4, 4);
            buttonExportErrorBook.Name = "buttonExportErrorBook";
            buttonExportErrorBook.Size = new Size(214, 63);
            buttonExportErrorBook.TabIndex = 15;
            buttonExportErrorBook.Text = "📝 导出错题本";
            buttonExportErrorBook.UseVisualStyleBackColor = true;
            buttonExportErrorBook.Click += ButtonExportErrorBook_Click;
            // 
            // panelStreakInfo
            // 
            panelStreakInfo.Controls.Add(labelStreakIcon);
            panelStreakInfo.Controls.Add(labelStreakDays);
            panelStreakInfo.Location = new Point(473, 21);
            panelStreakInfo.Margin = new Padding(4, 4, 4, 4);
            panelStreakInfo.Name = "panelStreakInfo";
            panelStreakInfo.Size = new Size(361, 111);
            panelStreakInfo.TabIndex = 17;
            // 
            // labelStreakIcon
            // 
            labelStreakIcon.Location = new Point(141, 17);
            labelStreakIcon.Margin = new Padding(4, 0, 4, 0);
            labelStreakIcon.Name = "labelStreakIcon";
            labelStreakIcon.Size = new Size(143, 28);
            labelStreakIcon.TabIndex = 0;
            // 
            // labelStreakDays
            // 
            labelStreakDays.Location = new Point(6, 17);
            labelStreakDays.Margin = new Padding(4, 0, 4, 0);
            labelStreakDays.Name = "labelStreakDays";
            labelStreakDays.Size = new Size(143, 28);
            labelStreakDays.TabIndex = 1;
            // 
            // tabPagePdf
            // 
            tabPagePdf.Location = new Point(4, 30);
            tabPagePdf.Margin = new Padding(4, 4, 4, 4);
            tabPagePdf.Name = "tabPagePdf";
            tabPagePdf.Padding = new Padding(4, 4, 4, 4);
            tabPagePdf.Size = new Size(1433, 845);
            tabPagePdf.TabIndex = 1;
            tabPagePdf.Text = "PDF阅读助手";
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItemFile, toolStripMenuItemSettings, toolStripMenuItemHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(9, 2, 0, 2);
            menuStrip1.Size = new Size(1441, 25);
            menuStrip1.TabIndex = 1;
            // 
            // toolStripMenuItemFile
            // 
            toolStripMenuItemFile.DropDownItems.AddRange(new ToolStripItem[] { toolStripMenuItemNewUser, toolStripMenuItemExit });
            toolStripMenuItemFile.Name = "toolStripMenuItemFile";
            toolStripMenuItemFile.Size = new Size(44, 21);
            toolStripMenuItemFile.Text = "文件";
            // 
            // toolStripMenuItemNewUser
            // 
            toolStripMenuItemNewUser.Name = "toolStripMenuItemNewUser";
            toolStripMenuItemNewUser.Size = new Size(124, 22);
            toolStripMenuItemNewUser.Text = "新建玩家";
            // 
            // toolStripMenuItemExit
            // 
            toolStripMenuItemExit.Name = "toolStripMenuItemExit";
            toolStripMenuItemExit.Size = new Size(124, 22);
            toolStripMenuItemExit.Text = "退出";
            toolStripMenuItemExit.Click += ToolStripMenuItemExit_Click;
            // 
            // toolStripMenuItemSettings
            // 
            toolStripMenuItemSettings.Name = "toolStripMenuItemSettings";
            toolStripMenuItemSettings.Size = new Size(44, 21);
            toolStripMenuItemSettings.Text = "设置";
            toolStripMenuItemSettings.Click += ToolStripMenuItemSettings_Click;
            // 
            // toolStripMenuItemHelp
            // 
            toolStripMenuItemHelp.Name = "toolStripMenuItemHelp";
            toolStripMenuItemHelp.Size = new Size(44, 21);
            toolStripMenuItemHelp.Text = "帮助";
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 882);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Padding = new Padding(1, 0, 20, 0);
            statusStrip1.Size = new Size(1441, 22);
            statusStrip1.TabIndex = 2;
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(32, 17);
            toolStripStatusLabel.Text = "就绪";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1441, 904);
            Controls.Add(statusStrip1);
            Controls.Add(tabControl1);
            Controls.Add(menuStrip1);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4, 4, 4, 4);
            Name = "MainForm";
            Text = "统一学习助手";
            tabControl1.ResumeLayout(false);
            tabPageLearning.ResumeLayout(false);
            panelMain.ResumeLayout(false);
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
            groupBoxLearning.ResumeLayout(false);
            groupBoxLanguage.ResumeLayout(false);
            groupBoxMode.ResumeLayout(false);
            groupBoxUser.ResumeLayout(false);
            panelStreakInfo.ResumeLayout(false);
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        #region Event Handlers

        private void ComboBoxUser_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UserChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioChinese_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioChinese.Checked)
                LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioEnglish_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioEnglish.Checked)
                LanguageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxSubCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            SubCategoryChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioStudyMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioStudyMode.Checked)
                ModeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioQuickMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioQuickMode.Checked)
                ModeChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxWordBank_SelectedIndexChanged(object? sender, EventArgs e)
        {
            WordBankChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxSortOrder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            SortOrderChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonStartLearning_Click(object? sender, EventArgs e)
        {
            StartLearningClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonContinueLearning_Click(object? sender, EventArgs e)
        {
            ContinueLearningClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonSettings_Click(object? sender, EventArgs e)
        {
            OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonOpenEditor_Click(object? sender, EventArgs e)
        {
            OpenEditorClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonOpenPdfReader_Click(object? sender, EventArgs e)
        {
            SetTabPage("PDF阅读助手");
        }

        private void ButtonOpenStatistics_Click(object? sender, EventArgs e)
        {
            OpenStatisticsClicked?.Invoke(this, EventArgs.Empty);
        }

        // 新增功能：错题本导出 - 导出按钮点击事件
        private void ButtonExportErrorBook_Click(object? sender, EventArgs e)
        {
            ExportErrorBookClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ToolStripMenuItemSettings_Click(object? sender, EventArgs e)
        {
            OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ToolStripMenuItemExit_Click(object? sender, EventArgs e)
        {
            Application.Exit();
        }

        private void TabControl1_SelectedIndexChanged(object? sender, EventArgs e)
        {
            TabChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_isDisposed)
                return;

            if (disposing)
            {
                if (_presenter != null)
                {
                    _presenter.OnStartLearning -= Presenter_OnStartLearning;
                    _presenter.OnOpenSettings -= Presenter_OnOpenSettings;
                    _presenter.OnOpenEditor -= Presenter_OnOpenEditor;
                    _presenter.OnOpenStatistics -= Presenter_OnOpenStatistics;
                    (_presenter as IDisposable)?.Dispose();
                }

                if (components != null)
                {
                    components.Dispose();
                }
            }

            _isDisposed = true;
            base.Dispose(disposing);
        }

    }
}
