using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning.SortStrategies
{
    public class RandomSortStrategy : ISortStrategy
    {
        private static readonly Random _random = new Random();
        private readonly object _lockObj = new object();

        public string StrategyName => "Random";

        public void Sort(List<LearningItem> items)
        {
            if (items == null || items.Count <= 1) return;

            lock (_lockObj)
            {
                int n = items.Count;
                while (n > 1)
                {
                    n--;
                    int k = _random.Next(n + 1);
                    (items[n], items[k]) = (items[k], items[n]);
                }
            }
        }
    }
}