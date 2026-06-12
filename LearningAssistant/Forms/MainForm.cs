using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public partial class MainForm : Form, IMainView, IThemeable
    {
        private readonly MainPresenter _presenter;
        private readonly IWindowManager _windowManager;
        private readonly AppConfig _appConfig;
        private readonly ICloudStorageService _cloudStorageService;
        private readonly IThemeService _themeService;
        private bool _isDisposed = false;

        public MainForm(MainPresenter presenter, IWindowManager windowManager, AppConfig appConfig, ICloudStorageService cloudStorageService, IThemeService themeService)
        {
            InitializeComponent();
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            Load += MainForm_Load;

            _themeService.RegisterThemeable(this);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (panelMain != null)
            {
                panelMain.BackColor = colors.Surface;
            }

            if (comboBoxUser != null)
            {
                comboBoxUser.BackColor = colors.Surface;
                comboBoxUser.ForeColor = colors.TextPrimary;
            }

            if (textBoxProgress != null)
            {
                textBoxProgress.BackColor = colors.Surface;
                textBoxProgress.ForeColor = colors.TextPrimary;
            }

            if (panelMain != null)
            {
                panelMain.BackColor = colors.Surface;
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            /*
            if (control is Button button)
            {
                button.BackColor = colors.Primary;
                button.ForeColor = Color.White;
            }
            else*/
            if (control is Label label)
            {
                label.ForeColor = colors.TextPrimary;
            }
            else if (control is Panel panel)
            {
                panel.BackColor = colors.Surface;
            }
            else if (control is GroupBox groupBox)
            {
                groupBox.BackColor = colors.Surface;
                groupBox.ForeColor = colors.TextPrimary;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }



        private void MainForm_Load(object? sender, EventArgs e)
        {
            _presenter?.SetView(this);
            _presenter?.Initialize();

            _presenter.OnOpenSettings += Presenter_OnOpenSettings;
            _presenter.OnOpenEditor += Presenter_OnOpenEditor;

            // 添加用户对比菜单项
            var userComparisonMenuItem = new ToolStripMenuItem("👥 用户对比");
            userComparisonMenuItem.Click += (s, args) => OpenUserComparisonClicked?.Invoke(this, EventArgs.Empty);
            toolStripMenuItemFile.DropDownItems.Add(userComparisonMenuItem);
        }

        private void Presenter_OnOpenSettings(object? sender, EventArgs e)
        {
            _windowManager.OpenSettingsWindow();
        }

        private void Presenter_OnOpenEditor(object? sender, EventArgs e)
        {
            _windowManager.OpenEditorWindow();
        }

        #region IMainView Implementation

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string SelectedUser
        {
            get => comboBoxUser.Text;
            set => comboBoxUser.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string ProgressSummary
        {
            get => textBoxProgress.Text;
            set => textBoxProgress.Text = value;
        }

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string StatusText
        {
            get => toolStripStatusLabel.Text;
            set => toolStripStatusLabel.Text = value;
        }

        public event EventHandler? UserChanged;
        public event EventHandler? OpenLearningWindowClicked;
        public event EventHandler? OpenSettingsClicked;
        public event EventHandler? OpenEditorClicked;
        public event EventHandler? TabChanged;
        public event EventHandler? NewUserClicked;
        public event EventHandler? OpenUserComparisonClicked;

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

        public void UpdateUserComparison(List<UserComparisonData> comparisonData)
        {
            var form = new UserComparisonForm(comparisonData);
            form.ShowDialog();
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private Panel panelMain;
        private GroupBox groupBoxUser;
        private ComboBox comboBoxUser;
        private Label labelUser;
        private Button buttonLearning;
        private Button buttonSettings;
        private Button buttonOpenEditor;
        private Button buttonOpenPdfReader;
        private Button buttonOpenStatistics;
        private Button buttonExportErrorBook;
        private GroupBox groupBoxProgress;
        private TextBox textBoxProgress;
        private Button buttonLearningManagement;
        private Button buttonBrowser;
        private Button buttonWebView2Browser;
        private Button buttonSubjectLearning;
        private Button buttonBaiduNetdisk;
        private Button buttonDictation;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem toolStripMenuItemFile;
        private ToolStripMenuItem toolStripMenuItemNewUser;
        private ToolStripMenuItem toolStripMenuItemExit;
        private ToolStripMenuItem toolStripMenuItemSettings;
        private ToolStripMenuItem toolStripMenuItemHelp;
        private ToolStripStatusLabel toolStripStatusLabel;
        private StatusStrip statusStrip1;
        private Panel panelStreakInfo;
        private Label labelStreakDays;
        private Label labelStreakIcon;

        private void InitializeComponent()
        {
            panelMain = new Panel();
            groupBoxProgress = new GroupBox();
            textBoxProgress = new TextBox();
            buttonOpenPdfReader = new Button();
            buttonOpenEditor = new Button();
            buttonSettings = new Button();
            buttonLearning = new Button();
            groupBoxUser = new GroupBox();
            comboBoxUser = new ComboBox();
            labelUser = new Label();
            panelStreakInfo = new Panel();
            labelStreakIcon = new Label();
            labelStreakDays = new Label();
            buttonLearningManagement = new Button();
            buttonBrowser = new Button();
            buttonWebView2Browser = new Button();
            buttonSubjectLearning = new Button();
            buttonBaiduNetdisk = new Button();
            buttonDictation = new Button();
            menuStrip1 = new MenuStrip();
            toolStripMenuItemFile = new ToolStripMenuItem();
            toolStripMenuItemNewUser = new ToolStripMenuItem();
            toolStripMenuItemExit = new ToolStripMenuItem();
            toolStripMenuItemSettings = new ToolStripMenuItem();
            toolStripMenuItemHelp = new ToolStripMenuItem();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            panelMain.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            groupBoxUser.SuspendLayout();
            panelStreakInfo.SuspendLayout();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // panelMain
            // 
            panelMain.Controls.Add(groupBoxProgress);
            panelMain.Controls.Add(buttonOpenPdfReader);
            panelMain.Controls.Add(buttonOpenEditor);
            panelMain.Controls.Add(buttonSettings);
            panelMain.Controls.Add(buttonLearning);
            panelMain.Controls.Add(groupBoxUser);
            panelMain.Controls.Add(buttonLearningManagement);
            panelMain.Controls.Add(buttonBrowser);
            panelMain.Controls.Add(buttonWebView2Browser);
            panelMain.Controls.Add(buttonSubjectLearning);
            panelMain.Controls.Add(buttonBaiduNetdisk);
            panelMain.Controls.Add(buttonDictation);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(0, 25);
            panelMain.Margin = new Padding(4);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(632, 593);
            panelMain.TabIndex = 0;
            // 
            // groupBoxProgress
            // 
            groupBoxProgress.Controls.Add(textBoxProgress);
            groupBoxProgress.Location = new Point(30, 107);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Size = new Size(558, 178);
            groupBoxProgress.TabIndex = 12;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxProgress
            // 
            textBoxProgress.Dock = DockStyle.Fill;
            textBoxProgress.Location = new Point(3, 24);
            textBoxProgress.Multiline = true;
            textBoxProgress.Name = "textBoxProgress";
            textBoxProgress.ReadOnly = true;
            textBoxProgress.ScrollBars = ScrollBars.Vertical;
            textBoxProgress.Size = new Size(552, 151);
            textBoxProgress.TabIndex = 0;
            // 
            // buttonOpenPdfReader
            // 
            buttonOpenPdfReader.BackColor = Color.FromArgb(0, 188, 212);
            buttonOpenPdfReader.FlatAppearance.BorderSize = 0;
            buttonOpenPdfReader.FlatStyle = FlatStyle.Flat;
            buttonOpenPdfReader.ForeColor = Color.White;
            buttonOpenPdfReader.Location = new Point(232, 316);
            buttonOpenPdfReader.Name = "buttonOpenPdfReader";
            buttonOpenPdfReader.Size = new Size(150, 51);
            buttonOpenPdfReader.TabIndex = 12;
            buttonOpenPdfReader.Text = "📖 PDF阅读";
            buttonOpenPdfReader.UseVisualStyleBackColor = false;
            buttonOpenPdfReader.Click += ButtonOpenPdfReader_Click;
            // 
            // buttonOpenEditor
            // 
            buttonOpenEditor.BackColor = Color.FromArgb(156, 39, 176);
            buttonOpenEditor.FlatAppearance.BorderSize = 0;
            buttonOpenEditor.FlatStyle = FlatStyle.Flat;
            buttonOpenEditor.ForeColor = Color.White;
            buttonOpenEditor.Location = new Point(31, 398);
            buttonOpenEditor.Name = "buttonOpenEditor";
            buttonOpenEditor.Size = new Size(150, 51);
            buttonOpenEditor.TabIndex = 11;
            buttonOpenEditor.Text = "📝 模板编辑";
            buttonOpenEditor.UseVisualStyleBackColor = false;
            buttonOpenEditor.Click += ButtonOpenEditor_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.BackColor = Color.FromArgb(33, 150, 243);
            buttonSettings.FlatAppearance.BorderSize = 0;
            buttonSettings.FlatStyle = FlatStyle.Flat;
            buttonSettings.ForeColor = Color.White;
            buttonSettings.Location = new Point(433, 316);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(150, 51);
            buttonSettings.TabIndex = 16;
            buttonSettings.Text = "⚙️ 设置";
            buttonSettings.UseVisualStyleBackColor = false;
            buttonSettings.Click += ButtonSettings_Click;
            // 
            // buttonLearning
            // 
            buttonLearning.BackColor = Color.FromArgb(33, 150, 243);
            buttonLearning.FlatAppearance.BorderSize = 0;
            buttonLearning.FlatStyle = FlatStyle.Flat;
            buttonLearning.ForeColor = Color.White;
            buttonLearning.Location = new Point(31, 316);
            buttonLearning.Name = "buttonLearning";
            buttonLearning.Size = new Size(150, 51);
            buttonLearning.TabIndex = 9;
            buttonLearning.Text = "📖 学习";
            buttonLearning.UseVisualStyleBackColor = false;
            buttonLearning.Click += ButtonLearning_Click;
            // 
            // groupBoxUser
            // 
            groupBoxUser.Controls.Add(comboBoxUser);
            groupBoxUser.Controls.Add(labelUser);
            groupBoxUser.Controls.Add(panelStreakInfo);
            groupBoxUser.Location = new Point(30, 17);
            groupBoxUser.Name = "groupBoxUser";
            groupBoxUser.Size = new Size(558, 74);
            groupBoxUser.TabIndex = 0;
            groupBoxUser.TabStop = false;
            groupBoxUser.Text = "多玩家";
            // 
            // comboBoxUser
            // 
            comboBoxUser.FormattingEnabled = true;
            comboBoxUser.Location = new Point(80, 34);
            comboBoxUser.Name = "comboBoxUser";
            comboBoxUser.Size = new Size(150, 29);
            comboBoxUser.TabIndex = 1;
            comboBoxUser.SelectedIndexChanged += ComboBoxUser_SelectedIndexChanged;
            // 
            // labelUser
            // 
            labelUser.Location = new Point(20, 37);
            labelUser.Name = "labelUser";
            labelUser.Size = new Size(50, 23);
            labelUser.TabIndex = 0;
            labelUser.Text = "玩家:";
            // 
            // panelStreakInfo
            // 
            panelStreakInfo.Controls.Add(labelStreakIcon);
            panelStreakInfo.Controls.Add(labelStreakDays);
            panelStreakInfo.Location = new Point(259, 26);
            panelStreakInfo.Margin = new Padding(4);
            panelStreakInfo.Name = "panelStreakInfo";
            panelStreakInfo.Size = new Size(248, 41);
            panelStreakInfo.TabIndex = 17;
            // 
            // labelStreakIcon
            // 
            labelStreakIcon.Location = new Point(118, 9);
            labelStreakIcon.Margin = new Padding(4, 0, 4, 0);
            labelStreakIcon.Name = "labelStreakIcon";
            labelStreakIcon.Size = new Size(105, 28);
            labelStreakIcon.TabIndex = 0;
            // 
            // labelStreakDays
            // 
            labelStreakDays.Location = new Point(7, 9);
            labelStreakDays.Margin = new Padding(4, 0, 4, 0);
            labelStreakDays.Name = "labelStreakDays";
            labelStreakDays.Size = new Size(105, 28);
            labelStreakDays.TabIndex = 1;
            // 
            // buttonLearningManagement
            // 
            buttonLearningManagement.BackColor = Color.FromArgb(103, 58, 183);
            buttonLearningManagement.FlatAppearance.BorderSize = 0;
            buttonLearningManagement.FlatStyle = FlatStyle.Flat;
            buttonLearningManagement.ForeColor = Color.White;
            buttonLearningManagement.Location = new Point(31, 476);
            buttonLearningManagement.Name = "buttonLearningManagement";
            buttonLearningManagement.Size = new Size(150, 51);
            buttonLearningManagement.TabIndex = 18;
            buttonLearningManagement.Text = "📋 学习管理";
            buttonLearningManagement.UseVisualStyleBackColor = false;
            buttonLearningManagement.Click += ButtonLearningManagement_Click;
            // 
            // buttonBrowser
            // 
            buttonBrowser.BackColor = Color.FromArgb(103, 58, 183);
            buttonBrowser.FlatAppearance.BorderSize = 0;
            buttonBrowser.FlatStyle = FlatStyle.Flat;
            buttonBrowser.ForeColor = Color.White;
            buttonBrowser.Location = new Point(232, 398);
            buttonBrowser.Name = "buttonBrowser";
            buttonBrowser.Size = new Size(150, 51);
            buttonBrowser.TabIndex = 19;
            buttonBrowser.Text = "🌐 学习浏览器(CefSharp)";
            buttonBrowser.UseVisualStyleBackColor = false;
            buttonBrowser.Click += ButtonBrowser_Click;
            // 
            // buttonWebView2Browser
            // 
            buttonWebView2Browser.BackColor = Color.FromArgb(255, 152, 0);
            buttonWebView2Browser.FlatAppearance.BorderSize = 0;
            buttonWebView2Browser.FlatStyle = FlatStyle.Flat;
            buttonWebView2Browser.ForeColor = Color.White;
            buttonWebView2Browser.Location = new Point(31, 554);
            buttonWebView2Browser.Name = "buttonWebView2Browser";
            buttonWebView2Browser.Size = new Size(150, 51);
            buttonWebView2Browser.TabIndex = 23;
            buttonWebView2Browser.Text = "🌐 WebView2浏览器";
            buttonWebView2Browser.UseVisualStyleBackColor = false;
            buttonWebView2Browser.Click += ButtonWebView2Browser_Click;
            // 
            // buttonSubjectLearning
            // 
            buttonSubjectLearning.BackColor = Color.FromArgb(76, 175, 80);
            buttonSubjectLearning.FlatAppearance.BorderSize = 0;
            buttonSubjectLearning.FlatStyle = FlatStyle.Flat;
            buttonSubjectLearning.ForeColor = Color.White;
            buttonSubjectLearning.Location = new Point(433, 476);
            buttonSubjectLearning.Name = "buttonSubjectLearning";
            buttonSubjectLearning.Size = new Size(150, 51);
            buttonSubjectLearning.TabIndex = 20;
            buttonSubjectLearning.Text = "📚 学科学习";
            buttonSubjectLearning.UseVisualStyleBackColor = false;
            buttonSubjectLearning.Click += ButtonSubjectLearning_Click;
            // 
            // buttonBaiduNetdisk
            // 
            buttonBaiduNetdisk.BackColor = Color.FromArgb(33, 150, 243);
            buttonBaiduNetdisk.FlatAppearance.BorderSize = 0;
            buttonBaiduNetdisk.FlatStyle = FlatStyle.Flat;
            buttonBaiduNetdisk.ForeColor = Color.White;
            buttonBaiduNetdisk.Location = new Point(232, 476);
            buttonBaiduNetdisk.Name = "buttonBaiduNetdisk";
            buttonBaiduNetdisk.Size = new Size(150, 51);
            buttonBaiduNetdisk.TabIndex = 21;
            buttonBaiduNetdisk.Text = "☁️ 百度网盘";
            buttonBaiduNetdisk.UseVisualStyleBackColor = false;
            buttonBaiduNetdisk.Click += ButtonBaiduNetdisk_Click;
            // 
            // buttonDictation
            // 
            buttonDictation.BackColor = Color.FromArgb(76, 175, 80);
            buttonDictation.FlatAppearance.BorderSize = 0;
            buttonDictation.FlatStyle = FlatStyle.Flat;
            buttonDictation.ForeColor = Color.White;
            buttonDictation.Location = new Point(433, 398);
            buttonDictation.Name = "buttonDictation";
            buttonDictation.Size = new Size(150, 51);
            buttonDictation.TabIndex = 22;
            buttonDictation.Text = "🎤 听写练习";
            buttonDictation.UseVisualStyleBackColor = false;
            buttonDictation.Click += ButtonDictation_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItemFile, toolStripMenuItemSettings, toolStripMenuItemHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(632, 25);
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
            toolStripMenuItemNewUser.Click += ToolStripMenuItemNewUser_Click;
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
            statusStrip1.Location = new Point(0, 618);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(632, 22);
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
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(632, 640);
            Controls.Add(panelMain);
            Controls.Add(statusStrip1);
            Controls.Add(menuStrip1);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "统一学习助手";
            panelMain.ResumeLayout(false);
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
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

        private void ButtonLearning_Click(object? sender, EventArgs e)
        {
            OpenLearningWindowClicked?.Invoke(this, EventArgs.Empty);
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
            _windowManager.OpenPdfReaderWindow();
        }

        private void ButtonLearningManagement_Click(object? sender, EventArgs e)
        {
            _windowManager.OpenLearningManagementWindow();
        }

        private void ButtonBrowser_Click(object? sender, EventArgs e)
        {
            _windowManager.OpenBrowserWindow();
        }

        private void ButtonWebView2Browser_Click(object? sender, EventArgs e)
        {
            try
            {
                var cloudStorageService = Program.GetService<ICloudStorageService>();
                var logger = Program.GetService<ILogger<WebView2BrowserForm>>();
                var form = new WebView2BrowserForm(cloudStorageService, logger);
                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开 WebView2 浏览器失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonSubjectLearning_Click(object? sender, EventArgs e)
        {
            MessageBox.Show("学科学习功能正在开发中，敬请期待！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ButtonBaiduNetdisk_Click(object? sender, EventArgs e)
        {
            try
            {
                if (!_cloudStorageService.IsConfigured)
                {
                    MessageBox.Show("百度网盘未配置，请先在系统设置中配置 Client ID 和 Client Secret。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!_cloudStorageService.IsAuthenticated)
                {
                    using var authForm = new BaiduNetdiskAuthForm(_cloudStorageService, OnBaiduNetdiskAuthCompleted);
                    authForm.ShowDialog();
                }
                else
                {
                    ShowBaiduNetdiskFiles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"百度网盘操作失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnBaiduNetdiskAuthCompleted(bool success)
        {
            if (success)
            {
                ShowBaiduNetdiskFiles();
            }
        }

        private void ButtonDictation_Click(object? sender, EventArgs e)
        {
            try
            {
                var speechService = Program.GetService<IAdvancedSpeechService>();
                if (speechService == null)
                {
                    MessageBox.Show("听写服务未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using var dictationForm = new DictationForm(speechService);
                dictationForm.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开听写窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ShowBaiduNetdiskFiles()
        {
            try
            {
                var files = await _cloudStorageService.ListFilesAsync("/");
                if (files != null && files.Count > 0)
                {
                    var fileList = string.Join("\n", files.Select(f => $"{(f.IsFolder ? "[文件夹]" : "[文件]")} {f.Name}"));
                    MessageBox.Show($"百度网盘根目录文件列表:\n\n{fileList}", "百度网盘文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("百度网盘根目录为空或获取文件列表失败", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取文件列表失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ToolStripMenuItemNewUser_Click(object? sender, EventArgs e)
        {
            NewUserClicked?.Invoke(this, EventArgs.Empty);
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
                    _presenter.OnOpenSettings -= Presenter_OnOpenSettings;
                    _presenter.OnOpenEditor -= Presenter_OnOpenEditor;
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
