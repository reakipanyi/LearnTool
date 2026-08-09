using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Managers;
using LearningAssistant.Models.Config;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
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
        private readonly LearningFormServices _services;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConfettiManager _confettiManager;
        private readonly EncouragementManager _encouragementManager;

        private ITTSService? _ttsService => _services.AudioServices.TTSService;
        private ISoundService? _soundService => _services.NotificationServices.SoundService;
        private IThemeService _themeService => _services.ThemeService;
        private IEncouragementService _encouragementService => _services.GamificationServices.EncouragementService;
        private IGamificationService _gamificationService => _services.GamificationServices.GamificationService;
        private IEventBus? _eventBus => _services.NotificationServices.EventBus;
        private IUserSessionService? _userSessionService => _services.UserSessionService;
        private IDataPersistenceService? _persistenceService => _services.PersistenceService;
        #endregion

        #region === 学习状态 ===
        private LearningItem? _currentItem;
        private bool _isShowAnswer = false;
        private bool _answerRevealed = false;
        private bool _isFavorite = false;
        private bool _disposed = false;
        private Settings _settings = new();
        private bool _autoPlayEnabled = false;
        private readonly System.Windows.Forms.Timer _autoPlayTimer = new System.Windows.Forms.Timer();
        private int _autoPlayDelaySeconds = 5;
        private HashSet<string>? _cachedFavorites;
        private DateTime _favoritesCacheTime = DateTime.MinValue;
        private static readonly TimeSpan FavoritesCacheDuration = TimeSpan.FromSeconds(10);
        
        private HashSet<string>? _cachedKnownItems;
        private HashSet<string>? _cachedUnknownItems;
        #endregion

        #region === 进度可视化 ===
        private CircularProgressControl _dailyGoalProgress = null!;
        #endregion

        #region === 卡片展示 ===
        private LearningCard _learningCard = null!;
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
        private SpeedSelectorControl speedSelector => _settingsView.SpeedSelector;
        private Button buttonFavorite => _buttonsView.ButtonFavorite;
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

        private FloatingText _floatingText = null!;
        #endregion

        private LevelBadge _levelBadge;

        #region === 设计器生成 ===
        private System.ComponentModel.IContainer components = null;
        #endregion

        #region === 构造函数 ===
        public LearningForm(
            LearningFormServices services,
            ILogger<LearningForm> logger,
            ILoggerFactory loggerFactory)
        {
            InitializeComponent();
            WindowState = FormWindowState.Maximized;
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

            _gamificationService.BadgesUnlocked += OnBadgesUnlocked;
            _gamificationService.LevelUp += OnLevelUp;
            _gamificationService.XPChanged += OnXPChanged;

            _encouragementManager = new EncouragementManager();

            _confettiManager = new ConfettiManager();

            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;

            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

            _themeService.RegisterThemeable(this);

            // 自动播放计时器
            _autoPlayTimer.Interval = 5000;
            _autoPlayTimer.Tick += AutoPlayTimer_Tick;

        }
        #endregion

        private void BindSubViewEvents()
        {
            _buttonsView.KnownClicked += ButtonKnown_Click;
            _buttonsView.UnknownClicked += ButtonUnknown_Click;
            _buttonsView.FavoriteClicked += ButtonFavorite_Click;
            _buttonsView.ExitClicked += ButtonExit_Click;

            _settingsView.RadioStudyMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioQuickMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioSequential.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioRandom.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.ComboBoxSubject.SelectedIndexChanged += ComboBoxSubject_SelectedIndexChanged;
            _settingsView.ComboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            _settingsView.SpeedSelector.TtsConfig = _services.AudioServices.TtsConfig;
            _settingsView.ButtonShowAnswer.Click += ButtonShowAnswer_Click;
            _settingsView.ButtonThemeToggle.Click += ButtonThemeToggle_Click;

            _contentView.ContentClicked += LabelContent_Click;
            _contentView.DetailClicked += ListBoxDisplay_Click;

            _listView.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;

            _settingsView.UserChanged += StatsButtonView_UserChanged;
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


        private async void LearningForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
            try
            {
                _gamificationService?.Save();

                if (_services.SpeechCoordinator != null)
                {
                    await _services.SpeechCoordinator.StopAsync();
                }

                if (_ttsService != null)
                {
                    await _ttsService.StopAsync();
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
                _settings.SubCategory = SubCategory.ToString();
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

                // 刷新语速选择器显示
                speedSelector.RefreshDisplay();

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
        public string CurrentDisplayText { set { } }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayStruct { set { } }

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
            _learningCard.Category = comboBoxSubCategory.Text;
            _learningCard.Icon = GetSubjectIcon();
            _learningCard.AccentColor = GetSubjectColor();
            _learningCard.IsSelected = true;

            UpdateCardFields();
        }

        private void UpdateCardFields()
        {
            if (_learningCard == null || _currentItem == null) return;

            var fields = LearningItemFormatter.BuildFields(_currentItem, _answerRevealed);
            _learningCard.SetFields(fields);
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
        public string SearchText
        {
            get => _listView?.TextBoxSearch.Text ?? string.Empty;
            set => _listView?.FilterItems(value);
        }

        public event EventHandler? SearchTextChanged;

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

        public LearningModeType CurrentMode => radioStudyMode.Checked ? LearningModeType.Study : LearningModeType.QuickReview;

        public LearningModeType LearningMode => radioStudyMode.Checked ? LearningModeType.Study : LearningModeType.QuickReview;

        public SortOrderType SortOrder => radioSequential.Checked ? SortOrderType.Sequential : SortOrderType.Random;

        public SubjectType Subject
        {
            get
            {
                var subjectStr = comboBoxSubject.Text;
                return SubjectSubCategoryMapping.TryParseSubject(subjectStr, out var subject) ? subject : SubjectType.Chinese;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public SubCategoryType SubCategory
        {
            get
            {
                if (comboBoxSubCategory.SelectedItem is SubCategoryItem item)
                {
                    return item.Value;
                }
                return SubjectSubCategoryMapping.TryParseSubCategory(comboBoxSubCategory.Text, out var subCategory) ? subCategory : SubCategoryType.ChineseCharacter;
            }
            set
            {
                for (int i = 0; i < comboBoxSubCategory.Items.Count; i++)
                {
                    if (comboBoxSubCategory.Items[i] is SubCategoryItem item && item.Value == value)
                    {
                        comboBoxSubCategory.SelectedIndex = i;
                        return;
                    }
                }
                comboBoxSubCategory.Text = SubjectSubCategoryMapping.GetSubCategoryDisplayName(value);
            }
        }

        public LearningContext CurrentContext => new(
            UserId: GetCurrentUserId(),
            Subject: Subject,
            SubCategory: SubCategory,
            Mode: LearningMode,
            SortOrder: SortOrder
        );


        public void RefreshSubCategories(List<SubCategoryType> subCategories)
        {
            comboBoxSubCategory.SelectedIndexChanged -= ComboBoxSubCategory_SelectedIndexChanged;

            comboBoxSubCategory.Items.Clear();
            foreach (var cat in subCategories)
            {
                comboBoxSubCategory.Items.Add(new SubCategoryItem(cat));
            }

            comboBoxSubCategory.DisplayMember = "DisplayName";

            if (comboBoxSubCategory.Items.Count > 0)
            {
                comboBoxSubCategory.SelectedIndex = 0;
            }

            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
        }

        private class SubCategoryItem
        {
            public SubCategoryType Value { get; }
            public string DisplayName { get; }

            public SubCategoryItem(SubCategoryType value)
            {
                Value = value;
                DisplayName = SubjectSubCategoryMapping.GetSubCategoryDisplayName(value);
            }

            public override string ToString() => DisplayName;
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

        public void ShowMessage(string msg)
        {
            ShowToast(msg, ToastType.Info);
        }

        public void ShowMessage(string msg, string title)
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
            _listView.Dock = DockStyle.Fill;
            _listView.Name = "_listView";
            _listView.TabIndex = 0;
            // 
            // _contentView
            // 
            _contentView.Dock = DockStyle.Fill;
            _contentView.Name = "_contentView";
            _contentView.TabIndex = 0;
            // 
            // _buttonsView
            // 
            _buttonsView.Dock = DockStyle.Fill;
            _buttonsView.Name = "_buttonsView";
            _buttonsView.TabIndex = 0;
            // 
            // _statsView
            // 
            _statsView.Dock = DockStyle.Fill;
            _statsView.Location = new Point(3, 806);
            _statsView.Name = "_statsView";
            _statsView.Size = new Size(1089, 29);
            _statsView.TabIndex = 9;
            // 
            // _statsProgressView
            // 
            _statsProgressView.Dock = DockStyle.Fill;
            _statsProgressView.Location = new Point(3, 753);
            _statsProgressView.Name = "_statsProgressView";
            _statsProgressView.Size = new Size(1089, 47);
            _statsProgressView.TabIndex = 1;
            // 
            // _settingsView
            // 
            _settingsView.Dock = DockStyle.Fill;
            _settingsView.Name = "_settingsView";
            _settingsView.TabIndex = 0;
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.ColumnCount = 3;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 266F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            mainTableLayoutPanel.Controls.Add(_listView, 0, 0);
            mainTableLayoutPanel.Controls.Add(middlePanel, 1, 0);
            mainTableLayoutPanel.Controls.Add(_settingsView, 2, 0);
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
            middleTableLayoutPanel.Controls.Add(_contentView, 0, 0);
            middleTableLayoutPanel.Controls.Add(_buttonsView, 0, 2);
            middleTableLayoutPanel.Controls.Add(_statsProgressView, 0, 3);
            middleTableLayoutPanel.Controls.Add(_statsView, 0, 4);
            middleTableLayoutPanel.Dock = DockStyle.Fill;
            middleTableLayoutPanel.Location = new Point(0, 0);
            middleTableLayoutPanel.Name = "middleTableLayoutPanel";
            middleTableLayoutPanel.RowCount = 5;
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 74F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 53F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 35F));
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

            LoadUserList();

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

            // 使用当前用户加载游戏化数据，避免覆盖单例 GamificationService 的用户上下文
            // （原硬编码 "default" 会导致真实用户事件被丢弃、数据存到 default 下）。
            _gamificationService.Load(GetCurrentUserId());
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
            ApplyRoundedStyle(buttonFavorite, 8);
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
            _toolTip.SetToolTip(_buttonsView.ButtonFavorite, "收藏/取消收藏 (3 / F)");
            _toolTip.SetToolTip(_buttonsView.ButtonEdit, "编辑内容 (5 / E)");
            _toolTip.SetToolTip(_buttonsView.ButtonExit, "退出学习 (Esc)");

            _toolTip.SetToolTip(buttonShowAnswer, "切换学习/答题模式 (F3)");

            _toolTip.SetToolTip(labelEncouragement,
                "鼓励语，每隔几句学习内容自动更新一次");
            _toolTip.SetToolTip(labelDailyGoal,
                $"每日目标: {_settings.DailyGoal}项 | 当前进度: {_gamificationService?.TodayLearnedCount ?? 0}项\n" +
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
                MaxValue = _settings.DailyGoal,
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
            InitializeLearningCard();
        }

        private void InitializeLearningCard()
        {
            _learningCard = new LearningCard
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(10),
                SpeechCoordinator = _services.SpeechCoordinator
            };
            _learningCard.Click += ContentArea_Click;

            if (panelContent != null)
            {
                panelContent.Controls.Add(_learningCard);
                panelContent.Controls.SetChildIndex(_learningCard, 0);
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
            int currentFullIndex = _listView.SelectedIndexInAllItems;
            if (currentFullIndex >= 0)
            {
                ItemSelectedFromList?.Invoke(this, new ItemSelectedEventArgs(currentFullIndex - 1));
            }
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
            _ = _encouragementService.PlayRandomKnownFeedbackAsync();
            TriggerLevelUp(e.NewLevel, e.LevelTitle);
            MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{e.LevelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void StudyTimer_Tick(object? sender, EventArgs e)
        {
            _studyDuration = _studyDuration.Add(TimeSpan.FromSeconds(1));
            _gamificationService.UpdateStudyDuration(_studyDuration);
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
            bool isUnknown = IsItemUnknown(text);

            int iconSize = 20;
            int iconMargin = 8;
            int textStartX = iconMargin + iconSize + 5;

            using var iconFont = new Font("Arial", 10F);
            using var favoriteBrush = new SolidBrush(Color.FromArgb(255, 152, 0));
            using var knownBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
            using var unknownBrush = new SolidBrush(Color.FromArgb(244, 67, 54));
            using var unlearnedBrush = new SolidBrush(Color.FromArgb(158, 158, 158));

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
            else if (isUnknown)
            {
                e.Graphics.DrawString("✗", iconFont,
                    isSelected ? _selectedForegroundBrush : unknownBrush,
                    e.Bounds.X + textStartX - iconSize - 5, e.Bounds.Y + (e.Bounds.Height - iconSize) / 2);
                textStartX += iconSize;
            }
            else
            {
                e.Graphics.DrawString("·", iconFont,
                    isSelected ? _selectedForegroundBrush : unlearnedBrush,
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

        public void UpdateLearningItemStates(HashSet<string> knownItems, HashSet<string> unknownItems)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLearningItemStates(knownItems, unknownItems)));
                return;
            }

            _cachedKnownItems = new HashSet<string>(knownItems);
            _cachedUnknownItems = new HashSet<string>(unknownItems);
            listBoxItems?.Invalidate();
        }

        private bool IsItemKnown(string itemText)
        {
            LoadLearningStateCache();
            return _cachedKnownItems?.Contains(itemText) ?? false;
        }

        private bool IsItemUnknown(string itemText)
        {
            LoadLearningStateCache();
            return _cachedUnknownItems?.Contains(itemText) ?? false;
        }

        private void LoadLearningStateCache()
        {
            if (_cachedKnownItems != null && _cachedUnknownItems != null)
            {
                return;
            }

            try
            {
                var userId = GetCurrentUserId();
                var subCategory = SubCategory;
                
                if (_persistenceService != null)
                {
                    _cachedKnownItems = new HashSet<string>(_persistenceService.GetKnownItems(userId, subCategory));
                    _cachedUnknownItems = new HashSet<string>(_persistenceService.GetUnknownItems(userId, subCategory));
                }
                else
                {
                    _cachedKnownItems = new HashSet<string>();
                    _cachedUnknownItems = new HashSet<string>();
                }
            }
            catch
            {
                _cachedKnownItems = new HashSet<string>();
                _cachedUnknownItems = new HashSet<string>();
            }
        }

        private void InvalidateLearningStateCache()
        {
            _cachedKnownItems = null;
            _cachedUnknownItems = null;
            listBoxItems?.Invalidate();
        }

        private void UpdateLearningStateCacheImmediately(string itemText, bool isKnown)
        {
            if (_cachedKnownItems == null || _cachedUnknownItems == null)
            {
                LoadLearningStateCache();
            }

            if (isKnown)
            {
                _cachedKnownItems?.Add(itemText);
                _cachedUnknownItems?.Remove(itemText);
            }
            else
            {
                _cachedUnknownItems?.Add(itemText);
                _cachedKnownItems?.Remove(itemText);
            }

            listBoxItems?.Invalidate();
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
        public event EventHandler<FieldSpeakEventArgs>? FieldSpeakRequested;
        public event EventHandler<FieldSpeakEventArgs>? FieldStopRequested;
        public event EventHandler<FieldCopyEventArgs>? FieldCopyRequested;

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
                    _answerRevealed = showAnswer;
                }
                else
                {
                    _answerRevealed = true;
                }

                UpdateCardFields();
            }
        }

        private void OnBadgesUnlocked(object? sender, BadgesUnlockedEventArgs e)
        {
            _gamificationService.AddScore(50 * e.BadgeIds.Count);
            _gamificationService.AddXP(50 * e.BadgeIds.Count);
            _gamificationService.UpdateAllDisplays();

            _soundService?.PlaySuccess();
            StartConfetti();
            _ = _encouragementService.PlayRandomKnownFeedbackAsync();
        }

        private void UpdateChallengesProgress()
        {
            _gamificationService.UpdateChallengeProgress("learn", _gamificationService.TodayLearnedCount);
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
            {
                InvalidateLearningStateCache();
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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

                if (_currentItem != null)
                {
                    UpdateLearningStateCacheImmediately(_currentItem.GetMainContent(), true);
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

                    UpdateLearningStateCacheImmediately(_currentItem.GetMainContent(), false);
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

                if (logger == null || appConfig == null || aiPanelPopupService == null || themeService == null ||
                    contentLoaderService == null)
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
                var subjectTemplateService = Program.GetService<ISubjectTemplateService>();
                var presenter = new Presenters.ContentEditorPresenter(presenterLogger, editorForm, contentLoaderService, subjectTemplateService);
                editorForm.SetPresenter(presenter);
                editorForm.ShowDialog();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "打开内容编辑器失败");
                MessageBox.Show($"打开内容编辑器失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private void LoadUserList()
        {
            try
            {
                // 以数据库为唯一权威用户源，与首页/设置保持一致，避免目录残留导致列表不一致。
                var users = (_userSessionService?.GetUserList() ?? new List<string>())
                    .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                _settingsView.ComboBoxUser.Items.Clear();
                foreach (var user in users)
                {
                    _settingsView.ComboBoxUser.Items.Add(user);
                }

                var currentUser = AppPaths.GetCurrentUserId();
                if (!string.IsNullOrEmpty(currentUser) && _settingsView.ComboBoxUser.Items.Contains(currentUser))
                {
                    _settingsView.ComboBoxUser.Text = currentUser;
                }
                else if (_settingsView.ComboBoxUser.Items.Count > 0)
                {
                    _settingsView.ComboBoxUser.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载用户列表失败");
            }
        }

        private void StatsButtonView_UserChanged(object? sender, EventArgs e)
        {
            var selectedUser = _settingsView.ComboBoxUser.Text;
            if (!string.IsNullOrEmpty(selectedUser))
            {
                AppPaths.SetCurrentUserId(selectedUser);
                _userSessionService?.SaveSession(selectedUser);
                _gamificationService.Load(selectedUser);
                _gamificationService.UpdateAllDisplays();

                InvalidateLearningStateCache();
                _cachedFavorites = null;
                _favoritesCacheTime = DateTime.MinValue;

                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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
            return _userSessionService?.CurrentUserId ?? Constants.DefaultUserId;
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
            if (keyCode.HasFlag(Keys.Alt))
            {
                var keyWithoutAlt = keyCode & ~Keys.Alt;
                switch (keyWithoutAlt)
                {
                    case Keys.D1:
                        _learningCard?.TriggerFieldSpeak(0);
                        return true;
                    case Keys.D2:
                        _learningCard?.TriggerFieldSpeak(1);
                        return true;
                    case Keys.D3:
                        _learningCard?.TriggerFieldSpeak(2);
                        return true;
                    case Keys.D4:
                        _learningCard?.TriggerFieldSpeak(3);
                        return true;
                    case Keys.D5:
                        _learningCard?.TriggerFieldSpeak(4);
                        return true;
                }
            }

            switch (keyCode)
            {
                case Keys.Space:
                    _soundService?.PlayNavigation();
                    PronounceClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.Enter:
                    _soundService?.PlayNavigation();
                    NextClicked?.Invoke(this, EventArgs.Empty);
                    return true;
                case Keys.D1:
                case Keys.K:
                    ButtonKnown_Click(this, EventArgs.Empty);
                    return true;
                case Keys.D2:
                case Keys.U:
                    ButtonUnknown_Click(this, EventArgs.Empty);
                    return true;
                case Keys.D3:
                case Keys.F:
                    ButtonFavorite_Click(this, EventArgs.Empty);
                    return true;
                case Keys.D4:

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
                    ButtonExit_Click(this, EventArgs.Empty);
                    return true;

                case Keys.F1:
                    ToggleAnswerDisplay();
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
                "  3 / F   → 收藏/取消收藏\n" +
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
                "  F3     → 切换学习/答题模式\n" +
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

                _autoPlayTimer?.Stop();
                _autoPlayTimer?.Dispose();


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
                    _buttonsView.FavoriteClicked -= ButtonFavorite_Click;
                    _buttonsView.ExitClicked -= ButtonExit_Click;
                }

                if (_settingsView != null)
                {
                    _settingsView.RadioStudyMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioQuickMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioSequential.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioRandom.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.ComboBoxSubject.SelectedIndexChanged -= ComboBoxSubject_SelectedIndexChanged;
                    _settingsView.ComboBoxSubCategory.SelectedIndexChanged -= ComboBoxSubCategory_SelectedIndexChanged;
                    _settingsView.ButtonShowAnswer.Click -= ButtonShowAnswer_Click;
                    _settingsView.ButtonThemeToggle.Click -= ButtonThemeToggle_Click;
                }

                if (_contentView != null)
                {
                    _contentView.ContentClicked -= LabelContent_Click;
                    _contentView.DetailClicked -= ListBoxDisplay_Click;
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
                _settingsView?.Dispose();

                _selectedBackgroundBrush.Dispose();
                _selectedForegroundBrush.Dispose();
                _normalForegroundBrush.Dispose();
                _selectedBorderPen.Dispose();
                _hoverBackgroundBrush.Dispose();
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
