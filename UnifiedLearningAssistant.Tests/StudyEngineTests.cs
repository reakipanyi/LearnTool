using Moq;
using Xunit;
using FluentAssertions;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Services.Persistence;
using UnifiedLearningAssistant.Models.Learning;

namespace UnifiedLearningAssistant.Tests
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

        [Fact]
        public void Initialize_WithValidData_ShouldLoadItems()
        {
            // Arrange
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            // Act
            _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

            // Assert
            _studyEngine.TotalCount.Should().Be(2);
            _mockContentLoaderService.Verify(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void GetCurrentItem_AfterInitialize_ShouldReturnFirstItem()
        {
            // Arrange
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

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
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

            // Act
            var hasNext = _studyEngine.HasNext();

            // Assert
            hasNext.Should().BeTrue();
        }

        [Fact]
        public void MoveNext_ShouldAdvanceToNextItem()
        {
            // Arrange
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

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
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            _studyEngine.Initialize("test_user", "English", "Words", "", "学习模式", "Sequential");

            // Act
            _studyEngine.MarkCurrentAsKnown();

            // Assert
            _studyEngine.KnownItems.Should().Contain("Apple");
        }

        [Fact]
        public void GetStatistics_WithCorrectAnswers_ShouldCalculateAccuracy()
        {
            // Arrange
            var testItems = new List<object>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/")
            };

            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(testItems);

            _studyEngine.Initialize("test_user", "English", "Words", "", "快速模式", "Sequential");
            _studyEngine.MarkCurrentAsKnown();

            // Act
            var stats = _studyEngine.GetStatistics();

            // Assert
            stats.AccuracyRate.Should().BeGreaterThan(0);
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
