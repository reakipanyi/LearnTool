using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Models.Home;
using System.ComponentModel;

namespace LearningAssistant.Forms.UserControls.Dashboard
{
    /// <summary>
    /// 简洁版首页：只保留问候、关键学习数据与任务型快捷入口，去掉推荐区与装饰性卡片。
    /// 通过 AppSettings.HomeStyle = "Simple" 启用；完整版仍由 DashboardView 提供。
    /// </summary>
    public class SimpleHomeView : UserControl, IThemeable
    {
        private const int MarginX = 32;
        private const int CardHeight = 96;
        private const int MinCardWidth = 140;
        private const int ActionWidth = 176;
        private const int ActionHeight = 84;
        private const int ItemSpacing = 12;

        private readonly Label _labelWelcome = new();
        private readonly Label _labelSubtitle = new();
        private readonly Label _labelStatsTitle = new();
        private readonly Label _labelActionsTitle = new();
        private readonly FlowLayoutPanel _panelStats = new();
        private readonly FlowLayoutPanel _panelActions = new();
        private readonly List<StatCard> _statCards = new();
        // 快捷入口按钮：与「今日概览」统计卡片同款白底/主色文字，不再使用彩色实心块
        private readonly List<Button> _actionButtons = new();
        // 新用户引导：整页覆盖，只有产生学习数据后才回到正常首页（07 方案 B2）
        private readonly Panel _panelEmptyState = new();
        private readonly Label _labelEmptyIcon = new();
        private readonly Label _labelEmptyTitle = new();
        private readonly Label _labelEmptyDesc = new();
        private readonly Button _buttonEmptyStart = new();
        private readonly Button _buttonEmptyImport = new();
        private string _userName = "同学";

        public event EventHandler<string>? QuickActionRequested;

        /// <summary>点击统计卡时触发，参数为卡片索引（复用完整版首页的下钻路径）。</summary>
        public event EventHandler<int>? StatCardClicked;

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
            _labelSubtitle.Text = "请选择一个任务开始今天的学习。";

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

            AddStat("⏱️", "今日学习", Color.FromArgb(63, 81, 181));
            AddStat("🔥", "连续学习", Color.FromArgb(255, 87, 34));
            AddStat("⭐", "总经验值", Color.FromArgb(255, 193, 7));
            AddStat("🏆", "当前等级", Color.FromArgb(76, 175, 80));
            AddStat("🎯", "今日挑战", Color.FromArgb(236, 72, 153));
            AddStat("🔁", "待复习", Color.FromArgb(0, 150, 136));
            AddStat("📝", "笔记数", Color.FromArgb(33, 150, 243));

            // 任务型入口：直接通往下一步可执行动作，不再重复左侧导航（07 方案 B3）
            AddAction("learning", "📚", "继续学习");
            AddAction("flashcard", "🔁", "开始复习");
            AddAction("pomodoro", "🍅", "番茄钟");
            AddAction("wrongbook", "❌", "错题本");

            InitializeEmptyStatePanel();
        }

        private void AddStat(string icon, string label, Color accentColor)
        {
            var card = new StatCard
            {
                Icon = icon,
                Value = "—",
                Label = label,
                AccentColor = accentColor,
                CardColor = Color.White,
                CornerRadius = 12,
                IconSize = 24,
                ValueFontSize = 18,
                Width = 180,
                Height = CardHeight,
                Margin = new Padding(0, 0, ItemSpacing, ItemSpacing)
            };

            int index = _statCards.Count;
            card.CardClicked += (_, _) => StatCardClicked?.Invoke(this, index);
            _panelStats.Controls.Add(card);
            _statCards.Add(card);
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

        private void InitializeEmptyStatePanel()
        {
            _panelEmptyState.Dock = DockStyle.Fill;
            _panelEmptyState.BackColor = Color.FromArgb(245, 245, 250);
            _panelEmptyState.Visible = false;

            _labelEmptyIcon.AutoSize = true;
            _labelEmptyIcon.Font = new Font("Segoe UI Emoji", 48F);
            _labelEmptyIcon.Text = "🎯";
            _labelEmptyIcon.BackColor = Color.Transparent;

            _labelEmptyTitle.AutoSize = true;
            _labelEmptyTitle.Font = new Font("微软雅黑", 16F, FontStyle.Bold);
            _labelEmptyTitle.ForeColor = Color.FromArgb(33, 33, 33);
            _labelEmptyTitle.Text = "欢迎来到学习助手！";
            _labelEmptyTitle.BackColor = Color.Transparent;

            _labelEmptyDesc.AutoSize = true;
            _labelEmptyDesc.Font = new Font("微软雅黑", 10F);
            _labelEmptyDesc.ForeColor = Color.FromArgb(117, 117, 117);
            _labelEmptyDesc.Text = "还没有学习记录，先选择一种方式开始吧。";
            _labelEmptyDesc.MaximumSize = new Size(420, 0);
            _labelEmptyDesc.TextAlign = ContentAlignment.MiddleCenter;
            _labelEmptyDesc.BackColor = Color.Transparent;

            _buttonEmptyStart.Text = "📚 开始学习";
            _buttonEmptyStart.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonEmptyStart.FlatStyle = FlatStyle.Flat;
            _buttonEmptyStart.BackColor = Color.FromArgb(99, 102, 241);
            _buttonEmptyStart.ForeColor = Color.White;
            _buttonEmptyStart.Size = new Size(150, 42);
            _buttonEmptyStart.Cursor = Cursors.Hand;
            _buttonEmptyStart.FlatAppearance.BorderSize = 0;
            _buttonEmptyStart.Click += (_, _) => QuickActionRequested?.Invoke(this, "learning");

            _buttonEmptyImport.Text = "📥 导入内容";
            _buttonEmptyImport.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            _buttonEmptyImport.FlatStyle = FlatStyle.Flat;
            _buttonEmptyImport.BackColor = Color.FromArgb(34, 197, 94);
            _buttonEmptyImport.ForeColor = Color.White;
            _buttonEmptyImport.Size = new Size(150, 42);
            _buttonEmptyImport.Cursor = Cursors.Hand;
            _buttonEmptyImport.FlatAppearance.BorderSize = 0;
            _buttonEmptyImport.Click += (_, _) => QuickActionRequested?.Invoke(this, "editor");

            _panelEmptyState.Controls.AddRange(new Control[]
            {
                _labelEmptyIcon, _labelEmptyTitle, _labelEmptyDesc, _buttonEmptyStart, _buttonEmptyImport
            });
            _panelEmptyState.Resize += (_, _) => CenterEmptyState();

            Controls.Add(_panelEmptyState);
            _panelEmptyState.BringToFront();
        }

        private void CenterEmptyState()
        {
            int cx = _panelEmptyState.Width / 2;
            int cy = Math.Max(20, _panelEmptyState.Height / 2 - 90);

            _labelEmptyIcon.Location = new Point(cx - _labelEmptyIcon.Width / 2, cy);
            _labelEmptyTitle.Location = new Point(cx - _labelEmptyTitle.Width / 2, _labelEmptyIcon.Bottom + 8);
            _labelEmptyDesc.Location = new Point(cx - _labelEmptyDesc.Width / 2, _labelEmptyTitle.Bottom + 12);
            _buttonEmptyStart.Location = new Point(cx - _buttonEmptyStart.Width - 8, _labelEmptyDesc.Bottom + 24);
            _buttonEmptyImport.Location = new Point(cx + 8, _labelEmptyDesc.Bottom + 24);
        }

        /// <summary>
        /// 用首页聚合快照刷新统计卡（07 方案 3.4）。
        /// </summary>
        public void ApplySnapshot(HomeSnapshot snapshot)
        {
            if (snapshot == null || _statCards.Count < 7) return;

            var studyCard = _statCards[0];
            studyCard.Value = FormatStudyMinutes(snapshot.TodayStudyMinutes);
            studyCard.Trend = snapshot.TodayStudyMinutes > 0 ? $"+{snapshot.TodayStudyMinutes}分" : "开始学习";
            studyCard.TrendDir = snapshot.TodayStudyMinutes > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

            var streakCard = _statCards[1];
            streakCard.Value = snapshot.StreakDays > 0 ? $"{snapshot.StreakDays}天" : "—";
            streakCard.Trend = snapshot.StreakDays > 0 ? $"已坚持{snapshot.StreakDays}天" : "今天开始";
            streakCard.TrendDir = snapshot.StreakDays > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

            _statCards[2].Value = snapshot.TotalXp.ToString();

            var levelCard = _statCards[3];
            levelCard.Value = $"Lv.{snapshot.Level}";
            levelCard.Trend = $"距下一级 {snapshot.XpToNextLevel}XP";

            var challengeCard = _statCards[4];
            challengeCard.Value = $"{snapshot.CompletedChallenges}/{snapshot.TotalChallenges}";
            challengeCard.Trend = snapshot.CompletedChallenges >= snapshot.TotalChallenges ? "全部完成" : "进行中";
            challengeCard.TrendDir = snapshot.CompletedChallenges >= snapshot.TotalChallenges
                ? StatCard.TrendDirection.Up
                : StatCard.TrendDirection.None;

            var reviewCard = _statCards[5];
            reviewCard.Value = snapshot.DueReviewCount > 0 ? $"{snapshot.DueReviewCount}项" : "无";
            reviewCard.Trend = snapshot.DueReviewCount > 0 ? "立即复习" : "";

            var noteCard = _statCards[6];
            noteCard.Value = snapshot.NoteCount.ToString();
            noteCard.Trend = snapshot.TodayNewNotes > 0 ? $"今日+{snapshot.TodayNewNotes}" : "查看全部";
            noteCard.TrendDir = snapshot.TodayNewNotes > 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;

            // 新用户整页引导：有任一学习数据后自动隐藏（07 方案 B2）
            _panelEmptyState.Visible = snapshot.IsBrandNew;
        }

        public void UpdateChallengeProgress(int completed, int total)
        {
            if (_statCards.Count < 5) return;
            _statCards[4].Value = $"{completed}/{total}";
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

            int columns = GetColumns(contentWidth);
            int cardWidth = Math.Max(MinCardWidth, (contentWidth - columns * ItemSpacing) / columns);
            foreach (var card in _statCards)
            {
                card.Width = cardWidth;
                card.Height = CardHeight;
            }

            int statsHeight = GetRowsHeight(_statCards.Count, columns, CardHeight);
            _panelStats.Bounds = new Rectangle(MarginX, y, contentWidth, statsHeight);
            y = _panelStats.Bottom + 22;

            _labelActionsTitle.Location = new Point(MarginX, y);
            y = _labelActionsTitle.Bottom + 10;

            int actionsHeight = Math.Max(0, Height - y - 16);
            _panelActions.Bounds = new Rectangle(MarginX, y, contentWidth, actionsHeight);
        }

        /// <summary>
        /// 断点：大屏单行展示全部指标，中屏四列两行，窄屏两列多行（07 方案 B4）。
        /// </summary>
        private static int GetColumns(int available)
        {
            if (available >= 1200) return 7;
            if (available >= 900) return 4;
            return 2;
        }

        private static int GetRowsHeight(int count, int columns, int itemHeight)
        {
            int rows = Math.Max(1, (count + columns - 1) / columns);
            return rows * (itemHeight + ItemSpacing) + ItemSpacing;
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;
            _panelStats.BackColor = colors.Background;
            _panelActions.BackColor = colors.Background;
            _panelEmptyState.BackColor = colors.Background;

            _labelWelcome.ForeColor = colors.TextPrimary;
            _labelSubtitle.ForeColor = colors.TextSecondary;
            _labelStatsTitle.ForeColor = colors.TextPrimary;
            _labelActionsTitle.ForeColor = colors.TextPrimary;

            _labelEmptyTitle.ForeColor = colors.TextPrimary;
            _labelEmptyDesc.ForeColor = colors.TextSecondary;

            foreach (var card in _statCards)
            {
                card.CardColor = colors.Surface;
                card.TextColor = colors.TextPrimary;
                card.LabelColor = colors.TextSecondary;
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
    }
}
