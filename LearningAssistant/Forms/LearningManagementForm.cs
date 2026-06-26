using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;
using System.Globalization;

namespace LearningAssistant.Forms
{
    public partial class LearningManagementForm : Form, IThemeable
    {
        private readonly ILearningAnalyticsService _analyticsService;
        private readonly ILearningReminderService _reminderService;
        private readonly LearningReportService _reportService;
        private readonly QuoteService _quoteService;
        private readonly ILearningGoalService _goalService;
        private readonly IWrongAnswerService _wrongAnswerService;
        private readonly ILogger<LearningManagementForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly string _userId;

        private string _currentTimeRange = "本周";
        private StatCard? _cardMinutes;
        private StatCard? _cardItems;
        private StatCard? _cardAccuracy;
        private StatCard? _cardStreak;

        private Panel? _panelTrendChart;
        private Panel? _panelCategoryProgress;
        private Panel? _panelWrongStats;
        private GoalCalendarView? _calendarView;

        public LearningManagementForm(
            ILearningAnalyticsService analyticsService,
            ILearningReminderService reminderService,
            LearningReportService reportService,
            QuoteService quoteService,
            ILearningGoalService goalService,
            IWrongAnswerService wrongAnswerService,
            ILogger<LearningManagementForm>? logger = null,
            IThemeService? themeService = null,
            string? userId = null)
        {
            InitializeComponent();
            _analyticsService = analyticsService;
            _reminderService = reminderService;
            _reportService = reportService;
            _quoteService = quoteService;
            _goalService = goalService;
            _wrongAnswerService = wrongAnswerService;
            _logger = logger;
            _themeService = themeService;
            _userId = userId ?? Environment.UserName;

            _themeService?.RegisterThemeable(this);

            _logger?.LogInformation("学习统计窗口初始化，用户ID: {UserId}", _userId);
            LoadStatsData();
        }

        private void LoadStatsData()
        {
            try
            {
                LoadStatCards();
                LoadTrendChart();
                LoadCategoryProgress();
                LoadWrongAnswerStats();
                LoadCalendar();
                _logger?.LogDebug("学习统计数据加载完成");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载学习统计数据失败");
                MessageBox.Show($"加载数据失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void LoadStatCards()
        {
            DateTime startDate, endDate;
            GetDateRange(out startDate, out endDate);

            var stats = _analyticsService.GetLearningTrend(_userId, startDate, endDate);
            var todayStats = _analyticsService.GetDailyStatistics(_userId, DateTime.Today);
            var yesterdayStats = _analyticsService.GetDailyStatistics(_userId, DateTime.Today.AddDays(-1));
            int streak = _analyticsService.GetStudyStreak(_userId);

            int totalMinutes = stats.Sum(s => s.TotalMinutes);
            int totalItems = stats.Sum(s => s.TotalItems);
            double avgAccuracy = stats.Count > 0 ? stats.Average(s => s.CorrectRate) : 0;

            int minutesChange = todayStats.TotalMinutes - yesterdayStats.TotalMinutes;
            int itemsChange = todayStats.TotalItems - yesterdayStats.TotalItems;
            double accuracyChange = todayStats.CorrectRate - yesterdayStats.CorrectRate;

            if (_cardMinutes != null)
            {
                _cardMinutes.Value = $"{totalMinutes}分";
                _cardMinutes.Trend = minutesChange >= 0 ? $"+{minutesChange}分" : $"{minutesChange}分";
                _cardMinutes.TrendDir = minutesChange >= 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down;
            }

            if (_cardItems != null)
            {
                _cardItems.Value = $"{totalItems}个";
                _cardItems.Trend = itemsChange >= 0 ? $"+{itemsChange}个" : $"{itemsChange}个";
                _cardItems.TrendDir = itemsChange >= 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down;
            }

            if (_cardAccuracy != null)
            {
                _cardAccuracy.Value = $"{avgAccuracy * 100:F1}%";
                _cardAccuracy.Trend = accuracyChange >= 0 ? $"+{accuracyChange * 100:F1}%" : $"{accuracyChange * 100:F1}%";
                _cardAccuracy.TrendDir = accuracyChange >= 0 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down;
            }

            if (_cardStreak != null)
            {
                _cardStreak.Value = $"{streak}天";
                _cardStreak.Trend = streak > 0 ? "继续加油" : "开始学习吧";
                _cardStreak.TrendDir = StatCard.TrendDirection.None;
            }
        }

        private void GetDateRange(out DateTime startDate, out DateTime endDate)
        {
            endDate = DateTime.Today;
            switch (_currentTimeRange)
            {
                case "今日":
                    startDate = DateTime.Today;
                    break;
                case "本周":
                    startDate = DateTime.Today.AddDays(-6);
                    break;
                case "本月":
                    startDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    break;
                case "全部":
                default:
                    startDate = DateTime.Today.AddDays(-30);
                    break;
            }
        }

        private void LoadTrendChart()
        {
            _panelTrendChart?.Invalidate();
        }

        private void LoadCategoryProgress()
        {
            _panelCategoryProgress?.Invalidate();
        }

        private void LoadWrongAnswerStats()
        {
            _panelWrongStats?.Invalidate();
        }

        private void LoadCalendar()
        {
            if (_calendarView != null)
            {
                _calendarView.GoalService = _goalService;
                _calendarView.CurrentUserId = _userId;
                _calendarView.CurrentMonth = DateTime.Today;
            }
        }

        private void BtnTimeRange_Click(object? sender, EventArgs e)
        {
            if (sender is Button btn)
            {
                _currentTimeRange = btn.Tag?.ToString() ?? "本周";
                UpdateTimeRangeButtons();
                LoadStatsData();
            }
        }

        private void UpdateTimeRangeButtons()
        {
            if (_timeRangeButtons == null) return;

            foreach (var btn in _timeRangeButtons)
            {
                bool isActive = btn.Tag?.ToString() == _currentTimeRange;
                btn.BackColor = isActive ? Color.FromArgb(63, 81, 181) : Color.FromArgb(240, 240, 245);
                btn.ForeColor = isActive ? Color.White : Color.FromArgb(60, 60, 60);
            }
        }

        private void PanelTrendChart_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _panelTrendChart == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _panelTrendChart.ClientRectangle;
            int paddingLeft = 50;
            int paddingRight = 20;
            int paddingTop = 20;
            int paddingBottom = 30;

            int chartWidth = rect.Width - paddingLeft - paddingRight;
            int chartHeight = rect.Height - paddingTop - paddingBottom;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            g.DrawString("📈 学习时长趋势", titleFont, titleBrush, paddingLeft, 8);

            DateTime startDate, endDate;
            GetDateRange(out startDate, out endDate);
            var trendData = _analyticsService.GetLearningTrend(_userId, startDate, endDate);

            if (trendData.Count == 0) return;

            int maxMinutes = Math.Max(30, trendData.Max(d => d.TotalMinutes));
            int barCount = trendData.Count;
            int barWidth = Math.Max(20, (chartWidth - (barCount - 1) * 8) / barCount);

            for (int i = 0; i < barCount; i++)
            {
                int barHeight = (int)((double)trendData[i].TotalMinutes / maxMinutes * chartHeight);
                int barX = paddingLeft + i * (barWidth + 8);
                int barY = paddingTop + chartHeight - barHeight;

                Color barColor = Color.FromArgb(63, 81, 181);
                using var barBrush = new SolidBrush(barColor);
                var barRect = new Rectangle(barX, barY, barWidth, barHeight);
                using var barPath = RoundedRect(barRect, 4);
                g.FillPath(barBrush, barPath);

                using var labelFont = new Font("微软雅黑", 8F);
                using var labelBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
                string dayLabel = trendData[i].Date.ToString("MM/dd");
                var labelSize = g.MeasureString(dayLabel, labelFont);
                g.DrawString(dayLabel, labelFont, labelBrush,
                    barX + barWidth / 2 - labelSize.Width / 2,
                    paddingTop + chartHeight + 5);
            }
        }

        private void PanelCategoryProgress_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _panelCategoryProgress == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _panelCategoryProgress.ClientRectangle;
            int paddingLeft = 15;
            int paddingRight = 15;
            int paddingTop = 35;
            int paddingBottom = 15;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            g.DrawString("📚 各分类学习情况", titleFont, titleBrush, paddingLeft, 10);

            var categoryStats = _analyticsService.GetCategoryStats(_userId);
            if (categoryStats.Count == 0)
            {
                using var emptyFont = new Font("微软雅黑", 9F);
                using var emptyBrush = new SolidBrush(Color.FromArgb(150, 150, 150));
                g.DrawString("暂无数据", emptyFont, emptyBrush, paddingLeft, paddingTop + 20);
                return;
            }

            int maxValue = Math.Max(1, categoryStats.Values.Max());
            int barHeight = 18;
            int barSpacing = 10;
            int contentWidth = rect.Width - paddingLeft - paddingRight;
            int barMaxWidth = contentWidth - 60;

            int y = paddingTop;
            int count = 0;
            var topCategories = categoryStats.OrderByDescending(kv => kv.Value).Take(5);

            Color[] barColors = {
                Color.FromArgb(63, 81, 181),
                Color.FromArgb(33, 150, 243),
                Color.FromArgb(76, 175, 80),
                Color.FromArgb(255, 152, 0),
                Color.FromArgb(156, 39, 176)
            };

            foreach (var kv in topCategories)
            {
                int barWidth = (int)((double)kv.Value / maxValue * barMaxWidth);
                barWidth = Math.Max(2, barWidth);

                using var nameFont = new Font("微软雅黑", 9F);
                using var nameBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                g.DrawString(kv.Key, nameFont, nameBrush, paddingLeft, y);

                int barY = y + 22;
                using var bgBrush = new SolidBrush(Color.FromArgb(240, 240, 245));
                var bgRect = new Rectangle(paddingLeft + 55, barY, barMaxWidth, barHeight);
                using var bgPath = RoundedRect(bgRect, 4);
                g.FillPath(bgBrush, bgPath);

                Color barColor = barColors[count % barColors.Length];
                using var barBrush = new SolidBrush(barColor);
                var barRect = new Rectangle(paddingLeft + 55, barY, barWidth, barHeight);
                using var barPath = RoundedRect(barRect, 4);
                g.FillPath(barBrush, barPath);

                using var valueFont = new Font("微软雅黑", 8F, FontStyle.Bold);
                using var valueBrush = new SolidBrush(Color.FromArgb(50, 50, 50));
                string valueText = $"{kv.Value}个";
                g.DrawString(valueText, valueFont, valueBrush,
                    paddingLeft + 55 + barWidth + 5, barY + 2);

                y += barHeight + barSpacing + 5;
                count++;
            }
        }

        private void PanelWrongStats_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _panelWrongStats == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _panelWrongStats.ClientRectangle;
            int paddingLeft = 15;
            int paddingTop = 35;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            g.DrawString("📕 错题统计", titleFont, titleBrush, paddingLeft, 10);

            try
            {
                var wrongAnswers = _wrongAnswerService.GetWrongAnswers(_userId);
                int total = wrongAnswers.Count;
                int mastered = wrongAnswers.Count(w => w.IsMastered);
                int review = total - mastered;
                double rate = total > 0 ? (double)mastered / total * 100 : 0;
                int today = wrongAnswers.Count(w => w.AddedAt.Date == DateTime.Today);

                using var statFont = new Font("微软雅黑", 9F);
                using var labelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
                using var valueBrush = new SolidBrush(Color.FromArgb(33, 33, 33));

                int y = paddingTop;
                int lineHeight = 24;

                g.DrawString("总错题:", statFont, labelBrush, paddingLeft, y);
                g.DrawString(total.ToString(), statFont, valueBrush, paddingLeft + 70, y);
                y += lineHeight;

                g.DrawString("待复习:", statFont, labelBrush, paddingLeft, y);
                using var reviewBrush = new SolidBrush(Color.FromArgb(244, 67, 54));
                g.DrawString(review.ToString(), statFont, reviewBrush, paddingLeft + 70, y);
                y += lineHeight;

                g.DrawString("已掌握:", statFont, labelBrush, paddingLeft, y);
                using var masteredBrush = new SolidBrush(Color.FromArgb(76, 175, 80));
                g.DrawString(mastered.ToString(), statFont, masteredBrush, paddingLeft + 70, y);
                y += lineHeight;

                g.DrawString("掌握率:", statFont, labelBrush, paddingLeft, y);
                using var rateBrush = new SolidBrush(Color.FromArgb(33, 150, 243));
                g.DrawString($"{rate:F1}%", statFont, rateBrush, paddingLeft + 70, y);
                y += lineHeight;

                g.DrawString("今日新增:", statFont, labelBrush, paddingLeft, y);
                using var todayBrush = new SolidBrush(Color.FromArgb(156, 39, 176));
                g.DrawString(today.ToString(), statFont, todayBrush, paddingLeft + 70, y);
            }
            catch { }
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

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "📊 学习统计";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("微软雅黑", 9F);
            this.MinimumSize = new Size(800, 550);

            Panel panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White
            };

            Label labelTitle = new Label
            {
                Text = "📊 学习统计仪表板",
                Font = new Font("微软雅黑", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(20, 12),
                AutoSize = true
            };

            Button btnToday = new Button
            {
                Text = "今日",
                Tag = "今日",
                Location = new Point(550, 12),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            btnToday.FlatAppearance.BorderSize = 0;
            btnToday.Click += BtnTimeRange_Click;

            Button btnWeek = new Button
            {
                Text = "本周",
                Tag = "本周",
                Location = new Point(615, 12),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(63, 81, 181),
                ForeColor = Color.White
            };
            btnWeek.FlatAppearance.BorderSize = 0;
            btnWeek.Click += BtnTimeRange_Click;

            Button btnMonth = new Button
            {
                Text = "本月",
                Tag = "本月",
                Location = new Point(680, 12),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            btnMonth.FlatAppearance.BorderSize = 0;
            btnMonth.Click += BtnTimeRange_Click;

            Button btnAll = new Button
            {
                Text = "全部",
                Tag = "全部",
                Location = new Point(745, 12),
                Size = new Size(60, 28),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("微软雅黑", 9F),
                BackColor = Color.FromArgb(240, 240, 245),
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            btnAll.FlatAppearance.BorderSize = 0;
            btnAll.Click += BtnTimeRange_Click;

            _timeRangeButtons = new List<Button> { btnToday, btnWeek, btnMonth, btnAll };

            panelHeader.Controls.Add(labelTitle);
            panelHeader.Controls.Add(btnToday);
            panelHeader.Controls.Add(btnWeek);
            panelHeader.Controls.Add(btnMonth);
            panelHeader.Controls.Add(btnAll);

            Panel panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 245, 250),
                AutoScroll = true
            };

            Panel panelCards = new Panel
            {
                Location = new Point(15, 15),
                Size = new Size(855, 110),
                BackColor = Color.Transparent
            };

            _cardMinutes = new StatCard
            {
                Location = new Point(0, 0),
                Size = new Size(200, 110),
                Icon = "⏱️",
                Value = "0分",
                Label = "学习时长",
                AccentColor = Color.FromArgb(33, 150, 243),
                CardColor = Color.White
            };

            _cardItems = new StatCard
            {
                Location = new Point(215, 0),
                Size = new Size(200, 110),
                Icon = "📚",
                Value = "0个",
                Label = "已学词汇",
                AccentColor = Color.FromArgb(76, 175, 80),
                CardColor = Color.White
            };

            _cardAccuracy = new StatCard
            {
                Location = new Point(430, 0),
                Size = new Size(200, 110),
                Icon = "🎯",
                Value = "0%",
                Label = "正确率",
                AccentColor = Color.FromArgb(255, 152, 0),
                CardColor = Color.White
            };

            _cardStreak = new StatCard
            {
                Location = new Point(645, 0),
                Size = new Size(200, 110),
                Icon = "🔥",
                Value = "0天",
                Label = "连续天数",
                AccentColor = Color.FromArgb(244, 67, 54),
                CardColor = Color.White
            };

            panelCards.Controls.Add(_cardMinutes);
            panelCards.Controls.Add(_cardItems);
            panelCards.Controls.Add(_cardAccuracy);
            panelCards.Controls.Add(_cardStreak);

            _panelTrendChart = new Panel
            {
                Location = new Point(15, 140),
                Size = new Size(855, 180),
                BackColor = Color.White
            };
            _panelTrendChart.Paint += PanelTrendChart_Paint;

            _panelCategoryProgress = new Panel
            {
                Location = new Point(15, 335),
                Size = new Size(520, 200),
                BackColor = Color.White
            };
            _panelCategoryProgress.Paint += PanelCategoryProgress_Paint;

            _panelWrongStats = new Panel
            {
                Location = new Point(550, 335),
                Size = new Size(320, 200),
                BackColor = Color.White
            };
            _panelWrongStats.Paint += PanelWrongStats_Paint;

            Panel panelCalendar = new Panel
            {
                Location = new Point(15, 550),
                Size = new Size(855, 320),
                BackColor = Color.White
            };

            Label labelCalendarTitle = new Label
            {
                Text = "📅 打卡日历（本月）",
                Font = new Font("微软雅黑", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(33, 33, 33),
                Location = new Point(15, 10),
                AutoSize = true
            };

            _calendarView = new GoalCalendarView
            {
                Location = new Point(15, 40),
                Size = new Size(350, 260),
                BackColor = Color.White
            };

            panelCalendar.Controls.Add(labelCalendarTitle);
            panelCalendar.Controls.Add(_calendarView);

            panelContent.Controls.Add(panelCards);
            panelContent.Controls.Add(_panelTrendChart);
            panelContent.Controls.Add(_panelCategoryProgress);
            panelContent.Controls.Add(_panelWrongStats);
            panelContent.Controls.Add(panelCalendar);

            this.Controls.Add(panelContent);
            this.Controls.Add(panelHeader);

            this.ResumeLayout(false);

            this.Resize += LearningManagementForm_Resize;
        }

        private void LearningManagementForm_Resize(object? sender, EventArgs e)
        {
            int width = this.ClientSize.Width - 30;
            if (width < 400) width = 400;

            if (_panelTrendChart != null)
                _panelTrendChart.Width = width;

            if (_panelCategoryProgress != null)
            {
                _panelCategoryProgress.Width = (int)(width * 0.6) - 10;
            }

            if (_panelWrongStats != null)
            {
                _panelWrongStats.Width = (int)(width * 0.4) - 10;
                if (_panelCategoryProgress != null)
                    _panelWrongStats.Left = _panelCategoryProgress.Right + 20;
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (_cardMinutes != null) _cardMinutes.CardColor = colors.Surface;
            if (_cardItems != null) _cardItems.CardColor = colors.Surface;
            if (_cardAccuracy != null) _cardAccuracy.CardColor = colors.Surface;
            if (_cardStreak != null) _cardStreak.CardColor = colors.Surface;

            if (_panelTrendChart != null)
                _panelTrendChart.BackColor = colors.Surface;
            if (_panelCategoryProgress != null)
                _panelCategoryProgress.BackColor = colors.Surface;
            if (_panelWrongStats != null)
                _panelWrongStats.BackColor = colors.Surface;

            if (_calendarView != null)
                _calendarView.BackColor = colors.Surface;

            _panelTrendChart?.Invalidate();
            _panelCategoryProgress?.Invalidate();
            _panelWrongStats?.Invalidate();
        }

        private List<Button> _timeRangeButtons = new();
    }
}
