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
        #region === 依赖服务 ===
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISoundService _soundService;
        private readonly IThemeService _themeService;
        private readonly IAIPanelPopupService? _aiPanelPopupService;
        private readonly IEncouragementService _encouragementService;
        private readonly BadgeManager _badgeManager;
        private readonly StudyStatsManager _statsManager;
        private readonly ChallengeManager _challengeManager;
        private readonly ConfettiManager _confettiManager;
        private readonly EncouragementManager _encouragementManager;
        #endregion

        #region === 学习状态 ===
        private LearningItem? _currentItem;
        private bool _isShowAnswer = false;      // 答题模式标志
        private bool _answerRevealed = false;    // 答案是否已揭示
        private bool _isFavorite = false;
        private bool _currentNoteCounted = false;
        private bool _disposed = false;
        private Settings _settings = new();
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
        #endregion

        #region === 子视图实例 ===
        private LearningListView _listView = null!;
        private LearningContentView _contentView = null!;
        private LearningButtonsView _buttonsView = null!;
        private LearningStatsView _statsView = null!;
        private LearningSettingsView _settingsView = null!;
        #endregion

        #region === 控件访问器 ===
        private Panel panelContent => _contentView.PanelContent;
        private ListBox listBoxDisplay => _contentView.ListBoxDisplay;
        private Label labelContent => _contentView.LabelContent;
        private Label labelStatistics => _statsView.LabelStatistics;
        private ListBox listBoxItems => _listView.ListBoxItems;
        private Label labelListStatus => _listView.LabelListStatus;
        private Panel panelConfig => _settingsView.PanelConfig;
        private Panel panelStats => _statsView.PanelStatsContainer;
        private Label labelStudyTime => _statsView.LabelStudyTime;
        private Label labelScore => _statsView.LabelScore;
        private Label labelTodayCount => _statsView.LabelTodayCount;
        private Label labelStreak => _statsView.LabelStreak;
        private Label labelEncouragement => _statsView.LabelEncouragement;
        private ProgressBar progressBar1 => _statsView.ProgressBar;
        private Panel panelQuizMode => _settingsView.PanelQuizMode;
        private Button buttonShowAnswer => _settingsView.ButtonShowAnswer;
        private Label labelQuizHint => _settingsView.LabelQuizHint;
        private Button buttonThemeToggle => _settingsView.ButtonThemeToggle;
        private CheckBox checkBoxVoice => _settingsView.CheckBoxVoice;
        private FlowLayoutPanel pronunciationFlowLayoutPanel => _settingsView.PronunciationFlowLayoutPanel;
        private RadioButton radioOriginal => _settingsView.RadioOriginal;
        private RadioButton radioExplanation => _settingsView.RadioExplanation;
        private RadioButton radioBoth => _settingsView.RadioBoth;
        private RadioButton radioStudyMode => _settingsView.RadioStudyMode;
        private RadioButton radioQuickMode => _settingsView.RadioQuickMode;
        private RadioButton radioSequential => _settingsView.RadioSequential;
        private RadioButton radioRandom => _settingsView.RadioRandom;
        private RadioButton radioChinese => _settingsView.RadioChinese;
        private RadioButton radioEnglish => _settingsView.RadioEnglish;
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
            IEncouragementService encouragementService)
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

            _badgeManager = new BadgeManager(_loggerFactory.CreateLogger<BadgeManager>());
            _badgeManager.BadgesUnlocked += OnBadgesUnlocked;

            _encouragementManager = new EncouragementManager();

            _statsManager = new StudyStatsManager(
                _loggerFactory.CreateLogger<StudyStatsManager>(),
                OnLevelUp,
                score => { },
                xp => { });

            _challengeManager = new ChallengeManager(
                _loggerFactory.CreateLogger<ChallengeManager>(),
                points => _statsManager.AddScore(points),
                xp => _statsManager.AddXP(xp),
                OnLevelUp,
                () => { });

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

            _settingsView.RadioStudyMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioQuickMode.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioSequential.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioRandom.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioChinese.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.RadioEnglish.CheckedChanged += RadioSetting_CheckedChanged;
            _settingsView.ComboBoxSubCategory.SelectedIndexChanged += ComboBoxSubCategory_SelectedIndexChanged;
            _settingsView.ButtonOpenStatistics.Click += ButtonOpenStatistics_Click;
            _settingsView.ButtonExportErrorBook.Click += ButtonExportErrorBook_Click;
            _settingsView.ButtonShowAnswer.Click += ButtonShowAnswer_Click;
            _settingsView.ButtonThemeToggle.Click += ButtonThemeToggle_Click;

            _contentView.ContentClicked += LabelContent_Click;
            _contentView.DetailClicked += ListBoxDisplay_Click;
            _contentView.NoteTextChanged += RichTextBoxNotes_TextChanged;

            _listView.SelectedIndexChanged += ListBoxItems_SelectedIndexChanged;
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

                // 同步 _isShowAnswer 状态
                _isShowAnswer = radioQuickMode.Checked;

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

        private void SuspendSettingsChangedEvents() => _settingsChangedEventsSuspended = true;
        private void ResumeSettingsChangedEvents() => _settingsChangedEventsSuspended = false;

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

            if (flowLayoutPanelBadges != null)
            {
                _badgeManager.SetUI(flowLayoutPanelBadges, _toolTip);
            }

            if (labelStudyTime != null && labelScore != null && labelTodayCount != null &&
                labelStreak != null && labelLevel != null && labelXP != null && progressXP != null)
            {
                _statsManager.SetUI(labelStudyTime, labelScore, labelTodayCount, labelStreak, labelLevel, labelXP, progressXP);
            }

            if (flowLayoutPanelChallenges != null)
            {
                _challengeManager.SetUI(flowLayoutPanelChallenges, _soundService);
            }

            _confettiManager.SetTargetControl(mainTableLayoutPanel);

            _statsManager.Load();
            _badgeManager.Load();
            _challengeManager.Load();
            UpdateEncouragement();
            _badgeManager.UpdateDisplay();
            _challengeManager.UpdateDisplay();
        }

        private void OnLevelUp()
        {
            _soundService?.PlaySuccess();
            StartConfetti();
            string levelTitle = _statsManager.LevelTitle;
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                    MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{levelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information)));
            }
            else
            {
                MessageBox.Show($"🎉 恭喜升级！\n\n你现在是「{levelTitle}」级别！", "升级成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void StudyTimer_Tick(object? sender, EventArgs e)
        {
            _studyDuration = _studyDuration.Add(TimeSpan.FromSeconds(1));
            _statsManager.UpdateStudyTime(_studyDuration);
        }

        #endregion

        #region List Management

        public void UpdateLearningList(List<string> items, int currentIndex)
        {
            if (listBoxItems == null) return;

            // 确保在UI线程上执行，避免跨线程问题
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLearningList(items, currentIndex)));
                return;
            }

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

            // 确保在UI线程上执行，避免跨线程问题
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateLearningListSelection(currentIndex)));
                return;
            }

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
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _confettiManager.Draw(e.Graphics);
        }

        #endregion

        #region Event Handlers



        /// <summary>
        /// 处理内容区点击事件（答题模式下切换答案显示）
        /// </summary>
        private void ContentArea_Click(object? sender, EventArgs e)
        {
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

        // 为了兼容性保留原来的事件处理方法名
        private void LabelContent_Click(object? sender, EventArgs e) => ContentArea_Click(sender, e);
        private void ListBoxDisplay_Click(object? sender, EventArgs e) => ContentArea_Click(sender, e);

        /// <summary>
        /// 更新详情区内容
        /// </summary>
        /// <param name="text">要显示的文本</param>
        private void UpdateDetailContent(string text)
        {
            if (listBoxDisplay == null) return;

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
            if (listBoxDisplay == null) return;

            listBoxDisplay.Visible = visible;

            if (visible && _currentItem != null)
            {
                if (_isShowAnswer)
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

        private void OnBadgesUnlocked(List<string> badges)
        {
            _badgeManager.ShowNotification(badges);
            _badgeManager.UpdateDisplay();
            _statsManager.AddScore(50 * badges.Count);
            _statsManager.AddXP(50 * badges.Count);
        }

        

        private void UpdateChallengesProgress()
        {
            _challengeManager.SetLearningData(_statsManager.TodayLearnedCount, _quizCorrectCount, _favoriteCount);
            _challengeManager.UpdateProgress();
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
                _statsManager.AddScore(10);
                _statsManager.AddXP(10);
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

                int points = _isShowAnswer && !_answerRevealed ? 20 : 10;
                _statsManager.AddScore(points);
                _statsManager.IncrementLearnedCount();

                _totalLearnedCount++;
                if (_isShowAnswer && !_answerRevealed)
                {
                    _quizCorrectCount++;
                }

                UpdateEncouragement();
                _badgeManager.CheckUnlock(_totalLearnedCount, _statsManager.StreakDays, _statsManager.TodayLearnedCount, _quizCorrectCount, _favoriteCount, _noteCount);
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

        private void ButtonShowAnswer_Click(object? sender, EventArgs e)
        {
            // 直接切换显示内容，不联动 RadioButton，不刷新列表
            if (_isShowAnswer)
            {
                // 当前是答题模式，切换到学习模式
                _isShowAnswer = false;
                UpdateDetailState(true, true);
                buttonShowAnswer.BackColor = Color.FromArgb(255, 193, 7);
                buttonShowAnswer.Text = "🎮 答题模式";
                labelQuizHint.Text = "学习模式，显示完整内容";
            }
            else
            {
                // 当前是学习模式，切换到答题模式
                _isShowAnswer = true;
                UpdateDetailState(true, false);
                buttonShowAnswer.BackColor = Color.FromArgb(76, 175, 80);
                buttonShowAnswer.Text = "📖 学习模式";
                labelQuizHint.Text = "答案已隐藏";
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

        private void ButtonFavorite_Click(object? sender, EventArgs e)
        {
            _isFavorite = !_isFavorite;

            if (_isFavorite)
            {
                _soundService?.PlaySuccess();
                _favoriteCount++;
                SaveFavorite();
                _badgeManager.CheckUnlock(_totalLearnedCount, _statsManager.StreakDays, _statsManager.TodayLearnedCount, _quizCorrectCount, _favoriteCount, _noteCount);
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
                    _badgeManager.CheckUnlock(_totalLearnedCount, _statsManager.StreakDays, _statsManager.TodayLearnedCount, _quizCorrectCount, _favoriteCount, _noteCount);
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

                _listView?.Dispose();
                _contentView?.Dispose();
                _buttonsView?.Dispose();
                _statsView?.Dispose();
                _settingsView?.Dispose();

                _selectedBackgroundBrush.Dispose();
                _selectedForegroundBrush.Dispose();
                _normalForegroundBrush.Dispose();
                _selectedBorderPen.Dispose();
            }

            _disposed = true;
            base.Dispose(disposing);
        }
    }
}
