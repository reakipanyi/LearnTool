using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
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
        private AiQuestionDialog? _aiDialog;
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
            "你真了不起！👏"
        };

        private readonly string[] _correctMessages = {
            "回答正确！🎉",
            "完美！🌟",
            "太棒了！👏",
            "正确！✅",
            "你真聪明！💡"
        };

        private readonly string[] _wrongMessages = {
            "再想想！💭",
            "加油！💪",
            "别灰心！🌈",
            "继续尝试！🔥",
            "下次会更好！🌟"
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

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory, ISoundService soundService, IThemeService themeService)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));


            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;
            Paint += LearningForm_Paint;

            _confettiTimer.Interval = 16;
            _confettiTimer.Tick += ConfettiTimer_Tick;

            _themeService.RegisterThemeable(this);
        }

        private void LearningForm_Paint(object? sender, PaintEventArgs e)
        {
            // 空方法保留，避免编译错误
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (panelContent != null)
            {
                panelContent.BackColor = colors.Surface;
            }

            if (panelAI != null)
            {
                panelAI.BackColor = colors.Surface;
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

            if (labelDisplay != null)
            {
                labelDisplay.ForeColor = colors.TextPrimary;
                labelDisplay.BackColor = colors.Surface;
            }

            if (labelContent != null)
            {
                labelContent.ForeColor = colors.TextPrimary;
                labelContent.BackColor = colors.Surface;
            }

            if (richTextBoxAI != null)
            {
                richTextBoxAI.BackColor = colors.Surface;
                richTextBoxAI.ForeColor = colors.TextPrimary;
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
                _settings.IsAIExplanationEnabled = checkBoxAIExplanation.Checked;
                if (radioOriginal.Checked) _settings.PronunciationScope = 0;
                else if (radioExplanation.Checked) _settings.PronunciationScope = 1;
                else _settings.PronunciationScope = 2;
                _settings.LearningMode = radioStudyMode.Checked ? Constants.LearningMode.Study : Constants.LearningMode.Quick;
                _settings.SortOrder = radioSequential.Checked ? Constants.SortOrder.Sequential : Constants.SortOrder.Random;
                _settings.Language = radioChinese.Checked ? Constants.Language.Chinese : Constants.Language.English;
                _settings.SubCategory = comboBoxSubCategory.Text;

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

                if (checkBoxAIExplanation.Checked != _settings.IsAIExplanationEnabled)
                    checkBoxAIExplanation.Checked = _settings.IsAIExplanationEnabled;

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
            set => labelContent.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string CurrentDisplayText
        {
            set => labelDisplay.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string AIExplanation
        {
            set => richTextBoxAI.Text = value;
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
        public bool IsAIExplanationEnabled
        {
            get => checkBoxAIExplanation.Checked;
            set => checkBoxAIExplanation.Checked = value;
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

        public void SetCurrentItem(LearningItem item)
        {
            _currentItem = item;
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        // 必须在类内部定义这些控件变量（修复编译报错）
        private Panel panelContent;
        private Label labelDisplay;
        private Label labelContent;
        private Panel panelAI;
        private RichTextBox richTextBoxAI;
        private Label labelAI;
        private Button buttonAddToPdf;
        private ProgressBar progressBar1;
        private Label labelStatistics;
        private Button buttonKnown;
        private Button buttonUnknown;
        private Button buttonNext;
        private Button buttonPronounce;
        private Button buttonExit;
        private CheckBox checkBoxVoice;
        private RadioButton radioOriginal;
        private RadioButton radioExplanation;
        private RadioButton radioBoth;
        private Label labelShortcutHints;
        private CheckBox checkBoxAIExplanation;
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
        private FlowLayoutPanel buttonsFlowLayoutPanel;
        private FlowLayoutPanel settingsFlowLayoutPanel;
        private FlowLayoutPanel pronunciationFlowLayoutPanel;
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
        private Button buttonRevealAnswer;
        private Panel panelQuizMode;
        private Button buttonQuizMode;
        private Label labelQuizHint;
        private Button buttonThemeToggle;
        private Button buttonFavorite;
        private Button buttonNote;
        private Panel panelNotes;
        private RichTextBox richTextBoxNotes;
        private Label labelNotesTitle;
        private Panel panelExamples;
        private ListBox listBoxExamples;
        private Label labelExamplesTitle;

        private Panel panelBadges;
        private Label labelBadgesTitle;
        private FlowLayoutPanel flowLayoutPanelBadges;
        private Label labelLevel;
        private ProgressBar progressXP;
        private Label labelXP;
        private Panel panelChallenges;
        private Label labelChallengesTitle;
        private FlowLayoutPanel flowLayoutPanelChallenges;
        private Button buttonMiniGame;
        private Panel panelGame;
        private Label labelGameTitle;
        private Label labelGameQuestion;
        private TextBox textBoxGameAnswer;
        private Button buttonGameSubmit;
        private Label labelGameResult;
        private System.Windows.Forms.Timer _gameTimer;
        private int _gameScore = 0;
        private bool _isGameActive = false;

        private void InitializeComponent()
        {
            panelContent = new Panel();
            labelDisplay = new Label();
            labelContent = new Label();
            panelAI = new Panel();
            richTextBoxAI = new RichTextBox();
            checkBoxAIExplanation = new CheckBox();
            labelAI = new Label();
            buttonAddToPdf = new Button();
            progressBar1 = new ProgressBar();
            labelStatistics = new Label();
            buttonKnown = new Button();
            buttonUnknown = new Button();
            buttonNext = new Button();
            buttonPronounce = new Button();
            buttonExit = new Button();
            checkBoxVoice = new CheckBox();
            radioOriginal = new RadioButton();
            radioExplanation = new RadioButton();
            radioBoth = new RadioButton();
            labelShortcutHints = new Label();
            panelList = new Panel();
            listBoxItems = new ListBox();
            labelListTitle = new Label();
            labelListStatus = new Label();
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
            mainTableLayoutPanel = new TableLayoutPanel();
            middlePanel = new Panel();
            middleTableLayoutPanel = new TableLayoutPanel();
            buttonsFlowLayoutPanel = new FlowLayoutPanel();
            settingsFlowLayoutPanel = new FlowLayoutPanel();
            pronunciationFlowLayoutPanel = new FlowLayoutPanel();
            groupBoxPronunciationScope = new GroupBox();
            panelStats = new Panel();
            labelStudyTime = new Label();
            labelScore = new Label();
            labelTodayCount = new Label();
            labelStreak = new Label();
            labelEncouragement = new Label();
            progressDailyGoal = new ProgressBar();
            labelDailyGoal = new Label();
            buttonRevealAnswer = new Button();
            panelQuizMode = new Panel();
            buttonQuizMode = new Button();
            labelQuizHint = new Label();
            panelContent.SuspendLayout();
            panelStats.SuspendLayout();
            panelQuizMode.SuspendLayout();
            panelAI.SuspendLayout();
            panelList.SuspendLayout();
            panelConfig.SuspendLayout();
            groupBoxMode.SuspendLayout();
            groupBoxSort.SuspendLayout();
            groupBoxLanguage.SuspendLayout();
            mainTableLayoutPanel.SuspendLayout();
            middlePanel.SuspendLayout();
            middleTableLayoutPanel.SuspendLayout();
            buttonsFlowLayoutPanel.SuspendLayout();
            settingsFlowLayoutPanel.SuspendLayout();
            pronunciationFlowLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.FromArgb(224, 224, 224);
            panelContent.Controls.Add(labelDisplay);
            panelContent.Controls.Add(labelContent);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(3, 3);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(1155, 382);
            panelContent.TabIndex = 0;
            panelContent.Paint += PanelContent_Paint;
            // 
            // labelDisplay
            // 
            labelDisplay.BackColor = Color.FromArgb(192, 255, 192);
            labelDisplay.Dock = DockStyle.Top;
            labelDisplay.Font = new Font("微软雅黑", 22F, FontStyle.Bold);
            labelDisplay.ForeColor = Color.FromArgb(100, 150, 180);
            labelDisplay.Location = new Point(0, 0);
            labelDisplay.Name = "labelDisplay";
            labelDisplay.Size = new Size(1155, 89);
            labelDisplay.TabIndex = 1;
            labelDisplay.TextAlign = ContentAlignment.TopCenter;
            // 
            // labelContent
            // 
            labelContent.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            labelContent.BackColor = Color.FromArgb(255, 255, 192);
            labelContent.Font = new Font("微软雅黑", 60F, FontStyle.Bold);
            labelContent.ForeColor = Color.FromArgb(70, 90, 110);
            labelContent.Location = new Point(0, 89);
            labelContent.Name = "labelContent";
            labelContent.Size = new Size(1155, 293);
            labelContent.TabIndex = 0;
            labelContent.TextAlign = ContentAlignment.MiddleCenter;
            labelContent.Click += LabelContent_Click;
            // 
            // panelAI
            // 
            panelAI.BackColor = Color.FromArgb(252, 248, 240);
            panelAI.Controls.Add(richTextBoxAI);
            panelAI.Controls.Add(checkBoxAIExplanation);
            panelAI.Controls.Add(labelAI);
            panelAI.Dock = DockStyle.Fill;
            panelAI.Location = new Point(3, 391);
            panelAI.Name = "panelAI";
            panelAI.Size = new Size(1155, 229);
            panelAI.TabIndex = 1;
            panelAI.Paint += PanelAI_Paint;
            // 
            // richTextBoxAI
            // 
            richTextBoxAI.BackColor = Color.FromArgb(250, 250, 250);
            richTextBoxAI.Dock = DockStyle.Bottom;
            richTextBoxAI.Font = new Font("微软雅黑", 11F);
            richTextBoxAI.ForeColor = Color.FromArgb(60, 80, 100);
            richTextBoxAI.Location = new Point(0, 45);
            richTextBoxAI.Name = "richTextBoxAI";
            richTextBoxAI.ReadOnly = true;
            richTextBoxAI.Size = new Size(1155, 184);
            richTextBoxAI.TabIndex = 1;
            richTextBoxAI.Text = "";
            // 
            // checkBoxAIExplanation
            // 
            checkBoxAIExplanation.AutoSize = true;
            checkBoxAIExplanation.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            checkBoxAIExplanation.ForeColor = Color.FromArgb(70, 90, 110);
            checkBoxAIExplanation.Location = new Point(155, 9);
            checkBoxAIExplanation.Name = "checkBoxAIExplanation";
            checkBoxAIExplanation.Size = new Size(89, 21);
            checkBoxAIExplanation.TabIndex = 6;
            checkBoxAIExplanation.Text = "🤖 AI 释义";
            checkBoxAIExplanation.CheckedChanged += CheckBoxAIExplanation_CheckedChanged;
            // 
            // labelAI
            // 
            labelAI.Dock = DockStyle.Top;
            labelAI.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelAI.ForeColor = Color.FromArgb(140, 100, 80);
            labelAI.Location = new Point(0, 0);
            labelAI.Name = "labelAI";
            labelAI.Padding = new Padding(15, 0, 0, 0);
            labelAI.Size = new Size(1155, 35);
            labelAI.TabIndex = 0;
            labelAI.Text = "📖 AI 释义面板";
            labelAI.TextAlign = ContentAlignment.MiddleLeft;
            labelAI.Click += labelAI_Click;
            // 
            // buttonAddToPdf
            // 
            buttonAddToPdf.BackColor = Color.FromArgb(156, 39, 176);
            buttonAddToPdf.FlatAppearance.BorderSize = 0;
            buttonAddToPdf.FlatStyle = FlatStyle.Flat;
            buttonAddToPdf.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonAddToPdf.ForeColor = Color.White;
            buttonAddToPdf.Location = new Point(575, 10);
            buttonAddToPdf.Margin = new Padding(5);
            buttonAddToPdf.Name = "buttonAddToPdf";
            buttonAddToPdf.Size = new Size(130, 45);
            buttonAddToPdf.TabIndex = 10;
            buttonAddToPdf.Text = "💡 AI提问";
            buttonAddToPdf.UseVisualStyleBackColor = false;
            buttonAddToPdf.Click += ButtonAddToPdf_Click;
            // 
            // progressBar1
            // 
            progressBar1.BackColor = Color.FromArgb(240, 240, 240);
            progressBar1.Dock = DockStyle.Fill;
            progressBar1.ForeColor = Color.FromArgb(255, 140, 0);
            progressBar1.Location = new Point(3, 626);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(1155, 36);
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.TabIndex = 2;
            // 
            // labelStatistics
            // 
            labelStatistics.Dock = DockStyle.Bottom;
            labelStatistics.Font = new Font("微软雅黑", 11F);
            labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            labelStatistics.Location = new Point(3, 671);
            labelStatistics.Name = "labelStatistics";
            labelStatistics.Size = new Size(1155, 28);
            labelStatistics.TabIndex = 3;
            // 
            // buttonKnown
            // 
            buttonKnown.BackColor = Color.FromArgb(76, 175, 80);
            buttonKnown.FlatAppearance.BorderSize = 0;
            buttonKnown.FlatStyle = FlatStyle.Flat;
            buttonKnown.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            buttonKnown.ForeColor = Color.White;
            buttonKnown.Location = new Point(15, 10);
            buttonKnown.Margin = new Padding(5);
            buttonKnown.Name = "buttonKnown";
            buttonKnown.Size = new Size(130, 52);
            buttonKnown.TabIndex = 4;
            buttonKnown.Text = "✅ 会了 [K/1]";
            buttonKnown.UseVisualStyleBackColor = false;
            buttonKnown.Click += ButtonKnown_Click;
            // 
            // buttonUnknown
            // 
            buttonUnknown.BackColor = Color.FromArgb(244, 67, 54);
            buttonUnknown.FlatAppearance.BorderSize = 0;
            buttonUnknown.FlatStyle = FlatStyle.Flat;
            buttonUnknown.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            buttonUnknown.ForeColor = Color.White;
            buttonUnknown.Location = new Point(155, 10);
            buttonUnknown.Margin = new Padding(5);
            buttonUnknown.Name = "buttonUnknown";
            buttonUnknown.Size = new Size(130, 52);
            buttonUnknown.TabIndex = 5;
            buttonUnknown.Text = "❌ 不会 [U/2]";
            buttonUnknown.UseVisualStyleBackColor = false;
            buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // buttonNext
            // 
            buttonNext.BackColor = Color.FromArgb(33, 150, 243);
            buttonNext.FlatAppearance.BorderSize = 0;
            buttonNext.FlatStyle = FlatStyle.Flat;
            buttonNext.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            buttonNext.ForeColor = Color.White;
            buttonNext.Location = new Point(295, 10);
            buttonNext.Margin = new Padding(5);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(130, 52);
            buttonNext.TabIndex = 6;
            buttonNext.Text = "➡ 下一个 [Enter]";
            buttonNext.UseVisualStyleBackColor = false;
            buttonNext.Click += ButtonNext_Click;
            // 
            // buttonPronounce
            // 
            buttonPronounce.BackColor = Color.FromArgb(0, 188, 212);
            buttonPronounce.FlatAppearance.BorderSize = 0;
            buttonPronounce.FlatStyle = FlatStyle.Flat;
            buttonPronounce.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonPronounce.ForeColor = Color.White;
            buttonPronounce.Location = new Point(435, 10);
            buttonPronounce.Margin = new Padding(5);
            buttonPronounce.Name = "buttonPronounce";
            buttonPronounce.Size = new Size(130, 45);
            buttonPronounce.TabIndex = 7;
            buttonPronounce.Text = "🔊 发音 [Space]";
            buttonPronounce.UseVisualStyleBackColor = false;
            buttonPronounce.Click += ButtonPronounce_Click;
            // 
            // buttonExit
            // 
            buttonExit.BackColor = Color.FromArgb(108, 117, 125);
            buttonExit.FlatAppearance.BorderSize = 0;
            buttonExit.FlatStyle = FlatStyle.Flat;
            buttonExit.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonExit.ForeColor = Color.White;
            buttonExit.Location = new Point(715, 10);
            buttonExit.Margin = new Padding(5);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(130, 45);
            buttonExit.TabIndex = 8;
            buttonExit.Text = "🏠 返回";
            buttonExit.UseVisualStyleBackColor = false;
            buttonExit.Click += ButtonExit_Click;
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
            // labelShortcutHints
            // 
            labelShortcutHints.AutoSize = true;
            labelShortcutHints.Dock = DockStyle.Bottom;
            labelShortcutHints.Font = new Font("Microsoft YaHei UI", 9F);
            labelShortcutHints.ForeColor = Color.FromArgb(120, 120, 120);
            labelShortcutHints.Location = new Point(3, 821);
            labelShortcutHints.Name = "labelShortcutHints";
            labelShortcutHints.Size = new Size(1155, 17);
            labelShortcutHints.TabIndex = 16;
            labelShortcutHints.Text = "快捷键: 空格-发音 | 回车-下一个 | 1/K-会了 | 2/U-不会 | Esc-返回";
            // 
            // panelList
            // 
            panelList.BackColor = Color.FromArgb(248, 248, 252);
            panelList.BorderStyle = BorderStyle.FixedSingle;
            panelList.Controls.Add(labelListStatus);
            panelList.Controls.Add(listBoxItems);
            panelList.Controls.Add(labelListTitle);
            panelList.Dock = DockStyle.Fill;
            panelList.Location = new Point(3, 3);
            panelList.Name = "panelList";
            panelList.Size = new Size(194, 838);
            panelList.TabIndex = 18;
            // 
            // listBoxItems
            // 
            listBoxItems.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            listBoxItems.Font = new Font("微软雅黑", 10F);
            listBoxItems.FormattingEnabled = true;
            listBoxItems.ItemHeight = 19;
            listBoxItems.Location = new Point(0, 35);
            listBoxItems.Name = "listBoxItems";
            listBoxItems.Size = new Size(192, 778);
            listBoxItems.TabIndex = 1;
            listBoxItems.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;
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
            // labelListStatus
            // 
            labelListStatus.BackColor = Color.FromArgb(240, 240, 245);
            labelListStatus.Dock = DockStyle.Bottom;
            labelListStatus.Font = new Font("微软雅黑", 9F);
            labelListStatus.ForeColor = Color.FromArgb(80, 100, 120);
            labelListStatus.Location = new Point(0, 813);
            labelListStatus.Name = "labelListStatus";
            labelListStatus.Size = new Size(192, 23);
            labelListStatus.TabIndex = 2;
            labelListStatus.Text = "共 0 项";
            labelListStatus.TextAlign = ContentAlignment.MiddleCenter;
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
            radioStudyMode.Size = new Size(69, 21);
            radioStudyMode.TabIndex = 0;
            radioStudyMode.TabStop = true;
            radioStudyMode.Text = "📚 学习";
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
            radioQuickMode.Text = "⚡ 快速";
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
            panelStats.BackColor = Color.FromArgb(248, 249, 251);
            panelStats.BorderStyle = BorderStyle.FixedSingle;
            panelStats.Controls.Add(labelStudyTime);
            panelStats.Controls.Add(labelScore);
            panelStats.Controls.Add(labelTodayCount);
            panelStats.Controls.Add(labelStreak);
            panelStats.Controls.Add(labelEncouragement);
            panelStats.Controls.Add(progressDailyGoal);
            panelStats.Controls.Add(labelDailyGoal);
            panelStats.Dock = DockStyle.Fill;
            panelStats.Location = new Point(10, 445);
            panelStats.Name = "panelStats";
            panelStats.Size = new Size(180, 200);
            panelStats.TabIndex = 8;
            // 
            // labelStudyTime
            // 
            labelStudyTime.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelStudyTime.ForeColor = Color.FromArgb(66, 133, 244);
            labelStudyTime.Location = new Point(10, 10);
            labelStudyTime.Name = "labelStudyTime";
            labelStudyTime.Size = new Size(160, 25);
            labelStudyTime.TabIndex = 0;
            labelStudyTime.Text = "⏱️ 学习时长: 00:00";
            // 
            // labelScore
            // 
            labelScore.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelScore.ForeColor = Color.FromArgb(255, 152, 0);
            labelScore.Location = new Point(10, 35);
            labelScore.Name = "labelScore";
            labelScore.Size = new Size(160, 25);
            labelScore.TabIndex = 1;
            labelScore.Text = "🏆 得分: 0";
            // 
            // labelTodayCount
            // 
            labelTodayCount.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelTodayCount.ForeColor = Color.FromArgb(76, 175, 80);
            labelTodayCount.Location = new Point(10, 60);
            labelTodayCount.Name = "labelTodayCount";
            labelTodayCount.Size = new Size(160, 25);
            labelTodayCount.TabIndex = 2;
            labelTodayCount.Text = "📚 今日学习: 0 项";
            // 
            // labelStreak
            // 
            labelStreak.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelStreak.ForeColor = Color.FromArgb(156, 39, 176);
            labelStreak.Location = new Point(10, 85);
            labelStreak.Name = "labelStreak";
            labelStreak.Size = new Size(160, 25);
            labelStreak.TabIndex = 3;
            labelStreak.Text = "🔥 连续学习: 0 天";
            // 
            // labelEncouragement
            // 
            labelEncouragement.Font = new Font("微软雅黑", 10F);
            labelEncouragement.ForeColor = Color.FromArgb(100, 100, 100);
            labelEncouragement.Location = new Point(10, 110);
            labelEncouragement.Name = "labelEncouragement";
            labelEncouragement.Size = new Size(160, 25);
            labelEncouragement.TabIndex = 4;
            labelEncouragement.Text = "💪 加油！";
            // 
            // progressDailyGoal
            // 
            progressDailyGoal.Location = new Point(10, 145);
            progressDailyGoal.Maximum = 50;
            progressDailyGoal.Name = "progressDailyGoal";
            progressDailyGoal.Size = new Size(160, 15);
            progressDailyGoal.TabIndex = 5;
            progressDailyGoal.Value = 0;
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
            // buttonRevealAnswer
            // 
            buttonRevealAnswer.BackColor = Color.FromArgb(66, 133, 244);
            buttonRevealAnswer.FlatAppearance.BorderSize = 0;
            buttonRevealAnswer.FlatStyle = FlatStyle.Flat;
            buttonRevealAnswer.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonRevealAnswer.ForeColor = Color.White;
            buttonRevealAnswer.Location = new Point(655, 10);
            buttonRevealAnswer.Margin = new Padding(5);
            buttonRevealAnswer.Name = "buttonRevealAnswer";
            buttonRevealAnswer.Size = new Size(130, 45);
            buttonRevealAnswer.TabIndex = 11;
            buttonRevealAnswer.Text = "👁️ 显示答案";
            buttonRevealAnswer.UseVisualStyleBackColor = false;
            buttonRevealAnswer.Click += ButtonRevealAnswer_Click;
            buttonRevealAnswer.Visible = false;
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
            labelQuizHint.Font = new Font("微软雅黑", 8F);
            labelQuizHint.ForeColor = Color.FromArgb(139, 119, 101);
            labelQuizHint.Location = new Point(10, 50);
            labelQuizHint.Name = "labelQuizHint";
            labelQuizHint.Size = new Size(160, 25);
            labelQuizHint.TabIndex = 1;
            labelQuizHint.Text = "先隐藏答案，测试自己";
            // 
            // buttonThemeToggle
            // 
            buttonThemeToggle = new Button();
            buttonThemeToggle.BackColor = Color.FromArgb(103, 58, 183);
            buttonThemeToggle.FlatAppearance.BorderSize = 0;
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
            // buttonFavorite
            // 
            buttonFavorite = new Button();
            buttonFavorite.BackColor = Color.FromArgb(255, 193, 7);
            buttonFavorite.FlatAppearance.BorderSize = 0;
            buttonFavorite.FlatStyle = FlatStyle.Flat;
            buttonFavorite.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonFavorite.ForeColor = Color.White;
            buttonFavorite.Location = new Point(855, 10);
            buttonFavorite.Margin = new Padding(5);
            buttonFavorite.Name = "buttonFavorite";
            buttonFavorite.Size = new Size(100, 45);
            buttonFavorite.TabIndex = 12;
            buttonFavorite.Text = "⭐ 收藏";
            buttonFavorite.UseVisualStyleBackColor = false;
            buttonFavorite.Click += ButtonFavorite_Click;
            // 
            // buttonNote
            // 
            buttonNote = new Button();
            buttonNote.BackColor = Color.FromArgb(76, 175, 80);
            buttonNote.FlatAppearance.BorderSize = 0;
            buttonNote.FlatStyle = FlatStyle.Flat;
            buttonNote.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            buttonNote.ForeColor = Color.White;
            buttonNote.Location = new Point(965, 10);
            buttonNote.Margin = new Padding(5);
            buttonNote.Name = "buttonNote";
            buttonNote.Size = new Size(100, 45);
            buttonNote.TabIndex = 13;
            buttonNote.Text = "📝 笔记";
            buttonNote.UseVisualStyleBackColor = false;
            buttonNote.Click += ButtonNote_Click;
            // 
            // panelNotes
            // 
            panelNotes = new Panel();
            panelNotes.BackColor = Color.FromArgb(255, 253, 238);
            panelNotes.BorderStyle = BorderStyle.FixedSingle;
            panelNotes.Controls.Add(richTextBoxNotes);
            panelNotes.Controls.Add(labelNotesTitle);
            panelNotes.Dock = DockStyle.Fill;
            panelNotes.Location = new Point(3, 3);
            panelNotes.Name = "panelNotes";
            panelNotes.Size = new Size(1155, 150);
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
            richTextBoxNotes.Size = new Size(1153, 118);
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
            labelNotesTitle.Size = new Size(1155, 30);
            labelNotesTitle.TabIndex = 0;
            labelNotesTitle.Text = "📝 我的笔记";
            labelNotesTitle.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // panelExamples
            // 
            panelExamples = new Panel();
            panelExamples.BackColor = Color.FromArgb(248, 250, 252);
            panelExamples.BorderStyle = BorderStyle.FixedSingle;
            panelExamples.Controls.Add(listBoxExamples);
            panelExamples.Controls.Add(labelExamplesTitle);
            panelExamples.Dock = DockStyle.Fill;
            panelExamples.Location = new Point(3, 3);
            panelExamples.Name = "panelExamples";
            panelExamples.Size = new Size(1155, 120);
            panelExamples.TabIndex = 8;
            panelExamples.Visible = false;
            // 
            // listBoxExamples
            // 
            listBoxExamples.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxExamples.BackColor = Color.FromArgb(248, 250, 252);
            listBoxExamples.Font = new Font("微软雅黑", 10F);
            listBoxExamples.FormattingEnabled = true;
            listBoxExamples.ItemHeight = 22;
            listBoxExamples.Location = new Point(0, 30);
            listBoxExamples.Name = "listBoxExamples";
            listBoxExamples.Size = new Size(1153, 88);
            listBoxExamples.TabIndex = 1;
            // 
            // labelExamplesTitle
            // 
            labelExamplesTitle.Dock = DockStyle.Top;
            labelExamplesTitle.Font = new Font("微软雅黑", 11F, FontStyle.Bold);
            labelExamplesTitle.ForeColor = Color.FromArgb(66, 133, 244);
            labelExamplesTitle.Location = new Point(0, 0);
            labelExamplesTitle.Name = "labelExamplesTitle";
            labelExamplesTitle.Padding = new Padding(10, 0, 0, 0);
            labelExamplesTitle.Size = new Size(1155, 30);
            labelExamplesTitle.TabIndex = 0;
            labelExamplesTitle.Text = "📚 例句参考";
            labelExamplesTitle.TextAlign = ContentAlignment.MiddleLeft;
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
            middleTableLayoutPanel.Controls.Add(panelAI, 0, 1);
            middleTableLayoutPanel.Controls.Add(progressBar1, 0, 2);
            middleTableLayoutPanel.Controls.Add(buttonsFlowLayoutPanel, 0, 4);
            middleTableLayoutPanel.Controls.Add(settingsFlowLayoutPanel, 0, 5);
            middleTableLayoutPanel.Controls.Add(labelShortcutHints, 0, 6);
            middleTableLayoutPanel.Controls.Add(labelStatistics, 0, 3);
            middleTableLayoutPanel.Dock = DockStyle.Fill;
            middleTableLayoutPanel.Location = new Point(0, 0);
            middleTableLayoutPanel.Name = "middleTableLayoutPanel";
            middleTableLayoutPanel.RowCount = 7;
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 75F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            middleTableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));
            middleTableLayoutPanel.Size = new Size(1161, 838);
            middleTableLayoutPanel.TabIndex = 0;
            // 
            // buttonsFlowLayoutPanel
            // 
            buttonsFlowLayoutPanel.Controls.Add(buttonKnown);
            buttonsFlowLayoutPanel.Controls.Add(buttonUnknown);
            buttonsFlowLayoutPanel.Controls.Add(buttonNext);
            buttonsFlowLayoutPanel.Controls.Add(buttonPronounce);
            buttonsFlowLayoutPanel.Controls.Add(buttonAddToPdf);
            buttonsFlowLayoutPanel.Controls.Add(buttonRevealAnswer);
            buttonsFlowLayoutPanel.Controls.Add(buttonFavorite);
            buttonsFlowLayoutPanel.Controls.Add(buttonNote);
            buttonsFlowLayoutPanel.Controls.Add(buttonExit);
            buttonsFlowLayoutPanel.Dock = DockStyle.Fill;
            buttonsFlowLayoutPanel.Location = new Point(3, 702);
            buttonsFlowLayoutPanel.Name = "buttonsFlowLayoutPanel";
            buttonsFlowLayoutPanel.Padding = new Padding(10, 5, 10, 5);
            buttonsFlowLayoutPanel.Size = new Size(1155, 69);
            buttonsFlowLayoutPanel.TabIndex = 4;
            buttonsFlowLayoutPanel.WrapContents = false;
            // 
            // settingsFlowLayoutPanel
            // 
            settingsFlowLayoutPanel.Controls.Add(checkBoxVoice);
            settingsFlowLayoutPanel.Controls.Add(pronunciationFlowLayoutPanel);
            settingsFlowLayoutPanel.Dock = DockStyle.Fill;
            settingsFlowLayoutPanel.Location = new Point(3, 777);
            settingsFlowLayoutPanel.Name = "settingsFlowLayoutPanel";
            settingsFlowLayoutPanel.Padding = new Padding(10, 5, 10, 5);
            settingsFlowLayoutPanel.Size = new Size(1155, 38);
            settingsFlowLayoutPanel.TabIndex = 5;
            settingsFlowLayoutPanel.WrapContents = false;
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
            // groupBoxPronunciationScope
            // 
            groupBoxPronunciationScope.Location = new Point(0, 0);
            groupBoxPronunciationScope.Name = "groupBoxPronunciationScope";
            groupBoxPronunciationScope.Size = new Size(200, 100);
            groupBoxPronunciationScope.TabIndex = 0;
            groupBoxPronunciationScope.TabStop = false;
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
            panelAI.ResumeLayout(false);
            panelAI.PerformLayout();
            panelList.ResumeLayout(false);
            panelConfig.ResumeLayout(false);
            groupBoxMode.ResumeLayout(false);
            groupBoxMode.PerformLayout();
            groupBoxSort.ResumeLayout(false);
            groupBoxSort.PerformLayout();
            groupBoxLanguage.ResumeLayout(false);
            groupBoxLanguage.PerformLayout();
            mainTableLayoutPanel.ResumeLayout(false);
            middlePanel.ResumeLayout(false);
            middleTableLayoutPanel.ResumeLayout(false);
            middleTableLayoutPanel.PerformLayout();
            buttonsFlowLayoutPanel.ResumeLayout(false);
            settingsFlowLayoutPanel.ResumeLayout(false);
            pronunciationFlowLayoutPanel.ResumeLayout(false);
            pronunciationFlowLayoutPanel.PerformLayout();
            panelStats.ResumeLayout(false);
            panelStats.PerformLayout();
            panelQuizMode.ResumeLayout(false);
            panelQuizMode.PerformLayout();
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
        /// 更新鼓励语显示
        /// </summary>
        private void UpdateEncouragement()
        {
            if (labelEncouragement != null)
            {
                labelEncouragement.Text = _encouragements[_random.Next(_encouragements.Length)];
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

        private void LabelContent_Click(object? sender, EventArgs e)
        {
            PronounceClicked?.Invoke(this, EventArgs.Empty);
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
                SettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void RadioEnglish_CheckedChanged(object? sender, EventArgs e)
        {
            if (radioEnglish.Checked && !_settingsChangedEventsSuspended)
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

        private void ButtonKnown_Click(object? sender, EventArgs e)
        {
            _soundService?.PlaySuccess();
            StartConfetti();

            int points = _isQuizMode && !_answerRevealed ? 20 : 10;
            IncrementScore(points);

            _totalLearnedCount++;
            if (_isQuizMode && !_answerRevealed)
            {
                _quizCorrectCount++;
                labelDisplay.Text = _correctMessages[_random.Next(_correctMessages.Length)];
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
                labelDisplay.Text = _wrongMessages[_random.Next(_wrongMessages.Length)];
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

        private void ShakeWindow()
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
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(stepDelay);
            }

            Location = originalLocation;
        }

        private void ButtonExit_Click(object? sender, EventArgs e)
        {
            Close();
            ExitClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonAddToPdf_Click(object? sender, EventArgs e)
        {
            if (_aiDialog == null || _aiDialog.IsDisposed)
            {
                var aiLogger = _loggerFactory.CreateLogger<AiQuestionDialog>();
                _aiDialog = new AiQuestionDialog(_aiQuestionService, _ttsService, aiLogger);
            }

            if (_currentItem != null)
            {
                _aiDialog.QuestionText = _currentItem.GetMainContent();
            }

            _aiDialog.Show();
            _aiDialog.BringToFront();
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
            labelDisplay.Visible = true;

            if (_currentItem != null)
            {
                labelDisplay.Text = _currentItem.GetDisplayText();
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
            if (labelDisplay != null)
            {
                labelDisplay.Text = "❓ 请猜测答案";
            }
            _answerRevealed = false;
            buttonRevealAnswer.Visible = true;
        }

        private void ShowAnswer()
        {
            if (labelDisplay != null && _currentItem != null)
            {
                labelDisplay.Text = _currentItem.GetDisplayText();
            }
            _answerRevealed = true;
            buttonRevealAnswer.Visible = false;
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
                buttonFavorite.BackColor = Color.FromArgb(255, 152, 0);
                buttonFavorite.Text = "❤️ 已收藏";
                _soundService?.PlaySuccess();
                _favoriteCount++;
                SaveFavorite();
                CheckBadgeUnlock();
                UpdateChallengesProgress();
            }
            else
            {
                buttonFavorite.BackColor = Color.FromArgb(255, 193, 7);
                buttonFavorite.Text = "⭐ 收藏";
                _favoriteCount = Math.Max(0, _favoriteCount - 1);
                RemoveFavorite();
            }
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

                string content = _currentItem.GetMainContent();
                if (!favorites.Contains(content))
                {
                    favorites.Add(content);
                    string json = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(favoritesPath, json);
                }
            }
            catch
            {
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

                    string content = _currentItem.GetMainContent();
                    favorites.Remove(content);

                    string newJson = JsonSerializer.Serialize(favorites, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(favoritesPath, newJson);
                }
            }
            catch
            {
            }
        }

        private void ButtonNote_Click(object? sender, EventArgs e)
        {
            panelNotes.Visible = !panelNotes.Visible;

            if (panelNotes.Visible)
            {
                buttonNote.BackColor = Color.FromArgb(156, 39, 176);
                buttonNote.Text = "📝 笔记 (开)";
                LoadNotes();
            }
            else
            {
                buttonNote.BackColor = Color.FromArgb(76, 175, 80);
                buttonNote.Text = "📝 笔记";
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

                    string key = _currentItem.GetMainContent();
                    if (notesDict.TryGetValue(key, out string note))
                    {
                        richTextBoxNotes.Text = note;
                    }
                    else
                    {
                        richTextBoxNotes.Text = "";
                    }
                }
                else
                {
                    richTextBoxNotes.Text = "";
                }
            }
            catch
            {
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

                string key = _currentItem.GetMainContent();
                notesDict[key] = richTextBoxNotes.Text;

                string jsonOutput = JsonSerializer.Serialize(notesDict, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(notesPath, jsonOutput);
            }
            catch
            {
            }
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

        private void LoadExamples()
        {
            if (_currentItem == null || listBoxExamples == null) return;

            listBoxExamples.Items.Clear();

            var examples = GetExamplesForItem(_currentItem.GetMainContent());
            foreach (var example in examples)
            {
                listBoxExamples.Items.Add(example);
            }

            panelExamples.Visible = examples.Count > 0;
        }

        private List<string> GetExamplesForItem(string content)
        {
            var examples = new List<string>();

            if (content.Length >= 2)
            {
                examples.Add($"这个词可以这样用：{content}是一个常用词。");
                examples.Add($"例如：我今天学到了'{content}'这个词。");
                examples.Add($"在句子中使用：学习'{content}'让我受益匪浅。");
            }

            return examples;
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
