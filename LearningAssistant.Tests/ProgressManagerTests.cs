using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Models.User;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class ProgressManagerTests
    {
        private readonly Mock<IDataPersistenceService> _mockPersistenceService;
        private readonly ProgressManager _progressManager;

        public ProgressManagerTests()
        {
            _mockPersistenceService = new Mock<IDataPersistenceService>();
            _progressManager = new ProgressManager(_mockPersistenceService.Object);
        }

        [Fact]
        public void GetProgressState_ShouldReturnInitializedState()
        {
            var state = _progressManager.GetProgressState();

            state.Should().NotBeNull();
            state.KnownItems.Should().BeEmpty();
            state.UnknownItems.Should().BeEmpty();
            state.StudyModeIndex.Should().Be(0);
            state.QuickModeIndex.Should().Be(0);
            state.CorrectCount.Should().Be(0);
            state.TotalCount.Should().Be(0);
        }

        [Fact]
        public void ResetProgress_ShouldClearAllState()
        {
            _mockPersistenceService.Setup(p => p.GetKnownItems(It.IsAny<string>(), It.IsAny<SubCategoryType>()))
                .Returns(new List<string> { "Apple", "Banana" });
            _mockPersistenceService.Setup(p => p.GetUnknownItems(It.IsAny<string>(), It.IsAny<SubCategoryType>()))
                .Returns(new List<string> { "Cherry" });

            _progressManager.LoadProgress("test_user", SubCategoryType.EnglishWord);

            var stateBefore = _progressManager.GetProgressState();
            stateBefore.KnownItems.Should().HaveCount(2);
            stateBefore.UnknownItems.Should().HaveCount(1);

            _progressManager.ResetProgress();

            var stateAfter = _progressManager.GetProgressState();
            stateAfter.KnownItems.Should().BeEmpty();
            stateAfter.UnknownItems.Should().BeEmpty();
            stateAfter.StudyModeIndex.Should().Be(0);
            stateAfter.QuickModeIndex.Should().Be(0);
            stateAfter.CorrectCount.Should().Be(0);
            stateAfter.TotalCount.Should().Be(0);
        }

        [Fact]
        public void LoadProgress_ShouldLoadKnownAndUnknownItems()
        {
            var knownItems = new List<string> { "Apple", "Banana" };
            var unknownItems = new List<string> { "Cherry" };

            _mockPersistenceService.Setup(p => p.GetKnownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(knownItems);
            _mockPersistenceService.Setup(p => p.GetUnknownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(unknownItems);

            var profile = CreateTestProfile();
            _mockPersistenceService.Setup(p => p.LoadUserProfile("test_user"))
                .Returns(profile);

            _progressManager.LoadProgress("test_user", SubCategoryType.EnglishWord);

            var state = _progressManager.GetProgressState();
            state.KnownItems.Should().Contain(knownItems);
            state.UnknownItems.Should().Contain(unknownItems);
        }

        [Fact]
        public void LoadProgress_FromCategoryProgress_ShouldLoadProgress()
        {
            _mockPersistenceService.Setup(p => p.GetKnownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());
            _mockPersistenceService.Setup(p => p.GetUnknownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());

            var profile = CreateTestProfile();
            profile.LearningProgress.CategoryProgresses["EnglishWord"] = new CategoryProgress
            {
                CategoryName = "EnglishWord",
                KnownItems = new List<string> { "Apple", "Banana" },
                UnknownItems = new List<string> { "Cherry" },
                CorrectCount = 10,
                TotalTestCount = 15,
                LastResumeIndex = 5,
                QuickTestResumeIndex = 3
            };
            _mockPersistenceService.Setup(p => p.LoadUserProfile("test_user"))
                .Returns(profile);

            _progressManager.LoadProgress("test_user", SubCategoryType.EnglishWord);

            var state = _progressManager.GetProgressState();
            state.KnownItems.Should().HaveCount(2);
            state.UnknownItems.Should().HaveCount(1);
            state.CorrectCount.Should().Be(10);
            state.TotalCount.Should().Be(15);
            state.StudyModeIndex.Should().Be(5);
            state.QuickModeIndex.Should().Be(3);
        }

        [Fact]
        public void AddUnknownItem_ItemNotInEitherList_ShouldAddToUnknown()
        {
            _mockPersistenceService.Setup(p => p.GetKnownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());
            _mockPersistenceService.Setup(p => p.GetUnknownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());

            _progressManager.AddUnknownItem("test_user", "Apple", SubCategoryType.EnglishWord);

            _mockPersistenceService.Verify(p => p.UpsertLearningItemState("test_user", SubCategoryType.EnglishWord, "Apple", false), Times.Once);

            var state = _progressManager.GetProgressState();
            state.UnknownItems.Should().Contain("Apple");
        }

        [Fact]
        public void AddUnknownItem_ItemAlreadyInKnown_ShouldMoveToUnknown()
        {
            _mockPersistenceService.Setup(p => p.GetKnownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string> { "Apple", "Banana" });
            _mockPersistenceService.Setup(p => p.GetUnknownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());

            var profile = CreateTestProfile();
            profile.LearningProgress.CategoryProgresses["EnglishWord"] = new CategoryProgress
            {
                CategoryName = "EnglishWord",
                KnownItems = new List<string> { "Apple", "Banana" },
                UnknownItems = new List<string>()
            };
            _mockPersistenceService.Setup(p => p.LoadUserProfile("test_user"))
                .Returns(profile);

            _progressManager.AddUnknownItem("test_user", "Apple", SubCategoryType.EnglishWord);

            _mockPersistenceService.Verify(p => p.UpsertLearningItemState("test_user", SubCategoryType.EnglishWord, "Apple", false), Times.Once);
            _mockPersistenceService.Verify(p => p.SaveUserProfile(It.IsAny<UserProfile>()), Times.Once);

            var state = _progressManager.GetProgressState();
            state.KnownItems.Should().NotContain("Apple");
            state.UnknownItems.Should().Contain("Apple");
        }

        [Fact]
        public void AddUnknownItem_ItemAlreadyInUnknown_ShouldNotAddAgain()
        {
            _mockPersistenceService.Setup(p => p.GetKnownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string>());
            _mockPersistenceService.Setup(p => p.GetUnknownItems("test_user", SubCategoryType.EnglishWord))
                .Returns(new List<string> { "Apple" });

            _progressManager.AddUnknownItem("test_user", "Apple", SubCategoryType.EnglishWord);

            _mockPersistenceService.Verify(p => p.UpsertLearningItemState(It.IsAny<string>(), It.IsAny<SubCategoryType>(), It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public void SaveProgress_ShouldSaveStateToPersistence()
        {
            var profile = CreateTestProfile();
            _mockPersistenceService.Setup(p => p.LoadUserProfile("test_user"))
                .Returns(profile);

            var state = new StudyEngineState
            {
                KnownItems = new List<string> { "Apple", "Banana" },
                UnknownItems = new List<string> { "Cherry" },
                StudyModeIndex = 5,
                QuickModeIndex = 3,
                CorrectCount = 10,
                TotalCount = 15,
                CurrentMode = LearningModeType.Study
            };

            _progressManager.SaveProgress("test_user", SubCategoryType.EnglishWord, state);

            _mockPersistenceService.Verify(p => p.SaveUserProfile(It.IsAny<UserProfile>()), Times.Once);
            _mockPersistenceService.Verify(p => p.SyncCategoryProgressToLearningItemStates(
                "test_user", SubCategoryType.EnglishWord,
                It.Is<List<string>>(l => l.Count == 2),
                It.Is<List<string>>(l => l.Count == 1)), Times.Once);
        }

        [Fact]
        public void Constructor_WithNullPersistenceService_ShouldThrow()
        {
            Action act = () => new ProgressManager(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        private UserProfile CreateTestProfile()
        {
            return new UserProfile
            {
                UserId = "test_user",
                LearningProgress = new LearningProgress
                {
                    CategoryProgresses = new Dictionary<string, CategoryProgress>()
                }
            };
        }
    }
}
