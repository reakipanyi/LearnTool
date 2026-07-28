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
        private LearningAnalyticsService CreateService()
        {
            var mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            var mockPersistence = new Mock<IDataPersistenceService>();
            var mockLogger = new Mock<ILogger<LearningAnalyticsService>>();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext(options));

            return new LearningAnalyticsService(
                mockLogger.Object,
                mockPersistence.Object,
                mockDbContextFactory.Object);
        }

        [Fact]
        public void RecordActivity_WithEmptyUserId_ShouldNotRecord()
        {
            var service = CreateService();
            service.RecordActivity("", "Learn", "EnglishWord");

            var stats = service.GetCategoryStats("");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithZeroCount_ShouldNotRecord()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord", 0);

            var stats = service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithNegativeCount_ShouldNotRecord()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord", -1);

            var stats = service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithEmptyActivityType_ShouldNotRecord()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "", "EnglishWord");

            var stats = service.GetCategoryStats("test_user");
            stats.Should().BeEmpty();
        }

        [Fact]
        public void RecordActivity_WithLearnType_ShouldIncrementItemsLearned()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord");

            var dailyStats = service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsLearned.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithReviewType_ShouldIncrementItemsReviewed()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Review", "EnglishWord");

            var dailyStats = service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsReviewed.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithCorrectType_ShouldIncrementCorrectCount()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Correct", "EnglishWord");

            var dailyStats = service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.CorrectCount.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithWrongType_ShouldIncrementWrongCount()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Wrong", "EnglishWord");

            var dailyStats = service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.WrongCount.Should().Be(1);
        }

        [Fact]
        public void RecordActivity_WithMultipleCounts_ShouldSumUp()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord", 5);

            var dailyStats = service.GetDailyStatistics("test_user", DateTime.Today);
            dailyStats.ItemsLearned.Should().Be(5);
        }

        [Fact]
        public void RecordActivity_WithCategory_ShouldUpdateCategoryStats()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord");
            service.RecordActivity("test_user", "Learn", "EnglishWord");

            var stats = service.GetCategoryStats("test_user");
            stats.Should().ContainKey("EnglishWord");
            stats["EnglishWord"].Should().Be(2);
        }

        [Fact]
        public void GetCategoryStats_WithNonExistentUser_ShouldReturnEmpty()
        {
            var service = CreateService();
            var stats = service.GetCategoryStats("non_existent_user");

            stats.Should().BeEmpty();
        }

        [Fact]
        public void GetStudyStreak_WithNoActivity_ShouldReturnZero()
        {
            var service = CreateService();
            var streak = service.GetStudyStreak("test_user");

            streak.Should().Be(0);
        }

        [Fact]
        public void GetStudyStreak_WithTodayActivity_ShouldReturnOne()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Learn", "EnglishWord");

            var streak = service.GetStudyStreak("test_user");

            streak.Should().Be(1);
        }

        [Fact]
        public void GetTotalStudyMinutes_WithNoData_ShouldReturnZero()
        {
            var service = CreateService();
            var minutes = service.GetTotalStudyMinutes("test_user", DateTime.Today.AddDays(-7));

            minutes.Should().Be(0);
        }

        [Fact]
        public void GetTotalLearnedItems_WithNoData_ShouldReturnZero()
        {
            var service = CreateService();
            var items = service.GetTotalLearnedItems("test_user", DateTime.Today.AddDays(-7));

            items.Should().Be(0);
        }

        [Fact]
        public void GetAccuracyRate_WithNoData_ShouldReturnZero()
        {
            var service = CreateService();
            var rate = service.GetAccuracyRate("test_user", DateTime.Today.AddDays(-7));

            rate.Should().Be(0);
        }

        [Fact]
        public void GetAccuracyRate_WithCorrectAndWrong_ShouldCalculateRate()
        {
            var service = CreateService();
            service.RecordActivity("test_user", "Correct", "EnglishWord", 8);
            service.RecordActivity("test_user", "Wrong", "EnglishWord", 2);

            var rate = service.GetAccuracyRate("test_user", DateTime.Today.AddDays(-7));

            rate.Should().Be(80);
        }

        [Fact]
        public void GetDailyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var service = CreateService();
            var stats = service.GetDailyStatistics("test_user", DateTime.Today);

            stats.Should().NotBeNull();
            stats.Date.Should().Be(DateTime.Today);
            stats.UserId.Should().Be("test_user");
            stats.ItemsLearned.Should().Be(0);
        }

        [Fact]
        public void GetWeeklyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var service = CreateService();
            var today = DateTime.Today;
            var weekNumber = ISOWeek.GetWeekOfYear(today);

            var stats = service.GetWeeklyStatistics("test_user", today.Year, weekNumber);

            stats.Should().NotBeNull();
            stats.Year.Should().Be(today.Year);
            stats.WeekNumber.Should().Be(weekNumber);
            stats.TotalItems.Should().Be(0);
        }

        [Fact]
        public void GetMonthlyStatistics_WithNoData_ShouldReturnEmptyStats()
        {
            var service = CreateService();
            var today = DateTime.Today;

            var stats = service.GetMonthlyStatistics("test_user", today.Year, today.Month);

            stats.Should().NotBeNull();
            stats.Year.Should().Be(today.Year);
            stats.Month.Should().Be(today.Month);
            stats.TotalItems.Should().Be(0);
        }

        [Fact]
        public void GetLearningTrend_WithDateRange_ShouldReturnDailyStats()
        {
            var service = CreateService();
            var startDate = DateTime.Today.AddDays(-3);
            var endDate = DateTime.Today;

            var trend = service.GetLearningTrend("test_user", startDate, endDate);

            trend.Should().HaveCount(4);
        }

        [Fact]
        public void CalculateRetentionRate_WithNullDbContext_ShouldReturnDefault()
        {
            var mockLogger = new Mock<ILogger<LearningAnalyticsService>>();
            var service = new LearningAnalyticsService(mockLogger.Object, null, null);

            var rate = service.CalculateRetentionRate("test_user");

            rate.Should().Be(0.5);
        }
    }
}
