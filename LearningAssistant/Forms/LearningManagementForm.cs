using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Text;

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
        private readonly IUserSessionService? _userSessionService;
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

        private List<DailyStatistics>? _cachedTrendData;
        private Dictionary<string, int>? _cachedCategoryStats;
        private List<WrongAnswerItem>? _cachedWrongAnswers;

        public LearningManagementForm(
            ILearningAnalyticsService analyticsService,
            ILearningReminderService reminderService,
            LearningReportService reportService,
            QuoteService quoteService,
            ILearningGoalService goalService,
            IWrongAnswerService wrongAnswerService,
            ILogger<LearningManagementForm>? logger = null,
            IThemeService? themeService = null,
            IUserSessionService? userSessionService = null,
            string? userId = null)
        {
            InitializeComponent();
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _reminderService = reminderService ?? throw new ArgumentNullException(nameof(reminderService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _goalService = goalService ?? throw new ArgumentNullException(nameof(goalService));
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _logger = logger;
            _themeService = themeService;
            _userSessionService = userSessionService;
            _userId = userId ?? userSessionService?.CurrentUserId ?? Environment.UserName;

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
            DateTime startDate, endDate;
            GetDateRange(out startDate, out endDate);
            _cachedTrendData = _analyticsService.GetLearningTrend(_userId, startDate, endDate);
            _panelTrendChart?.Invalidate();
        }

        private void LoadCategoryProgress()
        {
            _cachedCategoryStats = _analyticsService.GetCategoryStats(_userId);
            _panelCategoryProgress?.Invalidate();
        }

        private void LoadWrongAnswerStats()
        {
            _cachedWrongAnswers = _wrongAnswerService.GetWrongAnswers(_userId);
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

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = "Markdown文件|*.md|文本文件|*.txt",
                FileName = $"学习统计报告_{DateTime.Now:yyyyMMdd}.md",
                DefaultExt = "md"
            };

            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                ExportReport(dialog.FileName);
                MessageBox.Show("报告导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出报告失败");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportReport(string filePath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("# 📊 学习统计报告");
            sb.AppendLine();
            sb.AppendLine($"**生成时间**: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine($"**时间范围**: {_currentTimeRange}");
            sb.AppendLine($"**用户**: {_userId}");
            sb.AppendLine();

            // 统计概览
            sb.AppendLine("## 📈 统计概览");
            sb.AppendLine();
            sb.AppendLine("| 指标 | 数值 |");
            sb.AppendLine("|------|------|");

            try
            {
                var totalMinutes = _analyticsService.GetTotalStudyMinutes(_userId, GetTimeRangeStart());
                var totalItems = _analyticsService.GetTotalLearnedItems(_userId, GetTimeRangeStart());
                var accuracy = _analyticsService.GetAccuracyRate(_userId, GetTimeRangeStart());
                var streak = _analyticsService.GetStudyStreak(_userId);

                sb.AppendLine($"| 学习时长 | {totalMinutes}分钟 |");
                sb.AppendLine($"| 学习项目 | {totalItems}个 |");
                sb.AppendLine($"| 正确率 | {accuracy:F1}% |");
                sb.AppendLine($"| 连续学习 | {streak}天 |");
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "生成统计概览失败");
                sb.AppendLine("| 学习时长 | - |");
                sb.AppendLine("| 学习项目 | - |");
                sb.AppendLine("| 正确率 | - |");
                sb.AppendLine("| 连续学习 | - |");
            }
            sb.AppendLine();

            // 错题统计
            sb.AppendLine("## 📕 错题统计");
            sb.AppendLine();
            try
            {
                var wrongAnswers = _wrongAnswerService.GetWrongAnswers(_userId, 0, 100);
                var totalWrong = wrongAnswers.Count;
                var mastered = wrongAnswers.Count(w => w.IsMastered);
                var pending = totalWrong - mastered;

                sb.AppendLine($"- 总错题数: {totalWrong}");
                sb.AppendLine($"- 已掌握: {mastered}");
                sb.AppendLine($"- 待复习: {pending}");
                sb.AppendLine();

                if (wrongAnswers.Any())
                {
                    sb.AppendLine("### 分类分布");
                    sb.AppendLine();
                    var byCategory = wrongAnswers.GroupBy(w => w.Subject ?? "通用")
                        .Select(g => new { Category = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count);

                    foreach (var cat in byCategory.Take(5))
                    {
                        sb.AppendLine($"- {cat.Category}: {cat.Count}题");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "生成错题统计失败");
                sb.AppendLine("- 暂无错题数据");
            }
            sb.AppendLine();

            // 学习建议
            sb.AppendLine("## 💡 学习建议");
            sb.AppendLine();
            try
            {
                var recommendations = _reminderService.GetLearningRecommendations(_userId);
                foreach (var rec in recommendations.Take(3))
                {
                    sb.AppendLine($"- {rec}");
                }
            }
            catch
            {
                sb.AppendLine("- 继续保持学习习惯");
                sb.AppendLine("- 定期复习错题");
                sb.AppendLine("- 逐步拓展学习范围");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine("*本报告由学习助手自动生成*");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }

        private DateTime GetTimeRangeStart()
        {
            return _currentTimeRange switch
            {
                "今日" => DateTime.Today,
                "本周" => DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek),
                "本月" => new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1),
                _ => DateTime.MinValue
            };
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
            var trendData = _cachedTrendData ?? new List<DailyStatistics>();

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

            var categoryStats = _cachedCategoryStats ?? new Dictionary<string, int>();
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
                var wrongAnswers = _cachedWrongAnswers ?? new List<WrongAnswerItem>();
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
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "绘制错题统计面板失败");
            }
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


        #region 窗体控件字段（和拖拽生成格式完全一致，设计器可识别）
        private IContainer components = null;
        private ComponentResourceManager resources;

        // 顶部头部面板
        private Panel panelHeader;
        private Label labelTitle;
        private Button btnToday;
        private Button btnWeek;
        private Button btnMonth;
        private Button btnAll;
        private Button btnExport;

        // 主体内容面板
        private Panel panelContent;

        // 统计卡片区域
        private Panel panelCards;

        // 日历区域
        private Panel panelCalendar;
        private Label labelCalendarTitle;

        private List<Button> _timeRangeButtons = new();
        #endregion

        private void InitializeComponent()
        {
            this.components = new Container();
            this.resources = new ComponentResourceManager(typeof(LearningManagementForm));
            this._timeRangeButtons = new List<Button>();

            // 基础窗体配置
            this.SuspendLayout();
            // 
            // LearningManagementForm
            // 
            this.Text = "📊 学习统计";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.MinimumSize = new Size(800, 550);
            this.Resize += new EventHandler(this.LearningManagementForm_Resize);

            #region 1. 顶部 Header 区域
            // panelHeader
            this.panelHeader = new Panel();
            this.panelHeader.Dock = DockStyle.Top;
            this.panelHeader.Height = 50;
            this.panelHeader.BackColor = Color.White;

            // labelTitle
            this.labelTitle = new Label();
            this.labelTitle.Text = "📊 学习统计仪表板";
            this.labelTitle.Font = new Font("微软雅黑", 14F, FontStyle.Bold, GraphicsUnit.Point, 134);
            this.labelTitle.ForeColor = Color.FromArgb(33, 33, 33);
            this.labelTitle.Location = new Point(20, 12);
            this.labelTitle.AutoSize = true;

            // btnToday
            this.btnToday = new Button();
            this.btnToday.Text = "今日";
            this.btnToday.Tag = "今日";
            this.btnToday.Location = new Point(550, 12);
            this.btnToday.Size = new Size(60, 28);
            this.btnToday.FlatStyle = FlatStyle.Flat;
            this.btnToday.Cursor = Cursors.Hand;
            this.btnToday.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.btnToday.BackColor = Color.FromArgb(240, 240, 245);
            this.btnToday.ForeColor = Color.FromArgb(60, 60, 60);
            this.btnToday.FlatAppearance.BorderSize = 0;
            this.btnToday.Click += new EventHandler(this.BtnTimeRange_Click);

            // btnWeek
            this.btnWeek = new Button();
            this.btnWeek.Text = "本周";
            this.btnWeek.Tag = "本周";
            this.btnWeek.Location = new Point(615, 12);
            this.btnWeek.Size = new Size(60, 28);
            this.btnWeek.FlatStyle = FlatStyle.Flat;
            this.btnWeek.Cursor = Cursors.Hand;
            this.btnWeek.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.btnWeek.BackColor = Color.FromArgb(63, 81, 181);
            this.btnWeek.ForeColor = Color.White;
            this.btnWeek.FlatAppearance.BorderSize = 0;
            this.btnWeek.Click += new EventHandler(this.BtnTimeRange_Click);

            // btnMonth
            this.btnMonth = new Button();
            this.btnMonth.Text = "本月";
            this.btnMonth.Tag = "本月";
            this.btnMonth.Location = new Point(680, 12);
            this.btnMonth.Size = new Size(60, 28);
            this.btnMonth.FlatStyle = FlatStyle.Flat;
            this.btnMonth.Cursor = Cursors.Hand;
            this.btnMonth.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.btnMonth.BackColor = Color.FromArgb(240, 240, 245);
            this.btnMonth.ForeColor = Color.FromArgb(60, 60, 60);
            this.btnMonth.FlatAppearance.BorderSize = 0;
            this.btnMonth.Click += new EventHandler(this.BtnTimeRange_Click);

            // btnAll
            this.btnAll = new Button();
            this.btnAll.Text = "全部";
            this.btnAll.Tag = "全部";
            this.btnAll.Location = new Point(745, 12);
            this.btnAll.Size = new Size(60, 28);
            this.btnAll.FlatStyle = FlatStyle.Flat;
            this.btnAll.Cursor = Cursors.Hand;
            this.btnAll.Font = new Font("微软雅黑", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            this.btnAll.BackColor = Color.FromArgb(240, 240, 245);
            this.btnAll.ForeColor = Color.FromArgb(60, 60, 60);
            this.btnAll.FlatAppearance.BorderSize = 0;
            this.btnAll.Click += new EventHandler(this.BtnTimeRange_Click);

            // btnExport 导出按钮
            this.btnExport = new Button();
            this.btnExport.Text = "📤 导出";
            this.btnExport.Location = new Point(810, 12);
            this.btnExport.Size = new Size(70, 28);
            this.btnExport.FlatStyle = FlatStyle.Flat;
            this.btnExport.Cursor = Cursors.Hand;
            this.btnExport.Font = new Font("微软雅黑", 9F, FontStyle.Bold, GraphicsUnit.Point, 134);
            this.btnExport.BackColor = Color.FromArgb(76, 175, 80);
            this.btnExport.ForeColor = Color.White;
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.Click += new EventHandler(this.BtnExport_Click);

            // 按钮集合赋值
            this._timeRangeButtons.Add(this.btnToday);
            this._timeRangeButtons.Add(this.btnWeek);
            this._timeRangeButtons.Add(this.btnMonth);
            this._timeRangeButtons.Add(this.btnAll);

            // Header 添加子控件（从上到下顺序和拖拽一致）
            this.panelHeader.Controls.Add(this.labelTitle);
            this.panelHeader.Controls.Add(this.btnToday);
            this.panelHeader.Controls.Add(this.btnWeek);
            this.panelHeader.Controls.Add(this.btnMonth);
            this.panelHeader.Controls.Add(this.btnAll);
            this.panelHeader.Controls.Add(this.btnExport);
            #endregion

            #region 2. 主体内容面板 panelContent
            this.panelContent = new Panel();
            this.panelContent.Dock = DockStyle.Fill;
            this.panelContent.BackColor = Color.FromArgb(245, 245, 250);
            this.panelContent.AutoScroll = true;

            // panelCards 卡片容器
            this.panelCards = new Panel();
            this.panelCards.Location = new Point(15, 15);
            this.panelCards.Size = new Size(855, 110);
            this.panelCards.BackColor = Color.Transparent;

            // 自定义统计卡片 StatCard
            this._cardMinutes = new StatCard();
            this._cardMinutes.Location = new Point(0, 0);
            this._cardMinutes.Size = new Size(200, 110);
            this._cardMinutes.Icon = "⏱️";
            this._cardMinutes.Value = "0分";
            this._cardMinutes.Label = "学习时长";
            this._cardMinutes.AccentColor = Color.FromArgb(33, 150, 243);
            this._cardMinutes.CardColor = Color.White;

            this._cardItems = new StatCard();
            this._cardItems.Location = new Point(215, 0);
            this._cardItems.Size = new Size(200, 110);
            this._cardItems.Icon = "📚";
            this._cardItems.Value = "0个";
            this._cardItems.Label = "已学词汇";
            this._cardItems.AccentColor = Color.FromArgb(76, 175, 80);
            this._cardItems.CardColor = Color.White;

            this._cardAccuracy = new StatCard();
            this._cardAccuracy.Location = new Point(430, 0);
            this._cardAccuracy.Size = new Size(200, 110);
            this._cardAccuracy.Icon = "🎯";
            this._cardAccuracy.Value = "0%";
            this._cardAccuracy.Label = "正确率";
            this._cardAccuracy.AccentColor = Color.FromArgb(255, 152, 0);
            this._cardAccuracy.CardColor = Color.White;

            this._cardStreak = new StatCard();
            this._cardStreak.Location = new Point(645, 0);
            this._cardStreak.Size = new Size(200, 110);
            this._cardStreak.Icon = "🔥";
            this._cardStreak.Value = "0天";
            this._cardStreak.Label = "连续天数";
            this._cardStreak.AccentColor = Color.FromArgb(244, 67, 54);
            this._cardStreak.CardColor = Color.White;

            this.panelCards.Controls.Add(this._cardMinutes);
            this.panelCards.Controls.Add(this._cardItems);
            this.panelCards.Controls.Add(this._cardAccuracy);
            this.panelCards.Controls.Add(this._cardStreak);

            // 趋势图表面板
            this._panelTrendChart = new Panel();
            this._panelTrendChart.Location = new Point(15, 140);
            this._panelTrendChart.Size = new Size(855, 180);
            this._panelTrendChart.BackColor = Color.White;
            this._panelTrendChart.Paint += new PaintEventHandler(this.PanelTrendChart_Paint);

            // 分类进度面板
            this._panelCategoryProgress = new Panel();
            this._panelCategoryProgress.Location = new Point(15, 335);
            this._panelCategoryProgress.Size = new Size(520, 200);
            this._panelCategoryProgress.BackColor = Color.White;
            this._panelCategoryProgress.Paint += new PaintEventHandler(this.PanelCategoryProgress_Paint);

            // 错题统计面板
            this._panelWrongStats = new Panel();
            this._panelWrongStats.Location = new Point(550, 335);
            this._panelWrongStats.Size = new Size(320, 200);
            this._panelWrongStats.BackColor = Color.White;
            this._panelWrongStats.Paint += new PaintEventHandler(this.PanelWrongStats_Paint);

            // 日历面板
            this.panelCalendar = new Panel();
            this.panelCalendar.Location = new Point(15, 550);
            this.panelCalendar.Size = new Size(855, 320);
            this.panelCalendar.BackColor = Color.White;

            this.labelCalendarTitle = new Label();
            this.labelCalendarTitle.Text = "📅 打卡日历（本月）";
            this.labelCalendarTitle.Font = new Font("微软雅黑", 10F, FontStyle.Bold, GraphicsUnit.Point, 134);
            this.labelCalendarTitle.ForeColor = Color.FromArgb(33, 33, 33);
            this.labelCalendarTitle.Location = new Point(15, 10);
            this.labelCalendarTitle.AutoSize = true;

            this._calendarView = new GoalCalendarView();
            this._calendarView.Location = new Point(15, 40);
            this._calendarView.Size = new Size(350, 260);
            this._calendarView.BackColor = Color.White;

            this.panelCalendar.Controls.Add(this.labelCalendarTitle);
            this.panelCalendar.Controls.Add(this._calendarView);

            // 内容面板装载所有子模块
            this.panelContent.Controls.Add(this.panelCards);
            this.panelContent.Controls.Add(this._panelTrendChart);
            this.panelContent.Controls.Add(this._panelCategoryProgress);
            this.panelContent.Controls.Add(this._panelWrongStats);
            this.panelContent.Controls.Add(this.panelCalendar);
            #endregion

            // 窗体顶层控件添加（顺序和拖拽一致，后加的层级在上）
            this.Controls.Add(this.panelContent);
            this.Controls.Add(this.panelHeader);

            // 标准布局恢复（VS自动生成固定写法）
            this.ResumeLayout(false);
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

        #region IDisposable Support
        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // 注销主题服务
                _themeService?.UnregisterThemeable(this);

                // 注销事件订阅
                Resize -= LearningManagementForm_Resize;
                if (btnToday != null) btnToday.Click -= BtnTimeRange_Click;
                if (btnWeek != null) btnWeek.Click -= BtnTimeRange_Click;
                if (btnMonth != null) btnMonth.Click -= BtnTimeRange_Click;
                if (btnAll != null) btnAll.Click -= BtnTimeRange_Click;
                if (btnExport != null) btnExport.Click -= BtnExport_Click;

                if (_panelTrendChart != null)
                    _panelTrendChart.Paint -= PanelTrendChart_Paint;
                if (_panelCategoryProgress != null)
                    _panelCategoryProgress.Paint -= PanelCategoryProgress_Paint;
                if (_panelWrongStats != null)
                    _panelWrongStats.Paint -= PanelWrongStats_Paint;

                // 释放组件
                if (components != null)
                {
                    components.Dispose();
                    components = null;
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }
        #endregion
    }
}
