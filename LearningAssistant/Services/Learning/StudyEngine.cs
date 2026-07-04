using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public class StudyEngine : IStudyEngine
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IProgressManager _progressManager;
        private readonly IStudyListProcessor _studyListProcessor;
        private readonly ILearningAnalyticsService? _analyticsService;
        private readonly IDataPersistenceService _persistenceService;
        private readonly object _stateLock = new object();

        private readonly StudyEngineState _state = new StudyEngineState();
        private List<LearningItem> _allItems = [];
        private List<LearningItem> _studyItems = [];

        public int CurrentIndex => _state.CurrentMode == LearningModeType.Quick
            ? _state.QuickModeIndex
            : _state.StudyModeIndex;

        public int TotalCount => _studyItems.Count;
        public IReadOnlyList<string> KnownItems => _state.KnownItems.AsReadOnly();
        public IReadOnlyList<string> UnknownItems => _state.UnknownItems.AsReadOnly();
        public LearningModeType CurrentMode => _state.CurrentMode;
        public SortOrderType CurrentSortOrder => _state.CurrentSortOrder;
        public bool HasSavedProgress => _state.KnownItems.Count > 0 || _state.UnknownItems.Count > 0;

        public StudyEngine(
            IContentLoaderService contentLoaderService,
            IProgressManager progressManager,
            IStudyListProcessor studyListProcessor,
            ILearningAnalyticsService? analyticsService,
            IDataPersistenceService persistenceService)
        {
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _progressManager = progressManager ?? throw new ArgumentNullException(nameof(progressManager));
            _studyListProcessor = studyListProcessor ?? throw new ArgumentNullException(nameof(studyListProcessor));
            _analyticsService = analyticsService;
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
        }

        public void Initialize(LearningContext context, bool continueMode = true)
        {
            ValidateInitializeParameters(context);

            lock (_stateLock)
            {
                _state.UserId = context.UserId;
                _state.Subject = context.Subject;
                _state.SubCategory = context.SubCategory;
                _state.WordBankFile = context.WordBankFile;
                _state.CurrentMode = context.Mode == LearningModeType.Quick ? LearningModeType.Quick : LearningModeType.Study;
                _state.CurrentSortOrder = context.SortOrder;
            }

            LoadAllItems(context);

            if (continueMode)
            {
                _progressManager.LoadProgress(context.UserId, context.SubCategory);
                SyncProgressState();
            }
            else
            {
                ResetProgress();
            }

            BuildStudyItems();
            ValidateIndex();
        }

        private void ValidateInitializeParameters(LearningContext context)
        {
            if (string.IsNullOrWhiteSpace(context.UserId))
                throw new ArgumentException("UserId cannot be null or empty", nameof(context));
        }

        private void LoadAllItems(LearningContext context)
        {
            _allItems.Clear();
            var items = _contentLoaderService.LoadItems(context);
            foreach (var item in items)
            {
                if (item is LearningItem learningItem)
                {
                    _allItems.Add(learningItem);
                }
            }
        }

        private void SyncProgressState()
        {
            var progressState = _progressManager.GetProgressState();
            lock (_stateLock)
            {
                _state.KnownItems = progressState.KnownItems.ToList();
                _state.UnknownItems = progressState.UnknownItems.ToList();
                _state.CorrectCount = progressState.CorrectCount;
                _state.TotalCount = progressState.TotalCount;
                _state.StudyModeIndex = progressState.StudyModeIndex;
                _state.QuickModeIndex = progressState.QuickModeIndex;
            }
        }

        private void BuildStudyItems()
        {
            lock (_stateLock)
            {
                _studyItems.Clear();

                if (_state.CurrentMode == LearningModeType.Quick)
                {
                    _studyItems = _studyListProcessor.ProcessItems(new List<LearningItem>(_allItems), _state.CurrentSortOrder);
                    _state.QuickModeIndex = Math.Min(_state.QuickModeIndex, _studyItems.Count - 1);
                }
                else
                {
                    BuildStudyModeItemsInternal();
                }
            }
        }

        private void BuildStudyModeItemsInternal()
        {
            if (_state.UnknownItems.Any())
            {
                _studyItems = _allItems.Where(item => _state.UnknownItems.Contains(item.GetMainContent())).ToList();
                _studyItems = _studyListProcessor.RemoveDuplicates(_studyItems);

                if (_state.CurrentSortOrder == SortOrderType.Random)
                {
                    _studyItems = _studyListProcessor.ProcessItems(_studyItems, SortOrderType.Random);
                }

                _state.StudyModeIndex = Math.Min(_state.StudyModeIndex, _studyItems.Count - 1);
            }
            else
            {
                ResetAndStartNewInternal();
            }
        }

        private void ResetAndStartNew()
        {
            lock (_stateLock)
            {
                ResetAndStartNewInternal();
            }
        }

        private void ResetAndStartNewInternal()
        {
            _studyItems = _studyListProcessor.RemoveDuplicates(new List<LearningItem>(_allItems));
            _state.KnownItems.Clear();
            _state.UnknownItems.Clear();
            _state.CorrectCount = 0;
            _state.TotalCount = 0;
            _state.StudyModeIndex = 0;
            _state.QuickModeIndex = 0;
            SaveProgress();
        }

        private void ValidateIndex()
        {
            lock (_stateLock)
            {
                int currentIndex = _state.CurrentMode == LearningModeType.Quick
                    ? _state.QuickModeIndex
                    : _state.StudyModeIndex;

                if (currentIndex >= _studyItems.Count)
                {
                    if (_state.CurrentMode == LearningModeType.Quick)
                        _state.QuickModeIndex = Math.Max(0, _studyItems.Count - 1);
                    else
                        _state.StudyModeIndex = Math.Max(0, _studyItems.Count - 1);
                }
                if (currentIndex < 0)
                {
                    if (_state.CurrentMode == LearningModeType.Quick)
                        _state.QuickModeIndex = 0;
                    else
                        _state.StudyModeIndex = 0;
                }
            }
        }

        public LearningItem? GetCurrentItem()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return null;

                int index = _state.CurrentMode == LearningModeType.Quick
                    ? _state.QuickModeIndex
                    : _state.StudyModeIndex;
                return index >= 0 && index < _studyItems.Count ? _studyItems[index] : null;
            }
        }

        public bool HasNext()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return false;

                int index = _state.CurrentMode == LearningModeType.Quick
                    ? _state.QuickModeIndex
                    : _state.StudyModeIndex;
                return index < _studyItems.Count - 1;
            }
        }

        public void MoveNext()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return;

                int index = _state.CurrentMode == LearningModeType.Quick
                    ? _state.QuickModeIndex
                    : _state.StudyModeIndex;

                if (index < _studyItems.Count - 1)
                {
                    if (_state.CurrentMode == LearningModeType.Quick)
                        _state.QuickModeIndex++;
                    else
                        _state.StudyModeIndex++;
                }
            }
        }

        public void SetCurrentIndex(int index)
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return;

                if (index >= 0 && index < _studyItems.Count)
                {
                    if (_state.CurrentMode == LearningModeType.Quick)
                        _state.QuickModeIndex = index;
                    else
                        _state.StudyModeIndex = index;
                }
            }
        }

        public void MarkCurrentAsKnown()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return;

                var item = GetCurrentItemInternal();
                if (item == null)
                    return;

                string content = item.GetMainContent();
                if (!_state.KnownItems.Contains(content))
                    _state.KnownItems.Add(content);
                if (_state.UnknownItems.Contains(content))
                    _state.UnknownItems.Remove(content);

                _state.CorrectCount++;
                _state.TotalCount++;

                var userId = _state.UserId;
                var subCategory = _state.SubCategory;
                SaveProgressInternal();
                RecordActivityInternal(userId, subCategory, "Learn");
                RecordActivityInternal(userId, subCategory, "Correct");
            }
        }

        public void MarkCurrentAsUnknown()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    return;

                var item = GetCurrentItemInternal();
                if (item == null)
                    return;

                string content = item.GetMainContent();
                if (!_state.UnknownItems.Contains(content))
                    _state.UnknownItems.Add(content);

                _state.TotalCount++;

                var userId = _state.UserId;
                var subCategory = _state.SubCategory;
                SaveProgressInternal();
                RecordActivityInternal(userId, subCategory, "Learn");
                RecordActivityInternal(userId, subCategory, "Wrong");
            }
        }

        public int MarkItemsAsKnown(IEnumerable<string> contents)
        {
            if (string.IsNullOrWhiteSpace(_state.UserId) || contents == null)
                return 0;

            lock (_stateLock)
            {
                int count = 0;
                foreach (var content in contents)
                {
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    if (!_state.KnownItems.Contains(content))
                    {
                        _state.KnownItems.Add(content);
                    }
                    _state.UnknownItems.Remove(content);
                    count++;
                }

                if (count > 0)
                {
                    _state.CorrectCount += count;
                    _state.TotalCount += count;
                    var userId = _state.UserId;
                    var subCategory = _state.SubCategory;
                    SaveProgressInternal();
                    RecordActivityInternal(userId, subCategory, "Learn");
                    RecordActivityInternal(userId, subCategory, "Correct");
                }
                return count;
            }
        }

        public int MarkItemsAsUnknown(IEnumerable<string> contents)
        {
            if (string.IsNullOrWhiteSpace(_state.UserId) || contents == null)
                return 0;

            lock (_stateLock)
            {
                int count = 0;
                foreach (var content in contents)
                {
                    if (string.IsNullOrWhiteSpace(content))
                        continue;

                    if (!_state.UnknownItems.Contains(content))
                    {
                        _state.UnknownItems.Add(content);
                    }
                    count++;
                }

                if (count > 0)
                {
                    _state.TotalCount += count;
                    var userId = _state.UserId;
                    var subCategory = _state.SubCategory;
                    SaveProgressInternal();
                    RecordActivityInternal(userId, subCategory, "Learn");
                    RecordActivityInternal(userId, subCategory, "Wrong");
                }
                return count;
            }
        }

        private LearningItem? GetCurrentItemInternal()
        {
            int index = _state.CurrentMode == LearningModeType.Quick
                ? _state.QuickModeIndex
                : _state.StudyModeIndex;
            return index >= 0 && index < _studyItems.Count ? _studyItems[index] : null;
        }

        private void SaveProgressInternal()
        {
            _progressManager.SaveProgress(_state.UserId, _state.SubCategory, _state);
        }

        private void RecordActivityInternal(string userId, SubCategoryType subCategory, string activityType)
        {
            _analyticsService?.RecordActivity(userId, activityType, subCategory.ToString());
        }

        private void RecordActivity(string activityType)
        {
            _analyticsService?.RecordActivity(_state.UserId, activityType, _state.SubCategory.ToString());
        }

        public StudyStatistics GetStatistics()
        {
            return new StudyStatistics
            {
                TotalTestCount = _state.TotalCount,
                CorrectCount = _state.CorrectCount,
                LastTestDate = DateTime.Now
            };
        }

        public void SaveProgress()
        {
            lock (_stateLock)
            {
                SaveProgressInternal();
            }
        }

        public void ResetProgress()
        {
            lock (_stateLock)
            {
                _state.KnownItems.Clear();
                _state.UnknownItems.Clear();
                _state.StudyModeIndex = 0;
                _state.QuickModeIndex = 0;
                _state.CorrectCount = 0;
                _state.TotalCount = 0;
                _progressManager.ResetProgress();
                SaveProgressInternal();
            }
        }

        public List<LearningItem> GetUnknownItems()
        {
            lock (_stateLock)
            {
                return _studyItems.Where(item => _state.UnknownItems.Contains(item.GetMainContent())).ToList();
            }
        }

        public List<LearningItem> GetAllItems()
        {
            lock (_stateLock)
            {
                return _studyItems.ToList();
            }
        }

        public void ApplySettings(LearningModeType mode, SortOrderType sortOrder)
        {
            lock (_stateLock)
            {
                _state.CurrentMode = mode == LearningModeType.Quick ? LearningModeType.Quick : LearningModeType.Study;
                _state.CurrentSortOrder = sortOrder;
                BuildStudyItemsInternal();
                ValidateIndexInternal();
            }
        }

        private void BuildStudyItemsInternal()
        {
            _studyItems.Clear();

            if (_state.CurrentMode == LearningModeType.Quick)
            {
                _studyItems = _studyListProcessor.ProcessItems(new List<LearningItem>(_allItems), _state.CurrentSortOrder);
                _state.QuickModeIndex = Math.Min(_state.QuickModeIndex, _studyItems.Count - 1);
            }
            else
            {
                BuildStudyModeItemsInternal();
            }
        }

        private void ValidateIndexInternal()
        {
            int currentIndex = _state.CurrentMode == LearningModeType.Quick
                ? _state.QuickModeIndex
                : _state.StudyModeIndex;

            if (currentIndex >= _studyItems.Count)
            {
                if (_state.CurrentMode == LearningModeType.Quick)
                    _state.QuickModeIndex = Math.Max(0, _studyItems.Count - 1);
                else
                    _state.StudyModeIndex = Math.Max(0, _studyItems.Count - 1);
            }
            if (currentIndex < 0)
            {
                if (_state.CurrentMode == LearningModeType.Quick)
                    _state.QuickModeIndex = 0;
                else
                    _state.StudyModeIndex = 0;
            }
        }

        public void AddUnknownItem(string content, SubCategoryType subCategory)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content cannot be null or empty", nameof(content));

            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_state.UserId))
                    throw new InvalidOperationException("StudyEngine 未初始化，请先调用 Initialize 方法");

                try
                {
                    _progressManager.AddUnknownItem(_state.UserId, content, subCategory);
                    SyncProgressState();
                }
                catch (Exception ex)
                {
                    _analyticsService?.RecordActivity(_state.UserId, "Error", $"AddUnknownItem failed: {ex.Message}");
                    throw;
                }
            }
        }

        public string GetProgressSummary(string userId, SubjectType subject, SubCategoryType subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile?.LearningProgress == null)
                return $"玩家: 未知\n品类: {subject} > {subCategory}\n暂无学习数据";

            var progress = profile.LearningProgress;

            int totalKnown = 0;
            int totalUnknown = 0;
            double accuracy = 0;

            var subCategoryStr = subCategory.ToString();
            if (progress.CategoryProgresses.TryGetValue(subCategoryStr, out var catProgress))
            {
                totalKnown = catProgress.KnownItems.Count;
                totalUnknown = catProgress.UnknownItems.Count;
                if (catProgress.TotalTestCount > 0)
                {
                    accuracy = (double)catProgress.CorrectCount / catProgress.TotalTestCount * 100;
                }
            }

            return $"玩家: {profile.UserName}\n" +
                $"品类: {subject} > {subCategory}\n" +
                $"已掌握: {totalKnown} | 未掌握: {totalUnknown}\n" +
                $"正确率: {accuracy:F1}%";
        }

        public int GetKnownCount(string userId, SubCategoryType subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile?.LearningProgress == null)
                return 0;

            var subCategoryStr = subCategory.ToString();
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategoryStr, out var catProgress))
            {
                return catProgress.KnownItems.Count;
            }
            return 0;
        }

        public int GetUnknownCount(string userId, SubCategoryType subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile?.LearningProgress == null)
                return 0;

            var subCategoryStr = subCategory.ToString();
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategoryStr, out var catProgress))
            {
                return catProgress.UnknownItems.Count;
            }
            return 0;
        }

        public double GetAccuracy(string userId, SubCategoryType subCategory)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile?.LearningProgress == null)
                return 0;

            var subCategoryStr = subCategory.ToString();
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(subCategoryStr, out var catProgress) &&
                catProgress.TotalTestCount > 0)
            {
                return (double)catProgress.CorrectCount / catProgress.TotalTestCount * 100;
            }
            return 0;
        }

        List<string> IStudyEngine.GetUnknownItems(string userId)
        {
            var profile = _persistenceService.LoadUserProfile(userId);
            if (profile?.LearningProgress == null)
                return new List<string>();

            var allUnknownItems = new List<string>();

            foreach (var catProgress in profile.LearningProgress.CategoryProgresses.Values)
            {
                allUnknownItems.AddRange(catProgress.UnknownItems);
            }

            return allUnknownItems.Distinct().ToList();
        }
    }
}