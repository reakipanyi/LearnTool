using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services;
using LearningAssistant.Services.Cloud;
using LearningAssistant.Views;

namespace LearningAssistant.Forms
{
    public partial class MainForm : Form, IMainView
    {
        private readonly MainPresenter _presenter;
        private readonly IWindowManager _windowManager;
        private readonly AppConfig _appConfig;
        private readonly ICloudStorageService _cloudStorageService;
        private bool _isDisposed = false;

        public MainForm(MainPresenter presenter, IWindowManager windowManager, AppConfig appConfig, ICloudStorageService cloudStorageService)
        {
            InitializeComponent();
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _cloudStorageService = cloudStorageService ?? throw new ArgumentNullException(nameof(cloudStorageService));

            Load += MainForm_Load;
        }



        private void MainForm_Load(object? sender, EventArgs e)
        {
            _presenter?.SetView(this);
            _presenter?.Initialize();

            _presenter.OnOpenSettings += Presenter_OnOpenSettings;
            _presenter.OnOpenEditor += Presenter_OnOpenEditor;
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

        public string SelectedUser
        {
            get => comboBoxUser.Text;
            set => comboBoxUser.Text = value;
        }

        public string ProgressSummary
        {
            get => textBoxProgress.Text;
            set => textBoxProgress.Text = value;
        }

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

        public void SetTabPage(string tabName)
        {
            if (tabName == tabPageLearning.Text || tabName == "双语学习")
            {
                tabControl1.SelectedTab = tabPageLearning;
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
        private Button buttonSubjectLearning;
        private Button buttonBaiduNetdisk;
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
            tabControl1 = new TabControl();
            tabPageLearning = new TabPage();
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
            buttonSubjectLearning = new Button();
            buttonBaiduNetdisk = new Button();
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
            groupBoxUser.SuspendLayout();
            panelStreakInfo.SuspendLayout();
            menuStrip1.SuspendLayout();
            statusStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabPageLearning);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.Location = new Point(0, 25);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(900, 707);
            tabControl1.TabIndex = 0;
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;
            // 
            // tabPageLearning
            // 
            tabPageLearning.Controls.Add(panelMain);
            tabPageLearning.Location = new Point(4, 30);
            tabPageLearning.Name = "tabPageLearning";
            tabPageLearning.Padding = new Padding(3);
            tabPageLearning.Size = new Size(892, 673);
            tabPageLearning.TabIndex = 0;
            tabPageLearning.Text = "📖 双语学习";
            // 
            // panelMain
            // 
            panelMain.Controls.Add(groupBoxProgress);
            panelMain.Controls.Add(buttonOpenPdfReader);
            panelMain.Controls.Add(buttonOpenEditor);
            panelMain.Controls.Add(buttonSettings);
            panelMain.Controls.Add(buttonLearning);
            panelMain.Controls.Add(groupBoxUser);
            panelMain.Controls.Add(panelStreakInfo);
            panelMain.Controls.Add(buttonLearningManagement);
            panelMain.Controls.Add(buttonBrowser);
            panelMain.Controls.Add(buttonSubjectLearning);
            panelMain.Controls.Add(buttonBaiduNetdisk);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(3, 3);
            panelMain.Margin = new Padding(4);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(886, 667);
            panelMain.TabIndex = 0;
            // 
            // groupBoxProgress
            // 
            groupBoxProgress.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            groupBoxProgress.Controls.Add(textBoxProgress);
            groupBoxProgress.Location = new Point(620, 17);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Size = new Size(250, 207);
            groupBoxProgress.TabIndex = 12;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "学习统计摘要";
            // 
            // textBoxProgress
            // 
            textBoxProgress.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxProgress.Location = new Point(15, 34);
            textBoxProgress.Multiline = true;
            textBoxProgress.Name = "textBoxProgress";
            textBoxProgress.ReadOnly = true;
            textBoxProgress.ScrollBars = ScrollBars.Vertical;
            textBoxProgress.Size = new Size(220, 147);
            textBoxProgress.TabIndex = 0;
            // 
            // buttonOpenPdfReader
            // 
            buttonOpenPdfReader.BackColor = Color.FromArgb(0, 188, 212);
            buttonOpenPdfReader.FlatAppearance.BorderSize = 0;
            buttonOpenPdfReader.FlatStyle = FlatStyle.Flat;
            buttonOpenPdfReader.ForeColor = Color.White;
            buttonOpenPdfReader.Location = new Point(216, 226);
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
            buttonOpenEditor.Location = new Point(36, 226);
            buttonOpenEditor.Name = "buttonOpenEditor";
            buttonOpenEditor.Size = new Size(150, 51);
            buttonOpenEditor.TabIndex = 11;
            buttonOpenEditor.Text = "📝 模板编辑";
            buttonOpenEditor.UseVisualStyleBackColor = false;
            buttonOpenEditor.Click += ButtonOpenEditor_Click;
            // 
            // buttonSettings
            // 
            buttonSettings.BackColor = Color.FromArgb(255, 128, 0);
            buttonSettings.Location = new Point(389, 127);
            buttonSettings.Name = "buttonSettings";
            buttonSettings.Size = new Size(150, 51);
            buttonSettings.TabIndex = 16;
            buttonSettings.Text = "⚙️ 设置";
            buttonSettings.UseVisualStyleBackColor = false;
            buttonSettings.Visible = false;
            buttonSettings.Click += ButtonSettings_Click;
            // 
            // buttonLearning
            // 
            buttonLearning.BackColor = Color.FromArgb(33, 150, 243);
            buttonLearning.FlatAppearance.BorderSize = 0;
            buttonLearning.FlatStyle = FlatStyle.Flat;
            buttonLearning.ForeColor = Color.White;
            buttonLearning.Location = new Point(100, 127);
            buttonLearning.Name = "buttonLearning";
            buttonLearning.Size = new Size(200, 51);
            buttonLearning.TabIndex = 9;
            buttonLearning.Text = "📖 学习";
            buttonLearning.UseVisualStyleBackColor = false;
            buttonLearning.Click += ButtonLearning_Click;
            // 
            // groupBoxUser
            // 
            groupBoxUser.Controls.Add(comboBoxUser);
            groupBoxUser.Controls.Add(labelUser);
            groupBoxUser.Location = new Point(30, 17);
            groupBoxUser.Name = "groupBoxUser";
            groupBoxUser.Size = new Size(250, 74);
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
            panelStreakInfo.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panelStreakInfo.Controls.Add(labelStreakIcon);
            panelStreakInfo.Controls.Add(labelStreakDays);
            panelStreakInfo.Location = new Point(30, 399);
            panelStreakInfo.Margin = new Padding(4);
            panelStreakInfo.Name = "panelStreakInfo";
            panelStreakInfo.Size = new Size(365, 110);
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
            // buttonLearningManagement
            // 
            buttonLearningManagement.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonLearningManagement.BackColor = Color.FromArgb(103, 58, 183);
            buttonLearningManagement.FlatAppearance.BorderSize = 0;
            buttonLearningManagement.FlatStyle = FlatStyle.Flat;
            buttonLearningManagement.ForeColor = Color.White;
            buttonLearningManagement.Location = new Point(620, 302);
            buttonLearningManagement.Name = "buttonLearningManagement";
            buttonLearningManagement.Size = new Size(120, 44);
            buttonLearningManagement.TabIndex = 18;
            buttonLearningManagement.Text = "📋 学习管理";
            buttonLearningManagement.UseVisualStyleBackColor = false;
            buttonLearningManagement.Click += ButtonLearningManagement_Click;
            // 
            // buttonBrowser
            // 
            buttonBrowser.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowser.BackColor = Color.FromArgb(255, 87, 34);
            buttonBrowser.FlatAppearance.BorderSize = 0;
            buttonBrowser.FlatStyle = FlatStyle.Flat;
            buttonBrowser.ForeColor = Color.White;
            buttonBrowser.Location = new Point(750, 302);
            buttonBrowser.Name = "buttonBrowser";
            buttonBrowser.Size = new Size(120, 44);
            buttonBrowser.TabIndex = 19;
            buttonBrowser.Text = "🌐 学习浏览器";
            buttonBrowser.UseVisualStyleBackColor = false;
            buttonBrowser.Click += ButtonBrowser_Click;
            // 
            // buttonSubjectLearning
            // 
            buttonSubjectLearning.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonSubjectLearning.BackColor = Color.FromArgb(76, 175, 80);
            buttonSubjectLearning.FlatAppearance.BorderSize = 0;
            buttonSubjectLearning.FlatStyle = FlatStyle.Flat;
            buttonSubjectLearning.ForeColor = Color.White;
            buttonSubjectLearning.Location = new Point(750, 242);
            buttonSubjectLearning.Name = "buttonSubjectLearning";
            buttonSubjectLearning.Size = new Size(120, 44);
            buttonSubjectLearning.TabIndex = 20;
            buttonSubjectLearning.Text = "📚 学科学习";
            buttonSubjectLearning.UseVisualStyleBackColor = false;
            buttonSubjectLearning.Click += ButtonSubjectLearning_Click;
            // 
            // buttonBaiduNetdisk
            // 
            buttonBaiduNetdisk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBaiduNetdisk.BackColor = Color.FromArgb(33, 150, 243);
            buttonBaiduNetdisk.FlatAppearance.BorderSize = 0;
            buttonBaiduNetdisk.FlatStyle = FlatStyle.Flat;
            buttonBaiduNetdisk.ForeColor = Color.White;
            buttonBaiduNetdisk.Location = new Point(620, 242);
            buttonBaiduNetdisk.Name = "buttonBaiduNetdisk";
            buttonBaiduNetdisk.Size = new Size(120, 44);
            buttonBaiduNetdisk.TabIndex = 21;
            buttonBaiduNetdisk.Text = "☁️ 百度网盘";
            buttonBaiduNetdisk.UseVisualStyleBackColor = false;
            buttonBaiduNetdisk.Click += ButtonBaiduNetdisk_Click;
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { toolStripMenuItemFile, toolStripMenuItemSettings, toolStripMenuItemHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(900, 25);
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
            statusStrip1.Location = new Point(0, 710);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(900, 22);
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
            ClientSize = new Size(900, 732);
            Controls.Add(statusStrip1);
            Controls.Add(tabControl1);
            Controls.Add(menuStrip1);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MainMenuStrip = menuStrip1;
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "统一学习助手";
            tabControl1.ResumeLayout(false);
            tabPageLearning.ResumeLayout(false);
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
