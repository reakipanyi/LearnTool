using LearningAssistant.Common;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public interface IProgressManager
    {
        void LoadProgress(string userId, SubCategoryType subCategory, string? filterKey = null);
        void SaveProgress(string userId, SubCategoryType subCategory, StudyEngineState state, IReadOnlyDictionary<string, string>? itemKeyMap = null);
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
        private readonly Dictionary<string, ProgressState> _states = new Dictionary<string, ProgressState>(StringComparer.Ordinal);
        private string _activeKey = string.Empty;
        private readonly object _lock = new object();

        public ProgressManager(IDataPersistenceService persistenceService)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public ProgressState GetProgressState()
        {
            lock (_lock)
            {
                return GetOrCreateState(_activeKey);
            }
        }

        public void LoadProgress(string userId, SubCategoryType subCategory, string? filterKey = null)
        {
            var key = filterKey ?? string.Empty;
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

            lock (_lock)
            {
                var state = GetOrCreateState(key);
                state.KnownItems = knownItems;
                state.UnknownItems = unknownItems;

                if (key.Length == 0)
                {
                    var userProfile = _persistenceService.LoadUserProfile(userId);
                    var subCatStr = subCategory.ToString();
                    if (userProfile.LearningProgress.CategoryProgresses.TryGetValue(subCatStr, out var catProgress))
                    {
                        state.CorrectCount = catProgress.CorrectCount;
                        state.TotalCount = catProgress.TotalTestCount;
                        state.StudyModeIndex = catProgress.LastResumeIndex;
                        state.QuickModeIndex = catProgress.QuickTestResumeIndex;
                    }
                    else
                    {
                        ResetProgressInternal(state);
                    }
                }

                _activeKey = key;
            }
        }

        public void SaveProgress(string userId, SubCategoryType subCategory, StudyEngineState state, IReadOnlyDictionary<string, string>? itemKeyMap = null)
        {
            var key = state.ProgressKey ?? string.Empty;
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;
            var subCategoryStr = subCategory.ToString();

            _persistenceService.SyncCategoryProgressToLearningItemStates(userId, subCategory, state.KnownItems, state.UnknownItems, itemKeyMap);

            if (key.Length == 0)
            {
                if (!progress.CategoryProgresses.TryGetValue(subCategoryStr, out var categoryProgress))
                {
                    categoryProgress = new CategoryProgress { CategoryName = subCategoryStr };
                    progress.CategoryProgresses[subCategoryStr] = categoryProgress;
                }

                categoryProgress.KnownItems = state.KnownItems;
                categoryProgress.UnknownItems = state.UnknownItems;
                categoryProgress.LastStudyMode = state.CurrentMode.ToString();
                categoryProgress.LastResumeIndex = state.StudyModeIndex;
                categoryProgress.QuickTestResumeIndex = state.QuickModeIndex;
                categoryProgress.TotalTestCount = state.TotalCount;
                categoryProgress.CorrectCount = state.CorrectCount;
                categoryProgress.LastTestDate = DateTime.Now;
            }
            // 筛选态不写分类进度，避免不同筛选条件互相覆盖断点；掌握状态仍通过 ItemStates 全局共享。

            progress.LastStudyTime = DateTime.Now;

            profile.UpdateStudyRecord();
            profile.IncrementTodayItems();

            _persistenceService.SaveUserProfile(profile);

            lock (_lock)
            {
                var target = GetOrCreateState(key);
                target.KnownItems = state.KnownItems;
                target.UnknownItems = state.UnknownItems;
                target.StudyModeIndex = state.StudyModeIndex;
                target.QuickModeIndex = state.QuickModeIndex;
                target.CorrectCount = state.CorrectCount;
                target.TotalCount = state.TotalCount;
                _activeKey = key;
            }
        }

        public void AddUnknownItem(string userId, string content, SubCategoryType subCategory)
        {
            var knownItems = _persistenceService.GetKnownItems(userId, subCategory);
            var unknownItems = _persistenceService.GetUnknownItems(userId, subCategory);

            lock (_lock)
            {
                var state = GetOrCreateState(_activeKey);
                if (!knownItems.Contains(content) && !unknownItems.Contains(content))
                {
                    _persistenceService.UpsertLearningItemState(userId, subCategory, content, false);
                    if (!state.UnknownItems.Contains(content))
                        state.UnknownItems.Add(content);
                }
                else if (knownItems.Contains(content))
                {
                    _persistenceService.UpsertLearningItemState(userId, subCategory, content, false);
                    state.KnownItems.Remove(content);
                    if (!state.UnknownItems.Contains(content))
                        state.UnknownItems.Add(content);

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
        }

        public void ResetProgress()
        {
            lock (_lock)
            {
                ResetProgressInternal(GetOrCreateState(_activeKey));
            }
        }

        private static void ResetProgressInternal(ProgressState state)
        {
            state.KnownItems.Clear();
            state.UnknownItems.Clear();
            state.StudyModeIndex = 0;
            state.QuickModeIndex = 0;
            state.CorrectCount = 0;
            state.TotalCount = 0;
        }

        private ProgressState GetOrCreateState(string key)
        {
            if (!_states.TryGetValue(key, out var state))
            {
                state = new ProgressState();
                _states[key] = state;
            }
            return state;
        }
    }
}