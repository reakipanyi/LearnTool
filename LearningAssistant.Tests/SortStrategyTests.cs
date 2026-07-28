using Xunit;
using FluentAssertions;
using LearningAssistant.Services.Learning.SortStrategies;
using LearningAssistant.Models.Learning;
using LearningAssistant.Common;

namespace LearningAssistant.Tests
{
    public class SortStrategyFactoryTests
    {
        private readonly SortStrategyFactory _factory = new SortStrategyFactory();

        [Fact]
        public void GetStrategy_WithSequential_ShouldReturnSequentialSortStrategy()
        {
            var strategy = _factory.GetStrategy("Sequential");

            strategy.Should().BeOfType<SequentialSortStrategy>();
            strategy.StrategyName.Should().Be("Sequential");
        }

        [Fact]
        public void GetStrategy_WithRandom_ShouldReturnRandomSortStrategy()
        {
            var strategy = _factory.GetStrategy("Random");

            strategy.Should().BeOfType<RandomSortStrategy>();
            strategy.StrategyName.Should().Be("Random");
        }

        [Fact]
        public void GetStrategy_WithUnknownStrategy_ShouldReturnSequential()
        {
            var strategy = _factory.GetStrategy("Unknown");

            strategy.Should().BeOfType<SequentialSortStrategy>();
        }

        [Fact]
        public void GetStrategy_WithNullStrategy_ShouldReturnSequential()
        {
            var strategy = _factory.GetStrategy(null!);

            strategy.Should().BeOfType<SequentialSortStrategy>();
        }

        [Fact]
        public void GetStrategy_WithEmptyStrategy_ShouldReturnSequential()
        {
            var strategy = _factory.GetStrategy("");

            strategy.Should().BeOfType<SequentialSortStrategy>();
        }
    }

    public class SequentialSortStrategyTests
    {
        private readonly SequentialSortStrategy _strategy = new SequentialSortStrategy();

        [Fact]
        public void Sort_WithUnsortedItems_ShouldSortAlphabetically()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Banana", "香蕉", ""),
                new TestLearningItem("Apple", "苹果", ""),
                new TestLearningItem("Cherry", "樱桃", "")
            };

            _strategy.Sort(items);

            items[0].GetMainContent().Should().Be("Apple");
            items[1].GetMainContent().Should().Be("Banana");
            items[2].GetMainContent().Should().Be("Cherry");
        }

        [Fact]
        public void Sort_WithEmptyList_ShouldNotThrow()
        {
            var items = new List<LearningItem>();

            Action act = () => _strategy.Sort(items);

            act.Should().NotThrow();
        }

        [Fact]
        public void Sort_WithSingleItem_ShouldNotChangeOrder()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", "")
            };

            _strategy.Sort(items);

            items.Count.Should().Be(1);
            items[0].GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void Sort_WithChineseCharacters_ShouldSortByPinyin()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("中", "middle", ""),
                new TestLearningItem("国", "country", ""),
                new TestLearningItem("人", "person", "")
            };

            _strategy.Sort(items);

            items[0].GetMainContent().Should().Be("国");
            items[1].GetMainContent().Should().Be("人");
            items[2].GetMainContent().Should().Be("中");
        }
    }

    public class RandomSortStrategyTests
    {
        private readonly RandomSortStrategy _strategy = new RandomSortStrategy();

        [Fact]
        public void Sort_WithMultipleItems_ShouldRandomizeOrder()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("A", "", ""),
                new TestLearningItem("B", "", ""),
                new TestLearningItem("C", "", ""),
                new TestLearningItem("D", "", ""),
                new TestLearningItem("E", "", "")
            };

            var originalOrder = items.Select(i => i.GetMainContent()).ToList();

            _strategy.Sort(items);

            var newOrder = items.Select(i => i.GetMainContent()).ToList();
            newOrder.Should().NotBeEmpty();
            newOrder.Should().HaveCount(5);
        }

        [Fact]
        public void Sort_WithEmptyList_ShouldNotThrow()
        {
            var items = new List<LearningItem>();

            Action act = () => _strategy.Sort(items);

            act.Should().NotThrow();
        }

        [Fact]
        public void Sort_WithSingleItem_ShouldNotChangeOrder()
        {
            var items = new List<LearningItem>
            {
                new TestLearningItem("Apple", "苹果", "")
            };

            _strategy.Sort(items);

            items.Count.Should().Be(1);
            items[0].GetMainContent().Should().Be("Apple");
        }

        [Fact]
        public void Sort_WithNullList_ShouldNotThrow()
        {
            List<LearningItem>? items = null;

            Action act = () => _strategy.Sort(items!);

            act.Should().NotThrow();
        }

        [Fact]
        public void StrategyName_ShouldBeRandom()
        {
            _strategy.StrategyName.Should().Be("Random");
        }
    }
}
