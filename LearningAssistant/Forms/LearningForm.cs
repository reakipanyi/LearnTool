using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    public partial class LearningForm : Form, ILearningView, IThemeable
    {
        private LearningItem? _currentItem;
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISoundService _soundService;
        private readonly IThemeService _themeService;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private bool _disposed = false;
        private Settings _settings = new();

        private readonly List<ConfettiParticle> _confettiParticles = new List<ConfettiParticle>();
        private readonly System.Windows.Forms.Timer _confettiTimer = new System.Windows.Forms.Timer();
        private readonly Random _random = new Random();
        private bool _isConfettiActive;

        private readonly System.Windows.Forms.Timer _studyTimer = new System.Windows.Forms.Timer();
        private TimeSpan _studyDuration = TimeSpan.Zero;
        private int _todayLearnedCount = 0;
        private int _streakDays = 0;
        private int _score = 0;
        private bool _isQuizMode = false;
        private bool _answerRevealed = false;
        private int _encouragementCounter = 0;
        private int _celebrationCounter = 0;
        private const int EncouragementInterval = 3;
        private const int CelebrationInterval = 5;

        private readonly string[] _encouragements = {
            "太棒了！继续保持！💪",
            "你做得很好！🌟",
            "学习使我快乐！📚",
            "坚持就是胜利！✨",
            "知识就是力量！💡",
            "每天进步一点点！🌱",
            "加油，你可以的！🚀",
            "聪明的选择！🎯",
            "继续努力！🔥",
            "你真了不起！👏",
            "再接再厉！🏃",
            "勇往直前！⚡",
            "信心十足！💯",
            "专注学习！🎧",
            "收获满满！📖",
            "步步为营！🚶",
            "厚积薄发！📈",
            "持之以恒！⏰",
            "自强不息！🌟",
            "志在必得！🎯"
        };

        private readonly string[] _correctMessages = {
            "回答正确！🎉",
            "完美！🌟",
            "太棒了！👏",
            "正确！✅",
            "你真聪明！💡",
            "非常棒！⭐",
            "答对了！🎊",
            "真厉害！💪",
            "满分！💯",
            "超棒！🔥"
        };

        private readonly string[] _wrongMessages = {
            "再想想！💭",
            "加油！💪",
            "别灰心！🌈",
            "继续尝试！🔥",
            "下次会更好！🌟",
            "再接再厉！💡",
            "相信自己！💪",
            "仔细想想！🤔",
            "别放弃！🚀",
            "坚持就是胜利！✨"
        };

        private readonly Dictionary<string, Badge> _badges = new Dictionary<string, Badge>
        {
            {"first_blood", new Badge("first_blood", "首战告捷", "完成第一次学习", "🏆", 1)},
            {"streak_3", new Badge("streak_3", "三日坚持", "连续学习3天", "🔥", 3)},
            {"streak_7", new Badge("streak_7", "一周达人", "连续学习7天", "⭐", 7)},
            {"streak_30", new Badge("streak_30", "月度冠军", "连续学习30天", "👑", 30)},
            {"learn_100", new Badge("learn_100", "百题斩", "累计学习100项", "💯", 100)},
            {"learn_500", new Badge("learn_500", "五百勇士", "累计学习500项", "⚔️", 500)},
            {"learn_1000", new Badge("learn_1000", "千题大师", "累计学习1000项", "🏅", 1000)},
            {"perfect_day", new Badge("perfect_day", "完美一天", "单日学习50项", "🌟", 50)},
            {"quiz_master", new Badge("quiz_master", "答题高手", "答题模式答对20题", "🎯", 20)},
            {"favorite_collector", new Badge("favorite_collector", "收藏达人", "收藏20个内容", "❤️", 20)},
            {"note_taker", new Badge("note_taker", "笔记达人", "记录10条笔记", "📝", 10)},
            {"speed_learner", new Badge("speed_learner", "神速学习", "5分钟内完成10项", "⚡", 10)}
        };

        private readonly List<string> _levelTitles = new List<string>
        {
            "小白", "学徒", "学者", "秀才", "举人", "进士", "翰林", "大师", "宗师", "圣人"
        };

        private List<string> _unlockedBadges = new List<string>();
        private int _totalLearnedCount = 0;
        private int _quizCorrectCount = 0;
        private int _favoriteCount = 0;
        private int _noteCount = 0;
        private int _currentLevel = 0;
        private string _levelTitle = "小白";
        private int _xp = 0;
        private ProgressBar progressBar1;
        private FlowLayoutPanel buttonsFlowLayoutPanel;
        private Button buttonKnown;
        private Button buttonUnknown;
        private Button buttonNext;
        private Button buttonPronounce;
        private Button buttonRevealAnswer;
        private Button buttonFavorite;
        private Button buttonNote;
        private Button buttonAIAsk;
        private Button buttonExit;
        private FlowLayoutPanel settingsFlowLayoutPanel;
        private CheckBox checkBoxVoice;
        private FlowLayoutPanel pronunciationFlowLayoutPanel;
        private RadioButton radioOriginal;
        private RadioButton radioExplanation;
        private RadioButton radioBoth;
        private int _xpToNextLevel = 100;

        private class Badge
        {
            public string Id { get; }
            public string Name { get; }
            public string Description { get; }
            public string Emoji { get; }
            public int RequiredCount { get; }
            public bool Unlocked { get; set; }

            public Badge(string id, string name, string description, string emoji, int requiredCount)
            {
                Id = id;
                Name = name;
                Description = description;
                Emoji = emoji;
                RequiredCount = requiredCount;
                Unlocked = false;
            }
        }

        private class Challenge
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string Emoji { get; set; }
            public int Target { get; set; }
            public int Current { get; set; }
            public int Reward { get; set; }
            public bool Completed { get; set; }
            public bool Claimed { get; set; }

            public Challenge() { }

            public Challenge(string id, string name, string description, string emoji, int target, int reward)
            {
                Id = id;
                Name = name;
                Description = description;
                Emoji = emoji;
                Target = target;
                Current = 0;
                Reward = reward;
                Completed = false;
                Claimed = false;
            }
        }

        private List<Challenge> _dailyChallenges = new List<Challenge>();
        private bool _badgesEventBound = false;
        private ToolTip _toolTip = new ToolTip();

        private enum ParticleShape
        {
            Rectangle,
            Circle,
            Triangle,
            Star
        }

        private class ConfettiParticle
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Size { get; set; }
            public Color Color { get; set; }
            public float VelocityX { get; set; }
            public float VelocityY { get; set; }
            public float Rotation { get; set; }
            public float RotationSpeed { get; set; }
            public ParticleShape Shape { get; set; }
            public float Opacity { get; set; } = 1.0f;
            public float FadeSpeed { get; set; }
            public double WobbleOffset { get; set; }
            public float WobbleSpeed { get; set; }
        }

        private readonly Color[] _celebrationColors = new[]
        {
            Color.FromArgb(255, 59, 48),
            Color.FromArgb(255, 149, 0),
            Color.FromArgb(255, 204, 0),
            Color.FromArgb(52, 199, 89),
            Color.FromArgb(0, 122, 255),
            Color.FromArgb(88, 86, 214),
            Color.FromArgb(175, 82, 222),
            Color.FromArgb(255, 69, 58),
            Color.FromArgb(245, 90, 140),
            Color.FromArgb(100, 200, 220)
        };

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory, ISoundService soundService, IThemeService themeService, IAIPanelPopupService aiPanelPopupService)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _aiPanelPopupService = aiPanelPopupService ?? throw new ArgumentNullException(nameof(aiPanelPopupService));


            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;

            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

            _themeService.RegisterThemeable(this);
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

            if (panelStats != null)
            {
                panelStats.BackColor = colors.Surface;
            }

            if (panelQuizMode != null)
            {
                panelQuizMode.BackColor = colors.Background;
            }

            if (listBoxDisplay != null)
            {
                listBoxDisplay.ForeColor = colors.TextPrimary;
                listBoxDisplay.BackColor = colors.Surface;
            }

            if (labelContent != null)
            {
                labelContent.ForeColor = colors.TextPrimary;
                labelContent.BackColor = colors.Surface;
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
                labelStreak.ForeColor = colors.TextPrimary;
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
            }
            else if (control is RadioButton radioButton)
            {
                radioButton.ForeColor = colors.TextPrimary;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = colors.Surface;
                comboBox.ForeColor = colors.TextPrimary;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }



        private void LearningForm_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            ApplySettings();
            EnableListHighlighting(true);
            InitializeEnhancedFeatures();
        }


        private void LearningForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
            // 停止语音播放
            _ttsService?.StopAsync().ConfigureAwait(false);
        }

        private void LoadSettings()
        {
            try
            {
                string settingsPath = Path.Combine(Paths.DataDirectory, Paths.SettingsFile);
                if (File.Exists(settingsPath))
                {
                    string json = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<Settings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
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
                _settings.Language = radioChinese.Checked ? Constants.Language.Chinese : Constants.Language.English;
                _settings.SubCategory = comboBoxSubCategory.Text;
                _settings.IsDetailVisible = checkBoxShowDetail.Checked;

                string settingsDir = Paths.DataDirectory;
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                string settingsPath = Path.Combine(settingsDir, Paths.SettingsFile);
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

                // 处理排序方式单选按钮
                bool shouldSetRandom = _settings.SortOrder == "Random" && !radioRandom.Checked;
                bool shouldSetSequential = _settings.SortOrder != "Random" && !radioSequential.Checked;

                if (shouldSetRandom) radioRandom.Checked = true;
                else if (shouldSetSequential) radioSequential.Checked = true;

                // 处理语言单选按钮
                bool shouldSetChinese = _settings.Language == "Chinese" && !radioChinese.Checked;
                bool shouldSetEnglish = _settings.Language != "Chinese" && !radioEnglish.Checked;

                if (shouldSetChinese) radioChinese.Checked = true;
                else if (shouldSetEnglish) radioEnglish.Checked = true;

                // 处理子分类
                if (!string.IsNullOrEmpty(_settings.SubCategory))
                {
                    var index = comboBoxSubCategory.Items.IndexOf(_settings.SubCategory);
                    if (index >= 0 && comboBoxSubCategory.SelectedIndex != index)
                    {
                        comboBoxSubCategory.SelectedIndex = index;
                    }
                }

                // 处理显示详情复选框
                if (checkBoxShowDetail.Checked != _settings.IsDetailVisible)
                    checkBoxShowDetail.Checked = _settings.IsDetailVisible;
            }
            finally
            {
                // 恢复布局更新
                ResumeLayout();
                // 恢复事件
                ResumeSettingsChangedEvents();
            }
        }

        private bool _settingsChangedEventsSuspended = false;
        private void SuspendSettingsChangedEvents()
        {
            _settingsChangedEventsSuspended = true;
        }

        private void ResumeSettingsChangedEvents()
        {
            _settingsChangedEventsSuspended = false;
        }

        #region ILearningView Implementation

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentContent
        {
            set
            {
                labelContent.Text = value;
                // 切换学习项时重置详情区状态
                ResetDetailState();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayText
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    UpdateDetailContent(value);
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
                }
            }
        }

        /// <summary>
        /// 重置详情区状态（切换学习项时调用）
        /// </summary>
        private void ResetDetailState()
        {
            _answerRevealed = false;

            if (_isQuizMode)
            {
                // 答题模式：隐藏详情区
                _isDetailVisible = false;
                listBoxDisplay.Visible = false;
                UpdateDetailContent("❓ 请猜测答案");
                buttonRevealAnswer.Visible = true;
                // 答题模式下重置复选框状态
                if (checkBoxShowDetail != null)
                {
                    checkBoxShowDetail.Checked = false;
                }
            }
            else
            {
                // 根据用户之前的选择恢复显示状态
                if (checkBoxShowDetail != null && checkBoxShowDetail.Checked)
                {
                    _isDetailVisible = true;
                    listBoxDisplay.Visible = true;
                    if (_currentItem != null)
                    {
                        UpdateDetailContent(_currentItem.GetDisplayText());
                    }
                }
                else
                {
                    _isDetailVisible = false;
                    listBoxDisplay.Visible = false;
                }
            }

            // 重置收藏状态
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
                string favoritesPath = Path.Combine(Paths.DataDirectory, "favorites.json");
                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    var favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                    string key = GetFavoriteKey();
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
            catch
            {
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

        public string Language => radioChinese.Checked ? Constants.Language.Chinese : Constants.Language.English;

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

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void EnableButtons(bool enabled)
        {
            buttonKnown.Enabled = enabled;
            buttonUnknown.Enabled = enabled;
            buttonNext.Enabled = enabled;
            buttonPronounce.Enabled = enabled;
        }

        public void PlayPronunciation(string text, string language)
        {
            // 委托给TTS服务播放发音
            _ttsService.SpeakAsync(text, language).ConfigureAwait(false);
        }


        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        // 必须在类内部定义这些控件变量（修复编译报错）
        private Panel panelContent;
        private ListBox listBoxDisplay;
        private Label labelContent;
        private Label labelStatistics;
        private Panel panelList;
        private ListBox listBoxItems;
        private Label labelListTitle;
        private Label labelListStatus;
        private Panel panelConfig;
        private Label labelConfigTitle;
        private GroupBox groupBoxMode;
        private RadioButton radioStudyMode;
        private RadioButton radioQuickMode;
        private GroupBox groupBoxSort;
        private RadioButton radioSequential;
        private RadioButton radioRandom;
        private GroupBox groupBoxLanguage;
        private RadioButton radioChinese;
        private RadioButton radioEnglish;
        private Label labelSubCategory;
        private ComboBox comboBoxSubCategory;
        private TableLayoutPanel mainTableLayoutPanel;
        private Panel middlePanel;
        private TableLayoutPanel middleTableLayoutPanel;
        private GroupBox groupBoxPronunciationScope;
        private Button buttonOpenStatistics;
        private Button buttonExportErrorBook;

        private Panel panelStats;
        private Label labelStudyTime;
        private Label labelScore;
        private Label labelTodayCount;
        private Label labelStreak;
        private Label labelEncouragement;
        private ProgressBar progressDailyGoal;
        private Label labelDailyGoal;
        private CheckBox checkBoxShowDetail;
        private Panel panelQuizMode;
        private Button buttonQuizMode;
        private Label labelQuizHint;
        private Button buttonThemeToggle;
        private Panel panelNotes;
        private RichTextBox richTextBoxNotes;
        private Label labelNotesTitle;

        private Label labelGameTitle;
        private bool _isGameActive = false;
        private Panel panelBadges;
        private Label labelBadgesTitle;
        private Button buttonMiniGame;
        private Button buttonGameSubmit;
        private Panel panelChallenges;
        private Label labelChallengesTitle;
        private FlowLayoutPanel flowLayoutPanelBadges;
        private Label labelLevel;
        private ProgressBar progressXP;
        private Label labelXP;
        private FlowLayoutPanel flowLayoutPanelChallenges;

        private Panel panelGame;
        private Label labelGameQuestion;
        private TextBox textBoxGameAnswer;
        private Label labelGameResult;
        private System.Windows.Forms.Timer _gameTimer;
        private int _gameScore = 0;

        private void InitializeComponent()
        {
            panelContent = new Panel();
            listBoxDisplay = new ListBox();
            labelContent = new Label();
            labelStatistics = new Label();
            panelList = new Panel();
            labelListStatus = new Label();
            labelListTitle = new Label();
            listBoxItems = new ListBox();
            panelConfig = new Panel();
            labelConfigTitle = new Label();
            groupBoxMode = new GroupBox();
            radioStudyMode = new RadioButton();
            radioQuickMode = new RadioButton();
            groupBoxSort = new GroupBox();
            radioSequential = new RadioButton();
            radioRandom = new RadioButton();
            groupBoxLanguage = new GroupBox();
            radioChinese = new RadioButton();
            radioEnglish = new RadioButton();
            labelSubCategory = new Label();
            comboBoxSubCategory = new ComboBox();
            buttonOpenStatistics = new Button();
            buttonExportErrorBook = new Button();
            panelStats = new Panel();
            labelStudyTime = new Label();
            labelScore = new Label();
            labelTodayCount = new Label();
            labelStreak = new Label();
            labelEncouragement = new Label();
            progressDailyGoal = new ProgressBar();
            labelDailyGoal = new Label();
            panelQuizMode = new Panel();
            buttonQuizMode = new Button();
            labelQuizHint = new Label();
            buttonThemeToggle = new Button();
            checkBoxShowDetail = new CheckBox();
            mainTableLayoutPanel = new TableLayoutPanel();
            middlePanel = new Panel();
            middleTableLayoutPanel = new TableLayoutPanel();
            panelNotes = new Panel();
            richTextBoxNotes = new RichTextBox();
            labelNotesTitle = new Label();
            buttonsFlowLayoutPanel = new FlowLayoutPanel();
            buttonKnown = new Button();
            buttonUnknown = new Button();
            buttonNext = new Button();
            buttonPronounce = new Button();
            buttonRevealAnswer = new Button();
            buttonFavorite = new Button();
            buttonNote = new Button();
            buttonExit = new Button();
            buttonAIAsk = new Button();
            progressBar1 = new ProgressBar();
            settingsFlowLayoutPanel = new FlowLayoutPanel();
            checkBoxVoice = new CheckBox();
            pronunciationFlowLayoutPanel = new FlowLayoutPanel();
            radioOriginal = new RadioButton();
            radioExplanation = new RadioButton();
            radioBoth = new RadioButton();
            groupBoxPronunciationScope = new GroupBox();
            labelGameTitle = new Label();
            panelBadges = new Panel();
            labelBadgesTitle = new Label();
            buttonMiniGame = new Button();
            buttonGameSubmit = new Button();
            panelChallenges = new Panel();
            labelChallengesTitle = new Label();
            flowLayoutPanelBadges = new FlowLayoutPanel();
            labelLevel = new Label();
            progressXP = new ProgressBar();
            labelXP = new Label();
            flowLayoutPanelChallenges = new FlowLayoutPanel();
            panelGame = new Panel();
            labelGameQuestion = new Label();
            textBoxGameAnswer = new TextBox();
            labelGameResult = new Label();
            panelContent.SuspendLayout();
            panelList.SuspendLayout();
            panelConfig.SuspendLayout();
            groupBoxMode.SuspendLayout();
            groupBoxSort.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            panelStats.SuspendLayout();
            panelQuizMode.SuspendLayout();
            mainTableLayoutPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            middleTableLayoutPanel.SuspendLayout();
            panelNotes.SuspendLayout();
            buttonsFlowLayoutPanel.SuspendLayout();
            settingsFlowLayoutPanel.SuspendLayout();
            pronunciationFlowLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.FromArgb(224, 224, 224);
            panelContent.Controls.Add(listBoxDisplay);
            panelContent.Controls.Add(labelContent);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(3, 3);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(1155, 636);
            panelContent.TabIndex = 0;
            panelContent.Paint += PanelContent_Paint;
            // 
            // listBoxDisplay
            // 
            listBoxDisplay.BackColor = Color.FromArgb(192, 255, 192);
            listBoxDisplay.Dock = DockStyle.Top;
            listBoxDisplay.Font = new Font("微软雅黑", 21.75F, FontStyle.Bold, GraphicsUnit.Point, 134);
            listBoxDisplay.ForeColor = Color.FromArgb(100, 150, 180);
            listBoxDisplay.Location = new Point(0, 0);
            listBoxDisplay.Name = "listBoxDisplay";
            listBoxDisplay.Size = new Size(1155, 160);
            listBoxDisplay.TabIndex = 1;
            listBoxDisplay.Visible = false;
            listBoxDisplay.Click += ListBoxDisplay_Click;
            // 
            // labelContent
            // 
            labelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelContent.BackColor = Color.FromArgb(255, 255, 192);
            labelContent.Font = new Font("微软雅黑", 48F, FontStyle.Bold, GraphicsUnit.Point, 134);
            labelContent.ForeColor = Color.FromArgb(70, 90, 110);
            labelContent.Location = new Point(0, 0);
            labelContent.Name = "labelContent";
            labelContent.Size = new Size(1155, 636);
            labelContent.TabIndex = 0;
            labelContent.TextAlign = ContentAlignment.MiddleCenter;
            labelContent.Click += LabelContent_Click;
            // 
            // labelStatistics
            // 
            labelStatistics.Dock = DockStyle.Fill;
            labelStatistics.Font = new Font("微软雅黑", 11F);
            labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            labelStatistics.Location = new Point(3, 806);
            labelStatistics.Name = "labelStatistics";
            labelStatistics.Size = new Size(1155, 32);
            labelStatistics.TabIndex = 3;
            // 
            // panelList
            // 
            panelList.BackColor = Color.FromArgb(248, 248, 252);
            panelList.BorderStyle = BorderStyle.FixedSingle;
            panelList.Controls.Add(labelListStatus);
            panelList.Controls.Add(labelListTitle);
            panelList.Controls.Add(listBoxItems);
            panelList.Dock = DockStyle.Fill;
            panelList.Location = new Point(3, 3);
            panelList.Name = "panelList";
            panelList.Size = new Size(194, 838);
            panelList.TabIndex = 18;
            // 
            // labelListStatus
            // 
            labelListStatus.BackColor = Color.FromArgb(240, 240, 245);
            labelListStatus.Dock = DockStyle.Bottom;
            labelListStatus.Font = new Font("微软雅黑", 9F);
            labelListStatus.ForeColor = Color.FromArgb(80, 100, 120);
            labelListStatus.Location = new Point(0, 796);
            labelListStatus.Name = "labelListStatus";
            labelListStatus.Size = new Size(192, 40);
            labelListStatus.TabIndex = 2;
            labelListStatus.Text = "共 0 项";
            labelListStatus.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelListTitle
            // 
            labelListTitle.BackColor = Color.FromArgb(66, 133, 244);
            labelListTitle.Dock = DockStyle.Top;
            labelListTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelListTitle.ForeColor = Color.White;
            labelListTitle.Location = new Point(0, 0);
            labelListTitle.Name = "labelListTitle";
            labelListTitle.Size = new Size(192, 35);
            labelListTitle.TabIndex = 0;
            labelListTitle.Text = "📚 学习列表";
            labelListTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // listBoxItems
            // 
            listBoxItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listBoxItems.Font = new Font("微软雅黑", 10F);
            listBoxItems.FormattingEnabled = true;
            listBoxItems.Location = new Point(0, 35);
            listBoxItems.Name = "listBoxItems";
            listBoxItems.Size = new Size(192, 764);
            listBoxItems.TabIndex = 1;
            listBoxItems.ItemHeight = 30;//此行不要删
            listBoxItems.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;
            // 
            // panelConfig
            // 
            panelConfig.BackColor = Color.FromArgb(245, 245, 250);
            panelConfig.BorderStyle = BorderStyle.FixedSingle;
            panelConfig.Controls.Add(labelConfigTitle);
            panelConfig.Controls.Add(groupBoxMode);
            panelConfig.Controls.Add(groupBoxSort);
            panelConfig.Controls.Add(groupBoxLanguage);
            panelConfig.Controls.Add(labelSubCategory);
            panelConfig.Controls.Add(comboBoxSubCategory);
            panelConfig.Controls.Add(buttonOpenStatistics);
            panelConfig.Controls.Add(buttonExportErrorBook);
            panelConfig.Controls.Add(panelStats);
            panelConfig.Controls.Add(panelQuizMode);
            panelConfig.Controls.Add(buttonThemeToggle);
            panelConfig.Dock = DockStyle.Fill;
            panelConfig.Location = new Point(1370, 3);
            panelConfig.Name = "panelConfig";
            panelConfig.Size = new Size(214, 838);
            panelConfig.TabIndex = 19;
            // 
            // labelConfigTitle
            // 
            labelConfigTitle.BackColor = Color.FromArgb(103, 58, 183);
            labelConfigTitle.Dock = DockStyle.Top;
            labelConfigTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelConfigTitle.ForeColor = Color.White;
            labelConfigTitle.Location = new Point(0, 0);
            labelConfigTitle.Name = "labelConfigTitle";
            labelConfigTitle.Size = new Size(212, 35);
            labelConfigTitle.TabIndex = 0;
            labelConfigTitle.Text = "⚙️ 设置";
            labelConfigTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBoxMode
            // 
            groupBoxMode.Controls.Add(radioStudyMode);
            groupBoxMode.Controls.Add(radioQuickMode);
            groupBoxMode.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            groupBoxMode.ForeColor = Color.FromArgb(60, 80, 100);
            groupBoxMode.Location = new Point(10, 45);
            groupBoxMode.Name = "groupBoxMode";
            groupBoxMode.Size = new Size(180, 68);
            groupBoxMode.TabIndex = 1;
            groupBoxMode.TabStop = false;
            groupBoxMode.Text = "学习模式";
            // 
            // radioStudyMode
            // 
            radioStudyMode.AutoSize = true;
            radioStudyMode.Checked = true;
            radioStudyMode.Font = new Font("微软雅黑", 9F);
            radioStudyMode.ForeColor = Color.FromArgb(70, 90, 110);
            radioStudyMode.Location = new Point(15, 25);
            radioStudyMode.Name = "radioStudyMode";
            radioStudyMode.Size = new Size(70, 21);
            radioStudyMode.TabIndex = 0;
            radioStudyMode.TabStop = true;
            radioStudyMode.Text = "📝 练习";
            radioStudyMode.CheckedChanged += RadioStudyMode_CheckedChanged;
            // 
            // radioQuickMode
            // 
            radioQuickMode.AutoSize = true;
            radioQuickMode.Font = new Font("微软雅黑", 9F);
            radioQuickMode.ForeColor = Color.FromArgb(70, 90, 110);
            radioQuickMode.Location = new Point(90, 25);
            radioQuickMode.Name = "radioQuickMode";
            radioQuickMode.Size = new Size(70, 21);
            radioQuickMode.TabIndex = 1;
            radioQuickMode.Text = "🔄 复习";
            radioQuickMode.CheckedChanged += RadioQuickMode_CheckedChanged;
            // 
            // groupBoxSort
            // 
            groupBoxSort.Controls.Add(radioSequential);
            groupBoxSort.Controls.Add(radioRandom);
            groupBoxSort.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            groupBoxSort.ForeColor = Color.FromArgb(60, 80, 100);
            groupBoxSort.Location = new Point(10, 130);
            groupBoxSort.Name = "groupBoxSort";
            groupBoxSort.Size = new Size(180, 75);
            groupBoxSort.TabIndex = 2;
            groupBoxSort.TabStop = false;
            groupBoxSort.Text = "排序方式";
            // 
            // radioSequential
            // 
            radioSequential.AutoSize = true;
            radioSequential.Checked = true;
            radioSequential.Font = new Font("微软雅黑", 9F);
            radioSequential.ForeColor = Color.FromArgb(70, 90, 110);
            radioSequential.Location = new Point(15, 25);
            radioSequential.Name = "radioSequential";
            radioSequential.Size = new Size(68, 21);
            radioSequential.TabIndex = 0;
            radioSequential.TabStop = true;
            radioSequential.Text = "📋 顺序";
            radioSequential.CheckedChanged += RadioSequential_CheckedChanged;
            // 
            // radioRandom
            // 
            radioRandom.AutoSize = true;
            radioRandom.Font = new Font("微软雅黑", 9F);
            radioRandom.ForeColor = Color.FromArgb(70, 90, 110);
            radioRandom.Location = new Point(90, 25);
            radioRandom.Name = "radioRandom";
            radioRandom.Size = new Size(69, 21);
            radioRandom.TabIndex = 1;
            radioRandom.Text = "🎲 随机";
            radioRandom.CheckedChanged += RadioRandom_CheckedChanged;
            // 
            // groupBoxLanguage
            // 
            groupBoxLanguage.Controls.Add(radioChinese);
            groupBoxLanguage.Controls.Add(radioEnglish);
            groupBoxLanguage.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            groupBoxLanguage.ForeColor = Color.FromArgb(60, 80, 100);
            groupBoxLanguage.Location = new Point(10, 215);
            groupBoxLanguage.Name = "groupBoxLanguage";
            groupBoxLanguage.Size = new Size(180, 75);
            groupBoxLanguage.TabIndex = 3;
            groupBoxLanguage.TabStop = false;
            groupBoxLanguage.Text = "语言";
            // 
            // radioChinese
            // 
            radioChinese.AutoSize = true;
            radioChinese.Checked = true;
            radioChinese.Font = new Font("微软雅黑", 9F);
            radioChinese.ForeColor = Color.FromArgb(70, 90, 110);
            radioChinese.Location = new Point(15, 25);
            radioChinese.Name = "radioChinese";
            radioChinese.Size = new Size(67, 21);
            radioChinese.TabIndex = 0;
            radioChinese.TabStop = true;
            radioChinese.Text = "🇨🇳 中文";
            radioChinese.CheckedChanged += RadioChinese_CheckedChanged;
            // 
            // radioEnglish
            // 
            radioEnglish.AutoSize = true;
            radioEnglish.Font = new Font("微软雅黑", 9F);
            radioEnglish.ForeColor = Color.FromArgb(70, 90, 110);
            radioEnglish.Location = new Point(90, 25);
            radioEnglish.Name = "radioEnglish";
            radioEnglish.Size = new Size(66, 21);
            radioEnglish.TabIndex = 1;
            radioEnglish.Text = "🇬🇧 英语";
            radioEnglish.CheckedChanged += RadioEnglish_CheckedChanged;
            // 
            // labelSubCategory
            // 
            labelSubCategory.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            labelSubCategory.ForeColor = Color.FromArgb(70, 90, 110);
            labelSubCategory.Location = new Point(10, 300);
            labelSubCategory.Name = "labelSubCategory";
            labelSubCategory.Size = new Size(50, 23);
            labelSubCategory.TabIndex = 4;
            labelSubCategory.Text = "📖";
            labelSubCategory.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // comboBoxSubCategory
            // 
            comboBoxSubCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxSubCategory.Font = new Font("微软雅黑", 9F);
            comboBoxSubCategory.FormattingEnabled = true;
            comboBoxSubCategory.Location = new Point(65, 297);
            comboBoxSubCategory.Name = "comboBoxSubCategory";
            comboBoxSubCategory.Size = new Size(125, 25);
            comboBoxSubCategory.TabIndex = 5;
            comboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            // 
            // buttonOpenStatistics
            // 
            buttonOpenStatistics.BackColor = Color.FromArgb(255, 152, 0);
            buttonOpenStatistics.FlatAppearance.BorderSize = 0;
            buttonOpenStatistics.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 142, 0);
            buttonOpenStatistics.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 162, 20);
            buttonOpenStatistics.FlatStyle = FlatStyle.Flat;
            buttonOpenStatistics.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonOpenStatistics.ForeColor = Color.White;
            buttonOpenStatistics.Location = new Point(10, 335);
            buttonOpenStatistics.Name = "buttonOpenStatistics";
            buttonOpenStatistics.Size = new Size(180, 40);
            buttonOpenStatistics.TabIndex = 6;
            buttonOpenStatistics.Text = "📊 学习统计";
            buttonOpenStatistics.UseVisualStyleBackColor = false;
            buttonOpenStatistics.Click += ButtonOpenStatistics_Click;
            // 
            // buttonExportErrorBook
            // 
            buttonExportErrorBook.BackColor = Color.FromArgb(244, 67, 54);
            buttonExportErrorBook.FlatAppearance.BorderSize = 0;
            buttonExportErrorBook.FlatAppearance.MouseDownBackColor = Color.FromArgb(234, 57, 44);
            buttonExportErrorBook.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 77, 64);
            buttonExportErrorBook.FlatStyle = FlatStyle.Flat;
            buttonExportErrorBook.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonExportErrorBook.ForeColor = Color.White;
            buttonExportErrorBook.Location = new Point(10, 385);
            buttonExportErrorBook.Name = "buttonExportErrorBook";
            buttonExportErrorBook.Size = new Size(180, 40);
            buttonExportErrorBook.TabIndex = 7;
            buttonExportErrorBook.Text = "❌ 导出错题本";
            buttonExportErrorBook.UseVisualStyleBackColor = false;
            buttonExportErrorBook.Click += ButtonExportErrorBook_Click;
            // 
            // panelStats
            // 
            panelStats.BackColor = Color.FromArgb(250, 251, 253);
            panelStats.Controls.Add(labelStudyTime);
            panelStats.Controls.Add(labelScore);
            panelStats.Controls.Add(labelTodayCount);
            panelStats.Controls.Add(labelStreak);
            panelStats.Controls.Add(labelEncouragement);
            panelStats.Controls.Add(progressDailyGoal);
            panelStats.Controls.Add(labelDailyGoal);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(0, 0);
            panelStats.Name = "panelStats";
            panelStats.Size = new Size(212, 836);
            panelStats.TabIndex = 8;
            // 
            // labelStudyTime
            // 
            labelStudyTime.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelStudyTime.ForeColor = Color.FromArgb(66, 133, 244);
            labelStudyTime.Location = new Point(12, 12);
            labelStudyTime.Name = "labelStudyTime";
            labelStudyTime.Size = new Size(188, 28);
            labelStudyTime.TabIndex = 0;
            labelStudyTime.Text = "⏱️ 学习时长: 00:00";
            // 
            // labelScore
            // 
            labelScore.Font = new Font("微软雅黑", 18F, FontStyle.Bold, GraphicsUnit.Point, 134);
            labelScore.ForeColor = Color.FromArgb(255, 152, 0);
            labelScore.Location = new Point(12, 437);
            labelScore.Name = "labelScore";
            labelScore.Size = new Size(188, 40);
            labelScore.TabIndex = 1;
            labelScore.Text = "🏆 得分: 0";
            labelScore.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // labelTodayCount
            // 
            labelTodayCount.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelTodayCount.ForeColor = Color.FromArgb(76, 175, 80);
            labelTodayCount.Location = new Point(12, 65);
            labelTodayCount.Name = "labelTodayCount";
            labelTodayCount.Size = new Size(188, 28);
            labelTodayCount.TabIndex = 2;
            labelTodayCount.Text = "📚 今日学习: 0 项";
            // 
            // labelStreak
            // 
            labelStreak.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelStreak.ForeColor = Color.FromArgb(156, 39, 176);
            labelStreak.Location = new Point(12, 90);
            labelStreak.Name = "labelStreak";
            labelStreak.Size = new Size(188, 28);
            labelStreak.TabIndex = 3;
            labelStreak.Text = "🔥 连续学习: 0 天";
            // 
            // labelEncouragement
            // 
            labelEncouragement.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            labelEncouragement.ForeColor = Color.FromArgb(76, 175, 80);
            labelEncouragement.Location = new Point(10, 493);
            labelEncouragement.Name = "labelEncouragement";
            labelEncouragement.Size = new Size(188, 65);
            labelEncouragement.TabIndex = 4;
            labelEncouragement.Text = "💪 加油！";
            labelEncouragement.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // progressDailyGoal
            // 
            progressDailyGoal.Location = new Point(10, 145);
            progressDailyGoal.Maximum = 50;
            progressDailyGoal.Name = "progressDailyGoal";
            progressDailyGoal.Size = new Size(160, 15);
            progressDailyGoal.TabIndex = 5;
            // 
            // labelDailyGoal
            // 
            labelDailyGoal.Font = new Font("微软雅黑", 8F);
            labelDailyGoal.ForeColor = Color.FromArgb(120, 120, 120);
            labelDailyGoal.Location = new Point(10, 165);
            labelDailyGoal.Name = "labelDailyGoal";
            labelDailyGoal.Size = new Size(160, 20);
            labelDailyGoal.TabIndex = 6;
            labelDailyGoal.Text = "今日目标: 0/50";
            // 
            // panelQuizMode
            // 
            panelQuizMode.BackColor = Color.FromArgb(255, 248, 220);
            panelQuizMode.BorderStyle = BorderStyle.FixedSingle;
            panelQuizMode.Controls.Add(buttonQuizMode);
            panelQuizMode.Controls.Add(labelQuizHint);
            panelQuizMode.Location = new Point(10, 660);
            panelQuizMode.Name = "panelQuizMode";
            panelQuizMode.Size = new Size(180, 80);
            panelQuizMode.TabIndex = 9;
            // 
            // buttonQuizMode
            // 
            buttonQuizMode.BackColor = Color.FromArgb(255, 193, 7);
            buttonQuizMode.FlatAppearance.BorderSize = 0;
            buttonQuizMode.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 183, 0);
            buttonQuizMode.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 203, 27);
            buttonQuizMode.FlatStyle = FlatStyle.Flat;
            buttonQuizMode.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonQuizMode.ForeColor = Color.White;
            buttonQuizMode.Location = new Point(10, 10);
            buttonQuizMode.Name = "buttonQuizMode";
            buttonQuizMode.Size = new Size(160, 35);
            buttonQuizMode.TabIndex = 0;
            buttonQuizMode.Text = "🎮 答题模式";
            buttonQuizMode.UseVisualStyleBackColor = false;
            buttonQuizMode.Click += ButtonQuizMode_Click;
            // 
            // labelQuizHint
            // 
            labelQuizHint.Font = new Font("微软雅黑", 8.5F);
            labelQuizHint.ForeColor = Color.FromArgb(139, 119, 101);
            labelQuizHint.Location = new Point(10, 50);
            labelQuizHint.Name = "labelQuizHint";
            labelQuizHint.Size = new Size(160, 25);
            labelQuizHint.TabIndex = 1;
            labelQuizHint.Text = "先隐藏答案，测试自己";
            // 
            // buttonThemeToggle
            // 
            buttonThemeToggle.BackColor = Color.FromArgb(103, 58, 183);
            buttonThemeToggle.FlatAppearance.BorderSize = 0;
            buttonThemeToggle.FlatAppearance.MouseDownBackColor = Color.FromArgb(93, 48, 173);
            buttonThemeToggle.FlatAppearance.MouseOverBackColor = Color.FromArgb(113, 68, 193);
            buttonThemeToggle.FlatStyle = FlatStyle.Flat;
            buttonThemeToggle.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonThemeToggle.ForeColor = Color.White;
            buttonThemeToggle.Location = new Point(10, 760);
            buttonThemeToggle.Name = "buttonThemeToggle";
            buttonThemeToggle.Size = new Size(180, 40);
            buttonThemeToggle.TabIndex = 10;
            buttonThemeToggle.Text = "🌙 深色模式";
            buttonThemeToggle.UseVisualStyleBackColor = false;
            buttonThemeToggle.Click += ButtonThemeToggle_Click;
            // 
            // checkBoxShowDetail
            // 
            checkBoxShowDetail.BackColor = Color.FromArgb(250, 245, 235);
            checkBoxShowDetail.FlatAppearance.BorderSize = 0;
            checkBoxShowDetail.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 165, 70);
            checkBoxShowDetail.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            checkBoxShowDetail.FlatStyle = FlatStyle.Flat;
            checkBoxShowDetail.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            checkBoxShowDetail.ForeColor = Color.Black;
            checkBoxShowDetail.Location = new Point(333, 8);
            checkBoxShowDetail.Name = "checkBoxShowDetail";
            checkBoxShowDetail.Size = new Size(132, 27);
            checkBoxShowDetail.TabIndex = 10;
            checkBoxShowDetail.Text = "📋 显示详情";
            checkBoxShowDetail.TextAlign = ContentAlignment.MiddleCenter;
            checkBoxShowDetail.UseVisualStyleBackColor = false;
            checkBoxShowDetail.CheckedChanged += CheckBoxShowDetail_CheckedChanged;
            // 
            // mainTableLayoutPanel
            // 
            mainTableLayoutPanel.ColumnCount = 3;
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            mainTableLayoutPanel.Controls.Add(panelList, 0, 0);
            mainTableLayoutPanel.Controls.Add(middlePanel, 1, 0);
            mainTableLayoutPanel.Controls.Add(panelConfig, 2, 0);
            mainTableLayoutPanel.Dock = DockStyle.Fill;
            mainTableLayoutPanel.Location = new Point(0, 0);
            mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            mainTableLayoutPanel.RowCount = 1;
            mainTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainTableLayoutPanel.Size = new Size(1587, 844);
            mainTableLayoutPanel.TabIndex = 0;
            mainTableLayoutPanel.Paint += mainTableLayoutPanel_Paint;
            // 
            // middlePanel
            // 
            middlePanel.BackColor = Color.FromArgb(250, 245, 235);
            middlePanel.Controls.Add(middleTableLayoutPanel);
            middlePanel.Dock = DockStyle.Fill;
            middlePanel.Location = new Point(203, 3);
            middlePanel.Name = "middlePanel";
            middlePanel.Size = new Size(1161, 838);
            middlePanel.TabIndex = 20;
            // 
            // middleTableLayoutPanel
            // 
            middleTableLayoutPanel.ColumnCount = 1;
            middleTableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.Controls.Add(panelContent, 0, 0);
            middleTableLayoutPanel.Controls.Add(panelNotes, 0, 1);
            middleTableLayoutPanel.Controls.Add(buttonsFlowLayoutPanel, 0, 2);
            middleTableLayoutPanel.Controls.Add(progressBar1, 0, 3);
            middleTableLayoutPanel.Controls.Add(settingsFlowLayoutPanel, 0, 4);
            middleTableLayoutPanel.Controls.Add(labelStatistics, 0, 5);
            middleTableLayoutPanel.Dock = DockStyle.Fill;
            middleTableLayoutPanel.Location = new Point(0, 0);
            middleTableLayoutPanel.Name = "middleTableLayoutPanel";
            middleTableLayoutPanel.RowCount = 6;
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 0F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 71F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 51F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
            middleTableLayoutPanel.Size = new Size(1161, 838);
            middleTableLayoutPanel.TabIndex = 0;
            // 
            // panelNotes
            // 
            panelNotes.BackColor = Color.FromArgb(255, 253, 238);
            panelNotes.BorderStyle = BorderStyle.FixedSingle;
            panelNotes.Controls.Add(richTextBoxNotes);
            panelNotes.Controls.Add(labelNotesTitle);
            panelNotes.Dock = DockStyle.Fill;
            panelNotes.Location = new Point(3, 645);
            panelNotes.Name = "panelNotes";
            panelNotes.Size = new Size(1155, 1);
            panelNotes.TabIndex = 7;
            panelNotes.Visible = false;
            // 
            // richTextBoxNotes
            // 
            richTextBoxNotes.BackColor = Color.FromArgb(255, 253, 238);
            richTextBoxNotes.Dock = DockStyle.Fill;
            richTextBoxNotes.Font = new Font("微软雅黑", 11F);
            richTextBoxNotes.ForeColor = Color.FromArgb(60, 80, 100);
            richTextBoxNotes.Location = new Point(0, 30);
            richTextBoxNotes.Name = "richTextBoxNotes";
            richTextBoxNotes.Size = new Size(1153, 0);
            richTextBoxNotes.TabIndex = 1;
            richTextBoxNotes.Text = "";
            richTextBoxNotes.TextChanged += RichTextBoxNotes_TextChanged;
            // 
            // labelNotesTitle
            // 
            labelNotesTitle.Dock = DockStyle.Top;
            labelNotesTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelNotesTitle.ForeColor = Color.FromArgb(139, 119, 101);
            labelNotesTitle.Location = new Point(0, 0);
            labelNotesTitle.Name = "labelNotesTitle";
            labelNotesTitle.Padding = new Padding(10, 0, 0, 0);
            labelNotesTitle.Size = new Size(1153, 30);
            labelNotesTitle.TabIndex = 0;
            labelNotesTitle.Text = "📝 我的笔记";
            labelNotesTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonsFlowLayoutPanel
            // 
            buttonsFlowLayoutPanel.Controls.Add(buttonKnown);
            buttonsFlowLayoutPanel.Controls.Add(buttonUnknown);
            buttonsFlowLayoutPanel.Controls.Add(buttonNext);
            buttonsFlowLayoutPanel.Controls.Add(buttonPronounce);
            buttonsFlowLayoutPanel.Controls.Add(buttonRevealAnswer);
            buttonsFlowLayoutPanel.Controls.Add(buttonFavorite);
            buttonsFlowLayoutPanel.Controls.Add(buttonNote);
            buttonsFlowLayoutPanel.Controls.Add(buttonExit);
            buttonsFlowLayoutPanel.Controls.Add(buttonAIAsk);
            buttonsFlowLayoutPanel.Dock = DockStyle.Fill;
            buttonsFlowLayoutPanel.Location = new Point(3, 645);
            buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            buttonsFlowLayoutPanel.Padding = new Padding(10, 5, 10, 5);
            buttonsFlowLayoutPanel.Size = new Size(1155, 65);
            buttonsFlowLayoutPanel.TabIndex = 4;
            buttonsFlowLayoutPanel.WrapContents = false;
            // 
            // buttonKnown
            // 
            buttonKnown.BackColor = Color.FromArgb(76, 175, 80);
            buttonKnown.FlatAppearance.BorderSize = 0;
            buttonKnown.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 155, 70);
            buttonKnown.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            buttonKnown.FlatStyle = FlatStyle.Flat;
            buttonKnown.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonKnown.ForeColor = Color.White;
            buttonKnown.Location = new Point(15, 10);
            buttonKnown.Margin = new Padding(5);
            buttonKnown.Name = "buttonKnown";
            buttonKnown.Size = new Size(130, 45);
            buttonKnown.TabIndex = 4;
            buttonKnown.Text = "✅ 会了 [K/1]";
            buttonKnown.UseVisualStyleBackColor = false;
            buttonKnown.Click += ButtonKnown_Click;
            // 
            // buttonUnknown
            // 
            buttonUnknown.BackColor = Color.FromArgb(244, 67, 54);
            buttonUnknown.FlatAppearance.BorderSize = 0;
            buttonUnknown.FlatAppearance.MouseDownBackColor = Color.FromArgb(234, 57, 44);
            buttonUnknown.FlatAppearance.MouseOverBackColor = Color.FromArgb(254, 77, 64);
            buttonUnknown.FlatStyle = FlatStyle.Flat;
            buttonUnknown.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonUnknown.ForeColor = Color.White;
            buttonUnknown.Location = new Point(155, 10);
            buttonUnknown.Margin = new Padding(5);
            buttonUnknown.Name = "buttonUnknown";
            buttonUnknown.Size = new Size(130, 45);
            buttonUnknown.TabIndex = 5;
            buttonUnknown.Text = "❌ 不会 [U/2]";
            buttonUnknown.UseVisualStyleBackColor = false;
            buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // buttonNext
            // 
            buttonNext.BackColor = Color.FromArgb(33, 150, 243);
            buttonNext.FlatAppearance.BorderSize = 0;
            buttonNext.FlatAppearance.MouseDownBackColor = Color.FromArgb(23, 140, 233);
            buttonNext.FlatAppearance.MouseOverBackColor = Color.FromArgb(43, 160, 253);
            buttonNext.FlatStyle = FlatStyle.Flat;
            buttonNext.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonNext.ForeColor = Color.White;
            buttonNext.Location = new Point(295, 10);
            buttonNext.Margin = new Padding(5);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(150, 45);
            buttonNext.TabIndex = 6;
            buttonNext.Text = "➡ 下一个 [Enter]";
            buttonNext.UseVisualStyleBackColor = false;
            buttonNext.Click += ButtonNext_Click;
            // 
            // buttonPronounce
            // 
            buttonPronounce.BackColor = Color.FromArgb(0, 188, 212);
            buttonPronounce.FlatAppearance.BorderSize = 0;
            buttonPronounce.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 178, 202);
            buttonPronounce.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 198, 222);
            buttonPronounce.FlatStyle = FlatStyle.Flat;
            buttonPronounce.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonPronounce.ForeColor = Color.White;
            buttonPronounce.Location = new Point(455, 10);
            buttonPronounce.Margin = new Padding(5);
            buttonPronounce.Name = "buttonPronounce";
            buttonPronounce.Size = new Size(140, 45);
            buttonPronounce.TabIndex = 7;
            buttonPronounce.Text = "🔊 发音 [Space]";
            buttonPronounce.UseVisualStyleBackColor = false;
            buttonPronounce.Click += ButtonPronounce_Click;
            // 
            // buttonRevealAnswer
            // 
            buttonRevealAnswer.BackColor = Color.FromArgb(66, 133, 244);
            buttonRevealAnswer.FlatAppearance.BorderSize = 0;
            buttonRevealAnswer.FlatAppearance.MouseDownBackColor = Color.FromArgb(56, 123, 234);
            buttonRevealAnswer.FlatAppearance.MouseOverBackColor = Color.FromArgb(76, 143, 254);
            buttonRevealAnswer.FlatStyle = FlatStyle.Flat;
            buttonRevealAnswer.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonRevealAnswer.ForeColor = Color.White;
            buttonRevealAnswer.Location = new Point(605, 10);
            buttonRevealAnswer.Margin = new Padding(5);
            buttonRevealAnswer.Name = "buttonRevealAnswer";
            buttonRevealAnswer.Size = new Size(105, 45);
            buttonRevealAnswer.TabIndex = 11;
            buttonRevealAnswer.Text = "👁️ 显示答案";
            buttonRevealAnswer.UseVisualStyleBackColor = false;
            buttonRevealAnswer.Visible = false;
            buttonRevealAnswer.Click += ButtonRevealAnswer_Click;
            // 
            // buttonFavorite
            // 
            buttonFavorite.BackColor = Color.FromArgb(255, 193, 7);
            buttonFavorite.FlatAppearance.BorderSize = 0;
            buttonFavorite.FlatAppearance.MouseDownBackColor = Color.FromArgb(245, 183, 0);
            buttonFavorite.FlatAppearance.MouseOverBackColor = Color.FromArgb(255, 203, 27);
            buttonFavorite.FlatStyle = FlatStyle.Flat;
            buttonFavorite.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonFavorite.ForeColor = Color.White;
            buttonFavorite.Location = new Point(720, 10);
            buttonFavorite.Margin = new Padding(5);
            buttonFavorite.Name = "buttonFavorite";
            buttonFavorite.Size = new Size(105, 45);
            buttonFavorite.TabIndex = 12;
            buttonFavorite.Text = "⭐ 收藏";
            buttonFavorite.UseVisualStyleBackColor = false;
            buttonFavorite.Click += ButtonFavorite_Click;
            // 
            // buttonNote
            // 
            buttonNote.BackColor = Color.FromArgb(76, 175, 80);
            buttonNote.FlatAppearance.BorderSize = 0;
            buttonNote.FlatAppearance.MouseDownBackColor = Color.FromArgb(66, 155, 70);
            buttonNote.FlatAppearance.MouseOverBackColor = Color.FromArgb(86, 185, 90);
            buttonNote.FlatStyle = FlatStyle.Flat;
            buttonNote.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonNote.ForeColor = Color.White;
            buttonNote.Location = new Point(835, 10);
            buttonNote.Margin = new Padding(5);
            buttonNote.Name = "buttonNote";
            buttonNote.Size = new Size(105, 45);
            buttonNote.TabIndex = 13;
            buttonNote.Text = "📝 笔记";
            buttonNote.UseVisualStyleBackColor = false;
            buttonNote.Click += ButtonNote_Click;
            // 
            // buttonExit
            // 
            buttonExit.BackColor = Color.FromArgb(108, 117, 125);
            buttonExit.FlatAppearance.BorderSize = 0;
            buttonExit.FlatAppearance.MouseDownBackColor = Color.FromArgb(98, 107, 115);
            buttonExit.FlatAppearance.MouseOverBackColor = Color.FromArgb(118, 127, 135);
            buttonExit.FlatStyle = FlatStyle.Flat;
            buttonExit.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonExit.ForeColor = Color.White;
            buttonExit.Location = new Point(950, 10);
            buttonExit.Margin = new Padding(5);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(105, 45);
            buttonExit.TabIndex = 8;
            buttonExit.Text = "🏠 返回";
            buttonExit.UseVisualStyleBackColor = false;
            buttonExit.Click += ButtonExit_Click;
            // 
            // buttonAIAsk
            // 
            buttonAIAsk.BackColor = Color.FromArgb(0, 120, 215);
            buttonAIAsk.FlatAppearance.BorderSize = 0;
            buttonAIAsk.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 110, 205);
            buttonAIAsk.FlatAppearance.MouseOverBackColor = Color.FromArgb(10, 130, 225);
            buttonAIAsk.FlatStyle = FlatStyle.Flat;
            buttonAIAsk.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonAIAsk.ForeColor = Color.White;
            buttonAIAsk.Location = new Point(1065, 10);
            buttonAIAsk.Margin = new Padding(5);
            buttonAIAsk.Name = "buttonAIAsk";
            buttonAIAsk.Size = new Size(105, 45);
            buttonAIAsk.TabIndex = 14;
            buttonAIAsk.Text = "🤖 AI问答";
            buttonAIAsk.UseVisualStyleBackColor = false;
            buttonAIAsk.Click += ButtonAIAsk_Click;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = Color.FromArgb(240, 240, 240);
            progressBar1.Dock = DockStyle.Fill;
            progressBar1.ForeColor = Color.FromArgb(255, 140, 0);
            progressBar1.Location = new Point(3, 716);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1155, 36);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 2;
            // 
            // settingsFlowLayoutPanel
            // 
            settingsFlowLayoutPanel.Controls.Add(checkBoxVoice);
            settingsFlowLayoutPanel.Controls.Add(pronunciationFlowLayoutPanel);
            settingsFlowLayoutPanel.Controls.Add(checkBoxShowDetail);
            settingsFlowLayoutPanel.Dock = DockStyle.Fill;
            settingsFlowLayoutPanel.Location = new Point(3, 758);
            settingsFlowLayoutPanel.Name = "settingsFlowLayoutPanel";
            settingsFlowLayoutPanel.Padding = new Padding(10, 5, 10, 5);
            settingsFlowLayoutPanel.Size = new Size(1155, 45);
            settingsFlowLayoutPanel.TabIndex = 5;
            settingsFlowLayoutPanel.WrapContents = false;
            // 
            // checkBoxVoice
            // 
            checkBoxVoice.Checked = true;
            checkBoxVoice.CheckState = CheckState.Checked;
            checkBoxVoice.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            checkBoxVoice.ForeColor = Color.FromArgb(70, 90, 110);
            checkBoxVoice.Location = new Point(15, 10);
            checkBoxVoice.Margin = new Padding(5);
            checkBoxVoice.Name = "checkBoxVoice";
            checkBoxVoice.Size = new Size(85, 25);
            checkBoxVoice.TabIndex = 9;
            checkBoxVoice.Text = "自动朗读";
            // 
            // pronunciationFlowLayoutPanel
            // 
            pronunciationFlowLayoutPanel.Controls.Add(radioOriginal);
            pronunciationFlowLayoutPanel.Controls.Add(radioExplanation);
            pronunciationFlowLayoutPanel.Controls.Add(radioBoth);
            pronunciationFlowLayoutPanel.Location = new Point(108, 8);
            pronunciationFlowLayoutPanel.Name = "pronunciationFlowLayoutPanel";
            pronunciationFlowLayoutPanel.Size = new Size(219, 27);
            pronunciationFlowLayoutPanel.TabIndex = 0;
            pronunciationFlowLayoutPanel.WrapContents = false;
            // 
            // radioOriginal
            // 
            radioOriginal.AutoSize = true;
            radioOriginal.Checked = true;
            radioOriginal.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            radioOriginal.ForeColor = Color.FromArgb(70, 90, 110);
            radioOriginal.Location = new Point(3, 3);
            radioOriginal.Name = "radioOriginal";
            radioOriginal.Size = new Size(50, 21);
            radioOriginal.TabIndex = 13;
            radioOriginal.TabStop = true;
            radioOriginal.Text = "原文";
            // 
            // radioExplanation
            // 
            radioExplanation.AutoSize = true;
            radioExplanation.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            radioExplanation.ForeColor = Color.FromArgb(70, 90, 110);
            radioExplanation.Location = new Point(59, 3);
            radioExplanation.Name = "radioExplanation";
            radioExplanation.Size = new Size(50, 21);
            radioExplanation.TabIndex = 14;
            radioExplanation.Text = "释义";
            // 
            // radioBoth
            // 
            radioBoth.AutoSize = true;
            radioBoth.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            radioBoth.ForeColor = Color.FromArgb(70, 90, 110);
            radioBoth.Location = new Point(115, 3);
            radioBoth.Name = "radioBoth";
            radioBoth.Size = new Size(83, 21);
            radioBoth.TabIndex = 15;
            radioBoth.Text = "原文+释义";
            // 
            // groupBoxPronunciationScope
            // 
            groupBoxPronunciationScope.Location = new Point(0, 0);
            groupBoxPronunciationScope.Name = "groupBoxPronunciationScope";
            groupBoxPronunciationScope.Size = new Size(200, 100);
            groupBoxPronunciationScope.TabIndex = 0;
            groupBoxPronunciationScope.TabStop = false;
            // 
            // labelGameTitle
            // 
            labelGameTitle.Location = new Point(0, 0);
            labelGameTitle.Name = "labelGameTitle";
            labelGameTitle.Size = new Size(100, 23);
            labelGameTitle.TabIndex = 0;
            // 
            // panelBadges
            // 
            panelBadges.Location = new Point(0, 0);
            panelBadges.Name = "panelBadges";
            panelBadges.Size = new Size(200, 100);
            panelBadges.TabIndex = 0;
            // 
            // labelBadgesTitle
            // 
            labelBadgesTitle.Location = new Point(0, 0);
            labelBadgesTitle.Name = "labelBadgesTitle";
            labelBadgesTitle.Size = new Size(100, 23);
            labelBadgesTitle.TabIndex = 0;
            // 
            // buttonMiniGame
            // 
            buttonMiniGame.Location = new Point(0, 0);
            buttonMiniGame.Name = "buttonMiniGame";
            buttonMiniGame.Size = new Size(75, 23);
            buttonMiniGame.TabIndex = 0;
            // 
            // buttonGameSubmit
            // 
            buttonGameSubmit.Location = new Point(0, 0);
            buttonGameSubmit.Name = "buttonGameSubmit";
            buttonGameSubmit.Size = new Size(75, 23);
            buttonGameSubmit.TabIndex = 0;
            // 
            // panelChallenges
            // 
            panelChallenges.Location = new Point(0, 0);
            panelChallenges.Name = "panelChallenges";
            panelChallenges.Size = new Size(200, 100);
            panelChallenges.TabIndex = 0;
            // 
            // labelChallengesTitle
            // 
            labelChallengesTitle.Location = new Point(0, 0);
            labelChallengesTitle.Name = "labelChallengesTitle";
            labelChallengesTitle.Size = new Size(100, 23);
            labelChallengesTitle.TabIndex = 0;
            // 
            // flowLayoutPanelBadges
            // 
            flowLayoutPanelBadges.Location = new Point(0, 0);
            flowLayoutPanelBadges.Name = "flowLayoutPanelBadges";
            flowLayoutPanelBadges.Size = new Size(200, 100);
            flowLayoutPanelBadges.TabIndex = 0;
            // 
            // labelLevel
            // 
            labelLevel.Location = new Point(0, 0);
            labelLevel.Name = "labelLevel";
            labelLevel.Size = new Size(100, 23);
            labelLevel.TabIndex = 0;
            // 
            // progressXP
            // 
            progressXP.Location = new Point(0, 0);
            progressXP.Name = "progressXP";
            progressXP.Size = new Size(100, 23);
            progressXP.TabIndex = 0;
            // 
            // labelXP
            // 
            labelXP.Location = new Point(0, 0);
            labelXP.Name = "labelXP";
            labelXP.Size = new Size(100, 23);
            labelXP.TabIndex = 0;
            // 
            // flowLayoutPanelChallenges
            // 
            flowLayoutPanelChallenges.Location = new Point(0, 0);
            flowLayoutPanelChallenges.Name = "flowLayoutPanelChallenges";
            flowLayoutPanelChallenges.Size = new Size(200, 100);
            flowLayoutPanelChallenges.TabIndex = 0;
            // 
            // panelGame
            // 
            panelGame.Location = new Point(0, 0);
            panelGame.Name = "panelGame";
            panelGame.Size = new Size(200, 100);
            panelGame.TabIndex = 0;
            // 
            // labelGameQuestion
            // 
            labelGameQuestion.Location = new Point(0, 0);
            labelGameQuestion.Name = "labelGameQuestion";
            labelGameQuestion.Size = new Size(100, 23);
            labelGameQuestion.TabIndex = 0;
            // 
            // textBoxGameAnswer
            // 
            textBoxGameAnswer.Location = new Point(0, 0);
            textBoxGameAnswer.Name = "textBoxGameAnswer";
            textBoxGameAnswer.Size = new Size(100, 23);
            textBoxGameAnswer.TabIndex = 0;
            // 
            // labelGameResult
            // 
            labelGameResult.Location = new Point(0, 0);
            labelGameResult.Name = "labelGameResult";
            labelGameResult.Size = new Size(100, 23);
            labelGameResult.TabIndex = 0;
            // 
            // LearningForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(250, 245, 235);
            ClientSize = new Size(1587, 844);
            Controls.Add(mainTableLayoutPanel);
            DoubleBuffered = true;
            MinimumSize = new Size(800, 600);
            Name = "LearningForm";
            Text = "✨ 学习模式 ✨";
            TransparencyKey = Color.FromArgb(255, 0, 255);
            panelContent.ResumeLayout(false);
            panelList.ResumeLayout(false);
            panelConfig.ResumeLayout(false);
            groupBoxMode.ResumeLayout(false);
            groupBoxMode.PerformLayout();
            groupBoxSort.ResumeLayout(false);
            groupBoxSort.PerformLayout();
            groupBoxLanguage.ResumeLayout(false);
            groupBoxLanguage.PerformLayout();
            panelStats.ResumeLayout(false);
            panelQuizMode.ResumeLayout(false);
            mainTableLayoutPanel.ResumeLayout(false);
            middlePanel.ResumeLayout(false);
            middleTableLayoutPanel.ResumeLayout(false);
            panelNotes.ResumeLayout(false);
            buttonsFlowLayoutPanel.ResumeLayout(false);
            settingsFlowLayoutPanel.ResumeLayout(false);
            pronunciationFlowLayoutPanel.ResumeLayout(false);
            pronunciationFlowLayoutPanel.PerformLayout();
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

            LoadStudyStats();
            LoadBadges();
            LoadChallenges();
            UpdateEncouragement();
            UpdateLevelDisplay();
            UpdateBadgesDisplay();
            UpdateChallengesDisplay();
        }

        /// <summary>
        /// 加载学习统计数据
        /// </summary>
        private void LoadStudyStats()
        {
            try
            {
                string statsPath = Path.Combine(Paths.DataDirectory, "study_stats.json");
                if (File.Exists(statsPath))
                {
                    string json = File.ReadAllText(statsPath);
                    var stats = JsonSerializer.Deserialize<StudyStats>(json);
                    if (stats != null)
                    {
                        _todayLearnedCount = stats.TodayLearnedCount;
                        _streakDays = stats.StreakDays;
                        _score = stats.TotalScore;
                        _totalLearnedCount = stats.TotalLearnedCount;
                        _quizCorrectCount = stats.QuizCorrectCount;
                        _favoriteCount = stats.FavoriteCount;
                        _noteCount = stats.NoteCount;
                        _xp = stats.XP;
                        _currentLevel = stats.CurrentLevel;
                        _levelTitle = _currentLevel < _levelTitles.Count ? _levelTitles[_currentLevel] : "圣人";
                        _xpToNextLevel = (_currentLevel + 1) * 100;

                        if (stats.LastStudyDate != DateTime.Today.Date)
                        {
                            _todayLearnedCount = 0;
                            if (stats.LastStudyDate == DateTime.Today.Date.AddDays(-1))
                            {
                                _streakDays++;
                            }
                            else if (stats.LastStudyDate < DateTime.Today.Date.AddDays(-1))
                            {
                                _streakDays = 1;
                            }
                        }
                    }
                }
                else
                {
                    _streakDays = 1;
                }
            }
            catch
            {
                _streakDays = 1;
            }

            UpdateStatsDisplay();
        }

        /// <summary>
        /// 保存学习统计数据到文件
        /// </summary>
        private void SaveStudyStats()
        {
            try
            {
                var stats = new StudyStats
                {
                    TodayLearnedCount = _todayLearnedCount,
                    StreakDays = _streakDays,
                    TotalScore = _score,
                    LastStudyDate = DateTime.Today.Date,
                    TotalLearnedCount = _totalLearnedCount,
                    QuizCorrectCount = _quizCorrectCount,
                    FavoriteCount = _favoriteCount,
                    NoteCount = _noteCount,
                    XP = _xp,
                    CurrentLevel = _currentLevel
                };

                string statsPath = Path.Combine(Paths.DataDirectory, "study_stats.json");
                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(statsPath, json);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 从文件加载已解锁的徽章
        /// </summary>
        private void LoadBadges()
        {
            try
            {
                string badgesPath = Path.Combine(Paths.DataDirectory, "badges.json");
                if (File.Exists(badgesPath))
                {
                    string json = File.ReadAllText(badgesPath);
                    _unlockedBadges = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                    foreach (var badgeId in _unlockedBadges)
                    {
                        if (_badges.TryGetValue(badgeId, out var badge))
                        {
                            badge.Unlocked = true;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 保存已解锁的徽章到文件
        /// </summary>
        private void SaveBadges()
        {
            try
            {
                string badgesPath = Path.Combine(Paths.DataDirectory, "badges.json");
                string json = JsonSerializer.Serialize(_unlockedBadges, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(badgesPath, json);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 检查并解锁达成的徽章
        /// </summary>
        private void CheckBadgeUnlock()
        {
            List<string> newlyUnlocked = new List<string>();

            TryUnlockBadge("first_blood", _totalLearnedCount >= 1, newlyUnlocked);
            TryUnlockBadge("streak_3", _streakDays >= 3, newlyUnlocked);
            TryUnlockBadge("streak_7", _streakDays >= 7, newlyUnlocked);
            TryUnlockBadge("streak_30", _streakDays >= 30, newlyUnlocked);
            TryUnlockBadge("learn_100", _totalLearnedCount >= 100, newlyUnlocked);
            TryUnlockBadge("learn_500", _totalLearnedCount >= 500, newlyUnlocked);
            TryUnlockBadge("learn_1000", _totalLearnedCount >= 1000, newlyUnlocked);
            TryUnlockBadge("perfect_day", _todayLearnedCount >= 50, newlyUnlocked);
            TryUnlockBadge("quiz_master", _quizCorrectCount >= 20, newlyUnlocked);
            TryUnlockBadge("favorite_collector", _favoriteCount >= 20, newlyUnlocked);
            TryUnlockBadge("note_taker", _noteCount >= 10, newlyUnlocked);

            if (newlyUnlocked.Count > 0)
            {
                SaveBadges();
                UpdateBadgesDisplay();
                ShowBadgeNotification(newlyUnlocked);
            }
        }

        /// <summary>
        /// 尝试解锁徽章的安全方法
        /// </summary>
        /// <param name="badgeId">徽章ID</param>
        /// <param name="condition">解锁条件</param>
        /// <param name="newlyUnlocked">新解锁的徽章列表</param>
        private void TryUnlockBadge(string badgeId, bool condition, List<string> newlyUnlocked)
        {
            if (condition && _badges.TryGetValue(badgeId, out var badge) && !badge.Unlocked)
            {
                UnlockBadge(badgeId, newlyUnlocked);
            }
        }

        /// <summary>
        /// 解锁指定徽章并记录
        /// </summary>
        /// <param name="badgeId">徽章ID</param>
        /// <param name="newlyUnlocked">新解锁的徽章列表</param>
        private void UnlockBadge(string badgeId, List<string> newlyUnlocked)
        {
            if (_badges.TryGetValue(badgeId, out var badge))
            {
                badge.Unlocked = true;
                _unlockedBadges.Add(badgeId);
                newlyUnlocked.Add(badgeId);
                _score += 50;
                _xp += 50;
                CheckLevelUp();
            }
        }

        /// <summary>
        /// 显示徽章解锁通知
        /// </summary>
        /// <param name="badges">已解锁的徽章ID列表</param>
        private void ShowBadgeNotification(List<string> badges)
        {
            string message = "🎉 解锁成就！\n\n";
            foreach (var badgeId in badges)
            {
                if (_badges.TryGetValue(badgeId, out var badge))
                {
                    message += $"{badge.Emoji} {badge.Name}\n{badge.Description}\n\n";
                }
            }
            message += "获得 50 积分奖励！";
            MessageBox.Show(message, "成就解锁", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// 更新徽章显示面板
        /// </summary>
        private void UpdateBadgesDisplay()
        {
            if (flowLayoutPanelBadges != null)
            {
                flowLayoutPanelBadges.Controls.Clear();

                foreach (var badge in _badges.Values)
                {
                    Label label = new Label();
                    label.Font = new Font("微软雅黑", 14F);
                    label.Text = badge.Unlocked ? badge.Emoji : "🔒";
                    label.Size = new Size(40, 40);
                    label.TextAlign = ContentAlignment.MiddleCenter;
                    label.Cursor = Cursors.Hand;
                    label.Tag = badge;
                    label.Click += Badge_Click;
                    _toolTip.SetToolTip(label, badge.Unlocked ? $"{badge.Name}: {badge.Description}" : "未解锁");
                    flowLayoutPanelBadges.Controls.Add(label);
                }
            }
        }

        /// <summary>
        /// 徽章点击事件处理器
        /// </summary>
        private void Badge_Click(object? sender, EventArgs e)
        {
            if (sender is Label label && label.Tag is Badge badge)
            {
                MessageBox.Show($"{badge.Emoji} {badge.Name}\n\n{badge.Description}",
                    badge.Unlocked ? "成就详情" : "锁定的成就",
                    MessageBoxButtons.OK,
                    badge.Unlocked ? MessageBoxIcon.Information : MessageBoxIcon.Question);
            }
        }

        /// <summary>
        /// 加载每日挑战任务
        /// </summary>
        private void LoadChallenges()
        {
            try
            {
                string challengesPath = Path.Combine(Paths.DataDirectory, "challenges.json");

                if (File.Exists(challengesPath))
                {
                    string json = File.ReadAllText(challengesPath);
                    var saved = JsonSerializer.Deserialize<List<Challenge>>(json);

                    if (saved != null && saved.Any() && saved[0].Current != 0)
                    {
                        DateTime lastDate = File.GetLastWriteTime(challengesPath);
                        if (lastDate.Date == DateTime.Today.Date)
                        {
                            _dailyChallenges = saved;
                            return;
                        }
                    }
                }
            }
            catch
            {
            }

            _dailyChallenges = new List<Challenge>
            {
                new Challenge("daily_learn", "每日学习", "学习10个内容", "📚", 10, 30),
                new Challenge("daily_quiz", "答题挑战", "答题模式答对5题", "🎯", 5, 50),
                new Challenge("daily_streak", "连续打卡", "今天完成学习", "🔥", 1, 20),
                new Challenge("daily_favorite", "收藏达人", "收藏3个内容", "❤️", 3, 25)
            };
        }

        /// <summary>
        /// 保存每日挑战进度
        /// </summary>
        private void SaveChallenges()
        {
            try
            {
                string challengesPath = Path.Combine(Paths.DataDirectory, "challenges.json");
                string json = JsonSerializer.Serialize(_dailyChallenges, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(challengesPath, json);
            }
            catch
            {
            }
        }

        /// <summary>
        /// 更新挑战任务显示面板
        /// </summary>
        private void UpdateChallengesDisplay()
        {
            if (flowLayoutPanelChallenges != null)
            {
                flowLayoutPanelChallenges.Controls.Clear();

                foreach (var challenge in _dailyChallenges)
                {
                    Panel panel = new Panel();
                    panel.Size = new Size(160, 70);
                    panel.BackColor = challenge.Completed ? Color.FromArgb(200, 255, 200) : Color.FromArgb(240, 240, 240);
                    panel.BorderStyle = BorderStyle.FixedSingle;

                    Label labelName = new Label();
                    labelName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
                    labelName.Text = $"{challenge.Emoji} {challenge.Name}";
                    labelName.Size = new Size(150, 20);
                    labelName.Location = new Point(5, 5);

                    Label labelProgress = new Label();
                    labelProgress.Font = new Font("微软雅黑", 9F);
                    labelProgress.Text = challenge.Completed ? $"已完成 ✓ +{challenge.Reward}分" : $"{challenge.Current}/{challenge.Target}";
                    labelProgress.Size = new Size(150, 20);
                    labelProgress.Location = new Point(5, 30);

                    ProgressBar progress = new ProgressBar();
                    progress.Size = new Size(150, 12);
                    progress.Location = new Point(5, 50);
                    progress.Maximum = challenge.Target;
                    progress.Value = Math.Min(challenge.Current, challenge.Target);

                    panel.Controls.Add(labelName);
                    panel.Controls.Add(labelProgress);
                    panel.Controls.Add(progress);

                    if (challenge.Completed && !challenge.Claimed)
                    {
                        Button claimBtn = new Button();
                        claimBtn.Text = "领取";
                        claimBtn.Size = new Size(50, 20);
                        claimBtn.Location = new Point(100, 45);
                        claimBtn.Click += (s, e) => ClaimChallenge(challenge);
                        panel.Controls.Add(claimBtn);
                    }

                    flowLayoutPanelChallenges.Controls.Add(panel);
                }
            }
        }

        /// <summary>
        /// 领取挑战奖励
        /// </summary>
        /// <param name="challenge">要领取奖励的挑战</param>
        private void ClaimChallenge(Challenge challenge)
        {
            if (!challenge.Claimed)
            {
                challenge.Claimed = true;
                _score += challenge.Reward;
                _xp += challenge.Reward;
                CheckLevelUp();
                UpdateStatsDisplay();
                UpdateChallengesDisplay();
                SaveChallenges();
                SaveStudyStats();
                _soundService?.PlaySuccess();
                MessageBox.Show($"🎁 领取成功！获得 {challenge.Reward} 积分！", "挑战奖励", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// 更新所有挑战任务的进度
        /// </summary>
        private void UpdateChallengesProgress()
        {
            foreach (var challenge in _dailyChallenges)
            {
                switch (challenge.Id)
                {
                    case "daily_learn":
                        challenge.Current = _todayLearnedCount;
                        break;
                    case "daily_quiz":
                        challenge.Current = _quizCorrectCount;
                        break;
                    case "daily_streak":
                        challenge.Current = _todayLearnedCount > 0 ? 1 : 0;
                        break;
                    case "daily_favorite":
                        challenge.Current = _favoriteCount;
                        break;
                }

                if (challenge.Current >= challenge.Target && !challenge.Completed)
                {
                    challenge.Completed = true;
                }
            }

            SaveChallenges();
            UpdateChallengesDisplay();
        }

        /// <summary>
        /// 检查是否满足升级条件并进行升级
        /// </summary>
        private void CheckLevelUp()
        {
            while (_xp >= _xpToNextLevel && _currentLevel < _levelTitles.Count - 1)
            {
                _xp -= _xpToNextLevel;
                _currentLevel++;
                _levelTitle = _levelTitles[_currentLevel];
                _xpToNextLevel = (_currentLevel + 1) * 100;

                _soundService?.PlaySuccess();
                StartConfetti();

                if (InvokeRequired)
                {
                    Invoke(new Action(() =>
                        MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{_levelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information)));
                }
                else
                {
                    MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{_levelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            UpdateLevelDisplay();
        }

        /// <summary>
        /// 更新等级显示信息
        /// </summary>
        private void UpdateLevelDisplay()
        {
            if (labelLevel != null)
            {
                labelLevel.Text = $"🏅 {_levelTitle} Lv.{_currentLevel + 1}";
            }

            if (progressXP != null)
            {
                progressXP.Maximum = _xpToNextLevel;
                progressXP.Value = _xp;
            }

            if (labelXP != null)
            {
                labelXP.Text = $"经验值: {_xp}/{_xpToNextLevel}";
            }
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
                _score += 10;
                _xp += 10;
                CheckLevelUp();
                labelGameResult.Text = $"✅ 正确！得分: {_gameScore}";
                _soundService?.PlaySuccess();
            }
            else
            {
                labelGameResult.Text = $"❌ 错误！正确答案: {_currentItem.GetDisplayText()}";
                _soundService?.PlayError();
            }

            UpdateStatsDisplay();
            NextGameQuestion();
        }

        /// <summary>
        /// 游戏计时器回调（预留）
        /// </summary>
        private void GameTimer_Tick(object? sender, EventArgs e)
        {
        }

        /// <summary>
        /// 更新统计信息显示
        /// </summary>
        private void UpdateStatsDisplay()
        {
            if (labelStudyTime != null)
                labelStudyTime.Text = $"⏱️ 学习时长: {_studyDuration.ToString(@"hh\:mm")}";

            if (labelScore != null)
                labelScore.Text = $"🏆 得分: {_score}";

            if (labelTodayCount != null)
                labelTodayCount.Text = $"📚 今日学习: {_todayLearnedCount} 项";

            if (labelStreak != null)
                labelStreak.Text = $"🔥 连续学习: {_streakDays} 天";

            if (progressDailyGoal != null)
            {
                progressDailyGoal.Value = Math.Min(_todayLearnedCount, 50);
            }

            if (labelDailyGoal != null)
                labelDailyGoal.Text = $"今日目标: {_todayLearnedCount}/50";
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
                labelEncouragement.Text = _encouragements[_random.Next(_encouragements.Length)];
                _encouragementCounter = 0;
            }
        }

        /// <summary>
        /// 学习计时器回调
        /// </summary>
        private void StudyTimer_Tick(object? sender, EventArgs e)
        {
            _studyDuration = _studyDuration.Add(TimeSpan.FromSeconds(1));
            UpdateStatsDisplay();
        }

        /// <summary>
        /// 增加分数并保存
        /// </summary>
        /// <param name="points">要增加的分数</param>
        private void IncrementScore(int points)
        {
            _score += points;
            _todayLearnedCount++;
            UpdateStatsDisplay();
            SaveStudyStats();
        }

        #endregion

        #region List Management

        public void UpdateLearningList(List<string> items, int currentIndex)
        {
            if (listBoxItems == null) return;

            listBoxItems.Items.Clear();
            for (int i = 0; i < items.Count; i++)
            {
                listBoxItems.Items.Add($"{i + 1}. {items[i]}");
            }

            UpdateListStatus(items.Count, currentIndex);

            if (currentIndex >= 0 && currentIndex < items.Count)
            {
                listBoxItems.SelectedIndex = currentIndex;
                listBoxItems.TopIndex = Math.Max(0, currentIndex - 5);
            }
        }

        private void UpdateListStatus(int totalItems, int currentIndex)
        {
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

            if (currentIndex >= 0 && currentIndex < listBoxItems.Items.Count)
            {
                listBoxItems.SelectedIndex = currentIndex;
                listBoxItems.TopIndex = Math.Max(0, currentIndex - 5);
            }
        }

        public void EnableListHighlighting(bool enable)
        {
            if (listBoxItems == null) return;

            listBoxItems.DrawMode = enable ? DrawMode.OwnerDrawFixed : DrawMode.Normal;
            if (enable)
            {
                listBoxItems.DrawItem += ListBoxItems_DrawItem;
            }
            else
            {
                listBoxItems.DrawItem -= ListBoxItems_DrawItem;
            }
        }

        private void ListBoxItems_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (e.Index < 0) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            using (var brush = new SolidBrush(isSelected ? Color.FromArgb(76, 175, 80) : listBox.BackColor))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            string text = listBox.Items[e.Index].ToString() ?? string.Empty;

            using (var foreBrush = new SolidBrush(isSelected ? Color.White : Color.Black))
            {
                e.Graphics.DrawString(text, e.Font, foreBrush, e.Bounds, StringFormat.GenericDefault);
            }

            if (isSelected)
            {
                using (var pen = new Pen(Color.White, 2))
                {
                    e.Graphics.DrawRectangle(pen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
                }
            }

            e.DrawFocusRectangle();
        }

        private void ListBoxItems_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (listBoxItems.SelectedIndex >= 0)
            {
                ItemSelectedFromList?.Invoke(this, new ItemSelectedEventArgs(listBoxItems.SelectedIndex));
            }
        }

        public event EventHandler<ItemSelectedEventArgs>? ItemSelectedFromList;

        #endregion

        #region Paint Event Handlers

        private void PanelContent_Paint(object? sender, PaintEventArgs e)
        {
            if (DesignMode) return;
            if (sender is not Panel panel) return;

            try
            {
                using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0),
                    new Point(panel.Width, panel.Height),
                    Color.FromArgb(255, 255, 255),
                    Color.FromArgb(245, 250, 248)
                );
                e.Graphics.FillRectangle(gradient, panel.ClientRectangle);

                using var pen = new Pen(Color.FromArgb(200, 210, 220), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panel.Width - 3, panel.Height - 3);
            }
            catch
            {
                e.Graphics.Clear(Color.White);
            }
        }

        private void PanelAI_Paint(object? sender, PaintEventArgs e)
        {
            if (DesignMode) return;
            if (sender is not Panel panel) return;

            try
            {
                using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0),
                    new Point(0, panel.Height),
                    Color.FromArgb(252, 248, 240),
                    Color.FromArgb(248, 244, 235)
                );
                e.Graphics.FillRectangle(gradient, panel.ClientRectangle);

                using var pen = new Pen(Color.FromArgb(210, 200, 190), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panel.Width - 3, panel.Height - 3);
            }
            catch
            {
                e.Graphics.Clear(Color.LightYellow);
            }
        }

        private void labelAI_Click(object? sender, EventArgs e)
        {
        }

        private void mainTableLayoutPanel_Paint(object? sender, PaintEventArgs e)
        {
            DrawConfetti(e.Graphics);
        }

        #endregion

        #region Event Handlers

        private bool _isDetailVisible = false;

        private void LabelContent_Click(object? sender, EventArgs e)
        {
            ToggleDetail();
        }

        private void ListBoxDisplay_Click(object? sender, EventArgs e)
        {
            ToggleDetail();
        }

        /// <summary>
        /// 切换详情区的显示/隐藏
        /// </summary>
        private void ToggleDetail()
        {
            _isDetailVisible = !_isDetailVisible;
            listBoxDisplay.Visible = _isDetailVisible;

            if (_isDetailVisible && _currentItem != null)
            {
                UpdateDetailContent(_currentItem.GetDisplayText());
                // 答题模式下查看答案，标记为已显示
                if (_isQuizMode && !_answerRevealed)
                {
                    _answerRevealed = true;
                    buttonRevealAnswer.Visible = false;
                }
            }
        }

        /// <summary>
        /// 格式化显示文本，将竖线替换为换行
        /// </summary>
        private string FormatDisplayText(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("||", "\n\n").Replace("|", "\n");
        }

        /// <summary>
        /// 更新详情区内容（用于答题模式等场景）
        /// </summary>
        private void UpdateDetailContent(string text)
        {
            listBoxDisplay.Items.Clear();
            string formattedText = FormatDisplayText(text);
            // 将格式化后的文本按换行分割为多个列表项
            string[] lines = formattedText.Split(new[] { '\n', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    listBoxDisplay.Items.Add(trimmedLine);
                }
            }
        }

        private void RadioStudyMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioStudyMode.Checked && !_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioQuickMode_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioQuickMode.Checked && !_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioSequential_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioSequential.Checked && !_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioRandom_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioRandom.Checked && !_settingsChangedEventsSuspended)
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioChinese_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioChinese.Checked && !_settingsChangedEventsSuspended)
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RadioEnglish_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioEnglish.Checked && !_settingsChangedEventsSuspended)
            {
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
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

        private void ButtonKnown_Click(object? sender, EventArgs e)
        {
            _celebrationCounter++;

            if (_celebrationCounter >= CelebrationInterval)
            {
                _soundService?.PlaySuccess();
                StartConfetti();
                _celebrationCounter = 0;
            }

            int points = _isQuizMode && !_answerRevealed ? 20 : 10;
            IncrementScore(points);

            _totalLearnedCount++;
            if (_isQuizMode && !_answerRevealed)
            {
                _quizCorrectCount++;
                _isDetailVisible = true;
                listBoxDisplay.Visible = true;
                UpdateDetailContent(_correctMessages[_random.Next(_correctMessages.Length)]);
            }

            UpdateEncouragement();
            CheckBadgeUnlock();
            UpdateChallengesProgress();

            MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonUnknown_Click(object? sender, EventArgs e)
        {
            _soundService?.PlayError();
            ShakeWindow();

            if (_isQuizMode && !_answerRevealed)
            {
                _isDetailVisible = true;
                listBoxDisplay.Visible = true;
                UpdateDetailContent(_wrongMessages[_random.Next(_wrongMessages.Length)]);
            }

            MarkAsUnknownClicked?.Invoke(this, EventArgs.Empty);
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

        private void StartConfetti()
        {
            if (_isConfettiActive) return;

            _isConfettiActive = true;
            _confettiParticles.Clear();

            for (int i = 0; i < 100; i++)
            {
                _confettiParticles.Add(CreateConfettiParticle());
            }

            for (int i = 0; i < 50; i++)
            {
                var particle = CreateConfettiParticle();
                particle.X = _random.Next(Width);
                particle.Y = -_random.Next(100);
                particle.VelocityY = (float)(_random.NextDouble() * 4 + 3);
                particle.Size = _random.Next(6, 15);
                particle.RotationSpeed = (float)(_random.NextDouble() * 15 - 7.5);
                _confettiParticles.Add(particle);
            }

            _confettiTimer.Start();
        }

        private ConfettiParticle CreateConfettiParticle()
        {
            var shapes = new[] { ParticleShape.Rectangle, ParticleShape.Circle, ParticleShape.Triangle, ParticleShape.Star };
            return new ConfettiParticle
            {
                X = _random.Next(Width),
                Y = -_random.Next(200),
                Size = _random.Next(8, 20),
                Color = _celebrationColors[_random.Next(_celebrationColors.Length)],
                VelocityX = (float)(_random.NextDouble() * 6 - 3),
                VelocityY = (float)(_random.NextDouble() * 3 + 2),
                Rotation = _random.Next(360),
                RotationSpeed = (float)(_random.NextDouble() * 12 - 6),
                Shape = shapes[_random.Next(shapes.Length)],
                Opacity = 1.0f,
                FadeSpeed = (float)(_random.NextDouble() * 0.01 + 0.005),
                WobbleOffset = (float)(_random.NextDouble() * Math.PI * 2),
                WobbleSpeed = (float)(_random.NextDouble() * 0.1 + 0.05)
            };
        }

        private void ConfettiTimer_Tick(object? sender, EventArgs e)
        {
            if (!_isConfettiActive) return;

            bool hasActiveParticles = false;

            foreach (var particle in _confettiParticles)
            {
                particle.WobbleOffset += particle.WobbleSpeed;
                particle.X += particle.VelocityX + (float)Math.Sin(particle.WobbleOffset) * 0.5f;
                particle.Y += particle.VelocityY;
                particle.VelocityY += 0.08f;
                particle.Rotation += particle.RotationSpeed;

                if (particle.Y > Height * 0.7)
                {
                    particle.Opacity -= particle.FadeSpeed;
                }

                if (particle.Y < Height + 50 && particle.Opacity > 0)
                {
                    hasActiveParticles = true;
                }
            }

            if (!hasActiveParticles)
            {
                _isConfettiActive = false;
                _confettiTimer.Stop();
                _confettiParticles.Clear();
            }

            Invalidate();
        }

        private void DrawConfetti(Graphics g)
        {
            if (!_isConfettiActive || _confettiParticles.Count == 0) return;

            g.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var particle in _confettiParticles)
            {
                if (particle.Y > Height + 50 || particle.Opacity <= 0) continue;

                using var brush = new SolidBrush(Color.FromArgb((int)(particle.Opacity * 255), particle.Color));

                g.TranslateTransform(particle.X, particle.Y);
                g.RotateTransform(particle.Rotation);

                switch (particle.Shape)
                {
                    case ParticleShape.Rectangle:
                        g.FillRectangle(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size * 0.6f);
                        break;
                    case ParticleShape.Circle:
                        g.FillEllipse(brush, -particle.Size / 2, -particle.Size / 2, particle.Size, particle.Size);
                        break;
                    case ParticleShape.Triangle:
                        var trianglePoints = new PointF[]
                        {
                            new PointF(0, -particle.Size / 2),
                            new PointF(-particle.Size / 2, particle.Size / 2),
                            new PointF(particle.Size / 2, particle.Size / 2)
                        };
                        g.FillPolygon(brush, trianglePoints);
                        break;
                    case ParticleShape.Star:
                        var starPoints = new PointF[10];
                        for (int i = 0; i < 10; i++)
                        {
                            float radius = (i % 2 == 0) ? particle.Size / 2 : particle.Size / 4;
                            float angle = (float)(i * Math.PI / 5 - Math.PI / 2);
                            starPoints[i] = new PointF(
                                (float)Math.Cos(angle) * radius,
                                (float)Math.Sin(angle) * radius
                            );
                        }
                        g.FillPolygon(brush, starPoints);
                        break;
                }

                g.ResetTransform();
            }
        }

        private async void ShakeWindow()
        {
            var originalLocation = Location;
            var shakeAmount = 15;
            var shakeSteps = 8;
            var stepDelay = 15;
            var random = new Random();

            for (int i = 0; i < shakeSteps; i++)
            {
                int currentShakeAmount = (int)(shakeAmount * (1 - (i / (float)shakeSteps)));

                int dx = (random.Next(3) - 1) * currentShakeAmount;
                int dy = (random.Next(3) - 1) * currentShakeAmount;

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
                MessageBox.Show("请先选择一个学习内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_aiPanelPopupService == null)
            {
                MessageBox.Show("AI面板服务未初始化", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                // 获取当前学习内容作为上下文
                string context = _currentItem.GetMainContent();
                string displayText = _currentItem.GetDisplayText();

                // 构建AI提示词
                string prompt = $"请解释以下内容：\n{displayText}\n\n原文：{context}";

                // 使用AI面板服务显示AIAbilityPanel
                _aiPanelPopupService.ShowAIAbilityPanel(this, prompt, null, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "打开AI问答窗口失败");
                MessageBox.Show($"打开AI问答窗口失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonExit_Click(object? sender, EventArgs e)
        {
            Close();
            ExitClicked?.Invoke(this, EventArgs.Empty);
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
            buttonRevealAnswer.Visible = false;
            _isDetailVisible = true;
            listBoxDisplay.Visible = true;

            if (_currentItem != null)
            {
                UpdateDetailContent(_currentItem.GetDisplayText());
            }
        }

        private void ButtonQuizMode_Click(object? sender, EventArgs e)
        {
            _isQuizMode = !_isQuizMode;

            if (_isQuizMode)
            {
                buttonQuizMode.BackColor = Color.FromArgb(76, 175, 80);
                buttonQuizMode.Text = "📖 学习模式";
                labelQuizHint.Text = "答案已隐藏";
                HideAnswer();
            }
            else
            {
                buttonQuizMode.BackColor = Color.FromArgb(255, 193, 7);
                buttonQuizMode.Text = "🎮 答题模式";
                labelQuizHint.Text = "先隐藏答案，测试自己";
                ShowAnswer();
            }
        }

        private void HideAnswer()
        {
            _isDetailVisible = false;
            listBoxDisplay.Visible = false;
            UpdateDetailContent("❓ 请猜测答案");
            _answerRevealed = false;
            buttonRevealAnswer.Visible = true;
        }

        private void ShowAnswer()
        {
            _isDetailVisible = true;
            listBoxDisplay.Visible = true;

            if (_currentItem != null)
            {
                UpdateDetailContent(_currentItem.GetDisplayText());
            }
            _answerRevealed = true;
            buttonRevealAnswer.Visible = false;
        }

        private void CheckBoxShowDetail_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is not CheckBox checkBox) return;

            if (checkBox.Checked)
            {
                // 显示详情
                _isDetailVisible = true;
                listBoxDisplay.Visible = true;

                if (_currentItem != null)
                {
                    UpdateDetailContent(_currentItem.GetDisplayText());
                }
            }
            else
            {
                // 隐藏详情
                _isDetailVisible = false;
                listBoxDisplay.Visible = false;
            }
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

        private bool _isFavorite = false;

        private void ButtonFavorite_Click(object? sender, EventArgs e)
        {
            _isFavorite = !_isFavorite;

            if (_isFavorite)
            {
                _soundService?.PlaySuccess();
                _favoriteCount++;
                SaveFavorite();
                CheckBadgeUnlock();
                UpdateChallengesProgress();
            }
            else
            {
                _favoriteCount = Math.Max(0, _favoriteCount - 1);
                RemoveFavorite();
            }

            UpdateFavoriteButton();
        }

        private void SaveFavorite()
        {
            if (_currentItem == null) return;

            try
            {
                string favoritesPath = Path.Combine(Paths.DataDirectory, "favorites.json");
                List<string> favorites = new List<string>();

                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }

                string key = GetFavoriteKey();
                if (!string.IsNullOrEmpty(key) && !favorites.Contains(key))
                {
                    favorites.Add(key);
                    string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(favoritesPath, json);
                }
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
                string favoritesPath = Path.Combine(Paths.DataDirectory, "favorites.json");

                if (File.Exists(favoritesPath))
                {
                    string json = File.ReadAllText(favoritesPath);
                    List<string> favorites = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();

                    string key = GetFavoriteKey();
                    if (!string.IsNullOrEmpty(key))
                    {
                        favorites.Remove(key);
                        string newJson = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                        File.WriteAllText(favoritesPath, newJson);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "移除收藏失败");
            }
        }

        /// <summary>
        /// 生成收藏的唯一键（子类别+内容），避免跨类别冲突
        /// </summary>
        private string GetFavoriteKey()
        {
            if (_currentItem == null) return string.Empty;

            string subCategory = comboBoxSubCategory?.Text ?? "unknown";
            string content = _currentItem.GetMainContent();

            return $"[{subCategory}]{content}";
        }

        private void ButtonNote_Click(object? sender, EventArgs e)
        {
            panelNotes.Visible = !panelNotes.Visible;

            if (panelNotes.Visible)
            {
                buttonNote.BackColor = Color.FromArgb(156, 39, 176);
                buttonNote.Text = "📝 笔记 (开)";
                // 设置笔记面板行高
                middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 150F);
                LoadNotes();
            }
            else
            {
                buttonNote.BackColor = Color.FromArgb(76, 175, 80);
                buttonNote.Text = "📝 笔记";
                // 隐藏笔记面板行
                middleTableLayoutPanel.RowStyles[1] = new RowStyle(SizeType.Absolute, 0F);
            }
        }

        private void LoadNotes()
        {
            if (_currentItem == null || richTextBoxNotes == null) return;

            try
            {
                string notesPath = Path.Combine(Paths.DataDirectory, "notes.json");

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
                string notesPath = Path.Combine(Paths.DataDirectory, "notes.json");
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
        /// 生成笔记的唯一键（子类别+内容），避免跨类别冲突
        /// </summary>
        private string GetNoteKey()
        {
            if (_currentItem == null) return string.Empty;

            string subCategory = comboBoxSubCategory?.Text ?? "unknown";
            string content = _currentItem.GetMainContent();

            return $"[{subCategory}]{content}";
        }

        private void RichTextBoxNotes_TextChanged(object? sender, EventArgs e)
        {
            SaveNotes();

            if (richTextBoxNotes != null && !string.IsNullOrWhiteSpace(richTextBoxNotes.Text))
            {
                _noteCount++;
                CheckBadgeUnlock();
            }
        }

        private bool ContainsChinese(string text)
        {
            return text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
        }

        private void LearningForm_KeyDown(object? sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Space:
                    e.Handled = true;
                    PronounceClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.Enter:
                    e.Handled = true;
                    _soundService?.PlayNavigation();
                    NextClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.D1:
                case Keys.K:
                    e.Handled = true;
                    _soundService?.PlaySuccess();
                    MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.D2:
                case Keys.U:
                    e.Handled = true;
                    _soundService?.PlayError();
                    MarkAsUnknownClicked?.Invoke(this, EventArgs.Empty);
                    break;
                case Keys.Escape:
                    e.Handled = true;
                    ExitClicked?.Invoke(this, EventArgs.Empty);
                    Close();
                    break;
            }
        }

        #endregion

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

                _gameTimer?.Stop();
                _gameTimer?.Dispose();

                _confettiTimer?.Stop();
                _confettiTimer?.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }



        private class PanelDecorator
        {
        }

        public class ItemSelectedEventArgs : EventArgs
        {
            public int Index { get; }

            public ItemSelectedEventArgs(int index)
            {
                Index = index;
            }
        }

        public class StudyStats
        {
            public int TodayLearnedCount { get; set; }
            public int StreakDays { get; set; }
            public int TotalScore { get; set; }
            public DateTime LastStudyDate { get; set; }
            public int TotalLearnedCount { get; set; }
            public int QuizCorrectCount { get; set; }
            public int FavoriteCount { get; set; }
            public int NoteCount { get; set; }
            public int XP { get; set; }
            public int CurrentLevel { get; set; }
        }

    }
}
