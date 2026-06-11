using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Models.User;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public class StudyEngine : IStudyEngine
    {
        private static readonly Random _sharedRandom = new Random();
        private readonly object _randomLock = new object();
        private readonly object _stateLock = new object();

        private readonly IDataPersistenceService _persistenceService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly ILearningAnalyticsService? _analyticsService;

        private string _currentUserId = "";
        private string _currentLanguage = "";
        private string _currentSubCategory = "";
        private string _currentWordBankFile = "";
        private string _currentMode = "";
        private string _currentSortOrder = "";
        private List<LearningItem> _allItems = new List<LearningItem>();
        private List<LearningItem> _studyItems = new List<LearningItem>();

        private int _studyModeIndex = 0;
        private int _quickModeIndex = 0;
        private List<string> _knownItems = new List<string>();
        private List<string> _unknownItems = new List<string>();
        private int _correctCount = 0;
        private int _totalCount = 0;

        public int CurrentIndex => _currentMode == Constants.LearningMode.Quick ? _quickModeIndex : _studyModeIndex;
        public int TotalCount => _studyItems.Count;
        public IReadOnlyList<string> KnownItems => _knownItems.AsReadOnly();
        public IReadOnlyList<string> UnknownItems => _unknownItems.AsReadOnly();
        public string CurrentMode => _currentMode;
        public string CurrentSortOrder => _currentSortOrder;
        public bool HasSavedProgress => _knownItems.Count > 0 || _unknownItems.Count > 0;

        public StudyEngine(
            IDataPersistenceService persistenceService,
            IContentLoaderService contentLoaderService,
            ILearningAnalyticsService? analyticsService = null)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _analyticsService = analyticsService;
        }

        public void Initialize(string userId, string language, string subCategory, string wordBankFile, string mode = Constants.LearningMode.Study, string sortOrder = Constants.SortOrder.Sequential, bool continueMode = true)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId cannot be null or empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("language cannot be null or empty", nameof(language));
            if (string.IsNullOrWhiteSpace(subCategory))
                throw new ArgumentException("subCategory cannot be null or empty", nameof(subCategory));

            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
            _currentWordBankFile = wordBankFile;
            _currentMode = mode == Constants.LearningMode.Quick ? Constants.LearningMode.Quick : Constants.LearningMode.Study;
            _currentSortOrder = sortOrder;

            // 清空学习列表
            _allItems.Clear();
            _studyItems.Clear();

            var items = _contentLoaderService.LoadItems(subCategory, wordBankFile);
            foreach (var item in items)
            {
                if (item is LearningItem learningItem)
                {
                    _allItems.Add(learningItem);
                }
            }
            
            if (continueMode)
            {
                // 继续学习模式：从存储加载进度
                LoadUserProgress();
            }
            else
            {
                // 新开始模式：清空所有进度
                _studyModeIndex = 0;
                _quickModeIndex = 0;
                _knownItems.Clear();
                _unknownItems.Clear();
                _correctCount = 0;
                _totalCount = 0;
            }

            if (_currentMode == Constants.LearningMode.Quick)
            {
                _studyItems = new List<LearningItem>(_allItems);
                if (sortOrder == Constants.SortOrder.Random)
                    ShuffleList(_studyItems);
                _quickModeIndex = continueMode ? GetQuickTestResumeIndex() : 0;
            }
            else
            {
                _studyItems = _allItems.Where(item => _unknownItems.Contains(item.GetMainContent())).ToList();

                // 过滤重复项
                _studyItems = RemoveDuplicates(_studyItems);

                if (!continueMode || _studyItems.Count == 0)
                {
                    // "开始学习" 或没有未掌握项：全部内容重新学习
                    _studyItems = new List<LearningItem>(_allItems);
                    // 过滤重复项
                    _studyItems = RemoveDuplicates(_studyItems);
                    // 重置进度
                    _knownItems.Clear();
                    _unknownItems.Clear();
                    _correctCount = 0;
                    _totalCount = 0;
                    _studyModeIndex = 0;
                    _quickModeIndex = 0;
                    SaveProgress();
                }
                else
                {
                    if (sortOrder == Constants.SortOrder.Random)
                        ShuffleList(_studyItems);
                    _studyModeIndex = GetLastResumeIndex();
                }
            }

            // 确保索引不会超出范围
            int currentIndex = CurrentIndex;
            if (currentIndex >= _studyItems.Count)
            {
                if (_currentMode == Constants.LearningMode.Quick)
                    _quickModeIndex = Math.Max(0, _studyItems.Count - 1);
                else
                    _studyModeIndex = Math.Max(0, _studyItems.Count - 1);
            }
            if (currentIndex < 0)
            {
                if (_currentMode == Constants.LearningMode.Quick)
                    _quickModeIndex = 0;
                else
                    _studyModeIndex = 0;
            }
        }

        public LearningItem? GetCurrentItem()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_currentUserId))
                    return null;
                
                int index = CurrentIndex;
                if (index >= 0 && index < _studyItems.Count)
                    return _studyItems[index];
                return null;
            }
        }

        public bool HasNext()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_currentUserId))
                    return false;
                
                int index = CurrentIndex;
                return index < _studyItems.Count - 1;
            }
        }

        public void MoveNext()
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_currentUserId))
                    return;
                
                int index = CurrentIndex;
                if (index < _studyItems.Count - 1)
                {
                    if (_currentMode == Constants.LearningMode.Quick)
                        _quickModeIndex++;
                    else
                        _studyModeIndex++;
                }
            }
        }

        public void SetCurrentIndex(int index)
        {
            lock (_stateLock)
            {
                if (string.IsNullOrWhiteSpace(_currentUserId))
                    return;
                
                if (index >= 0 && index < _studyItems.Count)
                {
                    if (_currentMode == Constants.LearningMode.Quick)
                        _quickModeIndex = index;
                    else
                        _studyModeIndex = index;
                }
            }
        }

        public void MarkCurrentAsKnown()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
                return;

            string content;
            lock (_stateLock)
            {
                int index = CurrentIndex;
                var item = index >= 0 && index < _studyItems.Count ? _studyItems[index] : null;
                if (item == null)
                    return;

                content = item.GetMainContent();
                if (!_knownItems.Contains(content))
                    _knownItems.Add(content);
                if (_unknownItems.Contains(content))
                    _unknownItems.Remove(content);

                _correctCount++;
                _totalCount++;
            }
            SaveProgress();

            // 记录学习活动
            _analyticsService?.RecordActivity(_currentUserId, "Learn", _currentSubCategory);
            _analyticsService?.RecordActivity(_currentUserId, "Correct", _currentSubCategory);
        }

        public void MarkCurrentAsUnknown()
        {
            if (string.IsNullOrWhiteSpace(_currentUserId))
                return;

            string content;
            lock (_stateLock)
            {
                int index = CurrentIndex;
                var item = index >= 0 && index < _studyItems.Count ? _studyItems[index] : null;
                if (item == null)
                    return;

                content = item.GetMainContent();
                if (!_unknownItems.Contains(content))
                    _unknownItems.Add(content);

                _totalCount++;
            }
            SaveProgress();

            // 记录学习活动
            _analyticsService?.RecordActivity(_currentUserId, "Learn", _currentSubCategory);
            _analyticsService?.RecordActivity(_currentUserId, "Wrong", _currentSubCategory);
        }

        public StudyStatistics GetStatistics()
        {
            return new StudyStatistics
            {
                TotalTestCount = _totalCount,
                CorrectCount = _correctCount,
                LastTestDate = DateTime.Now
            };
        }

        public void SaveProgress()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            var progress = profile.LearningProgress;

            if (!progress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                categoryProgress = new CategoryProgress { CategoryName = _currentSubCategory };
                progress.CategoryProgresses[_currentSubCategory] = categoryProgress;
            }

            categoryProgress.KnownItems = _knownItems;
            categoryProgress.UnknownItems = _unknownItems;
            categoryProgress.LastStudyMode = _currentMode;
            // 总是保存两个索引，无论当前模式
            categoryProgress.LastResumeIndex = _studyModeIndex;
            categoryProgress.QuickTestResumeIndex = _quickModeIndex;
            categoryProgress.TotalTestCount = _totalCount;
            categoryProgress.CorrectCount = _correctCount;
            categoryProgress.LastTestDate = DateTime.Now;

            progress.LastStudyTime = DateTime.Now;
            progress.TotalItemsStudied = progress.CategoryProgresses.Values.Sum(c => c.TotalTestCount);
            progress.TotalItemsMastered = progress.CategoryProgresses.Values.Sum(c => c.CorrectCount);

            // 更新用户学习记录（连续天数、今日学习项数等）
            profile.UpdateStudyRecord();
            profile.IncrementTodayItems();

            _persistenceService.SaveUserProfile(profile);
        }

        public void ResetProgress()
        {
            _knownItems.Clear();
            _unknownItems.Clear();
            _studyModeIndex = 0;
            _quickModeIndex = 0;
            _correctCount = 0;
            _totalCount = 0;
            SaveProgress();
        }

        public List<LearningItem> GetUnknownItems()
        {
            return _studyItems.Where(item => _unknownItems.Contains(item.GetMainContent())).ToList();
        }

        public List<LearningItem> GetAllItems()
        {
            return _studyItems.ToList();
        }

        public void ApplySettings(string mode, string sortOrder)
        {
            lock (_stateLock)
            {
                _currentMode = mode == Constants.LearningMode.Quick ? Constants.LearningMode.Quick : Constants.LearningMode.Study;
                _currentSortOrder = sortOrder;

                // 重新构建学习列表，保持当前进度
                _studyItems.Clear();

                if (_currentMode == Constants.LearningMode.Quick)
                {
                    _studyItems = new List<LearningItem>(_allItems);
                    if (_currentSortOrder == Constants.SortOrder.Random)
                        ShuffleList(_studyItems);
                }
                else
                {
                    _studyItems = _allItems.Where(item => _unknownItems.Contains(item.GetMainContent())).ToList();
                    _studyItems = RemoveDuplicates(_studyItems);

                    if (_studyItems.Count == 0)
                    {
                        _studyItems = new List<LearningItem>(_allItems);
                        _studyItems = RemoveDuplicates(_studyItems);
                    }
                    else if (_currentSortOrder == Constants.SortOrder.Random)
                    {
                        ShuffleList(_studyItems);
                    }
                }

                // 确保当前模式的索引有效
                int currentIndex = CurrentIndex;
                if (currentIndex >= _studyItems.Count)
                {
                    if (_currentMode == Constants.LearningMode.Quick)
                        _quickModeIndex = Math.Max(0, _studyItems.Count - 1);
                    else
                        _studyModeIndex = Math.Max(0, _studyItems.Count - 1);
                }
                if (currentIndex < 0)
                {
                    if (_currentMode == Constants.LearningMode.Quick)
                        _quickModeIndex = 0;
                    else
                        _studyModeIndex = 0;
                }
            }
        }

        private void LoadUserProgress()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            var progress = profile.LearningProgress;

            if (progress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                _knownItems = categoryProgress.KnownItems.ToList();
                _unknownItems = categoryProgress.UnknownItems.ToList();
                _correctCount = categoryProgress.CorrectCount;
                _totalCount = categoryProgress.TotalTestCount;
                _studyModeIndex = categoryProgress.LastResumeIndex;
                _quickModeIndex = categoryProgress.QuickTestResumeIndex;
            }
            else
            {
                _knownItems = new List<string>();
                _unknownItems = new List<string>();
                _correctCount = 0;
                _totalCount = 0;
                _studyModeIndex = 0;
                _quickModeIndex = 0;
            }
        }

        private int GetResumeIndex(Func<CategoryProgress, int> indexSelector)
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                return indexSelector(categoryProgress);
            }
            return 0;
        }

        private int GetLastResumeIndex()
        {
            return GetResumeIndex(cp => cp.LastResumeIndex);
        }

        private int GetQuickTestResumeIndex()
        {
            return GetResumeIndex(cp => cp.QuickTestResumeIndex);
        }

        // 新增功能：PDF生词本联动 - 添加未掌握项
        public void AddUnknownItem(string content, string subCategory)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content cannot be null or empty", nameof(content));
            if (string.IsNullOrWhiteSpace(subCategory))
                throw new ArgumentException("subCategory cannot be null or empty", nameof(subCategory));

            try
            {
                // 加载用户的完整资料
                var profile = _persistenceService.LoadUserProfile(_currentUserId);

                // 找到或创建对应的分类进度
                if (!profile.LearningProgress.CategoryProgresses.TryGetValue(subCategory, out var catProgress))
                {
                    catProgress = new CategoryProgress { CategoryName = subCategory };
                    profile.LearningProgress.CategoryProgresses[subCategory] = catProgress;
                }

                // 添加到未掌握列表（如果不在已掌握列表中）
                if (!catProgress.KnownItems.Contains(content) && !catProgress.UnknownItems.Contains(content))
                {
                    catProgress.UnknownItems.Add(content);
                    // 同步更新内存缓存
                    if (!_unknownItems.Contains(content))
                        _unknownItems.Add(content);
                }
                else if (catProgress.KnownItems.Contains(content))
                {
                    // 如果已经在已掌握列表中，移除它并添加到未掌握
                    catProgress.KnownItems.Remove(content);
                    if (!catProgress.UnknownItems.Contains(content))
                    {
                        catProgress.UnknownItems.Add(content);
                    }
                    // 同步更新内存缓存
                    _knownItems.Remove(content);
                    if (!_unknownItems.Contains(content))
                        _unknownItems.Add(content);
                }

                // 保存用户资料
                _persistenceService.SaveUserProfile(profile);
            }
            catch (Exception ex)
            {
                _analyticsService?.RecordActivity(_currentUserId, "Error", $"AddUnknownItem failed: {ex.Message}");
                throw;
            }
        }

        private void ShuffleList<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return;
            
            lock (_randomLock)
            {
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = _sharedRandom.Next(n + 1);
                    (list[n], list[k]) = (list[k], list[n]);
                }
            }
        }

        private List<LearningItem> RemoveDuplicates(List<LearningItem> items)
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
