using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LearningAssistant.Tests
{
    public class LearningAnalyticsServiceTests
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<LearningAnalyticsService>> _mockLogger;
        private readonly LearningAnalyticsService _service;

        public LearningAnalyticsServiceTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<LearningAnalyticsService>>();

            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext());

            _service = new LearningAnalyticsService(
                _mockLogger.Object,
                _mockPersistence.Object,
                _mockDbContextFactory.Object);
        }

        [Fact]
        public void RecordActivity_WithEmptyUserId_ShouldNotRecord()
        {
            _service.RecordActivity("", "Learn", "EnglishWord");

            var stats = _service.GetCategoryStats("");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithZeroCount_ShouldNotRecord()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord", 0);

            var stats = _service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithNegativeCount_ShouldNotRecord()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord", -1);

            var stats = _service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithEmptyActivityType_ShouldNotRecord()
        {
            _service.RecordActivity("test_user", "", "EnglishWord");

            var stats = _service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithLearnType_ShouldIncrementItemsLearned()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord");

            var dailyStats = _service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsLearned.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithReviewType_ShouldIncrementItemsReviewed()
        {
            _service.RecordActivity("test_user", "Review", "EnglishWord");

            var dailyStats = _service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsReviewed.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithCorrectType_ShouldIncrementCorrectCount()
        {
            _service.RecordActivity("test_user", "Correct", "EnglishWord");

            var dailyStats = _service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.CorrectCount.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithWrongType_ShouldIncrementWrongCount()
        {
            _service.RecordActivity("test_user", "Wrong", "EnglishWord");

            var dailyStats = _service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.WrongCount.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithMultipleCounts_ShouldSumUp()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord", 5);

            var dailyStats = _service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsLearned.Should().Be(5);
        }

        [Fact]
        public void RecordActivity_WithCategory_ShouldUpdateCategoryStats()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord");
            _service.RecordActivity("test_user", "Learn", "EnglishWord");

            var stats = _service.GetCategoryStats("test_user");
            stats.Should().ContainKey("EnglishWord");
            stats["EnglishWord"].Should().Be(2);
        }

        [Fact]
        public void GetCategoryStats_WithNonExistentUser_ShouldReturnEmpty()
        {
            var stats = _service.GetCategoryStats("non_existent_user");

            stats.Should().BeEmpty();
        }

        [Fact]
        public void GetStudyStreak_WithNoActivity_ShouldReturnZero()
        {
            var streak = _service.GetStudyStreak("test_user");

            streak.Should().Be(0);
        }

        [Fact]
        public void GetStudyStreak_WithTodayActivity_ShouldReturnOne()
        {
            _service.RecordActivity("test_user", "Learn", "EnglishWord");

            var streak = _service.GetStudyStreak("test_user");

            streak.Should().Be(1);
        }

        [Fact]
        public void GetTotalStudyMinutes_WithNoData_ShouldReturnZero()
        {
            var minutes = _service.GetTotalStudyMinutes("test_user", DateTime.Today.AddDays(-7));

            minutes.Should().Be(0);
        }

        [Fact]
        public void GetTotalLearnedItems_WithNoData_ShouldReturnZero()
        {
            var items = _service.GetTotalLearnedItems("test_user", DateTime.Today.AddDays(-7));

            items.Should().Be(0);
        }

        [Fact]
        public void GetAccuracyRate_WithNoData_ShouldReturnZero()
        {
            var rate = _service.GetAccuracyRate("test_user", DateTime.Today.AddDays(-7));

            rate.Should().Be(0);
        }

        [Fact]
        public void GetAccuracyRate_WithCorrectAndWrong_ShouldCalculateRate()
        {
            _service.RecordActivity("test_user", "Correct", "EnglishWord", 8);
            _service.RecordActivity("test_user", "Wrong", "EnglishWord", 2);

            var rate = _service.GetAccuracyRate("test_user", DateTime.Today.AddDays(-7));

            rate.Should().Be(80);
        }

        [Fact]
        public void GetDailyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var stats = _service.GetDailyStatistics("test_user", DateTime.Today);

            stats.Should().NotBeNull();
            stats.Date.Should().Be(DateTime.Today);
            stats.UserId.Should().Be("test_user");
            stats.ItemsLearned.Should().Be(0);
        }

        [Fact]
        public void GetWeeklyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var today = DateTime.Today;
            var weekNumber = ISOWeek.GetWeekOfYear(today);

            var stats = _service.GetWeeklyStatistics("test_user", today.Year, weekNumber);

            stats.Should().NotBeNull();
            stats.Year.Should().Be(today.Year);
            stats.WeekNumber.Should().Be(weekNumber);
            stats.TotalItems.Should().Be(0);
        }

        [Fact]
        public void GetMonthlyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var today = DateTime.Today;

            var stats = _service.GetMonthlyStatistics("test_user", today.Year, today.Month);

            stats.Should().NotBeNull();
            stats.Year.Should().Be(today.Year);
            stats.Month.Should().Be(today.Month);
            stats.TotalItems.Should().Be(0);
        }

        [Fact]
        public void GetLearningTrend_WithDateRange_ShouldReturnDailyStats()
        {
            var startDate = DateTime.Today.AddDays(-3);
            var endDate = DateTime.Today;

            var trend = _service.GetLearningTrend("test_user", startDate, endDate);

            trend.Should().HaveCount(4);
        }

        [Fact]
        public void CalculateRetentionRate_WithNullDbContext_ShouldReturnDefault()
        {
            var service = new LearningAnalyticsService(_mockLogger.Object, null, null);

            var rate = service.CalculateRetentionRate("test_user");

            rate.Should().Be(0.5);
        }

        [Fact]
        public void GenerateForgettingCurve_WithNullDbContext_ShouldReturnDefaultCurve()
        {
            var service = new LearningAnalyticsService(_mockLogger.Object, null, null);

            var curve = service.GenerateForgettingCurve("test_user", 7);

            curve.Should().HaveCount(8);
        }

        [Fact]
        public void PredictFutureWorkload_WithNullDbContext_ShouldReturnEmptyWorkload()
        {
            var service = new LearningAnalyticsService(_mockLogger.Object, null, null);

            var workload = service.PredictFutureWorkload("test_user", 7);

            workload.Should().HaveCount(7);
        }

        [Fact]
        public void GetReviewEfficiencyStats_WithNullDbContext_ShouldReturnEmptyStats()
        {
            var service = new LearningAnalyticsService(_mockLogger.Object, null, null);

            var stats = service.GetReviewEfficiencyStats("test_user");

            stats.Should().NotBeNull();
            stats.TotalReviews.Should().Be(0);
        }

        [Fact]
        public void GetWeeklyHeatmap_WithNullDbContext_ShouldReturnEmpty()
        {
            var service = new LearningAnalyticsService(_mockLogger.Object, null, null);

            var heatmap = service.GetWeeklyHeatmap("test_user", 4);

            heatmap.Should().BeEmpty();
        }
    }
}