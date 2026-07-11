namespace LearningAssistant.Forms
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public class ToastNotification : Form
    {
        private Label _iconLabel;
        private Label _messageLabel;
        private Panel _iconPanel;

        private System.Windows.Forms.Timer _fadeTimer;

        public ToastNotification(string message, ToastType type)
        {
            InitializeComponent();
            SetToastStyle(message, type);
        }

        private void InitializeComponent()
        {
            _iconPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 48,
                BackColor = Color.White
            };

            _iconLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 18F)
            };

            _iconPanel.Controls.Add(_iconLabel);

            _messageLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 11F),
                Padding = new Padding(10, 0, 15, 0),
                ForeColor = Color.White,
                AutoSize = false
            };

            Controls.Add(_messageLabel);
            Controls.Add(_iconPanel);

            StartPosition = FormStartPosition.Manual;
            Size = new Size(320, 60);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            Opacity = 0;

            _fadeTimer = new System.Windows.Forms.Timer();
            _fadeTimer.Tick += FadeTimer_Tick;
        }

        private void SetToastStyle(string message, ToastType type)
        {
            _messageLabel.Text = message;

            switch (type)
            {
                case ToastType.Success:
                    BackColor = Color.FromArgb(76, 175, 80);
                    _iconPanel.BackColor = Color.FromArgb(66, 155, 70);
                    _iconLabel.Text = "✓";
                    _iconLabel.ForeColor = Color.White;
                    break;
                case ToastType.Warning:
                    BackColor = Color.FromArgb(255, 193, 7);
                    _iconPanel.BackColor = Color.FromArgb(245, 183, 0);
                    _iconLabel.Text = "!";
                    _iconLabel.ForeColor = Color.White;
                    break;
                case ToastType.Error:
                    BackColor = Color.FromArgb(244, 67, 54);
                    _iconPanel.BackColor = Color.FromArgb(234, 57, 44);
                    _iconLabel.Text = "✕";
                    _iconLabel.ForeColor = Color.White;
                    break;
                default:
                    BackColor = Color.FromArgb(33, 150, 243);
                    _iconPanel.BackColor = Color.FromArgb(23, 140, 233);
                    _iconLabel.Text = "ℹ";
                    _iconLabel.ForeColor = Color.White;
                    break;
            }
        }

        public void Show(Form owner, int duration = 3000)
        {
            if (owner == null || owner.IsDisposed)
                return;

            PositionToast(owner);
            Show(owner);
            BringToFront();

            Opacity = 0;
            _fadeTimer.Interval = 20;
            _fadeTimer.Start();

            Task.Delay(duration).ContinueWith(_ =>
            {
                if (!IsDisposed)
                {
                    Invoke(new Action(() =>
                    {
                        FadeOut();
                    }));
                }
            });
        }

        private void PositionToast(Form owner)
        {
            int margin = 10;
            Point screenOrigin = owner.PointToScreen(new Point(0, 0));
            int x = screenOrigin.X + owner.ClientSize.Width - Width - margin;
            int y = screenOrigin.Y + owner.ClientSize.Height - Height - margin;

            Location = new Point(x, y);
        }

        private void FadeTimer_Tick(object? sender, EventArgs e)
        {
            if (Opacity < 1)
            {
                Opacity += 0.1;
                if (Opacity >= 1)
                {
                    Opacity = 1;
                    _fadeTimer.Stop();
                }
            }
        }

        private void FadeOut()
        {
            _fadeTimer.Interval = 20;
            _fadeTimer.Start();

            void Fade()
            {
                if (Opacity > 0)
                {
                    Opacity -= 0.1;
                }
                else
                {
                    _fadeTimer.Stop();
                    Close();
                    Dispose();
                }
            }

            _fadeTimer.Tick -= FadeTimer_Tick;
            _fadeTimer.Tick += (s, e) => Fade();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _fadeTimer?.Stop();
                _fadeTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}