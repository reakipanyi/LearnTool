using System.ComponentModel;
using System.Drawing.Drawing2D;
using LearningAssistant.Common.UI;
using LearningAssistant.Services.Learning;

namespace LearningAssistant.Forms
{
    public partial class ReminderNotificationForm : Form
    {
        #region 字段

        private readonly System.Windows.Forms.Timer _fadeInTimer;
        private readonly System.Windows.Forms.Timer _fadeOutTimer;
        private readonly int _displayDuration = 6000;
        private int _currentOpacity = 0;
        private int _borderRadius = 16;
        private bool _isHovered;
        private ReminderType _reminderType = ReminderType.Study;

        #endregion

        #region 属性

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderTitle { get; set; } = "学习提醒";
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderMessage { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderTime { get; set; } = string.Empty;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public ReminderType ReminderType
        {
            get => _reminderType;
            set
            {
                _reminderType = value;
                Invalidate();
            }
        }
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string ReminderIcon { get; set; } = "📚";

        #endregion

        #region 事件

        public event EventHandler? OpenLearningClicked;
        public event EventHandler? SnoozeClicked;
        public event EventHandler? DismissClicked;

        #endregion

        #region 构造函数

        public ReminderNotificationForm()
        {
            InitializeComponent();

            _fadeInTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeInTimer.Tick += FadeInTimer_Tick;

            _fadeOutTimer = new System.Windows.Forms.Timer { Interval = 20 };
            _fadeOutTimer.Tick += FadeOutTimer_Tick;

            Load += ReminderNotificationForm_Load;
            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
        }

        #endregion

        #region 加载与动画

        private void ReminderNotificationForm_Load(object? sender, EventArgs e)
        {
            labelTitle.Text = ReminderTitle;
            labelMessage.Text = ReminderMessage;
            labelTime.Text = ReminderTime;
            labelIcon.Text = ReminderIcon;

            UpdateButtonColors();
            ShowFadeIn();
        }

        private void UpdateButtonColors()
        {
            var colors = GetTypeColors(_reminderType);
            buttonOpenLearning.BackColor = colors.primary;
            buttonOpenLearning.FlatAppearance.MouseOverBackColor = colors.hover;
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
                if (!_isHovered)
                {
                    ShowFadeOut();
                }
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

        #endregion

        #region 绘制

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = ClientRectangle;
            var shadowRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            using var shadowPath = GdiHelper.CreateRoundedRectPath(shadowRect, _borderRadius);
            using var shadowBrush = new SolidBrush(Color.FromArgb(40, 0, 0, 0));
            g.FillPath(shadowBrush, shadowPath);

            var cardRect = new Rectangle(rect.X + (_isHovered ? 1 : 2), rect.Y + (_isHovered ? 1 : 2),
                rect.Width - (_isHovered ? 2 : 4), rect.Height - (_isHovered ? 2 : 4));

            using var cardPath = GdiHelper.CreateRoundedRectPath(cardRect, _borderRadius);
            var colors = GetTypeColors(_reminderType);
            using var bgBrush = new LinearGradientBrush(
                cardRect,
                Color.White,
                Color.FromArgb(250, 250, 252),
                LinearGradientMode.Vertical);
            g.FillPath(bgBrush, cardPath);

            var accentRect = new Rectangle(cardRect.X, cardRect.Y, 5, cardRect.Height);
            using var accentPath = GdiHelper.CreateRoundedRectPath(accentRect, 3);
            using var accentBrush = new LinearGradientBrush(
                accentRect,
                colors.primary,
                colors.secondary,
                LinearGradientMode.Vertical);
            g.FillPath(accentBrush, accentPath);

            using var borderPen = new Pen(Color.FromArgb(30, 0, 0, 0), 1);
            g.DrawPath(borderPen, cardPath);
        }

        private static (Color primary, Color secondary, Color hover, Color light) GetTypeColors(ReminderType type) => type switch
        {
            ReminderType.Study => (Color.FromArgb(66, 133, 244), Color.FromArgb(26, 115, 232), Color.FromArgb(25, 103, 210), Color.FromArgb(210, 227, 252)),
            ReminderType.Review => (Color.FromArgb(156, 39, 176), Color.FromArgb(123, 31, 162), Color.FromArgb(106, 27, 154), Color.FromArgb(225, 190, 231)),
            ReminderType.Rest => (Color.FromArgb(76, 175, 80), Color.FromArgb(56, 142, 60), Color.FromArgb(46, 125, 50), Color.FromArgb(200, 230, 201)),
            ReminderType.Water => (Color.FromArgb(33, 150, 243), Color.FromArgb(21, 101, 192), Color.FromArgb(25, 118, 210), Color.FromArgb(187, 222, 251)),
            _ => (Color.FromArgb(255, 152, 0), Color.FromArgb(245, 124, 0), Color.FromArgb(230, 111, 0), Color.FromArgb(255, 224, 178))
        };

        #endregion

        #region 按钮事件处理

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

        #endregion

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
            panelContent.BackColor = Color.Transparent;
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
            panelContent.Padding = new Padding(20, 15, 20, 15);
            panelContent.Size = new Size(437, 200);
            panelContent.TabIndex = 0;
            // 
            // labelIcon
            // 
            labelIcon.AutoSize = true;
            labelIcon.Font = new Font("Segoe UI Emoji", 36F);
            labelIcon.Location = new Point(25, 20);
            labelIcon.Name = "labelIcon";
            labelIcon.Size = new Size(88, 65);
            labelIcon.TabIndex = 0;
            labelIcon.Text = "📚";
            // 
            // labelTitle
            // 
            labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            labelTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelTitle.Location = new Point(100, 22);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(300, 30);
            labelTitle.TabIndex = 1;
            labelTitle.Text = "学习提醒";
            // 
            // labelMessage
            // 
            labelMessage.Font = new Font("微软雅黑", 10.5F);
            labelMessage.ForeColor = Color.FromArgb(90, 90, 90);
            labelMessage.Location = new Point(100, 52);
            labelMessage.Name = "labelMessage";
            labelMessage.Size = new Size(300, 45);
            labelMessage.TabIndex = 2;
            labelMessage.Text = "是时候进行学习了！保持每日学习习惯，加油！";
            // 
            // labelTime
            // 
            labelTime.Font = new Font("微软雅黑", 9F);
            labelTime.ForeColor = Color.FromArgb(160, 160, 160);
            labelTime.Location = new Point(100, 95);
            labelTime.Name = "labelTime";
            labelTime.Size = new Size(300, 20);
            labelTime.TabIndex = 3;
            labelTime.Text = "提醒时间";
            // 
            // buttonOpenLearning
            // 
            buttonOpenLearning.BackColor = Color.FromArgb(66, 133, 244);
            buttonOpenLearning.FlatAppearance.BorderSize = 0;
            buttonOpenLearning.FlatAppearance.MouseOverBackColor = Color.FromArgb(25, 103, 210);
            buttonOpenLearning.FlatStyle = FlatStyle.Flat;
            buttonOpenLearning.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            buttonOpenLearning.ForeColor = Color.White;
            buttonOpenLearning.Location = new Point(30, 132);
            buttonOpenLearning.Name = "buttonOpenLearning";
            buttonOpenLearning.Size = new Size(130, 38);
            buttonOpenLearning.TabIndex = 4;
            buttonOpenLearning.Text = "开始学习";
            buttonOpenLearning.UseVisualStyleBackColor = false;
            buttonOpenLearning.Click += ButtonOpenLearning_Click;
            // 
            // buttonSnooze
            // 
            buttonSnooze.BackColor = Color.White;
            buttonSnooze.FlatAppearance.BorderColor = Color.FromArgb(200, 200, 200);
            buttonSnooze.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
            buttonSnooze.FlatStyle = FlatStyle.Flat;
            buttonSnooze.Font = new Font("微软雅黑", 10F);
            buttonSnooze.ForeColor = Color.FromArgb(80, 80, 80);
            buttonSnooze.Location = new Point(165, 132);
            buttonSnooze.Name = "buttonSnooze";
            buttonSnooze.Size = new Size(120, 38);
            buttonSnooze.TabIndex = 5;
            buttonSnooze.Text = "5分钟后提醒";
            buttonSnooze.UseVisualStyleBackColor = false;
            buttonSnooze.Click += ButtonSnooze_Click;
            // 
            // buttonDismiss
            // 
            buttonDismiss.BackColor = Color.Transparent;
            buttonDismiss.FlatAppearance.BorderSize = 0;
            buttonDismiss.FlatAppearance.MouseOverBackColor = Color.FromArgb(240, 240, 240);
            buttonDismiss.FlatStyle = FlatStyle.Flat;
            buttonDismiss.Font = new Font("微软雅黑", 9F);
            buttonDismiss.ForeColor = Color.FromArgb(150, 150, 150);
            buttonDismiss.Location = new Point(355, 132);
            buttonDismiss.Name = "buttonDismiss";
            buttonDismiss.Size = new Size(60, 38);
            buttonDismiss.TabIndex = 6;
            buttonDismiss.Text = "忽略";
            buttonDismiss.UseVisualStyleBackColor = false;
            buttonDismiss.Click += ButtonDismiss_Click;
            // 
            // ReminderNotificationForm
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(245, 245, 245);
            ClientSize = new Size(437, 200);
            Controls.Add(panelContent);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ReminderNotificationForm";
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterScreen;
            Text = "学习提醒";
            TopMost = true;
            TransparencyKey = Color.FromArgb(245, 245, 245);
            Click += ReminderNotificationForm_Click;
            panelContent.ResumeLayout(false);
            panelContent.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
    }
}
