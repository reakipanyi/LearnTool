using LearningAssistant.Models.User;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls
{
    public class AchievementsPanel : UserControl
    {
        private Panel _panelHeader = null!;
        private Label _labelTitle = null!;
        private Label _labelStats = null!;
        private FlowLayoutPanel _flowLayoutPanelFilters = null!;
        private Panel _panelContent = null!;
        private FlowLayoutPanel _flowLayoutPanelCards = null!;
        private EmptyStateView? _emptyState;

        private List<Badge> _allBadges = new();
        private Dictionary<string, int> _badgeProgress = new();
        private BadgeCategory? _currentFilter = null;
        private List<Button> _filterButtons = new();

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<Badge> Badges
        {
            get => _allBadges;
            set
            {
                _allBadges = value ?? new List<Badge>();
                UpdateCards();
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Dictionary<string, int> BadgeProgress
        {
            get => _badgeProgress;
            set
            {
                _badgeProgress = value ?? new Dictionary<string, int>();
                UpdateCards();
            }
        }

        public event EventHandler<Badge>? BadgeClicked;

        public AchievementsPanel()
        {
            InitializeComponent();
            CreateFilterButtons();
        }

        private void InitializeComponent()
        {
            _panelHeader = new Panel();
            _labelTitle = new Label();
            _labelStats = new Label();
            _flowLayoutPanelFilters = new FlowLayoutPanel();
            _panelContent = new Panel();
            _flowLayoutPanelCards = new FlowLayoutPanel();

            _panelHeader.SuspendLayout();
            _flowLayoutPanelFilters.SuspendLayout();
            _panelContent.SuspendLayout();
            SuspendLayout();

            _panelHeader.Dock = DockStyle.Top;
            _panelHeader.AutoSize = true;
            _panelHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _panelHeader.Padding = new Padding(15, 10, 15, 10);
            _panelHeader.BackColor = Color.FromArgb(250, 250, 252);

            _labelTitle.Dock = DockStyle.Top;
            _labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold);
            _labelTitle.ForeColor = Color.FromArgb(51, 51, 51);
            _labelTitle.Text = "🏆 成就系统";
            _labelTitle.Height = 30;

            _labelStats.Dock = DockStyle.Top;
            _labelStats.Font = new Font("微软雅黑", 10F);
            _labelStats.ForeColor = Color.FromArgb(102, 102, 102);
            _labelStats.Text = "已解锁 0 / 0";
            _labelStats.Height = 25;

            _flowLayoutPanelFilters.Dock = DockStyle.Top;
            _flowLayoutPanelFilters.AutoSize = true;
            _flowLayoutPanelFilters.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _flowLayoutPanelFilters.Padding = new Padding(10, 0, 10, 0);
            _flowLayoutPanelFilters.WrapContents = true;
            _flowLayoutPanelFilters.BackColor = Color.Transparent;

            _panelHeader.Controls.Add(_flowLayoutPanelFilters);
            _panelHeader.Controls.Add(_labelStats);
            _panelHeader.Controls.Add(_labelTitle);

            _panelContent.Dock = DockStyle.Fill;
            _panelContent.AutoScroll = true;
            _panelContent.Padding = new Padding(15, 10, 15, 10);
            _panelContent.BackColor = Color.White;

            _flowLayoutPanelCards.Dock = DockStyle.Top;
            _flowLayoutPanelCards.AutoSize = true;
            _flowLayoutPanelCards.WrapContents = true;
            _flowLayoutPanelCards.BackColor = Color.Transparent;

            _panelContent.Controls.Add(_flowLayoutPanelCards);

            Controls.Add(_panelContent);
            Controls.Add(_panelHeader);

            Size = new Size(600, 500);
            BackColor = Color.White;
            DoubleBuffered = true;

            _panelHeader.ResumeLayout(false);
            _flowLayoutPanelFilters.ResumeLayout(false);
            _panelContent.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void CreateFilterButtons()
        {
            _flowLayoutPanelFilters.Controls.Clear();
            _filterButtons.Clear();

            AddFilterButton("全部", null);
            AddFilterButton("📚 学习", BadgeCategory.Learning);
            AddFilterButton("🔥 坚持", BadgeCategory.Consistency);
            AddFilterButton("🏆 精通", BadgeCategory.Mastery);
            AddFilterButton("⭐ 特殊", BadgeCategory.Special);

            UpdateFilterButtonStyles();
        }

        private void AddFilterButton(string text, BadgeCategory? category)
        {
            Button btn = new()
            {
                Text = text,
                Tag = category,
                Size = new Size(78, 45),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("微软雅黑", 9F),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Paint += FilterButton_Paint;
            btn.Click += FilterButton_Click;
            _filterButtons.Add(btn);
            _flowLayoutPanelFilters.Controls.Add(btn);
        }

        private void FilterButton_Paint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button btn) return;
            var isActive = (btn.Tag as BadgeCategory?) == _currentFilter;
            var category = btn.Tag as BadgeCategory?;

            if (isActive)
            {
                int lineWidth = 24;
                int lineHeight = 3;
                int x = (btn.Width - lineWidth) / 2;
                int y = btn.Height - lineHeight - 2;

                using var brush = new SolidBrush(Color.FromArgb(255, 152, 0));
                e.Graphics.FillRectangle(brush, x, y, lineWidth, lineHeight);
            }

            int count = 0;
            if (category.HasValue)
            {
                count = _allBadges.Count(b => b.Category == category.Value);
            }
            else
            {
                count = _allBadges.Count;
            }

            if (count > 0)
            {
                string countText = count.ToString();
                var countFont = new Font("微软雅黑", 7.5F, FontStyle.Bold);
                var textSize = e.Graphics.MeasureString(countText, countFont);
                int badgeWidth = Math.Max(16, (int)textSize.Width + 6);
                int badgeX = btn.Width - badgeWidth + 8;
                int badgeY = 2;

                using var badgeBrush = new SolidBrush(isActive ? Color.White : Color.FromArgb(255, 152, 0));
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                var badgeRect = new Rectangle(badgeX, badgeY, badgeWidth, 14);
                using var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(badgeRect.X, badgeRect.Y, badgeRect.Height, badgeRect.Height, 90, 180);
                path.AddArc(badgeRect.Right - badgeRect.Height, badgeRect.Y, badgeRect.Height, badgeRect.Height, 270, 180);
                path.CloseFigure();
                e.Graphics.FillPath(badgeBrush, path);

                using var textBrush = new SolidBrush(isActive ? Color.FromArgb(255, 152, 0) : Color.White);
                var stringFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                e.Graphics.DrawString(countText, countFont, textBrush, badgeRect, stringFormat);
            }
        }

        private void FilterButton_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                BadgeCategory? category = btn.Tag is BadgeCategory c ? c : null;
                _currentFilter = category;
                UpdateFilterButtonStyles();
                UpdateCards();
            }
        }

        private void UpdateFilterButtonStyles()
        {
            foreach (var btn in _filterButtons)
            {
                var isActive = (btn.Tag as BadgeCategory?) == _currentFilter;
                btn.BackColor = isActive ? Color.FromArgb(255, 152, 0) : Color.Transparent;
                btn.ForeColor = isActive ? Color.White : Color.FromArgb(102, 102, 102);
                btn.Font = new Font("微软雅黑", 9F, isActive ? FontStyle.Bold : FontStyle.Regular);
            }
        }

        private void UpdateCards()
        {
            _flowLayoutPanelCards.Controls.Clear();

            var filtered = _currentFilter.HasValue
                ? _allBadges.Where(b => b.Category == _currentFilter.Value).ToList()
                : _allBadges;

            int unlockedCount = filtered.Count(b => b.IsUnlocked);
            int visibleTotal = filtered.Count(b => !b.IsHidden || b.IsUnlocked);
            int hiddenLockedCount = filtered.Count(b => b.IsHidden && !b.IsUnlocked);
            double progressPercent = visibleTotal > 0 ? (double)unlockedCount / visibleTotal * 100 : 0;

            string hiddenText = hiddenLockedCount > 0 ? $" · 隐藏成就 ?" : "";
            _labelStats.Text = $"已解锁 {unlockedCount} / {visibleTotal} · 完成度 {progressPercent:F0}%{hiddenText}";

            foreach (var btn in _filterButtons)
            {
                btn.Invalidate();
            }

            if (filtered.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            HideEmptyState();

            foreach (var badge in filtered)
            {
                var card = new AchievementCard
                {
                    Badge = badge,
                    IsUnlocked = badge.IsUnlocked,
                    CurrentValue = _badgeProgress.TryGetValue(badge.Id, out var val) ? val : 0,
                    Margin = new Padding(5)
                };
                card.CardClicked += (s, e) => BadgeClicked?.Invoke(this, badge);
                _flowLayoutPanelCards.Controls.Add(card);
            }
        }

        private void ShowEmptyState()
        {
            if (_emptyState == null)
            {
                _emptyState = new EmptyStateView
                {
                    Dock = DockStyle.Fill
                };
                _emptyState.SetState(EmptyStateType.NoAchievements);
                _panelContent.Controls.Add(_emptyState);
                _emptyState.BringToFront();
            }
            _emptyState.Visible = true;
            _flowLayoutPanelCards.Visible = false;
        }

        private void HideEmptyState()
        {
            if (_emptyState != null)
            {
                _emptyState.Visible = false;
            }
            _flowLayoutPanelCards.Visible = true;
        }

        public void RefreshData()
        {
            UpdateCards();
        }
    }
}
