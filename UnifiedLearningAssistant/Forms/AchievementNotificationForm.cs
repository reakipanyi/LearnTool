
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Feedback;
using UnifiedLearningAssistant.Views.UI;

namespace UnifiedLearningAssistant.Forms
{
    public partial class AchievementNotificationForm : Form
    {
        private readonly Timer _displayTimer = new Timer();
        private readonly Timer _fadeTimer = new Timer();
        private float _opacityValue = 0;
        private ConfettiControl? _confetti;
        private readonly ISoundService _soundService;

        public AchievementNotificationForm(Achievement achievement)
        {
            InitializeComponent();
            SetAchievementData(achievement);
            SetupAnimations();
            _soundService = new SoundService();
        }

        public AchievementNotificationForm(Achievement achievement, ISoundService soundService)
        {
            InitializeComponent();
            SetAchievementData(achievement);
            SetupAnimations();
            _soundService = soundService ?? new SoundService();
        }

        private void SetAchievementData(Achievement achievement)
        {
            lblIcon.Text = achievement.Icon;
            lblTitle.Text = "成就解锁！";
            lblAchievementName.Text = achievement.Name;
            lblDescription.Text = achievement.Description;
        }

        private void SetupAnimations()
        {
            _displayTimer.Interval = 3000;
            _displayTimer.Tick += OnDisplayTimerTick;

            _fadeTimer.Interval = 30;
            _fadeTimer.Tick += OnFadeTimerTick;

            Load += (s, e) => _fadeTimer.Start();
        }

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

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.BackColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(380, 120);
            this.StartPosition = FormStartPosition.Manual;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 0;

            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(15)
            };

            var contentPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };

            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            contentPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
            contentPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            lblIcon = new Label
            {
                Text = "🏆",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 32, FontStyle.Regular),
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "成就解锁！",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft,
                Font = new Font("Microsoft YaHei", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(255, 152, 0),
                BackColor = Color.Transparent
            };

            lblAchievementName = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Microsoft YaHei", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                BackColor = Color.Transparent
            };

            lblDescription = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Microsoft YaHei", 9),
                ForeColor = Color.FromArgb(117, 117, 117),
                BackColor = Color.Transparent
            };

            contentPanel.Controls.Add(lblIcon, 0, 0);
            contentPanel.SetRowSpan(lblIcon, 3);
            contentPanel.Controls.Add(lblTitle, 1, 0);
            contentPanel.Controls.Add(lblAchievementName, 1, 1);
            contentPanel.Controls.Add(lblDescription, 1, 2);

            mainPanel.Controls.Add(contentPanel);
            this.Controls.Add(mainPanel);

            this.Region = System.Drawing.Region.FromHrgn(
                CreateRoundRectRgn(0, 0, this.Width, this.Height, 10, 10)
            );

            this.ResumeLayout(false);
        }

        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int cx, int cy);

        private Label lblIcon = null!;
        private Label lblTitle = null!;
        private Label lblAchievementName = null!;
        private Label lblDescription = null!;
    }
}
