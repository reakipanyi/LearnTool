using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Models.Learning;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class LearningForm : Form, ILearningView
    {
        private LearningItem? _currentItem;
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        private readonly ILogger<LearningForm> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private AiQuestionDialog? _aiDialog;
        private bool _disposed = false;
        private Settings _settings = new();

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            Resize += LearningForm_Resize;
        }

        private void LearningForm_Resize(object? sender, EventArgs e)
        {
            UpdateControlLayout();
        }

        private void UpdateControlLayout()
        {
            if (panelContent == null || panelAI == null) return;
            
            int clientWidth = ClientSize.Width;
            int clientHeight = ClientSize.Height;
            
            int contentWidth = Math.Max(860, clientWidth - 40);
            int contentHeight = Math.Max(227, (clientHeight - 450) / 2);
            
            panelContent.Size = new Size(contentWidth, contentHeight);
            panelContent.Location = new Point((clientWidth - contentWidth) / 2, 20);
            
            labelDisplay.Size = new Size(contentWidth, 69);
            labelContent.Size = new Size(contentWidth, contentHeight);
            
            int aiHeight = Math.Max(204, (clientHeight - 450) / 2);
            panelAI.Size = new Size(contentWidth, aiHeight);
            panelAI.Location = new Point((clientWidth - contentWidth) / 2, panelContent.Location.Y + panelContent.Height + 15);
            
            labelAI.Size = new Size(contentWidth, 34);
            richTextBoxAI.Size = new Size(contentWidth, aiHeight - 34);
            richTextBoxAI.Location = new Point(0, 34);
            
            progressBar1.Size = new Size(contentWidth, 28);
            progressBar1.Location = new Point((clientWidth - contentWidth) / 2, panelAI.Location.Y + panelAI.Height + 15);
            
            labelStatistics.Location = new Point((clientWidth - contentWidth) / 2, progressBar1.Location.Y + progressBar1.Height + 5);
            
            int buttonY = labelStatistics.Location.Y + labelStatistics.Height + 20;
            int buttonXStart = (clientWidth - 660) / 2;
            
            buttonKnown.Location = new Point(buttonXStart, buttonY);
            buttonUnknown.Location = new Point(buttonXStart + 150 + 20, buttonY);
            buttonNext.Location = new Point(buttonXStart + 300 + 40, buttonY);
            buttonAddToPdf.Location = new Point(buttonXStart + 450 + 60, buttonY);
            
            buttonPronounce.Location = new Point((clientWidth - contentWidth) / 2, buttonY + 65);
            checkBoxVoice.Location = new Point(buttonPronounce.Location.X + 130, buttonY + 71);
            
            labelPronunciationScope.Location = new Point(checkBoxVoice.Location.X + 120, buttonY + 71);
            radioOriginal.Location = new Point(labelPronunciationScope.Location.X + 80, buttonY + 71);
            radioExplanation.Location = new Point(radioOriginal.Location.X + 70, buttonY + 71);
            radioBoth.Location = new Point(radioExplanation.Location.X + 70, buttonY + 71);
            
            buttonExit.Location = new Point((clientWidth - contentWidth) / 2 + contentWidth - 125, buttonY + 65);
        }

        private void LearningForm_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            ApplySettings();
        }

        private void LearningForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveSettings();
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

                string settingsDir = Paths.DataDirectory;
                if (!Directory.Exists(settingsDir))
                {
                    Directory.CreateDirectory(settingsDir);
                }

                string settingsPath = Path.Combine(settingsDir, Paths.SettingsFile);
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
        }

        private void ApplySettings()
        {
            checkBoxVoice.Checked = _settings.IsVoiceEnabled;
            switch (_settings.PronunciationScope)
            {
                case 0: radioOriginal.Checked = true; break;
                case 1: radioExplanation.Checked = true; break;
                case 2: radioBoth.Checked = true; break;
            }
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

        public string CurrentMode => Constants.LearningMode.Study;

        public event EventHandler? MarkAsKnownClicked;
        public event EventHandler? MarkAsUnknownClicked;
        public event EventHandler? PronounceClicked;
        public event EventHandler? NextClicked;
        public event EventHandler? ExitClicked;
        public event EventHandler? AddToPdfQuestionClicked;

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
        }

        public void SetCurrentItem(LearningItem item)
        {
            _currentItem = item;
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private Panel panelContent;
        private Label labelContent;
        private Label labelDisplay;
        private Panel panelAI;
        private RichTextBox richTextBoxAI;
        private ProgressBar progressBar1;
        private Label labelStatistics;
        private Button buttonKnown;
        private Button buttonUnknown;
        private Button buttonNext;
        private Button buttonPronounce;
        private Button buttonExit;
        private CheckBox checkBoxVoice;
        private Button buttonAddToPdf;
        private Label labelAI;
        private Label labelPronunciationScope;
        private RadioButton radioOriginal;
        private RadioButton radioExplanation;
        private RadioButton radioBoth;

        private void InitializeComponent()
        {
            panelContent = new Panel();
            labelDisplay = new Label();
            labelContent = new Label();
            panelAI = new Panel();
            richTextBoxAI = new RichTextBox();
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
            labelPronunciationScope = new Label();
            radioOriginal = new RadioButton();
            radioExplanation = new RadioButton();
            radioBoth = new RadioButton();
            panelContent.SuspendLayout();
            panelAI.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.Controls.Add(labelDisplay);
            panelContent.Controls.Add(labelContent);
            panelContent.Location = new Point(20, 23);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(860, 227);
            panelContent.TabIndex = 0;
            // 
            // labelDisplay
            // 
            labelDisplay.Dock = DockStyle.Top;
            labelDisplay.Font = new Font("微软雅黑", 24F);
            labelDisplay.Location = new Point(0, 0);
            labelDisplay.Name = "labelDisplay";
            labelDisplay.Size = new Size(860, 69);
            labelDisplay.TabIndex = 1;
            labelDisplay.TextAlign = ContentAlignment.TopCenter;
            // 
            // labelContent
            // 
            labelContent.Dock = DockStyle.Fill;
            labelContent.Font = new Font("微软雅黑", 72F, FontStyle.Bold);
            labelContent.Location = new Point(0, 0);
            labelContent.Name = "labelContent";
            labelContent.Size = new Size(860, 227);
            labelContent.TabIndex = 0;
            labelContent.TextAlign = ContentAlignment.MiddleCenter;
            labelContent.Click += LabelContent_Click;
            // 
            // panelAI
            // 
            panelAI.Controls.Add(richTextBoxAI);
            panelAI.Controls.Add(labelAI);
            panelAI.Location = new Point(20, 261);
            panelAI.Name = "panelAI";
            panelAI.Size = new Size(860, 204);
            panelAI.TabIndex = 1;
            // 
            // richTextBoxAI
            // 
            richTextBoxAI.Dock = DockStyle.Fill;
            richTextBoxAI.Location = new Point(0, 34);
            richTextBoxAI.Name = "richTextBoxAI";
            richTextBoxAI.ReadOnly = true;
            richTextBoxAI.Size = new Size(860, 170);
            richTextBoxAI.TabIndex = 1;
            richTextBoxAI.Text = "";
            // 
            // labelAI
            // 
            labelAI.Dock = DockStyle.Top;
            labelAI.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            labelAI.Location = new Point(0, 0);
            labelAI.Name = "labelAI";
            labelAI.Size = new Size(860, 34);
            labelAI.TabIndex = 0;
            labelAI.Text = "📖 AI 释义面板";
            labelAI.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonAddToPdf
            // 
            buttonAddToPdf.Font = new Font("微软雅黑", 12F);
            buttonAddToPdf.Location = new Point(705, 560);
            buttonAddToPdf.Name = "buttonAddToPdf";
            buttonAddToPdf.Size = new Size(120, 46);
            buttonAddToPdf.TabIndex = 10;
            buttonAddToPdf.Text = "AI提问";
            buttonAddToPdf.Click += ButtonAddToPdf_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(20, 476);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(860, 28);
            progressBar1.TabIndex = 2;
            // 
            // labelStatistics
            // 
            labelStatistics.Font = new Font("微软雅黑", 12F);
            labelStatistics.Location = new Point(20, 510);
            labelStatistics.Name = "labelStatistics";
            labelStatistics.Size = new Size(400, 28);
            labelStatistics.TabIndex = 3;
            // 
            // buttonKnown
            // 
            buttonKnown.BackColor = Color.LightGreen;
            buttonKnown.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonKnown.Location = new Point(120, 555);
            buttonKnown.Name = "buttonKnown";
            buttonKnown.Size = new Size(150, 57);
            buttonKnown.TabIndex = 4;
            buttonKnown.Text = "✅ 会了";
            buttonKnown.UseVisualStyleBackColor = false;
            buttonKnown.Click += ButtonKnown_Click;
            // 
            // buttonUnknown
            // 
            buttonUnknown.BackColor = Color.LightPink;
            buttonUnknown.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonUnknown.Location = new Point(320, 555);
            buttonUnknown.Name = "buttonUnknown";
            buttonUnknown.Size = new Size(150, 57);
            buttonUnknown.TabIndex = 5;
            buttonUnknown.Text = "❌ 不会";
            buttonUnknown.UseVisualStyleBackColor = false;
            buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // buttonNext
            // 
            buttonNext.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonNext.Location = new Point(520, 555);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(150, 57);
            buttonNext.TabIndex = 6;
            buttonNext.Text = "➡ 下一个";
            buttonNext.Click += ButtonNext_Click;
            // 
            // buttonPronounce
            // 
            buttonPronounce.Font = new Font("微软雅黑", 12F);
            buttonPronounce.Location = new Point(20, 623);
            buttonPronounce.Name = "buttonPronounce";
            buttonPronounce.Size = new Size(120, 40);
            buttonPronounce.TabIndex = 7;
            buttonPronounce.Text = "🔊 发音";
            buttonPronounce.Click += ButtonPronounce_Click;
            // 
            // buttonExit
            // 
            buttonExit.Font = new Font("微软雅黑", 12F);
            buttonExit.Location = new Point(705, 623);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(120, 40);
            buttonExit.TabIndex = 8;
            buttonExit.Text = "返回主界面";
            buttonExit.Click += ButtonExit_Click;
            // 
            // checkBoxVoice
            // 
            checkBoxVoice.Checked = true;
            checkBoxVoice.CheckState = CheckState.Checked;
            checkBoxVoice.Location = new Point(150, 629);
            checkBoxVoice.Name = "checkBoxVoice";
            checkBoxVoice.Size = new Size(90, 28);
            checkBoxVoice.TabIndex = 9;
            checkBoxVoice.Text = "自动朗读";
            // 
            // labelPronunciationScope
            // 
            labelPronunciationScope.Font = new Font("微软雅黑", 10F);
            labelPronunciationScope.Location = new Point(250, 629);
            labelPronunciationScope.Name = "labelPronunciationScope";
            labelPronunciationScope.Size = new Size(80, 25);
            labelPronunciationScope.TabIndex = 12;
            labelPronunciationScope.Text = "朗读范围:";
            labelPronunciationScope.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // radioOriginal
            // 
            radioOriginal.AutoSize = true;
            radioOriginal.Checked = true;
            radioOriginal.Font = new Font("微软雅黑", 10F);
            radioOriginal.Location = new Point(340, 629);
            radioOriginal.Name = "radioOriginal";
            radioOriginal.Size = new Size(60, 24);
            radioOriginal.TabIndex = 13;
            radioOriginal.TabStop = true;
            radioOriginal.Text = "原文";
            // 
            // radioExplanation
            // 
            radioExplanation.AutoSize = true;
            radioExplanation.Font = new Font("微软雅黑", 10F);
            radioExplanation.Location = new Point(410, 629);
            radioExplanation.Name = "radioExplanation";
            radioExplanation.Size = new Size(60, 24);
            radioExplanation.TabIndex = 14;
            radioExplanation.Text = "释义";
            // 
            // radioBoth
            // 
            radioBoth.AutoSize = true;
            radioBoth.Font = new Font("微软雅黑", 10F);
            radioBoth.Location = new Point(480, 629);
            radioBoth.Name = "radioBoth";
            radioBoth.Size = new Size(90, 24);
            radioBoth.TabIndex = 15;
            radioBoth.Text = "原文+释义";
            // 
            // LearningForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(255, 244, 230);
            ClientSize = new Size(900, 720);
            Controls.Add(radioBoth);
            Controls.Add(radioExplanation);
            Controls.Add(radioOriginal);
            Controls.Add(labelPronunciationScope);
            Controls.Add(buttonAddToPdf);
            Controls.Add(checkBoxVoice);
            Controls.Add(buttonExit);
            Controls.Add(buttonPronounce);
            Controls.Add(buttonNext);
            Controls.Add(buttonUnknown);
            Controls.Add(buttonKnown);
            Controls.Add(labelStatistics);
            Controls.Add(progressBar1);
            Controls.Add(panelAI);
            Controls.Add(panelContent);
            Name = "LearningForm";
            Text = "学习模式";
            Load += LearningForm_Load;
            panelContent.ResumeLayout(false);
            panelAI.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        #region Event Handlers

        private void LabelContent_Click(object? sender, EventArgs e)
        {
            PronounceClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonKnown_Click(object? sender, EventArgs e)
        {
            MarkAsKnownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonUnknown_Click(object? sender, EventArgs e)
        {
            MarkAsUnknownClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonNext_Click(object? sender, EventArgs e)
        {
            NextClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonPronounce_Click(object? sender, EventArgs e)
        {
            PronounceClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonExit_Click(object? sender, EventArgs e)
        {
            ExitClicked?.Invoke(this, EventArgs.Empty);
            Close();
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


    }
}
