using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class SettingForm : Form, ISettingView, IThemeable
    {
        private readonly ILogger<SettingForm> _logger;
        private readonly IThemeService _themeService;
        private bool _disposed = false;
        private CheckBox checkBoxNightMode;
        private Panel headerPanel;
        private Label labelTitle;
        private bool _isDarkMode = false;
        private bool _isProgrammaticChange = false;

        public SettingForm(ILogger<SettingForm> logger, IThemeService themeService)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            _themeService.RegisterThemeable(this);
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (headerPanel != null)
            {
                headerPanel.BackColor = colors.Primary;
            }

            foreach (Control control in Controls)
            {
                ApplyThemeToControl(control, colors);
            }
        }

        private void ApplyThemeToControl(Control control, ThemeColors colors)
        {
            if (control is GroupBox groupBox)
            {
                groupBox.BackColor = colors.Surface;
                groupBox.ForeColor = colors.TextPrimary;
            }
            else if (control is Label label)
            {
                label.ForeColor = colors.TextPrimary;
            }
            else if (control is TextBox textBox)
            {
                textBox.BackColor = colors.Surface;
                textBox.ForeColor = colors.TextPrimary;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = colors.Surface;
                comboBox.ForeColor = colors.TextPrimary;
            }
            else if (control is NumericUpDown numericUpDown)
            {
                numericUpDown.BackColor = colors.Surface;
                numericUpDown.ForeColor = colors.TextPrimary;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.ForeColor = colors.TextPrimary;
            }
            else if (control is TrackBar trackBar)
            {
                trackBar.BackColor = colors.Surface;
            }

            foreach (Control child in control.Controls)
            {
                ApplyThemeToControl(child, colors);
            }
        }

        #region ISettingView Implementation


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool TTSEnabled
        {
            get => checkBoxTtsEnabled.Checked;
            set => checkBoxTtsEnabled.Checked = value;
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string TtsApiKey
        {
            get => textBoxTtsApiKey.Text;
            set => textBoxTtsApiKey.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public string TtsVoice
        {
            get => comboBoxVoice.Text;
            set => comboBoxVoice.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TTSSpeed
        {
            get => trackBarSpeed.Value;
            set => trackBarSpeed.Value = Math.Clamp(value, trackBarSpeed.Minimum, trackBarSpeed.Maximum);
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int TTSVolume
        {
            get => trackBarVolume.Value;
            set => trackBarVolume.Value = Math.Clamp(value, trackBarVolume.Minimum, trackBarVolume.Maximum);
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int FontSize
        {
            get => (int)numericUpDownFontSize.Value;
            set => numericUpDownFontSize.Value = Math.Clamp(value, (int)numericUpDownFontSize.Minimum, (int)numericUpDownFontSize.Maximum);
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public string Theme
        {
            get => comboBoxTheme.Text;
            set
            {
                comboBoxTheme.Text = value;
                _isDarkMode = value == "Dark";
                checkBoxNightMode.Checked = _isDarkMode;
                if (_isDarkMode)
                {
                    ApplyTheme(true);
                }
            }
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BaiduAppId
        {
            get => textBoxBaiduAppId.Text;
            set => textBoxBaiduAppId.Text = value;
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string BaiduSecret
        {
            get => textBoxBaiduSecret.Text;
            set => textBoxBaiduSecret.Text = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public bool IsVoiceEnabled
        {
            get => checkBoxIsVoiceEnabled.Checked;
            set => checkBoxIsVoiceEnabled.Checked = value;
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]

        public int PronunciationScope
        {
            get => (int)numericUpDownPronunciationScope.Value;
            set => numericUpDownPronunciationScope.Value = value;
        }


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsAIExplanationEnabled
        {
            get => checkBoxIsAIExplanationEnabled.Checked;
            set => checkBoxIsAIExplanationEnabled.Checked = value;
        }

        public event EventHandler? SaveClicked;
        public event EventHandler? CancelClicked;

        public void ShowMessage(string msg)
        {
            MessageBox.Show(msg);
        }

        public void CloseView()
        {
            Close();
        }

        #endregion

        #region WinForms Designer Generated Code

        private System.ComponentModel.IContainer components = null;
        private GroupBox groupBoxTts;
        private CheckBox checkBoxTtsEnabled;
        private Label labelTtsApiKey;
        private TextBox textBoxTtsApiKey;
        private Label labelVoice;
        private ComboBox comboBoxVoice;
        private Label labelSpeed;
        private TrackBar trackBarSpeed;
        private Label labelSpeedValue;
        private Label labelVolume;
        private TrackBar trackBarVolume;
        private Label labelVolumeValue;
        private GroupBox groupBoxInterface;
        private Label labelFontSize;
        private NumericUpDown numericUpDownFontSize;
        private Label labelTheme;
        private ComboBox comboBoxTheme;
        private GroupBox groupBoxTranslation;
        private Label labelBaiduAppId;
        private TextBox textBoxBaiduAppId;
        private Label labelBaiduSecret;
        private TextBox textBoxBaiduSecret;
        private GroupBox groupBoxLearningSettings;
        private CheckBox checkBoxIsVoiceEnabled;
        private Label labelPronunciationScope;
        private NumericUpDown numericUpDownPronunciationScope;
        private CheckBox checkBoxIsAIExplanationEnabled;
        private Button buttonSave;
        private Button buttonCancel;

        private void InitializeComponent()
        {
            groupBoxTts = new GroupBox();
            labelVolumeValue = new Label();
            labelVolume = new Label();
            trackBarVolume = new TrackBar();
            labelSpeedValue = new Label();
            labelSpeed = new Label();
            trackBarSpeed = new TrackBar();
            textBoxTtsApiKey = new TextBox();
            labelTtsApiKey = new Label();
            comboBoxVoice = new ComboBox();
            labelVoice = new Label();
            checkBoxTtsEnabled = new CheckBox();
            groupBoxInterface = new GroupBox();
            checkBoxNightMode = new CheckBox();
            labelTheme = new Label();
            comboBoxTheme = new ComboBox();
            numericUpDownFontSize = new NumericUpDown();
            labelFontSize = new Label();
            groupBoxTranslation = new GroupBox();
            textBoxBaiduSecret = new TextBox();
            labelBaiduSecret = new Label();
            textBoxBaiduAppId = new TextBox();
            labelBaiduAppId = new Label();
            buttonSave = new Button();
            buttonCancel = new Button();
            headerPanel = new Panel();
            labelTitle = new Label();
            groupBoxLearningSettings = new GroupBox();
            checkBoxIsVoiceEnabled = new CheckBox();
            labelPronunciationScope = new Label();
            numericUpDownPronunciationScope = new NumericUpDown();
            checkBoxIsAIExplanationEnabled = new CheckBox();
            groupBoxTts.SuspendLayout();
            ((ISupportInitialize)trackBarVolume).BeginInit();
            ((ISupportInitialize)trackBarSpeed).BeginInit();
            groupBoxInterface.SuspendLayout();
            ((ISupportInitialize)numericUpDownFontSize).BeginInit();
            groupBoxTranslation.SuspendLayout();
            headerPanel.SuspendLayout();
            groupBoxLearningSettings.SuspendLayout();
            ((ISupportInitialize)numericUpDownPronunciationScope).BeginInit();
            SuspendLayout();
            // 
            // groupBoxTts
            // 
            groupBoxTts.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxTts.Controls.Add(labelVolumeValue);
            groupBoxTts.Controls.Add(labelVolume);
            groupBoxTts.Controls.Add(trackBarVolume);
            groupBoxTts.Controls.Add(labelSpeedValue);
            groupBoxTts.Controls.Add(labelSpeed);
            groupBoxTts.Controls.Add(trackBarSpeed);
            groupBoxTts.Controls.Add(textBoxTtsApiKey);
            groupBoxTts.Controls.Add(labelTtsApiKey);
            groupBoxTts.Controls.Add(comboBoxVoice);
            groupBoxTts.Controls.Add(labelVoice);
            groupBoxTts.Controls.Add(checkBoxTtsEnabled);
            groupBoxTts.FlatStyle = FlatStyle.Flat;
            groupBoxTts.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxTts.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxTts.Location = new Point(15, 68);
            groupBoxTts.Name = "groupBoxTts";
            groupBoxTts.Size = new Size(550, 181);
            groupBoxTts.TabIndex = 1;
            groupBoxTts.TabStop = false;
            groupBoxTts.Text = "🔊 千问TTS 语音设置";
            // 
            // labelVolumeValue
            // 
            labelVolumeValue.ForeColor = Color.FromArgb(33, 33, 33);
            labelVolumeValue.Location = new Point(500, 138);
            labelVolumeValue.Name = "labelVolumeValue";
            labelVolumeValue.Size = new Size(40, 23);
            labelVolumeValue.TabIndex = 10;
            labelVolumeValue.Text = "100%";
            // 
            // labelVolume
            // 
            labelVolume.ForeColor = Color.FromArgb(33, 33, 33);
            labelVolume.Location = new Point(15, 138);
            labelVolume.Name = "labelVolume";
            labelVolume.Size = new Size(60, 23);
            labelVolume.TabIndex = 9;
            labelVolume.Text = "音量:";
            // 
            // trackBarVolume
            // 
            trackBarVolume.Location = new Point(80, 136);
            trackBarVolume.Maximum = 100;
            trackBarVolume.Name = "trackBarVolume";
            trackBarVolume.Size = new Size(410, 45);
            trackBarVolume.TabIndex = 8;
            trackBarVolume.Value = 100;
            trackBarVolume.Scroll += TrackBarVolume_Scroll;
            // 
            // labelSpeedValue
            // 
            labelSpeedValue.ForeColor = Color.FromArgb(33, 33, 33);
            labelSpeedValue.Location = new Point(500, 99);
            labelSpeedValue.Name = "labelSpeedValue";
            labelSpeedValue.Size = new Size(40, 23);
            labelSpeedValue.TabIndex = 7;
            labelSpeedValue.Text = "100%";
            // 
            // labelSpeed
            // 
            labelSpeed.ForeColor = Color.FromArgb(33, 33, 33);
            labelSpeed.Location = new Point(15, 99);
            labelSpeed.Name = "labelSpeed";
            labelSpeed.Size = new Size(60, 23);
            labelSpeed.TabIndex = 6;
            labelSpeed.Text = "速度:";
            // 
            // trackBarSpeed
            // 
            trackBarSpeed.Location = new Point(80, 96);
            trackBarSpeed.Maximum = 200;
            trackBarSpeed.Minimum = 0;
            trackBarSpeed.Name = "trackBarSpeed";
            trackBarSpeed.Size = new Size(410, 45);
            trackBarSpeed.TabIndex = 5;
            trackBarSpeed.Value = 100;
            trackBarSpeed.Scroll += TrackBarSpeed_Scroll;
            // 
            // textBoxTtsApiKey
            // 
            textBoxTtsApiKey.Location = new Point(120, 57);
            textBoxTtsApiKey.Name = "textBoxTtsApiKey";
            textBoxTtsApiKey.PasswordChar = '*';
            textBoxTtsApiKey.Size = new Size(400, 25);
            textBoxTtsApiKey.TabIndex = 4;
            // 
            // labelTtsApiKey
            // 
            labelTtsApiKey.ForeColor = Color.FromArgb(33, 33, 33);
            labelTtsApiKey.Location = new Point(15, 60);
            labelTtsApiKey.Name = "labelTtsApiKey";
            labelTtsApiKey.Size = new Size(100, 23);
            labelTtsApiKey.TabIndex = 3;
            labelTtsApiKey.Text = "DashScope Key:";
            // 
            // comboBoxVoice
            // 
            comboBoxVoice.FormattingEnabled = true;
            comboBoxVoice.Items.AddRange(new object[] { "Aria", "Cherry", "Xiaobei", "Xiaoning" });
            comboBoxVoice.Location = new Point(80, 17);
            comboBoxVoice.Name = "comboBoxVoice";
            comboBoxVoice.Size = new Size(150, 27);
            comboBoxVoice.TabIndex = 2;
            // 
            // labelVoice
            // 
            labelVoice.ForeColor = Color.FromArgb(33, 33, 33);
            labelVoice.Location = new Point(15, 20);
            labelVoice.Name = "labelVoice";
            labelVoice.Size = new Size(60, 23);
            labelVoice.TabIndex = 1;
            labelVoice.Text = "声音:";
            // 
            // checkBoxTtsEnabled
            // 
            checkBoxTtsEnabled.ForeColor = Color.FromArgb(33, 33, 33);
            checkBoxTtsEnabled.Location = new Point(250, 17);
            checkBoxTtsEnabled.Name = "checkBoxTtsEnabled";
            checkBoxTtsEnabled.Size = new Size(100, 28);
            checkBoxTtsEnabled.TabIndex = 0;
            checkBoxTtsEnabled.Text = "启用TTS";
            // 
            // groupBoxInterface
            // 
            groupBoxInterface.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxInterface.Controls.Add(checkBoxNightMode);
            groupBoxInterface.Controls.Add(labelTheme);
            groupBoxInterface.Controls.Add(comboBoxTheme);
            groupBoxInterface.Controls.Add(numericUpDownFontSize);
            groupBoxInterface.Controls.Add(labelFontSize);
            groupBoxInterface.FlatStyle = FlatStyle.Flat;
            groupBoxInterface.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxInterface.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxInterface.Location = new Point(15, 448);
            groupBoxInterface.Name = "groupBoxInterface";
            groupBoxInterface.Size = new Size(550, 91);
            groupBoxInterface.TabIndex = 2;
            groupBoxInterface.TabStop = false;
            groupBoxInterface.Text = "🎨 界面与字体";
            // 
            // checkBoxNightMode
            // 
            checkBoxNightMode.Appearance = Appearance.Button;
            checkBoxNightMode.BackColor = Color.FromArgb(30, 30, 30);
            checkBoxNightMode.FlatAppearance.BorderSize = 0;
            checkBoxNightMode.FlatStyle = FlatStyle.Flat;
            checkBoxNightMode.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            checkBoxNightMode.ForeColor = Color.White;
            checkBoxNightMode.Location = new Point(440, 26);
            checkBoxNightMode.Name = "checkBoxNightMode";
            checkBoxNightMode.Size = new Size(65, 40);
            checkBoxNightMode.TabIndex = 7;
            checkBoxNightMode.Text = "🌙 夜间";
            checkBoxNightMode.TextAlign = ContentAlignment.MiddleCenter;
            checkBoxNightMode.UseVisualStyleBackColor = false;
            checkBoxNightMode.CheckedChanged += CheckBoxNightMode_CheckedChanged;
            // 
            // labelTheme
            // 
            labelTheme.ForeColor = Color.FromArgb(33, 33, 33);
            labelTheme.Location = new Point(180, 36);
            labelTheme.Name = "labelTheme";
            labelTheme.Size = new Size(60, 23);
            labelTheme.TabIndex = 3;
            labelTheme.Text = "主题:";
            // 
            // comboBoxTheme
            // 
            comboBoxTheme.FormattingEnabled = true;
            comboBoxTheme.Items.AddRange(new object[] { "Light", "Dark" });
            comboBoxTheme.Location = new Point(250, 32);
            comboBoxTheme.Name = "comboBoxTheme";
            comboBoxTheme.Size = new Size(170, 27);
            comboBoxTheme.TabIndex = 2;
            // 
            // numericUpDownFontSize
            // 
            numericUpDownFontSize.Location = new Point(120, 32);
            numericUpDownFontSize.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
            numericUpDownFontSize.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
            numericUpDownFontSize.Name = "numericUpDownFontSize";
            numericUpDownFontSize.Size = new Size(60, 25);
            numericUpDownFontSize.TabIndex = 1;
            numericUpDownFontSize.Value = new decimal(new int[] { 12, 0, 0, 0 });
            // 
            // labelFontSize
            // 
            labelFontSize.ForeColor = Color.FromArgb(33, 33, 33);
            labelFontSize.Location = new Point(15, 36);
            labelFontSize.Name = "labelFontSize";
            labelFontSize.Size = new Size(100, 23);
            labelFontSize.TabIndex = 0;
            labelFontSize.Text = "字体大小:";
            // 
            // groupBoxTranslation
            // 
            groupBoxTranslation.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxTranslation.Controls.Add(textBoxBaiduSecret);
            groupBoxTranslation.Controls.Add(labelBaiduSecret);
            groupBoxTranslation.Controls.Add(textBoxBaiduAppId);
            groupBoxTranslation.Controls.Add(labelBaiduAppId);
            groupBoxTranslation.FlatStyle = FlatStyle.Flat;
            groupBoxTranslation.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxTranslation.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxTranslation.Location = new Point(15, 255);
            groupBoxTranslation.Name = "groupBoxTranslation";
            groupBoxTranslation.Size = new Size(550, 91);
            groupBoxTranslation.TabIndex = 3;
            groupBoxTranslation.TabStop = false;
            groupBoxTranslation.Text = "🌐 翻译服务";
            // 
            // textBoxBaiduSecret
            // 
            textBoxBaiduSecret.Location = new Point(350, 23);
            textBoxBaiduSecret.Name = "textBoxBaiduSecret";
            textBoxBaiduSecret.Size = new Size(170, 25);
            textBoxBaiduSecret.TabIndex = 3;
            // 
            // labelBaiduSecret
            // 
            labelBaiduSecret.ForeColor = Color.FromArgb(33, 33, 33);
            labelBaiduSecret.Location = new Point(280, 26);
            labelBaiduSecret.Name = "labelBaiduSecret";
            labelBaiduSecret.Size = new Size(60, 23);
            labelBaiduSecret.TabIndex = 2;
            labelBaiduSecret.Text = "密钥:";
            // 
            // textBoxBaiduAppId
            // 
            textBoxBaiduAppId.Location = new Point(120, 23);
            textBoxBaiduAppId.Name = "textBoxBaiduAppId";
            textBoxBaiduAppId.Size = new Size(150, 25);
            textBoxBaiduAppId.TabIndex = 1;
            // 
            // labelBaiduAppId
            // 
            labelBaiduAppId.ForeColor = Color.FromArgb(33, 33, 33);
            labelBaiduAppId.Location = new Point(15, 26);
            labelBaiduAppId.Name = "labelBaiduAppId";
            labelBaiduAppId.Size = new Size(100, 23);
            labelBaiduAppId.TabIndex = 0;
            labelBaiduAppId.Text = "百度AppId:";
            // 
            // buttonSave
            // 
            buttonSave.BackColor = Color.FromArgb(76, 175, 80);
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.FlatStyle = FlatStyle.Flat;
            buttonSave.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonSave.ForeColor = Color.White;
            buttonSave.Location = new Point(315, 554);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(120, 45);
            buttonSave.TabIndex = 6;
            buttonSave.Text = "💾 保存并关闭";
            buttonSave.UseVisualStyleBackColor = false;
            buttonSave.Click += ButtonSave_Click;
            buttonSave.MouseEnter += Button_HoverEnter;
            buttonSave.MouseLeave += Button_HoverLeave;
            // 
            // buttonCancel
            // 
            buttonCancel.BackColor = Color.FromArgb(158, 158, 158);
            buttonCancel.FlatAppearance.BorderSize = 0;
            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonCancel.ForeColor = Color.White;
            buttonCancel.Location = new Point(445, 554);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(120, 45);
            buttonCancel.TabIndex = 7;
            buttonCancel.Text = "❌ 取消";
            buttonCancel.UseVisualStyleBackColor = false;
            buttonCancel.Click += ButtonCancel_Click;
            buttonCancel.MouseEnter += Button_HoverEnter;
            buttonCancel.MouseLeave += Button_HoverLeave;
            // 
            // headerPanel
            // 
            headerPanel.BackColor = Color.FromArgb(255, 152, 0);
            headerPanel.Controls.Add(labelTitle);
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(580, 57);
            headerPanel.TabIndex = 6;
            // 
            // labelTitle
            // 
            labelTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(180, 9);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(220, 40);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "⚙️ 系统设置";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // groupBoxLearningSettings
            // 
            groupBoxLearningSettings.BackColor = Color.FromArgb(255, 250, 240);
            groupBoxLearningSettings.Controls.Add(checkBoxIsVoiceEnabled);
            groupBoxLearningSettings.Controls.Add(labelPronunciationScope);
            groupBoxLearningSettings.Controls.Add(numericUpDownPronunciationScope);
            groupBoxLearningSettings.Controls.Add(checkBoxIsAIExplanationEnabled);
            groupBoxLearningSettings.FlatStyle = FlatStyle.Flat;
            groupBoxLearningSettings.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            groupBoxLearningSettings.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxLearningSettings.Location = new Point(15, 352);
            groupBoxLearningSettings.Name = "groupBoxLearningSettings";
            groupBoxLearningSettings.Size = new Size(550, 91);
            groupBoxLearningSettings.TabIndex = 5;
            groupBoxLearningSettings.TabStop = false;
            groupBoxLearningSettings.Text = "📚 学习设置";
            // 
            // checkBoxIsVoiceEnabled
            // 
            checkBoxIsVoiceEnabled.ForeColor = Color.FromArgb(33, 33, 33);
            checkBoxIsVoiceEnabled.Location = new Point(15, 32);
            checkBoxIsVoiceEnabled.Name = "checkBoxIsVoiceEnabled";
            checkBoxIsVoiceEnabled.Size = new Size(100, 23);
            checkBoxIsVoiceEnabled.TabIndex = 0;
            checkBoxIsVoiceEnabled.Text = "启用语音";
            // 
            // labelPronunciationScope
            // 
            labelPronunciationScope.ForeColor = Color.FromArgb(33, 33, 33);
            labelPronunciationScope.Location = new Point(130, 36);
            labelPronunciationScope.Name = "labelPronunciationScope";
            labelPronunciationScope.Size = new Size(70, 23);
            labelPronunciationScope.TabIndex = 1;
            labelPronunciationScope.Text = "发音范围:";
            // 
            // numericUpDownPronunciationScope
            // 
            numericUpDownPronunciationScope.Location = new Point(210, 32);
            numericUpDownPronunciationScope.Maximum = new decimal(new int[] { 2, 0, 0, 0 });
            numericUpDownPronunciationScope.Name = "numericUpDownPronunciationScope";
            numericUpDownPronunciationScope.Size = new Size(60, 25);
            numericUpDownPronunciationScope.TabIndex = 2;
            numericUpDownPronunciationScope.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // checkBoxIsAIExplanationEnabled
            // 
            checkBoxIsAIExplanationEnabled.ForeColor = Color.FromArgb(33, 33, 33);
            checkBoxIsAIExplanationEnabled.Location = new Point(300, 32);
            checkBoxIsAIExplanationEnabled.Name = "checkBoxIsAIExplanationEnabled";
            checkBoxIsAIExplanationEnabled.Size = new Size(120, 23);
            checkBoxIsAIExplanationEnabled.TabIndex = 3;
            checkBoxIsAIExplanationEnabled.Text = "启用AI讲解";
            // 
            // SettingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 240, 240);
            ClientSize = new Size(580, 609);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSave);
            Controls.Add(groupBoxLearningSettings);
            Controls.Add(groupBoxTranslation);
            Controls.Add(groupBoxInterface);
            Controls.Add(groupBoxTts);
            Controls.Add(headerPanel);
            Name = "SettingForm";
            Text = "⚙️ 系统设置";
            groupBoxTts.ResumeLayout(false);
            groupBoxTts.PerformLayout();
            ((ISupportInitialize)trackBarVolume).EndInit();
            ((ISupportInitialize)trackBarSpeed).EndInit();
            groupBoxInterface.ResumeLayout(false);
            ((ISupportInitialize)numericUpDownFontSize).EndInit();
            groupBoxTranslation.ResumeLayout(false);
            groupBoxTranslation.PerformLayout();
            headerPanel.ResumeLayout(false);
            groupBoxLearningSettings.ResumeLayout(false);
            ((ISupportInitialize)numericUpDownPronunciationScope).EndInit();
            ResumeLayout(false);
        }

        #endregion

        #region Event Handlers

        private void TrackBarSpeed_Scroll(object? sender, EventArgs e)
        {
            labelSpeedValue.Text = $"{trackBarSpeed.Value}%";
        }

        private void TrackBarVolume_Scroll(object? sender, EventArgs e)
        {
            labelVolumeValue.Text = $"{trackBarVolume.Value}%";
        }

        private void ButtonSave_Click(object? sender, EventArgs e)
        {
            SaveClicked?.Invoke(this, EventArgs.Empty);
        }

        private void ButtonCancel_Click(object? sender, EventArgs e)
        {
            CancelClicked?.Invoke(this, EventArgs.Empty);
        }

        private void CheckBoxNightMode_CheckedChanged(object? sender, EventArgs e)
        {
            _isDarkMode = checkBoxNightMode.Checked;
            comboBoxTheme.Text = _isDarkMode ? "Dark" : "Light";

            _themeService.SetTheme(_isDarkMode ? ThemeMode.Dark : ThemeMode.Light);
        }

        private void ApplyTheme(bool isDark)
        {
            if (isDark)
            {
                BackColor = ThemeHelper.Colors.DarkBackground;
                headerPanel.BackColor = ThemeHelper.Colors.DarkGray;

                foreach (Control control in Controls)
                {
                    if (control is GroupBox groupBox)
                    {
                        groupBox.BackColor = ThemeHelper.Colors.DarkSurface;
                        groupBox.ForeColor = ThemeHelper.Colors.DarkTextPrimary;
                        foreach (Control child in groupBox.Controls)
                        {
                            if (child is Label label)
                                label.ForeColor = ThemeHelper.Colors.DarkTextPrimary;
                            else if (child is TextBox textBox)
                            {
                                textBox.BackColor = ThemeHelper.Colors.DarkSurface;
                                textBox.ForeColor = ThemeHelper.Colors.DarkTextPrimary;
                            }
                            else if (child is ComboBox comboBox)
                            {
                                comboBox.BackColor = ThemeHelper.Colors.DarkSurface;
                                comboBox.ForeColor = ThemeHelper.Colors.DarkTextPrimary;
                            }
                            else if (child is NumericUpDown numericUpDown)
                            {
                                numericUpDown.BackColor = ThemeHelper.Colors.DarkSurface;
                                numericUpDown.ForeColor = ThemeHelper.Colors.DarkTextPrimary;
                            }
                            else if (child is TrackBar trackBar)
                            {
                                trackBar.BackColor = ThemeHelper.Colors.DarkSurface;
                            }
                        }
                    }
                }

                checkBoxNightMode.Text = "☀️ 日间";
                checkBoxNightMode.BackColor = ThemeHelper.Colors.Gold;
            }
            else
            {
                BackColor = ThemeHelper.Colors.WarmBeige;
                headerPanel.BackColor = ThemeHelper.Colors.Orange;

                foreach (Control control in Controls)
                {
                    if (control is GroupBox groupBox)
                    {
                        groupBox.BackColor = ThemeHelper.Colors.WarmCream;
                        groupBox.ForeColor = ThemeHelper.Colors.TextDark;
                        foreach (Control child in groupBox.Controls)
                        {
                            if (child is Label label)
                                label.ForeColor = ThemeHelper.Colors.TextDark;
                            else if (child is TextBox textBox)
                            {
                                textBox.BackColor = Color.White;
                                textBox.ForeColor = ThemeHelper.Colors.TextDark;
                            }
                            else if (child is ComboBox comboBox)
                            {
                                comboBox.BackColor = Color.White;
                                comboBox.ForeColor = ThemeHelper.Colors.TextDark;
                            }
                            else if (child is NumericUpDown numericUpDown)
                            {
                                numericUpDown.BackColor = Color.White;
                                numericUpDown.ForeColor = ThemeHelper.Colors.TextDark;
                            }
                        }
                    }
                }

                checkBoxNightMode.Text = "🌙 夜间";
                checkBoxNightMode.BackColor = ThemeHelper.Colors.DarkSurface;
            }
        }

        private void Button_HoverEnter(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                button.BackColor = Color.FromArgb(
                    Math.Min(255, (int)button.BackColor.R + 20),
                    Math.Min(255, (int)button.BackColor.G + 20),
                    Math.Min(255, (int)button.BackColor.B + 20));
                button.Cursor = Cursors.Hand;
            }
        }

        private void Button_HoverLeave(object? sender, EventArgs e)
        {
            if (sender is Button button)
            {
                if (button.Name == "buttonSave")
                    button.BackColor = ThemeHelper.Colors.Success;
                else if (button.Name == "buttonCancel")
                    button.BackColor = ThemeHelper.Colors.GrayLight;
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
    }
}
