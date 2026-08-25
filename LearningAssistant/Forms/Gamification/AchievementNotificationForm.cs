using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Gamification;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Feedback;
using System.ComponentModel;

namespace LearningAssistant.Forms.Gamification
{
    public partial class AchievementNotificationForm : Form
    {
        #region 字段

        private readonly System.Windows.Forms.Timer _displayTimer = new System.Windows.Forms.Timer();
        private readonly System.Windows.Forms.Timer _fadeTimer = new System.Windows.Forms.Timer();
        private float _opacityValue = 0;
        private ConfettiControl? _confetti;
        private readonly ISoundService _soundService;

        #endregion

        #region 构造函数

        public AchievementNotificationForm(Achievement achievement)
        {
            InitializeComponent();

            // 关键：仅运行时创建圆角区域，设计器跳过避免报错
            if (!DesignMode)
            {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 10, 10));
            }
            SetAchievementData(achievement);
            SetupAnimations();
            _soundService = new SoundService();
        }

        public AchievementNotificationForm(Achievement achievement, ISoundService soundService)
        {
            InitializeComponent();
            // 关键：仅运行时创建圆角区域，设计器跳过避免报错
            if (!DesignMode)
            {
                this.Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, this.Width, this.Height, 10, 10));
            }
            SetAchievementData(achievement);
            SetupAnimations();
            _soundService = soundService ?? new SoundService();
        }

        #endregion

        #region 初始化方法

        private void SetAchievementData(Achievement achievement)
        {
            bool isHidden = achievement.IsHidden;
            lblIcon.Text = achievement.Icon;
            lblTitle.Text = isHidden ? "🔮 隐藏成就解锁！" : "成就解锁！";
            lblAchievementName.Text = achievement.Name;
            lblDescription.Text = isHidden ? $"【隐藏成就】{achievement.Description}" : achievement.Description;

            if (isHidden)
            {
                lblTitle.ForeColor = Color.FromArgb(156, 39, 176);
                BackColor = Color.FromArgb(250, 245, 255);
            }
        }

        private void SetupAnimations()
        {
            _displayTimer.Interval = 3000;
            _displayTimer.Tick += OnDisplayTimerTick;

            _fadeTimer.Interval = 30;
            _fadeTimer.Tick += OnFadeTimerTick;

            Load += (s, e) => _fadeTimer.Start();
        }

        #endregion

        #region 动画事件处理

        private void OnFadeTimerTick(object? sender, EventArgs e)
        {
            _opacityValue += 0.05f;
            if (_opacityValue >= 1)
            {
                _opacityValue = 1;
                _fadeTimer.Stop();
                _displayTimer.Start();
            }
            Opacity = _opacityValue;
        }

        private void OnDisplayTimerTick(object? sender, EventArgs e)
        {
            _displayTimer.Stop();
            _fadeTimer.Tick -= OnFadeTimerTick;
            _fadeTimer.Tick += OnFadeOutTimerTick;
            _fadeTimer.Start();
        }

        private void OnFadeOutTimerTick(object? sender, EventArgs e)
        {
            _opacityValue -= 0.05f;
            if (_opacityValue <= 0)
            {
                _opacityValue = 0;
                _fadeTimer.Stop();
                _confetti?.StopCelebration();
                Close();
            }
            Opacity = _opacityValue;
        }

        #endregion

        #region 重写方法

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var screen = Screen.PrimaryScreen;
            if (screen != null)
            {
                Location = new Point(
                    screen.WorkingArea.Right - Width - 20,
                    screen.WorkingArea.Bottom - Height - 20
                );
            }

            _confetti = new ConfettiControl
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            Controls.Add(_confetti);
            _confetti.BringToFront();
            _confetti.StartCelebration();

            _soundService?.PlayAchievement();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _displayTimer.Dispose();
                _fadeTimer.Dispose();
                _confetti?.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region Windows Form Designer generated code

        #region 窗体控件字段（设计器识别必备）

        private Label lblIcon = null!;
        private Label lblTitle = null!;
        private Label lblAchievementName = null!;
        private Label lblDescription = null!;

        private IContainer components;
        private Panel mainPanel;
        private TableLayoutPanel contentPanel;

        #endregion


        private void InitializeComponent()
        {
            this.components = new Container();
            this.SuspendLayout();

            // AchievementNotifyForm
            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(380, 120);
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 0D;

            // mainPanel
            this.mainPanel = new Panel();
            this.mainPanel.Dock = DockStyle.Fill;
            this.mainPanel.BackColor = Color.White;
            this.mainPanel.Padding = new Padding(15);

            // contentPanel
            this.contentPanel = new TableLayoutPanel();
            this.contentPanel.Dock = DockStyle.Fill;
            this.contentPanel.ColumnCount = 2;
            this.contentPanel.RowCount = 3;
            this.contentPanel.BackColor = Color.Transparent;
            this.contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));
            this.contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            this.contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            this.contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // lblIcon
            this.lblIcon = new Label();
            this.lblIcon.Text = "🏆";
            this.lblIcon.Dock = DockStyle.Fill;
            this.lblIcon.TextAlign = ContentAlignment.MiddleCenter;
            this.lblIcon.Font = new Font("Segoe UI Emoji", 32F, FontStyle.Regular, GraphicsUnit.Point, 0);
            this.lblIcon.BackColor = Color.Transparent;

            // lblTitle
            this.lblTitle = new Label();
            this.lblTitle.Text = "成就解锁！";
            this.lblTitle.Dock = DockStyle.Fill;
            this.lblTitle.TextAlign = ContentAlignment.BottomLeft;
            this.lblTitle.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold, GraphicsUnit.Point, 134);
            this.lblTitle.ForeColor = Color.FromArgb(255, 152, 0);
            this.lblTitle.BackColor = Color.Transparent;

            // lblAchievementName
            this.lblAchievementName = new Label();
            this.lblAchievementName.Dock = DockStyle.Fill;
            this.lblAchievementName.TextAlign = ContentAlignment.MiddleLeft;
            this.lblAchievementName.Font = new Font("Microsoft YaHei", 14F, FontStyle.Bold, GraphicsUnit.Point, 134);
            this.lblAchievementName.ForeColor = Color.FromArgb(33, 33, 33);
            this.lblAchievementName.BackColor = Color.Transparent;

            // lblDescription
            this.lblDescription = new Label();
            this.lblDescription.Dock = DockStyle.Fill;
            this.lblDescription.TextAlign = ContentAlignment.TopLeft;
            this.lblDescription.Font = new Font("Microsoft YaHei", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.lblDescription.ForeColor = Color.FromArgb(117, 117, 117);
            this.lblDescription.BackColor = Color.Transparent;

            // 表格布局挂载控件
            this.contentPanel.Controls.Add(this.lblIcon, 0, 0);
            this.contentPanel.SetRowSpan(this.lblIcon, 3);
            this.contentPanel.Controls.Add(this.lblTitle, 1, 0);
            this.contentPanel.Controls.Add(this.lblAchievementName, 1, 1);
            this.contentPanel.Controls.Add(this.lblDescription, 1, 2);

            this.mainPanel.Controls.Add(this.contentPanel);
            this.Controls.Add(this.mainPanel);

            this.ResumeLayout(false);

        }


        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);



        #endregion
    }
}
