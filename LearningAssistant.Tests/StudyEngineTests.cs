using Moq;
using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Models.Learning;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class StudyEngineTests
    {
        private readonly Mock<IDataPersistenceService> _mockPersistenceService;
        private readonly Mock<IContentLoaderService> _mockContentLoaderService;
        private readonly Mock<IProgressManager> _mockProgressManager;
        private readonly Mock<IStudyListProcessor> _mockStudyListProcessor;
        private readonly StudyEngine _studyEngine;

        public StudyEngineTests()
        {
            _mockPersistenceService = new Mock<IDataPersistenceService>();
            _mockContentLoaderService = new Mock<IContentLoaderService>();
            _mockProgressManager = new Mock<IProgressManager>();
            _mockStudyListProcessor = new Mock<IStudyListProcessor>();
            _studyEngine = new StudyEngine(
                _mockContentLoaderService.Object,
                _mockProgressManager.Object,
                _mockStudyListProcessor.Object,
                null,
                _mockPersistenceService.Object);
        }

        private List<LearningItem> CreateTestItems()
        {
            return new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/"),
                new TestLearningItem("Banana", "香蕉", "/bəˈnæn.ə/")
            };
        }

        private List<LearningItem> CreateSingleTestItem()
        {
            return new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", "/ˈæp.l/")
            };
        }

        private LearningContext CreateContext(string mode = "学习模式")
        {
            var learningMode = mode == "快速模式" ? LearningModeType.Quick : LearningModeType.Study;
            return new LearningContext(
                UserId: "test_user",
                Subject: SubjectType.English,
                SubCategory: SubCategoryType.EnglishWord,
                Mode: learningMode,
                SortOrder: SortOrderType.Sequential
            );
        }

        private void SetupAndInitializeWithItems(List<LearningItem> items, string mode = "学习模式")
        {
            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<LearningContext>()))
                .Returns(items);

            _mockStudyListProcessor
                .Setup(x => x.ProcessItems(It.IsAny<List<LearningItem>>(), It.IsAny<SortOrderType>()))
                .Returns<List<LearningItem>, SortOrderType>((items, sort) => items);

            _mockStudyListProcessor
                .Setup(x => x.RemoveDuplicates(It.IsAny<List<LearningItem>>()))
                .Returns<List<LearningItem>>(items => items);

            var context = CreateContext(mode);
            _studyEngine.Initialize(context);
        }

        [Fact]
        public void Initialize_WithValidData_ShouldLoadItems()
        {
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.TotalCount.Should().Be(2);
            _mockContentLoaderService.Verify(x => x.LoadItems(It.IsAny<LearningContext>()), Times.Once);
        }

        [Fact]
        public void GetCurrentItem_AfterInitialize_ShouldReturnFirstItem()
        {
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            var currentItem = _studyEngine.GetCurrentItem();

            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void HasNext_WithMultipleItems_ShouldReturnTrue()
        {
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            var hasNext = _studyEngine.HasNext();

            hasNext.Should().BeTrue();
        }

        [Fact]
        public void MoveNext_ShouldAdvanceToNextItem()
        {
            var testItems = CreateTestItems();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.MoveNext();
            var currentItem = _studyEngine.GetCurrentItem();

            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Banana");
        }

        [Fact]
        public void MarkCurrentAsKnown_ShouldAddToKnownList()
        {
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.MarkCurrentAsKnown();

            _studyEngine.KnownItems.Should().Contain("Apple");
        }

        [Fact]
        public void GetStatistics_WithCorrectAnswers_ShouldCalculateAccuracy()
        {
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems, "快速模式");
            _studyEngine.MarkCurrentAsKnown();

            var stats = _studyEngine.GetStatistics();

            stats.AccuracyRate.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Initialize_WithEmptyItemsList_ShouldNotThrow()
        {
            _mockContentLoaderService
                .Setup(x => x.LoadItems(It.IsAny<LearningContext>()))
                .Returns(new List<LearningItem>());

            _mockStudyListProcessor
                .Setup(x => x.ProcessItems(It.IsAny<List<LearningItem>>(), It.IsAny<SortOrderType>()))
                .Returns<List<LearningItem>, SortOrderType>((items, sort) => new List<LearningItem>());

            _mockStudyListProcessor
                .Setup(x => x.RemoveDuplicates(It.IsAny<List<LearningItem>>()))
                .Returns(new List<LearningItem>());

            Action act = () => _studyEngine.Initialize(CreateContext());

            act.Should().NotThrow();
            _studyEngine.TotalCount.Should().Be(0);
        }

        [Fact]
        public void GetCurrentItem_BeforeInitialize_ShouldReturnNull()
        {
            var currentItem = _studyEngine.GetCurrentItem();

            currentItem.Should().BeNull();
        }

        [Fact]
        public void MoveNext_AtEndOfList_ShouldNotAdvance()
        {
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.MoveNext();
            var hasNextAfterFirst = _studyEngine.HasNext();
            
            _studyEngine.MoveNext();
            var currentItem = _studyEngine.GetCurrentItem();

            hasNextAfterFirst.Should().BeFalse();
            currentItem.Should().NotBeNull();
            currentItem!.GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void MarkCurrentAsKnown_DuplicateItem_ShouldOnlyAddOnce()
        {
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.MarkCurrentAsKnown();
            _studyEngine.MarkCurrentAsKnown();

            _studyEngine.KnownItems.Should().HaveCount(1);
        }

        [Fact]
        public void MarkCurrentAsUnknown_ShouldAddToUnknownList()
        {
            var testItems = CreateSingleTestItem();

            SetupAndInitializeWithItems(testItems);

            _studyEngine.MarkCurrentAsUnknown();

            _studyEngine.UnknownItems.Should().Contain("Apple");
        }
    }

    public class TestLearningItem : LearningItem
    {
        private readonly string _content;
        private readonly string _meaning;
        private readonly string _pronunciation;

        public TestLearningItem(string content, string meaning, string pronunciation)
        {
            _content = content;
            _meaning = meaning;
            _pronunciation = pronunciation;
        }

        public override string GetMainContent() => _content;

        public override string GetDisplayText() => $"{_content}: {_meaning}";

        public override string GetPronunciation() => _pronunciation;

        public override string GetDisplayStruct() => "单词 | 音标 | 释义";
    }
}