using LearningAssistant.Common;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Forms.UserControls.Charts;
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
        private readonly ISpacedRepetitionService? _spacedRepetitionService;
        private readonly ILogger<LearningManagementForm>? _logger;
        private readonly IThemeService? _themeService;
        private readonly IUserSessionService? _userSessionService;
        private readonly string _userId;

        private string _currentTimeRange = "本周";
        private StatCard? _cardMinutes;
        private StatCard? _cardItems;
        private StatCard? _cardAccuracy;
        private StatCard? _cardStreak;
        private StatCard? _cardRetention;

        private LearningTrendChart? _chartTrend;
        private CategoryProgressChart? _chartCategory;
        private ForgettingCurveChart? _chartForgettingCurve;
        private ReviewDistributionChart? _chartRating;
        private Panel? _panelWrongStats;
        private Panel? _panelHeatmap;
        private Panel? _panelAlgorithm;
        private GoalCalendarView? _calendarView;
        private ComboBox? _cmbAlgorithm;

        private List<DailyStatistics>? _cachedTrendData;
        private Dictionary<string, int>? _cachedCategoryStats;
        private List<WrongAnswerItem>? _cachedWrongAnswers;
        private Dictionary<int, double>? _cachedForgettingCurve;
        private ReviewEfficiencyStats? _cachedEfficiencyStats;
        private List<HeatmapData>? _cachedHeatmapData;

        public LearningManagementForm(
            ILearningAnalyticsService analyticsService,
            ILearningReminderService reminderService,
            LearningReportService reportService,
            QuoteService quoteService,
            ILearningGoalService goalService,
            IWrongAnswerService wrongAnswerService,
            ISpacedRepetitionService? spacedRepetitionService = null,
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
            _spacedRepetitionService = spacedRepetitionService;
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
                LoadForgettingCurve();
                LoadHeatmap();
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

            // 加载记忆保留率
            if (_cardRetention != null)
            {
                double retentionRate = _analyticsService.CalculateRetentionRate(_userId);
                _cardRetention.Value = $"{retentionRate * 100:F0}%";
                _cardRetention.Trend = retentionRate >= 0.9 ? "掌握良好" : (retentionRate >= 0.7 ? "正常范围" : "建议复习");
                _cardRetention.TrendDir = retentionRate >= 0.7 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down;
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

            if (_chartTrend != null && _cachedTrendData != null)
            {
                var labels = _cachedTrendData.Select(s => $"{s.Date.Month}/{s.Date.Day}").ToList();
                var values = _cachedTrendData.Select(s => (double)s.TotalItems).ToList();
                var accuracyValues = _cachedTrendData.Select(s => s.CorrectRate * 100).ToList();
                _chartTrend.UpdateDataWithLabels(labels, values, accuracyValues);
            }
        }

        private void LoadCategoryProgress()
        {
            _cachedCategoryStats = _analyticsService.GetCategoryStats(_userId);

            if (_chartCategory != null && _cachedCategoryStats != null)
            {
                var categories = _cachedCategoryStats.Keys.Take(6).ToList();
                var progress = _cachedCategoryStats.Values.Take(6).Select(v => (double)v).ToList();
                _chartCategory.UpdateData(categories, progress);
            }
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

        private void LoadForgettingCurve()
        {
            try
            {
                _cachedForgettingCurve = _analyticsService.GenerateForgettingCurve(_userId, 30);
                _cachedEfficiencyStats = _analyticsService.GetReviewEfficiencyStats(_userId);

                if (_chartForgettingCurve != null && _cachedForgettingCurve != null)
                {
                    _chartForgettingCurve.UpdateCurve(_cachedForgettingCurve);
                }

                if (_chartRating != null && _cachedEfficiencyStats != null)
                {
                    _chartRating.UpdateData(_cachedEfficiencyStats.RatingDistribution);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载遗忘曲线数据失败");
            }
        }

        private void LoadHeatmap()
        {
            try
            {
                _cachedHeatmapData = _analyticsService.GetWeeklyHeatmap(_userId, 12);
                _panelHeatmap?.Invalidate();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载热力图数据失败");
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

        private void CmbAlgorithm_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_cmbAlgorithm == null || _spacedRepetitionService == null) return;

            string selectedAlgorithm = _cmbAlgorithm.SelectedItem?.ToString() ?? "SM-2";
            _spacedRepetitionService.SetAlgorithm(selectedAlgorithm);
            _logger?.LogInformation("用户切换学习算法: {Algorithm}", selectedAlgorithm);
            MessageBox.Show($"已切换到 {selectedAlgorithm} 算法\n新复习将使用此算法计算间隔。",
                "算法切换", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnCompareAlgorithm_Click(object? sender, EventArgs e)
        {
            if (_spacedRepetitionService == null)
            {
                MessageBox.Show("算法服务不可用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var comparison = _spacedRepetitionService.CompareAlgorithms(_userId);

                var sb = new StringBuilder();
                sb.AppendLine("📊 算法对比报告");
                sb.AppendLine("═".PadRight(40, '═'));
                sb.AppendLine();
                sb.AppendLine($"推荐算法: {comparison.RecommendedAlgorithm}");
                sb.AppendLine($"推荐理由: {comparison.Reason}");
                sb.AppendLine();

                foreach (var kvp in comparison.AlgorithmStats)
                {
                    var stats = kvp.Value;
                    sb.AppendLine($"【{stats.AlgorithmType}】");
                    sb.AppendLine($"  总复习次数: {stats.TotalReviews}");
                    sb.AppendLine($"  正确次数: {stats.CorrectReviews}");
                    sb.AppendLine($"  正确率: {stats.AccuracyRate:F1}%");
                    sb.AppendLine($"  平均间隔: {stats.AverageInterval:F1}天");
                    sb.AppendLine($"  一致性评分: {stats.ConsistencyScore:F1}");
                    sb.AppendLine();
                }

                MessageBox.Show(sb.ToString(), "算法对比", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "算法对比失败");
                MessageBox.Show($"算法对比失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRecommendAlgorithm_Click(object? sender, EventArgs e)
        {
            if (_spacedRepetitionService == null)
            {
                MessageBox.Show("算法服务不可用", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string recommended = _spacedRepetitionService.GetAdaptiveRecommendation(_userId);

                var result = MessageBox.Show(
                    $"根据您的学习数据，系统推荐使用【{recommended}】算法。\n\n是否切换到该算法？",
                    "算法推荐",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    _spacedRepetitionService.SetAlgorithm(recommended);
                    if (_cmbAlgorithm != null)
                    {
                        _cmbAlgorithm.SelectedItem = recommended;
                    }
                    MessageBox.Show($"已切换到 {recommended} 算法", "切换成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "获取算法推荐失败");
                MessageBox.Show($"获取推荐失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    var byCategory = wrongAnswers.GroupBy(w => w.Subject == SubjectType.Unknown ? "通用" : w.Subject.ToString())
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
            if (e == null || _chartTrend == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _chartTrend.ClientRectangle;
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
            if (e == null || _chartCategory == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _chartCategory.ClientRectangle;
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

        private void PanelForgettingCurve_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _chartForgettingCurve == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _chartForgettingCurve.ClientRectangle;
            int paddingLeft = 15;
            int paddingTop = 35;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            g.DrawString("📉 遗忘曲线与复习效率", titleFont, titleBrush, paddingLeft, 10);

            try
            {
                var curve = _cachedForgettingCurve;
                var stats = _cachedEfficiencyStats;

                if (curve != null && curve.Count > 0)
                {
                    int chartLeft = paddingLeft + 50;
                    int chartTop = paddingTop + 10;
                    int chartWidth = rect.Width - chartLeft - 30;
                    int chartHeight = rect.Height - chartTop - 30;

                    using var axisPen = new Pen(Color.FromArgb(200, 200, 200), 1);
                    using var curvePen = new Pen(Color.FromArgb(156, 39, 176), 2);
                    using var pointBrush = new SolidBrush(Color.FromArgb(156, 39, 176));
                    using var labelFont = new Font("微软雅黑", 8F);
                    using var labelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

                    g.DrawLine(axisPen, chartLeft, chartTop + chartHeight, chartLeft + chartWidth, chartTop + chartHeight);
                    g.DrawLine(axisPen, chartLeft, chartTop, chartLeft, chartTop + chartHeight);

                    int maxDays = curve.Keys.Max();
                    var points = new List<PointF>();
                    foreach (var kvp in curve.OrderBy(x => x.Key))
                    {
                        float x = chartLeft + (kvp.Key / (float)maxDays) * chartWidth;
                        float y = chartTop + chartHeight - (float)(kvp.Value * chartHeight);
                        points.Add(new PointF(x, y));
                    }

                    if (points.Count > 1)
                    {
                        using var curvePath = new System.Drawing.Drawing2D.GraphicsPath();
                        curvePath.AddLines(points.ToArray());
                        g.DrawPath(curvePen, curvePath);

                        foreach (var pt in points.Where((p, i) => i % 5 == 0))
                        {
                            g.FillEllipse(pointBrush, pt.X - 3, pt.Y - 3, 6, 6);
                        }
                    }

                    g.DrawString("0天", labelFont, labelBrush, chartLeft - 15, chartTop + chartHeight + 5);
                    g.DrawString($"{maxDays}天", labelFont, labelBrush, chartLeft + chartWidth - 20, chartTop + chartHeight + 5);
                    g.DrawString("100%", labelFont, labelBrush, chartLeft - 25, chartTop - 5);
                    g.DrawString("0%", labelFont, labelBrush, chartLeft - 15, chartTop + chartHeight);
                }

                if (stats != null)
                {
                    int infoX = rect.Width - 200;
                    int infoY = paddingTop;

                    using var statFont = new Font("微软雅黑", 9F);
                    using var labelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));
                    using var valueBrush = new SolidBrush(Color.FromArgb(33, 33, 33));

                    g.DrawString("复习统计:", statFont, labelBrush, infoX, infoY);
                    infoY += 20;

                    g.DrawString($"总复习: {stats.TotalReviews}次", statFont, valueBrush, infoX, infoY);
                    infoY += 18;
                    g.DrawString($"正确: {stats.TotalCorrect}次", statFont, valueBrush, infoX, infoY);
                    infoY += 18;
                    g.DrawString($"用时/题: {stats.ReviewTimePerCard:F1}秒", statFont, valueBrush, infoX, infoY);
                    infoY += 18;
                    g.DrawString($"使用算法: {stats.MostUsedAlgorithm}", statFont, valueBrush, infoX, infoY);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "绘制遗忘曲线面板失败");
            }
        }

        private void PanelHeatmap_Paint(object? sender, PaintEventArgs e)
        {
            if (e == null || _panelHeatmap == null) return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            var rect = _panelHeatmap.ClientRectangle;

            using var titleFont = new Font("微软雅黑", 10F, FontStyle.Bold);
            using var labelFont = new Font("微软雅黑", 8F);
            using var titleBrush = new SolidBrush(Color.FromArgb(33, 33, 33));
            using var labelBrush = new SolidBrush(Color.FromArgb(100, 100, 100));

            g.DrawString("📊 学习热力图（近12周）", titleFont, titleBrush, 15, 10);

            string[] dayLabels = { "周一", "", "周三", "", "周五", "", "周日" };
            for (int i = 0; i < dayLabels.Length; i++)
            {
                int y = 35 + i * 15;
                if (dayLabels[i] != "")
                {
                    g.DrawString(dayLabels[i], labelFont, labelBrush, 15, y);
                }
            }

            try
            {
                var heatmap = _cachedHeatmapData;
                if (heatmap != null && heatmap.Count > 0)
                {
                    int startX = 55;
                    int startY = 35;
                    int cellSize = 13;
                    int cellGap = 2;

                    Color[] levelColors = {
                        Color.FromArgb(235, 237, 240),
                        Color.FromArgb(155, 233, 168),
                        Color.FromArgb(64, 196, 99),
                        Color.FromArgb(48, 161, 78),
                        Color.FromArgb(31, 111, 56)
                    };

                    var groupedByWeek = heatmap.GroupBy(h => h.Week).OrderBy(g => g.Key).ToList();
                    int weekIndex = 0;

                    foreach (var week in groupedByWeek)
                    {
                        int x = startX + weekIndex * (cellSize + cellGap);

                        foreach (var day in week.OrderBy(d => d.DayOfWeek))
                        {
                            int y = startY + (day.DayOfWeek == 0 ? 6 : day.DayOfWeek - 1) * cellSize;
                            var cellRect = new Rectangle(x, y, cellSize, cellSize);

                            using var cellBrush = new SolidBrush(levelColors[day.Level]);
                            g.FillRectangle(cellBrush, cellRect);
                        }

                        if (weekIndex % 4 == 0)
                        {
                            int month = week.First().Date.Month;
                            g.DrawString($"{month}月", labelFont, labelBrush, x, startY - 15);
                        }

                        weekIndex++;
                    }

                    using var legendFont = new Font("微软雅黑", 8F);
                    int legendX = startX + groupedByWeek.Count * (cellSize + cellGap) + 20;
                    g.DrawString("少", legendFont, labelBrush, legendX, startY);
                    for (int i = 0; i < levelColors.Length; i++)
                    {
                        using var brush = new SolidBrush(levelColors[i]);
                        g.FillRectangle(brush, legendX + 20 + i * (cellSize + 2), startY, cellSize, cellSize);
                    }
                    g.DrawString("多", legendFont, labelBrush, legendX + 20 + levelColors.Length * (cellSize + 2) + 5, startY);
                }
                else
                {
                    g.DrawString("暂无学习数据", labelFont, labelBrush, 200, 80);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "绘制热力图面板失败");
            }
        }

        private string _heatmapTooltip = "";
        private Point _lastHeatmapMousePos;

        private void PanelHeatmap_MouseMove(object? sender, MouseEventArgs e)
        {
            if (_panelHeatmap == null || _cachedHeatmapData == null || _cachedHeatmapData.Count == 0)
                return;

            int startX = 55;
            int startY = 35;
            int cellSize = 13;
            int cellGap = 2;

            int col = (e.X - startX) / (cellSize + cellGap);
            int row = (e.Y - startY) / cellSize;

            if (col < 0 || row < 0 || row > 6) return;

            var groupedByWeek = _cachedHeatmapData
                .GroupBy(h => h.Week)
                .OrderBy(g => g.Key)
                .ToList();

            if (col >= groupedByWeek.Count) return;

            int dayOfWeek = row == 0 ? 1 : (row == 6 ? 0 : row + 1);
            var dayData = groupedByWeek[col].FirstOrDefault(d => d.DayOfWeek == dayOfWeek);

            if (dayData != null)
            {
                _heatmapTooltip = $"{dayData.Date:yyyy-MM-dd}: {dayData.Count}次复习";
                _lastHeatmapMousePos = e.Location;
                _panelHeatmap.Invalidate();
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

            this._cardRetention = new StatCard();
            this._cardRetention.Location = new Point(0, 0);
            this._cardRetention.Size = new Size(200, 110);
            this._cardRetention.Icon = "🧠";
            this._cardRetention.Value = "0%";
            this._cardRetention.Label = "记忆保留率";
            this._cardRetention.AccentColor = Color.FromArgb(156, 39, 176);
            this._cardRetention.CardColor = Color.White;

            this.panelCards.Controls.Add(this._cardMinutes);
            this.panelCards.Controls.Add(this._cardItems);
            this.panelCards.Controls.Add(this._cardAccuracy);
            this.panelCards.Controls.Add(this._cardStreak);
            this.panelCards.Controls.Add(this._cardRetention);

            // 趋势图表
            this._chartTrend = new LearningTrendChart();
            this._chartTrend.Location = new Point(15, 140);
            this._chartTrend.Size = new Size(855, 180);

            // 分类进度图表
            this._chartCategory = new CategoryProgressChart();
            this._chartCategory.Location = new Point(15, 335);
            this._chartCategory.Size = new Size(520, 200);

            // 错题统计面板
            this._panelWrongStats = new Panel();
            this._panelWrongStats.Location = new Point(550, 335);
            this._panelWrongStats.Size = new Size(320, 200);
            this._panelWrongStats.BackColor = Color.White;
            this._panelWrongStats.Paint += new PaintEventHandler(this.PanelWrongStats_Paint);

            // 遗忘曲线图
            this._chartForgettingCurve = new ForgettingCurveChart();
            this._chartForgettingCurve.Location = new Point(15, 550);
            this._chartForgettingCurve.Size = new Size(520, 180);

            // 评分分布图
            this._chartRating = new ReviewDistributionChart();
            this._chartRating.Location = new Point(550, 550);
            this._chartRating.Size = new Size(320, 180);

            // 算法切换面板
            this._panelAlgorithm = new Panel();
            this._panelAlgorithm.Location = new Point(880, 15);
            this._panelAlgorithm.Size = new Size(300, 80);
            this._panelAlgorithm.BackColor = Color.White;

            Label lblAlgorithm = new Label();
            lblAlgorithm.Text = "🧠 学习算法";
            lblAlgorithm.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            lblAlgorithm.Location = new Point(10, 10);
            lblAlgorithm.AutoSize = true;

            this._cmbAlgorithm = new ComboBox();
            this._cmbAlgorithm.Location = new Point(10, 35);
            this._cmbAlgorithm.Size = new Size(140, 25);
            this._cmbAlgorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbAlgorithm.Items.AddRange(new object[] { "SM-2", "FSRS" });
            this._cmbAlgorithm.SelectedIndex = 0;
            this._cmbAlgorithm.SelectedIndexChanged += new EventHandler(this.CmbAlgorithm_SelectedIndexChanged);

            Button btnCompare = new Button();
            btnCompare.Text = "📊 对比";
            btnCompare.Location = new Point(160, 35);
            btnCompare.Size = new Size(60, 25);
            btnCompare.FlatStyle = FlatStyle.Flat;
            btnCompare.BackColor = Color.FromArgb(33, 150, 243);
            btnCompare.ForeColor = Color.White;
            btnCompare.Click += new EventHandler(this.BtnCompareAlgorithm_Click);

            Button btnRecommend = new Button();
            btnRecommend.Text = "✨ 推荐";
            btnRecommend.Location = new Point(225, 35);
            btnRecommend.Size = new Size(60, 25);
            btnRecommend.FlatStyle = FlatStyle.Flat;
            btnRecommend.BackColor = Color.FromArgb(76, 175, 80);
            btnRecommend.ForeColor = Color.White;
            btnRecommend.Click += new EventHandler(this.BtnRecommendAlgorithm_Click);

            this._panelAlgorithm.Controls.Add(lblAlgorithm);
            this._panelAlgorithm.Controls.Add(this._cmbAlgorithm);
            this._panelAlgorithm.Controls.Add(btnCompare);
            this._panelAlgorithm.Controls.Add(btnRecommend);

            // 热力图面板
            this._panelHeatmap = new Panel();
            this._panelHeatmap.Location = new Point(15, 740);
            this._panelHeatmap.Size = new Size(855, 160);
            this._panelHeatmap.BackColor = Color.White;
            this._panelHeatmap.Paint += new PaintEventHandler(this.PanelHeatmap_Paint);
            this._panelHeatmap.MouseMove += new MouseEventHandler(this.PanelHeatmap_MouseMove);

            // 日历面板
            this.panelCalendar = new Panel();
            this.panelCalendar.Location = new Point(15, 910);
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
            this.panelContent.Controls.Add(this._chartTrend);
            this.panelContent.Controls.Add(this._chartCategory);
            this.panelContent.Controls.Add(this._panelWrongStats);
            this.panelContent.Controls.Add(this._chartForgettingCurve);
            this.panelContent.Controls.Add(this._chartRating);
            this.panelContent.Controls.Add(this._panelAlgorithm);
            this.panelContent.Controls.Add(this._panelHeatmap);
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

            if (_chartTrend != null)
                _chartTrend.Width = width;

            if (_chartCategory != null)
            {
                _chartCategory.Width = (int)(width * 0.6) - 10;
            }

            if (_panelWrongStats != null)
            {
                _panelWrongStats.Width = (int)(width * 0.4) - 10;
                if (_chartCategory != null)
                    _panelWrongStats.Left = _chartCategory.Right + 20;
            }

            if (_chartForgettingCurve != null)
            {
                _chartForgettingCurve.Width = (int)(width * 0.6) - 10;
            }

            if (_chartRating != null)
            {
                _chartRating.Width = (int)(width * 0.4) - 10;
                if (_chartForgettingCurve != null)
                    _chartRating.Left = _chartForgettingCurve.Right + 20;
            }
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (_cardMinutes != null) _cardMinutes.CardColor = colors.Surface;
            if (_cardItems != null) _cardItems.CardColor = colors.Surface;
            if (_cardAccuracy != null) _cardAccuracy.CardColor = colors.Surface;
            if (_cardStreak != null) _cardStreak.CardColor = colors.Surface;

            if (_chartTrend != null)
                _chartTrend.BackColor = colors.Surface;
            if (_chartCategory != null)
                _chartCategory.BackColor = colors.Surface;
            if (_chartForgettingCurve != null)
                _chartForgettingCurve.BackColor = colors.Surface;
            if (_chartRating != null)
                _chartRating.BackColor = colors.Surface;
            if (_panelWrongStats != null)
                _panelWrongStats.BackColor = colors.Surface;

            if (_calendarView != null)
                _calendarView.BackColor = colors.Surface;
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

                if (_chartTrend != null)
                    _chartTrend.Paint -= PanelTrendChart_Paint;
                if (_chartCategory != null)
                    _chartCategory.Paint -= PanelCategoryProgress_Paint;
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
