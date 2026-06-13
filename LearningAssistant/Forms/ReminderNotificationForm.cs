using System.ComponentModel;

namespace LearningAssistant.Forms
{
    public partial class ReminderNotificationForm : Form
    {
        private readonly System.Windows.Forms.Timer _fadeInTimer;
        private readonly System.Windows.Forms.Timer _fadeOutTimer;
        private readonly int _displayDuration = 5000;
        private int _currentOpacity = 0;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderTitle { get; set; } = "学习提醒";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderMessage { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderTime { get; set; } = string.Empty;

        public event EventHandler? OpenLearningClicked;
        public event EventHandler? SnoozeClicked;
        public event EventHandler? DismissClicked;

        public ReminderNotificationForm()
        {
            InitializeComponent();

            _fadeInTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeInTimer.Tick += FadeInTimer_Tick;

            _fadeOutTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeOutTimer.Tick += FadeOutTimer_Tick;

            Load += ReminderNotificationForm_Load;
        }

        private void ReminderNotificationForm_Load(object? sender, EventArgs e)
        {
            labelTitle.Text = ReminderTitle;
            labelMessage.Text = ReminderMessage;
            labelTime.Text = ReminderTime;

            ShowFadeIn();
        }

        private void ShowFadeIn()
        {
            Opacity = 0;
            _currentOpacity = 0;
            _fadeInTimer.Start();
        }

        private void FadeInTimer_Tick(object? sender, EventArgs e)
        {
            _currentOpacity += 5;
            Opacity = _currentOpacity / 100.0;

            if (_currentOpacity >= 100)
            {
                _fadeInTimer.Stop();
                StartAutoCloseTimer();
            }
        }

        private void StartAutoCloseTimer()
        {
            var autoCloseTimer = new System.Windows.Forms.Timer { Interval = _displayDuration };
            autoCloseTimer.Tick += (s, e) =>
            {
                autoCloseTimer.Stop();
                ShowFadeOut();
            };
            autoCloseTimer.Start();
        }

        private void ShowFadeOut()
        {
            _currentOpacity = 100;
            _fadeOutTimer.Start();
        }

        private void FadeOutTimer_Tick(object? sender, EventArgs e)
        {
            _currentOpacity -= 5;
            Opacity = _currentOpacity / 100.0;

            if (_currentOpacity <= 0)
            {
                _fadeOutTimer.Stop();
                Close();
            }
        }

        private void ButtonOpenLearning_Click(object? sender, EventArgs e)
        {
            _fadeInTimer.Stop();
            OpenLearningClicked?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void ButtonSnooze_Click(object? sender, EventArgs e)
        {
            _fadeInTimer.Stop();
            SnoozeClicked?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void ButtonDismiss_Click(object? sender, EventArgs e)
        {
            _fadeInTimer.Stop();
            DismissClicked?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private void ReminderNotificationForm_Click(object? sender, EventArgs e)
        {
            OpenLearningClicked?.Invoke(this, EventArgs.Empty);
            Close();
        }

        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private Label labelTitle;
        private Label labelMessage;
        private Label labelTime;
        private Button buttonOpenLearning;
        private Button buttonSnooze;
        private Button buttonDismiss;
        private Panel panelContent;
        private Label labelIcon;

        private void InitializeComponent()
        {
            panelContent = new Panel();
            labelIcon = new Label();
            labelTitle = new Label();
            labelMessage = new Label();
            labelTime = new Label();
            buttonOpenLearning = new Button();
            buttonSnooze = new Button();
            buttonDismiss = new Button();
            panelContent.SuspendLayout();
            SuspendLayout();
            // 
            // panelContent
            // 
            panelContent.BackColor = Color.FromArgb(255, 255, 255);
            panelContent.BorderStyle = BorderStyle.FixedSingle;
            panelContent.Controls.Add(labelIcon);
            panelContent.Controls.Add(labelTitle);
            panelContent.Controls.Add(labelMessage);
            panelContent.Controls.Add(labelTime);
            panelContent.Controls.Add(buttonOpenLearning);
            panelContent.Controls.Add(buttonSnooze);
            panelContent.Controls.Add(buttonDismiss);
            panelContent.Dock = DockStyle.Fill;
            panelContent.Location = new Point(0, 0);
            panelContent.Name = "panelContent";
            panelContent.Size = new Size(437, 200);
            panelContent.TabIndex = 0;
            // 
            // labelIcon
            // 
            labelIcon.AutoSize = true;
            labelIcon.Font = new Font("Segoe UI", 48F);
            labelIcon.Location = new Point(20, 20);
            labelIcon.Name = "labelIcon";
            labelIcon.Size = new Size(118, 86);
            labelIcon.TabIndex = 0;
            labelIcon.Text = "📚";
            // 
            // labelTitle
            // 
            labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelTitle.Location = new Point(95, 25);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(260, 30);
            labelTitle.TabIndex = 1;
            labelTitle.Text = "学习提醒";
            // 
            // labelMessage
            // 
            labelMessage.Font = new Font("微软雅黑", 11F);
            labelMessage.ForeColor = Color.FromArgb(66, 66, 66);
            labelMessage.Location = new Point(132, 55);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(260, 40);
            labelMessage.TabIndex = 2;
            labelMessage.Text = "是时候进行学习了！保持每日学习习惯，加油！";
            // 
            // labelTime
            // 
            labelTime.Font = new Font("微软雅黑", 9F);
            labelTime.ForeColor = Color.FromArgb(150, 150, 150);
            labelTime.Location = new Point(95, 105);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(260, 20);
            labelTime.TabIndex = 3;
            labelTime.Text = "提醒时间";
            // 
            // buttonOpenLearning
            // 
            buttonOpenLearning.BackColor = Color.FromArgb(66, 133, 244);
            buttonOpenLearning.FlatAppearance.BorderSize = 0;
            buttonOpenLearning.FlatStyle = FlatStyle.Flat;
            buttonOpenLearning.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonOpenLearning.ForeColor = Color.White;
            buttonOpenLearning.Location = new Point(33, 142);
            buttonOpenLearning.Name = "buttonOpenLearning";
            buttonOpenLearning.Size = new Size(120, 35);
            buttonOpenLearning.TabIndex = 4;
            buttonOpenLearning.Text = "开始学习";
            buttonOpenLearning.UseVisualStyleBackColor = false;
            buttonOpenLearning.Click += ButtonOpenLearning_Click;
            // 
            // buttonSnooze
            // 
            buttonSnooze.BackColor = Color.FromArgb(255, 193, 7);
            buttonSnooze.FlatAppearance.BorderSize = 0;
            buttonSnooze.FlatStyle = FlatStyle.Flat;
            buttonSnooze.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonSnooze.ForeColor = Color.White;
            buttonSnooze.Location = new Point(158, 142);
            buttonSnooze.Name = "buttonSnooze";
            buttonSnooze.Size = new Size(110, 35);
            buttonSnooze.TabIndex = 5;
            buttonSnooze.Text = "稍后提醒";
            buttonSnooze.UseVisualStyleBackColor = false;
            buttonSnooze.Click += ButtonSnooze_Click;
            // 
            // buttonDismiss
            // 
            buttonDismiss.BackColor = Color.FromArgb(158, 158, 158);
            buttonDismiss.FlatAppearance.BorderSize = 0;
            buttonDismiss.FlatStyle = FlatStyle.Flat;
            buttonDismiss.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonDismiss.ForeColor = Color.White;
            buttonDismiss.Location = new Point(273, 142);
            buttonDismiss.Name = "buttonDismiss";
            buttonDismiss.Size = new Size(100, 35);
            buttonDismiss.TabIndex = 6;
            buttonDismiss.Text = "忽略";
            buttonDismiss.UseVisualStyleBackColor = false;
            buttonDismiss.Click += ButtonDismiss_Click;
            // 
            // ReminderNotificationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(437, 200);
            Controls.Add(panelContent);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ReminderNotificationForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "学习提醒";
            TopMost = true;
            Click += ReminderNotificationForm_Click;
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
