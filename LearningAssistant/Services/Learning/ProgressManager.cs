using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public interface IProgressManager
    {
        void LoadProgress(string userId, string subCategory);
        void SaveProgress(string userId, string subCategory, StudyEngineState state);
        ProgressState GetProgressState();
        void AddUnknownItem(string userId, string content, string subCategory);
        void ResetProgress();
    }

    public class ProgressState
    {
        public List<string> KnownItems { get; set; } = new List<string>();
        public List<string> UnknownItems { get; set; } = new List<string>();
        public int StudyModeIndex { get; set; }
        public int QuickModeIndex { get; set; }
        public int CorrectCount { get; set; }
        public int TotalCount { get; set; }
    }

    public class ProgressManager : IProgressManager
    {
        private readonly IDataPersistenceService _persistenceService;
        private readonly ProgressState _currentState = new ProgressState();

        public ProgressManager(IDataPersistenceService persistenceService)
        {
            _persistenceService = persistenceService;
        }

        public ProgressState GetProgressState()
        {
            return _currentState;
        }

        public void LoadProgress(string userId, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;

            if (progress.CategoryProgresses.TryGetValue(subCategory, out var categoryProgress))
            {
                _currentState.KnownItems = categoryProgress.KnownItems.ToList();
                _currentState.UnknownItems = categoryProgress.UnknownItems.ToList();
                _currentState.CorrectCount = categoryProgress.CorrectCount;
                _currentState.TotalCount = categoryProgress.TotalTestCount;
                _currentState.StudyModeIndex = categoryProgress.LastResumeIndex;
                _currentState.QuickModeIndex = categoryProgress.QuickTestResumeIndex;
            }
            else
            {
                ResetProgress();
            }
        }

        public void SaveProgress(string userId, string subCategory, StudyEngineState state)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;

            if (!progress.CategoryProgresses.TryGetValue(subCategory, out var categoryProgress))
            {
                categoryProgress = new CategoryProgress { CategoryName = subCategory };
                progress.CategoryProgresses[subCategory] = categoryProgress;
            }

            categoryProgress.KnownItems = state.KnownItems;
            categoryProgress.UnknownItems = state.UnknownItems;
            categoryProgress.LastStudyMode = state.CurrentMode;
            categoryProgress.LastResumeIndex = state.StudyModeIndex;
            categoryProgress.QuickTestResumeIndex = state.QuickModeIndex;
            categoryProgress.TotalTestCount = state.TotalCount;
            categoryProgress.CorrectCount = state.CorrectCount;
            categoryProgress.LastTestDate = DateTime.Now;

            progress.LastStudyTime = DateTime.Now;
            // TotalItemsStudied / TotalItemsMastered 改为计算属性，无需手动同步

            profile.UpdateStudyRecord();
            profile.IncrementTodayItems();

            _persistenceService.SaveUserProfile(profile);
        }

        public void AddUnknownItem(string userId, string content, string subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);

            if (!profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
            {
                catProgress = new CategoryProgress { CategoryName = subCategory };
                profile.LearningProgress.CategoryProgresses[subCategory] = catProgress;
            }

            if (!catProgress.KnownItems.Contains(content) && !catProgress.UnknownItems.Contains(content))
            {
                catProgress.UnknownItems.Add(content);
                if (!_currentState.UnknownItems.Contains(content))
                    _currentState.UnknownItems.Add(content);
            }
            else if (catProgress.KnownItems.Contains(content))
            {
                catProgress.KnownItems.Remove(content);
                if (!catProgress.UnknownItems.Contains(content))
                {
                    catProgress.UnknownItems.Add(content);
                }
                _currentState.KnownItems.Remove(content);
                if (!_currentState.UnknownItems.Contains(content))
                    _currentState.UnknownItems.Add(content);
            }

            _persistenceService.SaveUserProfile(profile);
        }

        public void ResetProgress()
        {
            _currentState.KnownItems.Clear();
            _currentState.UnknownItems.Clear();
            _currentState.StudyModeIndex = 0;
            _currentState.QuickModeIndex = 0;
            _currentState.CorrectCount = 0;
            _currentState.TotalCount = 0;
        }
    }
}