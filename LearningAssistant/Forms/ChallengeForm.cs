using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Managers;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Forms
{
    public class ChallengeForm : Form, IThemeable
    {
        private readonly IGamificationService _gamificationService;
        private readonly ILearningAnalyticsService? _analyticsService;
        private readonly INoteService? _noteService;
        private readonly IWrongAnswerService? _wrongAnswerService;
        private readonly ILogger<ChallengeForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly IUserSessionService? _userSessionService;
        private readonly string _userId;

        private TabControl _tabControl;
        private TabPage _tabToday;
        private TabPage _tabHistory;
        private TabPage _tabAchievements;
        private ChallengesPanel _challengesPanel;
        private Panel _historyPanel;
        private Panel _statsPanel;
        private Panel _achievementsPanel;
        private System.Windows.Forms.Timer? _countdownTimer;

        private ChallengeHistoryStats _historyStats = new();

        public ChallengeForm(
            IGamificationService gamificationService,
            ILearningAnalyticsService? analyticsService = null,
            INoteService? noteService = null,
            IWrongAnswerService? wrongAnswerService = null,
            ILogger<ChallengeForm>? logger = null,
            IThemeService? themeService = null,
            IUserSessionService? userSessionService = null,
            string? userId = null)
        {
            _gamificationService = gamificationService;
            _analyticsService = analyticsService;
            _noteService = noteService;
            _wrongAnswerService = wrongAnswerService;
            _logger = logger;
            _themeService = themeService;
            _userSessionService = userSessionService;
            _userId = userId ?? userSessionService?.CurrentUserId ?? Environment.UserName;

            InitializeComponent();
            _themeService?.RegisterThemeable(this);

            LoadData();
            StartCountdown();
        }


        #region 设计器识别控件字段（必须写在类内）
        private IContainer components = null;

        // Tab容器

        // 今日挑战页头部
        private Panel panelHeader;
        private Label labelTitle;
        private Label labelCountdown;
        private ProgressRingControl progressRing;

        // 历史记录页 
        private Label labelHistoryTitle;
        private Label labelTotalDays;
        private Label labelPerfectDays;
        private Label labelTotalXP;
        private Label labelStreak;
        #endregion


        private void InitializeComponent()
        {
            _tabControl = new TabControl();
            _tabToday = new TabPage();
            panelHeader = new Panel();
            labelTitle = new Label();
            labelCountdown = new Label();
            progressRing = new ProgressRingControl();
            _tabHistory = new TabPage();
            _historyPanel = new Panel();
            _statsPanel = new Panel();
            labelHistoryTitle = new Label();
            labelTotalDays = new Label();
            labelPerfectDays = new Label();
            labelTotalXP = new Label();
            labelStreak = new Label();
            _tabAchievements = new TabPage();
            _achievementsPanel = new Panel();
            _challengesPanel = new ChallengesPanel();
            _tabControl.SuspendLayout();
            _tabToday.SuspendLayout();
            panelHeader.SuspendLayout();
            _tabHistory.SuspendLayout();
            _statsPanel.SuspendLayout();
            _tabAchievements.SuspendLayout();
            SuspendLayout();
            // 
            // _tabControl
            // 
            _tabControl.Controls.Add(_tabToday);
            _tabControl.Controls.Add(_tabHistory);
            _tabControl.Controls.Add(_tabAchievements);
            _tabControl.Dock = DockStyle.Fill;
            _tabControl.Font = new Font("微软雅黑", 10F, FontStyle.Regular, GraphicsUnit.Point, 134);
            _tabControl.Location = new Point(0, 0);
            _tabControl.Name = "_tabControl";
            _tabControl.SelectedIndex = 0;
            _tabControl.Size = new Size(534, 461);
            _tabControl.TabIndex = 0;
            // 
            // _tabToday
            // 
            _tabToday.BackColor = Color.White;
            _tabToday.Controls.Add(panelHeader);
            _tabToday.Location = new Point(4, 28);
            _tabToday.Name = "_tabToday";
            _tabToday.Size = new Size(526, 429);
            _tabToday.TabIndex = 0;
            _tabToday.Text = "📅 今日挑战";
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.FromArgb(250, 250, 252);
            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(labelCountdown);
            panelHeader.Controls.Add(progressRing);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(526, 80);
            panelHeader.TabIndex = 0;
            // 
            // labelTitle
            // 
            labelTitle.AutoSize = true;
            labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold, GraphicsUnit.Point, 134);
            labelTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelTitle.Location = new Point(15, 15);
            labelTitle.Name = "labelTitle";
            labelTitle.Size = new Size(121, 26);
            labelTitle.TabIndex = 0;
            labelTitle.Text = "🎯 每日挑战";
            // 
            // labelCountdown
            // 
            labelCountdown.AutoSize = true;
            labelCountdown.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelCountdown.ForeColor = Color.FromArgb(150, 150, 150);
            labelCountdown.Location = new Point(15, 45);
            labelCountdown.Name = "labelCountdown";
            labelCountdown.Size = new Size(131, 17);
            labelCountdown.TabIndex = 1;
            labelCountdown.Text = "⏰ 刷新倒计时: --:--:--";
            // 
            // progressRing
            // 
            progressRing.BackColor = Color.Transparent;
            progressRing.Location = new Point(465, 12);
            progressRing.Name = "progressRing";
            progressRing.Size = new Size(56, 56);
            progressRing.TabIndex = 2;
            // 
            // _tabHistory
            // 
            _tabHistory.BackColor = Color.White;
            _tabHistory.Controls.Add(_historyPanel);
            _tabHistory.Controls.Add(_statsPanel);
            _tabHistory.Location = new Point(4, 28);
            _tabHistory.Name = "_tabHistory";
            _tabHistory.Size = new Size(192, 68);
            _tabHistory.TabIndex = 1;
            _tabHistory.Text = "📜 历史记录";
            // 
            // _tabAchievements
            // 
            _tabAchievements.BackColor = Color.White;
            _tabAchievements.Controls.Add(_achievementsPanel);
            _tabAchievements.Location = new Point(4, 28);
            _tabAchievements.Name = "_tabAchievements";
            _tabAchievements.Size = new Size(526, 429);
            _tabAchievements.TabIndex = 2;
            _tabAchievements.Text = "🏆 成就徽章";
            // 
            // _achievementsPanel
            // 
            _achievementsPanel.BackColor = Color.White;
            _achievementsPanel.Dock = DockStyle.Fill;
            _achievementsPanel.Location = new Point(0, 0);
            _achievementsPanel.Name = "_achievementsPanel";
            _achievementsPanel.Size = new Size(526, 429);
            _achievementsPanel.TabIndex = 0;
            // 
            // _historyPanel
            // 
            _historyPanel.BackColor = Color.White;
            _historyPanel.Dock = DockStyle.Fill;
            _historyPanel.Location = new Point(0, 100);
            _historyPanel.Name = "_historyPanel";
            _historyPanel.Size = new Size(192, 0);
            _historyPanel.TabIndex = 0;
            _historyPanel.Paint += HistoryPanel_Paint;
            // 
            // _statsPanel
            // 
            _statsPanel.BackColor = Color.FromArgb(250, 250, 252);
            _statsPanel.Controls.Add(labelHistoryTitle);
            _statsPanel.Controls.Add(labelTotalDays);
            _statsPanel.Controls.Add(labelPerfectDays);
            _statsPanel.Controls.Add(labelTotalXP);
            _statsPanel.Controls.Add(labelStreak);
            _statsPanel.Dock = DockStyle.Top;
            _statsPanel.Location = new Point(0, 0);
            _statsPanel.Name = "_statsPanel";
            _statsPanel.Size = new Size(192, 100);
            _statsPanel.TabIndex = 1;
            // 
            // labelHistoryTitle
            // 
            labelHistoryTitle.AutoSize = true;
            labelHistoryTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold, GraphicsUnit.Point, 134);
            labelHistoryTitle.ForeColor = Color.FromArgb(33, 33, 33);
            labelHistoryTitle.Location = new Point(15, 15);
            labelHistoryTitle.Name = "labelHistoryTitle";
            labelHistoryTitle.Size = new Size(159, 26);
            labelHistoryTitle.TabIndex = 0;
            labelHistoryTitle.Text = "📜 挑战历史统计";
            // 
            // labelTotalDays
            // 
            labelTotalDays.AutoSize = true;
            labelTotalDays.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelTotalDays.ForeColor = Color.FromArgb(100, 100, 100);
            labelTotalDays.Location = new Point(15, 50);
            labelTotalDays.Name = "labelTotalDays";
            labelTotalDays.Size = new Size(102, 17);
            labelTotalDays.TabIndex = 1;
            labelTotalDays.Text = "📊 总挑战天数: 0";
            // 
            // labelPerfectDays
            // 
            labelPerfectDays.AutoSize = true;
            labelPerfectDays.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelPerfectDays.ForeColor = Color.FromArgb(255, 152, 0);
            labelPerfectDays.Location = new Point(150, 50);
            labelPerfectDays.Name = "labelPerfectDays";
            labelPerfectDays.Size = new Size(90, 17);
            labelPerfectDays.TabIndex = 2;
            labelPerfectDays.Text = "🏆 完美天数: 0";
            // 
            // labelTotalXP
            // 
            labelTotalXP.AutoSize = true;
            labelTotalXP.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelTotalXP.ForeColor = Color.FromArgb(76, 175, 80);
            labelTotalXP.Location = new Point(280, 50);
            labelTotalXP.Name = "labelTotalXP";
            labelTotalXP.Size = new Size(75, 17);
            labelTotalXP.TabIndex = 3;
            labelTotalXP.Text = "⭐ 累计XP: 0";
            // 
            // labelStreak
            // 
            labelStreak.AutoSize = true;
            labelStreak.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            labelStreak.ForeColor = Color.FromArgb(244, 67, 54);
            labelStreak.Location = new Point(400, 50);
            labelStreak.Name = "labelStreak";
            labelStreak.Size = new Size(99, 17);
            labelStreak.TabIndex = 4;
            labelStreak.Text = "🔥 近期连击: 0天";
            // 
            // ChallengeForm
            // 
            BackColor = Color.FromArgb(245, 245, 250);
            ClientSize = new Size(534, 461);
            Controls.Add(_tabControl);
            Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            MinimumSize = new Size(450, 400);
            Name = "ChallengeForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "🎯 每日挑战";
            _tabControl.ResumeLayout(false);
            _tabToday.ResumeLayout(false);
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            _tabHistory.ResumeLayout(false);
            _statsPanel.ResumeLayout(false);
            _statsPanel.PerformLayout();
            _tabAchievements.ResumeLayout(false);
            ResumeLayout(false);
        }
        private void LoadData()
        {
            _challengesPanel.RefreshData();
            LoadHistoryStats();
            LoadAchievements();
        }

        private void LoadHistoryStats()
        {
            try
            {
                // 从 GamificationService 获取历史统计
                // 这里需要扩展 IGamificationService 接口，暂时使用默认值
                _historyStats = new ChallengeHistoryStats
                {
                    TotalDays = 0,
                    PerfectDays = 0,
                    TotalXPClaimed = _gamificationService.XP,
                    AverageCompletionRate = 0,
                    RecentStreak = _gamificationService.StreakDays
                };

                UpdateHistoryStatsDisplay();
                _historyPanel?.Invalidate();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载挑战历史统计失败");
            }
        }

        private void UpdateHistoryStatsDisplay()
        {
            if (_statsPanel == null) return;

            foreach (Control control in _statsPanel.Controls)
            {
                switch (control.Name)
                {
                    case "labelTotalDays":
                        control.Text = $"📊 总挑战天数: {_historyStats.TotalDays}";
                        break;
                    case "labelPerfectDays":
                        control.Text = $"🏆 完美天数: {_historyStats.PerfectDays}";
                        break;
                    case "labelTotalXP":
                        control.Text = $"⭐ 累计XP: {_historyStats.TotalXPClaimed}";
                        break;
                    case "labelStreak":
                        control.Text = $"🔥 近期连击: {_historyStats.RecentStreak}天";
                        break;
                }
            }
        }

        private void LoadAchievements()
        {
            if (_achievementsPanel == null) return;

            try
            {
                _achievementsPanel.Controls.Clear();

                var badges = _gamificationService.GetAllBadges().ToList();
                var unlockedCount = badges.Count(b => b.IsUnlocked);
                var totalCount = badges.Count;

                var headerPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 60,
                    BackColor = Color.FromArgb(250, 250, 252)
                };

                var titleLabel = new Label
                {
                    Text = $"🏆 成就徽章",
                    Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(33, 33, 33),
                    Location = new Point(15, 10)
                };

                var countLabel = new Label
                {
                    Text = $"已解锁: {unlockedCount} / {totalCount}",
                    Font = new Font("微软雅黑", 10F),
                    ForeColor = Color.FromArgb(100, 100, 100),
                    Location = new Point(15, 35)
                };

                headerPanel.Controls.Add(titleLabel);
                headerPanel.Controls.Add(countLabel);
                _achievementsPanel.Controls.Add(headerPanel);

                var flowPanel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    Padding = new Padding(15),
                    WrapContents = true
                };

                foreach (var badge in badges)
                {
                    var badgeCard = CreateBadgeCard(badge);
                    flowPanel.Controls.Add(badgeCard);
                }

                _achievementsPanel.Controls.Add(flowPanel);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载成就徽章失败");
            }
        }

        private Panel CreateBadgeCard(Models.User.Badge badge)
        {
            var panel = new Panel
            {
                Size = new Size(120, 140),
                Margin = new Padding(8),
                BackColor = badge.IsUnlocked ? Color.FromArgb(255, 248, 225) : Color.FromArgb(245, 245, 248),
                Cursor = badge.IsUnlocked ? Cursors.Hand : Cursors.Default
            };

            panel.Paint += (s, e) =>
            {
                if (e == null) return;
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                var rect = new Rectangle(0, 0, panel.Width - 1, panel.Height - 1);
                using var borderPen = new Pen(badge.IsUnlocked ? Color.FromArgb(255, 152, 0) : Color.FromArgb(220, 220, 225), 1);
                using var path = RoundedRect(rect, 8);
                g.DrawPath(borderPen, path);
            };

            var iconLabel = new Label
            {
                Text = badge.IsUnlocked ? badge.Icon : "🔒",
                Font = new Font("Segoe UI Emoji", 28F),
                Location = new Point(35, 15),
                Size = new Size(50, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = badge.IsUnlocked ? Color.FromArgb(255, 152, 0) : Color.FromArgb(180, 180, 180)
            };

            var nameLabel = new Label
            {
                Text = badge.Name,
                Font = new Font("微软雅黑", 9F, FontStyle.Bold),
                Location = new Point(10, 60),
                Size = new Size(100, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = badge.IsUnlocked ? Color.FromArgb(33, 33, 33) : Color.FromArgb(150, 150, 150)
            };

            var descLabel = new Label
            {
                Text = badge.Description,
                Font = new Font("微软雅黑", 7F),
                Location = new Point(5, 85),
                Size = new Size(110, 30),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = badge.IsUnlocked ? Color.FromArgb(100, 100, 100) : Color.FromArgb(180, 180, 180)
            };

            var statusLabel = new Label
            {
                Text = badge.IsUnlocked ? "✓ 已解锁" : "未解锁",
                Font = new Font("微软雅黑", 7F),
                Location = new Point(5, 115),
                Size = new Size(110, 15),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = badge.IsUnlocked ? Color.FromArgb(76, 175, 80) : Color.FromArgb(180, 180, 180)
            };

            panel.Controls.Add(iconLabel);
            panel.Controls.Add(nameLabel);
            panel.Controls.Add(descLabel);
            panel.Controls.Add(statusLabel);

            if (badge.IsUnlocked)
            {
                panel.MouseEnter += (s, e) =>
                {
                    panel.BackColor = Color.FromArgb(255, 240, 200);
                };
                panel.MouseLeave += (s, e) =>
                {
                    panel.BackColor = Color.FromArgb(255, 248, 225);
                };
            }

            return panel;
        }

        private void HistoryPanel_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _historyPanel == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _historyPanel.ClientRectangle;
            int paddingLeft = 20;
            int paddingTop = 20;

            // 绘制日历热力图风格的挑战历史
            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            g.DrawString("近30天挑战完成情况（日历热力图）", titleFont, titleBrush, paddingLeft, paddingTop);

            int cellSize = 28;
            int cols = 7;
            int rows = 5;
            int startX = paddingLeft;
            int startY = paddingTop + 30;

            // 绘制星期标题
            string[] weekdays = { "一", "二", "三", "四", "五", "六", "日" };
            using var weekdayFont = new Font("微软雅黑", 8F);
            using var weekdayBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
            for (int col = 0; col < cols; col++)
            {
                g.DrawString(weekdays[col], weekdayFont, weekdayBrush,
                    startX + col * cellSize + cellSize / 2 - 5, startY - 15);
            }

            // 绘制30天热力图
            var startDate = DateTime.Today.AddDays(-29);
            for (int day = 0; day < 30; day++)
            {
                int row = day / cols;
                int col = day % cols;
                int x = startX + col * cellSize;
                int y = startY + row * cellSize;

                var date = startDate.AddDays(day);
                bool isToday = date == DateTime.Today;

                // 模拟数据：根据日期生成不同的完成率颜色
                int completionRate = day % 7 == 0 ? 100 : (day % 3 == 0 ? 80 : (day % 5 == 0 ? 50 : 20));

                Color cellColor = GetHeatmapColor(completionRate);
                using var cellBrush = new SolidBrush(cellColor);
                var cellRect = new Rectangle(x, y, cellSize - 2, cellSize - 2);
                using var cellPath = RoundedRect(cellRect, 4);
                g.FillPath(cellBrush, cellPath);

                // 绘制日期数字
                using var dayFont = new Font("微软雅黑", 7F);
                using var dayBrush = new SolidBrush(isToday ? Color.White : Color.FromArgb(80, 80, 80));
                g.DrawString(date.Day.ToString(), dayFont, dayBrush,
                    x + cellSize / 2 - 6, y + cellSize / 2 - 8);

                if (isToday)
                {
                    using var borderPen = new Pen(Color.FromArgb(63, 81, 181), 2);
                    g.DrawRectangle(borderPen, x, y, cellSize - 2, cellSize - 2);
                }
            }

            // 绘制颜色图例
            int legendY = startY + rows * cellSize + 20;
            using var legendFont = new Font("微软雅黑", 8F);
            using var legendBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
            g.DrawString("完成率:", legendFont, legendBrush, startX, legendY);

            string[] legendLabels = { "0%", "25%", "50%", "75%", "100%" };
            int[] legendRates = { 0, 25, 50, 75, 100 };
            int legendX = startX + 60;
            for (int i = 0; i < 5; i++)
            {
                using var legendCellBrush = new SolidBrush(GetHeatmapColor(legendRates[i]));
                var legendRect = new Rectangle(legendX + i * 50, legendY, 18, 18);
                using var legendPath = RoundedRect(legendRect, 3);
                g.FillPath(legendCellBrush, legendPath);
                g.DrawString(legendLabels[i], legendFont, legendBrush, legendX + i * 50 + 22, legendY + 2);
            }
        }

        private Color GetHeatmapColor(int completionRate)
        {
            // 0% = 浅灰, 25% = 浅绿, 50% = 中绿, 75% = 深绿, 100% = 最深绿
            if (completionRate == 0)
                return Color.FromArgb(235, 235, 235);
            else if (completionRate <= 25)
                return Color.FromArgb(200, 230, 200);
            else if (completionRate <= 50)
                return Color.FromArgb(150, 200, 150);
            else if (completionRate <= 75)
                return Color.FromArgb(100, 180, 100);
            else
                return Color.FromArgb(76, 175, 80);
        }

        private static GraphicsPath RoundedRect(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            var r = radius;

            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.X + rect.Width - r, rect.Y + rect.Height - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Y + rect.Height - r, r, r, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void StartCountdown()
        {
            if (_countdownTimer == null)
            {
                _countdownTimer = new System.Windows.Forms.Timer();
                _countdownTimer.Interval = 1000;
                _countdownTimer.Tick += CountdownTimer_Tick;
            }

            UpdateCountdownDisplay();
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object? sender, EventArgs e)
        {
            UpdateCountdownDisplay();
        }

        private void UpdateCountdownDisplay()
        {
            var now = DateTime.Now;
            var nextRefresh = DateTime.Today.AddDays(1).AddHours(6);

            if (now < DateTime.Today.AddHours(6))
            {
                nextRefresh = DateTime.Today.AddHours(6);
            }

            var timeLeft = nextRefresh - now;

            if (timeLeft.TotalSeconds <= 0)
            {
                UpdateControlText("labelCountdown", "⏰ 正在刷新...");
                LoadData();
                return;
            }

            UpdateControlText("labelCountdown",
                $"⏰ 刷新倒计时: {timeLeft.Hours:D2}:{timeLeft.Minutes:D2}:{timeLeft.Seconds:D2}");
        }

        private void UpdateControlText(string controlName, string text)
        {
            foreach (Control control in _tabToday.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control child in panel.Controls)
                    {
                        if (child.Name == controlName && child is Label label)
                        {
                            label.Text = text;
                            return;
                        }
                    }
                }
            }
        }

        private void OnClaimRewardClicked(object? sender, Challenge e)
        {
            LoadData();
        }

        private void OnStatsUpdated(object? sender, (int completed, int total) e)
        {
            double percent = e.total > 0 ? (double)e.completed / e.total : 0;

            foreach (Control control in _tabToday.Controls)
            {
                if (control is Panel panel)
                {
                    foreach (Control child in panel.Controls)
                    {
                        if (child.Name == "progressRing" && child is ProgressRingControl ring)
                        {
                            ring.Progress = (float)percent;
                            ring.CenterText = $"{(int)(percent * 100)}%";
                        }
                    }
                }
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (_tabControl != null)
            {
                _tabControl.BackColor = colors.Background;
            }

            if (_tabToday != null)
            {
                _tabToday.BackColor = colors.Surface;
            }

            if (_tabHistory != null)
            {
                _tabHistory.BackColor = colors.Surface;
            }

            if (_tabAchievements != null)
            {
                _tabAchievements.BackColor = colors.Surface;
            }

            if (_challengesPanel != null)
            {
                _challengesPanel.BackColor = colors.Surface;
            }

            if (_historyPanel != null)
            {
                _historyPanel.BackColor = colors.Surface;
                _historyPanel.Invalidate();
            }

            if (_statsPanel != null)
            {
                _statsPanel.BackColor = colors.ThemeMode == ThemeMode.Dark ? colors.Background : Color.FromArgb(250, 250, 252);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _countdownTimer?.Stop();
            _countdownTimer?.Dispose();
            _themeService?.UnregisterThemeable(this);
            base.OnFormClosed(e);
        }
    }
}