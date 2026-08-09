using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.Pomodoro;
using LearningAssistant.Data.Database;
using LearningAssistant.Services.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Tests
{
    public class PomodoroServiceTests : IDisposable
    {
        private readonly Mock<IDbContextFactory<AppDbContext>> _mockDbContextFactory;
        private readonly Mock<IDataPersistenceService> _mockPersistence;
        private readonly Mock<ILogger<PomodoroService>> _mockLogger;
        private PomodoroService? _service;

        public PomodoroServiceTests()
        {
            _mockDbContextFactory = new Mock<IDbContextFactory<AppDbContext>>();
            _mockPersistence = new Mock<IDataPersistenceService>();
            _mockLogger = new Mock<ILogger<PomodoroService>>();

            _mockDbContextFactory.Setup(f => f.CreateDbContext())
                .Returns(() => new AppDbContext());
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        private PomodoroService CreateService()
        {
            return new PomodoroService(
                _mockDbContextFactory.Object,
                _mockPersistence.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void Constructor_WithNullDbContextFactory_ShouldThrow()
        {
            Action act = () => new PomodoroService(
                null!,
                _mockPersistence.Object,
                _mockLogger.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("dbContextFactory");
        }

        [Fact]
        public void Constructor_WithNullPersistenceService_ShouldThrow()
        {
            Action act = () => new PomodoroService(
                _mockDbContextFactory.Object,
                null!,
                _mockLogger.Object);

            act.Should().Throw<ArgumentNullException>().WithParameterName("persistenceService");
        }

        [Fact]
        public void Constructor_WithNullLogger_ShouldThrow()
        {
            Action act = () => new PomodoroService(
                _mockDbContextFactory.Object,
                _mockPersistence.Object,
                null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
        }

        [Fact]
        public void InitialState_ShouldBeIdle()
        {
            _service = CreateService();

            _service.CurrentState.Should().Be(PomodoroState.Idle);
            _service.TimeRemaining.Should().Be(TimeSpan.Zero);
            _service.TotalDuration.Should().Be(TimeSpan.Zero);
            _service.CompletedPomodoros.Should().Be(0);
            _service.TodayCompletedPomodoros.Should().Be(0);
        }

        [Fact]
        public void Start_ShouldChangeStateToStudying()
        {
            _service = CreateService();

            _service.Start();

            _service.CurrentState.Should().Be(PomodoroState.Studying);
            _service.TimeRemaining.Should().Be(TimeSpan.FromMinutes(25));
            _service.TotalDuration.Should().Be(TimeSpan.FromMinutes(25));
        }

        [Fact]
        public void StartWork_WithTaskName_ShouldSetCurrentTask()
        {
            _service = CreateService();

            _service.StartWork("Test Task");

            _service.CurrentState.Should().Be(PomodoroState.Studying);
        }

        [Fact]
        public void StartWork_WhenAlreadyStudying_ShouldNotChangeState()
        {
            _service = CreateService();
            _service.Start();

            _service.StartWork("Another Task");

            _service.CurrentState.Should().Be(PomodoroState.Studying);
        }

        [Fact]
        public void Pause_WhenStudying_ShouldChangeStateToPaused()
        {
            _service = CreateService();
            _service.Start();

            _service.Pause();

            _service.CurrentState.Should().Be(PomodoroState.Paused);
        }

        [Fact]
        public void Pause_WhenIdle_ShouldNotChangeState()
        {
            _service = CreateService();

            _service.Pause();

            _service.CurrentState.Should().Be(PomodoroState.Idle);
        }

        [Fact]
        public void Resume_WhenPaused_ShouldChangeStateToPrevious()
        {
            _service = CreateService();
            _service.Start();
            _service.Pause();

            _service.Resume();

            _service.CurrentState.Should().Be(PomodoroState.Studying);
        }

        [Fact]
        public void Resume_WhenIdle_ShouldNotChangeState()
        {
            _service = CreateService();

            _service.Resume();

            _service.CurrentState.Should().Be(PomodoroState.Idle);
        }

        [Fact]
        public void Stop_ShouldResetToIdle()
        {
            _service = CreateService();
            _service.Start();

            _service.Stop();

            _service.CurrentState.Should().Be(PomodoroState.Idle);
            _service.TimeRemaining.Should().Be(TimeSpan.Zero);
            _service.TotalDuration.Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void Reset_ShouldResetCompletedCount()
        {
            _service = CreateService();
            _service.Start();
            _service.Stop();

            _service.Reset();

            _service.CompletedPomodoros.Should().Be(0);
            _service.CurrentState.Should().Be(PomodoroState.Idle);
        }

        [Fact]
        public void StartShortBreak_ShouldChangeStateToShortBreak()
        {
            _service = CreateService();

            _service.StartShortBreak();

            _service.CurrentState.Should().Be(PomodoroState.ShortBreak);
            _service.TimeRemaining.Should().Be(TimeSpan.FromMinutes(5));
        }

        [Fact]
        public void StartLongBreak_ShouldChangeStateToLongBreak()
        {
            _service = CreateService();

            _service.StartLongBreak();

            _service.CurrentState.Should().Be(PomodoroState.LongBreak);
            _service.TimeRemaining.Should().Be(TimeSpan.FromMinutes(15));
        }

        [Fact]
        public void Skip_WhenStudying_ShouldStopTimer()
        {
            _service = CreateService();
            _service.Start();

            _service.Skip();

            _service.CurrentState.Should().Be(PomodoroState.Idle);
        }

        [Fact]
        public void UpdateSettings_ShouldUpdateSettings()
        {
            _service = CreateService();
            var newSettings = new PomodoroSettings
            {
                StudyMinutes = 30,
                ShortBreakMinutes = 10,
                LongBreakMinutes = 20,
                LongBreakInterval = 4
            };

            _service.UpdateSettings(newSettings);

            _service.Settings.StudyMinutes.Should().Be(30);
            _service.Settings.ShortBreakMinutes.Should().Be(10);
            _service.Settings.LongBreakMinutes.Should().Be(20);
        }

        [Fact]
        public void GetTodayStats_ShouldReturnDefaultStats()
        {
            _service = CreateService();

            var stats = _service.GetTodayStats();

            stats.Should().NotBeNull();
            stats.Date.Should().Be(DateTime.Today);
            stats.CompletedPomodoros.Should().Be(0);
        }

        [Fact]
        public void GetStatistics_ShouldReturnValidStatistics()
        {
            _service = CreateService();

            var stats = _service.GetStatistics();

            stats.Should().NotBeNull();
            stats.TodayCount.Should().Be(0);
            stats.WeekCount.Should().Be(0);
            stats.MonthCount.Should().Be(0);
            stats.TotalCount.Should().Be(0);
            stats.StreakDays.Should().Be(0);
        }

        [Fact]
        public void AddManualRecord_ShouldAddRecord()
        {
            _service = CreateService();

            _service.AddManualRecord(DateTime.Now, 25, "Manual Task");

            var records = _service.GetTodayRecords();
            records.Should().HaveCount(1);
            records[0].Task.Should().Be("Manual Task");
            records[0].Completed.Should().Be(true);
        }

        [Fact]
        public void DeleteRecord_WithNonExistentId_ShouldReturnFalse()
        {
            _service = CreateService();

            var result = _service.DeleteRecord("non_existent_id");

            result.Should().Be(false);
        }

        [Fact]
        public void DeleteRecord_WithExistingId_ShouldReturnTrue()
        {
            _service = CreateService();
            _service.AddManualRecord(DateTime.Now, 25, "Test Task");
            var records = _service.GetTodayRecords();
            var recordId = records[0].Id;

            var result = _service.DeleteRecord(recordId);

            result.Should().Be(true);
        }

        [Fact]
        public void GetRecords_WithDateRange_ShouldReturnFilteredRecords()
        {
            _service = CreateService();
            var today = DateTime.Today;
            _service.AddManualRecord(today, 25, "Today Task");
            _service.AddManualRecord(today.AddDays(-1), 25, "Yesterday Task");

            var records = _service.GetRecords(today, today.AddDays(1));

            records.Should().HaveCount(1);
            records[0].Task.Should().Be("Today Task");
        }

        [Fact]
        public void StateChangedEvent_ShouldBeRaisedOnStateChange()
        {
            _service = CreateService();
            bool eventRaised = false;
            PomodoroStateChangedEventArgs? args = null;

            _service.StateChanged += (sender, e) =>
            {
                eventRaised = true;
                args = e;
            };

            _service.Start();

            eventRaised.Should().Be(true);
            args.Should().NotBeNull();
            args!.OldState.Should().Be(PomodoroState.Idle);
            args.NewState.Should().Be(PomodoroState.Studying);
        }
    }
}