using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Common.Events;

namespace LearningAssistant.Tests
{
    public class AchievementServiceTests : IDisposable
    {
        private readonly Mock<IEventBus> _mockEventBus;
        private AchievementService? _service;

        public AchievementServiceTests()
        {
            _mockEventBus = new Mock<IEventBus>();
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        private AchievementService CreateService()
        {
            return new AchievementService(_mockEventBus.Object);
        }

        [Fact]
        public void Constructor_WithNullEventBus_ShouldThrow()
        {
            Action act = () => new AchievementService(null!);

            act.Should().Throw<ArgumentNullException>().WithParameterName("eventBus");
        }

        [Fact]
        public void GetAllAchievements_ShouldReturnAllAchievements()
        {
            _service = CreateService();

            var achievements = _service.GetAllAchievements();

            achievements.Should().NotBeEmpty();
        }

        [Fact]
        public void GetUnlockedAchievements_WithNoProgress_ShouldReturnEmpty()
        {
            _service = CreateService();

            var unlocked = _service.GetUnlockedAchievements();

            unlocked.Should().BeEmpty();
        }

        [Fact]
        public void GetLockedAchievements_WithNoProgress_ShouldReturnAll()
        {
            _service = CreateService();
            var all = _service.GetAllAchievements();

            var locked = _service.GetLockedAchievements();

            locked.Should().HaveCount(all.Count);
        }

        [Fact]
        public void LoadProgress_ShouldLoadUnlockedAchievements()
        {
            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>
                {
                    new UnlockedAchievement { Id = "first_steps", UnlockedAt = DateTime.Now.AddDays(-1) }
                }
            };

            _service.LoadProgress(profile);

            var unlocked = _service.GetUnlockedAchievements();
            unlocked.Should().HaveCount(1);
            unlocked[0].Id.Should().Be("first_steps");
        }

        [Fact]
        public void CheckAndUnlockAchievements_WithNewRequirement_ShouldUnlock()
        {
            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress
                {
                    CategoryProgresses = new Dictionary<string, CategoryProgress>
                    {
                        { "EnglishWord", new CategoryProgress { TotalTestCount = 100, CorrectCount = 50 } }
                    }
                }
            };

            _service.LoadProgress(profile);
            _service.CheckAndUnlockAchievements(profile, profile.LearningProgress);

            var unlocked = _service.GetUnlockedAchievements();
            unlocked.Should().NotBeEmpty();
        }

        [Fact]
        public void AchievementUnlockedEvent_ShouldBeRaisedOnUnlock()
        {
            _service = CreateService();
            bool eventRaised = false;
            Achievement? unlockedAchievement = null;

            _service.AchievementUnlocked += (sender, e) =>
            {
                eventRaised = true;
                unlockedAchievement = e.Achievement;
            };

            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress
                {
                    CategoryProgresses = new Dictionary<string, CategoryProgress>
                    {
                        { "EnglishWord", new CategoryProgress { TotalTestCount = 1000, CorrectCount = 500 } }
                    }
                }
            };

            _service.LoadProgress(profile);
            _service.CheckAndUnlockAchievements(profile, profile.LearningProgress);

            eventRaised.Should().Be(true);
            unlockedAchievement.Should().NotBeNull();
            unlockedAchievement!.IsUnlocked.Should().Be(true);
        }

        [Fact]
        public void EventBusPublish_ShouldBeCalledOnUnlock()
        {
            _service = CreateService();

            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress
                {
                    CategoryProgresses = new Dictionary<string, CategoryProgress>
                    {
                        { "EnglishWord", new CategoryProgress { TotalTestCount = 1000 } }
                    }
                }
            };

            _service.LoadProgress(profile);
            _service.CheckAndUnlockAchievements(profile, profile.LearningProgress);

            _mockEventBus.Verify(e => e.Publish(It.IsAny<AchievementUnlockedEvent>()), Times.AtLeastOnce);
        }

        [Fact]
        public void OnItemLearned_ShouldUpdateProgress()
        {
            Action<ItemLearnedEvent>? itemLearnedHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<ItemLearnedEvent>>()))
                .Callback<Action<ItemLearnedEvent>>(handler => itemLearnedHandler = handler);

            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress()
            };

            _service.LoadProgress(profile);
            itemLearnedHandler?.Invoke(new ItemLearnedEvent { UserId = "test_user" });

            profile.LearningProgress.ComputedTotalItemsStudied.Should().Be(1);
            profile.LearningProgress.ComputedTotalItemsMastered.Should().Be(1);
        }

        [Fact]
        public void OnItemWrong_ShouldUpdateProgress()
        {
            Action<ItemWrongEvent>? itemWrongHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<ItemWrongEvent>>()))
                .Callback<Action<ItemWrongEvent>>(handler => itemWrongHandler = handler);

            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress()
            };

            _service.LoadProgress(profile);
            itemWrongHandler?.Invoke(new ItemWrongEvent { UserId = "test_user" });

            profile.LearningProgress.ComputedTotalItemsStudied.Should().Be(1);
        }

        [Fact]
        public void OnFeynmanCompleted_ShouldUpdateCount()
        {
            Action<FeynmanCompletedEvent>? feynmanHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<FeynmanCompletedEvent>>()))
                .Callback<Action<FeynmanCompletedEvent>>(handler => feynmanHandler = handler);

            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                UnlockedAchievements = new List<UnlockedAchievement>(),
                LearningProgress = new LearningProgress
                {
                    FeynmanCompletedCount = 0
                }
            };

            _service.LoadProgress(profile);
            feynmanHandler?.Invoke(new FeynmanCompletedEvent { UserId = "test_user" });

            profile.LearningProgress.FeynmanCompletedCount.Should().Be(1);
        }

        [Fact]
        public void OnItemLearned_WithDifferentUserId_ShouldNotUpdate()
        {
            Action<ItemLearnedEvent>? itemLearnedHandler = null;
            _mockEventBus.Setup(e => e.Subscribe(It.IsAny<Action<ItemLearnedEvent>>()))
                .Callback<Action<ItemLearnedEvent>>(handler => itemLearnedHandler = handler);

            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                LearningProgress = new LearningProgress()
            };

            _service.LoadProgress(profile);
            itemLearnedHandler?.Invoke(new ItemLearnedEvent { UserId = "other_user" });

            profile.LearningProgress.ComputedTotalItemsStudied.Should().Be(0);
        }

        [Fact]
        public void Dispose_ShouldUnsubscribeFromEvents()
        {
            _service = CreateService();
            var profile = new UserProfile
            {
                UserId = "test_user",
                LearningProgress = new LearningProgress()
            };
            _service.LoadProgress(profile);

            _service.Dispose();

            _mockEventBus.Verify(e => e.Unsubscribe<ItemLearnedEvent>(It.IsAny<Action<ItemLearnedEvent>>()), Times.AtLeastOnce);
            _mockEventBus.Verify(e => e.Unsubscribe<ItemWrongEvent>(It.IsAny<Action<ItemWrongEvent>>()), Times.AtLeastOnce);
            _mockEventBus.Verify(e => e.Unsubscribe<FeynmanCompletedEvent>(It.IsAny<Action<FeynmanCompletedEvent>>()), Times.AtLeastOnce);
        }
    }
}