using UnifiedLearningAssistant.Services.Persistence;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class ProgressService : IProgressService
    {
        private readonly IDataPersistenceService _persistenceService;

        public ProgressService(IDataPersistenceService persistenceService)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public string GetProgressSummary(string userId, string language, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;

            int totalKnown = 0;
            int totalUnknown = 0;
            double accuracy = 0;

            if (progress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
            {
                totalKnown = catProgress.KnownItems.Count;
                totalUnknown = catProgress.UnknownItems.Count;
                if (catProgress.TotalTestCount > 0)
                {
                    accuracy = (double)catProgress.CorrectCount / catProgress.TotalTestCount * 100;
                }
            }

            return $"玩家: {profile.UserName}\n" +
                $"品类: {language} > {subCategory}\n" +
                $"已掌握: {totalKnown} | 未掌握: {totalUnknown}\n" +
                $"正确率: {accuracy:F1}%";
        }

        public int GetKnownCount(string userId, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
            {
                return catProgress.KnownItems.Count;
            }
            return 0;
        }

        public int GetUnknownCount(string userId, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
            {
                return catProgress.UnknownItems.Count;
            }
            return 0;
        }

        public double GetAccuracy(string userId, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress) &&
                catProgress.TotalTestCount > 0)
            {
                return (double)catProgress.CorrectCount / catProgress.TotalTestCount * 100;
            }
            return 0;
        }

        public List<string> GetUnknownItems(string userId)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var allUnknownItems = new List<string>();

            foreach (var catProgress in profile.LearningProgress.CategoryProgresses.Values)
            {
                allUnknownItems.AddRange(catProgress.UnknownItems);
            }

            return allUnknownItems.Distinct().ToList();
        }
    }
}