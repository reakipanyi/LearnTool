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
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public ProgressState GetProgressState()
        {
            return _currentState;
        }

        public void LoadProgress(string userId, string subCategory)
        {
            // 优先从 LearningItemStates 表加载（新的 SQLite 存储）
            var knownItems = _persistenceService.GetKnownItems(userId, subCategory);
            var unknownItems = _persistenceService.GetUnknownItems(userId, subCategory);

            // 如果新表没有数据，尝试从 CategoryProgress 的 JSON 字段加载（兼容旧数据）
            if (knownItems.Count == 0 && unknownItems.Count == 0)
            {
                var profile = _persistenceService.LoadUserProfile(userId);
                var progress = profile.LearningProgress;

                if (progress.CategoryProgresses.TryGetValue(subCategory, out var categoryProgress))
                {
                    knownItems = categoryProgress.KnownItems.ToList();
                    unknownItems = categoryProgress.UnknownItems.ToList();

                    // 如果从 CategoryProgress 加载了数据，同步到 LearningItemStates 表
                    if (knownItems.Count > 0 || unknownItems.Count > 0)
                    {
                        _persistenceService.SyncCategoryProgressToLearningItemStates(userId, subCategory, knownItems, unknownItems);
                    }
                }
            }

            _currentState.KnownItems = knownItems;
            _currentState.UnknownItems = unknownItems;

            // 从 CategoryProgress 加载统计和索引信息
            var userProfile = _persistenceService.LoadUserProfile(userId);
            if (userProfile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
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

        public void SaveProgress(string userId, string subCategory, StudyEngineState state)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            var progress = profile.LearningProgress;

            if (!progress.CategoryProgresses.TryGetValue(subCategory, out var categoryProgress))
            {
                categoryProgress = new CategoryProgress { CategoryName = subCategory };
                progress.CategoryProgresses[subCategory] = categoryProgress;
            }

            // 同步到 LearningItemStates 表（新存储）
            _persistenceService.SyncCategoryProgressToLearningItemStates(userId, subCategory, state.KnownItems, state.UnknownItems);

            // 同时保留 CategoryProgress 的 JSON 字段（向后兼容）
            categoryProgress.KnownItems = state.KnownItems;
            categoryProgress.UnknownItems = state.UnknownItems;
            categoryProgress.LastStudyMode = state.CurrentMode;
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

        public void AddUnknownItem(string userId, string content, string subCategory)
        {
            // 使用 LearningItemStates 表
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

                // 同时更新 CategoryProgress（向后兼容）
                var profile = _persistenceService.LoadUserProfile(userId);
                if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
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