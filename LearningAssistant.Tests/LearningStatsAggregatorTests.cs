using System.Collections.Concurrent;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Cache;
using LearningAssistant.Services.Learning;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FluentAssertions;

namespace LearningAssistant.Tests
{
    /// <summary>
    /// 统计底座聚合口径测试（对应 docs/优化改进方案/02-数据分析-统计底座模块.md 验收 #6）：
    /// 时长=会话+番茄、正确率、连击、分段缓存失效。
    /// 使用隔离的内存 SQLite 库，不污染真实数据库。
    /// </summary>
    public class LearningStatsAggregatorTests
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;
        private readonly IDbContextFactory<AppDbContext> _dbFactory;

        public LearningStatsAggregatorTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();
            _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
            _dbFactory = new InMemoryFactory(_options);

            using var db = _dbFactory.CreateDbContext();
            db.Database.EnsureCreated();
        }

        private LearningStatsAggregator CreateAggregator(ICacheService? cache = null) =>
            new(_dbFactory, cache ?? new FakeCache());

        /// <summary>
        /// 预置用户：统计明细写入受 LearningRecord.UserId→UserProfile.Id 级联外键约束，
        /// 需先存在对应用户（与生产一致：统计只针对已存在用户）。
        /// </summary>
        private void EnsureUser(string userId)
        {
            using var db = _dbFactory.CreateDbContext();
            if (db.UserProfiles.Any(u => u.UserId == userId))
                return;
            db.UserProfiles.Add(new UserProfileEntity { UserId = userId, UserName = userId });
            db.SaveChanges();
        }

        [Fact]
        public void RecordStudyTime_And_Learn_ShouldAccumulateDurationAndItems()
        {
            using var agg = CreateAggregator();
            var user = "u_dur";
            EnsureUser(user);

            // 番茄钟完成 25 分钟 + 学习 1 项
            agg.RecordStudyTime(user, 25, "番茄钟", idempotencyKey: "pom-1");
            agg.RecordActivity(user, "Learn", "Math", 1, idempotencyKey: "learn-1");
            agg.Flush();

            var daily = agg.GetDailyOverview(user, DateTime.Today);

            daily.TimeSpentMinutes.Should().Be(25);
            daily.ItemsStudied.Should().Be(1);
        }

        [Fact]
        public void CorrectAndWrong_ShouldComputeAccuracy()
        {
            using var agg = CreateAggregator();
            var user = "u_acc";
            EnsureUser(user);

            agg.RecordActivity(user, "Correct", "Math", 3, idempotencyKey: "c1");
            agg.RecordActivity(user, "Wrong", "Math", 1, idempotencyKey: "w1");
            agg.Flush();

            var daily = agg.GetDailyOverview(user, DateTime.Today);

            daily.CorrectCount.Should().Be(3);
            daily.WrongCount.Should().Be(1);
            // 正确率 = 3/(3+1)=75
            daily.Accuracy.Should().Be(75);
        }

        [Fact]
        public void ConsecutiveStudyDays_ShouldComputeStreak()
        {
            using var agg = CreateAggregator();
            var user = "u_streak";
            var today = DateTime.Today;

            // 预置昨天、今天两条有学习行为的记录 → 连击 2
            using (var db = _dbFactory.CreateDbContext())
            {
                db.DailyRollups.Add(new DailyRollupEntity { UserId = user, Date = today.AddDays(-1), ItemsStudied = 5, TimeSpentMinutes = 10 });
                db.DailyRollups.Add(new DailyRollupEntity { UserId = user, Date = today, ItemsStudied = 5, TimeSpentMinutes = 10 });
                db.SaveChanges();
            }

            var weekly = agg.GetWeeklyOverview(user, today);

            weekly.StreakDays.Should().Be(2);
        }

        [Fact]
        public void NoStudy_ShouldReturnZeroStreak()
        {
            using var agg = CreateAggregator();
            var user = "u_no_streak";

            var weekly = agg.GetWeeklyOverview(user, DateTime.Today);

            weekly.StreakDays.Should().Be(0);
        }

        [Fact]
        public void Invalidate_ShouldDropSegmentedCache()
        {
            var cache = new FakeCache();
            using var agg = CreateAggregator(cache);
            var user = "u_cache";
            EnsureUser(user);
            agg.RecordActivity(user, "Learn", "Math", 1, idempotencyKey: "l1");
            agg.Flush();

            var daily = agg.GetDailyOverview(user, DateTime.Today);
            var cacheCountAfterFirst = cache.Count;
            cacheCountAfterFirst.Should().BeGreaterThan(0);

            // 命中缓存：再次读取返回同一实例
            var dailyAgain = agg.GetDailyOverview(user, DateTime.Today);
            ReferenceEquals(daily, dailyAgain).Should().BeTrue();

            // 失效某日分段后，再读应重算（新实例），且缓存被移除
            agg.Invalidate(user, DateTime.Today);
            var dailyAfterInvalidate = agg.GetDailyOverview(user, DateTime.Today);
            ReferenceEquals(daily, dailyAfterInvalidate).Should().BeFalse();
            cache.RemovedKeys.Should().NotBeEmpty();
        }

        /// <summary>测试用内存库工厂：同一连接共享同一 SQLite :memory: 库</summary>
        private sealed class InMemoryFactory : IDbContextFactory<AppDbContext>
        {
            private readonly DbContextOptions<AppDbContext> _options;
            public InMemoryFactory(DbContextOptions<AppDbContext> options) => _options = options;
            public AppDbContext CreateDbContext() => new(_options);
        }

        /// <summary>测试用 ICacheService 存根：跟踪 Set/Remove 以便断言缓存失效</summary>
        private sealed class FakeCache : ICacheService
        {
            private readonly Dictionary<string, object> _store = new();
            public List<string> RemovedKeys { get; } = new();

            public bool TryGet<T>(string key, out T value)
            {
                if (_store.TryGetValue(key, out var o) && o is T t) { value = t; return true; }
                value = default!;
                return false;
            }

            public Task<T?> TryGetAsync<T>(string key)
                => Task.FromResult(TryGet<T>(key, out var v) ? v : default);

            public void Set<T>(string key, T value, int? expirationMinutes = null) => _store[key] = value!;

            public Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, int? expirationMinutes = null)
                => throw new NotSupportedException();

            public void SetMany<T>(IDictionary<string, T> items, int? expirationMinutes = null)
            { foreach (var kv in items) _store[kv.Key] = kv.Value!; }

            public void Remove(string key) { _store.Remove(key); RemovedKeys.Add(key); }
            public void Clear() => _store.Clear();
            public void Persist() { }
            public Task<int> WarmupAsync(IDictionary<string, Func<Task>> warmupTasks, CancellationToken cancellationToken = default) => Task.FromResult(0);
            public (int Hits, int Misses) GetStatistics() => (0, 0);
            public int Count => _store.Count;
        }
    }
}