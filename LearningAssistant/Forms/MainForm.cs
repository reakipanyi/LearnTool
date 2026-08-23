using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Forms.UserControls.Dashboard;
using LearningAssistant.Forms.UserControls.Navigation;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Presenters;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Hotkeys;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.SystemTray;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Forms
{
    public partial class MainForm : Form, IMainView, IThemeable
    {
        private readonly MainPresenter _presenter;
        private readonly IWindowManager _windowManager;
        private readonly AppConfig _appConfig;
        private readonly IThemeService _themeService;
        private readonly ILogger<MainForm> _logger;
        private readonly Services.Web.IWebBookmarkService _webBookmarkService;
        private readonly ITrayIconService _trayIconService;
        private readonly IHotkeyService _hotkeyService;
        private readonly IPomodoroService _pomodoroService;
        private PomodoroTrayIntegration? _pomodoroTrayIntegration;
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly IUserSessionService? _userSessionService;


        public MainForm(
            MainPresenter presenter,
            IWindowManager windowManager,
            AppConfig appConfig,
            IThemeService themeService,
            ILogger<MainForm> logger,
            Services.Web.IWebBookmarkService webBookmarkService,
            ITrayIconService trayIconService,
            IHotkeyService hotkeyService,
            IPomodoroService pomodoroService,
            ISpacedRepetitionService? spacedRepetitionService = null,
            IUserSessionService? userSessionService = null)
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _presenter = presenter ?? throw new ArgumentNullException(nameof(presenter));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _webBookmarkService = webBookmarkService ?? throw new ArgumentNullException(nameof(webBookmarkService));
            _trayIconService = trayIconService ?? throw new ArgumentNullException(nameof(trayIconService));
            _hotkeyService = hotkeyService ?? throw new ArgumentNullException(nameof(hotkeyService));
            _pomodoroService = pomodoroService ?? throw new ArgumentNullException(nameof(pomodoroService));
            _spacedRepetitionService = spacedRepetitionService;
            _userSessionService = userSessionService;

            Load += MainForm_Load;

            _themeService.RegisterThemeable(this);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;


            if (userSwitcherControl != null)
            {
                userSwitcherControl.ApplyTheme(colors);
            }

            if (textBoxProgress != null)
            {
                textBoxProgress.BackColor = colors.Surface;
                textBoxProgress.ForeColor = colors.TextPrimary;
            }

            if (panelTopBar != null)
            {
                panelTopBar.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.White;
            }

            if (labelTopBarTitle != null)
            {
                labelTopBarTitle.ForeColor = colors.TextPrimary;
            }

            if (buttonThemeToggle != null)
            {
                buttonThemeToggle.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.FromArgb(245, 245, 250);
                buttonThemeToggle.ForeColor = colors.TextPrimary;
                buttonThemeToggle.Text = colors.ThemeMode == ThemeMode.Light ? "🌙" : "☀️";
            }

            if (sideNavigation != null)
            {
                sideNavigation.BackColor = colors.ThemeMode == ThemeMode.Dark
                    ? Color.FromArgb(30, 30, 40)
                    : Color.FromArgb(248, 248, 252);
            }

            if (panelContent != null)
            {
                panelContent.BackColor = colors.ThemeMode == ThemeMode.Dark
                    ? Color.FromArgb(20, 20, 25)
                    : Color.FromArgb(245, 245, 250);
            }

            if (splitContainerMain != null)
            {
                splitContainerMain.Panel2.BackColor = colors.ThemeMode == ThemeMode.Dark
                    ? Color.FromArgb(20, 20, 25)
                    : Color.FromArgb(245, 245, 250);
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            if (control == null) return;

            if (control is IThemeable themeable)
            {
                themeable.ApplyTheme(colors);
                return;
            }

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

            if (control.Controls != null)
            {
                foreach (Control child in control.Controls)
                {
                    if (child != null)
                    {
                        ApplyThemeToControl(child, colors);
                    }
                }
            }
        }



        private void MainForm_Load(object? sender, EventArgs e)
        {
            _presenter?.SetView(this);
            _presenter?.Initialize();

            _presenter.OnOpenSettings += Presenter_OnOpenSettings;
            _presenter.OnOpenEditor += Presenter_OnOpenEditor;

            // 设置窗体改为非模态后，用户增删通过事件异步通知主窗体刷新下拉框
            _windowManager.SettingUsersChanged += WindowManager_SettingUsersChanged;

            // 初始化系统托盘
            InitializeTray();

            // 初始化番茄钟托盘集成
            InitializePomodoroTray();

            // 初始化全局快捷键
            InitializeGlobalHotkeys();

            // 绑定FeatureCard点击
            foreach (FeatureCard card in this.dashboardView.Controls.OfType<Panel>()
                .SelectMany(p => p.Controls.OfType<FeatureCard>()))
            {
                card.CardClicked += this.FeatureCard_Clicked;
            }

            // 首页统计卡片下钻：点击任一卡片打开学习数据中心（06 方案 3.4）
            dashboardView.StatCardClicked += (s, index) =>
            {
                try { _windowManager.OpenStatisticsWindow(); }
                catch (Exception ex) { _logger?.LogError(ex, "打开统计中心失败"); }
            };

            RefreshDashboardChallengeProgress();
            sideNavigation.AddItems(new List<NavigationItem>
            {
                new() { Key = "dashboard", Icon = "🏠", Text = "首页", Order = 0, Group = "main" },
                new() { Key = "learning", Icon = "📚", Text = "学习", Order = 1, Group = "main" },
                new() { Key = "editor", Icon = "✏️", Text = "内容编辑", Order = 12, Group = "tools" },
                new() { Key = "pdf", Icon = "📖", Text = "PDF阅读", Order = 2, Group = "main" },
                new() { Key = "browser", Icon = "🌐", Text = "浏览器", Order = 11, Group = "tools" },
                new() { Key = "data", Icon = "📊", Text = "学习数据", Order = 6, Group = "main" },
                new() { Key = "settings", Icon = "⚙️", Text = "设置", Order = 99, Group = "system" }
                //new() { Key = "mentor", Icon = "🤖", Text = "AI导师", Order = 3, Group = "main" },
                //new() { Key = "flashcard", Icon = "🧠", Text = "闪卡复习", Order = 4, Group = "main" },
                //new() { Key = "statistics", Icon = "📊", Text = "学习统计", Order = 5, Group = "main" },
                //new() { Key = "challenges", Icon = "🎯", Text = "每日挑战", Order = 6, Group = "main" },
                //new() { Key = "achievements", Icon = "🏆", Text = "成就徽章", Order = 7, Group = "main" },
                //new() { Key = "notes", Icon = "📝", Text = "笔记", Order = 8, Group = "tools" },
                //new() { Key = "wrongbook", Icon = "📕", Text = "错题本", Order = 9, Group = "tools" },
                //new() { Key = "graph", Icon = "🌐", Text = "知识图谱", Order = 10, Group = "tools" },
            });

        }

        /// <summary>
        /// 初始化系统托盘
        /// </summary>
        private void InitializeTray()
        {
            try
            {
                _trayIconService.Initialize(this);
                _trayIconService.Show();
                _trayIconService.TrayDoubleClick += TrayIconService_TrayDoubleClick;
                _logger.LogInformation("系统托盘已初始化");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化系统托盘失败");
            }
        }

        /// <summary>
        /// 初始化番茄钟托盘集成
        /// </summary>
        private void InitializePomodoroTray()
        {
            try
            {
                _pomodoroTrayIntegration = new PomodoroTrayIntegration(
                    _pomodoroService,
                    _trayIconService,
                    _hotkeyService,
                    Program.GetRequiredService<ILogger<PomodoroTrayIntegration>>());

                _pomodoroTrayIntegration.Initialize();
                _pomodoroTrayIntegration.PomodoroCompleted += PomodoroTrayIntegration_PomodoroCompleted;

                _logger.LogInformation("番茄钟托盘集成已初始化");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化番茄钟托盘集成失败");
            }
        }

        /// <summary>
        /// 初始化全局快捷键
        /// </summary>
        private void InitializeGlobalHotkeys()
        {
            try
            {
                // 设置窗口句柄以接收全局快捷键消息
                HotkeyService.SetWindowHandle(this.Handle);

                // 注册番茄钟快捷键处理
                RegisterPomodoroHotkeyHandlers();

                _logger.LogInformation("全局快捷键已初始化");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化全局快捷键失败");
            }
        }

        /// <summary>
        /// 注册番茄钟快捷键处理
        /// </summary>
        private void RegisterPomodoroHotkeyHandlers()
        {
            // 番茄钟开始/暂停
            var startPauseHotkey = _hotkeyService.GetHotkey("pomodoro_start_pause");
            if (startPauseHotkey != null)
            {
                _hotkeyService.RegisterHotkey("pomodoro_start_pause", startPauseHotkey, (s, e) =>
                {
                    TogglePomodoroStartPause();
                });
            }

            // 番茄钟重置
            var resetHotkey = _hotkeyService.GetHotkey("pomodoro_reset");
            if (resetHotkey != null)
            {
                _hotkeyService.RegisterHotkey("pomodoro_reset", resetHotkey, (s, e) =>
                {
                    _pomodoroService.Reset();
                    _trayIconService.ShowNotification("番茄钟已重置", "番茄钟已重置为初始状态", 3000);
                });
            }

            // 番茄钟跳过
            var skipHotkey = _hotkeyService.GetHotkey("pomodoro_skip");
            if (skipHotkey != null)
            {
                _hotkeyService.RegisterHotkey("pomodoro_skip", skipHotkey, (s, e) =>
                {
                    _pomodoroService.Skip();
                    _trayIconService.ShowNotification("阶段已跳过", "当前阶段已跳过", 3000);
                });
            }
        }

        /// <summary>
        /// 切换番茄钟开始/暂停
        /// </summary>
        private void TogglePomodoroStartPause()
        {
            var state = _pomodoroService.CurrentState;

            if (state == Models.Pomodoro.PomodoroState.Idle)
            {
                _pomodoroService.Start();
                _trayIconService.ShowNotification("番茄钟已开始", "开始专注学习！", 3000);
            }
            else if (state == Models.Pomodoro.PomodoroState.Paused)
            {
                _pomodoroService.Resume();
                _trayIconService.ShowNotification("番茄钟已恢复", "继续专注学习", 3000);
            }
            else
            {
                _pomodoroService.Pause();
                _trayIconService.ShowNotification("番茄钟已暂停", "学习已暂停", 3000);
            }
        }

        /// <summary>
        /// 托盘图标双击事件处理
        /// </summary>
        private void TrayIconService_TrayDoubleClick(object? sender, EventArgs e)
        {
            if (this.Visible)
            {
                _trayIconService.HideToTray();
            }
            else
            {
                _trayIconService.ShowMainWindow();
            }
        }

        /// <summary>
        /// 番茄钟完成事件处理
        /// </summary>
        private void PomodoroTrayIntegration_PomodoroCompleted(object? sender, PomodoroTrayIntegration.PomodoroCompletedEventArgs e)
        {
            // 更新 Dashboard 显示
            RefreshDashboardChallengeProgress();

            // 显示成就通知（如果有）
            _ = Task.Run(async () =>
            {
                try
                {
                    var gamificationService = Program.GetService<IGamificationService>();
                    if (gamificationService != null)
                    {
                        // 完成番茄钟奖励
                        await gamificationService.AddXpAsync(10, "完成番茄钟");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "番茄钟奖励处理失败");
                }
            });
        }


        #region 复用字体，Dispose统一释放
        private readonly Font _fontLogoEmoji = new Font("Segoe UI Emoji", 20F);
        private readonly Font _fontTopTitle = new Font("微软雅黑", 14F, FontStyle.Bold);
        private readonly Font _fontIconBtn = new Font("Segoe UI Emoji", 10F);
        #endregion




        #region Windows Form Designer generated code


        /// <summary>顶部栏自适应位置事件提取为方法，符合设计器规范</summary>
        private void PanelTopBar_Resize(object s, EventArgs e)
        {
            int y = (panelTopBar.Height - labelTopBarLogo.Height) / 2;
            labelTopBarLogo.Location = new Point(16, y);
            labelTopBarTitle.Location = new Point(labelTopBarLogo.Right + 8, y + 2);

            int rightX = panelTopBar.Width - 16;
            buttonThemeToggle.Location = new Point(rightX - buttonThemeToggle.Width, (panelTopBar.Height - buttonThemeToggle.Height) / 2);
            userSwitcherControl.Location = new Point(buttonThemeToggle.Left - 16 - userSwitcherControl.Width, (panelTopBar.Height - userSwitcherControl.Height) / 2);
        }
        #endregion


        private void OnNavigationItemClicked(object? sender, string key)
        {
            switch (key)
            {
                case "dashboard":
                    ShowDashboard();
                    break;
                case "learning":
                    OpenLearningWindowClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case "pdf":
                    _windowManager.OpenPdfReaderWindow();
                    break;
                case "mentor":
                    ShowMentorPanel();
                    break;
                case "flashcard":
                    OpenFlashcardReview();
                    break;
                case "graph":
                    ShowKnowledgeGraph();
                    break;
                case "statistics":
                case "data":
                    _windowManager.OpenStatisticsWindow();
                    break;
                case "notes":
                    _windowManager.OpenNotesWindow();
                    break;
                case "wrongbook":
                    buttonExportErrorBook?.PerformClick();
                    break;
                case "browser":
                    ButtonWebView2Browser_Click(null, EventArgs.Empty);
                    break;
                case "editor":
                    OpenEditorClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case "settings":
                    OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case "challenges":
                    ShowChallengeForm();
                    break;
                case "achievements":
                    ShowAchievementForm();
                    break;
            }
        }

        private void ShowDashboard()
        {
            if (dashboardView == null) return;

            panelContent.Controls.Clear();
            panelContent.Controls.Add(dashboardView);
            dashboardView.BringToFront();
            Text = "🏠 工具 - 首页";
        }

        private void ShowMentorPanel()
        {
            ShowMessageLocal("AI导师功能已移除");
        }

        private void OpenFlashcardReview()
        {
            if (_spacedRepetitionService == null || _userSessionService == null)
            {
                ShowMessageLocal("闪卡复习服务未配置");
                return;
            }

            try
            {
                var form = new FlashcardReviewForm(
                    _spacedRepetitionService,
                    null,
                    _userSessionService);

                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开闪卡复习失败");
                ShowMessageLocal("打开闪卡复习失败");
            }
        }

        private void ShowKnowledgeGraph()
        {
            ShowMessageLocal("知识图谱功能已移除");
        }

        private void ShowMessageLocal(string message)
        {
            MessageBox.Show(message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void FeatureCard_Clicked(object? sender, EventArgs e)
        {
            if (sender is FeatureCard card)
            {
                switch (card.Title)
                {
                    case "开始学习":
                        OpenLearningWindowClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "PDF阅读":
                        _windowManager.OpenPdfReaderWindow();
                        break;
                    case "错题本":
                        buttonExportErrorBook?.PerformClick();
                        break;
                    case "每日挑战":
                        ShowChallengeForm();
                        break;
                    case "成就徽章":
                        ShowAchievementForm();
                        break;
                    case "学习统计":
                        _windowManager.OpenStatisticsWindow();
                        break;
                    case "笔记":
                        _windowManager.OpenNotesWindow();
                        break;
                    case "模版编辑":
                        OpenEditorClicked?.Invoke(this, EventArgs.Empty);
                        break;
                    case "浏览器":
                        ButtonWebView2Browser_Click(null, EventArgs.Empty);
                        break;
                    case "单词消消乐":
                        _windowManager.OpenWordMatchGameWindow();
                        break;
                    case "记忆翻牌":
                        _windowManager.OpenMemoryMatchGameWindow();
                        break;
                    case "连连看":
                        _windowManager.OpenLinkMatchGameWindow();
                        break;
                    case "单词拼写":
                        _windowManager.OpenSpellingGameWindow();
                        break;
                    case "打地鼠":
                        _windowManager.OpenWhackAMoleGameWindow();
                        break;
                    case "设置":
                        _windowManager.OpenSettingsWindow();
                        break;
                }
            }
        }

        private void ShowAchievementForm()
        {
            try
            {
                var gamificationService = Program.GetRequiredService<IGamificationService>();
                var form = new AchievementForm(gamificationService);
                form.StartPosition = FormStartPosition.CenterParent;
                form.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开成就窗口失败");
                MessageBox.Show($"打开成就窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void Presenter_OnOpenSettings(object? sender, EventArgs e)
        {
            _windowManager.OpenSettingsWindow();
            // 用户列表刷新改由 SettingUsersChanged 事件异步触发（设置窗体改为非模态后不再阻塞于此）
        }

        private void WindowManager_SettingUsersChanged(object? sender, EventArgs e)
        {
            // 设置窗体中用户增删成功后异步刷新首页下拉框
            _presenter?.RefreshUsers();
        }

        private void Presenter_OnOpenEditor(object? sender, EventArgs e)
        {
            _windowManager.OpenEditorWindow();
        }

        private void ShowChallengeForm()
        {
            try
            {
                var gamificationService = Program.GetRequiredService<IGamificationService>();
                using var form = new ChallengeForm(gamificationService);
                form.ShowDialog(this);
                RefreshDashboardChallengeProgress();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开每日挑战失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshDashboardChallengeProgress()
        {
            try
            {
                var gamificationService = Program.GetService<IGamificationService>();
                if (gamificationService != null)
                {
                    var challenges = gamificationService.GetDailyChallenges().ToList();
                    int completed = challenges.Count(c => c.Completed);
                    int total = challenges.Count;
                    dashboardView?.UpdateChallengeProgress(completed, total);
                }
            }
            catch
            {
                // 静默处理，不影响主界面加载
            }
        }


        #region IMainView Implementation

        [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public string SelectedUser
        {
            get => userSwitcherControl?.UserName ?? string.Empty;
            set
            {
                if (userSwitcherControl != null)
                    userSwitcherControl.UserName = value;
                if (dashboardView != null)
                    dashboardView.UserName = value;
            }
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
        public event EventHandler? OpenUserComparisonClicked;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void RefreshUserList(IEnumerable<string> users)
        {
            var userList = users.ToList();

            if (userSwitcherControl != null)
            {
                userSwitcherControl.SetUsers(userList);
                if (string.IsNullOrEmpty(userSwitcherControl.UserName) && userList.Count > 0)
                    userSwitcherControl.UserName = userList[0];
            }
        }


        public void UpdateStatus(string status)
        {
            StatusText = status;
        }

        public void UpdateStreakInfo(int consecutiveDays, string studyTimeSummary)
        {
            if (userSwitcherControl != null)
                userSwitcherControl.StreakDays = consecutiveDays;

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

        public void UpdateDashboardStats(int todayStudyMinutes, int streakDays, int totalXP,
            int currentLevel, int xpToNextLevel, int completedChallenges, int totalChallenges,
            int noteCount = 0, int todayNewNotes = 0)
        {
            if (dashboardView != null)
            {
                dashboardView.UpdateDashboardStats(
                    todayStudyMinutes,
                    streakDays,
                    totalXP,
                    currentLevel,
                    xpToNextLevel,
                    completedChallenges,
                    totalChallenges,
                    noteCount,
                    todayNewNotes);
            }
        }

        public void UpdateRecommendations(List<Models.Learning.LearningRecommendation> recommendations)
        {
            if (dashboardView != null)
            {
                dashboardView.UpdateRecommendations(recommendations);
            }
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        //private Panel panelMain;
        private Button buttonLearning;
        private Button buttonSettings;
        private Button buttonOpenEditor;
        private Button buttonOpenPdfReader;
        private Button buttonExportErrorBook;
        private GroupBox groupBoxProgress;
        private TextBox textBoxProgress;
        private Button buttonLearningManagement;
        private Button buttonBrowser;
        private Button buttonWebView2Browser;
        private ToolStripStatusLabel toolStripStatusLabel;
        private StatusStrip statusStrip1;
        private Panel panelStreakInfo;
        private Label labelStreakDays;
        private Label labelStreakIcon;

        // 新布局控件
        private SplitContainer splitContainerMain;
        private SideNavigationPanel sideNavigation;
        private DashboardView dashboardView;
        private Panel panelContent;
        private Panel panelTopBar;
        private Label labelTopBarTitle;
        private Label labelTopBarLogo;
        private UserSwitcherControl userSwitcherControl;
        private Button buttonThemeToggle;
        private bool useNewLayout = true;


        private void InitializeComponent()
        {
            groupBoxProgress = new GroupBox();
            textBoxProgress = new TextBox();
            buttonOpenPdfReader = new Button();
            buttonOpenEditor = new Button();
            buttonSettings = new Button();
            buttonLearning = new Button();
            panelStreakInfo = new Panel();
            labelStreakIcon = new Label();
            labelStreakDays = new Label();
            buttonLearningManagement = new Button();
            buttonWebView2Browser = new Button();
            buttonBrowser = new Button();
            statusStrip1 = new StatusStrip();
            toolStripStatusLabel = new ToolStripStatusLabel();
            splitContainerMain = new SplitContainer();
            sideNavigation = new SideNavigationPanel();
            panelContent = new Panel();
            dashboardView = new DashboardView();
            panelTopBar = new Panel();
            labelTopBarLogo = new Label();
            labelTopBarTitle = new Label();
            userSwitcherControl = new UserSwitcherControl();
            buttonThemeToggle = new Button();
            groupBoxProgress.SuspendLayout();
            panelStreakInfo.SuspendLayout();
            statusStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).BeginInit();
            splitContainerMain.Panel1.SuspendLayout();
            splitContainerMain.Panel2.SuspendLayout();
            splitContainerMain.SuspendLayout();
            panelContent.SuspendLayout();
            panelTopBar.SuspendLayout();
            SuspendLayout();
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
            textBoxProgress.Location = new Point(3, 19);
            textBoxProgress.Multiline = true;
            textBoxProgress.Name = "textBoxProgress";
            textBoxProgress.ReadOnly = true;
            textBoxProgress.ScrollBars = ScrollBars.Vertical;
            textBoxProgress.Size = new Size(552, 156);
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
            buttonOpenEditor.Text = "📝 内容编辑";
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
            buttonLearningManagement.Location = new Point(433, 398);
            buttonLearningManagement.Name = "buttonLearningManagement";
            buttonLearningManagement.Size = new Size(150, 51);
            buttonLearningManagement.TabIndex = 18;
            buttonLearningManagement.Text = "📋 学习管理";
            buttonLearningManagement.UseVisualStyleBackColor = false;
            buttonLearningManagement.Click += ButtonLearningManagement_Click;
            // 
            // buttonWebView2Browser
            // 
            buttonWebView2Browser.BackColor = Color.FromArgb(255, 152, 0);
            buttonWebView2Browser.FlatAppearance.BorderSize = 0;
            buttonWebView2Browser.FlatStyle = FlatStyle.Flat;
            buttonWebView2Browser.ForeColor = Color.White;
            buttonWebView2Browser.Location = new Point(232, 398);
            buttonWebView2Browser.Name = "buttonWebView2Browser";
            buttonWebView2Browser.Size = new Size(150, 51);
            buttonWebView2Browser.TabIndex = 23;
            buttonWebView2Browser.Text = "🌐 浏览器";
            buttonWebView2Browser.UseVisualStyleBackColor = false;
            buttonWebView2Browser.Click += ButtonWebView2Browser_Click;
            // 
            // buttonBrowser
            // 
            buttonBrowser.Location = new Point(0, 0);
            buttonBrowser.Name = "buttonBrowser";
            buttonBrowser.Size = new Size(75, 23);
            buttonBrowser.TabIndex = 0;
            // 
            // statusStrip1
            // 
            statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel });
            statusStrip1.Location = new Point(0, 690);
            statusStrip1.Name = "statusStrip1";
            statusStrip1.Size = new Size(1593, 22);
            statusStrip1.TabIndex = 2;
            // 
            // toolStripStatusLabel
            // 
            toolStripStatusLabel.Name = "toolStripStatusLabel";
            toolStripStatusLabel.Size = new Size(32, 17);
            toolStripStatusLabel.Text = "就绪";
            // 
            // splitContainerMain
            // 
            splitContainerMain.Dock = DockStyle.Fill;
            splitContainerMain.FixedPanel = FixedPanel.Panel1;
            splitContainerMain.Location = new Point(0, 0);
            splitContainerMain.Name = "splitContainerMain";
            // 
            // splitContainerMain.Panel1
            // 
            splitContainerMain.Panel1.Controls.Add(sideNavigation);
            // 
            // splitContainerMain.Panel2
            // 
            splitContainerMain.Panel2.Controls.Add(panelContent);
            splitContainerMain.Panel2.Controls.Add(panelTopBar);
            splitContainerMain.Size = new Size(1593, 690);
            splitContainerMain.SplitterDistance = 121;
            splitContainerMain.SplitterWidth = 1;
            splitContainerMain.TabIndex = 1;
            // 
            // sideNavigation
            // 
            sideNavigation.BackColor = Color.FromArgb(248, 248, 252);
            sideNavigation.Dock = DockStyle.Fill;
            sideNavigation.Location = new Point(0, 0);
            sideNavigation.Name = "sideNavigation";
            sideNavigation.Size = new Size(121, 690);
            sideNavigation.TabIndex = 0;
            sideNavigation.NavigationItemClicked += OnNavigationItemClicked;
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.FromArgb(245, 245, 250);
            panelContent.Controls.Add(dashboardView);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(0, 56);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(1471, 634);
            panelContent.TabIndex = 1;
            // 
            // dashboardView
            // 
            dashboardView.BackColor = Color.FromArgb(245, 245, 250);
            dashboardView.Dock = DockStyle.Fill;
            dashboardView.Location = new Point(0, 0);
            dashboardView.Name = "dashboardView";
            dashboardView.Size = new Size(1471, 634);
            dashboardView.TabIndex = 0;
            // 
            // panelTopBar
            // 
            panelTopBar.BackColor = Color.White;
            panelTopBar.Controls.Add(labelTopBarLogo);
            panelTopBar.Controls.Add(labelTopBarTitle);
            panelTopBar.Controls.Add(userSwitcherControl);
            panelTopBar.Controls.Add(buttonThemeToggle);
            panelTopBar.Dock = DockStyle.Top;
            panelTopBar.Location = new Point(0, 0);
            panelTopBar.Name = "panelTopBar";
            panelTopBar.Padding = new Padding(16, 0, 16, 0);
            panelTopBar.Size = new Size(1471, 56);
            panelTopBar.TabIndex = 0;
            panelTopBar.Resize += PanelTopBar_Resize;
            // 
            // labelTopBarLogo
            // 
            labelTopBarLogo.AutoSize = true;
            labelTopBarLogo.Location = new Point(3, 0);
            labelTopBarLogo.Name = "labelTopBarLogo";
            labelTopBarLogo.Size = new Size(30, 21);
            labelTopBarLogo.TabIndex = 0;
            labelTopBarLogo.Text = "📚";
            labelTopBarLogo.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelTopBarTitle
            // 
            labelTopBarTitle.AutoSize = true;
            labelTopBarTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelTopBarTitle.Location = new Point(34, 0);
            labelTopBarTitle.Name = "labelTopBarTitle";
            labelTopBarTitle.Size = new Size(74, 21);
            labelTopBarTitle.TabIndex = 1;
            labelTopBarTitle.Text = "工具";
            labelTopBarTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // userSwitcherControl
            // 
            userSwitcherControl.Location = new Point(0, 0);
            userSwitcherControl.Name = "userSwitcherControl";
            userSwitcherControl.Size = new Size(160, 34);
            userSwitcherControl.TabIndex = 2;
            userSwitcherControl.UserSelected += UserSwitcherControl_UserSelected;
            userSwitcherControl.OpenSettingsClicked += UserSwitcherControl_OpenSettingsClicked;
            // 
            // buttonThemeToggle
            // 
            buttonThemeToggle.BackColor = Color.FromArgb(245, 245, 250);
            buttonThemeToggle.Cursor = Cursors.Hand;
            buttonThemeToggle.FlatAppearance.BorderSize = 0;
            buttonThemeToggle.FlatStyle = FlatStyle.Flat;
            buttonThemeToggle.ForeColor = Color.FromArgb(33, 33, 33);
            buttonThemeToggle.Location = new Point(0, 0);
            buttonThemeToggle.Name = "buttonThemeToggle";
            buttonThemeToggle.Size = new Size(36, 28);
            buttonThemeToggle.TabIndex = 4;
            buttonThemeToggle.Text = "🌙";
            buttonThemeToggle.UseVisualStyleBackColor = false;
            buttonThemeToggle.Click += ButtonThemeToggle_Click;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(1593, 712);
            Controls.Add(splitContainerMain);
            Controls.Add(statusStrip1);
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Regular, GraphicsUnit.Point, 134);
            Margin = new Padding(4);
            Name = "MainForm";
            Text = "🏠 工具 - 首页";
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
            panelStreakInfo.ResumeLayout(false);
            statusStrip1.ResumeLayout(false);
            statusStrip1.PerformLayout();
            splitContainerMain.Panel1.ResumeLayout(false);
            splitContainerMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainerMain).EndInit();
            splitContainerMain.ResumeLayout(false);
            panelContent.ResumeLayout(false);
            panelTopBar.ResumeLayout(false);
            panelTopBar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();


        }

        #endregion

        #region Event Handlers

        private void UserSwitcherControl_UserSelected(object? sender, string userName)
        {
            // 更新界面选中并通知 Presenter 切换用户会话（复用 UserChanged 链路，保持对外接口不变）
            if (dashboardView != null)
                dashboardView.UserName = userName;
            UserChanged?.Invoke(this, EventArgs.Empty);

            // 切换用户后为其幂等创建默认日报提醒（不存在才创建，保证新用户也能收到日报）
            EnsureDailyReportReminder(userName);
        }

        private void EnsureDailyReportReminder(string userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId)) return;
                var svc = Program.GetService<ILearningDailyReportReminderService>();
                if (svc == null) return;
                svc.EnsureDefaultSummaryReminder(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "为用户创建默认日报提醒失败：{UserId}", userId);
            }
        }

        private void UserSwitcherControl_OpenSettingsClicked(object? sender, EventArgs e)
        {
            // 添加/管理用户统一跳转设置窗体，首页不再内联新增（01 方案 3.2-3.4）
            OpenSettingsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonThemeToggle_Click(object? sender, EventArgs e)
        {
            if (_themeService == null) return;

            var currentTheme = _themeService.CurrentTheme;
            var newTheme = currentTheme == ThemeMode.Light
                ? ThemeMode.Dark
                : ThemeMode.Light;
            _themeService.SetTheme(newTheme);

            buttonThemeToggle.Text = newTheme == ThemeMode.Light ? "🌙" : "☀️";
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

        private void ButtonWebView2Browser_Click(object? sender, EventArgs e)
        {
            _windowManager.OpenAIWebViewWindow();
        }



        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fontLogoEmoji?.Dispose();
                _fontTopTitle?.Dispose();
                _fontIconBtn?.Dispose();

                // 清理番茄钟托盘集成
                _pomodoroTrayIntegration?.Dispose();

                // 清理系统托盘
                _trayIconService?.Cleanup();

                if (_presenter != null)
                {
                    _presenter.OnOpenSettings -= Presenter_OnOpenSettings;
                    _presenter.OnOpenEditor -= Presenter_OnOpenEditor;
                    (_presenter as IDisposable)?.Dispose();
                }

                _windowManager.SettingUsersChanged -= WindowManager_SettingUsersChanged;

                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

    }
}
