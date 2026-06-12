using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning.SortStrategies
{
    public class SequentialSortStrategy : ISortStrategy
    {
        public string StrategyName => "Sequential";

        public void Sort(List<LearningItem> items)
        {
            items.Sort((a, b) => string.Compare(a.GetMainContent(), b.GetMainContent()));
        }
    }
}