using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.Persistence;

namespace LearningAssistant.Services.Learning
{
    public class StudyEngine : IStudyEngine
    {
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IProgressManager _progressManager;
        private readonly IStudyListProcessor _studyListProcessor;
        private readonly ILearningAnalyticsService? _analyticsService;

        private readonly StudyEngineState _state = new StudyEngineState();
        private List<LearningItem> _allItems = new List<LearningItem>();
        private List<LearningItem> _studyItems = new List<LearningItem>();

        public int CurrentIndex => _state.CurrentMode == Constants.LearningMode.Quick 
            ? _state.QuickModeIndex 
            : _state.StudyModeIndex;

        public int TotalCount => _studyItems.Count;
        public IReadOnlyList<string> KnownItems => _state.KnownItems.AsReadOnly();
        public IReadOnlyList<string> UnknownItems => _state.UnknownItems.AsReadOnly();
        public string CurrentMode => _state.CurrentMode;
        public string CurrentSortOrder => _state.CurrentSortOrder;
        public bool HasSavedProgress => _state.KnownItems.Count > 0 || _state.UnknownItems.Count > 0;

        public StudyEngine(
            IContentLoaderService contentLoaderService,
            IProgressManager progressManager,
            IStudyListProcessor studyListProcessor,
            ILearningAnalyticsService? analyticsService = null)
        {
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _progressManager = progressManager ?? throw new ArgumentNullException(nameof(progressManager));
            _studyListProcessor = studyListProcessor ?? throw new ArgumentNullException(nameof(studyListProcessor));
            _analyticsService = analyticsService;
        }

        public void Initialize(string userId, string language, string subCategory, string wordBankFile, 
                              string mode = Constants.LearningMode.Study, string sortOrder = Constants.SortOrder.Sequential, 
                              bool continueMode = true)
        {
            ValidateInitializeParameters(userId, language, subCategory);

            _state.UserId = userId;
            _state.Language = language;
            _state.SubCategory = subCategory;
            _state.WordBankFile = wordBankFile;
            _state.CurrentMode = mode == Constants.LearningMode.Quick ? Constants.LearningMode.Quick : Constants.LearningMode.Study;
            _state.CurrentSortOrder = sortOrder;

            LoadAllItems(subCategory, wordBankFile);

            if (continueMode)
            {
                _progressManager.LoadProgress(userId, subCategory);
                SyncProgressState();
            }
            else
            {
                ResetProgress();
            }

            BuildStudyItems();
            ValidateIndex();
        }

        private void ValidateInitializeParameters(string userId, string language, string subCategory)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentException("userId cannot be null or empty", nameof(userId));
            if (string.IsNullOrWhiteSpace(language))
                throw new ArgumentException("language cannot be null or empty", nameof(language));
            if (string.IsNullOrWhiteSpace(subCategory))
                throw new ArgumentException("subCategory cannot be null or empty", nameof(subCategory));
        }

        private void LoadAllItems(string subCategory, string wordBankFile)
        {
            _allItems.Clear();
            var items = _contentLoaderService.LoadItems(subCategory, wordBankFile);
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
            _state.KnownItems = progressState.KnownItems.ToList();
            _state.UnknownItems = progressState.UnknownItems.ToList();
            _state.CorrectCount = progressState.CorrectCount;
            _state.TotalCount = progressState.TotalCount;
            _state.StudyModeIndex = progressState.StudyModeIndex;
            _state.QuickModeIndex = progressState.QuickModeIndex;
        }

        private void BuildStudyItems()
        {
            _studyItems.Clear();

            if (_state.CurrentMode == Constants.LearningMode.Quick)
            {
                _studyItems = _studyListProcessor.ProcessItems(new List<LearningItem>(_allItems), _state.CurrentSortOrder);
                _state.QuickModeIndex = Math.Min(_state.QuickModeIndex, _studyItems.Count - 1);
            }
            else
            {
                BuildStudyModeItems();
            }
        }

        private void BuildStudyModeItems()
        {
            if (_state.UnknownItems.Any())
            {
                _studyItems = _allItems.Where(item => _state.UnknownItems.Contains(item.GetMainContent())).ToList();
                _studyItems = _studyListProcessor.RemoveDuplicates(_studyItems);

                if (_state.CurrentSortOrder == Constants.SortOrder.Random)
                {
                    _studyItems = _studyListProcessor.ProcessItems(_studyItems, Constants.SortOrder.Random);
                }

                _state.StudyModeIndex = Math.Min(_state.StudyModeIndex, _studyItems.Count - 1);
            }
            else
            {
                ResetAndStartNew();
            }
        }

        private void ResetAndStartNew()
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
            int currentIndex = CurrentIndex;
            if (currentIndex >= _studyItems.Count)
            {
                if (_state.CurrentMode == Constants.LearningMode.Quick)
                    _state.QuickModeIndex = Math.Max(0, _studyItems.Count - 1);
                else
                    _state.StudyModeIndex = Math.Max(0, _studyItems.Count - 1);
            }
            if (currentIndex < 0)
            {
                if (_state.CurrentMode == Constants.LearningMode.Quick)
                    _state.QuickModeIndex = 0;
                else
                    _state.StudyModeIndex = 0;
            }
        }

        public LearningItem? GetCurrentItem()
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return null;

            int index = CurrentIndex;
            return index >= 0 && index < _studyItems.Count ? _studyItems[index] : null;
        }

        public bool HasNext()
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return false;

            int index = CurrentIndex;
            return index < _studyItems.Count - 1;
        }

        public void MoveNext()
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return;

            int index = CurrentIndex;
            if (index < _studyItems.Count - 1)
            {
                if (_state.CurrentMode == Constants.LearningMode.Quick)
                    _state.QuickModeIndex++;
                else
                    _state.StudyModeIndex++;
            }
        }

        public void SetCurrentIndex(int index)
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return;

            if (index >= 0 && index < _studyItems.Count)
            {
                if (_state.CurrentMode == Constants.LearningMode.Quick)
                    _state.QuickModeIndex = index;
                else
                    _state.StudyModeIndex = index;
            }
        }

        public void MarkCurrentAsKnown()
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return;

            var item = GetCurrentItem();
            if (item == null)
                return;

            string content = item.GetMainContent();
            if (!_state.KnownItems.Contains(content))
                _state.KnownItems.Add(content);
            if (_state.UnknownItems.Contains(content))
                _state.UnknownItems.Remove(content);

            _state.CorrectCount++;
            _state.TotalCount++;

            SaveProgress();
            RecordActivity("Learn");
            RecordActivity("Correct");
        }

        public void MarkCurrentAsUnknown()
        {
            if (string.IsNullOrWhiteSpace(_state.UserId))
                return;

            var item = GetCurrentItem();
            if (item == null)
                return;

            string content = item.GetMainContent();
            if (!_state.UnknownItems.Contains(content))
                _state.UnknownItems.Add(content);

            _state.TotalCount++;

            SaveProgress();
            RecordActivity("Learn");
            RecordActivity("Wrong");
        }

        public int MarkItemsAsKnown(IEnumerable<string> contents)
        {
            if (string.IsNullOrWhiteSpace(_state.UserId) || contents == null)
                return 0;

            int count = 0;
            foreach (var content in contents)
            {
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                if (!_state.KnownItems.Contains(content))
                {
                    _state.KnownItems.Add(content);
                    count++;
                }
                _state.UnknownItems.Remove(content);
            }

            if (count > 0)
            {
                _state.CorrectCount += count;
                _state.TotalCount += count;
                // 批量操作只持久化一次
                SaveProgress();
                RecordActivity("Learn");
                RecordActivity("Correct");
            }
            return count;
        }

        public int MarkItemsAsUnknown(IEnumerable<string> contents)
        {
            if (string.IsNullOrWhiteSpace(_state.UserId) || contents == null)
                return 0;

            int count = 0;
            foreach (var content in contents)
            {
                if (string.IsNullOrWhiteSpace(content))
                    continue;

                if (!_state.UnknownItems.Contains(content))
                {
                    _state.UnknownItems.Add(content);
                    count++;
                }
            }

            if (count > 0)
            {
                _state.TotalCount += count;
                SaveProgress();
                RecordActivity("Learn");
                RecordActivity("Wrong");
            }
            return count;
        }

        private void RecordActivity(string activityType)
        {
            _analyticsService?.RecordActivity(_state.UserId, activityType, _state.SubCategory);
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
            _progressManager.SaveProgress(_state.UserId, _state.SubCategory, _state);
        }

        public void ResetProgress()
        {
            _state.KnownItems.Clear();
            _state.UnknownItems.Clear();
            _state.StudyModeIndex = 0;
            _state.QuickModeIndex = 0;
            _state.CorrectCount = 0;
            _state.TotalCount = 0;
            _progressManager.ResetProgress();
            SaveProgress();
        }

        public List<LearningItem> GetUnknownItems()
        {
            return _studyItems.Where(item => _state.UnknownItems.Contains(item.GetMainContent())).ToList();
        }

        public List<LearningItem> GetAllItems()
        {
            return _studyItems.ToList();
        }

        public void ApplySettings(string mode, string sortOrder)
        {
            _state.CurrentMode = mode == Constants.LearningMode.Quick ? Constants.LearningMode.Quick : Constants.LearningMode.Study;
            _state.CurrentSortOrder = sortOrder;
            BuildStudyItems();
            ValidateIndex();
        }

        public void AddUnknownItem(string content, string subCategory)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("content cannot be null or empty", nameof(content));
            if (string.IsNullOrWhiteSpace(subCategory))
                throw new ArgumentException("subCategory cannot be null or empty", nameof(subCategory));

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
}