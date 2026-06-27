using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    public partial class LearningForm : Form, ILearningView, IThemeable
    {
        #region === 依赖服务 ===
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISoundService _soundService;
        private readonly IThemeService _themeService;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private readonly IEncouragementService _encouragementService;
        private readonly IThinkingStimulator? _thinkingStimulator;
        private readonly IAchievementService? _achievementService;
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly IGamificationService _gamificationService;
        private readonly IEventBus? _eventBus;
        private readonly IUserSessionService? _userSessionService;
        private readonly IPomodoroService? _pomodoroService;
        private readonly ConfettiManager _confettiManager;
        private readonly EncouragementManager _encouragementManager;
        private readonly IConversationContextService? _conversationContextService;
        private MentorAIPanel? _mentorPanel;
        #endregion

        #region === 学习状态 ===
        private LearningItem? _currentItem;
        private bool _isShowAnswer = false;
        private bool _answerRevealed = false;
        private bool _isFavorite = false;
        private bool _currentNoteCounted = false;
        private bool _disposed = false;
        private Settings _settings = new();
        private bool _autoPlayEnabled = false;
        private readonly System.Windows.Forms.Timer _autoPlayTimer = new System.Windows.Forms.Timer();
        private int _autoPlayDelaySeconds = 5;
        private HashSet<string>? _cachedFavorites;
        private DateTime _favoritesCacheTime = DateTime.MinValue;
        private static readonly TimeSpan FavoritesCacheDuration = TimeSpan.FromSeconds(10);
        #endregion

        #region === 进度可视化 ===
        private CircularProgressControl _dailyGoalProgress = null!;
        private const int DailyGoal = 30;
        #endregion

        #region === 笔记增强 ===
        private Label _noteWordCountLabel = null!;
        private ToolStrip _noteFormattingToolbar = null!;
        #endregion

        #region === 卡片展示 ===
        private LearningCard _learningCard = null!;
        #endregion

        #region === AI历史 ===
        private AIHistoryPanel _aiHistoryPanel = null!;
        #endregion

        #region === UI 状态 ===
        private bool _settingsChangedEventsSuspended = false;
        private bool _isGameActive = false;
        private bool _isConfettiActive = false;
        private ToolTip _toolTip = new ToolTip();
        #endregion

        #region === 统计数据 ===
        private readonly System.Windows.Forms.Timer _studyTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _confettiTimer = new System.Windows.Forms.Timer();
        private TimeSpan _studyDuration = TimeSpan.Zero;
        private int _encouragementCounter = 0;
        private int _celebrationCounter = 0;
        private int _quizCorrectCount = 0;
        private int _favoriteCount = 0;
        private int _noteCount = 0;
        private int _totalLearnedCount = 0;
        private int _gameScore = 0;
        private readonly System.Windows.Forms.Timer _noteSaveTimer = new System.Windows.Forms.Timer();
        #endregion

        #region === 静态资源 ===
        private const int EncouragementInterval = 3;
        private const int CelebrationInterval = 5;

        // 列表绘制相关
        private readonly SolidBrush _selectedBackgroundBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
        private readonly SolidBrush _selectedForegroundBrush = new SolidBrush(Color.White);
        private readonly SolidBrush _normalForegroundBrush = new SolidBrush(Color.Black);
        private readonly Pen _selectedBorderPen = new Pen(Color.White, 2);
        private readonly SolidBrush _hoverBackgroundBrush = new SolidBrush(Color.FromArgb(232, 245, 233));
        private int _hoverIndex = -1;
        #endregion

        #region === 子视图实例 ===
        private LearningListView _listView = null!;
        private LearningContentView _contentView = null!;
        private LearningButtonsView _buttonsView = null!;
        private LearningStatsView _statsView = null!;
        private LearningStatsButtonView _statsButtonView = null!;
        private LearningProcessStatsView _statsProgressView = null!;
        private LearningSettingsView _settingsView = null!;
        #endregion

        #region === 控件访问器 ===
        private Panel panelContent => _contentView.PanelContent;
        private ListBox listBoxItems => _listView.ListBoxItems;
        private Label labelListStatus => _listView.LabelListStatus;
        private Panel panelConfig => _settingsView.PanelConfig;
        private Label labelStudyTime => _statsView.LabelStudyTime;
        private Label labelScore => _statsView.LabelScore;
        private Label labelTodayCount => _statsView.LabelTodayCount;
        private Label labelStreak => _statsView.LabelStreak;
        private Label labelEncouragement => _statsView.LabelEncouragement;
        private ProgressBar progressBar1 => _statsProgressView.ProgressBar;
        private Label labelStatistics => _statsProgressView.LabelStatistics;
        private Panel panelQuizMode => _settingsView.PanelQuizMode;
        private Button buttonShowAnswer => _settingsView.ButtonShowAnswer;
        private Label labelQuizHint => _settingsView.LabelQuizHint;
        private Button buttonThemeToggle => _settingsView.ButtonThemeToggle;
        private CheckBox checkBoxVoice => _settingsView.CheckBoxVoice;
        private RadioButton radioOriginal => _settingsView.RadioOriginal;
        private RadioButton radioExplanation => _settingsView.RadioExplanation;
        private RadioButton radioBoth => _settingsView.RadioBoth;
        private RadioButton radioStudyMode => _settingsView.RadioStudyMode;
        private RadioButton radioQuickMode => _settingsView.RadioQuickMode;
        private RadioButton radioSequential => _settingsView.RadioSequential;
        private RadioButton radioRandom => _settingsView.RadioRandom;
        private ComboBox comboBoxSubject => _settingsView.ComboBoxSubject;
        private ComboBox comboBoxSubCategory => _settingsView.ComboBoxSubCategory;
        private Panel panelNotes => _contentView.PanelNotes;
        private RichTextBox richTextBoxNotes => _contentView.RichTextBoxNotes;
        private Button buttonPronounce => _buttonsView.ButtonPronounce;
        private Button buttonFavorite => _buttonsView.ButtonFavorite;
        private Button buttonNote => _buttonsView.ButtonNote;
        private Button buttonKnown => _buttonsView.ButtonKnown;
        private Button buttonUnknown => _buttonsView.ButtonUnknown;

        private Label labelDailyGoal = new Label();
        #endregion

        #region === 布局控件 ===
        private TableLayoutPanel mainTableLayoutPanel = null!;
        private Panel middlePanel = null!;
        private TableLayoutPanel middleTableLayoutPanel = null!;
        #endregion

        #region === 游戏相关控件 ===
        private FlowLayoutPanel flowLayoutPanelBadges = null!;
        private FlowLayoutPanel flowLayoutPanelChallenges = null!;
        private Label labelLevel = null!;
        private ProgressBar progressXP = null!;
        private Label labelXP = null!;
        private Panel panelGame = null!;
        private Label labelGameQuestion = null!;
        private TextBox textBoxGameAnswer = null!;
        private Label labelGameResult = null!;
        private System.Windows.Forms.Timer _gameTimer = null!;
        private FloatingText _floatingText = null!;
        #endregion

        #region === 费曼学习面板 ===
        private FeynmanLearningPanel? _feynmanPanel;
        private Panel? _feynmanContainerPanel;
        private bool _isFeynmanPanelVisible = false;
        private readonly FeynmanHistoryService _feynmanHistoryService = new();
        private SpeechService? _speechService;
        private bool _isDictationActive = false;
        private LevelBadge _levelBadge;
        #endregion

        #region === 设计器生成 ===
        private System.ComponentModel.IContainer components = null;
        #endregion

        #region === 构造函数 ===
        public LearningForm(
            IAiQuestionService aiQuestionService,
            ITTSService ttsService,
            ILogger<LearningForm> logger,
            ILoggerFactory loggerFactory,
            ISoundService soundService,
            IThemeService themeService,
            IAIPanelPopupService aiPanelPopupService,
            IEncouragementService encouragementService,
            IThinkingStimulator? thinkingStimulator = null,
            IAchievementService? achievementService = null,
            ISpacedRepetitionService? spacedRepetitionService = null,
            IGamificationService? gamificationService = null,
            IEventBus? eventBus = null,
            IUserSessionService? userSessionService = null,
            IConversationContextService? conversationContextService = null,
            IPomodoroService? pomodoroService = null)
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _aiPanelPopupService = aiPanelPopupService ?? throw new ArgumentNullException(nameof(aiPanelPopupService));
            _encouragementService = encouragementService ?? throw new ArgumentNullException(nameof(encouragementService));
            _thinkingStimulator = thinkingStimulator;
            _achievementService = achievementService;
            _spacedRepetitionService = spacedRepetitionService;
            _eventBus = eventBus;
            _userSessionService = userSessionService;
            _conversationContextService = conversationContextService;
            _pomodoroService = pomodoroService;
            _gamificationService = gamificationService ?? new GamificationService(
                _loggerFactory,
                null);

            _gamificationService.BadgesUnlocked += OnBadgesUnlocked;
            _gamificationService.LevelUp += OnLevelUp;
            _gamificationService.XPChanged += OnXPChanged;

            if (_pomodoroService != null)
            {
                _pomodoroService.PomodoroCompleted += PomodoroService_PomodoroCompleted;
                _pomodoroService.StateChanged += PomodoroService_StateChanged;
            }

            _encouragementManager = new EncouragementManager();

            _confettiManager = new ConfettiManager();

            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;

            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

            _themeService.RegisterThemeable(this);

            // 笔记保存计时器
            _noteSaveTimer.Tick += NoteSaveTimer_Tick;

            // 自动播放计时器
            _autoPlayTimer.Interval = 5000;
            _autoPlayTimer.Tick += AutoPlayTimer_Tick;

        }
        #endregion

        private void BindSubViewEvents()
        {
            _buttonsView.KnownClicked += ButtonKnown_Click;
            _buttonsView.UnknownClicked += ButtonUnknown_Click;
            _buttonsView.PronounceClicked += ButtonPronounce_Click;
            _buttonsView.FavoriteClicked += ButtonFavorite_Click;
            _buttonsView.NoteClicked += ButtonNote_Click;
            _buttonsView.ExitClicked += ButtonExit_Click;
            _buttonsView.AIAskClicked += ButtonAIAsk_Click;
            _buttonsView.FeynmanClicked += ButtonFeynman_Click;

            _settingsView.RadioStudyMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioQuickMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioSequential.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioRandom.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.ComboBoxSubject.SelectedIndexChanged += ComboBoxSubject_SelectedIndexChanged;
            _settingsView.ComboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            _settingsView.ButtonOpenStatistics.Click += ButtonOpenStatistics_Click;
            _settingsView.ButtonExportErrorBook.Click += ButtonExportErrorBook_Click;
            _settingsView.ButtonShowAnswer.Click += ButtonShowAnswer_Click;
            _settingsView.ButtonThemeToggle.Click += ButtonThemeToggle_Click;

            _contentView.ContentClicked += LabelContent_Click;
            _contentView.DetailClicked += ListBoxDisplay_Click;
            _contentView.NoteTextChanged += RichTextBoxNotes_TextChanged;

            _listView.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;

            _statsButtonView.AchievementsClicked += ButtonAchievements_Click;
            _statsButtonView.ChallengesClicked += ButtonChallenges_Click;
            _statsButtonView.ReviewClicked += ButtonReview_Click;
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (panelContent != null)
            {
                panelContent.BackColor = colors.Surface;
            }

            if (panelConfig != null)
            {
                panelConfig.BackColor = colors.Surface;
            }


            if (panelQuizMode != null)
            {
                panelQuizMode.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Surface : Color.FromArgb(255, 248, 220);
            }

            if (middlePanel != null)
            {
                middlePanel.BackColor = colors.Background;
            }

            if (_buttonsView != null && _buttonsView.ButtonsPanel != null)
            {
                _buttonsView.ButtonsPanel.BackColor = colors.Background;
            }

            if (buttonKnown != null)
            {
                buttonKnown.ForeColor = Color.White;
            }

            if (buttonUnknown != null)
            {
                buttonUnknown.ForeColor = Color.White;
            }

            if (labelStudyTime != null)
            {
                labelStudyTime.ForeColor = colors.TextPrimary;
            }

            if (labelScore != null)
            {
                labelScore.ForeColor = colors.TextPrimary;
            }

            if (labelTodayCount != null)
            {
                labelTodayCount.ForeColor = colors.TextPrimary;
            }

            if (labelStreak != null)
            {
                labelStreak.ForeColor = colors.ThemeMode == ThemeMode.Dark ? colors.Accent : Color.FromArgb(255, 140, 0);
            }

            if (labelEncouragement != null)
            {
                labelEncouragement.ForeColor = colors.TextSecondary;
            }

            if (labelDailyGoal != null)
            {
                labelDailyGoal.ForeColor = colors.TextSecondary;
            }

            if (labelQuizHint != null)
            {
                labelQuizHint.ForeColor = colors.TextSecondary;
            }

            if (listBoxItems != null)
            {
                listBoxItems.ForeColor = colors.TextPrimary;
                listBoxItems.BackColor = colors.Surface;
            }

            if (labelListStatus != null)
            {
                labelListStatus.ForeColor = colors.TextSecondary;
                labelListStatus.BackColor = colors.Surface;
            }

            if (panelNotes != null)
            {
                panelNotes.BackColor = colors.Surface;
            }

            if (richTextBoxNotes != null)
            {
                richTextBoxNotes.ForeColor = colors.TextPrimary;
                richTextBoxNotes.BackColor = colors.Surface;
            }

            if (_noteFormattingToolbar != null)
            {
                _noteFormattingToolbar.BackColor = colors.Surface;
                _noteFormattingToolbar.ForeColor = colors.TextPrimary;
                _noteFormattingToolbar.Renderer = new ToolStripProfessionalRenderer(
                    new ThemeColorTable(colors));
            }

            if (_noteWordCountLabel != null)
            {
                _noteWordCountLabel.ForeColor = colors.TextSecondary;
                _noteWordCountLabel.BackColor = colors.Surface;
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
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
            else if (control is CheckBox checkBox)
            {
                checkBox.ForeColor = colors.TextPrimary;
                checkBox.BackColor = colors.Surface;
            }
            else if (control is RadioButton radioButton)
            {
                radioButton.ForeColor = colors.TextPrimary;
                radioButton.BackColor = colors.Surface;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = colors.Surface;
                comboBox.ForeColor = colors.TextPrimary;
            }
            else if (control is FlowLayoutPanel flowLayoutPanel)
            {
                flowLayoutPanel.BackColor = colors.Background;
            }
            else if (control is RichTextBox richTextBox)
            {
                richTextBox.ForeColor = colors.TextPrimary;
                richTextBox.BackColor = colors.Surface;
            }
            else if (control is ProgressBar progressBar)
            {
                progressBar.BackColor = colors.Surface;
            }
            else if (control is TableLayoutPanel tableLayoutPanel)
            {
                tableLayoutPanel.BackColor = colors.Background;
            }
            else if (control is ListBox listBox)
            {
                listBox.ForeColor = colors.TextPrimary;
                listBox.BackColor = colors.Surface;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }



        private void LearningForm_Load(object? sender, EventArgs e)
        {
            BindSubViewEvents();
            LoadSettings();
            ApplySettings();
            EnableListHighlighting(true);
            InitializeEnhancedFeatures();
        }


        private void LearningForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
            try
            {
                _gamificationService?.Save();

                if (_ttsService != null)
                {
                    _ttsService.StopAsync().GetAwaiter().GetResult();
                }

                ShowStudySessionSummary();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to stop TTS service");
            }
        }

        private void ShowStudySessionSummary()
        {
            if (_totalLearnedCount == 0 && _favoriteCount == 0 && _noteCount == 0)
                return;

            int correctCount = _quizCorrectCount;
            int wrongCount = Math.Max(0, _totalLearnedCount - correctCount);
            string accuracy = _totalLearnedCount > 0
                ? $"{(correctCount * 100.0 / _totalLearnedCount):F1}%"
                : "N/A";

            // 计算进度条
            int progressBarWidth = 30;
            int filledCount = _totalLearnedCount > 0 ? Math.Min(correctCount, progressBarWidth) : 0;
            string progressBar = new string('█', filledCount) + new string('░', progressBarWidth - filledCount);

            string summary = $"📚 本次学习总结\n" +
                           $"{"─".PadRight(28, '─')}\n" +
                           $"⏱️ 学习时长: {_studyDuration.Hours:D2}:{_studyDuration.Minutes:D2}:{_studyDuration.Seconds:D2}\n" +
                           $"\n" +
                           $"📈 学习进度:\n" +
                           $"[{progressBar}] {accuracy}\n" +
                           $"   ✅ 学会了: {correctCount} 项\n" +
                           $"   ❌ 不会的: {wrongCount} 项\n" +
                           $"\n" +
                           $"⭐ 收藏了: {_favoriteCount} 项\n" +
                           $"📝 添加笔记: {_noteCount} 条\n" +
                           $"{"─".PadRight(28, '─')}\n" +
                           $"继续加油，保持学习热情！💪";

            MessageBox.Show(summary, "学习总结", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LoadSettings()
        {
            try
            {
                // 填充学科下拉框
                InitSubjectComboBox();

                string settingsPath = GetUserSettingsPath();
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        _settings = settings;

                        // 从旧版设置迁移：Language -> Subject
                        if (string.IsNullOrEmpty(_settings.Subject))
                        {
                            if (_settings.Language == "Chinese")
                                _settings.Subject = Constants.Subject.Chinese;
                            else if (_settings.Language == "English")
                                _settings.Subject = Constants.Subject.English;
                            else
                                _settings.Subject = Constants.Subject.English;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }

        private void InitSubjectComboBox()
        {
            comboBoxSubject.Items.Clear();
            var subjects = new List<string>
            {
                Constants.Subject.Chinese,
                Constants.Subject.English,
                Constants.Subject.Math,
                Constants.Subject.Physics,
                Constants.Subject.Chemistry,
                Constants.Subject.History,
                Constants.Subject.Geography,
                Constants.Subject.Biology
            };
            foreach (var subject in subjects)
            {
                comboBoxSubject.Items.Add(subject);
            }
            if (comboBoxSubject.Items.Count > 0)
                comboBoxSubject.SelectedIndex = 1; // 默认选英语
        }

        private void SaveSettings()
        {
            try
            {
                _settings.IsVoiceEnabled = checkBoxVoice.Checked;
                if (radioOriginal.Checked) _settings.PronunciationScope = 0;
                else if (radioExplanation.Checked) _settings.PronunciationScope = 1;
                else _settings.PronunciationScope = 2;
                _settings.LearningMode = radioStudyMode.Checked ? Constants.LearningMode.Study : Constants.LearningMode.Quick;
                _settings.SortOrder = radioSequential.Checked ? Constants.SortOrder.Sequential : Constants.SortOrder.Random;
                _settings.Subject = comboBoxSubject.Text;
                _settings.SubCategory = comboBoxSubCategory.Text;
                string settingsPath = GetUserSettingsPath();
                var dir = Path.GetDirectoryName(settingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
        }

        private void ApplySettings()
        {
            // 临时禁用事件，避免在应用设置时触发 SettingsChanged 事件
            SuspendSettingsChangedEvents();

            // 暂停布局更新，提高性能
            SuspendLayout();

            try
            {
                // 只有在值真正变化时才设置属性，避免不必要的事件触发
                if (checkBoxVoice.Checked != _settings.IsVoiceEnabled)
                    checkBoxVoice.Checked = _settings.IsVoiceEnabled;

                // 处理发音范围单选按钮
                bool shouldSetOriginal = _settings.PronunciationScope == 0 && !radioOriginal.Checked;
                bool shouldSetExplanation = _settings.PronunciationScope == 1 && !radioExplanation.Checked;
                bool shouldSetBoth = _settings.PronunciationScope == 2 && !radioBoth.Checked;

                if (shouldSetOriginal) radioOriginal.Checked = true;
                else if (shouldSetExplanation) radioExplanation.Checked = true;
                else if (shouldSetBoth) radioBoth.Checked = true;

                // 处理学习模式单选按钮
                bool shouldSetQuick = _settings.LearningMode == "Quick" && !radioQuickMode.Checked;
                bool shouldSetStudy = _settings.LearningMode != "Quick" && !radioStudyMode.Checked;

                if (shouldSetQuick) radioQuickMode.Checked = true;
                else if (shouldSetStudy) radioStudyMode.Checked = true;

                // 同步 _isShowAnswer 状态
                _isShowAnswer = radioQuickMode.Checked;

                // 处理排序方式单选按钮
                bool shouldSetRandom = _settings.SortOrder == "Random" && !radioRandom.Checked;
                bool shouldSetSequential = _settings.SortOrder != "Random" && !radioSequential.Checked;

                if (shouldSetRandom) radioRandom.Checked = true;
                else if (shouldSetSequential) radioSequential.Checked = true;

                // 处理学科下拉框
                if (!string.IsNullOrEmpty(_settings.Subject))
                {
                    var index = comboBoxSubject.Items.IndexOf(_settings.Subject);
                    if (index >= 0 && comboBoxSubject.SelectedIndex != index)
                    {
                        comboBoxSubject.SelectedIndex = index;
                    }
                }

                // 处理子分类
                if (!string.IsNullOrEmpty(_settings.SubCategory))
                {
                    var index = comboBoxSubCategory.Items.IndexOf(_settings.SubCategory);
                    if (index >= 0 && comboBoxSubCategory.SelectedIndex != index)
                    {
                        comboBoxSubCategory.SelectedIndex = index;
                    }
                }

            }
            finally
            {
                // 恢复布局更新
                ResumeLayout();
                // 恢复事件
                ResumeSettingsChangedEvents();
            }
        }

        private void SuspendSettingsChangedEvents() => _settingsChangedEventsSuspended = true;
        private void ResumeSettingsChangedEvents() => _settingsChangedEventsSuspended = false;

        #region ILearningView Implementation

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentContent
        {
            set
            {
                if (_learningCard != null)
                {
                    _learningCard.Title = value;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayText
        {
            set
            {
                if (_learningCard != null && !string.IsNullOrEmpty(value))
                {
                    _learningCard.Content = value;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayStruct
        {
            set
            {
                if (_learningCard != null && !string.IsNullOrEmpty(value))
                {
                    _learningCard.Content = value;
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public LearningItem? CurrentItem
        {
            set
            {
                _currentItem = value;
                if (_currentItem != null)
                {
                    UpdateLearningCard();
                    ResetDetailState();
                }
            }
        }

        private void UpdateLearningCard()
        {
            if (_learningCard == null || _currentItem == null) return;

            _learningCard.Title = _currentItem.GetMainContent();
            _learningCard.Content = _currentItem.GetDisplayText();
            _learningCard.Category = comboBoxSubCategory.Text;
            _learningCard.Icon = GetSubjectIcon();
            _learningCard.AccentColor = GetSubjectColor();
            _learningCard.IsSelected = true;
        }

        private string GetSubjectIcon()
        {
            string subject = comboBoxSubject.Text;
            return subject switch
            {
                "语文" => "📖",
                "英语" => "🔤",
                "数学" => "🔢",
                "物理" => "⚛️",
                "化学" => "🧪",
                "历史" => "🏛️",
                "地理" => "🌍",
                "生物" => "🧬",
                _ => "📚"
            };
        }

        private Color GetSubjectColor()
        {
            string subject = comboBoxSubject.Text;
            return subject switch
            {
                "语文" => Color.FromArgb(244, 67, 54),
                "英语" => Color.FromArgb(33, 150, 243),
                "数学" => Color.FromArgb(156, 39, 176),
                "物理" => Color.FromArgb(0, 188, 212),
                "化学" => Color.FromArgb(255, 152, 0),
                "历史" => Color.FromArgb(76, 175, 80),
                "地理" => Color.FromArgb(79, 109, 255),
                "生物" => Color.FromArgb(96, 125, 139),
                _ => Color.FromArgb(76, 175, 80)
            };
        }

        /// <summary>
        /// 重置详情区状态（切换学习项时调用）
        /// </summary>
        private void ResetDetailState()
        {
            _answerRevealed = false;
            _currentNoteCounted = false;

            UpdateDetailState(true, !_isShowAnswer);

            ResetFavoriteState();
        }

        /// <summary>
        /// 重置收藏状态（切换学习项时调用）
        /// </summary>
        private void ResetFavoriteState()
        {
            if (_currentItem == null)
            {
                _isFavorite = false;
                UpdateFavoriteButton();
                return;
            }

            // 检查当前项是否已收藏
            try
            {
                string favoritesPath = GetUserFavoritesPath();
                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    var favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                    string key = GetItemKey();
                    _isFavorite = favorites.Contains(key);

                    // 兼容旧数据：检查旧键
                    if (!_isFavorite)
                    {
                        string oldKey = _currentItem.GetMainContent();
                        if (favorites.Contains(oldKey))
                        {
                            _isFavorite = true;
                            // 迁移到新键
                            favorites.Remove(oldKey);
                            favorites.Add(key);
                            File.WriteAllText(favoritesPath, JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true }));
                        }
                    }
                }
                else
                {
                    _isFavorite = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to reset favorite state");
                _isFavorite = false;
            }

            UpdateFavoriteButton();
        }

        /// <summary>
        /// 更新收藏按钮显示状态
        /// </summary>
        private void UpdateFavoriteButton()
        {
            if (_isFavorite)
            {
                buttonFavorite.BackColor = Color.FromArgb(255, 152, 0);
                buttonFavorite.Text = "❤️ 已收藏";
            }
            else
            {
                buttonFavorite.BackColor = Color.FromArgb(255, 193, 7);
                buttonFavorite.Text = "⭐ 收藏";
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string Statistics
        {
            set => labelStatistics.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ProgressValue
        {
            set => progressBar1.Value = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int ProgressMax
        {
            set => progressBar1.Maximum = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsVoiceEnabled
        {
            get => checkBoxVoice.Checked;
            set => checkBoxVoice.Checked = value;
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PronunciationScope PronunciationScope
        {
            get
            {
                if (radioOriginal.Checked) return PronunciationScope.Original;
                if (radioExplanation.Checked) return PronunciationScope.Explanation;
                return PronunciationScope.Both;
            }
            set
            {
                switch (value)
                {
                    case PronunciationScope.Original:
                        radioOriginal.Checked = true;
                        break;
                    case PronunciationScope.Explanation:
                        radioExplanation.Checked = true;
                        break;
                    case PronunciationScope.Both:
                        radioBoth.Checked = true;
                        break;
                }
            }
        }

        public string CurrentMode => radioStudyMode.Checked ? Constants.LearningMode.Study : Constants.LearningMode.Quick;

        public string LearningMode => radioStudyMode.Checked ? Constants.LearningMode.Study : Constants.LearningMode.Quick;

        public string SortOrder => radioSequential.Checked ? "Sequential" : "Random";

        public string Subject => comboBoxSubject.Text;

        public string Language
        {
            get
            {
                if (Subject == Constants.Subject.Chinese) return Constants.Language.Chinese;
                if (Subject == Constants.Subject.English) return Constants.Language.English;
                return Constants.Language.Chinese;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SubCategory
        {
            get => comboBoxSubCategory.Text;
            set
            {
                var index = comboBoxSubCategory.Items.IndexOf(value);
                if (index >= 0)
                {
                    comboBoxSubCategory.SelectedIndex = index;
                }
                else
                {
                    comboBoxSubCategory.Text = value;
                }
            }
        }


        public void RefreshSubCategories(List<string> subCategories)
        {
            // 暂时取消订阅事件，避免触发SettingsChanged
            comboBoxSubCategory.SelectedIndexChanged -= ComboBoxSubCategory_SelectedIndexChanged;

            comboBoxSubCategory.Items.Clear();
            foreach (var cat in subCategories)
            {
                comboBoxSubCategory.Items.Add(cat);
            }

            // 自动选择第一个子分类（在重新订阅事件之前，避免触发不必要的SettingsChanged）
            if (comboBoxSubCategory.Items.Count > 0)
            {
                comboBoxSubCategory.SelectedIndex = 0;
            }

            // 重新订阅事件
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
        }

        public void SetLoadingState(bool isLoading, string message = "加载中...")
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => SetLoadingState(isLoading, message)));
                return;
            }

            if (isLoading)
            {
                labelStatistics.Text = message;
                EnableButtons(false);
            }
            else
            {
                EnableButtons(true);
            }
        }

        public event EventHandler? MarkAsKnownClicked;
        public event EventHandler? MarkAsUnknownClicked;
        public event EventHandler? PronounceClicked;
        public event EventHandler? NextClicked;
        public event EventHandler? ExitClicked;
        public event EventHandler? AddToPdfQuestionClicked;
        public event EventHandler? SettingsChanged;
        public event EventHandler? OpenStatisticsClicked;
        public event EventHandler? ExportErrorBookClicked;
        public event EventHandler? ReviewClicked;

        public void ShowMessage(string msg)
        {
            ShowToast(msg, ToastType.Info);
        }

        public void ShowToast(string message, ToastType type = ToastType.Info, int duration = 3000)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => ShowToast(message, type, duration)));
                return;
            }

            var toast = new ToastNotification(message, type);
            toast.Show(this, duration);
        }

        public void EnableButtons(bool enabled)
        {
            buttonKnown.Enabled = enabled;
            buttonUnknown.Enabled = enabled;
            buttonPronounce.Enabled = enabled;
        }

        public async Task PlayPronunciationAsync(string text, string language)
        {
            try
            {
                await _ttsService.SpeakAsync(text, language);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to play pronunciation");
            }
        }


        #endregion

        #region WinForms Designer Generated Code

        private void InitializeComponent()
        {
            _listView = new LearningListView();
            _contentView = new LearningContentView();
            _buttonsView = new LearningButtonsView();
            _statsView = new LearningStatsView();
            _statsProgressView = new LearningProcessStatsView();
            _statsButtonView = new LearningStatsButtonView();
            _settingsView = new LearningSettingsView();
            mainTableLayoutPanel = new TableLayoutPanel();
            middlePanel = new Panel();
            middleTableLayoutPanel = new TableLayoutPanel();
            mainTableLayoutPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            middleTableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // _listView
            // 
            _listView.Location = new Point(0, 0);
            _listView.Name = "_listView";
            _listView.Size = new Size(260, 981);
            _listView.TabIndex = 0;
            // 
            // _contentView
            // 
            _contentView.Location = new Point(0, 0);
            _contentView.Name = "_contentView";
            _contentView.Size = new Size(954, 225);
            _contentView.TabIndex = 0;
            // 
            // _buttonsView
            // 
            _buttonsView.Location = new Point(3, 648);
            _buttonsView.Name = "_buttonsView";
            _buttonsView.Size = new Size(1089, 66);
            _buttonsView.TabIndex = 0;
            // 
            // _statsView
            // 
            _statsView.Dock = DockStyle.Fill;
            _statsView.Location = new Point(3, 797);
            _statsView.Name = "_statsView";
            _statsView.Size = new Size(1089, 38);
            _statsView.TabIndex = 9;
            // 
            // _statsProgressView
            // 
            _statsProgressView.Dock = DockStyle.Fill;
            _statsProgressView.Location = new Point(3, 753);
            _statsProgressView.Name = "_statsProgressView";
            _statsProgressView.Size = new Size(1089, 38);
            _statsProgressView.TabIndex = 1;
            // 
            // _statsButtonView
            // 
            _statsButtonView.BackColor = Color.White;
            _statsButtonView.Dock = DockStyle.Fill;
            _statsButtonView.Location = new Point(3, 3);
            _statsButtonView.Name = "_statsButtonView";
            _statsButtonView.Padding = new Padding(10, 4, 10, 4);
            _statsButtonView.Size = new Size(1089, 42);
            _statsButtonView.TabIndex = 0;
            // 
            // _settingsView
            // 
            _settingsView.Location = new Point(0, 0);
            _settingsView.Name = "_settingsView";
            _settingsView.Size = new Size(220, 837);
            _settingsView.TabIndex = 0;
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.ColumnCount = 3;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 266F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            mainTableLayoutPanel.Controls.Add(_listView.PanelList, 0, 0);
            mainTableLayoutPanel.Controls.Add(middlePanel, 1, 0);
            mainTableLayoutPanel.Controls.Add(_settingsView.PanelConfig, 2, 0);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 1;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Size = new Size(1587, 844);
            mainTableLayoutPanel.TabIndex = 0;
            // 
            // middlePanel
            // 
            middlePanel.Controls.Add(middleTableLayoutPanel);
            middlePanel.Dock = DockStyle.Fill;
            middlePanel.Location = new Point(269, 3);
            middlePanel.Name = "middlePanel";
            middlePanel.Size = new Size(1095, 838);
            middlePanel.TabIndex = 20;
            // 
            // middleTableLayoutPanel
            // 
            middleTableLayoutPanel.ColumnCount = 1;
            middleTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.Controls.Add(_statsButtonView, 0, 0);
            middleTableLayoutPanel.Controls.Add(_contentView.PanelContent, 0, 1);
            middleTableLayoutPanel.Controls.Add(_contentView.PanelNotes, 0, 2);
            middleTableLayoutPanel.Controls.Add(_buttonsView.ButtonsPanel, 0, 3);
            middleTableLayoutPanel.Controls.Add(_statsProgressView, 0, 4);
            middleTableLayoutPanel.Controls.Add(_statsView, 0, 5);
            middleTableLayoutPanel.Dock = DockStyle.Fill;
            middleTableLayoutPanel.Location = new Point(0, 0);
            middleTableLayoutPanel.Name = "middleTableLayoutPanel";
            middleTableLayoutPanel.RowCount = 6;
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 91F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            middleTableLayoutPanel.Size = new Size(1095, 838);
            middleTableLayoutPanel.TabIndex = 0;
            // 
            // LearningForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(1587, 844);
            Controls.Add(mainTableLayoutPanel);
            DoubleBuffered = true;
            MinimumSize = new Size(1024, 768);
            Name = "LearningForm";
            Text = "✨ 学习模式 ✨";
            TransparencyKey = Color.FromArgb(255, 0, 255);
            mainTableLayoutPanel.ResumeLayout(false);
            middlePanel.ResumeLayout(false);
            middleTableLayoutPanel.ResumeLayout(false);
            ResumeLayout(false);
        }
        #endregion

        #region Enhanced Features Initialization

        private void InitializeEnhancedFeatures()
        {
            _studyTimer.Interval = 1000;
            _studyTimer.Tick += StudyTimer_Tick;
            _studyTimer.Start();

            _gameTimer = new System.Windows.Forms.Timer();
            _gameTimer.Interval = 1000;
            _gameTimer.Tick += GameTimer_Tick;

            _noteSaveTimer.Interval = 1000;
            _noteSaveTimer.Tick += NoteSaveTimer_Tick;

            ApplyButtonStyles();

            InitializeButtonTooltips();

            InitializeNavigationButtons();

            if (flowLayoutPanelBadges != null)
            {
                _gamificationService.SetBadgeUI(flowLayoutPanelBadges, _toolTip);
            }

            if (labelStudyTime != null && labelScore != null && labelTodayCount != null &&
                labelStreak != null && labelLevel != null && labelXP != null && progressXP != null)
            {
                _gamificationService.SetStatsUI(labelStudyTime, labelScore, labelTodayCount, labelStreak, labelLevel, labelXP, progressXP);
            }

            if (flowLayoutPanelChallenges != null)
            {
                _gamificationService.SetChallengeUI(flowLayoutPanelChallenges, _soundService);
            }

            _confettiManager.SetTargetControl(mainTableLayoutPanel);

            _gamificationService.Load("default");
            UpdateEncouragement();
            _gamificationService.UpdateAllDisplays();

            UpdateLevel(
                _gamificationService.CurrentLevel,
                _gamificationService.XP,
                _gamificationService.XPToNextLevel,
                _gamificationService.LevelTitle);
        }



        /// <summary>
        /// 更新等级信息
        /// </summary>
        public void UpdateLevel(int level, int currentXP, int xpToNextLevel, string levelTitle)
        {
            if (_levelBadge == null) return;
            _levelBadge.Level = level;
            _levelBadge.LevelTitle = levelTitle;
            _levelBadge.SetXP(currentXP, xpToNextLevel);
        }

        /// <summary>
        /// 触发升级动画
        /// </summary>
        public void TriggerLevelUp(int newLevel, string newTitle)
        {
            if (_levelBadge == null) return;
            _levelBadge.TriggerLevelUp(newLevel, newTitle);
        }
        private void ApplyButtonStyles()
        {
            ApplyRoundedStyle(buttonKnown, 8);
            ApplyRoundedStyle(buttonUnknown, 8);
            ApplyRoundedStyle(buttonPronounce, 8);
            ApplyRoundedStyle(buttonFavorite, 8);
            ApplyRoundedStyle(buttonNote, 8);
            //ApplyRoundedStyle(buttonExit, 8);
            ApplyRoundedStyle(buttonShowAnswer, 8);
            ApplyRoundedStyle(buttonThemeToggle, 8);

            foreach (Control ctrl in _buttonsView.ButtonsPanel.Controls)
            {
                if (ctrl is Button btn)
                {
                    ApplyRoundedStyle(btn, 8);
                }
            }
        }

        private void InitializeButtonTooltips()
        {
            _toolTip.InitialDelay = 300;
            _toolTip.AutoPopDelay = 6000;
            _toolTip.ShowAlways = true;

            _toolTip.SetToolTip(_buttonsView.ButtonPrevious, "上一项 (← / PageUp / Home)");
            _toolTip.SetToolTip(_buttonsView.ButtonNext, "下一项 (Enter / → / PageDown / End)");
            _toolTip.SetToolTip(_buttonsView.ButtonKnown, "标记为已知 (1 / K)");
            _toolTip.SetToolTip(_buttonsView.ButtonUnknown, "标记为未知 (2 / U)");
            _toolTip.SetToolTip(_buttonsView.ButtonPronounce, "播放发音 (Space)");
            _toolTip.SetToolTip(_buttonsView.ButtonFavorite, "收藏/取消收藏 (3 / F)");
            _toolTip.SetToolTip(_buttonsView.ButtonNote, "打开笔记 (4 / N)");
            _toolTip.SetToolTip(_buttonsView.ButtonEdit, "编辑内容 (5 / E)");
            _toolTip.SetToolTip(_buttonsView.ButtonExit, "退出学习 (Esc)");
            _toolTip.SetToolTip(_buttonsView.ButtonAIAsk, "AI 问答 (F6)");
            _toolTip.SetToolTip(_buttonsView.ButtonFeynman, "费曼学习法 (F7)");

            _toolTip.SetToolTip(buttonShowAnswer, "切换学习/答题模式 (F3)");

            _toolTip.SetToolTip(labelEncouragement,
                "鼓励语，每隔几句学习内容自动更新一次");
            _toolTip.SetToolTip(labelDailyGoal,
                $"每日目标: {DailyGoal}项 | 当前进度: {_gamificationService?.TodayLearnedCount ?? 0}项\n" +
                "快捷键: F8 开启/关闭自动播放");
        }

        private void ApplyRoundedStyle(Button button, int radius = 8)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            button.FlatAppearance.MouseOverBackColor = Color.Transparent;
            button.FlatAppearance.MouseDownBackColor = Color.Transparent;
            button.FlatAppearance.CheckedBackColor = Color.Transparent;

            button.Paint += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    using var path = new System.Drawing.Drawing2D.GraphicsPath();
                    path.AddArc(0, 0, radius, radius, 180, 90);
                    path.AddArc(btn.Width - radius, 0, radius, radius, 270, 90);
                    path.AddArc(btn.Width - radius, btn.Height - radius, radius, radius, 0, 90);
                    path.AddArc(0, btn.Height - radius, radius, radius, 90, 90);
                    path.CloseAllFigures();
                    btn.Region = new Region(path);
                }
            };

            button.MouseEnter += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    // 悬停时边框变为主题色
                    btn.FlatAppearance.BorderColor = Color.FromArgb(76, 175, 80);
                    btn.FlatAppearance.BorderSize = 2;

                    // 根据按钮背景色亮度调整悬停效果
                    float brightness = (btn.BackColor.R * 0.299f + btn.BackColor.G * 0.587f + btn.BackColor.B * 0.114f) / 255f;
                    if (brightness > 0.6f)
                    {
                        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Dark(btn.BackColor, 15);
                    }
                    else
                    {
                        btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(btn.BackColor, 15);
                    }
                }
            };

            button.MouseLeave += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
                    btn.FlatAppearance.BorderSize = 1;
                    btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                }
            };

            button.MouseDown += (sender, e) =>
            {
                if (sender is Button btn && e.Button == MouseButtons.Left)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(56, 142, 60);
                    btn.FlatAppearance.BorderSize = 2;
                    btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(btn.BackColor, 20);
                }
            };

            button.MouseUp += (sender, e) =>
            {
                if (sender is Button btn)
                {
                    btn.FlatAppearance.BorderColor = Color.FromArgb(76, 175, 80);
                    btn.FlatAppearance.BorderSize = 2;
                }
            };
        }

        private void InitializeNavigationButtons()
        {
            _buttonsView.PreviousClicked += ButtonPrevious_Click;
            _buttonsView.NextClicked += ButtonNextNav_Click;
            _buttonsView.EditClicked += ButtonEdit_Click;
            InitializeDailyGoalProgress();
        }

        private void InitializeDailyGoalProgress()
        {
            _dailyGoalProgress = new CircularProgressControl
            {
                Size = new Size(100, 100),
                MaxValue = DailyGoal,
                CurrentValue = _gamificationService.TodayLearnedCount,
                ProgressColor = Color.FromArgb(255, 152, 0),
                TrackColor = Color.FromArgb(220, 220, 220),
                TextColor = Color.Black
            };


            _dailyGoalProgress.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            _dailyGoalProgress.Location = new Point(20, 20);

            InitializeShortcutHint();
        }


        private void InitializeShortcutHint()
        {


            InitializeNoteEnhancements();
            InitializeLearningCard();
        }

        private void InitializeLearningCard()
        {
            _learningCard = new LearningCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                Height = 220
            };
            _learningCard.Click += ContentArea_Click;

            if (panelContent != null)
            {
                panelContent.Controls.Add(_learningCard);
                panelContent.Controls.SetChildIndex(_learningCard, 0);
            }

            InitializeAIHistoryPanel();
            InitializeMentorPanel();
        }

        private void InitializeMentorPanel()
        {
            if (_conversationContextService == null) return;

            _mentorPanel = new MentorAIPanel
            {
                Dock = DockStyle.Right,
                Width = 350,
                Visible = false,
                ContextService = _conversationContextService
            };

            Controls.Add(_mentorPanel);
        }

        public void ToggleMentorPanel()
        {
            if (_mentorPanel == null)
            {
                InitializeMentorPanel();
            }

            if (_mentorPanel != null)
            {
                _mentorPanel.Visible = !_mentorPanel.Visible;

                if (_mentorPanel.Visible && _currentItem != null)
                {
                    _mentorPanel.SetLearningContext(_currentItem.Content);
                }
            }
        }

        private void InitializeAIHistoryPanel()
        {
            _aiHistoryPanel = new AIHistoryPanel
            {
                Dock = DockStyle.Right,
                Width = 300,
                Visible = false
            };
            _aiHistoryPanel.HistoryItemSelected += AIHistoryPanel_HistoryItemSelected;

            Controls.Add(_aiHistoryPanel);
        }

        private void AIHistoryPanel_HistoryItemSelected(object? sender, AIHistoryEventArgs e)
        {
            ShowToast($"问题: {e.Item.Question}", ToastType.Info);
        }

        private void InitializeNoteEnhancements()
        {
            _noteWordCountLabel = new Label
            {
                Text = "字数: 0",
                Font = new Font("微软雅黑", 8F),
                ForeColor = Color.Gray,
                Dock = DockStyle.Bottom,
                Padding = new Padding(5),
                TextAlign = ContentAlignment.MiddleRight
            };

            _noteFormattingToolbar = new ToolStrip
            {
                Dock = DockStyle.Top,
                Height = 25,
                BackColor = Color.White
            };

            var boldBtn = new ToolStripButton("B")
            {
                ToolTipText = "加粗",
                Font = new Font("微软雅黑", 9F, FontStyle.Bold)
            };
            boldBtn.Click += BoldBtn_Click;

            var italicBtn = new ToolStripButton("I")
            {
                ToolTipText = "斜体",
                Font = new Font("微软雅黑", 9F, FontStyle.Italic)
            };
            italicBtn.Click += ItalicBtn_Click;

            var underlineBtn = new ToolStripButton("U")
            {
                ToolTipText = "下划线",
                Font = new Font("微软雅黑", 9F, FontStyle.Underline)
            };
            underlineBtn.Click += UnderlineBtn_Click;

            _noteFormattingToolbar.Items.Add(boldBtn);
            _noteFormattingToolbar.Items.Add(italicBtn);
            _noteFormattingToolbar.Items.Add(underlineBtn);
            _noteFormattingToolbar.Items.Add(new ToolStripSeparator());

            var fontColorBtn = new ToolStripButton("A")
            {
                ToolTipText = "字体颜色"
            };
            fontColorBtn.Click += FontColorBtn_Click;
            _noteFormattingToolbar.Items.Add(fontColorBtn);

            if (panelNotes != null)
            {
                panelNotes.Controls.Add(_noteWordCountLabel);
                panelNotes.Controls.Add(_noteFormattingToolbar);
            }

            if (richTextBoxNotes != null)
            {
                richTextBoxNotes.TextChanged += RichTextBoxNotes_TextChangedEnhanced;
            }
        }

        private void BoldBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Bold);
        }

        private void ItalicBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Italic);
        }

        private void UnderlineBtn_Click(object? sender, EventArgs e)
        {
            ApplyNoteFormat(FontStyle.Underline);
        }

        private void FontColorBtn_Click(object? sender, EventArgs e)
        {
            ChangeNoteFontColor();
        }

        private void ApplyNoteFormat(FontStyle style)
        {
            if (richTextBoxNotes == null) return;

            var currentFont = richTextBoxNotes.SelectionFont;
            if (currentFont != null)
            {
                var newFont = new Font(currentFont, currentFont.Style ^ style);
                richTextBoxNotes.SelectionFont = newFont;
            }
        }

        private void ChangeNoteFontColor()
        {
            if (richTextBoxNotes == null) return;

            using (var colorDialog = new ColorDialog())
            {
                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    richTextBoxNotes.SelectionColor = colorDialog.Color;
                }
            }
        }

        private void RichTextBoxNotes_TextChangedEnhanced(object? sender, EventArgs e)
        {
            if (_noteWordCountLabel != null && richTextBoxNotes != null)
            {
                int wordCount = richTextBoxNotes.Text.Length;
                _noteWordCountLabel.Text = $"字数: {wordCount}";
            }
        }

        private void UpdateDailyGoalProgress()
        {
            if (_dailyGoalProgress != null && !_dailyGoalProgress.IsDisposed)
            {
                _dailyGoalProgress.CurrentValue = _gamificationService.TodayLearnedCount;
            }
        }

        private void ButtonPrevious_Click(object? sender, EventArgs e)
        {
            _soundService?.PlayNavigation();
            ItemSelectedFromList?.Invoke(this, new ItemSelectedEventArgs(listBoxItems.SelectedIndex - 1));
        }

        private void ButtonNextNav_Click(object? sender, EventArgs e)
        {
            _soundService?.PlayNavigation();
            NextClicked?.Invoke(this, EventArgs.Empty);
        }

        private void OnXPChanged(object? sender, XPChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnXPChanged(sender, e)));
                return;
            }

            UpdateLevel(
                _gamificationService.CurrentLevel,
                _gamificationService.XP,
                _gamificationService.XPToNextLevel,
                _gamificationService.LevelTitle);

            if (e.Added > 0)
            {
                ShowXPFloatingText(e.Added);
            }
        }

        private void OnLevelUp(object? sender, LevelUpEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnLevelUp(sender, e)));
                return;
            }

            _soundService?.PlaySuccess();
            StartConfetti();
            TriggerLevelUp(e.NewLevel, e.LevelTitle);
            MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{e.LevelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StudyTimer_Tick(object? sender, EventArgs e)
        {
            _studyDuration = _studyDuration.Add(TimeSpan.FromSeconds(1));
            _gamificationService.UpdateStudyDuration(_studyDuration);
        }

        private void PomodoroService_PomodoroCompleted(object? sender, int completedCount)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PomodoroService_PomodoroCompleted(sender, completedCount)));
                return;
            }

            _logger.LogInformation("番茄钟完成: 累计 {Count} 个", completedCount);
            ShowMessage($"🍅 恭喜完成第 {completedCount} 个番茄钟！", "番茄钟完成");
        }

        private void PomodoroService_StateChanged(object? sender, Services.Learning.PomodoroStateChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => PomodoroService_StateChanged(sender, e)));
                return;
            }

            _logger.LogInformation("番茄钟状态变更: {OldState} -> {NewState}", e.OldState, e.NewState);

            switch (e.NewState)
            {
                case Services.Learning.PomodoroState.Studying:
                    break;
                case Services.Learning.PomodoroState.ShortBreak:
                    ShowMessage("⏸️ 短休息时间到了！休息一下吧~", "休息提醒");
                    break;
                case Services.Learning.PomodoroState.LongBreak:
                    ShowMessage("🛌 长休息时间到了！好好放松一下~", "休息提醒");
                    break;
                case Services.Learning.PomodoroState.Paused:
                    break;
                case Services.Learning.PomodoroState.Idle:
                    break;
            }
        }

        #endregion

        #region List Management

        public void UpdateLearningList(List<string> items, int currentIndex)
        {
            if (listBoxItems == null) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLearningList(items, currentIndex)));
                return;
            }

            _listView.SetItems(items);
            LoadFavoritesToListView();

            if (currentIndex >= 0 && currentIndex < items.Count)
            {
                var currentItem = items[currentIndex];
                int filteredIndex = listBoxItems.Items.IndexOf(currentItem);
                if (filteredIndex >= 0)
                {
                    listBoxItems.SelectedIndex = filteredIndex;
                    listBoxItems.TopIndex = Math.Max(0, filteredIndex - 5);
                }
            }
        }

        private void LoadFavoritesToListView()
        {
            try
            {
                if (_cachedFavorites != null && DateTime.Now - _favoritesCacheTime < FavoritesCacheDuration)
                {
                    _listView.SetFavoriteItems(_cachedFavorites);
                    return;
                }

                string favoritesPath = GetUserFavoritesPath();
                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    var favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    _cachedFavorites = new HashSet<string>(favorites);
                    _favoritesCacheTime = DateTime.Now;
                    _listView.SetFavoriteItems(_cachedFavorites);
                }
            }
            catch
            {
            }
        }

        private void InvalidateFavoritesCache()
        {
            _cachedFavorites = null;
            _favoritesCacheTime = DateTime.MinValue;
        }

        private void UpdateListStatus(int totalItems, int currentIndex)
        {
            if (labelListStatus == null) return;

            if (totalItems == 0)
            {
                labelListStatus.Text = "暂无学习内容";
                return;
            }

            int progressPercent = totalItems > 0 ? (int)((currentIndex + 1) * 100.0 / totalItems) : 0;
            labelListStatus.Text = $"共 {totalItems} 项 | 当前 {currentIndex + 1} | 进度 {progressPercent}%";
        }

        public void UpdateLearningListSelection(int currentIndex)
        {
            if (listBoxItems == null) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLearningListSelection(currentIndex)));
                return;
            }

            _listView.SetSelectedIndexFromFullList(currentIndex);
        }

        public void EnableListHighlighting(bool enable)
        {
            if (listBoxItems == null) return;

            listBoxItems.ItemHeight = 30;
            listBoxItems.DrawMode = enable ? DrawMode.OwnerDrawFixed : DrawMode.Normal;
            if (enable)
            {
                listBoxItems.DrawItem += ListBoxItems_DrawItem;
                listBoxItems.MouseMove += ListBoxItems_MouseMove;
                listBoxItems.MouseLeave += ListBoxItems_MouseLeave;
            }
            else
            {
                listBoxItems.DrawItem -= ListBoxItems_DrawItem;
                listBoxItems.MouseMove -= ListBoxItems_MouseMove;
                listBoxItems.MouseLeave -= ListBoxItems_MouseLeave;
            }
        }

        private void ListBoxItems_MouseMove(object? sender, MouseEventArgs e)
        {
            if (listBoxItems == null) return;

            int index = listBoxItems.IndexFromPoint(e.Location);
            if (index != _hoverIndex)
            {
                int oldIndex = _hoverIndex;
                _hoverIndex = index;

                if (oldIndex >= 0 && oldIndex < listBoxItems.Items.Count)
                {
                    listBoxItems.Invalidate(listBoxItems.GetItemRectangle(oldIndex));
                }
                if (index >= 0 && index < listBoxItems.Items.Count)
                {
                    listBoxItems.Invalidate(listBoxItems.GetItemRectangle(index));
                }
            }
        }

        private void ListBoxItems_MouseLeave(object? sender, EventArgs e)
        {
            if (_hoverIndex >= 0 && listBoxItems != null)
            {
                int oldIndex = _hoverIndex;
                _hoverIndex = -1;
                if (oldIndex < listBoxItems.Items.Count)
                {
                    listBoxItems.Invalidate(listBoxItems.GetItemRectangle(oldIndex));
                }
            }
        }

        private void ListBoxItems_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (e.Index < 0) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            bool isHovered = e.Index == _hoverIndex && !isSelected;

            if (isSelected)
            {
                e.Graphics.FillRectangle(_selectedBackgroundBrush, e.Bounds);
            }
            else if (isHovered)
            {
                e.Graphics.FillRectangle(_hoverBackgroundBrush, e.Bounds);
            }

            string text = listBox.Items[e.Index].ToString() ?? string.Empty;

            bool isFavorite = _listView.IsFavoriteItem(text);
            bool isKnown = IsItemKnown(text);

            int iconSize = 20;
            int iconMargin = 8;
            int textStartX = iconMargin + iconSize + 5;

            using var iconFont = new Font("Arial", 10F);
            using var favoriteBrush = new SolidBrush(Color.FromArgb(255, 152, 0));
            using var knownBrush = new SolidBrush(Color.FromArgb(76, 175, 80));

            if (isFavorite)
            {
                e.Graphics.DrawString("⭐", iconFont,
                    isSelected ? _selectedForegroundBrush : favoriteBrush,
                    e.Bounds.X + iconMargin, e.Bounds.Y + (e.Bounds.Height - iconSize) / 2);
                textStartX += iconSize;
            }

            if (isKnown)
            {
                e.Graphics.DrawString("✓", iconFont,
                    isSelected ? _selectedForegroundBrush : knownBrush,
                    e.Bounds.X + textStartX - iconSize - 5, e.Bounds.Y + (e.Bounds.Height - iconSize) / 2);
                textStartX += iconSize;
            }

            using var format = new StringFormat
            {
                LineAlignment = StringAlignment.Center
            };

            e.Graphics.DrawString(text, e.Font, isSelected ? _selectedForegroundBrush : _normalForegroundBrush,
                new Rectangle(e.Bounds.X + textStartX, e.Bounds.Y, e.Bounds.Width - textStartX - iconMargin, e.Bounds.Height),
                format);

            if (isSelected)
            {
                e.Graphics.DrawRectangle(_selectedBorderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
            }

            e.DrawFocusRectangle();
        }

        private bool IsItemKnown(string itemText)
        {
            try
            {
                string errorBookPath = Path.Combine(AppPaths.UsersDir, GetCurrentUserId(), "error_book.json");
                if (!File.Exists(errorBookPath)) return true;

                string json = File.ReadAllText(errorBookPath);
                var errorItems = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                return !errorItems.Contains(itemText);
            }
            catch
            {
                return true;
            }
        }

        private void ListBoxItems_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int fullIndex = _listView.SelectedIndexInAllItems;
            if (fullIndex >= 0)
            {
                ItemSelectedFromList?.Invoke(this, new ItemSelectedEventArgs(fullIndex));
            }
        }

        public event EventHandler<ItemSelectedEventArgs>? ItemSelectedFromList;

        #endregion

        #region 高级学习功能

        public void StartProgressiveHint()
        {
            if (_currentItem == null || _thinkingStimulator == null) return;
            var content = _currentItem.GetMainContent();
            var answer = _currentItem.GetDisplayText();
            _thinkingStimulator.StartProgressiveHint(content, answer);
        }

        public void StartAssociationLearning()
        {
            if (_currentItem == null || _thinkingStimulator == null) return;
            var content = _currentItem.GetMainContent();
            _thinkingStimulator.StartAssociationLearning(content);
        }

        public void ShowFeynmanQuestions()
        {
            if (_currentItem == null || _thinkingStimulator == null) return;

            if (!_isFeynmanPanelVisible)
            {
                ShowFeynmanPanel();
            }

            _feynmanPanel?.GoToStep(FeynmanStep.Review);
        }

        public void ShowDailyThinkingTask()
        {
            _thinkingStimulator?.ShowDailyThinkingTask();
        }

        #endregion

        #region Event Handlers



        /// <summary>
        /// 处理内容区点击事件（答题模式下切换答案显示）
        /// </summary>
        private void ContentArea_Click(object? sender, EventArgs e)
        {
            _ = PlayCardClickAnimationAsync();

            if (_isShowAnswer)
            {
                // 答题模式：切换答案显示状态
                UpdateDetailState(true, !_answerRevealed);
            }
            else
            {
                // 学习模式：直接显示完整内容
                UpdateDetailState(true, true);
            }
        }

        private async Task PlayCardClickAnimationAsync()
        {
            if (_learningCard == null || _disposed) return;

            var originalMargin = _learningCard.Margin;
            var shrinkMargin = new Padding(
                originalMargin.Left + 5,
                originalMargin.Top + 3,
                originalMargin.Right + 5,
                originalMargin.Bottom + 3);

            try
            {
                _learningCard.Margin = shrinkMargin;
                await Task.Delay(60);
                if (_disposed || _learningCard == null) return;
                _learningCard.Margin = originalMargin;
            }
            catch
            {
            }
        }

        // 为了兼容性保留原来的事件处理方法名
        private void LabelContent_Click(object? sender, EventArgs e) => ContentArea_Click(sender, e);
        private void ListBoxDisplay_Click(object? sender, EventArgs e) => ContentArea_Click(sender, e);

        /// <summary>
        /// 统一更新详情区状态
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <param name="showAnswer">是否显示答案（答题模式下）</param>
        private void UpdateDetailState(bool visible, bool showAnswer = true)
        {
            if (_learningCard == null) return;

            _learningCard.Visible = visible;

            if (visible && _currentItem != null)
            {
                if (_isShowAnswer)
                {
                    // 答题模式：根据是否已揭示答案显示不同内容
                    _answerRevealed = showAnswer;
                    if (_answerRevealed)
                    {
                        _learningCard.Content = _currentItem.GetDisplayText();
                    }
                    else
                    {
                        _learningCard.Content = _currentItem.GetDisplayStruct();
                    }
                }
                else
                {
                    // 学习模式：显示完整内容
                    _answerRevealed = true;
                    _learningCard.Content = _currentItem.GetDisplayText();
                }
            }
        }

        private void OnBadgesUnlocked(object? sender, BadgesUnlockedEventArgs e)
        {
            _gamificationService.AddScore(50 * e.BadgeIds.Count);
            _gamificationService.AddXP(50 * e.BadgeIds.Count);
            _gamificationService.UpdateAllDisplays();
        }

        private void UpdateChallengesProgress()
        {
            _gamificationService.UpdateChallengeProgress("learn", _gamificationService.TodayLearnedCount);
        }



        /// <summary>
        /// 启动猜词小游戏
        /// </summary>
        private void StartMiniGame()
        {
            _isGameActive = true;
            _gameScore = 0;
            panelGame.Visible = true;
            _gameTimer.Start();
            NextGameQuestion();
        }

        /// <summary>
        /// 生成下一道游戏题目
        /// </summary>
        private void NextGameQuestion()
        {
            if (_currentItem == null) return;

            labelGameQuestion.Text = $"❓ {_currentItem.GetMainContent()} 的意思是？";
            textBoxGameAnswer.Text = "";
            labelGameResult.Text = "";
        }

        /// <summary>
        /// 处理游戏提交答案
        /// </summary>
        private void ButtonGameSubmit_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null) return;

            string userAnswer = textBoxGameAnswer.Text.Trim().ToLower();
            string correctAnswer = _currentItem.GetDisplayText().ToLower();

            if (correctAnswer.Contains(userAnswer) || userAnswer.Contains(correctAnswer))
            {
                _gameScore += 10;
                _gamificationService.AddScore(10);
                _gamificationService.AddXP(10);
                labelGameResult.Text = $"✅ 正确！得分: {_gameScore}";
                _soundService?.PlaySuccess();
            }
            else
            {
                labelGameResult.Text = $"❌ 错误！正确答案: {_currentItem.GetDisplayText()}";
                _soundService?.PlayError();
            }

            NextGameQuestion();
        }

        /// <summary>
        /// 游戏计时器回调（预留）
        /// </summary>
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// 更新鼓励语显示（按间隔显示，避免太频繁）
        /// </summary>
        private void UpdateEncouragement()
        {
            _encouragementCounter++;

            // 只在达到间隔时更新鼓励语
            if (_encouragementCounter >= EncouragementInterval && labelEncouragement != null)
            {
                labelEncouragement.Text = _encouragementManager.GetRandomEncouragement();
                _encouragementCounter = 0;
            }
        }

        #endregion


        private void RadioSetting_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked && !_settingsChangedEventsSuspended)
            {
                // 练习/复习切换：只控制左侧列表显示，触发 SettingsChanged 刷新列表
                if (radio == radioStudyMode || radio == radioQuickMode)
                {
                    _isShowAnswer = radioQuickMode.Checked;
                    // 触发列表刷新，不更新显示内容
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    // 排序、语言等设置变更，触发列表刷新
                    SettingsChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        private void ComboBoxSubject_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ComboBoxSubCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (!_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CheckBoxAIExplanation_CheckedChanged(object? sender, EventArgs e)
        {
            if (!_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private async void ButtonKnown_Click(object? sender, EventArgs e)
        {
            try
            {
                EnableButtons(false);

                _celebrationCounter++;

                if (_celebrationCounter >= CelebrationInterval)
                {
                    _soundService?.PlaySuccess();
                    StartConfetti();
                    _celebrationCounter = 0;
                }

                _totalLearnedCount++;
                if (_isShowAnswer && !_answerRevealed)
                {
                    _quizCorrectCount++;
                }

                UpdateEncouragement();
                UpdateChallengesProgress();
                UpdateDailyGoalProgress();

                if (_eventBus != null && _currentItem != null)
                {
                    _eventBus.Publish(new ItemLearnedEvent
                    {
                        UserId = GetCurrentUserId(),
                        ItemId = _currentItem.GetMainContent(),
                        ItemContent = _currentItem.GetDisplayText(),
                        SubCategory = _settings.SubCategory,
                        LearnedAt = DateTime.Now
                    });
                }

                _ = _encouragementService.PlayRandomKnownFeedbackAsync();

                await Task.Delay(500);

                EnableButtons(true);

                MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ButtonKnown_Click failed");
                EnableButtons(true);
            }
        }

        private async void ButtonUnknown_Click(object? sender, EventArgs e)
        {
            try
            {
                EnableButtons(false);

                if (_currentItem != null)
                {
                    UpdateDetailState(true, true);

                    if (_eventBus != null)
                    {
                        _eventBus.Publish(new ItemWrongEvent
                        {
                            UserId = GetCurrentUserId(),
                            ItemId = _currentItem.GetMainContent(),
                            ItemContent = _currentItem.GetDisplayText(),
                            CorrectAnswer = _currentItem.GetDisplayText(),
                            UserAnswer = string.Empty,
                            SubCategory = _settings.SubCategory,
                            WrongAt = DateTime.Now
                        });
                    }
                }

                _soundService?.PlayError();

                _ = _encouragementService.PlayRandomUnknownFeedbackAsync();

                await Task.Delay(2000);

                EnableButtons(true);

                MarkAsUnknownClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "ButtonUnknown_Click failed");
                EnableButtons(true);
            }
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            _soundService?.PlayNavigation();
            NextClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonPronounce_Click(object? sender, EventArgs e)
        {
            PronounceClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ShowXPFloatingText(int xpGained)
        {
            if (_floatingText == null)
            {
                _floatingText = new FloatingText();
                Controls.Add(_floatingText);
            }

            var badgeRect = _levelBadge.RectangleToScreen(
                new Rectangle(0, 0, _levelBadge.Width, _levelBadge.Height));
            var formPoint = PointToClient(badgeRect.Location);

            _floatingText.Text = $"+{xpGained} XP";
            _floatingText.TextColor = Color.FromArgb(255, 152, 0);
            _floatingText.ShowAt(this,
                formPoint.X + _levelBadge.Width / 2 - 40,
                formPoint.Y,
                $"+{xpGained} XP");
        }

        private void StartConfetti()
        {
            if (_isConfettiActive) return;
            _isConfettiActive = true;
            _confettiManager.Start(Width / 2f, 0, 150);
            _confettiTimer.Start();
        }

        private void ConfettiTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isConfettiActive) return;
            _confettiManager.Update();
            if (!_confettiManager.HasActiveParticles)
            {
                _isConfettiActive = false;
                _confettiTimer.Stop();
            }
            Invalidate();
        }

        private async void ShakeWindow()
        {
            var originalLocation = Location;
            var shakeAmount = 15;
            var shakeSteps = 8;
            var stepDelay = 15;

            for (int i = 0; i < shakeSteps; i++)
            {
                int currentShakeAmount = (int)(shakeAmount * (1 - (i / (float)shakeSteps)));

                int dx = (Random.Shared.Next(3) - 1) * currentShakeAmount;
                int dy = (Random.Shared.Next(3) - 1) * currentShakeAmount;

                Location = new Point(originalLocation.X + dx, originalLocation.Y + dy);
                await Task.Delay(stepDelay);
            }

            Location = originalLocation;
        }

        /// <summary>
        /// AI问答按钮点击事件
        /// </summary>
        private void ButtonAIAsk_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null)
            {
                ShowToast("请先选择一个学习内容", ToastType.Warning);
                return;
            }

            ToggleMentorPanel();
        }

        /// <summary>
        /// 费曼学习按钮点击事件
        /// </summary>
        private void ButtonFeynman_Click(object? sender, EventArgs e)
        {
            if (_currentItem == null)
            {
                MessageBox.Show("请先选择一个学习内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                ToggleFeynmanPanel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开费曼学习面板失败");
                MessageBox.Show($"打开费曼学习面板失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// 切换费曼学习面板显示状态
        /// </summary>
        private void ToggleFeynmanPanel()
        {
            if (_isFeynmanPanelVisible)
            {
                HideFeynmanPanel();
            }
            else
            {
                ShowFeynmanPanel();
            }
        }

        /// <summary>
        /// 显示费曼学习面板
        /// </summary>
        private void ShowFeynmanPanel()
        {
            if (_currentItem == null || _thinkingStimulator == null)
                return;

            if (_feynmanContainerPanel == null)
            {
                CreateFeynmanPanel();
            }

            if (_feynmanPanel != null && _currentItem != null)
            {
                var content = _currentItem.GetMainContent();
                var displayText = _currentItem.GetDisplayText();
                var questions = _thinkingStimulator.CreateFeynmanQuestions(content);

                _feynmanPanel.Content = content;
                _feynmanPanel.DisplayText = displayText;
                _feynmanPanel.SetQuestions(questions);
                _feynmanPanel.GoToStep(FeynmanStep.Study);

                // 加载历史记录
                var historyRecords = _feynmanHistoryService.GetRecordsByContent(displayText, 5);
                var entries = historyRecords.Select(r => new FeynmanLearningPanel.HistoryEntry
                {
                    Id = r.Id,
                    TeachAnswer = r.TeachAnswer,
                    Date = r.CompletedAt,
                    IsCompleted = r.IsCompleted
                }).ToList();
                _feynmanPanel.LoadHistoryRecords(entries);

                _themeService?.RegisterThemeable(_feynmanPanel);
            }

            if (_feynmanContainerPanel != null)
            {
                _feynmanContainerPanel.Visible = true;
                _feynmanContainerPanel.BringToFront();
                _isFeynmanPanelVisible = true;
            }
        }

        private void CloseButton_Click(object? sender, EventArgs e)
        {
            HideFeynmanPanel();
        }

        /// <summary>
        /// 隐藏费曼学习面板
        /// </summary>
        private void HideFeynmanPanel()
        {
            if (_feynmanContainerPanel != null)
            {
                _feynmanContainerPanel.Visible = false;
                _isFeynmanPanelVisible = false;
            }
        }

        private void CreateFeynmanPanel()
        {
            _feynmanPanel = new FeynmanLearningPanel
            {
                Dock = DockStyle.Fill
            };
            _feynmanPanel.CloseClicked += FeynmanPanel_CloseClicked;
            _feynmanPanel.Completed += FeynmanPanel_Completed;
            _feynmanPanel.AIFeedbackRequested += FeynmanPanel_AIFeedbackRequested;
            _feynmanPanel.GenerateSimplifiedRequested += FeynmanPanel_GenerateSimplifiedRequested;
            _feynmanPanel.GenerateAnalogyRequested += FeynmanPanel_GenerateAnalogyRequested;
            _feynmanPanel.VoiceInputRequested += FeynmanPanel_VoiceInputRequested;

            _feynmanContainerPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 420,
                Name = "FeynmanPanelContainer",
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Padding = new Padding(0, 0, 0, 0)
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                BackColor = Color.FromArgb(147, 112, 219)
            };

            var titleLabel = new Label
            {
                Text = "🧠 费曼学习法",
                Dock = DockStyle.Left,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Padding = new Padding(20, 10, 0, 0),
                AutoSize = true
            };

            var closeButton = new Button
            {
                Text = "✕",
                Dock = DockStyle.Right,
                Width = 45,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(127, 92, 199);
            closeButton.Click += CloseButton_Click;

            var gradientPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.White
            };
            gradientPanel.Paint += (sender, e) =>
            {
                using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Rectangle(0, 0, gradientPanel.Width, gradientPanel.Height),
                    Color.FromArgb(147, 112, 219),
                    Color.FromArgb(76, 175, 80),
                    System.Drawing.Drawing2D.LinearGradientMode.Horizontal))
                {
                    e.Graphics.FillRectangle(brush, e.ClipRectangle);
                }
            };

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(closeButton);

            _feynmanContainerPanel.Controls.Add(_feynmanPanel);
            _feynmanContainerPanel.Controls.Add(gradientPanel);
            _feynmanContainerPanel.Controls.Add(headerPanel);

            Controls.Add(_feynmanContainerPanel);
        }

        private void FeynmanPanel_CloseClicked(object? sender, EventArgs e)
        {
            HideFeynmanPanel();
        }

        private void FeynmanPanel_Completed(object? sender, EventArgs e)
        {
            _soundService?.PlaySuccess();

            if (_currentItem != null)
            {
                var record = new Models.Learning.FeynmanHistoryRecord
                {
                    ContentId = _currentItem.GetDisplayText(),
                    ContentTitle = _currentItem.GetDisplayText(),
                    TeachAnswer = _feynmanPanel?.TeachAnswer ?? string.Empty,
                    AIFeedback = _feynmanPanel?.AIFeedbackText,
                    SimplifiedText = _feynmanPanel?.SimplifiedText,
                    AnalogyText = _feynmanPanel?.AnalogyText,
                    IsCompleted = true
                };
                _feynmanHistoryService.SaveRecord(record);

                if (_eventBus != null)
                {
                    _eventBus.Publish(eventData: new FeynmanCompletedEvent
                    {
                        UserId = GetCurrentUserId(),
                        ItemContent = _currentItem.GetDisplayText(),
                        SubCategory = _settings.SubCategory,
                        SimplifiedText = record.SimplifiedText ?? string.Empty
                    });
                }
            }

            _gamificationService.Save();
            MessageBox.Show("🎉 恭喜完成费曼学习法四步流程！\n\n获得 50 XP 和 100 分！\n你的理解会更加深刻！", "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
            HideFeynmanPanel();
        }

        private async void FeynmanPanel_AIFeedbackRequested(object? sender, string userExplanation)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetAIFeedbackLoading(true);

                var content = _currentItem.GetMainContent();
                var displayText = _currentItem.GetDisplayText();

                var prompt = $"请评估以下用户对知识点的解释是否准确，并给出改进建议。\n\n" +
                             $"知识点：{displayText}\n" +
                             $"参考内容：{content}\n\n" +
                             $"用户的解释：{userExplanation}\n\n" +
                             $"请从以下几个方面评估：\n" +
                             $"1. 准确性：解释是否正确\n" +
                             $"2. 清晰度：是否容易理解\n" +
                             $"3. 完整性：是否涵盖了关键点\n" +
                             $"4. 改进建议：如何更好地解释";

                var feedback = await _aiQuestionService.AskAsync(prompt, content);
                _feynmanPanel.SetAIFeedback(feedback);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取AI反馈失败");
                _feynmanPanel?.SetAIFeedback($"❌ 获取AI反馈失败：{ex.Message}");
            }
        }

        private async void FeynmanPanel_GenerateSimplifiedRequested(object? sender, EventArgs e)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetSimplifiedLoading(true);

                var content = _currentItem.GetMainContent();
                var prompt = $"请用一句话（不超过30个字）总结以下知识点的核心内容：\n\n{content}";

                var result = await _aiQuestionService.AskAsync(prompt, content);
                result = result.Trim().Trim('"', '。', '.');
                _feynmanPanel.SetSimplifiedText(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成简化总结失败");
                _feynmanPanel?.SetSimplifiedText($"❌ 生成失败：{ex.Message}");
            }
        }

        private async void FeynmanPanel_GenerateAnalogyRequested(object? sender, EventArgs e)
        {
            if (_currentItem == null || _aiQuestionService == null || _feynmanPanel == null)
                return;

            try
            {
                _feynmanPanel.SetAnalogyLoading(true);

                var content = _currentItem.GetMainContent();
                var displayText = _currentItem.GetDisplayText();
                var prompt = $"请用一个生动形象的比喻/类比来解释\"{displayText}\"这个概念，让初学者也能轻松理解：\n\n参考内容：{content}";

                var result = await _aiQuestionService.AskAsync(prompt, content);
                result = result.Trim();
                _feynmanPanel.SetAnalogyText(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成类比失败");
                _feynmanPanel?.SetAnalogyText($"❌ 生成失败：{ex.Message}");
            }
        }

        private void FeynmanPanel_VoiceInputRequested(object? sender, EventArgs e)
        {
            try
            {
                if (_isDictationActive)
                {
                    _speechService?.StopDictation();
                    _isDictationActive = false;
                    return;
                }

                _speechService ??= Program.GetService<SpeechService>();
                if (_speechService == null)
                {
                    MessageBox.Show("语音服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _speechService.DictationCompleted -= OnDictationCompleted;
                _speechService.DictationCompleted += OnDictationCompleted;
                _speechService.DictationError -= OnDictationError;
                _speechService.DictationError += OnDictationError;

                _speechService.StartDictation();
                _isDictationActive = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启动语音输入失败");
                MessageBox.Show($"启动语音输入失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _isDictationActive = false;
            }
        }

        private void OnDictationCompleted(object? sender, Services.Learning.DictationResultEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDictationCompleted(sender, e)));
                return;
            }

            if (e.Success && !string.IsNullOrWhiteSpace(e.Text))
            {
                _feynmanPanel?.AppendVoiceText(e.Text);
            }
        }

        private void OnDictationError(object? sender, string e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnDictationError(sender, e)));
                return;
            }

            _logger.LogWarning("语音输入错误：{Error}", e);
        }

        private void ButtonExit_Click(object? sender, EventArgs e)
        {
            Close();
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonEdit_Click(object? sender, EventArgs e)
        {
            try
            {
                var logger = Program.GetService<ILogger<ContentEditorForm>>();
                var appConfig = Program.GetService<AppConfig>();
                var aiPanelPopupService = Program.GetService<IAIPanelPopupService>();
                var themeService = Program.GetService<IThemeService>();
                var contentLoaderService = Program.GetService<Services.Learning.IContentLoaderService>();
                var aiQuestionService = Program.GetService<Services.AI.IAiQuestionService>();

                if (logger == null || appConfig == null || aiPanelPopupService == null || themeService == null ||
                    contentLoaderService == null || aiQuestionService == null)
                {
                    MessageBox.Show("无法加载内容编辑器所需的服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var editorForm = new ContentEditorForm(logger, appConfig, aiPanelPopupService, themeService);
                var presenterLogger = Program.GetService<ILogger<Presenters.ContentEditorPresenter>>();
                if (presenterLogger == null)
                {
                    MessageBox.Show("无法获取Presenter日志服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                var presenter = new Presenters.ContentEditorPresenter(presenterLogger, editorForm, contentLoaderService, aiQuestionService);
                editorForm.SetPresenter(presenter);
                editorForm.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开内容编辑器失败");
                MessageBox.Show($"打开内容编辑器失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /*
        private async void ButtonNote_Click(object? sender, EventArgs e)
        {
            if (panelNotes.Visible)
            {
                await AnimateNotesPanel(false);
            }
            else
            {
                LoadNotes();
                await AnimateNotesPanel(true);
            }
        }*/
        private void ButtonNote_Click(object? sender, EventArgs e)
        {
            try
            {
                var noteService = Program.GetService<INoteService>();
                if (noteService == null)
                {
                    MessageBox.Show("无法加载笔记服务", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string currentContent = _learningCard?.Title ?? string.Empty;
                string currentCategory = comboBoxSubCategory.Text ?? "学习笔记";

                using var addNoteForm = new Notes.AddNoteForm(noteService, GetCurrentUserId());
                addNoteForm.Category = currentCategory;
                addNoteForm.Title = currentContent;
                addNoteForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开笔记窗口失败");
                MessageBox.Show($"打开笔记窗口失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonAchievements_Click(object? sender, EventArgs e)
        {
            try
            {
                var achievementForm = new AchievementForm(_gamificationService);
                achievementForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开成就窗口失败");
                MessageBox.Show($"打开成就窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonChallenges_Click(object? sender, EventArgs e)
        {
            try
            {
                var challengeForm = new ChallengeForm(_gamificationService);
                challengeForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开挑战窗口失败");
                MessageBox.Show($"打开挑战窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        private void ButtonReview_Click(object? sender, EventArgs e)
        {
            try
            {
                if (_spacedRepetitionService == null)
                {
                    MessageBox.Show("复习服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var reviewForm = new ReviewForm(_spacedRepetitionService, "default");
                reviewForm.StartReview += OnStartReview;
                reviewForm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开复习窗口失败");
                MessageBox.Show($"打开复习窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnStartReview(object? sender, EventArgs e)
        {
            ReviewClicked?.Invoke(this, EventArgs.Empty);
        }


        private void ButtonOpenStatistics_Click(object? sender, EventArgs e)
        {
            OpenStatisticsClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonExportErrorBook_Click(object? sender, EventArgs e)
        {
            ExportErrorBookClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonRevealAnswer_Click(object? sender, EventArgs e)
        {
            _answerRevealed = true;
            if (_learningCard != null)
            {
                _learningCard.Visible = true;
                if (_currentItem != null)
                {
                    _learningCard.Content = _currentItem.GetDisplayText();
                }
            }
        }

        private void ButtonShowAnswer_Click(object? sender, EventArgs e)
        {
            ToggleAnswerDisplay();
        }

        private void ToggleAnswerDisplay()
        {
            if (_isShowAnswer)
            {
                _isShowAnswer = false;
                UpdateDetailState(true, true);
                buttonShowAnswer.BackColor = Color.FromArgb(255, 193, 7);
                buttonShowAnswer.Text = "🎮 答题模式";
                labelQuizHint.Text = "学习模式，显示完整内容";
            }
            else
            {
                _isShowAnswer = true;
                UpdateDetailState(true, false);
                buttonShowAnswer.BackColor = Color.FromArgb(76, 175, 80);
                buttonShowAnswer.Text = "📖 学习模式";
                labelQuizHint.Text = "答案已隐藏";
            }
        }

        private void ToggleAutoPlay()
        {
            _autoPlayEnabled = !_autoPlayEnabled;
            if (_autoPlayEnabled)
            {
                _autoPlayTimer.Start();
                UpdateAutoPlayStatus(true);
                ShowToast($"▶️ 自动播放已开启，每隔 {_autoPlayDelaySeconds} 秒自动切换", ToastType.Info, 2000);
            }
            else
            {
                _autoPlayTimer.Stop();
                UpdateAutoPlayStatus(false);
                ShowToast("⏸️ 自动播放已暂停", ToastType.Info, 1500);
            }
        }

        private void UpdateAutoPlayStatus(bool enabled)
        {
            var statusText = enabled
                ? $"下一项 (Enter / → / PageDown / End)\n▶️ 自动播放中 ({_autoPlayDelaySeconds}s) - F8关闭"
                : "下一项 (Enter / → / PageDown / End)";
            _toolTip.SetToolTip(_buttonsView.ButtonNext, statusText);
        }

        private void AutoPlayTimer_Tick(object? sender, EventArgs e)
        {
            if (!_autoPlayEnabled || _disposed)
                return;

            NextClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonThemeToggle_Click(object? sender, EventArgs e)
        {
            if (_themeService.CurrentTheme == ThemeMode.Light)
            {
                _themeService.SetTheme(ThemeMode.Dark);
                buttonThemeToggle.Text = "☀️ 浅色模式";
            }
            else
            {
                _themeService.SetTheme(ThemeMode.Light);
                buttonThemeToggle.Text = "🌙 深色模式";
            }
        }

        private void ButtonFavorite_Click(object? sender, EventArgs e)
        {
            _isFavorite = !_isFavorite;

            if (_isFavorite)
            {
                _soundService?.PlaySuccess();
                _favoriteCount++;
                SaveFavorite();
                _gamificationService.RecordFavorite();
                _gamificationService.CheckBadgeUnlock("favorite", _favoriteCount);
                UpdateChallengesProgress();
                ShowToast("⭐ 已收藏", ToastType.Success, 1500);
            }
            else
            {
                _favoriteCount = Math.Max(0, _favoriteCount - 1);
                RemoveFavorite();
                ShowToast("💔 已取消收藏", ToastType.Info, 1200);
            }

            UpdateFavoriteButton();
        }

        private void SaveFavorite()
        {
            if (_currentItem == null) return;

            try
            {
                string favoritesPath = GetUserFavoritesPath();
                List<string> favorites = new List<string>();

                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }

                string key = GetItemKey();
                if (!string.IsNullOrEmpty(key) && !favorites.Contains(key))
                {
                    favorites.Add(key);
                    string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(favoritesPath, json);
                }

                InvalidateFavoritesCache();
                LoadFavoritesToListView();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存收藏失败");
            }
        }

        private void RemoveFavorite()
        {
            if (_currentItem == null) return;

            try
            {
                string favoritesPath = GetUserFavoritesPath();

                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    List<string> favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                    string key = GetItemKey();
                    if (!string.IsNullOrEmpty(key))
                    {
                        favorites.Remove(key);
                        string newJson = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(favoritesPath, newJson);
                    }

                    InvalidateFavoritesCache();
                    LoadFavoritesToListView();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "移除收藏失败");
            }
        }

        /// <summary>
        /// 获取当前用户ID
        /// </summary>
        private string GetCurrentUserId()
        {
            return _userSessionService?.CurrentUserId ?? "default";
        }

        /// <summary>
        /// 获取用户收藏文件路径
        /// </summary>
        private string GetUserFavoritesPath()
        {
            var userId = GetCurrentUserId();
            var userDir = Path.Combine(AppPaths.UsersDir, userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "favorites.json");
        }

        /// <summary>
        /// 获取用户笔记文件路径
        /// </summary>
        private string GetUserNotesPath()
        {
            var userId = GetCurrentUserId();
            var userDir = Path.Combine(AppPaths.UsersDir, userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "notes.json");
        }

        /// <summary>
        /// 获取用户设置文件路径
        /// </summary>
        private string GetUserSettingsPath()
        {
            var userId = GetCurrentUserId();
            var userDir = Path.Combine(AppPaths.UsersDir, userId);
            if (!Directory.Exists(userDir))
                Directory.CreateDirectory(userDir);
            return Path.Combine(userDir, "settings.json");
        }

        /// <summary>
        /// 生成项目的唯一键（子类别+内容），避免跨类别冲突
        /// </summary>
        private string GetItemKey()
        {
            if (_currentItem == null) return string.Empty;

            string subCategory = comboBoxSubCategory?.Text ?? "unknown";
            string content = _currentItem.GetMainContent();

            return $"[{subCategory}]{content}";
        }


        private async Task AnimateNotesPanel(bool show)
        {
            const int targetHeight = 200;
            const int step = 10;
            const int delay = 10;

            if (show)
            {
                panelNotes.Visible = true;
                middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
                middleTableLayoutPanel.PerformLayout();

                for (int height = 0; height <= targetHeight; height += step)
                {
                    middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, height);
                    middleTableLayoutPanel.PerformLayout();
                    await Task.Delay(delay);
                }

                middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, targetHeight);
                middleTableLayoutPanel.PerformLayout();
                buttonNote.Text = "📝 笔记 (开)";
            }
            else
            {
                var currentStyle = middleTableLayoutPanel.RowStyles[1];
                int currentHeight = (int)(currentStyle.SizeType == SizeType.Absolute ? currentStyle.Height : targetHeight);

                for (int height = currentHeight; height >= 0; height -= step)
                {
                    middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, Math.Max(0, height));
                    middleTableLayoutPanel.PerformLayout();
                    await Task.Delay(delay);
                }

                middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
                middleTableLayoutPanel.PerformLayout();
                panelNotes.Visible = false;
                buttonNote.Text = "📝 笔记";
            }
        }

        private void LoadNotes()
        {
            if (_currentItem == null || richTextBoxNotes == null) return;

            try
            {
                string notesPath = GetUserNotesPath();

                if (File.Exists(notesPath))
                {
                    string json = File.ReadAllText(notesPath);
                    var notesDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();

                    // 使用唯一键（子类别+内容）
                    string key = GetNoteKey();
                    if (notesDict.TryGetValue(key, out string note))
                    {
                        richTextBoxNotes.Text = note;
                    }
                    else
                    {
                        // 兼容旧数据：尝试使用旧键（仅内容）
                        string oldKey = _currentItem.GetMainContent();
                        if (notesDict.TryGetValue(oldKey, out string oldNote))
                        {
                            richTextBoxNotes.Text = oldNote;
                            // 迁移到新键
                            notesDict[key] = oldNote;
                            notesDict.Remove(oldKey);
                            File.WriteAllText(notesPath, JsonSerializer.Serialize(notesDict, new JsonSerializerOptions { WriteIndented = true }));
                        }
                        else
                        {
                            richTextBoxNotes.Text = "";
                        }
                    }
                }
                else
                {
                    richTextBoxNotes.Text = "";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载笔记失败");
                richTextBoxNotes.Text = "";
            }
        }

        private void SaveNotes()
        {
            if (_currentItem == null || richTextBoxNotes == null) return;

            try
            {
                string notesPath = GetUserNotesPath();
                Dictionary<string, string> notesDict = new Dictionary<string, string>();

                if (File.Exists(notesPath))
                {
                    string json = File.ReadAllText(notesPath);
                    notesDict = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                }

                // 使用唯一键（子类别+内容）
                string key = GetNoteKey();
                if (!string.IsNullOrEmpty(key))
                {
                    notesDict[key] = richTextBoxNotes.Text;

                    string jsonOutput = JsonSerializer.Serialize(notesDict, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(notesPath, jsonOutput);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存笔记失败");
            }
        }

        /// <summary>
        /// 获取笔记的唯一键（复用 GetItemKey）
        /// </summary>
        private string GetNoteKey() => GetItemKey();

        private void RichTextBoxNotes_TextChanged(object? sender, EventArgs e)
        {
            if (richTextBoxNotes != null)
            {
                bool hasContent = !string.IsNullOrWhiteSpace(richTextBoxNotes.Text);
                if (hasContent && !_currentNoteCounted)
                {
                    _currentNoteCounted = true;
                    _noteCount++;
                    _gamificationService.RecordNote();
                    _gamificationService.CheckBadgeUnlock("note", _noteCount);
                }
                else if (!hasContent && _currentNoteCounted)
                {
                    _currentNoteCounted = false;
                    _noteCount = Math.Max(0, _noteCount - 1);
                }
            }

            _noteSaveTimer.Stop();
            _noteSaveTimer.Start();
        }

        private void NoteSaveTimer_Tick(object? sender, EventArgs e)
        {
            _noteSaveTimer.Stop();
            SaveNotes();
        }

        private bool ContainsChinese(string text) => text.Any(c => c >= 0x4E00 && c <= 0x9FFF);

        private void LearningForm_KeyDown(object? sender, KeyEventArgs e)
        {
            if (ProcessShortcut(e.KeyCode))
            {
                e.Handled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (ProcessShortcut(keyData))
            {
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override bool ProcessDialogKey(Keys keyData)
        {
            // 全局拦截上下键用于列表导航，防止被其他控件（如RadioButton）消耗
            if (keyData == Keys.Up || keyData == Keys.Down)
            {
                if (listBoxItems != null && listBoxItems.Items.Count > 0)
                {
                    int newIndex = keyData == Keys.Up
                        ? listBoxItems.SelectedIndex - 1
                        : listBoxItems.SelectedIndex + 1;

                    if (newIndex < 0) newIndex = listBoxItems.Items.Count - 1;
                    if (newIndex >= listBoxItems.Items.Count) newIndex = 0;

                    listBoxItems.SelectedIndex = newIndex;
                    return true;
                }
            }
            return base.ProcessDialogKey(keyData);
        }

        private bool ProcessShortcut(Keys keyCode)
        {
            switch (keyCode)
            {
                case Keys.Space:
                    PronounceClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.Enter:
                    _soundService?.PlayNavigation();
                    NextClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.D1:
                case Keys.K:
                    _soundService?.PlaySuccess();
                    MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.D2:
                case Keys.U:
                    _soundService?.PlayError();
                    MarkAsUnknownClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.D3:
                case Keys.F:
                    ButtonFavorite_Click(this, EventArgs.Empty);
                    return true;
                case Keys.D4:
                case Keys.N:
                    ButtonNote_Click(this, EventArgs.Empty);
                    return true;
                case Keys.D5:
                case Keys.E:
                    ButtonEdit_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Left:
                case Keys.PageUp:
                    ButtonPrevious_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Right:
                case Keys.PageDown:
                    ButtonNextNav_Click(this, EventArgs.Empty);
                    return true;
                case Keys.Home:
                    JumpToItem(0);
                    return true;
                case Keys.End:
                    if (listBoxItems != null && listBoxItems.Items.Count > 0)
                    {
                        JumpToItem(listBoxItems.Items.Count - 1);
                    }
                    return true;
                case Keys.R:
                    JumpToRandomItem();
                    return true;
                case Keys.Escape:
                    ExitClicked?.Invoke(this, EventArgs.Empty);
                    Close();
                    return true;
                case Keys.F1:
                    StartProgressiveHint();
                    return true;
                case Keys.F2:
                    StartAssociationLearning();
                    return true;
                case Keys.F3:
                    ToggleAnswerDisplay();
                    return true;
                case Keys.F4:
                    ShowFeynmanQuestions();
                    return true;
                case Keys.F5:
                    ShowDailyThinkingTask();
                    return true;
                case Keys.F6:
                    ButtonAIAsk_Click(this, EventArgs.Empty);
                    return true;
                case Keys.F7:
                    ButtonFeynman_Click(this, EventArgs.Empty);
                    return true;
                case Keys.F8:
                    ToggleAutoPlay();
                    return true;
                case Keys.F9:
                    ShowJumpToDialog();
                    return true;
                case Keys.F10:
                    ShowShortcutHelp();
                    return true;
                default:
                    return false;
            }
        }

        private void ShowShortcutHelp()
        {
            string helpText =
                "⌨️  快捷键帮助\n\n" +
                "━━━━━ 学习操作 ━━━━━\n" +
                "  1 / K   → 标记为已知\n" +
                "  2 / U   → 标记为未知\n" +
                "  Space   → 播放发音\n" +
                "  3 / F   → 收藏/取消收藏\n" +
                "  4 / N   → 打开笔记\n" +
                "  5 / E   → 编辑内容\n\n" +
                "━━━━━ 导航操作 ━━━━━\n" +
                "  ← / PageUp  → 上一项\n" +
                "  → / PageDown → 下一项\n" +
                "  Enter       → 下一项\n" +
                "  Home        → 跳到第一项\n" +
                "  End         → 跳到最后一项\n" +
                "  R           → 随机跳转\n" +
                "  ↑ / ↓       → 列表上下选择\n" +
                "  F9          → 跳转到指定序号\n\n" +
                "━━━━━ 功能按键 ━━━━━\n" +
                "  F1     → 渐进式提示\n" +
                "  F2     → 联想学习\n" +
                "  F3     → 切换学习/答题模式\n" +
                "  F4     → 费曼学习法\n" +
                "  F5     → 每日思考任务\n" +
                "  F6     → AI 问答\n" +
                "  F7     → 费曼学习面板\n" +
                "  F8     → 自动播放开关\n" +
                "  F10    → 快捷键帮助（本窗口）\n" +
                "  Esc    → 退出学习\n\n" +
                "提示：鼠标悬停在按钮上也可查看快捷键";

            MessageBox.Show(helpText, "快捷键大全", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void JumpToItem(int index)
        {
            if (listBoxItems == null || listBoxItems.Items.Count == 0) return;
            if (index < 0) index = 0;
            if (index >= listBoxItems.Items.Count) index = listBoxItems.Items.Count - 1;

            listBoxItems.SelectedIndex = index;
            listBoxItems.TopIndex = Math.Max(0, index - 5);
            _soundService?.PlayNavigation();
        }

        private void JumpToRandomItem()
        {
            if (listBoxItems == null || listBoxItems.Items.Count <= 1) return;

            Random rnd = new Random();
            int newIndex;
            do
            {
                newIndex = rnd.Next(listBoxItems.Items.Count);
            } while (newIndex == listBoxItems.SelectedIndex && listBoxItems.Items.Count > 1);

            JumpToItem(newIndex);
        }

        private void ShowJumpToDialog()
        {
            if (listBoxItems == null || listBoxItems.Items.Count == 0) return;

            using var form = new Form();
            form.Text = "跳转到";
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterParent;
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.Width = 300;
            form.Height = 150;

            var label = new Label
            {
                Text = $"输入序号 (1 - {listBoxItems.Items.Count}):",
                Location = new Point(20, 20),
                AutoSize = true
            };

            var textBox = new TextBox
            {
                Location = new Point(20, 50),
                Width = 240,
                Text = (listBoxItems.SelectedIndex + 1).ToString()
            };

            var okButton = new Button
            {
                Text = "确定",
                DialogResult = DialogResult.OK,
                Location = new Point(110, 85),
                Width = 70
            };

            var cancelButton = new Button
            {
                Text = "取消",
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, 85),
                Width = 70
            };

            form.Controls.Add(label);
            form.Controls.Add(textBox);
            form.Controls.Add(okButton);
            form.Controls.Add(cancelButton);
            form.AcceptButton = okButton;
            form.CancelButton = cancelButton;

            if (form.ShowDialog(this) == DialogResult.OK)
            {
                if (int.TryParse(textBox.Text, out int target) && target >= 1 && target <= listBoxItems.Items.Count)
                {
                    JumpToItem(target - 1);
                }
            }
        }


        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }

                _studyTimer?.Stop();
                _studyTimer?.Dispose();

                _confettiTimer?.Stop();
                _confettiTimer?.Dispose();

                _noteSaveTimer?.Stop();
                _noteSaveTimer?.Dispose();

                _autoPlayTimer?.Stop();
                _autoPlayTimer?.Dispose();

                _gameTimer?.Stop();
                _gameTimer?.Dispose();

                _toolTip?.Dispose();

                _themeService?.UnregisterThemeable(this);

                if (_gamificationService != null)
                {
                    _gamificationService.BadgesUnlocked -= OnBadgesUnlocked;
                    _gamificationService.LevelUp -= OnLevelUp;
                    _gamificationService.XPChanged -= OnXPChanged;
                }

                (_confettiManager as IDisposable)?.Dispose();

                // 解绑子视图事件，防止内存泄漏
                if (_buttonsView != null)
                {
                    _buttonsView.KnownClicked -= ButtonKnown_Click;
                    _buttonsView.UnknownClicked -= ButtonUnknown_Click;
                    _buttonsView.NextClicked -= ButtonNext_Click;
                    _buttonsView.PronounceClicked -= ButtonPronounce_Click;
                    _buttonsView.FavoriteClicked -= ButtonFavorite_Click;
                    _buttonsView.NoteClicked -= ButtonNote_Click;
                    _buttonsView.ExitClicked -= ButtonExit_Click;
                    _buttonsView.AIAskClicked -= ButtonAIAsk_Click;
                    _buttonsView.FeynmanClicked -= ButtonFeynman_Click;
                }

                if (_settingsView != null)
                {
                    _settingsView.RadioStudyMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioQuickMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioSequential.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioRandom.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.ComboBoxSubject.SelectedIndexChanged -= ComboBoxSubject_SelectedIndexChanged;
                    _settingsView.ComboBoxSubCategory.SelectedIndexChanged -= ComboBoxSubCategory_SelectedIndexChanged;
                    _settingsView.ButtonOpenStatistics.Click -= ButtonOpenStatistics_Click;
                    _settingsView.ButtonExportErrorBook.Click -= ButtonExportErrorBook_Click;
                    _settingsView.ButtonShowAnswer.Click -= ButtonShowAnswer_Click;
                    _settingsView.ButtonThemeToggle.Click -= ButtonThemeToggle_Click;
                }

                if (_contentView != null)
                {
                    _contentView.ContentClicked -= LabelContent_Click;
                    _contentView.DetailClicked -= ListBoxDisplay_Click;
                    _contentView.NoteTextChanged -= RichTextBoxNotes_TextChanged;
                }

                if (_listView != null)
                {
                    _listView.SelectedIndexChanged -= ListBoxItems_SelectedIndexChanged;
                }

                if (listBoxItems != null)
                {
                    listBoxItems.MouseMove -= ListBoxItems_MouseMove;
                    listBoxItems.MouseLeave -= ListBoxItems_MouseLeave;
                }

                _listView?.Dispose();
                _contentView?.Dispose();
                _buttonsView?.Dispose();
                _statsView?.Dispose();
                _statsProgressView?.Dispose();
                _statsButtonView?.Dispose();
                _settingsView?.Dispose();

                _selectedBackgroundBrush.Dispose();
                _selectedForegroundBrush.Dispose();
                _normalForegroundBrush.Dispose();
                _selectedBorderPen.Dispose();
                _hoverBackgroundBrush.Dispose();

                if (_speechService != null)
                {
                    _speechService.DictationCompleted -= OnDictationCompleted;
                    _speechService.DictationError -= OnDictationError;
                    if (_isDictationActive)
                    {
                        try { _speechService.StopDictation(); } catch { }
                    }
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        private class ThemeColorTable : ProfessionalColorTable
        {
            private readonly ThemeColors _colors;

            public ThemeColorTable(ThemeColors colors)
            {
                _colors = colors;
            }

            public override Color ToolStripGradientBegin => _colors.Surface;
            public override Color ToolStripGradientMiddle => _colors.Surface;
            public override Color ToolStripGradientEnd => _colors.Surface;
            public override Color ToolStripBorder => _colors.Divider;
            public override Color ButtonSelectedBorder => _colors.Divider;
            public override Color ButtonSelectedHighlight => _colors.Accent;
            public override Color ButtonSelectedHighlightBorder => _colors.Accent;
            public override Color ButtonPressedHighlight => ControlPaint.Dark(_colors.Accent, 20);
            public override Color ButtonPressedHighlightBorder => ControlPaint.Dark(_colors.Accent, 30);
            public override Color MenuBorder => _colors.Divider;
            public override Color SeparatorDark => _colors.Divider;
            public override Color SeparatorLight => _colors.Surface;
        }
    }
}
