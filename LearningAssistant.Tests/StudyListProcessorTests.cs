using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Learning.SortStrategies;
using LearningAssistant.Models.Learning;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class StudyListProcessorTests
    {
        private readonly SortStrategyFactory _sortStrategyFactory = new SortStrategyFactory();
        private readonly StudyListProcessor _processor;

        public StudyListProcessorTests()
        {
            _processor = new StudyListProcessor(_sortStrategyFactory);
        }

        [Fact]
        public void ProcessItems_WithSequentialSort_ShouldSortAlphabetically()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Banana", "香蕉", ""),
                new TestLearningItem("Apple", "苹果", ""),
                new TestLearningItem("Cherry", "樱桃", "")
            };

            var result = _processor.ProcessItems(items, SortOrderType.Sequential);

            result.Should().HaveCount(3);
            result[0].GetMainContent().Should().Be("Apple");
            result[1].GetMainContent().Should().Be("Banana");
            result[2].GetMainContent().Should().Be("Cherry");
        }

        [Fact]
        public void ProcessItems_WithRandomSort_ShouldReturnItems()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("A", "", ""),
                new TestLearningItem("B", "", ""),
                new TestLearningItem("C", "", "")
            };

            var result = _processor.ProcessItems(items, SortOrderType.Random);

            result.Should().HaveCount(3);
            result.Select(i => i.GetMainContent()).Should().Contain("A", "B", "C");
        }

        [Fact]
        public void ProcessItems_WithNullItems_ShouldReturnEmptyList()
        {
            var result = _processor.ProcessItems(null!, SortOrderType.Sequential);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void ProcessItems_WithEmptyItems_ShouldReturnEmptyList()
        {
            var result = _processor.ProcessItems(new List<LearningItem>(), SortOrderType.Sequential);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void RemoveDuplicates_WithDuplicateItems_ShouldRemoveDuplicates()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", ""),
                new TestLearningItem("Banana", "香蕉", ""),
                new TestLearningItem("Apple", "苹果2", ""),
                new TestLearningItem("Cherry", "樱桃", ""),
                new TestLearningItem("Banana", "香蕉2", "")
            };

            var result = _processor.RemoveDuplicates(items);

            result.Should().HaveCount(3);
            result.Select(i => i.GetMainContent()).Should().Contain("Apple", "Banana", "Cherry");
        }

        [Fact]
        public void RemoveDuplicates_WithNoDuplicates_ShouldReturnSameList()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", ""),
                new TestLearningItem("Banana", "香蕉", ""),
                new TestLearningItem("Cherry", "樱桃", "")
            };

            var result = _processor.RemoveDuplicates(items);

            result.Should().HaveCount(3);
        }

        [Fact]
        public void RemoveDuplicates_WithNullItems_ShouldReturnEmptyList()
        {
            var result = _processor.RemoveDuplicates(null!);

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void RemoveDuplicates_WithEmptyItems_ShouldReturnEmptyList()
        {
            var result = _processor.RemoveDuplicates(new List<LearningItem>());

            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Fact]
        public void RemoveDuplicates_WithSingleItem_ShouldReturnSameItem()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", "")
            };

            var result = _processor.RemoveDuplicates(items);

            result.Should().HaveCount(1);
            result[0].GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void ProcessItems_ShouldRemoveDuplicatesBeforeSorting()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Banana", "香蕉", ""),
                new TestLearningItem("Apple", "苹果", ""),
                new TestLearningItem("Banana", "香蕉2", "")
            };

            var result = _processor.ProcessItems(items, SortOrderType.Sequential);

            result.Should().HaveCount(2);
            result[0].GetMainContent().Should().Be("Apple");
            result[1].GetMainContent().Should().Be("Banana");
        }
    }
}
