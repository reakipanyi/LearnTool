using UnifiedLearningAssistant.Models.Learning;
using UnifiedLearningAssistant.Models.User;
using UnifiedLearningAssistant.Services.Persistence;

namespace UnifiedLearningAssistant.Services.Learning
{
    public class StudyEngine : IStudyEngine
    {
        private static readonly Random _sharedRandom = new Random();
        private readonly object _randomLock = new object();
        private readonly object _stateLock = new object();

        private readonly IDataPersistenceService _persistenceService;
        private readonly IContentLoaderService _contentLoaderService;

        private string _currentUserId = "";
        private string _currentLanguage = "";
        private string _currentSubCategory = "";
        private string _currentWordBankFile = "";
        private string _currentMode = "";
        private List<LearningItem> _allItems = new List<LearningItem>();
        private List<LearningItem> _studyItems = new List<LearningItem>();

        private int _currentIndex = 0;
        private List<string> _knownItems = new List<string>();
        private List<string> _unknownItems = new List<string>();
        private int _correctCount = 0;
        private int _totalCount = 0;

        public int CurrentIndex => _currentIndex;
        public int TotalCount => _studyItems.Count;
        public IReadOnlyList<string> KnownItems => _knownItems.AsReadOnly();
        public IReadOnlyList<string> UnknownItems => _unknownItems.AsReadOnly();
        public string CurrentMode => _currentMode;

        public StudyEngine(IDataPersistenceService persistenceService, IContentLoaderService contentLoaderService)
        {
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
        }

        public void Initialize(string userId, string language, string subCategory, string wordBankFile, string mode, string sortOrder)
        {
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
            _currentWordBankFile = wordBankFile;
            _currentMode = mode;

            _allItems = _contentLoaderService.LoadItems(subCategory, wordBankFile);

            LoadUserProgress();

            if (mode == "快速模式")
            {
                _studyItems = new List<LearningItem>(_allItems);
                if (sortOrder == "Random")
                    ShuffleList(_studyItems);
                _currentIndex = GetQuickTestResumeIndex();
            }
            else
            {
                _studyItems = _allItems.Where(item => _unknownItems.Contains(item.GetMainContent())).ToList();
                if (_studyItems.Count == 0)
                    _studyItems = new List<LearningItem>(_allItems);
                if (sortOrder == "Random")
                    ShuffleList(_studyItems);
                _currentIndex = GetLastResumeIndex();
            }

            _correctCount = 0;
            _totalCount = 0;
        }

        public LearningItem? GetCurrentItem()
        {
            lock (_stateLock)
            {
                if (_currentIndex >= 0 && _currentIndex < _studyItems.Count)
                    return _studyItems[_currentIndex];
                return null;
            }
        }

        public bool HasNext()
        {
            lock (_stateLock)
            {
                return _currentIndex < _studyItems.Count - 1;
            }
        }

        public void MoveNext()
        {
            lock (_stateLock)
            {
                if (_currentIndex < _studyItems.Count - 1)
                    _currentIndex++;
            }
        }

        public void MarkCurrentAsKnown()
        {
            string content;
            lock (_stateLock)
            {
                var item = _currentIndex >= 0 && _currentIndex < _studyItems.Count ? _studyItems[_currentIndex] : null;
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
        }

        public void MarkCurrentAsUnknown()
        {
            string content;
            lock (_stateLock)
            {
                var item = _currentIndex >= 0 && _currentIndex < _studyItems.Count ? _studyItems[_currentIndex] : null;
                if (item == null)
                    return;

                content = item.GetMainContent();
                if (!_unknownItems.Contains(content))
                    _unknownItems.Add(content);

                _totalCount++;
            }
            SaveProgress();
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
            categoryProgress.LastResumeIndex = _currentMode == "学习模式" ? _currentIndex : 0;
            categoryProgress.QuickTestResumeIndex = _currentMode == "快速模式" ? _currentIndex : 0;
            categoryProgress.TotalTestCount += _totalCount;
            categoryProgress.CorrectCount += _correctCount;
            categoryProgress.LastTestDate = DateTime.Now;

            progress.LastStudyTime = DateTime.Now;
            progress.TotalItemsStudied += _totalCount;
            progress.TotalItemsMastered += _correctCount;

            _persistenceService.SaveUserProfile(profile);
        }

        public void ResetProgress()
        {
            _knownItems.Clear();
            _unknownItems.Clear();
            _currentIndex = 0;
            _correctCount = 0;
            _totalCount = 0;
            SaveProgress();
        }

        public List<LearningItem> GetUnknownItems()
        {
            return _studyItems.Where(item => _unknownItems.Contains(item.GetMainContent())).ToList();
        }

        private void LoadUserProgress()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            var progress = profile.LearningProgress;

            if (progress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                _knownItems = categoryProgress.KnownItems.ToList();
                _unknownItems = categoryProgress.UnknownItems.ToList();
            }
            else
            {
                _knownItems = new List<string>();
                _unknownItems = new List<string>();
            }
        }

        private int GetLastResumeIndex()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                return categoryProgress.LastResumeIndex;
            }
            return 0;
        }

        private int GetQuickTestResumeIndex()
        {
            var profile = _persistenceService.LoadUserProfile(_currentUserId);
            if (profile.LearningProgress.CategoryProgresses.TryGetValue(_currentSubCategory, out var categoryProgress))
            {
                return categoryProgress.QuickTestResumeIndex;
            }
            return 0;
        }

        private void ShuffleList<T>(List<T> list)
        {
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
    }
}
