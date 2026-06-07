using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Feedback;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LearningAssistant.Forms
{
    public partial class LearningForm : Form, ILearningView
    {
        private LearningItem? _currentItem;
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ISoundService _soundService;
        private AiQuestionDialog? _aiDialog;
        private bool _disposed = false;
        private Settings _settings = new();

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory, ISoundService soundService)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = soundService ?? throw new ArgumentNullException(nameof(soundService));



            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;
        }



        private void LearningForm_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            ApplySettings();
            //UpdateControlLayout();
        }

        private void LearningForm_Resize(object? sender, EventArgs e)
        {
            //UpdateControlLayout();
        }

        private void UpdateControlLayout()
        {
            if (panelContent == null || panelAI == null || panelConfig == null || panelList == null) return;

            int clientWidth = ClientSize.Width;
            int clientHeight = ClientSize.Height;
            int spacing = 25;
            int configPanelWidth = 200;
            int listPanelWidth = 220;
            int middleStartX = configPanelWidth + spacing * 2;
            int middleWidth = clientWidth - middleStartX - listPanelWidth - spacing;
            int contentPanelHeight = 200;
            int aiPanelHeight = 180;

            // 左侧配置面板
            panelConfig.Location = new Point(spacing, spacing);
            panelConfig.Size = new Size(configPanelWidth, clientHeight - spacing * 2);

            // 右侧学习列表
            panelList.Location = new Point(clientWidth - listPanelWidth - spacing, spacing);
            panelList.Size = new Size(listPanelWidth, clientHeight - spacing * 2);

            // 中间内容区域 - panelContent
            panelContent.Location = new Point(middleStartX, spacing);
            panelContent.Size = new Size(middleWidth, contentPanelHeight);

            // 中间内容区域 - panelAI
            panelAI.Location = new Point(middleStartX, spacing + contentPanelHeight + spacing);
            panelAI.Size = new Size(middleWidth, aiPanelHeight);

            // 进度条
            progressBar1.Location = new Point(middleStartX, spacing + contentPanelHeight + spacing + aiPanelHeight + spacing);
            progressBar1.Size = new Size(middleWidth, 28);

            // 统计信息
            labelStatistics.Location = new Point(middleStartX, spacing + contentPanelHeight + spacing + aiPanelHeight + spacing + 33);
            labelStatistics.Size = new Size(middleWidth, 28);

            // 按钮区域 - 会了/不会/下一个
            int buttonY = labelStatistics.Location.Y + labelStatistics.Height + spacing;
            int buttonWidth = 130;
            int buttonHeight = 52;
            int buttonSpacing = 20;
            int totalButtonsWidth = buttonWidth * 3 + buttonSpacing * 2;
            int buttonsStartX = middleStartX + (middleWidth - totalButtonsWidth) / 2;

            buttonKnown.Location = new Point(buttonsStartX, buttonY);
            buttonKnown.Size = new Size(buttonWidth, buttonHeight);

            buttonUnknown.Location = new Point(buttonsStartX + buttonWidth + buttonSpacing, buttonY);
            buttonUnknown.Size = new Size(buttonWidth, buttonHeight);

            buttonNext.Location = new Point(buttonsStartX + (buttonWidth + buttonSpacing) * 2, buttonY);
            buttonNext.Size = new Size(buttonWidth, buttonHeight);

            // AI提问按钮
            buttonAddToPdf.Location = new Point(middleStartX + middleWidth - 130, buttonY + 3);
            buttonAddToPdf.Size = new Size(130, 45);

            // 发音和设置按钮
            int bottomButtonY = buttonY + buttonHeight + spacing;
            buttonPronounce.Location = new Point(middleStartX, bottomButtonY);
            buttonPronounce.Size = new Size(120, 40);

            checkBoxVoice.Location = new Point(middleStartX + 130, bottomButtonY + 8);

            //labelPronunciationScope.Location = new Point(middleStartX + 220, bottomButtonY + 8);
            radioOriginal.Location = new Point(middleStartX + 300, bottomButtonY + 8);
            radioExplanation.Location = new Point(middleStartX + 365, bottomButtonY + 8);
            radioBoth.Location = new Point(middleStartX + 430, bottomButtonY + 8);

            buttonExit.Location = new Point(middleStartX + middleWidth - 120, bottomButtonY);
            buttonExit.Size = new Size(120, 40);

            // 快捷键提示
            labelShortcutHints.Location = new Point(middleStartX + (middleWidth - labelShortcutHints.Width) / 2, bottomButtonY + 45);
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

        public string CurrentContent
        {
            set => labelContent.Text = value;
        }

        public string CurrentDisplayText
        {
            set => labelDisplay.Text = value;
        }

        public string AIExplanation
        {
            set => richTextBoxAI.Text = value;
        }

        public string Statistics
        {
            set => labelStatistics.Text = value;
        }

        public int ProgressValue
        {
            set => progressBar1.Value = value;
        }

        public int ProgressMax
        {
            set => progressBar1.Maximum = value;
        }

        public bool IsVoiceEnabled
        {
            get => checkBoxVoice.Checked;
            set => checkBoxVoice.Checked = value;
        }

        public bool IsAIExplanationEnabled
        {
            get => checkBoxAIExplanation.Checked;
            set => checkBoxAIExplanation.Checked = value;
        }

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
            panelContent.SuspendLayout();
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
            buttonKnown.Text = "✅ 会了";
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
            buttonUnknown.Text = "❌ 不会";
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
            buttonNext.Text = "➡ 下一个";
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
            buttonPronounce.Text = "🔊 发音";
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
            ResumeLayout(false);
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

            labelListStatus.Text = $"共 {items.Count} 项";

            if (currentIndex >= 0 && currentIndex < items.Count)
            {
                listBoxItems.SelectedIndex = currentIndex;
                listBoxItems.TopIndex = Math.Max(0, currentIndex - 5);
            }
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
            MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonUnknown_Click(object? sender, EventArgs e)
        {
            _soundService?.PlayError();
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

    }
}
