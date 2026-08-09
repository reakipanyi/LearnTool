using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.Learning;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Common.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class LearningGoalServiceTests : IDisposable
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<LearningGoalService>> _mockLogger;
        private readonly Mock<IEventBus> _mockEventBus;
        private LearningGoalService? _service;

        public LearningGoalServiceTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<LearningGoalService>>();
            _mockEventBus = new Mock<IEventBus>();

            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext());
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        private LearningGoalService CreateService()
        {
            return new LearningGoalService(
                _mockDbContextFactory.Object,
                _mockPersistence.Object,
                _mockLogger.Object,
                _mockEventBus.Object);
        }

        [Fact]
        public void Constructor_WithNullDbContextFactory_ShouldThrow()
        {
            Action action = () => new LearningGoalService(
                null!,
                _mockPersistence.Object,
                _mockLogger.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNullPersistence_ShouldThrow()
        {
            Action action = () => new LearningGoalService(
                _mockDbContextFactory.Object,
                null!,
                _mockLogger.Object);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Action action = () => new LearningGoalService(
                _mockDbContextFactory.Object,
                _mockPersistence.Object,
                null!);

            action.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void SetDailyGoal_ShouldSetGoal()
        {
            _service = CreateService();
            _service.SetDailyGoal("test_user", 20);

            var goal = _service.GetDailyGoal("test_user");
            goal.Should().NotBeNull();
            goal!.TargetItems.Should().Be(20);
        }

        [Fact]
        public void GetDailyGoal_WithNoGoal_ShouldReturnNull()
        {
            _service = CreateService();

            var goal = _service.GetDailyGoal("unknown_user");
            goal.Should().BeNull();
        }

        [Fact]
        public void GetTodayProgress_WithNoProgress_ShouldReturnZero()
        {
            _service = CreateService();

            var progress = _service.GetTodayProgress("test_user");
            progress.Should().Be(0);
        }

        [Fact]
        public void IsDailyGoalCompleted_WithNoGoal_ShouldReturnFalse()
        {
            _service = CreateService();

            var completed = _service.IsDailyGoalCompleted("test_user");
            completed.Should().Be(false);
        }

        [Fact]
        public void GetGoalHistory_WithNoHistory_ShouldReturnEmpty()
        {
            _service = CreateService();

            var history = _service.GetGoalHistory("test_user");
            history.Should().BeEmpty();
        }

        [Fact]
        public void GetGoals_WithNoGoals_ShouldReturnEmpty()
        {
            _service = CreateService();

            var goals = _service.GetGoals("test_user");
            goals.Should().BeEmpty();
        }

        [Fact]
        public void UpdateGoal_ShouldUpdateExistingGoal()
        {
            _service = CreateService();
            var goal = new LearningGoal
            {
                Type = GoalType.DailyStudyMinutes,
                TargetValue = 30,
                Enabled = true
            };

            _service.UpdateGoal("test_user", goal);

            var goals = _service.GetGoals("test_user");
            goals.Should().HaveCount(1);
            goals[0].TargetValue.Should().Be(30);
        }

        [Fact]
        public void SetGoalEnabled_ShouldToggleEnabledState()
        {
            _service = CreateService();
            var goal = new LearningGoal
            {
                Type = GoalType.DailyStudyItems,
                TargetValue = 20,
                Enabled = true
            };
            _service.UpdateGoal("test_user", goal);

            _service.SetGoalEnabled("test_user", GoalType.DailyStudyItems, false);

            var goals = _service.GetGoals("test_user");
            goals[0].Enabled.Should().Be(false);
        }

        [Fact]
        public void GetTodayProgressList_WithNoProgress_ShouldReturnEmpty()
        {
            _service = CreateService();

            var progressList = _service.GetTodayProgressList("test_user");
            progressList.Should().BeEmpty();
        }

        [Fact]
        public void UpdateStudyMinutes_ShouldIncreaseMinutes()
        {
            _service = CreateService();
            _service.UpdateStudyMinutes("test_user", 25);

            var progressList = _service.GetTodayProgressList("test_user");
            var studyMinutesProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyStudyMinutes);
            studyMinutesProgress.Should().NotBeNull();
            studyMinutesProgress!.CurrentValue.Should().Be(25);
        }

        [Fact]
        public void IncrementStudyItems_ShouldIncreaseCount()
        {
            _service = CreateService();
            _service.IncrementStudyItems("test_user", 5);

            var progressList = _service.GetTodayProgressList("test_user");
            var studyItemsProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyStudyItems);
            studyItemsProgress.Should().NotBeNull();
            studyItemsProgress!.CurrentValue.Should().Be(5);
        }

        [Fact]
        public void IncrementReviewItems_ShouldIncreaseCount()
        {
            _service = CreateService();
            _service.IncrementReviewItems("test_user", 3);

            var progressList = _service.GetTodayProgressList("test_user");
            var reviewItemsProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyReviewItems);
            reviewItemsProgress.Should().NotBeNull();
            reviewItemsProgress!.CurrentValue.Should().Be(3);
        }

        [Fact]
        public void CheckGoalCompletion_ShouldNotThrow()
        {
            _service = CreateService();

            Action action = () => _service.CheckGoalCompletion("test_user");
            action.Should().NotThrow();
        }

        [Fact]
        public void GetDailyRecords_WithNoRecords_ShouldReturnEmpty()
        {
            _service = CreateService();

            var records = _service.GetDailyRecords("test_user");
            records.Should().BeEmpty();
        }

        [Fact]
        public void GetStreakInfo_WithNoData_ShouldReturnZero()
        {
            _service = CreateService();

            var streak = _service.GetStreakInfo("test_user");
            streak.CurrentStreak.Should().Be(0);
            streak.LongestStreak.Should().Be(0);
        }

        [Fact]
        public void GoalCompletedEvent_ShouldBeRaisedOnCompletion()
        {
            _service = CreateService();
            bool eventRaised = false;
            GoalType? raisedGoalType = null;

            _service.GoalCompleted += (sender, e) =>
            {
                eventRaised = true;
                raisedGoalType = e;
            };

            var goal = new LearningGoal
            {
                Type = GoalType.DailyStudyItems,
                TargetValue = 1,
                Enabled = true
            };
            _service.UpdateGoal("test_user", goal);

            _service.IncrementStudyItems("test_user", 1);
            _service.CheckGoalCompletion("test_user");

            eventRaised.Should().Be(true);
            raisedGoalType.Should().Be(GoalType.DailyStudyItems);
        }

        [Fact]
        public void OnPomodoroCompleted_ShouldUpdateStudyMinutes()
        {
            Action<PomodoroCompletedEvent>? pomodoroHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<PomodoroCompletedEvent>>()))
                .Callback<Action<PomodoroCompletedEvent>>(handler => pomodoroHandler = handler);

            _service = CreateService();
            pomodoroHandler?.Invoke(new PomodoroCompletedEvent { UserId = "test_user", DurationMinutes = 25 });

            var progressList = _service.GetTodayProgressList("test_user");
            var studyMinutesProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyStudyMinutes);
            studyMinutesProgress.Should().NotBeNull();
            studyMinutesProgress!.CurrentValue.Should().Be(25);
        }

        [Fact]
        public void OnItemLearned_ShouldIncrementStudyItems()
        {
            Action<ItemLearnedEvent>? itemLearnedHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<ItemLearnedEvent>>()))
                .Callback<Action<ItemLearnedEvent>>(handler => itemLearnedHandler = handler);

            _service = CreateService();
            itemLearnedHandler?.Invoke(new ItemLearnedEvent { UserId = "test_user" });

            var progressList = _service.GetTodayProgressList("test_user");
            var studyItemsProgress = progressList.FirstOrDefault(p => p.Type == GoalType.DailyStudyItems);
            studyItemsProgress.Should().NotBeNull();
            studyItemsProgress!.CurrentValue.Should().Be(1);
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            _service = CreateService();
            _service.Dispose();

            _mockEventBus.Verify(e => e.Unsubscribe<PomodoroCompletedEvent>(It.IsAny<Action<PomodoroCompletedEvent>>()), Times.AtLeastOnce);
            _mockEventBus.Verify(e => e.Unsubscribe<ItemLearnedEvent>(It.IsAny<Action<ItemLearnedEvent>>()), Times.AtLeastOnce);
        }
    }
}