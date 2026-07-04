using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Learning.SortStrategies;

namespace LearningAssistant.Services.Learning
{
    public interface IStudyListProcessor
    {
        List<LearningItem> ProcessItems(List<LearningItem> items, SortOrderType sortOrder);
        List<LearningItem> RemoveDuplicates(List<LearningItem> items);
    }

    public class StudyListProcessor : IStudyListProcessor
    {
        private readonly SortStrategyFactory _sortStrategyFactory;

        public StudyListProcessor(SortStrategyFactory sortStrategyFactory)
        {
            _sortStrategyFactory = sortStrategyFactory;
        }

        public List<LearningItem> ProcessItems(List<LearningItem> items, SortOrderType sortOrder)
        {
            var processed = RemoveDuplicates(items);
            var strategy = _sortStrategyFactory.GetStrategy(sortOrder.ToString());
            strategy.Sort(processed);
            return processed;
        }

        public List<LearningItem> RemoveDuplicates(List<LearningItem> items)
        {
            var seen = new HashSet<string>();
            var result = new List<LearningItem>();

            foreach (var item in items)
            {
                var content = item.GetMainContent();
                if (!seen.Contains(content))
                {
                    seen.Add(content);
                    result.Add(item);
                }
            }

            return result;
        }
    }
}