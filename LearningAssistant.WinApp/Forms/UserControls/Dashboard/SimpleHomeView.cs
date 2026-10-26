using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Dashboard
{
    /// <summary>
    /// 简洁版首页：只保留问候、关键学习数据和常用入口，去掉推荐区与装饰性卡片。
    /// 通过 AppSettings.HomeStyle = "Simple" 启用；完整版仍由 DashboardView 提供。
    /// </summary>
    public class SimpleHomeView : UserControl, IThemeable
    {
        private const int MarginX = 32;
        private const int ItemWidth = 180;
        private const int ItemHeight = 66;
        private const int ActionWidth = 176;
        private const int ActionHeight = 84;
        private const int ItemSpacing = 12;

        private readonly Label _labelWelcome = new();
        private readonly Label _labelSubtitle = new();
        private readonly Label _labelStatsTitle = new();
        private readonly Label _labelActionsTitle = new();
        private readonly FlowLayoutPanel _panelStats = new();
        private readonly FlowLayoutPanel _panelActions = new();
        private readonly List<StatItem> _statItems = new();
        // 快捷入口按钮：与「今日概览」统计卡片同款白底/主色文字，不再使用彩色实心块
        private readonly List<Button> _actionButtons = new();
        private string _userName = "同学";

        public event EventHandler<string>? QuickActionRequested;

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = string.IsNullOrWhiteSpace(value) ? "同学" : value;
                _labelWelcome.Text = $"你好，{_userName}！";
            }
        }

        public SimpleHomeView()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw,
                true);

            BackColor = Color.FromArgb(245, 245, 250);
            DoubleBuffered = true;
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            _labelWelcome.AutoSize = true;
            _labelWelcome.Font = new Font("微软雅黑", 20F, FontStyle.Bold);
            _labelWelcome.ForeColor = Color.FromArgb(33, 33, 33);
            _labelWelcome.Text = $"你好，{_userName}！";

            _labelSubtitle.AutoSize = true;
            _labelSubtitle.Font = new Font("微软雅黑", 10F);
            _labelSubtitle.ForeColor = Color.FromArgb(117, 117, 117);
            _labelSubtitle.Text = "选择一个入口开始今天的学习。";

            _labelStatsTitle.AutoSize = true;
            _labelStatsTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelStatsTitle.ForeColor = Color.FromArgb(33, 33, 33);
            _labelStatsTitle.Text = "今日概览";

            _labelActionsTitle.AutoSize = true;
            _labelActionsTitle.Font = new Font("微软雅黑", 12F, FontStyle.Bold);
            _labelActionsTitle.ForeColor = Color.FromArgb(33, 33, 33);
            _labelActionsTitle.Text = "快捷入口";

            _panelStats.FlowDirection = FlowDirection.LeftToRight;
            _panelStats.WrapContents = true;
            _panelStats.AutoScroll = false;
            _panelStats.BackColor = Color.Transparent;

            _panelActions.FlowDirection = FlowDirection.LeftToRight;
            _panelActions.WrapContents = true;
            _panelActions.AutoScroll = true;
            _panelActions.BackColor = Color.Transparent;

            Controls.Add(_labelWelcome);
            Controls.Add(_labelSubtitle);
            Controls.Add(_labelStatsTitle);
            Controls.Add(_panelStats);
            Controls.Add(_labelActionsTitle);
            Controls.Add(_panelActions);

            AddStat("⏱️", "今日学习");
            AddStat("🔥", "连续学习");
            AddStat("⭐", "总经验值");
            AddStat("🏆", "当前等级");
            AddStat("🎯", "今日挑战");
            AddStat("📝", "笔记数");

            AddAction("learning", "📚", "开始学习");
            AddAction("pdf", "📖", "PDF阅读");
            AddAction("browser", "🌐", "浏览器");
            AddAction("editor", "✏️", "内容编辑");
            AddAction("data", "📊", "学习数据");
            AddAction("settings", "⚙️", "设置");
        }

        private void AddStat(string icon, string caption)
        {
            var panel = new Panel
            {
                Width = ItemWidth,
                Height = ItemHeight,
                Margin = new Padding(0, 0, ItemSpacing, ItemSpacing),
                BackColor = Color.White
            };

            var iconLabel = new Label
            {
                AutoSize = false,
                Width = 38,
                Height = ItemHeight,
                Text = icon,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI Emoji", 15F),
                BackColor = Color.Transparent
            };

            var valueLabel = new Label
            {
                AutoSize = false,
                Location = new Point(42, 9),
                Size = new Size(ItemWidth - 50, 26),
                Text = "—",
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                BackColor = Color.Transparent
            };

            var captionLabel = new Label
            {
                AutoSize = false,
                Location = new Point(42, 35),
                Size = new Size(ItemWidth - 50, 20),
                Text = caption,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("微软雅黑", 9F),
                ForeColor = Color.FromArgb(117, 117, 117),
                BackColor = Color.Transparent
            };

            panel.Controls.Add(iconLabel);
            panel.Controls.Add(valueLabel);
            panel.Controls.Add(captionLabel);
            _panelStats.Controls.Add(panel);

            _statItems.Add(new StatItem
            {
                Panel = panel,
                Value = valueLabel,
                Caption = captionLabel
            });
        }

        /// <summary>
        /// 快捷入口：与「今日概览」卡片保持同一视觉语言（白底、主文字色、无彩色块），
        /// 只用图标区分入口类型，悬停时给一层浅色反馈。
        /// </summary>
        private void AddAction(string key, string icon, string title)
        {
            var button = new Button
            {
                Width = ActionWidth,
                Height = ActionHeight,
                Margin = new Padding(0, 0, ItemSpacing, ItemSpacing),
                Text = $"{icon}   {title}",
                Tag = key,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(33, 33, 33),
                Font = new Font("微软雅黑", 11F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(248, 249, 252);
            button.FlatAppearance.MouseDownBackColor = Color.FromArgb(240, 241, 246);
            button.Click += (_, _) => QuickActionRequested?.Invoke(this, (string)button.Tag);
            _panelActions.Controls.Add(button);
            _actionButtons.Add(button);
        }

        public void UpdateStats(int todayStudyMinutes, int streakDays, int totalXP,
            int currentLevel, int xpToNextLevel, int completedChallenges, int totalChallenges,
            int noteCount = 0, int todayNewNotes = 0)
        {
            if (_statItems.Count < 6) return;

            _statItems[0].Value.Text = FormatStudyMinutes(todayStudyMinutes);
            _statItems[1].Value.Text = streakDays > 0 ? $"{streakDays}天" : "—";
            _statItems[2].Value.Text = totalXP.ToString();
            _statItems[3].Value.Text = $"Lv.{currentLevel}";
            _statItems[4].Value.Text = $"{completedChallenges}/{totalChallenges}";
            _statItems[5].Value.Text = noteCount.ToString();
            _statItems[5].Caption.Text = todayNewNotes > 0 ? $"笔记数（今日+{todayNewNotes}）" : "笔记数";
        }

        public void UpdateChallengeProgress(int completed, int total)
        {
            if (_statItems.Count < 5) return;
            _statItems[4].Value.Text = $"{completed}/{total}";
        }

        private static string FormatStudyMinutes(int minutes)
        {
            if (minutes <= 0) return "—";
            if (minutes < 60) return $"{minutes}分钟";
            var hours = minutes / 60;
            var rest = minutes % 60;
            return rest == 0 ? $"{hours}小时" : $"{hours}时{rest}分";
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
        }

        private void LayoutControls()
        {
            if (_labelWelcome == null) return;

            int contentWidth = Math.Max(240, Width - MarginX * 2);
            int y = 28;

            _labelWelcome.Location = new Point(MarginX, y);
            y = _labelWelcome.Bottom + 4;

            _labelSubtitle.Location = new Point(MarginX, y);
            y = _labelSubtitle.Bottom + 26;

            _labelStatsTitle.Location = new Point(MarginX, y);
            y = _labelStatsTitle.Bottom + 10;

            _panelStats.Bounds = new Rectangle(MarginX, y, contentWidth, GetRowsHeight(_statItems.Count, ItemWidth, ItemHeight));
            y = _panelStats.Bottom + 22;

            _labelActionsTitle.Location = new Point(MarginX, y);
            y = _labelActionsTitle.Bottom + 10;

            int actionsHeight = Math.Max(0, Height - y - 16);
            _panelActions.Bounds = new Rectangle(MarginX, y, contentWidth, actionsHeight);
        }

        private int GetRowsHeight(int count, int itemWidth, int itemHeight)
        {
            int available = Math.Max(itemWidth, Width - MarginX * 2);
            int columns = Math.Max(1, (available + ItemSpacing) / (itemWidth + ItemSpacing));
            int rows = Math.Max(1, (count + columns - 1) / columns);
            return rows * (itemHeight + ItemSpacing) + ItemSpacing;
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            _panelStats.BackColor = colors.Background;
            _panelActions.BackColor = colors.Background;

            _labelWelcome.ForeColor = colors.TextPrimary;
            _labelSubtitle.ForeColor = colors.TextSecondary;
            _labelStatsTitle.ForeColor = colors.TextPrimary;
            _labelActionsTitle.ForeColor = colors.TextPrimary;

            foreach (var item in _statItems)
            {
                item.Panel.BackColor = colors.Surface;
                item.Value.ForeColor = colors.TextPrimary;
                item.Caption.ForeColor = colors.TextSecondary;
            }

            // 快捷入口与统计卡片共用 Surface 底色，夜间不再是刺眼的白色块
            foreach (var button in _actionButtons)
            {
                button.BackColor = colors.Surface;
                button.ForeColor = colors.TextPrimary;
                button.FlatAppearance.MouseOverBackColor = colors.SurfaceElevated;
                button.FlatAppearance.MouseDownBackColor = colors.SurfaceElevated;
            }
        }

        private sealed class StatItem
        {
            public Panel Panel { get; init; } = new();
            public Label Value { get; init; } = new();
            public Label Caption { get; init; } = new();
        }
    }
}
