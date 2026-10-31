using FluentAssertions;
using LearningAssistant.Models.Home;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Gamification;
using LearningAssistant.Services.Learning;
using Moq;
using Xunit;

namespace LearningAssistant.Core.Tests;

/// <summary>
/// 首页快照聚合服务测试（07 方案 批次 A）。
/// </summary>
public class HomeSnapshotServiceTests
{
    [Fact]
    public void GetSnapshot_AggregatesAllSources()
    {
        var gamification = new Mock<IGamificationService>();
        gamification.SetupGet(g => g.XP).Returns(120);
        gamification.SetupGet(g => g.CurrentLevel).Returns(3);
        gamification.SetupGet(g => g.XPToNextLevel).Returns(80);
        gamification.Setup(g => g.GetDailyChallenges()).Returns(new List<Challenge>
        {
            new Challenge { Id = "c1", Completed = true },
            new Challenge { Id = "c2", Completed = false }
        });

        var notes = new Mock<INoteService>();
        notes.Setup(n => n.GetNoteCount("u1")).Returns(2);
        notes.Setup(n => n.GetNotes("u1", It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new List<NoteItem>
            {
                new NoteItem { CreatedAt = DateTime.Today },
                new NoteItem { CreatedAt = DateTime.Today.AddDays(-1) }
            });

        var spacedRepetition = new Mock<ISpacedRepetitionService>();
        spacedRepetition.Setup(s => s.GetItemsDueForReview("u1", It.IsAny<DateTime?>()))
            .Returns(new List<ReviewItem> { new ReviewItem(), new ReviewItem(), new ReviewItem() });

        var goals = new Mock<ILearningGoalService>();
        goals.Setup(g => g.GetDailyGoal("u1"))
            .Returns(new DailyGoal { UserId = "u1", TargetItems = 20, CompletedItems = 5 });

        var aggregator = new Mock<ILearningStatsAggregator>();
        aggregator.Setup(a => a.GetDailyOverview("u1", DateTime.Today))
            .Returns(new DailyOverview { TimeSpentMinutes = 45, StreakDays = 7 });

        var service = new HomeSnapshotService(
            gamification.Object,
            notes.Object,
            spacedRepetition.Object,
            goals.Object,
            aggregator.Object);

        var snapshot = service.GetSnapshot("u1");

        snapshot.TodayStudyMinutes.Should().Be(45);
        snapshot.StreakDays.Should().Be(7);
        snapshot.TotalXp.Should().Be(120);
        snapshot.Level.Should().Be(3);
        snapshot.XpToNextLevel.Should().Be(80);
        snapshot.CompletedChallenges.Should().Be(1);
        snapshot.TotalChallenges.Should().Be(2);
        snapshot.NoteCount.Should().Be(2);
        snapshot.TodayNewNotes.Should().Be(1);
        snapshot.DueReviewCount.Should().Be(3);
        snapshot.GoalCompleted.Should().Be(5);
        snapshot.GoalTarget.Should().Be(20);
        snapshot.GoalPercent.Should().Be(25);
        snapshot.IsBrandNew.Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_EmptyUserId_ReturnsEmptySnapshot()
    {
        var gamification = new Mock<IGamificationService>();
        var service = new HomeSnapshotService(gamification.Object);

        var snapshot = service.GetSnapshot(" ");

        snapshot.Should().Be(HomeSnapshot.Empty);
    }

    [Fact]
    public void GetSnapshot_WithoutOptionalServices_StillReturnsGamificationData()
    {
        var gamification = new Mock<IGamificationService>();
        gamification.SetupGet(g => g.XP).Returns(50);
        gamification.SetupGet(g => g.CurrentLevel).Returns(2);
        gamification.SetupGet(g => g.XPToNextLevel).Returns(50);
        gamification.Setup(g => g.GetDailyChallenges()).Returns(new List<Challenge>());

        var service = new HomeSnapshotService(gamification.Object);

        var snapshot = service.GetSnapshot("u1");

        snapshot.TotalXp.Should().Be(50);
        snapshot.Level.Should().Be(2);
        snapshot.NoteCount.Should().Be(0);
        snapshot.DueReviewCount.Should().Be(0);
        snapshot.GoalTarget.Should().Be(0);
    }
}
