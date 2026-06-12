using System.Collections.Concurrent;

namespace LearningAssistant.Services.Learning.SortStrategies
{
    public class SortStrategyFactory
    {
        private readonly ConcurrentDictionary<string, ISortStrategy> _strategies = new ConcurrentDictionary<string, ISortStrategy>();

        public SortStrategyFactory()
        {
            _strategies.TryAdd("Sequential", new SequentialSortStrategy());
            _strategies.TryAdd("Random", new RandomSortStrategy());
        }

        public ISortStrategy GetStrategy(string strategyName)
        {
            if (_strategies.TryGetValue(strategyName, out var strategy))
            {
                return strategy;
            }
            return _strategies["Sequential"];
        }
    }
}