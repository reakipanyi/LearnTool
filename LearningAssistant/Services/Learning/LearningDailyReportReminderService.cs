using LearningAssistant.Models.Learning;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 日报自动提醒事件参数（05 方案 3.4）。
    /// </summary>
    public class DailyReportReminderEventArgs : EventArgs
    {
        /// <summary>所属用户ID</summary>
        public string UserId { get; set; } = string.Empty;
        /// <summary>提醒标题（含日期）</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>昨日学习小结正文</summary>
        public string Summary { get; set; } = string.Empty;
        /// <summary>是否有学习数据（无数据时用于提示文案）</summary>
        public bool HasLearning { get; set; }
    }

    /// <summary>
    /// 日报自动提醒服务：复用 <see cref="ILearningReminderService"/>，每日定时推送“昨日学习小结”
    /// （时长/数量/正确率/连击），让统计被动触达用户（05 方案 3.4）。
    /// </summary>
    public interface ILearningDailyReportReminderService : IDisposable
    {
        /// <summary>日报小结就绪事件（通常已在 UI 线程被消费端订阅）</summary>
        event EventHandler<DailyReportReminderEventArgs>? DailyReportReady;

        /// <summary>订阅提醒触发事件，开始检测日报小结提醒</summary>
        void Start();

        /// <summary>取消订阅提醒触发事件</summary>
        void Stop();

        /// <summary>
        /// 幂等地为指定用户创建默认日报提醒（Daily，默认 08:00）。已存在则复用。
        /// </summary>
        Reminder EnsureDefaultSummaryReminder(string userId);

        /// <summary>构建“昨日学习小结”文本（无记录时返回提示文案）。</summary>
        string BuildYesterdaySummary(string userId);
    }

    /// <summary>
    /// 日报小结提醒实现（05 方案 3.4）。
    /// </summary>
    public class LearningDailyReportReminderService : ILearningDailyReportReminderService
    {
        /// <summary>日报小结提醒标题标记，用于识别“该提醒属于日报小结”</summary>
        public const string SummaryMarker = "#学习小结#";

        /// <summary>默认触发时间（当天 08:00）</summary>
        public static readonly TimeSpan DefaultTime = new TimeSpan(8, 0, 0);

        private readonly ILearningReminderService _reminderService;
        private readonly ILearningStatsAggregator _aggregator;
        private readonly ILogger<LearningDailyReportReminderService>? _logger;
        private bool _started;
        private bool _disposed;

        /// <inheritdoc/>
        public event EventHandler<DailyReportReminderEventArgs>? DailyReportReady;

        public LearningDailyReportReminderService(
            ILearningReminderService reminderService,
            ILearningStatsAggregator aggregator,
            ILogger<LearningDailyReportReminderService>? logger)
        {
            _reminderService = reminderService;
            _aggregator = aggregator;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void Start()
        {
            if (_started) return;
            _started = true;
            _reminderService.ReminderTriggered += OnReminderTriggered;
        }

        /// <inheritdoc/>
        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _reminderService.ReminderTriggered -= OnReminderTriggered;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }

        /// <inheritdoc/>
        public Reminder EnsureDefaultSummaryReminder(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("用户ID不能为空", nameof(userId));
            }

            var existing = _reminderService.GetUserReminders(userId)
                .FirstOrDefault(r =>
                    !string.IsNullOrEmpty(r.Title) && r.Title.Contains(SummaryMarker, StringComparison.Ordinal));
            if (existing != null) return existing;

            var reminder = new Reminder
            {
                UserId = userId,
                Title = $"{SummaryMarker} 昨日学习小结",
                Description = "每日定时推送昨日学习情况汇总（时长 / 数量 / 正确率 / 连击）",
                Type = ReminderType.Study,
                Trigger = ReminderTrigger.FixedTime,
                RepeatType = ReminderRepeatType.Daily,
                Time = DefaultTime,
                Icon = "📋",
                ShowPopup = true,
                PlaySound = true
            };
            _reminderService.AddReminder(reminder);
            return reminder;
        }

        /// <inheritdoc/>
        public string BuildYesterdaySummary(string userId)
        {
            if (string.IsNullOrEmpty(userId)) return "暂无学习数据。请先登录/选择用户。";

            DailyOverview overview;
            try
            {
                overview = _aggregator.GetDailyOverview(userId, DateTime.Today.AddDays(-1));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "读取昨日学习概览失败");
                return "暂时无法读取昨日学习数据，请稍后再试。";
            }

            if (overview == null || (overview.TimeSpentMinutes <= 0 && overview.ItemsStudied <= 0))
            {
                return "昨天没有学习记录，今天开始吧！\n(时长 / 数量 / 正确率 / 连击均无数据)";
            }

            return string.Join("\n",
                $"学习时长：{FormatMinutes(overview.TimeSpentMinutes)}",
                $"学习项数：{overview.ItemsStudied} 项",
                $"正确率：{overview.Accuracy:F1}%",
                $"连续学习：{overview.StreakDays} 天",
                $"等级 / 经验：Lv.{overview.Level} · {overview.XP} XP",
                $"目标达成：{(overview.GoalCompleted ? "已完成 🎉" : "未达成")}");
        }

        private void OnReminderTriggered(object? sender, ReminderTriggeredEventArgs e)
        {
            try
            {
                var reminder = e.Reminder;
                if (reminder == null || string.IsNullOrEmpty(reminder.Title)
                    || !reminder.Title.Contains(SummaryMarker, StringComparison.Ordinal))
                {
                    return;
                }
                BuildAndRaise(reminder.UserId);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "生成日报小结提醒失败");
            }
        }

        private void BuildAndRaise(string userId)
        {
            var summary = BuildYesterdaySummary(userId);
            var yesterday = DateTime.Today.AddDays(-1);
            DailyReportReady?.Invoke(this, new DailyReportReminderEventArgs
            {
                UserId = userId,
                Title = $"昨日({yesterday:MM-dd})学习小结",
                Summary = summary,
                HasLearning = !summary.StartsWith("昨天没有") && !summary.StartsWith("暂时无法") && !summary.StartsWith("请先登录")
            });
        }

        private static string FormatMinutes(int minutes)
        {
            if (minutes < 60) return $"{minutes} 分钟";
            return $"{minutes / 60} 小时 {minutes % 60} 分";
        }
    }
}