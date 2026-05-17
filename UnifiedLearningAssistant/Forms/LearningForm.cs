using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Models.Learning;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Services.Feedback;
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
        private readonly ISoundService _soundService;
        private AiQuestionDialog? _aiDialog;
        private bool _disposed = false;
        private Settings _settings = new();
        private PanelDecorator _panelDecorator;

        public LearningForm(IAiQuestionService aiQuestionService, ITTSService ttsService, ILogger<LearningForm> logger, ILoggerFactory loggerFactory)
        {
            InitializeComponent();
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _soundService = new SoundService();
            _panelDecorator = new PanelDecorator();
            
            // 应用主题样式
            ApplyColorScheme();
            AddAnimation();
            
            Load += LearningForm_Load;
            FormClosing += LearningForm_FormClosing;
            Resize += LearningForm_Resize;
            KeyPreview = true;
            KeyDown += LearningForm_KeyDown;
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
            int contentHeight = Math.Max(240, (clientHeight - 500) / 2);
            
            panelContent.Size = new Size(contentWidth, contentHeight);
            panelContent.Location = new Point((clientWidth - contentWidth) / 2, 25);
            
            labelDisplay.Size = new Size(contentWidth, 69);
            labelContent.Size = new Size(contentWidth, contentHeight);
            
            int aiHeight = Math.Max(220, (clientHeight - 500) / 2);
            panelAI.Size = new Size(contentWidth, aiHeight);
            panelAI.Location = new Point((clientWidth - contentWidth) / 2, panelContent.Location.Y + panelContent.Height + 20);
            
            labelAI.Size = new Size(contentWidth, 40);
            richTextBoxAI.Size = new Size(contentWidth, aiHeight - 40);
            richTextBoxAI.Location = new Point(0, 40);
            
            progressBar1.Size = new Size(contentWidth, 32);
            progressBar1.Location = new Point((clientWidth - contentWidth) / 2, panelAI.Location.Y + panelAI.Height + 20);
            
            labelStatistics.Location = new Point((clientWidth - contentWidth) / 2, progressBar1.Location.Y + progressBar1.Height + 10);
            
            int buttonY = labelStatistics.Location.Y + labelStatistics.Height + 25;
            int buttonXStart = (clientWidth - 660) / 2;
            
            buttonKnown.Location = new Point(buttonXStart, buttonY);
            buttonUnknown.Location = new Point(buttonXStart + 150 + 25, buttonY);
            buttonNext.Location = new Point(buttonXStart + 300 + 50, buttonY);
            buttonAddToPdf.Location = new Point(buttonXStart + 450 + 75, buttonY);
            
            buttonPronounce.Location = new Point((clientWidth - contentWidth) / 2, buttonY + 70);
            checkBoxVoice.Location = new Point(buttonPronounce.Location.X + 130, buttonY + 76);
            
            labelPronunciationScope.Location = new Point(checkBoxVoice.Location.X + 120, buttonY + 76);
            radioOriginal.Location = new Point(labelPronunciationScope.Location.X + 80, buttonY + 76);
            radioExplanation.Location = new Point(radioOriginal.Location.X + 75, buttonY + 76);
            radioBoth.Location = new Point(radioExplanation.Location.X + 75, buttonY + 76);
            
            buttonExit.Location = new Point((clientWidth - contentWidth) / 2 + contentWidth - 125, buttonY + 70);
        }

        private void LearningForm_Load(object? sender, EventArgs e)
        {
            LoadSettings();
            ApplySettings();
        }

        private void ApplyColorScheme()
        {
            BackColor = ThemeHelper.Colors.WarmBackground;
            TransparencyKey = Color.FromArgb(255, 0, 255);
            
            panelContent.BackColor = ThemeHelper.Colors.PanelLight;
            panelAI.BackColor = ThemeHelper.Colors.PanelWarm;
            
            labelDisplay.ForeColor = ThemeHelper.Colors.TextSecondary;
            labelContent.ForeColor = ThemeHelper.Colors.TextPrimary;
            labelAI.ForeColor = Color.FromArgb(140, 100, 80);
            labelStatistics.ForeColor = Color.FromArgb(80, 100, 120);
            
            progressBar1.ForeColor = ThemeHelper.Colors.Progress;
            progressBar1.BackColor = Color.FromArgb(240, 240, 240);
            progressBar1.Style = ProgressBarStyle.Continuous;
            
            // 配置按钮样式
            ThemeHelper.ConfigureButton(buttonKnown, ThemeHelper.Colors.Success);
            ThemeHelper.ConfigureButton(buttonUnknown, ThemeHelper.Colors.Error);
            ThemeHelper.ConfigureButton(buttonNext, ThemeHelper.Colors.Primary);
            ThemeHelper.ConfigureButton(buttonAddToPdf, ThemeHelper.Colors.Purple);
            ThemeHelper.ConfigureButton(buttonPronounce, ThemeHelper.Colors.Cyan);
            ThemeHelper.ConfigureButton(buttonExit, ThemeHelper.Colors.Gray);
            
            checkBoxVoice.ForeColor = ThemeHelper.Colors.TextPrimary;
            labelPronunciationScope.ForeColor = ThemeHelper.Colors.TextPrimary;
            
            radioOriginal.ForeColor = ThemeHelper.Colors.TextPrimary;
            radioExplanation.ForeColor = ThemeHelper.Colors.TextPrimary;
            radioBoth.ForeColor = ThemeHelper.Colors.TextPrimary;
            
            richTextBoxAI.BackColor = Color.FromArgb(250, 250, 250);
            richTextBoxAI.ForeColor = Color.FromArgb(60, 80, 100);
            
            panelContent.BorderStyle = BorderStyle.None;
            panelAI.BorderStyle = BorderStyle.None;
            
            // 添加按钮悬停效果
            ThemeHelper.AddButtonHoverEffect(buttonKnown, ThemeHelper.Colors.Success);
            ThemeHelper.AddButtonHoverEffect(buttonUnknown, ThemeHelper.Colors.Error);
            ThemeHelper.AddButtonHoverEffect(buttonNext, ThemeHelper.Colors.Primary);
            ThemeHelper.AddButtonHoverEffect(buttonAddToPdf, ThemeHelper.Colors.Purple);
            ThemeHelper.AddButtonHoverEffect(buttonPronounce, ThemeHelper.Colors.Cyan);
            ThemeHelper.AddButtonHoverEffect(buttonExit, ThemeHelper.Colors.Gray);
        }

        private void AddAnimation()
        {
            panelContent.Paint += (s, e) =>
            {
                using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0),
                    new Point(panelContent.Width, panelContent.Height),
                    Color.FromArgb(255, 255, 255),
                    Color.FromArgb(245, 250, 248)
                );
                e.Graphics.FillRectangle(gradient, panelContent.ClientRectangle);
                
                using var pen = new Pen(Color.FromArgb(200, 210, 220), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panelContent.Width - 2, panelContent.Height - 2);
            };

            panelAI.Paint += (s, e) =>
            {
                using var gradient = new System.Drawing.Drawing2D.LinearGradientBrush(
                    new Point(0, 0),
                    new Point(0, panelAI.Height),
                    Color.FromArgb(252, 248, 240),
                    Color.FromArgb(248, 244, 235)
                );
                e.Graphics.FillRectangle(gradient, panelAI.ClientRectangle);
                
                using var pen = new Pen(Color.FromArgb(210, 200, 190), 2);
                e.Graphics.DrawRectangle(pen, 1, 1, panelAI.Width - 2, panelAI.Height - 2);
            };
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
        private Label labelShortcutHints;

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
            labelShortcutHints = new Label();
            panelContent.SuspendLayout();
            panelAI.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.Controls.Add(labelDisplay);
            panelContent.Controls.Add(labelContent);
            panelContent.Location = new Point(20, 25);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(860, 240);
            panelContent.TabIndex = 0;
            // 
            // labelDisplay
            // 
            labelDisplay.Dock = DockStyle.Top;
            labelDisplay.Font = new Font("微软雅黑", 22F, FontStyle.Bold);
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
            labelContent.Size = new Size(860, 240);
            labelContent.TabIndex = 0;
            labelContent.TextAlign = ContentAlignment.MiddleCenter;
            labelContent.Click += LabelContent_Click;
            // 
            // panelAI
            // 
            panelAI.Controls.Add(richTextBoxAI);
            panelAI.Controls.Add(labelAI);
            panelAI.Location = new Point(20, 280);
            panelAI.Name = "panelAI";
            panelAI.Size = new Size(860, 220);
            panelAI.TabIndex = 1;
            // 
            // richTextBoxAI
            // 
            richTextBoxAI.Dock = DockStyle.Fill;
            richTextBoxAI.Font = new Font("微软雅黑", 11F);
            richTextBoxAI.Location = new Point(0, 40);
            richTextBoxAI.Name = "richTextBoxAI";
            richTextBoxAI.ReadOnly = true;
            richTextBoxAI.Size = new Size(860, 180);
            richTextBoxAI.TabIndex = 1;
            richTextBoxAI.Text = "";
            // 
            // labelAI
            // 
            labelAI.Dock = DockStyle.Top;
            labelAI.Font = new Font("微软雅黑", 13F, FontStyle.Bold);
            labelAI.Location = new Point(0, 0);
            labelAI.Name = "labelAI";
            labelAI.Padding = new Padding(15, 0, 0, 0);
            labelAI.Size = new Size(860, 40);
            labelAI.TabIndex = 0;
            labelAI.Text = "📖 AI 释义面板";
            labelAI.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // buttonAddToPdf
            // 
            buttonAddToPdf.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonAddToPdf.Location = new Point(705, 565);
            buttonAddToPdf.Name = "buttonAddToPdf";
            buttonAddToPdf.Size = new Size(130, 52);
            buttonAddToPdf.TabIndex = 10;
            buttonAddToPdf.Text = "💡 AI提问";
            buttonAddToPdf.Click += ButtonAddToPdf_Click;
            // 
            // progressBar1
            // 
            progressBar1.Location = new Point(20, 520);
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(860, 32);
            progressBar1.TabIndex = 2;
            // 
            // labelStatistics
            // 
            labelStatistics.Font = new Font("微软雅黑", 12F);
            labelStatistics.Location = new Point(20, 560);
            labelStatistics.Name = "labelStatistics";
            labelStatistics.Size = new Size(500, 32);
            labelStatistics.TabIndex = 3;
            // 
            // buttonKnown
            // 
            buttonKnown.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonKnown.Location = new Point(120, 590);
            buttonKnown.Name = "buttonKnown";
            buttonKnown.Size = new Size(150, 62);
            buttonKnown.TabIndex = 4;
            buttonKnown.Text = "✅ 会了";
            buttonKnown.UseVisualStyleBackColor = false;
            buttonKnown.Click += ButtonKnown_Click;
            // 
            // buttonUnknown
            // 
            buttonUnknown.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonUnknown.Location = new Point(320, 590);
            buttonUnknown.Name = "buttonUnknown";
            buttonUnknown.Size = new Size(150, 62);
            buttonUnknown.TabIndex = 5;
            buttonUnknown.Text = "❌ 不会";
            buttonUnknown.UseVisualStyleBackColor = false;
            buttonUnknown.Click += ButtonUnknown_Click;
            // 
            // buttonNext
            // 
            buttonNext.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            buttonNext.Location = new Point(520, 590);
            buttonNext.Name = "buttonNext";
            buttonNext.Size = new Size(150, 62);
            buttonNext.TabIndex = 6;
            buttonNext.Text = "➡ 下一个";
            buttonNext.Click += ButtonNext_Click;
            // 
            // buttonPronounce
            // 
            buttonPronounce.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonPronounce.Location = new Point(20, 665);
            buttonPronounce.Name = "buttonPronounce";
            buttonPronounce.Size = new Size(130, 45);
            buttonPronounce.TabIndex = 7;
            buttonPronounce.Text = "🔊 发音";
            buttonPronounce.Click += ButtonPronounce_Click;
            // 
            // buttonExit
            // 
            buttonExit.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            buttonExit.Location = new Point(705, 665);
            buttonExit.Name = "buttonExit";
            buttonExit.Size = new Size(130, 45);
            buttonExit.TabIndex = 8;
            buttonExit.Text = "🏠 返回";
            buttonExit.Click += ButtonExit_Click;
            // 
            // checkBoxVoice
            // 
            checkBoxVoice.Checked = true;
            checkBoxVoice.CheckState = CheckState.Checked;
            checkBoxVoice.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            checkBoxVoice.Location = new Point(160, 671);
            checkBoxVoice.Name = "checkBoxVoice";
            checkBoxVoice.Size = new Size(100, 30);
            checkBoxVoice.TabIndex = 9;
            checkBoxVoice.Text = "自动朗读";
            // 
            // labelPronunciationScope
            // 
            labelPronunciationScope.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            labelPronunciationScope.Location = new Point(270, 671);
            labelPronunciationScope.Name = "labelPronunciationScope";
            labelPronunciationScope.Size = new Size(90, 30);
            labelPronunciationScope.TabIndex = 12;
            labelPronunciationScope.Text = "朗读范围:";
            labelPronunciationScope.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // radioOriginal
            // 
            radioOriginal.AutoSize = true;
            radioOriginal.Checked = true;
            radioOriginal.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioOriginal.Location = new Point(370, 671);
            radioOriginal.Name = "radioOriginal";
            radioOriginal.Size = new Size(65, 26);
            radioOriginal.TabIndex = 13;
            radioOriginal.TabStop = true;
            radioOriginal.Text = "原文";
            // 
            // radioExplanation
            // 
            radioExplanation.AutoSize = true;
            radioExplanation.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioExplanation.Location = new Point(455, 671);
            radioExplanation.Name = "radioExplanation";
            radioExplanation.Size = new Size(65, 26);
            radioExplanation.TabIndex = 14;
            radioExplanation.Text = "释义";
            // 
            // radioBoth
            // 
            radioBoth.AutoSize = true;
            radioBoth.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            radioBoth.Location = new Point(540, 671);
            radioBoth.Name = "radioBoth";
            radioBoth.Size = new Size(100, 26);
            radioBoth.TabIndex = 15;
            radioBoth.Text = "原文+释义";
            // 
            // labelShortcutHints
            // 
            labelShortcutHints.AutoSize = true;
            labelShortcutHints.Font = new Font("Microsoft YaHei UI", 9F);
            labelShortcutHints.ForeColor = Color.FromArgb(120, 120, 120);
            labelShortcutHints.Location = new Point(20, 720);
            labelShortcutHints.Name = "labelShortcutHints";
            labelShortcutHints.Size = new Size(800, 17);
            labelShortcutHints.TabIndex = 16;
            labelShortcutHints.Text = "快捷键: 空格-发音 | 回车-下一个 | 1/K-会了 | 2/U-不会 | Esc-返回";
            // 
            // LearningForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(900, 750);
            Controls.Add(labelShortcutHints);
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
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "LearningForm";
            Text = "✨ 学习模式 ✨";
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
    }
}
