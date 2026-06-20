using LearningAssistant.Models.User;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class AchievementCard : UserControl
    {
        private Panel _panelCard = null!;
        private Label _labelIcon = null!;
        private Label _labelName = null!;
        private Label _labelDescription = null!;
        private ProgressBar _progressBar = null!;
        private Label _labelProgress = null!;
        private Label _labelCategory = null!;

        private Badge? _badge;
        private int _currentValue;
        private bool _isUnlocked;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Badge? Badge
        {
            get => _badge;
            set
            {
                _badge = value;
                UpdateDisplay();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public int CurrentValue
        {
            get => _currentValue;
            set
            {
                _currentValue = value;
                UpdateProgress();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsUnlocked
        {
            get => _isUnlocked;
            set
            {
                _isUnlocked = value;
                UpdateStyle();
            }
        }

        public event EventHandler? CardClicked;

        public AchievementCard()
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
            _labelIcon = new Label();
            _labelName = new Label();
            _labelDescription = new Label();
            _progressBar = new ProgressBar();
            _labelProgress = new Label();
            _labelCategory = new Label();

            _panelCard.SuspendLayout();
            SuspendLayout();

            _panelCard.Dock = DockStyle.Fill;
            _panelCard.BackColor = Color.White;
            _panelCard.Padding = new Padding(12);
            _panelCard.Cursor = Cursors.Hand;
            _panelCard.Click += (s, e) => CardClicked?.Invoke(this, e);

            _labelIcon.Dock = DockStyle.Top;
            _labelIcon.Font = new Font("Segoe UI Emoji", 28F);
            _labelIcon.TextAlign = ContentAlignment.MiddleCenter;
            _labelIcon.Height = 50;
            _labelIcon.Click += (s, e) => CardClicked?.Invoke(this, e);

            _labelName.Dock = DockStyle.Top;
            _labelName.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _labelName.ForeColor = Color.FromArgb(51, 51, 51);
            _labelName.TextAlign = ContentAlignment.MiddleCenter;
            _labelName.Height = 25;
            _labelName.Click += (s, e) => CardClicked?.Invoke(this, e);

            _labelCategory.Dock = DockStyle.Top;
            _labelCategory.Font = new Font("微软雅黑", 8F);
            _labelCategory.ForeColor = Color.FromArgb(153, 153, 153);
            _labelCategory.TextAlign = ContentAlignment.MiddleCenter;
            _labelCategory.Height = 18;
            _labelCategory.Click += (s, e) => CardClicked?.Invoke(this, e);

            _labelDescription.Dock = DockStyle.Top;
            _labelDescription.Font = new Font("微软雅黑", 8.5F);
            _labelDescription.ForeColor = Color.FromArgb(102, 102, 102);
            _labelDescription.TextAlign = ContentAlignment.MiddleCenter;
            _labelDescription.Height = 30;
            _labelDescription.Click += (s, e) => CardClicked?.Invoke(this, e);

            _progressBar.Dock = DockStyle.Top;
            _progressBar.Height = 8;
            _progressBar.Style = ProgressBarStyle.Continuous;
            _progressBar.MarqueeAnimationSpeed = 0;
            _progressBar.Click += (s, e) => CardClicked?.Invoke(this, e);

            _labelProgress.Dock = DockStyle.Top;
            _labelProgress.Font = new Font("微软雅黑", 8F);
            _labelProgress.ForeColor = Color.FromArgb(102, 102, 102);
            _labelProgress.TextAlign = ContentAlignment.MiddleCenter;
            _labelProgress.Height = 18;
            _labelProgress.Click += (s, e) => CardClicked?.Invoke(this, e);

            _panelCard.Controls.Add(_labelProgress);
            _panelCard.Controls.Add(_progressBar);
            _panelCard.Controls.Add(_labelDescription);
            _panelCard.Controls.Add(_labelCategory);
            _panelCard.Controls.Add(_labelName);
            _panelCard.Controls.Add(_labelIcon);

            Controls.Add(_panelCard);

            Size = new Size(160, 180);
            BackColor = Color.Transparent;
            DoubleBuffered = true;

            _panelCard.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void SetupHoverEffect()
        {
            _panelCard.MouseEnter += (s, e) =>
            {
                if (!_isUnlocked) return;
                _panelCard.BackColor = Color.FromArgb(245, 245, 250);
                _panelCard.Location = new Point(_panelCard.Left, _panelCard.Top - 2);
            };

            _panelCard.MouseLeave += (s, e) =>
            {
                if (!_isUnlocked) return;
                _panelCard.BackColor = Color.White;
                _panelCard.Location = new Point(_panelCard.Left, _panelCard.Top + 2);
            };
        }

        private void UpdateDisplay()
        {
            if (_badge == null)
            {
                _labelIcon.Text = "🏅";
                _labelName.Text = "未知成就";
                _labelDescription.Text = "";
                _labelCategory.Text = "";
                return;
            }

            _labelIcon.Text = _badge.Icon;
            _labelName.Text = _badge.Name;
            _labelDescription.Text = _badge.Description;
            _labelCategory.Text = GetCategoryText(_badge.Category);
            _labelCategory.ForeColor = GetCategoryColor(_badge.Category);

            _progressBar.Maximum = _badge.Requirement.TargetValue;
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_badge == null) return;

            int target = _badge.Requirement.TargetValue;
            int current = Math.Min(_currentValue, target);

            _progressBar.Value = current;
            _labelProgress.Text = _isUnlocked
                ? "✅ 已解锁"
                : $"{current} / {target}";
        }

        private void UpdateStyle()
        {
            if (_isUnlocked)
            {
                _panelCard.BackColor = Color.White;
                _panelCard.BorderStyle = BorderStyle.None;
                _labelIcon.ForeColor = Color.Black;
                _labelName.ForeColor = Color.FromArgb(51, 51, 51);
                _progressBar.ForeColor = Color.FromArgb(76, 175, 80);
                _labelProgress.ForeColor = Color.FromArgb(76, 175, 80);
            }
            else
            {
                _panelCard.BackColor = Color.FromArgb(245, 245, 245);
                _panelCard.BorderStyle = BorderStyle.None;
                _labelIcon.ForeColor = Color.Gray;
                _labelName.ForeColor = Color.FromArgb(153, 153, 153);
                _progressBar.ForeColor = Color.FromArgb(200, 200, 200);
                _labelProgress.ForeColor = Color.FromArgb(153, 153, 153);
            }

            UpdateProgress();
        }

        private string GetCategoryText(BadgeCategory category)
        {
            return category switch
            {
                BadgeCategory.Learning => "📚 学习",
                BadgeCategory.Consistency => "🔥 坚持",
                BadgeCategory.Mastery => "🏆 精通",
                BadgeCategory.Special => "⭐ 特殊",
                _ => ""
            };
        }

        private Color GetCategoryColor(BadgeCategory category)
        {
            return category switch
            {
                BadgeCategory.Learning => Color.FromArgb(33, 150, 243),
                BadgeCategory.Consistency => Color.FromArgb(255, 152, 0),
                BadgeCategory.Mastery => Color.FromArgb(156, 39, 176),
                BadgeCategory.Special => Color.FromArgb(233, 30, 99),
                _ => Color.Gray
            };
        }

        private Color GetRarityColor(BadgeRarity rarity)
        {
            return rarity switch
            {
                BadgeRarity.Common => Color.FromArgb(158, 158, 158),
                BadgeRarity.Uncommon => Color.FromArgb(76, 175, 80),
                BadgeRarity.Rare => Color.FromArgb(33, 150, 243),
                BadgeRarity.Epic => Color.FromArgb(156, 39, 176),
                BadgeRarity.Legendary => Color.FromArgb(255, 152, 0),
                _ => Color.Gray
            };
        }

        private string GetRarityText(BadgeRarity rarity)
        {
            return rarity switch
            {
                BadgeRarity.Common => "普通",
                BadgeRarity.Uncommon => "优秀",
                BadgeRarity.Rare => "稀有",
                BadgeRarity.Epic => "史诗",
                BadgeRarity.Legendary => "传说",
                _ => ""
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_panelCard == null) return;

            using var path = new System.Drawing.Drawing2D.GraphicsPath();
            int radius = 8;
            Rectangle rect = new(0, 0, Width - 1, Height - 1);
            path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
            path.AddArc(rect.Right - radius, rect.Y, radius, radius, 270, 90);
            path.AddArc(rect.Right - radius, rect.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius, radius, radius, 90, 90);
            path.CloseAllFigures();

            _panelCard.Region = new Region(path);

            Color borderColor;
            if (_isUnlocked && _badge != null)
            {
                borderColor = GetRarityColor(_badge.Rarity);
            }
            else if (_isUnlocked)
            {
                borderColor = Color.FromArgb(220, 220, 230);
            }
            else
            {
                borderColor = Color.FromArgb(230, 230, 230);
            }

            using Pen borderPen = new(borderColor, _isUnlocked && _badge?.Rarity == BadgeRarity.Legendary ? 2 : 1);
            e.Graphics.DrawPath(borderPen, path);
        }
    }
}
