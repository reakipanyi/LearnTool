using LearningAssistant.Models.Learning;

namespace LearningAssistant.Services.Learning.SortStrategies
{
    public interface ISortStrategy
    {
        string StrategyName { get; }
        void Sort(List<LearningItem> items);
    }
}