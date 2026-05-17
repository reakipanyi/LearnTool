using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Forms
{
    public partial class SettingForm : Form, ISettingView
    {
        private readonly ILogger<SettingForm> _logger;
        private bool _disposed = false;
        private CheckBox checkBoxNightMode;
        private Panel headerPanel;



        private bool _isDarkMode = false;

        public SettingForm(ILogger<SettingForm> logger)
        {
            InitializeComponent();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region ISettingView Implementation

        public string ApiKey
        {
            get => textBoxApiKey.Text;
            set => textBoxApiKey.Text = value;
        }

        public string ApiEndpoint
        {
            get => textBoxApiEndpoint.Text;
            set => textBoxApiEndpoint.Text = value;
        }

        public string Model
        {
            get => textBoxModel.Text;
            set => textBoxModel.Text = value;
        }

        public bool TTSEnabled
        {
            get => checkBoxTtsEnabled.Checked;
            set => checkBoxTtsEnabled.Checked = value;
        }

        public string TtsApiKey
        {
            get => textBoxTtsApiKey.Text;
            set => textBoxTtsApiKey.Text = value;
        }

        public string TtsVoice
        {
            get => comboBoxVoice.Text;
            set => comboBoxVoice.Text = value;
        }

        public int TTSSpeed
        {
            get => trackBarSpeed.Value;
            set => trackBarSpeed.Value = value;
        }

        public int TTSVolume
        {
            get => trackBarVolume.Value;
            set => trackBarVolume.Value = value;
        }

        public int FontSize
        {
            get => (int)numericUpDownFontSize.Value;
            set => numericUpDownFontSize.Value = value;
        }

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

        public string BaiduAppId
        {
            get => textBoxBaiduAppId.Text;
            set => textBoxBaiduAppId.Text = value;
        }

        public string BaiduSecret
        {
            get => textBoxBaiduSecret.Text;
            set => textBoxBaiduSecret.Text = value;
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
        private GroupBox groupBoxAi;
        private Label labelApiKey;
        private TextBox textBoxApiKey;
        private Label labelApiEndpoint;
        private TextBox textBoxApiEndpoint;
        private Label labelModel;
        private TextBox textBoxModel;
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
        private Button buttonSave;
        private Button buttonCancel;

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            groupBoxAi = new GroupBox();
            labelModel = new Label();
            textBoxModel = new TextBox();
            labelApiEndpoint = new Label();
            textBoxApiEndpoint = new TextBox();
            labelApiKey = new Label();
            textBoxApiKey = new TextBox();
            groupBoxTts = new GroupBox();
            labelVolumeValue = new Label();
            labelVolume = new Label();
            trackBarVolume = new TrackBar();
            labelSpeedValue = new Label();
            labelSpeed = new Label();
            trackBarSpeed = new TrackBar();
            comboBoxVoice = new ComboBox();
            labelVoice = new Label();
            checkBoxTtsEnabled = new CheckBox();
            labelTtsApiKey = new Label();
            textBoxTtsApiKey = new TextBox();
            groupBoxInterface = new GroupBox();
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
            labelTtsApiKey = new Label();
            textBoxTtsApiKey = new TextBox();
            headerPanel = new Panel();
            checkBoxNightMode = new CheckBox();

            groupBoxAi.SuspendLayout();
            groupBoxTts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)trackBarVolume).BeginInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSpeed).BeginInit();
            groupBoxInterface.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownFontSize).BeginInit();
            groupBoxTranslation.SuspendLayout();
            headerPanel.SuspendLayout();
            SuspendLayout();

            BackColor = LightBackground;

            headerPanel.BackColor = WarmOrange;
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Location = new Point(0, 0);
            headerPanel.Name = "headerPanel";
            headerPanel.Size = new Size(580, 50);
            headerPanel.TabIndex = 6;

            var labelTitle = new Label();
            labelTitle.Font = new Font("Microsoft YaHei", 16F, FontStyle.Bold);
            labelTitle.ForeColor = Color.White;
            labelTitle.Location = new Point(180, 8);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(220, 35);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "⚙️ 系统设置";
            labelTitle.TextAlign = ContentAlignment.MiddleCenter;
            headerPanel.Controls.Add(labelTitle);

            checkBoxNightMode.Appearance = Appearance.Button;
            checkBoxNightMode.BackColor = DarkSurface;
            checkBoxNightMode.FlatAppearance.BorderSize = 0;
            checkBoxNightMode.FlatStyle = FlatStyle.Flat;
            checkBoxNightMode.Font = new Font("Microsoft YaHei", 9F, FontStyle.Bold);
            checkBoxNightMode.ForeColor = Color.White;
            checkBoxNightMode.Location = new Point(470, 8);
            checkBoxNightMode.Name = "checkBoxNightMode";
            checkBoxNightMode.Size = new Size(100, 35);
            checkBoxNightMode.TabIndex = 7;
            checkBoxNightMode.Text = "🌙 夜间";
            checkBoxNightMode.TextAlign = ContentAlignment.MiddleCenter;
            checkBoxNightMode.UseVisualStyleBackColor = false;
            checkBoxNightMode.CheckedChanged += CheckBoxNightMode_CheckedChanged;

            groupBoxAi.BackColor = LightSurface;
            groupBoxAi.FlatStyle = FlatStyle.Flat;
            groupBoxAi.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            groupBoxAi.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxAi.Controls.Add(labelModel);
            groupBoxAi.Controls.Add(textBoxModel);
            groupBoxAi.Controls.Add(labelApiEndpoint);
            groupBoxAi.Controls.Add(textBoxApiEndpoint);
            groupBoxAi.Controls.Add(labelApiKey);
            groupBoxAi.Controls.Add(textBoxApiKey);
            groupBoxAi.Location = new Point(15, 60);
            groupBoxAi.Name = "groupBoxAi";
            groupBoxAi.Size = new Size(550, 120);
            groupBoxAi.TabIndex = 0;
            groupBoxAi.TabStop = false;
            groupBoxAi.Text = "🤖 AI 接口配置";

            labelModel.ForeColor = Color.FromArgb(33, 33, 33);
            labelModel.Location = new Point(15, 90);
            labelModel.Name = "labelModel";
            labelModel.Size = new Size(100, 20);
            labelModel.TabIndex = 5;
            labelModel.Text = "模型:";

            textBoxModel.Location = new Point(120, 87);
            textBoxModel.Size = new Size(400, 23);
            textBoxModel.TabIndex = 4;

            labelApiEndpoint.ForeColor = Color.FromArgb(33, 33, 33);
            labelApiEndpoint.Location = new Point(15, 55);
            labelApiEndpoint.Name = "labelApiEndpoint";
            labelApiEndpoint.Size = new Size(100, 20);
            labelApiEndpoint.TabIndex = 3;
            labelApiEndpoint.Text = "API端点:";

            textBoxApiEndpoint.Location = new Point(120, 52);
            textBoxApiEndpoint.Size = new Size(400, 23);
            textBoxApiEndpoint.TabIndex = 2;

            labelApiKey.ForeColor = Color.FromArgb(33, 33, 33);
            labelApiKey.Location = new Point(15, 20);
            labelApiKey.Name = "labelApiKey";
            labelApiKey.Size = new Size(100, 20);
            labelApiKey.TabIndex = 1;
            labelApiKey.Text = "API Key:";

            textBoxApiKey.Location = new Point(120, 17);
            textBoxApiKey.Size = new Size(400, 23);
            textBoxApiKey.TabIndex = 0;

            groupBoxTts.BackColor = LightSurface;
            groupBoxTts.FlatStyle = FlatStyle.Flat;
            groupBoxTts.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            groupBoxTts.ForeColor = Color.FromArgb(33, 33, 33);
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
            groupBoxTts.Location = new Point(15, 190);
            groupBoxTts.Name = "groupBoxTts";
            groupBoxTts.Size = new Size(550, 160);
            groupBoxTts.TabIndex = 1;
            groupBoxTts.TabStop = false;
            groupBoxTts.Text = "🔊 千问TTS 语音设置";

            labelVolumeValue.ForeColor = Color.FromArgb(33, 33, 33);
            labelVolumeValue.Location = new Point(500, 122);
            labelVolumeValue.Name = "labelVolumeValue";
            labelVolumeValue.Size = new Size(40, 20);
            labelVolumeValue.TabIndex = 10;
            labelVolumeValue.Text = "100%";

            labelVolume.ForeColor = Color.FromArgb(33, 33, 33);
            labelVolume.Location = new Point(15, 122);
            labelVolume.Name = "labelVolume";
            labelVolume.Size = new Size(60, 20);
            labelVolume.TabIndex = 9;
            labelVolume.Text = "音量:";

            trackBarVolume.Location = new Point(80, 120);
            trackBarVolume.Maximum = 100;
            trackBarVolume.Name = "trackBarVolume";
            trackBarVolume.Size = new Size(410, 25);
            trackBarVolume.TabIndex = 8;
            trackBarVolume.Value = 100;
            trackBarVolume.Scroll += TrackBarVolume_Scroll;

            labelSpeedValue.ForeColor = Color.FromArgb(33, 33, 33);
            labelSpeedValue.Location = new Point(500, 87);
            labelSpeedValue.Name = "labelSpeedValue";
            labelSpeedValue.Size = new Size(40, 20);
            labelSpeedValue.TabIndex = 7;
            labelSpeedValue.Text = "100%";

            labelSpeed.ForeColor = Color.FromArgb(33, 33, 33);
            labelSpeed.Location = new Point(15, 87);
            labelSpeed.Name = "labelSpeed";
            labelSpeed.Size = new Size(60, 20);
            labelSpeed.TabIndex = 6;
            labelSpeed.Text = "速度:";

            trackBarSpeed.Location = new Point(80, 85);
            trackBarSpeed.Maximum = 200;
            trackBarSpeed.Minimum = 50;
            trackBarSpeed.Name = "trackBarSpeed";
            trackBarSpeed.Size = new Size(410, 25);
            trackBarSpeed.TabIndex = 5;
            trackBarSpeed.Value = 100;
            trackBarSpeed.Scroll += TrackBarSpeed_Scroll;

            textBoxTtsApiKey.Location = new Point(120, 50);
            textBoxTtsApiKey.Name = "textBoxTtsApiKey";
            textBoxTtsApiKey.Size = new Size(400, 23);
            textBoxTtsApiKey.TabIndex = 4;
            textBoxTtsApiKey.PasswordChar = '*';

            labelTtsApiKey.ForeColor = Color.FromArgb(33, 33, 33);
            labelTtsApiKey.Location = new Point(15, 53);
            labelTtsApiKey.Name = "labelTtsApiKey";
            labelTtsApiKey.Size = new Size(100, 20);
            labelTtsApiKey.TabIndex = 3;
            labelTtsApiKey.Text = "DashScope Key:";

            comboBoxVoice.FormattingEnabled = true;
            comboBoxVoice.Items.AddRange(new object[] { "Aria", "Cherry", "Xiaobei", "Xiaoning" });
            comboBoxVoice.Location = new Point(80, 15);
            comboBoxVoice.Name = "comboBoxVoice";
            comboBoxVoice.Size = new Size(150, 23);
            comboBoxVoice.TabIndex = 2;

            labelVoice.ForeColor = Color.FromArgb(33, 33, 33);
            labelVoice.Location = new Point(15, 18);
            labelVoice.Name = "labelVoice";
            labelVoice.Size = new Size(60, 20);
            labelVoice.TabIndex = 1;
            labelVoice.Text = "声音:";

            checkBoxTtsEnabled.ForeColor = Color.FromArgb(33, 33, 33);
            checkBoxTtsEnabled.Location = new Point(250, 15);
            checkBoxTtsEnabled.Name = "checkBoxTtsEnabled";
            checkBoxTtsEnabled.Size = new Size(100, 25);
            checkBoxTtsEnabled.TabIndex = 0;
            checkBoxTtsEnabled.Text = "启用TTS";

            groupBoxInterface.BackColor = LightSurface;
            groupBoxInterface.FlatStyle = FlatStyle.Flat;
            groupBoxInterface.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            groupBoxInterface.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxInterface.Controls.Add(checkBoxNightMode);
            groupBoxInterface.Controls.Add(labelTheme);
            groupBoxInterface.Controls.Add(comboBoxTheme);
            groupBoxInterface.Controls.Add(numericUpDownFontSize);
            groupBoxInterface.Controls.Add(labelFontSize);
            groupBoxInterface.Location = new Point(15, 360);
            groupBoxInterface.Name = "groupBoxInterface";
            groupBoxInterface.Size = new Size(550, 80);
            groupBoxInterface.TabIndex = 2;
            groupBoxInterface.TabStop = false;
            groupBoxInterface.Text = "🎨 界面与字体";

            labelTheme.ForeColor = Color.FromArgb(33, 33, 33);
            labelTheme.Location = new Point(180, 20);
            labelTheme.Name = "labelTheme";
            labelTheme.Size = new Size(60, 20);
            labelTheme.TabIndex = 3;
            labelTheme.Text = "主题:";

            comboBoxTheme.FormattingEnabled = true;
            comboBoxTheme.Items.AddRange(new object[] { "Light", "Dark" });
            comboBoxTheme.Location = new Point(250, 17);
            comboBoxTheme.Name = "comboBoxTheme";
            comboBoxTheme.Size = new Size(170, 23);
            comboBoxTheme.TabIndex = 2;

            numericUpDownFontSize.Location = new Point(120, 17);
            numericUpDownFontSize.Maximum = new decimal(new int[] { 32, 0, 0, 0 });
            numericUpDownFontSize.Minimum = new decimal(new int[] { 8, 0, 0, 0 });
            numericUpDownFontSize.Name = "numericUpDownFontSize";
            numericUpDownFontSize.Size = new Size(60, 23);
            numericUpDownFontSize.TabIndex = 1;
            numericUpDownFontSize.Value = new decimal(new int[] { 12, 0, 0, 0 });

            labelFontSize.ForeColor = Color.FromArgb(33, 33, 33);
            labelFontSize.Location = new Point(15, 20);
            labelFontSize.Name = "labelFontSize";
            labelFontSize.Size = new Size(100, 20);
            labelFontSize.TabIndex = 0;
            labelFontSize.Text = "字体大小:";

            labelTtsApiKey.Location = new Point(15, 53);
            textBoxTtsApiKey.Location = new Point(120, 50);
            textBoxTtsApiKey.PasswordChar = '*';

            groupBoxTranslation.BackColor = LightSurface;
            groupBoxTranslation.FlatStyle = FlatStyle.Flat;
            groupBoxTranslation.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            groupBoxTranslation.ForeColor = Color.FromArgb(33, 33, 33);
            groupBoxTranslation.Controls.Add(textBoxBaiduSecret);
            groupBoxTranslation.Controls.Add(labelBaiduSecret);
            groupBoxTranslation.Controls.Add(textBoxBaiduAppId);
            groupBoxTranslation.Controls.Add(labelBaiduAppId);
            groupBoxTranslation.Location = new Point(15, 450);
            groupBoxTranslation.Name = "groupBoxTranslation";
            groupBoxTranslation.Size = new Size(550, 80);
            groupBoxTranslation.TabIndex = 3;
            groupBoxTranslation.TabStop = false;
            groupBoxTranslation.Text = "🌐 翻译服务";

            textBoxBaiduSecret.Location = new Point(350, 20);
            textBoxBaiduSecret.Size = new Size(170, 23);
            textBoxBaiduSecret.TabIndex = 3;

            labelBaiduSecret.ForeColor = Color.FromArgb(33, 33, 33);
            labelBaiduSecret.Location = new Point(280, 23);
            labelBaiduSecret.Name = "labelBaiduSecret";
            labelBaiduSecret.Size = new Size(60, 20);
            labelBaiduSecret.TabIndex = 2;
            labelBaiduSecret.Text = "密钥:";

            textBoxBaiduAppId.Location = new Point(120, 20);
            textBoxBaiduAppId.Size = new Size(150, 23);
            textBoxBaiduAppId.TabIndex = 1;

            labelBaiduAppId.ForeColor = Color.FromArgb(33, 33, 33);
            labelBaiduAppId.Location = new Point(15, 23);
            labelBaiduAppId.Name = "labelBaiduAppId";
            labelBaiduAppId.Size = new Size(100, 20);
            labelBaiduAppId.TabIndex = 0;
            labelBaiduAppId.Text = "百度AppId:";

            buttonSave.FlatStyle = FlatStyle.Flat;
            buttonSave.FlatAppearance.BorderSize = 0;
            buttonSave.BackColor = SuccessGreen;
            buttonSave.ForeColor = Color.White;
            buttonSave.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            buttonSave.Location = new Point(300, 550);
            buttonSave.Name = "buttonSave";
            buttonSave.Size = new Size(120, 40);
            buttonSave.TabIndex = 4;
            buttonSave.Text = "💾 保存并关闭";
            buttonSave.UseVisualStyleBackColor = false;
            buttonSave.MouseEnter += Button_HoverEnter;
            buttonSave.MouseLeave += Button_HoverLeave;
            buttonSave.Click += ButtonSave_Click;

            buttonCancel.FlatStyle = FlatStyle.Flat;
            buttonCancel.FlatAppearance.BorderSize = 0;
            buttonCancel.BackColor = Color.FromArgb(158, 158, 158);
            buttonCancel.ForeColor = Color.White;
            buttonCancel.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            buttonCancel.Location = new Point(430, 550);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(120, 40);
            buttonCancel.TabIndex = 5;
            buttonCancel.Text = "❌ 取消";
            buttonCancel.UseVisualStyleBackColor = false;
            buttonCancel.MouseEnter += Button_HoverEnter;
            buttonCancel.MouseLeave += Button_HoverLeave;
            buttonCancel.Click += ButtonCancel_Click;

            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(580, 610);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSave);
            Controls.Add(groupBoxTranslation);
            Controls.Add(groupBoxInterface);
            Controls.Add(groupBoxTts);
            Controls.Add(groupBoxAi);
            Controls.Add(headerPanel);
            Name = "SettingForm";
            Text = "⚙️ 系统设置";
            groupBoxAi.ResumeLayout(false);
            groupBoxTts.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)trackBarVolume).EndInit();
            ((System.ComponentModel.ISupportInitialize)trackBarSpeed).EndInit();
            groupBoxInterface.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)numericUpDownFontSize).EndInit();
            groupBoxTranslation.ResumeLayout(false);
            headerPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
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
            ApplyTheme(_isDarkMode);
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
                    Math.Min(255, button.BackColor.R + 20),
                    Math.Min(255, button.BackColor.G + 20),
                    Math.Min(255, button.BackColor.B + 20));
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
