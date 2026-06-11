using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Models.Learning;

namespace LearningAssistant.Tests
{
    /// <summary>
    /// 测试 StudyEngine 服务
    /// </summary>
    public class StudyEngineTests
    {
        private readonly Mock<IDataPersistenceService> _mockPersistenceService;
        private readonly Mock<IContentLoaderService> _mockContentLoaderService;
        private readonly StudyEngine _studyEngine;

        public StudyEngineTests()
        {
            _mockPersistenceService = new Mock<IDataPersistenceService>();
            _mockContentLoaderService = new Mock<IContentLoaderService>();
            _studyEngine = new StudyEngine(_mockPersistenceService.Object, _mockContentLoaderService.Object);
        }

        private List<object> CreateTestItems()
        {
            return new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };
        }

        private List<object> CreateSingleTestItem()
        {
            return new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/")
            };
        }

        private void SetupAndInitializeWithItems(List<object> items, string mode = "学习模式")
        {
            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(items);

            _studyEngine.Initialize("test_user", "English", "Words", "", mode, "Sequential");
        }

        [Fact]
        public void Initialize_WithValidData_ShouldLoadItems()
        {
            // Arrange
            var testItems = CreateTestItems();

            // Act
            SetupAndInitializeWithItems(testItems);

            // Assert
            _studyEngine.TotalCount.Should().Be(2);
            _mockContentLoaderService.Verify(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void GetCurrentItem_AfterInitialize_ShouldReturnFirstItem()
        {
            // Arrange
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            // Act
            var currentItem = _studyEngine.GetCurrentItem();

            // Assert
            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void HasNext_WithMultipleItems_ShouldReturnTrue()
        {
            // Arrange
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            // Act
            var hasNext = _studyEngine.HasNext();

            // Assert
            hasNext.Should().BeTrue();
        }

        [Fact]
        public void MoveNext_ShouldAdvanceToNextItem()
        {
            // Arrange
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            // Act
            _studyEngine.MoveNext();
            var currentItem = _studyEngine.GetCurrentItem();

            // Assert
            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Banana");
        }

        [Fact]
        public void MarkCurrentAsKnown_ShouldAddToKnownList()
        {
            // Arrange
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            // Act
            _studyEngine.MarkCurrentAsKnown();

            // Assert
            _studyEngine.KnownItems.Should().Contain("Apple");
        }

        [Fact]
        public void GetStatistics_WithCorrectAnswers_ShouldCalculateAccuracy()
        {
            // Arrange
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems, "快速模式");
            _studyEngine.MarkCurrentAsKnown();

            // Act
            var stats = _studyEngine.GetStatistics();

            // Assert
            stats.AccuracyRate.Should().BeGreaterThan(0);
        }

        // 边缘情况测试

        [Fact]
        public void Initialize_WithEmptyItemsList_ShouldNotThrow()
        {
            // Arrange
            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<object>());

            // Act
            Action act = () => _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

            // Assert
            act.Should().NotThrow();
            _studyEngine.TotalCount.Should().Be(0);
        }

        [Fact]
        public void GetCurrentItem_BeforeInitialize_ShouldReturnNull()
        {
            // Act
            var currentItem = _studyEngine.GetCurrentItem();

            // Assert
            currentItem.Should().BeNull();
        }

        [Fact]
        public void MoveNext_AtEndOfList_ShouldNotAdvance()
        {
            // Arrange
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            // Act
            _studyEngine.MoveNext(); // 第一次移动到第一个
            var hasNextAfterFirst = _studyEngine.HasNext();
            
            _studyEngine.MoveNext(); // 尝试再移动
            var currentItem = _studyEngine.GetCurrentItem();

            // Assert
            hasNextAfterFirst.Should().BeFalse();
            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void MarkCurrentAsKnown_DuplicateItem_ShouldOnlyAddOnce()
        {
            // Arrange
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            // Act
            _studyEngine.MarkCurrentAsKnown();
            _studyEngine.MarkCurrentAsKnown(); // 重复标记

            // Assert
            _studyEngine.KnownItems.Should().HaveCount(1);
        }

        [Fact]
        public void MarkCurrentAsUnknown_ShouldAddToUnknownList()
        {
            // Arrange
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            // Act
            _studyEngine.MarkCurrentAsUnknown();

            // Assert
            _studyEngine.UnknownItems.Should().Contain("Apple");
        }
    }

    /// <summary>
    /// 测试用的学习项类
    /// </summary>
    public class TestLearningItem : LearningItem
    {
        private readonly string _mainContent;
        private readonly string _displayText;
        private readonly string _pronunciation;

        public TestLearningItem(string mainContent, string displayText, string pronunciation)
        {
            _mainContent = mainContent;
            _displayText = displayText;
            _pronunciation = pronunciation;
        }

        public override string GetMainContent() => _mainContent;
        public override string GetDisplayText() => _displayText;
        public override string GetPronunciation() => _pronunciation;
    }
}
