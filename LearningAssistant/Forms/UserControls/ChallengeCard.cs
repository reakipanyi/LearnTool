using LearningAssistant.Models.User;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class ChallengeCard : UserControl
    {
        private Panel _panelCard = null!;
        private Panel _panelHeader = null!;
        private Label _labelIcon = null!;
        private Label _labelName = null!;
        private Label _labelReward = null!;
        private Label _labelDescription = null!;
        private ProgressBar _progressBar = null!;
        private Panel _panelFooter = null!;
        private Label _labelProgress = null!;
        private Button _buttonClaim = null!;
        private Label _labelStatus = null!;

        private Challenge? _challenge;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Challenge? Challenge
        {
            get => _challenge;
            set
            {
                _challenge = value;
                UpdateDisplay();
            }
        }

        public event EventHandler<Challenge>? ClaimClicked;

        public ChallengeCard()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);
            InitializeComponent();
            SetupHoverEffect();
        }

        private void InitializeComponent()
        {
            _panelCard = new Panel();
            _panelHeader = new Panel();
            _labelIcon = new Label();
            _labelName = new Label();
            _labelReward = new Label();
            _labelDescription = new Label();
            _progressBar = new ProgressBar();
            _panelFooter = new Panel();
            _labelProgress = new Label();
            _buttonClaim = new Button();
            _labelStatus = new Label();

            _panelCard.SuspendLayout();
            _panelHeader.SuspendLayout();
            _panelFooter.SuspendLayout();
            SuspendLayout();

            _panelCard.Dock = DockStyle.Fill;
            _panelCard.BackColor = Color.White;
            _panelCard.Padding = new Padding(12);
            _panelCard.Cursor = Cursors.Hand;

            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.Height = 40;
            _panelHeader.BackColor = Color.Transparent;

            _labelIcon.Dock = DockStyle.Left;
            _labelIcon.Font = new Font("Segoe UI Emoji", 22F);
            _labelIcon.Size = new Size(40, 40);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;

            _labelName.Dock = DockStyle.Fill;
            _labelName.Font = new Font("微软雅黑", 10.5F, FontStyle.Bold);
            _labelName.ForeColor = Color.FromArgb(51, 51, 51);
            _labelName.TextAlign = ContentAlignment.MiddleLeft;

            _labelReward.Dock = DockStyle.Right;
            _labelReward.Font = new Font("微软雅黑", 8.5F);
            _labelReward.ForeColor = Color.FromArgb(255, 152, 0);
            _labelReward.Size = new Size(80, 40);
            _labelReward.TextAlign = ContentAlignment.MiddleRight;

            _panelHeader.Controls.Add(_labelName);
            _panelHeader.Controls.Add(_labelIcon);
            _panelHeader.Controls.Add(_labelReward);

            _labelDescription.Dock = DockStyle.Top;
            _labelDescription.Font = new Font("微软雅黑", 8.5F);
            _labelDescription.ForeColor = Color.FromArgb(102, 102, 102);
            _labelDescription.Height = 20;

            _progressBar.Dock = DockStyle.Top;
            _progressBar.Height = 10;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.ForeColor = Color.FromArgb(76, 175, 80);
            _progressBar.BackColor = Color.FromArgb(240, 240, 240);

            _panelFooter.Dock = DockStyle.Top;
            _panelFooter.Height = 30;
            _panelFooter.BackColor = Color.Transparent;

            _labelProgress.Dock = DockStyle.Left;
            _labelProgress.Font = new Font("微软雅黑", 8.5F);
            _labelProgress.ForeColor = Color.FromArgb(102, 102, 102);
            _labelProgress.Size = new Size(100, 30);
            _labelProgress.TextAlign = ContentAlignment.MiddleLeft;

            _labelStatus.Dock = DockStyle.Fill;
            _labelStatus.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _labelStatus.ForeColor = Color.FromArgb(76, 175, 80);
            _labelStatus.TextAlign = ContentAlignment.MiddleRight;
            _labelStatus.Visible = false;

            _buttonClaim.Dock = DockStyle.Right;
            _buttonClaim.Text = "领取奖励";
            _buttonClaim.Size = new Size(80, 28);
            _buttonClaim.FlatStyle = FlatStyle.Flat;
            _buttonClaim.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            _buttonClaim.ForeColor = Color.White;
            _buttonClaim.BackColor = Color.FromArgb(255, 152, 0);
            _buttonClaim.Cursor = Cursors.Hand;
            _buttonClaim.Visible = false;
            _buttonClaim.Click += ButtonClaim_Click;
            _buttonClaim.FlatAppearance.BorderSize = 0;

            _panelFooter.Controls.Add(_labelStatus);
            _panelFooter.Controls.Add(_buttonClaim);
            _panelFooter.Controls.Add(_labelProgress);

            _panelCard.Controls.Add(_panelFooter);
            _panelCard.Controls.Add(_progressBar);
            _panelCard.Controls.Add(_labelDescription);
            _panelCard.Controls.Add(_panelHeader);

            Controls.Add(_panelCard);

            Size = new Size(220, 130);
            BackColor = Color.Transparent;
            DoubleBuffered = true;

            _panelCard.ResumeLayout(false);
            _panelHeader.ResumeLayout(false);
            _panelFooter.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void SetupHoverEffect()
        {
            _panelCard.MouseEnter += (s, e) =>
            {
                if (_challenge?.Completed == true && !_challenge.Claimed)
                    _panelCard.BackColor = Color.FromArgb(255, 250, 240);
                else
                    _panelCard.BackColor = Color.FromArgb(248, 248, 252);
            };

            _panelCard.MouseLeave += (s, e) =>
            {
                _panelCard.BackColor = Color.White;
            };
        }

        private void ButtonClaim_Click(object? sender, EventArgs e)
        {
            if (_challenge != null && _challenge.Completed && !_challenge.Claimed)
            {
                ClaimClicked?.Invoke(this, _challenge);
            }
        }

        private void UpdateDisplay()
        {
            if (_challenge == null)
            {
                _labelIcon.Text = "🎯";
                _labelName.Text = "挑战";
                _labelDescription.Text = "";
                _labelReward.Text = "";
                return;
            }

            _labelIcon.Text = _challenge.Emoji;
            _labelName.Text = _challenge.Name;
            _labelDescription.Text = _challenge.Description;
            _labelReward.Text = $"🎁 {_challenge.Reward} XP";

            int current = Math.Min(_challenge.Current, _challenge.Target);
            _progressBar.Maximum = _challenge.Target;
            _progressBar.Value = current;
            _labelProgress.Text = $"{current} / {_challenge.Target}";

            if (_challenge.Completed)
            {
                if (_challenge.Claimed)
                {
                    _buttonClaim.Visible = false;
                    _labelStatus.Visible = true;
                    _labelStatus.Text = "✅ 已领取";
                    _labelStatus.ForeColor = Color.FromArgb(76, 175, 80);
                    _progressBar.ForeColor = Color.FromArgb(76, 175, 80);
                    _panelCard.BackColor = Color.FromArgb(245, 250, 245);
                }
                else
                {
                    _buttonClaim.Visible = true;
                    _labelStatus.Visible = false;
                    _progressBar.ForeColor = Color.FromArgb(255, 152, 0);
                    _panelCard.BackColor = Color.White;
                }
            }
            else
            {
                _buttonClaim.Visible = false;
                _labelStatus.Visible = false;
                _progressBar.ForeColor = Color.FromArgb(33, 150, 243);
                _panelCard.BackColor = Color.White;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 10;
            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            _panelCard.Region = new Region(path);

            using Pen borderPen = new(Color.FromArgb(225, 225, 235));
            e.Graphics.DrawPath(borderPen, path);
        }

        public void RefreshStatus()
        {
            UpdateDisplay();
        }
    }
}
