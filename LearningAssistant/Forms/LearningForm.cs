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
        private readonly IEncouragementService _encouragementService;
        private bool _disposed = false;
        private Settings _settings = new();

        private readonly List<ConfettiParticle> _confettiParticles = new List<ConfettiParticle>();
        private readonly System.Windows.Forms.Timer _confettiTimer = new System.Windows.Forms.Timer();
        private static readonly ThreadLocal<Random> _random = new ThreadLocal<Random>(() => new Random());
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

        private readonly Dictionary<int, SolidBrush> _colorBrushCache = new Dictionary<int, SolidBrush>();

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory, ISoundService soundService, IThemeService themeService, IAIPanelPopupService aiPanelPopupService, IEncouragementService encouragementService)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
            _aiPanelPopupService = aiPanelPopupService ?? throw new ArgumentNullException(nameof(aiPanelPopupService));
            _encouragementService = encouragementService ?? throw new ArgumentNullException(nameof(encouragementService));


            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;

            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

            _themeService.RegisterThemeable(this);

            // 初始化笔记保存计时器
            _noteSaveTimer.Tick += NoteSaveTimer_Tick;
        }

        private void BindSubViewEvents()
        {
            _buttonsView.KnownClicked += ButtonKnown_Click;
            _buttonsView.UnknownClicked += ButtonUnknown_Click;
            _buttonsView.PronounceClicked += ButtonPronounce_Click;
            _buttonsView.FavoriteClicked += ButtonFavorite_Click;
            _buttonsView.NoteClicked += ButtonNote_Click;
            _buttonsView.ExitClicked += ButtonExit_Click;
            _buttonsView.AIAskClicked += ButtonAIAsk_Click;

            _settingsView.RadioStudyMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioQuickMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioSequential.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioRandom.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioChinese.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioEnglish.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.ComboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            _settingsView.ButtonOpenStatistics.Click += ButtonOpenStatistics_Click;
            _settingsView.ButtonExportErrorBook.Click += ButtonExportErrorBook_Click;
            _settingsView.ButtonQuizMode.Click += ButtonQuizMode_Click;
            _settingsView.ButtonThemeToggle.Click += ButtonThemeToggle_Click;

            _contentView.ContentClicked += LabelContent_Click;
            _contentView.DetailClicked += ListBoxDisplay_Click;
            _contentView.NoteTextChanged += RichTextBoxNotes_TextChanged;

            _listView.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;
        }



        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            // Light 主题下不改变 Panel 的颜色
            if (colors.ThemeMode == ThemeMode.Dark)
            {
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
                // Light 主题下不改变 Panel 的颜色
                if (colors.ThemeMode == ThemeMode.Dark)
                {
                    panel.BackColor = colors.Surface;
                }
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
                if (_ttsService != null)
                {
                    await _ttsService.StopAsync();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to stop TTS service");
            }
        }

        private void LoadSettings()
        {
            try
            {
                string settingsPath = AppPaths.AppSettingsPath;
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
                string settingsPath = Path.Combine(AppPaths.ConfigDir, "settings.json");
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

                // 同步 _isQuizMode 状态
                _isQuizMode = radioQuickMode.Checked;

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
                AdjustFontSizeBasedOnContent(value);
                // 注意：不在此处调用 ResetDetailState，由 CurrentItem 统一管理状态
            }
        }

        private void AdjustFontSizeBasedOnContent(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            int charCount = content.Length;
            float fontSize;

            if (charCount <= 2)
            {
                fontSize = 80F;
            }
            else if (charCount <= 4)
            {
                fontSize = 72F;
            }
            else if (charCount <= 8)
            {
                fontSize = 64F;
            }
            else if (charCount <= 15)
            {
                fontSize = 56F;
            }
            else
            {
                fontSize = 48F;
            }

            labelContent.Font = new Font("微软雅黑", fontSize, FontStyle.Bold, GraphicsUnit.Point, 134);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayText
        {
            set
            {
                // 仅更新内容，由 CurrentItem 统一管理状态
                if (!string.IsNullOrEmpty(value))
                {
                    UpdateDetailContent(value);
                }
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayStruct
        {
            set
            {
                // 仅更新内容，由 CurrentItem 统一管理状态
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
                    // 切换学习项时，根据当前模式重置详情区状态
                    ResetDetailState();
                }
            }
        }

        /// <summary>
        /// 重置详情区状态（切换学习项时调用）
        /// </summary>
        private void ResetDetailState()
        {
            _answerRevealed = false;
            _currentNoteCounted = false;

            UpdateDetailState(true, !_isQuizMode);

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
                string favoritesPath = Path.Combine(AppPaths.DataDir, "favorites.json");
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
            buttonPronounce.Enabled = enabled;
        }

        public async void PlayPronunciation(string text, string language)
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

        private System.ComponentModel.IContainer components = null;

        // ========== 子视图实例（主视图通过这些管理子视图）==========
        private LearningListView _listView = null!;
        private LearningContentView _contentView = null!;
        private LearningButtonsView _buttonsView = null!;
        private LearningStatsView _statsView = null!;
        private LearningSettingsView _settingsView = null!;

        // ========== 兼容访问器（委托到子视图，保持现有代码不动）==========
        private Panel panelContent => _contentView.PanelContent;
        private ListBox listBoxDisplay => _contentView.ListBoxDisplay;
        private Label labelContent => _contentView.LabelContent;
        private Label labelStatistics => _statsView.LabelStatistics;
        private Panel panelList => _listView.PanelList;
        private ListBox listBoxItems => _listView.ListBoxItems;
        private Label labelListTitle => _listView.LabelListTitle;
        private Label labelListStatus => _listView.LabelListStatus;
        private Panel panelConfig => _settingsView.PanelConfig;
        private Label labelConfigTitle => _settingsView.PanelConfig.Controls.OfType<Label>().FirstOrDefault(l => l.Dock == DockStyle.Top) ?? new Label();
        private GroupBox groupBoxMode => _settingsView.GroupBoxMode;
        private RadioButton radioStudyMode => _settingsView.RadioStudyMode;
        private RadioButton radioQuickMode => _settingsView.RadioQuickMode;
        private GroupBox groupBoxSort => _settingsView.GroupBoxSort;
        private RadioButton radioSequential => _settingsView.RadioSequential;
        private RadioButton radioRandom => _settingsView.RadioRandom;
        private GroupBox groupBoxLanguage => _settingsView.GroupBoxLanguage;
        private RadioButton radioChinese => _settingsView.RadioChinese;
        private RadioButton radioEnglish => _settingsView.RadioEnglish;
        private Label labelSubCategory => _settingsView.LabelSubCategory;
        private ComboBox comboBoxSubCategory => _settingsView.ComboBoxSubCategory;
        private Button buttonOpenStatistics => _settingsView.ButtonOpenStatistics;
        private Button buttonExportErrorBook => _settingsView.ButtonExportErrorBook;
        private Button buttonPronounce => _buttonsView.ButtonPronounce;
        private Button buttonFavorite => _buttonsView.ButtonFavorite;
        private Button buttonNote => _buttonsView.ButtonNote;
        private Button buttonExit => _buttonsView.ButtonExit;
        private Button buttonAIAsk => _buttonsView.ButtonAIAsk;
        private Button buttonKnown => _buttonsView.ButtonKnown;
        private Button buttonUnknown => _buttonsView.ButtonUnknown;

        private FlowLayoutPanel buttonsFlowLayoutPanel => _buttonsView.ButtonsPanel;
        private Panel panelNotes => _contentView.PanelNotes;
        private RichTextBox richTextBoxNotes => _contentView.RichTextBoxNotes;
        private Label labelNotesTitle => _contentView.LabelNotesTitle;
        private Panel panelQuizMode => _settingsView.PanelQuizMode;
        private Button buttonQuizMode => _settingsView.ButtonQuizMode;
        private Label labelQuizHint => _settingsView.LabelQuizHint;
        private Button buttonThemeToggle => _settingsView.ButtonThemeToggle;

        private CheckBox checkBoxVoice => _settingsView.CheckBoxVoice;
        private FlowLayoutPanel pronunciationFlowLayoutPanel => _settingsView.PronunciationFlowLayoutPanel;
        private RadioButton radioOriginal => _settingsView.RadioOriginal;
        private RadioButton radioExplanation => _settingsView.RadioExplanation;
        private RadioButton radioBoth => _settingsView.RadioBoth;

        // ========== 统计相关控件（委托到 StatsView）==========
        private Panel panelStats => _statsView.PanelStatsContainer;
        private Label labelStudyTime => _statsView.LabelStudyTime;
        private Label labelScore => _statsView.LabelScore;
        private Label labelTodayCount => _statsView.LabelTodayCount;
        private Label labelStreak => _statsView.LabelStreak;
        private Label labelEncouragement => _statsView.LabelEncouragement;
        private ProgressBar progressBar1 => _statsView.ProgressBar;
        private Label labelDailyGoal;
        // ========== 布局相关控件（暂保留）==========
        private TableLayoutPanel mainTableLayoutPanel = null!;
        private Panel middlePanel = null!;
        private TableLayoutPanel middleTableLayoutPanel = null!;

        // ========== 游戏相关控件（暂保留，后续清理）========== 
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
            // ========== 创建子视图（它们内部已创建所有子控件）==========
            _listView = new LearningListView();
            _contentView = new LearningContentView();
            _buttonsView = new LearningButtonsView();
            _statsView = new LearningStatsView();
            _settingsView = new LearningSettingsView();

            // ========== 仅创建独立控件（不是子视图覆盖的）==========
            mainTableLayoutPanel = new TableLayoutPanel();
            middlePanel = new Panel();
            middleTableLayoutPanel = new TableLayoutPanel();


            // ========== SuspendLayout（仅对独立控件）==========
            panelStats.SuspendLayout();
            mainTableLayoutPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            middleTableLayoutPanel.SuspendLayout();
            SuspendLayout();

            // ========== 子视图已在其构造函数中完成所有初始化，此处仅设置布局引用 ==========

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
            mainTableLayoutPanel.Paint += mainTableLayoutPanel_Paint;

            //
            // middlePanel
            //
            middlePanel.BackColor = Color.FromArgb(250, 245, 235);
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
            middleTableLayoutPanel.Controls.Add(_contentView.PanelContent, 0, 0);
            middleTableLayoutPanel.Controls.Add(_contentView.PanelNotes, 0, 1);
            middleTableLayoutPanel.Controls.Add(_buttonsView.ButtonsPanel, 0, 2);
            middleTableLayoutPanel.Controls.Add(progressBar1, 0, 3);
            middleTableLayoutPanel.Controls.Add(_statsView.LabelStatistics, 0, 5);
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
            middleTableLayoutPanel.Size = new Size(1095, 838);
            middleTableLayoutPanel.TabIndex = 0;



            //
            // _confettiTimer
            //
            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

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

            panelStats.ResumeLayout(false);
            mainTableLayoutPanel.ResumeLayout(false);
            middlePanel.ResumeLayout(false);
            middleTableLayoutPanel.ResumeLayout(false);
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

            _noteSaveTimer.Interval = 1000;
            _noteSaveTimer.Tick += NoteSaveTimer_Tick;

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
                string statsPath = Path.Combine(AppPaths.DataDir, "study_stats.json");
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载学习统计失败");
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

                string statsPath = Path.Combine(AppPaths.DataDir, "study_stats.json");
                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(statsPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存学习统计失败");
            }
        }

        /// <summary>
        /// 更新统计显示
        /// </summary>
        private void UpdateStatsDisplay()
        {
            labelStudyTime.Text = $"⏱️ 学习时长: {_studyDuration:hh\\:mm\\:ss}";
            labelScore.Text = $"🏆 得分: {_score}";
            labelTodayCount.Text = $"📚 今日学习: {_todayLearnedCount} 项";
            labelStreak.Text = $"🔥 连续学习: {_streakDays} 天";
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
            foreach (var item in items)
            {
                listBoxItems.Items.Add(item);
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

            listBoxItems.ItemHeight = 30;
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

        private readonly SolidBrush _selectedBackgroundBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
        private readonly SolidBrush _selectedForegroundBrush = new SolidBrush(Color.White);
        private readonly SolidBrush _normalForegroundBrush = new SolidBrush(Color.Black);
        private readonly Pen _selectedBorderPen = new Pen(Color.White, 2);

        private void ListBoxItems_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (sender is not ListBox listBox) return;
            if (e.Index < 0) return;

            e.DrawBackground();

            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;

            if (isSelected)
            {
                e.Graphics.FillRectangle(_selectedBackgroundBrush, e.Bounds);
            }

            string text = listBox.Items[e.Index].ToString() ?? string.Empty;

            e.Graphics.DrawString(text, e.Font, isSelected ? _selectedForegroundBrush : _normalForegroundBrush, e.Bounds, StringFormat.GenericDefault);

            if (isSelected)
            {
                e.Graphics.DrawRectangle(_selectedBorderPen, e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);
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

        private void labelAI_Click(object? sender, EventArgs e)
        {
            // AI功能已通过按钮触发，此事件保留以兼容设计器
        }

        private void mainTableLayoutPanel_Paint(object? sender, PaintEventArgs e)
        {
            DrawConfetti(e.Graphics);
        }

        #endregion

        #region Event Handlers



        private void LabelContent_Click(object? sender, EventArgs e)
        {
            // 点击内容区：根据模式显示或隐藏答案
            if (_isQuizMode)
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

        private void ListBoxDisplay_Click(object? sender, EventArgs e)
        {
            // 点击详情区：根据模式显示或隐藏答案
            if (_isQuizMode)
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

        /// <summary>
        /// 更新详情区内容
        /// </summary>
        /// <param name="text">要显示的文本</param>
        private void UpdateDetailContent(string text)
        {
            listBoxDisplay.Items.Clear();
            if (string.IsNullOrEmpty(text)) return;

            string[] lines = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    listBoxDisplay.Items.Add(trimmedLine);
                }
            }
        }

        /// <summary>
        /// 统一更新详情区状态
        /// </summary>
        /// <param name="visible">是否可见</param>
        /// <param name="showAnswer">是否显示答案（答题模式下）</param>
        private void UpdateDetailState(bool visible, bool showAnswer = true)
        {
            listBoxDisplay.Visible = visible;

            if (visible && _currentItem != null)
            {
                if (_isQuizMode)
                {
                    // 答题模式：根据是否已揭示答案显示不同内容
                    _answerRevealed = showAnswer;
                    if (_answerRevealed)
                    {
                        UpdateDetailContent(_currentItem.GetDisplayText()); // 显示完整答案
                    }
                    else
                    {
                        UpdateDetailContent(_currentItem.GetDisplayStruct()); // 只显示结构（问题）
                    }
                }
                else
                {
                    // 学习模式：显示完整内容
                    _answerRevealed = true;
                    UpdateDetailContent(_currentItem.GetDisplayText());
                }
            }
        }

        /// <summary>
        /// 从文件加载已解锁的徽章
        /// </summary>
        private void LoadBadges()
        {
            try
            {
                string badgesPath = Path.Combine(AppPaths.DataDir, "badges.json");
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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载徽章失败");
            }
        }

        /// <summary>
        /// 保存已解锁的徽章到文件
        /// </summary>
        private void SaveBadges()
        {
            try
            {
                string badgesPath = Path.Combine(AppPaths.DataDir, "badges.json");
                var badgesDir = Path.GetDirectoryName(badgesPath);
                if (!string.IsNullOrEmpty(badgesDir) && !Directory.Exists(badgesDir)) Directory.CreateDirectory(badgesDir);
                string json = JsonSerializer.Serialize(_unlockedBadges, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(badgesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存徽章失败");
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
                string challengesPath = Path.Combine(AppPaths.DataDir, "challenges.json");

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
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载每日挑战失败");
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
                string challengesPath = Path.Combine(AppPaths.DataDir, "challenges.json");
                var challengesDir = Path.GetDirectoryName(challengesPath);
                if (!string.IsNullOrEmpty(challengesDir) && !Directory.Exists(challengesDir)) Directory.CreateDirectory(challengesDir);
                string json = JsonSerializer.Serialize(_dailyChallenges, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(challengesPath, json);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "保存每日挑战失败");
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
        /// 更新鼓励语显示（按间隔显示，避免太频繁）
        /// </summary>
        private void UpdateEncouragement()
        {
            _encouragementCounter++;

            // 只在达到间隔时更新鼓励语
            if (_encouragementCounter >= EncouragementInterval && labelEncouragement != null)
            {
                labelEncouragement.Text = _encouragements[Random.Shared.Next(_encouragements.Length)];
                _encouragementCounter = 0;
            }
        }

        #endregion


        private void RadioSetting_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is RadioButton radio && radio.Checked && !_settingsChangedEventsSuspended)
            {
                // 学习模式/答题模式切换时，更新 _isQuizMode 和详情区状态
                if (radio == radioStudyMode)
                {
                    _isQuizMode = false;
                    UpdateDetailState(true, true);  // 学习模式：显示答案
                    // 更新快捷按钮文本
                    buttonQuizMode.BackColor = Color.FromArgb(255, 193, 7);
                    buttonQuizMode.Text = "🎮 答题模式";
                    labelQuizHint.Text = "学习模式，显示完整内容";
                }
                else if (radio == radioQuickMode)
                {
                    _isQuizMode = true;
                    UpdateDetailState(true, false);  // 答题模式：隐藏答案
                    // 更新快捷按钮文本
                    buttonQuizMode.BackColor = Color.FromArgb(76, 175, 80);
                    buttonQuizMode.Text = "📖 学习模式";
                    labelQuizHint.Text = "答案已隐藏";
                }

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

                int points = _isQuizMode && !_answerRevealed ? 20 : 10;
                IncrementScore(points);

                _totalLearnedCount++;
                if (_isQuizMode && !_answerRevealed)
                {
                    _quizCorrectCount++;
                }

                UpdateEncouragement();
                CheckBadgeUnlock();
                UpdateChallengesProgress();

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
                    // 不认识按钮：强制显示答案（用于学习目的）
                    UpdateDetailState(true, true);
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
                particle.X = Random.Shared.Next(Width);
                particle.Y = -Random.Shared.Next(100);
                particle.VelocityY = (float)(Random.Shared.NextDouble() * 4 + 3);
                particle.Size = Random.Shared.Next(6, 15);
                particle.RotationSpeed = (float)(Random.Shared.NextDouble() * 15 - 7.5);
                _confettiParticles.Add(particle);
            }

            _confettiTimer.Start();
        }

        private ConfettiParticle CreateConfettiParticle()
        {
            var shapes = new[] { ParticleShape.Rectangle, ParticleShape.Circle, ParticleShape.Triangle, ParticleShape.Star };
            return new ConfettiParticle
            {
                X = Random.Shared.Next(Width),
                Y = -Random.Shared.Next(200),
                Size = Random.Shared.Next(8, 20),
                Color = _celebrationColors[Random.Shared.Next(_celebrationColors.Length)],
                VelocityX = (float)(Random.Shared.NextDouble() * 6 - 3),
                VelocityY = (float)(Random.Shared.NextDouble() * 3 + 2),
                Rotation = Random.Shared.Next(360),
                RotationSpeed = (float)(Random.Shared.NextDouble() * 12 - 6),
                Shape = shapes[Random.Shared.Next(shapes.Length)],
                Opacity = 1.0f,
                FadeSpeed = (float)(Random.Shared.NextDouble() * 0.01 + 0.005),
                WobbleOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                WobbleSpeed = (float)(Random.Shared.NextDouble() * 0.1 + 0.05)
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

                int colorKey = particle.Color.ToArgb();
                if (!_colorBrushCache.TryGetValue(colorKey, out var brush))
                {
                    brush = new SolidBrush(particle.Color);
                    _colorBrushCache[colorKey] = brush;
                }

                brush.Color = Color.FromArgb((int)(particle.Opacity * 255), particle.Color);

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
                MessageBox.Show("请先选择一个学习内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            listBoxDisplay.Visible = true;

            if (_currentItem != null)
            {
                UpdateDetailContent(_currentItem.GetDisplayText());
            }
        }

        private void ButtonQuizMode_Click(object? sender, EventArgs e)
        {
            // 通过切换RadioButton来同步状态
            if (_isQuizMode)
            {
                // 当前是答题模式，切换到学习模式
                radioStudyMode.Checked = true;
            }
            else
            {
                // 当前是学习模式，切换到答题模式
                radioQuickMode.Checked = true;
            }
        }

        private void HideAnswer()
        {
            UpdateDetailState(true, false);
        }

        private void ShowAnswer()
        {
            UpdateDetailState(true, true);
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
        private bool _currentNoteCounted = false;
        private readonly System.Windows.Forms.Timer _noteSaveTimer = new System.Windows.Forms.Timer();

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
                string favoritesPath = Path.Combine(AppPaths.DataDir, "favorites.json");
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
                string favoritesPath = Path.Combine(AppPaths.DataDir, "favorites.json");

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
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "移除收藏失败");
            }
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
                string notesPath = Path.Combine(AppPaths.DataDir, "notes.json");

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
                string notesPath = Path.Combine(AppPaths.DataDir, "notes.json");
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
                    CheckBadgeUnlock();
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

        private bool ContainsChinese(string text)
        {
            return text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
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
                case Keys.Escape:
                    ExitClicked?.Invoke(this, EventArgs.Empty);
                    Close();
                    return true;
                default:
                    return false;
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

                _toolTip?.Dispose();

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
                }

                if (_settingsView != null)
                {
                    _settingsView.RadioStudyMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioQuickMode.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioSequential.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioRandom.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioChinese.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.RadioEnglish.CheckedChanged -= RadioSetting_CheckedChanged;
                    _settingsView.ComboBoxSubCategory.SelectedIndexChanged -= ComboBoxSubCategory_SelectedIndexChanged;
                    _settingsView.ButtonOpenStatistics.Click -= ButtonOpenStatistics_Click;
                    _settingsView.ButtonExportErrorBook.Click -= ButtonExportErrorBook_Click;
                    _settingsView.ButtonQuizMode.Click -= ButtonQuizMode_Click;
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

                _listView?.Dispose();
                _contentView?.Dispose();
                _buttonsView?.Dispose();
                _statsView?.Dispose();
                _settingsView?.Dispose();

                _confettiParticles.Clear();

                _selectedBackgroundBrush.Dispose();
                _selectedForegroundBrush.Dispose();
                _normalForegroundBrush.Dispose();
                _selectedBorderPen.Dispose();

                foreach (var brush in _colorBrushCache.Values)
                {
                    brush.Dispose();
                }
                _colorBrushCache.Clear();
            }

            _disposed = true;
            base.Dispose(disposing);
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
