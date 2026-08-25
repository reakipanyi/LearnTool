using System.Collections.Concurrent;
using System.Globalization;
using LearningAssistant.Common.Events;
using LearningAssistant.Data.Database;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Cache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    /// <summary>
    /// 统一统计聚合服务实现（统计底座模块 A2+A7+A8）
    /// - A2：统一聚合多源数据，输出统一 DTO（读路径优先走 DailyRollup 预聚合，避免长周期全表扫描）
    /// - A7：聚合结果分段缓存（按 用户+时间范围），学习事件只失效受影响分段；提供 ForceRefresh 兜底
    /// - A8：写路径走内存队列 + 定时/定量批量落库；时长以幂等键去重，防重复计数
    /// </summary>
    public class LearningStatsAggregator : ILearningStatsAggregator
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly ICacheService _cache;
        private readonly IEventBus? _eventBus;
        private readonly ILogger<LearningStatsAggregator>? _logger;

        // ====== A8 写路径：内存队列 + 批量落库 ======
        private readonly ConcurrentQueue<ActivityEntry> _activityQueue = new();
        private readonly ConcurrentDictionary<string, byte> _idempotencyKeys = new(StringComparer.Ordinal);
        private readonly object _flushLock = new();
        private readonly System.Timers.Timer _flushTimer;
        private const int FlushBatchSize = 50;
        private const int FlushIntervalMs = 5000;
        private const int DefaultCacheTtlMinutes = 5;

        // ====== A7 分段缓存索引："{userId}|{yyyyMMdd}" -> 覆盖该日期的缓存键集合 ======
        private readonly ConcurrentDictionary<string, HashSet<string>> _cacheIndex = new(StringComparer.Ordinal);

        // 事件订阅句柄（Dispose 时反订阅需持有同一引用）
        private Action<ItemLearnedEvent>? _onItemLearned;
        private Action<ItemWrongEvent>? _onItemWrong;
        private Action<ReviewDoneEvent>? _onReviewDone;
        private Action<PomodoroCompletedEvent>? _onPomodoroCompleted;
        private Action<LearningSessionCompletedEvent>? _onSessionCompleted;

        /// <summary>BuildOverview 输出载体（PeriodOverview 为抽象基类，用此私有子类承载聚合值）</summary>
        private sealed class OverviewSnapshot : PeriodOverview
        {
        }

        private bool _disposed;

        /// <summary>写路径活动条目</summary>
        private sealed record ActivityEntry(
            string UserId,
            string ActivityType,
            string SubCategory,
            int Count,
            int TimeSpentMinutes,
            string IdempotencyKey,
            DateTime OccurredAt);

        public LearningStatsAggregator(
            IDbContextFactory<AppDbContext> dbFactory,
            ICacheService cache,
            IEventBus? eventBus = null,
            ILogger<LearningStatsAggregator>? logger = null)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _eventBus = eventBus;
            _logger = logger;

            _flushTimer = new System.Timers.Timer(FlushIntervalMs);
            _flushTimer.Elapsed += (_, _) => _ = Task.Run(TryFlush);
            _flushTimer.AutoReset = true;
            _flushTimer.Start();

            SubscribeEvents();
        }

        #region A8 写路径

        public void RecordActivity(string userId, string activityType, string subCategory, int count = 1,
            int timeSpentMinutes = 0, string? idempotencyKey = null)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(activityType) || count <= 0)
                return;

            // 幂等键：同一键只累计一次，防重复计数
            var key = string.IsNullOrEmpty(idempotencyKey)
                ? $"{userId}:{activityType}:{subCategory}:{DateTime.UtcNow.Ticks}"
                : idempotencyKey;

            if (!_idempotencyKeys.TryAdd(key, 0))
                return; // 该事件已处理过

            _activityQueue.Enqueue(new ActivityEntry(userId, activityType, subCategory ?? string.Empty, count,
                Math.Max(0, timeSpentMinutes), key, DateTime.Now));

            // 达标即触发批量落库
            if (_activityQueue.Count >= FlushBatchSize)
            {
                _ = Task.Run(TryFlush);
            }
        }

        public void RecordStudyTime(string userId, int minutes, string subCategory, string? idempotencyKey = null)
        {
            RecordActivity(userId, "StudyTime", subCategory ?? string.Empty, 1, minutes, idempotencyKey);
        }

        public void Flush()
        {
            TryFlush();
        }

        private void TryFlush()
        {
            lock (_flushLock)
            {
                if (_activityQueue.IsEmpty)
                    return;

                var entries = new List<ActivityEntry>(_activityQueue.Count);
                while (_activityQueue.TryDequeue(out var entry))
                {
                    entries.Add(entry);
                }

                try
                {
                    WriteBatch(entries);
                    // 受影响日期分段失效
                    var affectedDates = entries.Select(e => e.OccurredAt.Date).Distinct().ToArray();
                    foreach (var date in affectedDates)
                    {
                        Invalidate(entries[0].UserId, date);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "统计聚合批量落库失败，失败 {Count} 条", entries.Count);
                }
            }
        }

        /// <summary>批量 upsert DailyRollup（物化表）+ 写入 LearningRecords 明细</summary>
        private void WriteBatch(List<ActivityEntry> entries)
        {
            using var db = _dbFactory.CreateDbContext();

            foreach (var group in entries.GroupBy(e => (e.UserId, e.OccurredAt.Date)))
            {
                var userId = group.Key.UserId;
                var date = group.Key.Date;

                // 按子类别统计当日条目数，用于 Top/Weak 分类
                var categoryItems = new Dictionary<string, int>(StringComparer.Ordinal);
                var categoryWrong = new Dictionary<string, int>(StringComparer.Ordinal);
                int learn = 0, review = 0, correct = 0, wrong = 0, time = 0;

                foreach (var e in group)
                {
                    switch (e.ActivityType)
                    {
                        case "Learn": learn += e.Count; break;
                        case "Review": review += e.Count; break;
                        case "Correct": correct += e.Count; break;
                        case "Wrong": wrong += e.Count; break;
                        case "StudyTime": time += e.TimeSpentMinutes; break;
                        default: learn += e.Count; break;
                    }
                    if (!string.IsNullOrEmpty(e.SubCategory))
                        categoryItems[e.SubCategory] = categoryItems.GetValueOrDefault(e.SubCategory) + e.Count;
                    if (e.ActivityType == "Wrong")
                        categoryWrong[e.SubCategory] = categoryWrong.GetValueOrDefault(e.SubCategory) + e.Count;

                    // 明细表（供分类/错因等精确视图）
                    db.LearningRecords.Add(new LearningRecordEntity
                    {
                        UserId = userId,
                        ActivityType = e.ActivityType,
                        SubCategory = e.SubCategory,
                        Count = e.Count,
                        RecordDate = e.OccurredAt
                    });
                }

                // upsert 物化表
                var rollup = db.DailyRollups.FirstOrDefault(r => r.UserId == userId && r.Date.Date == date);
                if (rollup == null)
                {
                    rollup = new DailyRollupEntity { UserId = userId, Date = date.Date };
                    db.DailyRollups.Add(rollup);
                }

                rollup.TimeSpentMinutes += time;
                rollup.ItemsStudied += learn + review;
                rollup.CorrectCount += correct;
                rollup.WrongCount += wrong;
                rollup.Accuracy = StatsAggregation.ComputeAccuracy(rollup.CorrectCount, rollup.WrongCount);
                rollup.TopCategory = categoryItems.Count > 0
                    ? categoryItems.OrderByDescending(kv => kv.Value).First().Key
                    : rollup.TopCategory;
                rollup.WeakCategory = categoryWrong.Count > 0
                    ? categoryWrong.OrderByDescending(kv => kv.Value).First().Key
                    : rollup.WeakCategory;
                rollup.Version++;
                rollup.UpdatedAt = DateTime.Now;
            }

            db.SaveChanges();
        }

        #endregion

        #region A7 分段缓存

        private void SubscribeEvents()
        {
            if (_eventBus == null)
                return;

            // 学习数量（Learn/Wrong）由单个项事件实时累计；会话完成事件只补 Correct + 时长，
            // 避免与逐项事件重复计数学习数量。
            _onItemLearned = e => RecordActivity(e.UserId, "Learn", e.SubCategory, 1,
                idempotencyKey: $"learn:{e.UserId}:{e.ItemId}:{e.LearnedAt.Ticks}");
            _onItemWrong = e => RecordActivity(e.UserId, "Wrong", e.SubCategory, 1,
                idempotencyKey: $"wrong:{e.UserId}:{e.ItemId}:{e.WrongAt.Ticks}");
            _onReviewDone = e => RecordActivity(e.UserId, "Review", string.Empty, 1,
                idempotencyKey: $"review:{e.UserId}:{e.ItemId}:{e.ReviewedAt.Ticks}");
            _onPomodoroCompleted = e => RecordStudyTime(e.UserId, e.DurationMinutes, "番茄钟",
                idempotencyKey: $"pom:{e.UserId}:{e.CompletedAt:yyyyMMddHHmmssfff}");
            _onSessionCompleted = e =>
            {
                var now = DateTime.Now;
                RecordActivity(e.UserId, "Correct", e.SubCategory, e.CorrectCount,
                    idempotencyKey: $"sess-correct:{e.UserId}:{now:yyyyMMddHHmmssfff}");
                RecordStudyTime(e.UserId, (int)e.Duration.TotalMinutes, e.SubCategory,
                    idempotencyKey: $"sess-time:{e.UserId}:{e.Duration.Ticks}");
            };

            _eventBus.Subscribe(_onItemLearned);
            _eventBus.Subscribe(_onItemWrong);
            _eventBus.Subscribe(_onReviewDone);
            _eventBus.Subscribe(_onPomodoroCompleted);
            _eventBus.Subscribe(_onSessionCompleted);
        }

        private static string CacheKey(string userId, string section, DateTime date) =>
            $"stats_{userId}_{section}_{date:yyyyMMdd}";

        /// <summary>
        /// 带分段缓存的读取：优先读缓存，未命中则计算；键按覆盖的日期全集注册，供 Invalidate 按日精确失效。
        /// </summary>
        private T GetCached<T>(string userId, DateTime[] coveredDates, string key, Func<T> compute) where T : class
        {
            if (_cache.TryGet<T>(key, out var cached) && cached != null)
                return cached;

            var value = compute();
            _cache.Set(key, value, DefaultCacheTtlMinutes);

            lock (_cacheIndex)
            {
                foreach (var date in coveredDates)
                {
                    var bucket = $"{userId}|{date:yyyyMMdd}";
                    var set = _cacheIndex.GetOrAdd(bucket, _ => new HashSet<string>(StringComparer.Ordinal));
                    set.Add(key);
                }
            }

            return value;
        }

        public void Invalidate(string userId, DateTime date)
        {
            HashSet<string>? keys = null;

            lock (_cacheIndex)
            {
                var bucket = $"{userId}|{date:yyyyMMdd}";
                if (_cacheIndex.TryRemove(bucket, out keys))
                {
                    // 空桶一次性取出即可
                }
            }

            if (keys != null)
            {
                foreach (var key in keys)
                    _cache.Remove(key);
            }
        }

        public void InvalidateAll(string userId)
        {
            List<string> keysToRemove;
            lock (_cacheIndex)
            {
                var buckets = _cacheIndex.Keys
                    .Where(b => b.StartsWith($"{userId}|", StringComparison.Ordinal)).ToList();
                keysToRemove = buckets.SelectMany(b => _cacheIndex[b]).Distinct().ToList();
                foreach (var b in buckets)
                    _cacheIndex.TryRemove(b, out _);
            }

            foreach (var key in keysToRemove)
                _cache.Remove(key);
        }

        public void ForceRefresh(string userId)
        {
            InvalidateAll(userId);
        }

        #endregion

        #region A2 统一聚合 DTO

        public DailyOverview GetDailyOverview(string userId, DateTime date)
        {
            var daily = date.Date;
            return GetCached(userId, new[] { daily }, CacheKey(userId, "daily", daily), () => ComputeDailyOverview(userId, daily));
        }

        private DailyOverview ComputeDailyOverview(string userId, DateTime date)
        {
            using var db = _dbFactory.CreateDbContext();

            var rollup = db.DailyRollups.AsNoTracking().FirstOrDefault(r => r.UserId == userId && r.Date.Date == date);
            var studyStats = db.StudyStats.AsNoTracking().FirstOrDefault(s => s.UserId == userId);

            return new DailyOverview
            {
                Date = date,
                TimeSpentMinutes = rollup?.TimeSpentMinutes ?? 0,
                ItemsStudied = rollup?.ItemsStudied ?? 0,
                CorrectCount = rollup?.CorrectCount ?? 0,
                WrongCount = rollup?.WrongCount ?? 0,
                Accuracy = rollup?.Accuracy ?? 0,
                StreakDays = rollup?.StreakDays ?? ComputeStreak(db, userId, date),
                XP = studyStats?.XP ?? 0,
                Level = studyStats != null ? Math.Max(1, studyStats.XP / 100 + 1) : 1,
                GoalCompleted = rollup?.GoalCompleted ?? false
            };
        }

        public WeeklyOverview GetWeeklyOverview(string userId, DateTime date)
        {
            var monday = StartOfWeek(date);
            var days = CoveredDates(monday, monday.AddDays(6));
            return GetCached(userId, days, CacheKey(userId, "weekly", monday), () => ComputeWeeklyOverview(userId, monday, days));
        }

        private static DateTime StartOfWeek(DateTime date) =>
            date.Date.AddDays(-((int)date.DayOfWeek + 6) % 7);

        private WeeklyOverview ComputeWeeklyOverview(string userId, DateTime monday, DateTime[] days)
        {
            var sunday = monday.AddDays(6);
            var prev = StartOfWeek(monday.AddDays(-1));

            using var db = _dbFactory.CreateDbContext();
            var rollups = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= monday && r.Date.Date <= sunday).ToList();
            var prevRollups = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= prev && r.Date.Date <= prev.AddDays(6)).ToList();

            var ov = BuildOverview(db, userId, rollups);
            var prevOv = BuildOverview(db, userId, prevRollups);

            return new WeeklyOverview
            {
                StartDate = monday,
                EndDate = sunday,
                Year = monday.Year,
                WeekNumber = ISOWeek.GetWeekOfYear(monday),
                TimeSpentMinutes = ov.TimeSpentMinutes,
                ItemsStudied = ov.ItemsStudied,
                CorrectCount = ov.CorrectCount,
                WrongCount = ov.WrongCount,
                Accuracy = ov.Accuracy,
                StreakDays = ov.StreakDays,
                XP = ov.XP,
                Level = ov.Level,
                GoalCompleted = ov.GoalCompleted,
                TimeSpentDeltaMinutes = ov.TimeSpentMinutes - prevOv.TimeSpentMinutes,
                ItemsStudiedDelta = ov.ItemsStudied - prevOv.ItemsStudied,
                AccuracyDelta = Math.Round(ov.Accuracy - prevOv.Accuracy, 2),
                TopCategory = ov.TopCategory,
                WeakCategory = ov.WeakCategory
            };
        }

        public MonthlyOverview GetMonthlyOverview(string userId, DateTime date)
        {
            var first = new DateTime(date.Year, date.Month, 1);
            var days = CoveredDates(first, first.AddMonths(1).AddDays(-1));
            return GetCached(userId, days, CacheKey(userId, "monthly", first), () => ComputeMonthlyOverview(userId, first, days));
        }

        private MonthlyOverview ComputeMonthlyOverview(string userId, DateTime first, DateTime[] days)
        {
            var last = first.AddMonths(1).AddDays(-1);
            var prevFirst = first.AddMonths(-1);
            var prevLast = first.AddDays(-1);

            using var db = _dbFactory.CreateDbContext();
            var rollups = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= first && r.Date.Date <= last).ToList();
            var prevRollups = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= prevFirst && r.Date.Date <= prevLast).ToList();

            var ov = BuildOverview(db, userId, rollups);
            var prevOv = BuildOverview(db, userId, prevRollups);

            return new MonthlyOverview
            {
                StartDate = first,
                EndDate = last,
                Year = first.Year,
                Month = first.Month,
                TimeSpentMinutes = ov.TimeSpentMinutes,
                ItemsStudied = ov.ItemsStudied,
                CorrectCount = ov.CorrectCount,
                WrongCount = ov.WrongCount,
                Accuracy = ov.Accuracy,
                StreakDays = ov.StreakDays,
                XP = ov.XP,
                Level = ov.Level,
                GoalCompleted = ov.GoalCompleted,
                TimeSpentDeltaMinutes = ov.TimeSpentMinutes - prevOv.TimeSpentMinutes,
                ItemsStudiedDelta = ov.ItemsStudied - prevOv.ItemsStudied,
                AccuracyDelta = Math.Round(ov.Accuracy - prevOv.Accuracy, 2),
                TopCategory = ov.TopCategory,
                WeakCategory = ov.WeakCategory
            };
        }

        /// <summary>基于每日明细列表构建周期概览（不含增量字段）</summary>
        private PeriodOverview BuildOverview(AppDbContext db, string userId, List<DailyRollupEntity> rollups)
        {
            var top = rollups.Where(r => !string.IsNullOrEmpty(r.TopCategory))
                .GroupBy(r => r.TopCategory)
                .OrderByDescending(g => g.Sum(r => r.ItemsStudied))
                .Select(g => g.Key).FirstOrDefault() ?? string.Empty;
            var weak = rollups.Where(r => !string.IsNullOrEmpty(r.WeakCategory))
                .GroupBy(r => r.WeakCategory)
                .OrderByDescending(g => g.Sum(r => r.WrongCount))
                .Select(g => g.Key).FirstOrDefault() ?? string.Empty;

            var studyStats = db.StudyStats.AsNoTracking().FirstOrDefault(s => s.UserId == userId);

            // 连击：以周期内最后一个有学习行为的日期为基准向前连续计数
            var activeDates = rollups
                .Where(r => r.ItemsStudied > 0 || r.TimeSpentMinutes > 0)
                .Select(r => r.Date.Date)
                .ToHashSet();
            var streak = 0;
            if (activeDates.Count > 0)
            {
                var cursor = activeDates.Max();
                while (activeDates.Contains(cursor))
                {
                    streak++;
                    cursor = cursor.AddDays(-1);
                }
            }

            return new OverviewSnapshot
            {
                TimeSpentMinutes = rollups.Sum(r => r.TimeSpentMinutes),
                ItemsStudied = rollups.Sum(r => r.ItemsStudied),
                CorrectCount = rollups.Sum(r => r.CorrectCount),
                WrongCount = rollups.Sum(r => r.WrongCount),
                Accuracy = StatsAggregation.ComputeAccuracy(rollups.Sum(r => r.CorrectCount), rollups.Sum(r => r.WrongCount)),
                StreakDays = streak,
                XP = studyStats?.XP ?? 0,
                Level = studyStats != null ? Math.Max(1, studyStats.XP / 100 + 1) : 1,
                TopCategory = top,
                WeakCategory = weak
            };
        }

        public TrendSeries GetTrend(string userId, DateTime start, DateTime end, TrendSeriesType type = TrendSeriesType.Trend)
        {
            var days = CoveredDates(start, end);
            var section = $"trend_{type}_{start:yyyyMMdd}_{end:yyyyMMdd}";
            return GetCached(userId, days, CacheKey(userId, section, start), () => ComputeTrend(userId, days, type));
        }

        private TrendSeries ComputeTrend(string userId, DateTime[] dates, TrendSeriesType type)
        {
            var start = dates[0];
            var end = dates[^1];

            using var db = _dbFactory.CreateDbContext();
            var map = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end)
                .ToDictionary(r => r.Date.Date);

            var points = new List<TrendPoint>(dates.Length);
            foreach (var d in dates)
            {
                double value = 0;
                if (map.TryGetValue(d, out var r))
                {
                    value = type switch
                    {
                        TrendSeriesType.Accuracy => r.Accuracy,
                        _ => r.ItemsStudied
                    };
                }
                points.Add(new TrendPoint { Date = d, Value = value });
            }

            return new TrendSeries { SeriesType = type, Points = points };
        }

        public List<CategoryBreakdown> GetCategoryBreakdown(string userId, DateTime start, DateTime end)
        {
            var days = CoveredDates(start, end);
            var section = $"cat_{start:yyyyMMdd}_{end:yyyyMMdd}";
            return GetCached(userId, days, CacheKey(userId, section, start), () => ComputeCategoryBreakdown(userId, start.Date, end.Date));
        }

        private List<CategoryBreakdown> ComputeCategoryBreakdown(string userId, DateTime start, DateTime end)
        {
            using var db = _dbFactory.CreateDbContext();
            var groups = db.LearningRecords.AsNoTracking()
                .Where(r => r.UserId == userId && r.RecordDate.Date >= start && r.RecordDate.Date <= end)
                .AsEnumerable()
                .GroupBy(r => string.IsNullOrEmpty(r.SubCategory) ? "未分类" : r.SubCategory);

            var list = new List<CategoryBreakdown>();
            foreach (var g in groups)
            {
                var correct = g.Where(r => r.ActivityType == "Correct").Sum(r => r.Count);
                var wrong = g.Where(r => r.ActivityType == "Wrong").Sum(r => r.Count);
                var items = g.Where(r => r.ActivityType is "Learn" or "Review").Sum(r => r.Count);
                var time = g.Where(r => r.ActivityType == "StudyTime").Sum(r => r.Count);

                list.Add(new CategoryBreakdown
                {
                    Category = g.Key,
                    ItemsStudied = items,
                    CorrectCount = correct,
                    WrongCount = wrong,
                    TimeSpentMinutes = time,
                    Accuracy = StatsAggregation.ComputeAccuracy(correct, wrong)
                });
            }

            return list.OrderByDescending(c => c.ItemsStudied).ToList();
        }

        public MemoryInsights GetMemoryInsights(string userId)
        {
            var today = DateTime.Today;
            return GetCached(userId, new[] { today }, CacheKey(userId, "memory", today), () => ComputeMemoryInsights(userId));
        }

        private MemoryInsights ComputeMemoryInsights(string userId)
        {
            var result = new MemoryInsights();
            using var db = _dbFactory.CreateDbContext();

            var items = db.SpacedRepetitionItems.AsNoTracking()
                .Where(i => i.UserId == userId && i.IsActive).ToList();

            result.TotalItems = items.Count;
            result.DueToday = items.Count(i => i.NextReviewDate.Date <= DateTime.Today);

            result.Maturity.NewCount = items.Count(i => i.LearningStage <= 0);
            result.Maturity.LearningCount = items.Count(i => i.LearningStage == 1);
            result.Maturity.MasteredCount = items.Count(i => i.LearningStage >= 2);

            var recallItems = items.Where(i => i.CorrectCount + i.WrongCount > 0).ToList();
            result.RetentionRate = recallItems.Count > 0
                ? Math.Round(recallItems.Sum(i => (double)i.CorrectCount / (i.CorrectCount + i.WrongCount)) / recallItems.Count, 3)
                : 0;

            result.ReviewDistribution = items
                .Where(i => i.LastReviewDate.HasValue)
                .GroupBy(i => (int)(DateTime.Today - i.LastReviewDate!.Value).TotalDays)
                .ToDictionary(g => g.Key, g => g.Count());

            var avgStability = items.Where(i => i.Stability > 0).Select(i => i.Stability).DefaultIfEmpty(10).Average();
            for (int d = 0; d <= 30; d++)
                result.ForgettingCurve[d] = Math.Round(Math.Exp(-d / avgStability), 3);

            return result;
        }

        public WrongAnswerSummary GetWrongAnswerSummary(string userId)
        {
            var today = DateTime.Today;
            return GetCached(userId, new[] { today }, CacheKey(userId, "wrong", today), () => ComputeWrongAnswerSummary(userId));
        }

        private WrongAnswerSummary ComputeWrongAnswerSummary(string userId)
        {
            var result = new WrongAnswerSummary();
            using var db = _dbFactory.CreateDbContext();

            var items = db.WrongAnswers.AsNoTracking().Where(w => w.UserId == userId && w.IsActive).ToList();

            result.TotalCount = items.Count;
            result.ActiveCount = items.Count;
            result.UnreviewedCount = items.Count(w => !w.LastReviewAt.HasValue);
            result.DifficultyDistribution = items
                .GroupBy(w => w.MasteryLevel)
                .ToDictionary(g => $"级别{g.Key}", g => g.Count());
            result.TopWrongReasons = items
                .Where(w => !string.IsNullOrEmpty(w.Category))
                .GroupBy(w => w.Category!)
                .ToDictionary(g => g.Key, g => g.Count());

            return result;
        }

        public EfficiencyReport GetEfficiencyReport(string userId)
        {
            var today = DateTime.Today;
            return GetCached(userId, new[] { today }, CacheKey(userId, "efficiency", today), () => ComputeEfficiencyReport(userId));
        }

        private EfficiencyReport ComputeEfficiencyReport(string userId)
        {
            var start = DateTime.Today.AddDays(-29);
            var end = DateTime.Today;

            using var db = _dbFactory.CreateDbContext();
            var rollups = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && r.Date.Date >= start && r.Date.Date <= end).ToList();

            var report = new EfficiencyReport
            {
                TimeSpentMinutes = rollups.Sum(r => r.TimeSpentMinutes),
                Accuracy = StatsAggregation.ComputeAccuracy(rollups.Sum(r => r.CorrectCount), rollups.Sum(r => r.WrongCount))
            };

            var activeDates = rollups.Where(r => r.ItemsStudied > 0 || r.TimeSpentMinutes > 0)
                .Select(r => r.Date.Date).ToHashSet();
            int streak = 0;
            for (var d = end; d >= start; d = d.AddDays(-1))
            {
                if (activeDates.Contains(d)) streak++;
                else break;
            }
            report.StreakDays = streak;

            var accuracyScore = report.Accuracy;
            var consistencyScore = Math.Min(100, report.StreakDays * 10);
            var volumeScore = Math.Min(100, report.TimeSpentMinutes * 0.5);
            report.EfficiencyScore = Math.Round(accuracyScore * 0.5 + consistencyScore * 0.3 + volumeScore * 0.2, 2);

            report.Summary = report.EfficiencyScore >= 80 ? "学习效率优秀，继续保持"
                : report.EfficiencyScore >= 60 ? "学习效率良好，可加强薄弱环节"
                : report.EfficiencyScore >= 40 ? "学习效率一般，建议提升正确率与连续性"
                : "学习效率较低，建议制定规律学习计划";

            return report;
        }

        private int ComputeStreak(AppDbContext db, string userId, DateTime date)
        {
            var dates = db.DailyRollups.AsNoTracking()
                .Where(r => r.UserId == userId && (r.ItemsStudied > 0 || r.TimeSpentMinutes > 0) && r.Date.Date <= date.Date)
                .Select(r => r.Date.Date).OrderByDescending(d => d).ToList();

            int streak = 0;
            var check = date.Date;
            for (var i = 0; i < dates.Count && check >= dates.Min(); i++)
            {
                if (dates[i] == check) { streak++; check = check.AddDays(-1); }
                else break;
            }
            return streak;
        }

        private static DateTime[] CoveredDates(DateTime start, DateTime end)
        {
            var list = new List<DateTime>();
            for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
                list.Add(d);
            return list.ToArray();
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (_eventBus != null && _onSessionCompleted != null)
            {
                _eventBus.Unsubscribe(_onItemLearned);
                _eventBus.Unsubscribe(_onItemWrong);
                _eventBus.Unsubscribe(_onReviewDone);
                _eventBus.Unsubscribe(_onPomodoroCompleted);
                _eventBus.Unsubscribe(_onSessionCompleted);
            }

            _flushTimer.Stop();
            _flushTimer.Dispose();
            TryFlush();
        }
    }
}