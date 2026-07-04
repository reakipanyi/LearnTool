using LearningAssistant.Common;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public interface IProgressManager
    {
        void LoadProgress(string userId, SubCategoryType subCategory);
        void SaveProgress(string userId, SubCategoryType subCategory, StudyEngineState state);
        ProgressState GetProgressState();
        void AddUnknownItem(string userId, string content, SubCategoryType subCategory);
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
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public ProgressState GetProgressState()
        {
            return _currentState;
        }

        public void LoadProgress(string userId, SubCategoryType subCategory)
        {
            var knownItems = _persistenceService.GetKnownItems(userId, subCategory);
            var unknownItems = _persistenceService.GetUnknownItems(userId, subCategory);

            if (knownItems.Count == 0 && unknownItems.Count == 0)
            {
                var profile = _persistenceService.LoadUserProfile(userId);
                var progress = profile.LearningProgress;
                var subCategoryStr = subCategory.ToString();

                if (progress.CategoryProgresses.TryGetValue(subCategoryStr, out var categoryProgress))
                {
                    knownItems = categoryProgress.KnownItems.ToList();
                    unknownItems = categoryProgress.UnknownItems.ToList();

                    if (knownItems.Count > 0 || unknownItems.Count > 0)
                    {
                        _persistenceService.SyncCategoryProgressToLearningItemStates(userId, subCategory, knownItems, unknownItems);
                    }
                }
            }

            _currentState.KnownItems = knownItems;
            _currentState.UnknownItems = unknownItems;

            var userProfile = _persistenceService.LoadUserProfile(userId);
            var subCatStr = subCategory.ToString();
            if (userProfile.LearningProgress.CategoryProgresses.TryGetValue(subCatStr, out var catProgress))
            {
                _currentState.CorrectCount = catProgress.CorrectCount;
                _currentState.TotalCount = catProgress.TotalTestCount;
                _currentState.StudyModeIndex = catProgress.LastResumeIndex;
                _currentState.QuickModeIndex = catProgress.QuickTestResumeIndex;
            }
            else
            {
                ResetProgress();
            }
        }

        public void SaveProgress(string userId, SubCategoryType subCategory, StudyEngineState state)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;
            var subCategoryStr = subCategory.ToString();

            if (!progress.CategoryProgresses.TryGetValue(subCategoryStr, out var categoryProgress))
            {
                categoryProgress = new CategoryProgress { CategoryName = subCategoryStr };
                progress.CategoryProgresses[subCategoryStr] = categoryProgress;
            }

            _persistenceService.SyncCategoryProgressToLearningItemStates(userId, subCategory, state.KnownItems, state.UnknownItems);

            categoryProgress.KnownItems = state.KnownItems;
            categoryProgress.UnknownItems = state.UnknownItems;
            categoryProgress.LastStudyMode = state.CurrentMode.ToString();
            categoryProgress.LastResumeIndex = state.StudyModeIndex;
            categoryProgress.QuickTestResumeIndex = state.QuickModeIndex;
            categoryProgress.TotalTestCount = state.TotalCount;
            categoryProgress.CorrectCount = state.CorrectCount;
            categoryProgress.LastTestDate = DateTime.Now;

            progress.LastStudyTime = DateTime.Now;

            profile.UpdateStudyRecord();
            profile.IncrementTodayItems();

            _persistenceService.SaveUserProfile(profile);
        }

        public void AddUnknownItem(string userId, string content, SubCategoryType subCategory)
        {
            var knownItems = _persistenceService.GetKnownItems(userId, subCategory);
            var unknownItems = _persistenceService.GetUnknownItems(userId, subCategory);

            if (!knownItems.Contains(content) && !unknownItems.Contains(content))
            {
                _persistenceService.UpsertLearningItemState(userId, subCategory, content, false);
                if (!_currentState.UnknownItems.Contains(content))
                    _currentState.UnknownItems.Add(content);
            }
            else if (knownItems.Contains(content))
            {
                _persistenceService.UpsertLearningItemState(userId, subCategory, content, false);
                _currentState.KnownItems.Remove(content);
                if (!_currentState.UnknownItems.Contains(content))
                    _currentState.UnknownItems.Add(content);

                var profile = _persistenceService.LoadUserProfile(userId);
                var subCategoryStr = subCategory.ToString();
                if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategoryStr, out var catProgress))
                {
                    catProgress.KnownItems.Remove(content);
                    if (!catProgress.UnknownItems.Contains(content))
                    {
                        catProgress.UnknownItems.Add(content);
                    }
                    _persistenceService.SaveUserProfile(profile);
                }
            }
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