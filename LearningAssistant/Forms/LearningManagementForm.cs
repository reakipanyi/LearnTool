using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Common.Themes;
using LearningAssistant.Forms.UserControls;
using LearningAssistant.Forms.UserControls.Cards;
using LearningAssistant.Forms.UserControls.Charts;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using Microsoft.Extensions.Logging;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Globalization;
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
        // 统一统计聚合服务（统计底座模块产出，集中读取聚合 DTO）
        private readonly ILearningStatsAggregator? _aggregator;
        // 事件总线（学习事件 → 实时刷新，04 方案 3.3）
        private readonly IEventBus? _eventBus;
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
        private StatCard? _cardGoal;

        // 04 数据中心：Tab 布局
        private TabControl? _tabMain;
        // 概览 Tab：行动建议面板（今日待复习 / 弱科提示 / 空态引导）
        private Panel? _panelAdvice;
        private Label? _lblAdvice;
        private Label? _lblAdviceHint;

        private LearningTrendChart? _chartTrend;
        private CategoryProgressChart? _chartCategory;
        private ForgettingCurveChart? _chartForgettingCurve;
        private ReviewDistributionChart? _chartRating;
        // 03 图表模块新增：周热力图 / 目标进度 / 记忆成熟度
        private WeeklyHeatmapChart? _chartHeatmap;
        private GoalProgressChart? _goalChart;
        private MemoryMaturityChart? _maturityChart;
        // 错题 Tab：复用 WrongAnswerStatsPanel（04 方案 3.1）
        private WrongAnswerStatsPanel? _wrongStatsPanel;
        private Panel? _panelAlgorithm;
        private GoalCalendarView? _calendarView;
        private ComboBox? _cmbAlgorithm;
        // 05 报告模块：报告 Tab
        private StructuredReportService? _structuredReportService;
        private LearningReportAIService? _reportAiService;
        private ComboBox? _cmbReportKind;
        private DateTimePicker? _dtpReportDate;
        private Label? _lblReportCurrent;
        private FlowLayoutPanel? _reportMetricPanel;
        private LearningTrendChart? _chartReportTrend;
        private FlowLayoutPanel? _reportCategoryPanel;
        private Label? _lblReportSuggestions;
        private RichTextBox? _reportSuggestionsBox;
        private Label? _lblReportAi;
        private RichTextBox? _reportAiBox;

        // 04 数据中心：多用户对比 Tab（维度可配置：时长/正确率/连击/经验，04 方案 3.4）
        private UserComparisonChart? _chartComparison;
        private ComboBox? _cmbComparisonMetric;
        private ComboBox? _cmbComparisonPeriod;

        private List<DailyStatistics>? _cachedTrendData;
        private Dictionary<string, int>? _cachedCategoryStats;
        private List<WrongAnswerItem>? _cachedWrongAnswers;
        private Dictionary<int, double>? _cachedForgettingCurve;
        private ReviewEfficiencyStats? _cachedEfficiencyStats;

        // 学习事件订阅句柄（Dispose 反订阅需持同一引用）
        private Action<ItemLearnedEvent>? _onItemLearned;
        private Action<ItemWrongEvent>? _onItemWrong;
        private Action<ReviewDoneEvent>? _onReviewDone;
        private Action<PomodoroCompletedEvent>? _onPomodoro;
        private Action<LearningSessionCompletedEvent>? _onSessionCompleted;

        public LearningManagementForm(
            ILearningAnalyticsService analyticsService,
            ILearningReminderService reminderService,
            LearningReportService reportService,
            QuoteService quoteService,
            ILearningGoalService goalService,
            IWrongAnswerService wrongAnswerService,
            ISpacedRepetitionService? spacedRepetitionService = null,
            ILearningStatsAggregator? aggregator = null,
            IEventBus? eventBus = null,
            ILogger<LearningManagementForm>? logger = null,
            IThemeService? themeService = null,
            IUserSessionService? userSessionService = null,
            string? userId = null,
            LearningReportAIService? reportAiService = null)
        {
            InitializeComponent();
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
            _reminderService = reminderService ?? throw new ArgumentNullException(nameof(reminderService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _quoteService = quoteService ?? throw new ArgumentNullException(nameof(quoteService));
            _goalService = goalService ?? throw new ArgumentNullException(nameof(goalService));
            _wrongAnswerService = wrongAnswerService ?? throw new ArgumentNullException(nameof(wrongAnswerService));
            _spacedRepetitionService = spacedRepetitionService;
            _aggregator = aggregator;
            _eventBus = eventBus;
            _logger = logger;
            _themeService = themeService;
            _userSessionService = userSessionService;
            _userId = userId ?? userSessionService?.CurrentUserId ?? Environment.UserName;
            _reportAiService = reportAiService;

            // 05 报告模块：结构化报告仅依赖统计底座聚合 DTO
            if (_aggregator != null)
            {
                _structuredReportService = new StructuredReportService(_aggregator, _logger);
            }

            _themeService?.RegisterThemeable(this);
            SubscribeLearningEvents();

            _logger?.LogInformation("学习数据中心初始化，用户ID: {UserId}", _userId);
            LoadStatsData();
            // 报告 Tab 默认加载（若聚合服务可用）
            if (_structuredReportService != null)
            {
                try { LoadReport(); } catch (Exception ex) { _logger?.LogWarning(ex, "初始加载报告失败"); }
            }
        }

        /// <summary>
        /// 订阅学习事件，实现统计中心实时刷新（04 方案 3.3）。
        /// 学习/复习/番茄钟/会话完成后自动刷新当前数据，配合 02 底座缓存失效。
        /// </summary>
        private void SubscribeLearningEvents()
        {
            if (_eventBus == null) return;

            _onItemLearned = _ => RefreshOnLearningEvent();
            _onItemWrong = _ => RefreshOnLearningEvent();
            _onReviewDone = _ => RefreshOnLearningEvent();
            _onPomodoro = _ => RefreshOnLearningEvent();
            _onSessionCompleted = _ => RefreshOnLearningEvent();

            _eventBus.Subscribe<ItemLearnedEvent>(_onItemLearned);
            _eventBus.Subscribe<ItemWrongEvent>(_onItemWrong);
            _eventBus.Subscribe<ReviewDoneEvent>(_onReviewDone);
            _eventBus.Subscribe<PomodoroCompletedEvent>(_onPomodoro);
            _eventBus.Subscribe<LearningSessionCompletedEvent>(_onSessionCompleted);
        }

        private void UnsubscribeLearningEvents()
        {
            if (_eventBus == null) return;

            if (_onItemLearned != null) _eventBus.Unsubscribe<ItemLearnedEvent>(_onItemLearned);
            if (_onItemWrong != null) _eventBus.Unsubscribe<ItemWrongEvent>(_onItemWrong);
            if (_onReviewDone != null) _eventBus.Unsubscribe<ReviewDoneEvent>(_onReviewDone);
            if (_onPomodoro != null) _eventBus.Unsubscribe<PomodoroCompletedEvent>(_onPomodoro);
            if (_onSessionCompleted != null) _eventBus.Unsubscribe<LearningSessionCompletedEvent>(_onSessionCompleted);
        }

        private void RefreshOnLearningEvent()
        {
            try
            {
                if (IsDisposed || !IsHandleCreated) return;
                // 事件可能来自后台线程，编组到 UI 线程刷新
                if (InvokeRequired)
                {
                    BeginInvoke(new Action(LoadStatsData));
                }
                else
                {
                    LoadStatsData();
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "刷新学习数据中心失败");
            }
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
                LoadGoalChart();
                LoadMemoryMaturity();
                LoadAdvice();
                LoadCalendar();
                LoadComparisonData();
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
            RefreshAggregatorDeltas();

            var stats = _analyticsService.GetLearningTrend(_userId, startDate, endDate);
            var todayStats = _analyticsService.GetDailyStatistics(_userId, DateTime.Today);
            var yesterdayStats = _analyticsService.GetDailyStatistics(_userId, DateTime.Today.AddDays(-1));
            int streak = _analyticsService.GetStudyStreak(_userId);

            int totalMinutes = stats.Sum(s => s.TotalMinutes);
            int totalItems = stats.Sum(s => s.TotalItems);
            double avgAccuracy = stats.Count > 0 ? stats.Average(s => s.CorrectRate) : 0;

            // 环比增量优先取统计底座 WeeklyOverview.Delta（04 概览 3.4）
            int minutesChange;
            int itemsChange;
            double accuracyChange;
            if (_aggregator != null)
            {
                minutesChange = _aggregatorDeltaMinutes;
                itemsChange = _aggregatorDeltaItems;
                accuracyChange = _aggregatorDeltaAccuracy;
            }
            else
            {
                minutesChange = todayStats.TotalMinutes - yesterdayStats.TotalMinutes;
                itemsChange = todayStats.TotalItems - yesterdayStats.TotalItems;
                accuracyChange = todayStats.CorrectRate - yesterdayStats.CorrectRate;
            }

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

            // 记忆保留率：优先统计底座 MemoryInsights
            double retentionRate;
            if (_aggregator != null)
            {
                retentionRate = _aggregator.GetMemoryInsights(_userId).RetentionRate;
            }
            else
            {
                retentionRate = _analyticsService.CalculateRetentionRate(_userId);
            }

            if (_cardRetention != null)
            {
                _cardRetention.Value = $"{retentionRate * 100:F0}%";
                _cardRetention.Trend = retentionRate >= 0.9 ? "掌握良好" : (retentionRate >= 0.7 ? "正常范围" : "建议复习");
                _cardRetention.TrendDir = retentionRate >= 0.7 ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down;
            }

            // 今日目标达成卡片（04 概览新增）
            if (_cardGoal != null)
            {
                var overview = _aggregator?.GetDailyOverview(_userId, DateTime.Today) ?? new DailyOverview { Date = DateTime.Today };
                var goal = _goalService.GetDailyGoal(_userId);
                if (goal == null || goal.TargetItems <= 0)
                {
                    _cardGoal.Value = "--";
                    _cardGoal.Trend = "未设目标";
                    _cardGoal.TrendDir = StatCard.TrendDirection.None;
                }
                else
                {
                    _cardGoal.Value = $"{Math.Min(overview.ItemsStudied, goal.TargetItems)}/{goal.TargetItems}";
                    _cardGoal.Trend = overview.GoalCompleted ? "目标达成 🎉" : $"还需 {Math.Max(0, goal.TargetItems - overview.ItemsStudied)} 项";
                    _cardGoal.TrendDir = overview.GoalCompleted ? StatCard.TrendDirection.Up : StatCard.TrendDirection.None;
                }
            }
        }

        private int _aggregatorDeltaMinutes;
        private int _aggregatorDeltaItems;
        private double _aggregatorDeltaAccuracy;

        /// <summary>
        /// 缓存统计底座周环比增量（LoadStatCards 内读取），避免重复计算。
        /// </summary>
        private void RefreshAggregatorDeltas()
        {
            _aggregatorDeltaMinutes = 0;
            _aggregatorDeltaItems = 0;
            _aggregatorDeltaAccuracy = 0;
            if (_aggregator == null) return;

            try
            {
                var week = _aggregator.GetWeeklyOverview(_userId, DateTime.Today);
                _aggregatorDeltaMinutes = week.TimeSpentDeltaMinutes;
                _aggregatorDeltaItems = week.ItemsStudiedDelta;
                _aggregatorDeltaAccuracy = week.AccuracyDelta;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "读取统计底座周环比增量失败");
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

            // 优先统计底座聚合 DTO（TrendSeries），保证口径与 02 底座一致
            if (_aggregator != null && _chartTrend != null)
            {
                var studySeries = _aggregator.GetTrend(_userId, startDate, endDate, TrendSeriesType.Trend);
                var accuracySeries = _aggregator.GetTrend(_userId, startDate, endDate, TrendSeriesType.Accuracy);
                _chartTrend.UpdateData(studySeries, accuracySeries);
                return;
            }

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
            // 优先统计底座分类维度 DTO，口径一致
            if (_aggregator != null && _chartCategory != null)
            {
                DateTime s, e;
                GetDateRange(out s, out e);
                var breakdown = _aggregator.GetCategoryBreakdown(_userId, s, e);
                var categories = breakdown.OrderByDescending(b => b.ItemsStudied).Take(6)
                    .Select(b => b.Category).ToList();
                var progress = breakdown.OrderByDescending(b => b.ItemsStudied).Take(6)
                    .Select(b => (double)b.ItemsStudied).ToList();
                _chartCategory.UpdateData(categories, progress);
                return;
            }

            _cachedCategoryStats = _analyticsService.GetCategoryStats(_userId);

            if (_chartCategory != null && _cachedCategoryStats != null)
            {
                var categories = _cachedCategoryStats.Keys.Take(6).ToList();
                var progress = _cachedCategoryStats.Values.Take(6).Select(v => (double)v).ToList();
                _chartCategory.UpdateData(categories, progress);
            }
        }

        /// <summary>
        /// 概览 Tab 行动建议面板（04 方案 3.4）：今日待复习 N 项 / 弱科提示 / 空态引导。
        /// 把统计转成“下一步行动”，数据来自统计底座 MemoryInsights + WeeklyOverview。
        /// </summary>
        private void LoadAdvice()
        {
            if (string.IsNullOrEmpty(_userId) || _panelAdvice == null) return;

            try
            {
                var advice = new List<string>();
                if (_aggregator != null)
                {
                    var memory = _aggregator.GetMemoryInsights(_userId);
                    if (memory.DueToday > 0)
                    {
                        advice.Add($"📌 今日待复习 {memory.DueToday} 项 — 及时复习能显著提升记忆保留率");
                    }

                    var week = _aggregator.GetWeeklyOverview(_userId, DateTime.Today);
                    if (!string.IsNullOrEmpty(week.WeakCategory))
                    {
                        advice.Add($"📉 弱项科目：{week.WeakCategory} — 建议优先安排学习");
                    }

                    if (memory.TotalItems == 0 && advice.Count == 0)
                    {
                        advice.Add("🚀 还没有学习数据 — 去【学习】页开始第一次学习吧，或到【内容编辑】导入词库");
                    }
                }

                if (advice.Count == 0)
                {
                    advice.Add("✅ 状态不错，继续保持当前的学习节奏！");
                }

                _lblAdvice.Text = "💡 行动建议";
                _lblAdviceHint.Text = string.Join("\n", advice);
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "加载行动建议失败");
            }
        }

        private void LoadWrongAnswerStats()
        {
            _cachedWrongAnswers = _wrongAnswerService.GetWrongAnswers(_userId);
            if (_wrongStatsPanel != null)
            {
                _wrongStatsPanel.WrongAnswerService = _wrongAnswerService;
                _wrongStatsPanel.CurrentUserId = _userId;
                _wrongStatsPanel.RefreshStats();
            }
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

        /// <summary>
        /// 对比 Tab：指标维度 / 周期切换时实时刷新（04 方案 3.4）。
        /// </summary>
        private void CmbComparison_Changed(object? sender, EventArgs e)
        {
            LoadComparisonData();
        }

        /// <summary>
        /// 加载多用户对比数据：从统一统计底座聚合各用户选定周期/维度的指标，
        /// 注入 <see cref="UserComparisonChart"/> 柱状图展示。
        /// </summary>
        private void LoadComparisonData()
        {
            try
            {
                if (_chartComparison == null || _aggregator == null)
                {
                    // 统计底座未就绪时展示空态
                    _chartComparison?.UpdateData(new List<string>(), new List<double>(), "指标值");
                    return;
                }

                var userIds = _userSessionService?.GetUserList() ?? new List<string>();
                // 图表横轴使用用户昵称，值仍按用户 ID 拉取，避免重复用户列表扩容时 key 错位
                var users = userIds.Select(ResolveUserDisplayName).ToList();
                if (userIds.Count < 2)
                {
                    // UpdateData 内部对不足两人的情况展示“至少需要两位用户”空态
                    _chartComparison.UpdateData(users, new List<double>(), "指标值");
                    return;
                }

                var metricIndex = _cmbComparisonMetric?.SelectedIndex ?? 0;
                var periodIndex = _cmbComparisonPeriod?.SelectedIndex ?? 1;
                var metricLabel = _cmbComparisonMetric?.SelectedItem as string ?? "指标值";

                // 按周期拉取对应聚合 DTO，各指标取值方式一致
                var values = new List<double>();
                foreach (var userId in userIds)
                {
                    values.Add(ResolveComparisonMetric(userId, metricIndex, periodIndex));
                }

                _chartComparison.UpdateData(users, values, metricLabel);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载多用户对比数据失败");
            }
        }

        /// <summary>
        /// 解析单个用户在指定周期下的对比指标值（时长/正确率/连击/经验）。
        /// </summary>
        private double ResolveComparisonMetric(string userId, int metricIndex, int periodIndex)
        {
            switch (periodIndex)
            {
                case 0:
                    var daily = _aggregator.GetDailyOverview(userId, DateTime.Today);
                    return PickMetric(metricIndex, daily.TimeSpentMinutes, daily.Accuracy, daily.StreakDays, daily.XP);
                case 2:
                    var monthly = _aggregator.GetMonthlyOverview(userId, DateTime.Today);
                    return PickMetric(metricIndex, monthly.TimeSpentMinutes, monthly.Accuracy, monthly.StreakDays, monthly.XP);
                default:
                    var weekly = _aggregator.GetWeeklyOverview(userId, DateTime.Today);
                    return PickMetric(metricIndex, weekly.TimeSpentMinutes, weekly.Accuracy, weekly.StreakDays, weekly.XP);
            }
        }

        private static double PickMetric(int metricIndex, int minutes, double accuracy, int streakDays, int xp)
            => metricIndex switch
            {
                0 => minutes,
                1 => Math.Round(accuracy, 2),
                2 => streakDays,
                3 => xp,
                _ => 0
            };

        /// <summary>
        /// 解析用户展示名：优先取昵称（UserName），缺失时回退为用户 ID。
        /// </summary>
        private string ResolveUserDisplayName(string userId)
        {
            try
            {
                var name = _userSessionService?.LoadUserProfile(userId)?.UserName;
                return string.IsNullOrWhiteSpace(name) ? userId : name;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "解析用户昵称失败，回退为用户 ID：{UserId}", userId);
                return userId;
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
                if (_chartHeatmap == null || _aggregator == null) return;

                // 从统计底座取近 6+1 周学习量趋势，绘制周热力图（03 方案 5.2）
                var series = _aggregator.GetTrend(_userId, DateTime.Today.AddDays(-42), DateTime.Today, TrendSeriesType.Trend);
                _chartHeatmap.UpdateData(series, DateTime.Today);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载热力图数据失败");
            }
        }

        /// <summary>
        /// 今日目标进度图（03 方案 5.1）：统计底座每日概览 vs 每日目标。
        /// </summary>
        private void LoadGoalChart()
        {
            try
            {
                if (_goalChart == null || _aggregator == null) return;

                var overview = _aggregator.GetDailyOverview(_userId, DateTime.Today);
                var goal = _goalService.GetDailyGoal(_userId);
                _goalChart.UpdateData(overview, goal?.TargetItems ?? 0);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载目标进度图失败");
            }
        }

        /// <summary>
        /// 记忆成熟度分布图（03 方案 5.3）：统计底座 <see cref="MemoryInsights"/>。
        /// </summary>
        private void LoadMemoryMaturity()
        {
            try
            {
                if (_maturityChart == null || _aggregator == null) return;

                _maturityChart.UpdateData(_aggregator.GetMemoryInsights(_userId));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "加载记忆成熟度图失败");
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

        // ============ 报告 Tab（05 方案 3.1/3.2/3.3） ============

        private StructuredReport? GetCurrentReport()
        {
            if (_structuredReportService == null || _cmbReportKind == null || _dtpReportDate == null)
            {
                MessageBox.Show("聚合统计服务未配置，无法生成报告", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            int kind = _cmbReportKind.SelectedIndex;
            var date = _dtpReportDate.Value;
            try
            {
                return kind switch
                {
                    1 => _structuredReportService.BuildWeekly(_userId, date.Year, ISOWeek.GetWeekOfYear(date)),
                    2 => _structuredReportService.BuildMonthly(_userId, date.Year, date.Month),
                    _ => _structuredReportService.BuildDaily(_userId, date.Date)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "生成结构化报告失败");
                MessageBox.Show($"生成报告失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        private void BtnReportLoad_Click(object? sender, EventArgs e)
        {
            var report = GetCurrentReport();
            if (report == null) return;
            LoadReport(report);
        }

        private void LoadReport(StructuredReport report)
        {
            if (_structuredReportService == null || _chartReportTrend == null || _reportMetricPanel == null
                || _reportCategoryPanel == null || _reportSuggestionsBox == null || _reportAiBox == null)
                return;

            string weak = string.IsNullOrEmpty(report.WeakCategory) ? "" : $"  弱项：{report.WeakCategory}";
            string eff = report.EfficiencyScore > 0 ? $"  综合效率分 {report.EfficiencyScore:F0}" : "";
            if (_lblReportCurrent != null)
            {
                _lblReportCurrent.Text =
                    $"{report.Title} · {report.PeriodLabel}（{report.StartDate:yyyy-MM-dd} ~ {report.EndDate:yyyy-MM-dd}）" +
                    $"  目标达成：{(report.GoalCompleted ? "✅ 是" : "❌ 否")}{weak}{eff}";
            }

            // 指标卡
            _reportMetricPanel.Controls.Clear();
            _reportMetricPanel.Controls.Add(MakeReportCard("⏱️", "学习时长", FormatMinutes(report.TimeSpentMinutes),
                report.Kind == ReportPeriodKind.Daily ? "" : DeltaLabel(report.TimeSpentDeltaMinutes, "分钟")));
            _reportMetricPanel.Controls.Add(MakeReportCard("📚", "学习项数", $"{report.ItemsStudied}",
                report.Kind == ReportPeriodKind.Daily ? "" : DeltaLabel(report.ItemsStudiedDelta, "项")));
            _reportMetricPanel.Controls.Add(MakeReportCard("✅", "正确率", $"{report.Accuracy:F1}%",
                report.Kind == ReportPeriodKind.Daily ? "" : $"{report.AccuracyDelta:+0.0;-0.0;0}%"));
            _reportMetricPanel.Controls.Add(MakeReportCard("🔥", "连续学习", $"{report.StreakDays} 天", ""));
            _reportMetricPanel.Controls.Add(MakeReportCard("⭐", "等级", $"Lv.{report.Level}", $"XP {report.XP}"));
            _reportMetricPanel.Controls.Add(MakeReportCard("🎯", "目标", report.GoalCompleted ? "已达成" : "未达成",
                report.EfficiencyScore > 0 ? $"效率 {report.EfficiencyScore:F0}" : ""));

            // 趋势图（聚合 TrendSeries）
            _chartReportTrend.UpdateData(new TrendSeries { Points = report.Trend });

            // 分类分布
            _reportCategoryPanel.Controls.Clear();
            if (report.Categories.Count == 0)
            {
                var empty = new Label { Text = "暂无分类数据", AutoSize = true, Padding = new Padding(4), ForeColor = Color.Gray };
                _reportCategoryPanel.Controls.Add(empty);
            }
            else
            {
                foreach (var c in report.Categories.OrderByDescending(c => c.TimeSpentMinutes).Take(8))
                {
                    _reportCategoryPanel.Controls.Add(new Label
                    {
                        Text = $"{c.Category}: {FormatMinutes(c.TimeSpentMinutes)}（{c.ItemsStudied}项/正确率{c.Accuracy:F0}%）",
                        AutoSize = true,
                        Padding = new Padding(4),
                        Margin = new Padding(0, 0, 10, 0),
                        BackColor = Color.White
                    });
                }
            }

            // 建议（规则文案，AI 不可用时即回退此文案）
            _reportSuggestionsBox.Text = string.Join("\n", report.Suggestions);

            // AI 总结（初始为空，点击按钮生成）
            _reportAiBox.Text = report.AiSummary ?? "点击【🤖 AI 总结】获取自然语言总结（依赖网络；不可用时将展示规则建议）。";
        }

        private void LoadReport()
        {
            if (_structuredReportService == null || _cmbReportKind == null || _dtpReportDate == null) return;

            var report = GetCurrentReport();
            if (report != null) LoadReport(report);
        }

        private async Task<bool> LoadAiSummaryAsync(StructuredReport report)
        {
            if (_reportAiService == null || _reportAiBox == null)
            {
                _reportAiBox.Text = "AI 服务未配置，已使用规则建议回退。";
                return false;
            }

            try
            {
                _reportAiBox.Text = "正在生成 AI 总结…";
                string? summary = await _reportAiService.GenerateSummaryAsync(_userId, report);
                if (!string.IsNullOrEmpty(summary))
                {
                    report.AiSummary = summary;
                    _reportAiBox.Text = summary;
                    return true;
                }

                _reportAiBox.Text = "AI 总结不可用（无网络或失败），已回退到上面的规则建议。";
                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "生成 AI 报告总结失败");
                _reportAiBox.Text = "AI 总结生成失败，已回退到上面的规则建议。";
                return false;
            }
        }

        private async void BtnAiReport_Click(object? sender, EventArgs e)
        {
            var report = GetCurrentReport();
            if (report == null) return;
            await LoadAiSummaryAsync(report);
        }

        private void BtnExportReport_Click(object? sender, EventArgs e)
        {
            if (_structuredReportService == null) return;
            var report = GetCurrentReport();
            if (report == null) return;

            var button = sender as Button;
            string tag = button?.Text ?? "导出 Markdown";

            // Excel：EPPlus 二进制导出
            if (tag.Contains("Excel"))
            {
                using var xlsxDialog = new SaveFileDialog
                {
                    Filter = "Excel工作簿|*.xlsx",
                    FileName = $"学习报告_{report.PeriodLabel.Replace(" ", "")}.xlsx",
                    DefaultExt = "xlsx"
                };
                if (xlsxDialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    _structuredReportService.ExportExcel(report, xlsxDialog.FileName);
                    MessageBox.Show("报告导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "导出 Excel 报告失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            // PDF：SkiaSharp 生成
            if (tag.Contains("PDF"))
            {
                using var pdfDialog = new SaveFileDialog
                {
                    Filter = "PDF文件|*.pdf",
                    FileName = $"学习报告_{report.PeriodLabel.Replace(" ", "")}.pdf",
                    DefaultExt = "pdf"
                };
                if (pdfDialog.ShowDialog() != DialogResult.OK) return;
                try
                {
                    using var fs = new FileStream(pdfDialog.FileName, FileMode.Create, FileAccess.Write);
                    _structuredReportService.ExportPdf(report, fs);
                    MessageBox.Show("报告导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "导出 PDF 报告失败");
                    MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            string content;
            string filter;
            string defaultName;

            if (tag.Contains("HTML"))
            {
                content = _structuredReportService.ExportHtml(report);
                filter = "HTML文件|*.html";
                defaultName = $"学习报告_{report.PeriodLabel.Replace(" ", "")}.html";
            }
            else if (tag.Contains("TXT"))
            {
                content = _structuredReportService.ExportText(report);
                filter = "文本文件|*.txt";
                defaultName = $"学习报告_{report.PeriodLabel.Replace(" ", "")}.txt";
            }
            else
            {
                content = _structuredReportService.ExportMarkdown(report);
                filter = "Markdown文件|*.md";
                defaultName = $"学习报告_{report.PeriodLabel.Replace(" ", "")}.md";
            }

            using var dialog = new SaveFileDialog { Filter = filter, FileName = defaultName, DefaultExt = "*" };
            if (dialog.ShowDialog() != DialogResult.OK) return;

            try
            {
                File.WriteAllText(dialog.FileName, content);
                MessageBox.Show("报告导出成功！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "导出结构化报告失败");
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static StatCard MakeReportCard(string icon, string label, string value, string trend)
        {
            return new StatCard
            {
                Icon = icon,
                Label = label,
                Value = value,
                Trend = trend,
                TrendDir = string.IsNullOrEmpty(trend) ? StatCard.TrendDirection.None
                    : (trend.StartsWith('+') ? StatCard.TrendDirection.Up : StatCard.TrendDirection.Down),
                AccentColor = Color.FromArgb(63, 81, 181),
                TextColor = Color.FromArgb(33, 33, 33),
                Size = new Size(132, 100)
            };
        }

        private static string DeltaLabel(int delta, string unit)
        {
            if (delta == 0) return "持平";
            return delta > 0 ? $"+{delta} {unit}" : $"{delta} {unit}";
        }

        private static string FormatMinutes(int minutes)
            => minutes >= 60 ? $"{minutes / 60.0:F1}小时" : $"{minutes}分钟";

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

        // 统计卡片区域
        private FlowLayoutPanel panelCards;

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

            #region 2. 学习数据中心 Tab 布局（04 方案 3.1）
            // 统一学习数据中心：Tab 化组织 概览/记忆/目标/错题
            this._tabMain = new TabControl();
            this._tabMain.Dock = DockStyle.Fill;
            this._tabMain.BackColor = Color.FromArgb(245, 245, 250);
            this._tabMain.Padding = new Point(20, 6);

            // 概览 Tab
            var tabOverview = new TabPage("📊 概览");
            tabOverview.AutoScroll = true;
            tabOverview.BackColor = Color.FromArgb(245, 245, 250);
            tabOverview.Padding = new Padding(10);

            // 统计卡片容器（FlowLayout 自动换行，适配 6 张卡片）
            this.panelCards = new FlowLayoutPanel();
            this.panelCards.Location = new Point(15, 15);
            this.panelCards.Size = new Size(855, 240);
            this.panelCards.BackColor = Color.Transparent;
            this.panelCards.WrapContents = true;

            // 自定义统计卡片 StatCard（FlowLayout 内由 Margin 排版）
            this._cardMinutes = new StatCard();
            this._cardMinutes.Size = new Size(200, 110);
            this._cardMinutes.Margin = new Padding(0, 0, 15, 15);
            this._cardMinutes.Icon = "⏱️";
            this._cardMinutes.Value = "0分";
            this._cardMinutes.Label = "学习时长";
            this._cardMinutes.AccentColor = Color.FromArgb(33, 150, 243);
            this._cardMinutes.CardColor = Color.White;

            this._cardItems = new StatCard();
            this._cardItems.Size = new Size(200, 110);
            this._cardItems.Margin = new Padding(0, 0, 15, 15);
            this._cardItems.Icon = "📚";
            this._cardItems.Value = "0个";
            this._cardItems.Label = "已学词汇";
            this._cardItems.AccentColor = Color.FromArgb(76, 175, 80);
            this._cardItems.CardColor = Color.White;

            this._cardAccuracy = new StatCard();
            this._cardAccuracy.Size = new Size(200, 110);
            this._cardAccuracy.Margin = new Padding(0, 0, 15, 15);
            this._cardAccuracy.Icon = "🎯";
            this._cardAccuracy.Value = "0%";
            this._cardAccuracy.Label = "正确率";
            this._cardAccuracy.AccentColor = Color.FromArgb(255, 152, 0);
            this._cardAccuracy.CardColor = Color.White;

            this._cardStreak = new StatCard();
            this._cardStreak.Size = new Size(200, 110);
            this._cardStreak.Margin = new Padding(0, 0, 15, 15);
            this._cardStreak.Icon = "🔥";
            this._cardStreak.Value = "0天";
            this._cardStreak.Label = "连续天数";
            this._cardStreak.AccentColor = Color.FromArgb(244, 67, 54);
            this._cardStreak.CardColor = Color.White;

            this._cardRetention = new StatCard();
            this._cardRetention.Size = new Size(200, 110);
            this._cardRetention.Margin = new Padding(0, 0, 15, 15);
            this._cardRetention.Icon = "🧠";
            this._cardRetention.Value = "0%";
            this._cardRetention.Label = "记忆保留率";
            this._cardRetention.AccentColor = Color.FromArgb(156, 39, 176);
            this._cardRetention.CardColor = Color.White;

            // 今日目标达成卡片（04 概览新增）
            this._cardGoal = new StatCard();
            this._cardGoal.Size = new Size(200, 110);
            this._cardGoal.Margin = new Padding(0, 0, 15, 15);
            this._cardGoal.Icon = "🎯";
            this._cardGoal.Value = "--";
            this._cardGoal.Label = "今日目标";
            this._cardGoal.AccentColor = Color.FromArgb(0, 188, 212);
            this._cardGoal.CardColor = Color.White;

            this.panelCards.Controls.Add(this._cardMinutes);
            this.panelCards.Controls.Add(this._cardItems);
            this.panelCards.Controls.Add(this._cardAccuracy);
            this.panelCards.Controls.Add(this._cardStreak);
            this.panelCards.Controls.Add(this._cardRetention);
            this.panelCards.Controls.Add(this._cardGoal);
            tabOverview.Controls.Add(this.panelCards);

            // 趋势图表（统计底座 TrendSeries）
            this._chartTrend = new LearningTrendChart();
            this._chartTrend.Dock = DockStyle.None;
            this._chartTrend.Location = new Point(15, 270);
            this._chartTrend.Size = new Size(855, 190);
            tabOverview.Controls.Add(this._chartTrend);

            // 分类进度图
            this._chartCategory = new CategoryProgressChart();
            this._chartCategory.Dock = DockStyle.None;
            this._chartCategory.Location = new Point(15, 475);
            this._chartCategory.Size = new Size(520, 190);
            tabOverview.Controls.Add(this._chartCategory);

            // 行动建议面板（04 方案 3.4）
            this._panelAdvice = new Panel();
            this._panelAdvice.Location = new Point(550, 475);
            this._panelAdvice.Size = new Size(320, 190);
            this._panelAdvice.BackColor = Color.White;

            this._lblAdvice = new Label();
            this._lblAdvice.Text = "💡 行动建议";
            this._lblAdvice.Font = new Font("微软雅黑", 10F, FontStyle.Bold);
            this._lblAdvice.ForeColor = Color.FromArgb(50, 50, 50);
            this._lblAdvice.Location = new Point(12, 10);
            this._lblAdvice.AutoSize = true;
            this._panelAdvice.Controls.Add(this._lblAdvice);

            this._lblAdviceHint = new Label();
            this._lblAdviceHint.Text = "";
            this._lblAdviceHint.Font = new Font("微软雅黑", 9F);
            this._lblAdviceHint.ForeColor = Color.FromArgb(100, 100, 100);
            this._lblAdviceHint.Location = new Point(12, 38);
            this._lblAdviceHint.Size = new Size(290, 140);
            this._panelAdvice.Controls.Add(this._lblAdviceHint);
            tabOverview.Controls.Add(this._panelAdvice);

            // ===== 记忆 Tab =====
            var tabMemory = new TabPage("🧠 记忆");
            tabMemory.AutoScroll = true;
            tabMemory.BackColor = Color.FromArgb(245, 245, 250);
            tabMemory.Padding = new Padding(10);

            // 遗忘曲线图
            this._chartForgettingCurve = new ForgettingCurveChart();
            this._chartForgettingCurve.Dock = DockStyle.None;
            this._chartForgettingCurve.Location = new Point(15, 15);
            this._chartForgettingCurve.Size = new Size(520, 180);
            tabMemory.Controls.Add(this._chartForgettingCurve);

            // 评分分布图
            this._chartRating = new ReviewDistributionChart();
            this._chartRating.Dock = DockStyle.None;
            this._chartRating.Location = new Point(550, 15);
            this._chartRating.Size = new Size(320, 180);
            tabMemory.Controls.Add(this._chartRating);

            // 记忆成熟度图
            this._maturityChart = new MemoryMaturityChart();
            this._maturityChart.Dock = DockStyle.None;
            this._maturityChart.Location = new Point(15, 210);
            this._maturityChart.Size = new Size(420, 180);
            tabMemory.Controls.Add(this._maturityChart);

            // 周热力图
            this._chartHeatmap = new WeeklyHeatmapChart();
            this._chartHeatmap.Dock = DockStyle.None;
            this._chartHeatmap.Location = new Point(450, 210);
            this._chartHeatmap.Size = new Size(420, 180);
            tabMemory.Controls.Add(this._chartHeatmap);

            // ===== 错题 Tab =====
            var tabWrong = new TabPage("📕 错题");
            tabWrong.AutoScroll = true;
            tabWrong.BackColor = Color.FromArgb(245, 245, 250);
            tabWrong.Padding = new Padding(10);

            this._wrongStatsPanel = new WrongAnswerStatsPanel();
            this._wrongStatsPanel.Dock = DockStyle.Fill;
            tabWrong.Controls.Add(this._wrongStatsPanel);

            // ===== 目标 Tab =====
            var tabGoal = new TabPage("🎯 目标");
            tabGoal.AutoScroll = true;
            tabGoal.BackColor = Color.FromArgb(245, 245, 250);
            tabGoal.Padding = new Padding(10);

            // 目标进度图
            this._goalChart = new GoalProgressChart();
            this._goalChart.Dock = DockStyle.None;
            this._goalChart.Location = new Point(15, 15);
            this._goalChart.Size = new Size(855, 150);
            tabGoal.Controls.Add(this._goalChart);

            // 算法切换面板
            this._panelAlgorithm = new Panel();
            this._panelAlgorithm.Location = new Point(15, 180);
            this._panelAlgorithm.Size = new Size(855, 60);
            this._panelAlgorithm.BackColor = Color.White;

            Label lblAlgorithm = new Label();
            lblAlgorithm.Text = "🧠 学习算法";
            lblAlgorithm.Font = new Font("微软雅黑", 9F, FontStyle.Bold);
            lblAlgorithm.Location = new Point(10, 16);
            lblAlgorithm.AutoSize = true;

            this._cmbAlgorithm = new ComboBox();
            this._cmbAlgorithm.Location = new Point(95, 16);
            this._cmbAlgorithm.Size = new Size(140, 25);
            this._cmbAlgorithm.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbAlgorithm.Items.AddRange(new object[] { "SM-2", "FSRS" });
            this._cmbAlgorithm.SelectedIndex = 0;
            this._cmbAlgorithm.SelectedIndexChanged += new EventHandler(this.CmbAlgorithm_SelectedIndexChanged);

            Button btnCompare = new Button();
            btnCompare.Text = "📊 对比";
            btnCompare.Location = new Point(250, 16);
            btnCompare.Size = new Size(60, 25);
            btnCompare.FlatStyle = FlatStyle.Flat;
            btnCompare.BackColor = Color.FromArgb(33, 150, 243);
            btnCompare.ForeColor = Color.White;
            btnCompare.Click += new EventHandler(this.BtnCompareAlgorithm_Click);

            Button btnRecommend = new Button();
            btnRecommend.Text = "✨ 推荐";
            btnRecommend.Location = new Point(315, 16);
            btnRecommend.Size = new Size(60, 25);
            btnRecommend.FlatStyle = FlatStyle.Flat;
            btnRecommend.BackColor = Color.FromArgb(76, 175, 80);
            btnRecommend.ForeColor = Color.White;
            btnRecommend.Click += new EventHandler(this.BtnRecommendAlgorithm_Click);

            this._panelAlgorithm.Controls.Add(lblAlgorithm);
            this._panelAlgorithm.Controls.Add(this._cmbAlgorithm);
            this._panelAlgorithm.Controls.Add(btnCompare);
            this._panelAlgorithm.Controls.Add(btnRecommend);
            tabGoal.Controls.Add(this._panelAlgorithm);

            // 打卡日历面板
            this.panelCalendar = new Panel();
            this.panelCalendar.Location = new Point(15, 255);
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
            tabGoal.Controls.Add(this.panelCalendar);

            // ===== 报告 Tab（05 方案 3.1/3.2/3.3）=====
            var tabReport = new TabPage("📊 报告");
            tabReport.AutoScroll = true;
            tabReport.BackColor = Color.FromArgb(245, 245, 250);
            tabReport.Padding = new Padding(10);

            this._cmbReportKind = new ComboBox();
            this._cmbReportKind.Items.AddRange(new object[] { "日报", "周报", "月报" });
            this._cmbReportKind.SelectedIndex = 0;
            this._cmbReportKind.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbReportKind.Location = new Point(15, 15);
            this._cmbReportKind.Size = new Size(80, 26);

            this._dtpReportDate = new DateTimePicker();
            this._dtpReportDate.Location = new Point(105, 15);
            this._dtpReportDate.Size = new Size(140, 26);
            this._dtpReportDate.Format = DateTimePickerFormat.Short;

            var btnReportLoad = new Button();
            btnReportLoad.Text = "生成报告";
            btnReportLoad.Location = new Point(255, 15);
            btnReportLoad.Size = new Size(80, 26);
            btnReportLoad.FlatStyle = FlatStyle.Flat;
            btnReportLoad.BackColor = Color.FromArgb(63, 81, 181);
            btnReportLoad.ForeColor = Color.White;
            btnReportLoad.Click += new EventHandler(this.BtnReportLoad_Click);

            var btnAiReport = new Button();
            btnAiReport.Text = "🤖 AI 总结";
            btnAiReport.Location = new Point(340, 15);
            btnAiReport.Size = new Size(90, 26);
            btnAiReport.FlatStyle = FlatStyle.Flat;
            btnAiReport.BackColor = Color.FromArgb(156, 39, 176);
            btnAiReport.ForeColor = Color.White;
            btnAiReport.Click += new EventHandler(this.BtnAiReport_Click);

            var btnExportMd = new Button();
            btnExportMd.Text = "导出 Markdown";
            btnExportMd.Location = new Point(440, 15);
            btnExportMd.Size = new Size(110, 26);
            btnExportMd.FlatStyle = FlatStyle.Flat;
            btnExportMd.BackColor = Color.FromArgb(76, 175, 80);
            btnExportMd.ForeColor = Color.White;
            btnExportMd.Click += new EventHandler(this.BtnExportReport_Click);

            var btnExportHtml = new Button();
            btnExportHtml.Text = "导出 HTML";
            btnExportHtml.Location = new Point(555, 15);
            btnExportHtml.Size = new Size(90, 26);
            btnExportHtml.FlatStyle = FlatStyle.Flat;
            btnExportHtml.BackColor = Color.FromArgb(33, 150, 243);
            btnExportHtml.ForeColor = Color.White;
            btnExportHtml.Click += new EventHandler(this.BtnExportReport_Click);

            var btnExportTxt = new Button();
            btnExportTxt.Text = "导出 TXT";
            btnExportTxt.Location = new Point(650, 15);
            btnExportTxt.Size = new Size(80, 26);
            btnExportTxt.FlatStyle = FlatStyle.Flat;
            btnExportTxt.BackColor = Color.FromArgb(255, 152, 0);
            btnExportTxt.ForeColor = Color.White;
            btnExportTxt.Click += new EventHandler(this.BtnExportReport_Click);

            var btnExportExcel = new Button();
            btnExportExcel.Text = "导出 Excel";
            btnExportExcel.Location = new Point(15, 47);
            btnExportExcel.Size = new Size(110, 26);
            btnExportExcel.FlatStyle = FlatStyle.Flat;
            btnExportExcel.BackColor = Color.FromArgb(0, 150, 136);
            btnExportExcel.ForeColor = Color.White;
            btnExportExcel.Click += new EventHandler(this.BtnExportReport_Click);

            var btnExportPdf = new Button();
            btnExportPdf.Text = "导出 PDF";
            btnExportPdf.Location = new Point(130, 47);
            btnExportPdf.Size = new Size(110, 26);
            btnExportPdf.FlatStyle = FlatStyle.Flat;
            btnExportPdf.BackColor = Color.FromArgb(63, 81, 181);
            btnExportPdf.ForeColor = Color.White;
            btnExportPdf.Click += new EventHandler(this.BtnExportReport_Click);

            this._lblReportCurrent = new Label();
            this._lblReportCurrent.Text = "请选择报告周期与日期后点击【生成报告】";
            this._lblReportCurrent.Location = new Point(250, 48);
            this._lblReportCurrent.AutoSize = true;
            this._lblReportCurrent.ForeColor = Color.FromArgb(100, 100, 100);

            this._reportMetricPanel = new FlowLayoutPanel();
            this._reportMetricPanel.Location = new Point(15, 75);
            this._reportMetricPanel.Size = new Size(835, 120);
            this._reportMetricPanel.FlowDirection = FlowDirection.LeftToRight;
            this._reportMetricPanel.WrapContents = false;
            this._reportMetricPanel.BackColor = Color.Transparent;

            this._chartReportTrend = new LearningTrendChart();
            this._chartReportTrend.Location = new Point(15, 205);
            this._chartReportTrend.Size = new Size(835, 145);

            var lblReportCatTitle = new Label();
            lblReportCatTitle.Text = "📂 分类分布";
            lblReportCatTitle.Location = new Point(15, 360);
            lblReportCatTitle.AutoSize = true;
            lblReportCatTitle.ForeColor = Color.FromArgb(33, 33, 33);

            this._reportCategoryPanel = new FlowLayoutPanel();
            this._reportCategoryPanel.Location = new Point(15, 382);
            this._reportCategoryPanel.Size = new Size(835, 70);
            this._reportCategoryPanel.FlowDirection = FlowDirection.LeftToRight;
            this._reportCategoryPanel.WrapContents = true;
            this._reportCategoryPanel.BackColor = Color.Transparent;

            this._lblReportSuggestions = new Label();
            this._lblReportSuggestions.Text = "💡 建议";
            this._lblReportSuggestions.Location = new Point(15, 462);
            this._lblReportSuggestions.AutoSize = true;
            this._lblReportSuggestions.ForeColor = Color.FromArgb(33, 33, 33);

            this._reportSuggestionsBox = new RichTextBox();
            this._reportSuggestionsBox.Location = new Point(15, 484);
            this._reportSuggestionsBox.Size = new Size(835, 70);
            this._reportSuggestionsBox.ReadOnly = true;
            this._reportSuggestionsBox.BorderStyle = BorderStyle.None;
            this._reportSuggestionsBox.BackColor = Color.White;

            this._lblReportAi = new Label();
            this._lblReportAi.Text = "🤖 AI 总结";
            this._lblReportAi.Location = new Point(15, 564);
            this._lblReportAi.AutoSize = true;
            this._lblReportAi.ForeColor = Color.FromArgb(33, 33, 33);

            this._reportAiBox = new RichTextBox();
            this._reportAiBox.Location = new Point(15, 586);
            this._reportAiBox.Size = new Size(835, 90);
            this._reportAiBox.ReadOnly = true;
            this._reportAiBox.BorderStyle = BorderStyle.None;
            this._reportAiBox.BackColor = Color.FromArgb(248, 248, 252);

            tabReport.Controls.Add(this._cmbReportKind);
            tabReport.Controls.Add(this._dtpReportDate);
            tabReport.Controls.Add(btnReportLoad);
            tabReport.Controls.Add(btnAiReport);
            tabReport.Controls.Add(btnExportMd);
            tabReport.Controls.Add(btnExportHtml);
            tabReport.Controls.Add(btnExportTxt);
            tabReport.Controls.Add(btnExportExcel);
            tabReport.Controls.Add(btnExportPdf);
            tabReport.Controls.Add(this._lblReportCurrent);
            tabReport.Controls.Add(this._reportMetricPanel);
            tabReport.Controls.Add(this._chartReportTrend);
            tabReport.Controls.Add(lblReportCatTitle);
            tabReport.Controls.Add(this._reportCategoryPanel);
            tabReport.Controls.Add(this._lblReportSuggestions);
            tabReport.Controls.Add(this._reportSuggestionsBox);
            tabReport.Controls.Add(this._lblReportAi);
            tabReport.Controls.Add(this._reportAiBox);

            // ===== 对比 Tab（多用户数据对比，04 方案 3.4） =====
            var tabComparison = new TabPage("📊 对比");
            tabComparison.AutoScroll = true;
            tabComparison.BackColor = Color.FromArgb(245, 245, 250);
            tabComparison.Padding = new Padding(10);

            // 顶部工具条：指标维度 + 对比周期
            var lblComparisonMetric = new Label();
            lblComparisonMetric.Text = "对比维度：";
            lblComparisonMetric.Location = new Point(15, 20);
            lblComparisonMetric.AutoSize = true;
            lblComparisonMetric.ForeColor = Color.FromArgb(33, 33, 33);

            this._cmbComparisonMetric = new ComboBox();
            this._cmbComparisonMetric.Location = new Point(90, 16);
            this._cmbComparisonMetric.Size = new Size(150, 25);
            this._cmbComparisonMetric.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbComparisonMetric.Items.AddRange(new[] { "学习时长", "正确率", "连续天数", "总经验值" });
            this._cmbComparisonMetric.SelectedIndex = 0;
            this._cmbComparisonMetric.SelectedIndexChanged += new EventHandler(this.CmbComparison_Changed);

            var lblComparisonPeriod = new Label();
            lblComparisonPeriod.Text = "周期：";
            lblComparisonPeriod.Location = new Point(265, 20);
            lblComparisonPeriod.AutoSize = true;
            lblComparisonPeriod.ForeColor = Color.FromArgb(33, 33, 33);

            this._cmbComparisonPeriod = new ComboBox();
            this._cmbComparisonPeriod.Location = new Point(300, 16);
            this._cmbComparisonPeriod.Size = new Size(120, 25);
            this._cmbComparisonPeriod.DropDownStyle = ComboBoxStyle.DropDownList;
            this._cmbComparisonPeriod.Items.AddRange(new[] { "今日", "本周", "本月" });
            this._cmbComparisonPeriod.SelectedIndex = 1;
            this._cmbComparisonPeriod.SelectedIndexChanged += new EventHandler(this.CmbComparison_Changed);

            this._chartComparison = new UserComparisonChart();
            this._chartComparison.Dock = DockStyle.None;
            this._chartComparison.Location = new Point(15, 55);
            this._chartComparison.Size = new Size(850, 380);

            tabComparison.Controls.Add(lblComparisonMetric);
            tabComparison.Controls.Add(this._cmbComparisonMetric);
            tabComparison.Controls.Add(lblComparisonPeriod);
            tabComparison.Controls.Add(this._cmbComparisonPeriod);
            tabComparison.Controls.Add(this._chartComparison);

            // 全部 Tab 挂载到 TabControl
            this._tabMain.TabPages.Add(tabOverview);
            this._tabMain.TabPages.Add(tabMemory);
            this._tabMain.TabPages.Add(tabWrong);
            this._tabMain.TabPages.Add(tabGoal);
            this._tabMain.TabPages.Add(tabReport);
            this._tabMain.TabPages.Add(tabComparison);
            this._tabMain.SelectedIndex = 0;
            #endregion

            // 窗体顶层控件添加：Header 置顶，Tab 主体填满（04 方案 3.1）
            this.Controls.Add(this._tabMain);
            this.Controls.Add(this.panelHeader);

            // 标准布局恢复（VS自动生成固定写法）
            this.ResumeLayout(false);
        }
        private void LearningManagementForm_Resize(object? sender, EventArgs e)
        {
            // Tab 内图表按固定位置布局 + AutoScroll，窗体尺寸变化无需手动重排（04 方案 3.1）。
        }

        public void ApplyTheme(ThemeColors colors)
        {
            BackColor = colors.Background;

            if (_cardMinutes != null) _cardMinutes.CardColor = colors.Surface;
            if (_cardItems != null) _cardItems.CardColor = colors.Surface;
            if (_cardAccuracy != null) _cardAccuracy.CardColor = colors.Surface;
            if (_cardStreak != null) _cardStreak.CardColor = colors.Surface;
            if (_cardRetention != null) _cardRetention.CardColor = colors.Surface;
            if (_cardGoal != null) _cardGoal.CardColor = colors.Surface;

            // Tab 主体与背景联动
            if (_tabMain != null)
            {
                _tabMain.BackColor = colors.Surface;
                _tabMain.ForeColor = colors.TextPrimary;
            }
            if (_panelAdvice != null)
                _panelAdvice.BackColor = colors.Surface;
            if (_lblAdvice != null)
                _lblAdvice.ForeColor = colors.TextPrimary;
            if (_lblAdviceHint != null)
                _lblAdviceHint.ForeColor = colors.TextSecondary;
            if (_panelAlgorithm != null)
                _panelAlgorithm.BackColor = colors.Surface;
            if (panelCalendar != null)
                panelCalendar.BackColor = colors.Surface;
            if (labelCalendarTitle != null)
                labelCalendarTitle.ForeColor = colors.TextPrimary;

            // 03 新图主题联动（基类 ApplyTheme 统一配色/背景）
            if (_chartTrend != null) _chartTrend.ApplyTheme(colors);
            if (_chartCategory != null) _chartCategory.ApplyTheme(colors);
            if (_chartForgettingCurve != null) _chartForgettingCurve.ApplyTheme(colors);
            if (_chartRating != null) _chartRating.ApplyTheme(colors);
            if (_chartHeatmap != null) _chartHeatmap.ApplyTheme(colors);
            if (_goalChart != null) _goalChart.ApplyTheme(colors);
            if (_maturityChart != null) _maturityChart.ApplyTheme(colors);
            if (_chartComparison != null) _chartComparison.ApplyTheme(colors);

            if (_calendarView != null)
                _calendarView.BackColor = colors.Surface;
            if (_wrongStatsPanel != null)
                _wrongStatsPanel.BackColor = colors.Surface;
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

                // 注销学习事件订阅
                UnsubscribeLearningEvents();

                // 注销事件订阅
                Resize -= LearningManagementForm_Resize;
                if (btnToday != null) btnToday.Click -= BtnTimeRange_Click;
                if (btnWeek != null) btnWeek.Click -= BtnTimeRange_Click;
                if (btnMonth != null) btnMonth.Click -= BtnTimeRange_Click;
                if (btnAll != null) btnAll.Click -= BtnTimeRange_Click;
                if (btnExport != null) btnExport.Click -= BtnExport_Click;
                if (_cmbAlgorithm != null) _cmbAlgorithm.SelectedIndexChanged -= CmbAlgorithm_SelectedIndexChanged;
                if (_cmbComparisonMetric != null) _cmbComparisonMetric.SelectedIndexChanged -= CmbComparison_Changed;
                if (_cmbComparisonPeriod != null) _cmbComparisonPeriod.SelectedIndexChanged -= CmbComparison_Changed;

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
