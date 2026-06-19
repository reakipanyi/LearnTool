using LearningAssistant.Common;
using LearningAssistant.Managers;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public interface ILearningFlowHandler
    {
        Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true);
        Task MarkAsKnownAsync();
        Task MarkAsUnknownAsync();
        Task HandlePronounceAsync();
        Task MoveToNextAsync();
        Task HandleSettingsChangedAsync();
        Task HandleItemSelectedAsync(int index);
        void Exit();
        void OpenStatistics();
        void SendToPdfQuestion();

        event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }

    public class LearningFlowHandler : ILearningFlowHandler
    {
        private readonly ILogger<LearningFlowHandler> _logger;
        private readonly IStudyEngine _studyEngine;
        private readonly IAIService _aiService;
        private readonly ITTSService _ttsService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IExportService _exportService;
        private readonly IWindowManager _windowManager;
        private readonly ILearningSettingsManager _settingsManager;
        private readonly ILearningView _view;

        private CancellationTokenSource? _cts;
        private string _currentExplanation = "";
        private bool _isLoading = false;
        private string _currentUserId = "";
        private string _currentSubject = "";
        private string _currentSubCategory = "";
        private int _autoPronunciationCount = 0;
        private const int MaxAutoPronunciationCount = 5;

        public LearningFlowHandler(
            ILogger<LearningFlowHandler> logger,
            IStudyEngine studyEngine,
            IAIService aiService,
            ITTSService ttsService,
            IContentLoaderService contentLoaderService,
            IExportService exportService,
            IWindowManager windowManager,
            ILearningSettingsManager settingsManager,
            ILearningView view)
        {
            _logger = logger;
            _studyEngine = studyEngine;
            _aiService = aiService;
            _ttsService = ttsService;
            _contentLoaderService = contentLoaderService;
            _exportService = exportService;
            _windowManager = windowManager;
            _settingsManager = settingsManager;
            _view = view;
            _cts = new CancellationTokenSource();
        }

        public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Initializing learning session for user {UserId}, category {SubCategory}", userId, subCategory);
            _view.SetLoadingState(true, "正在加载学习内容...");
            try
            {
                await InitializeCore(userId, language, subCategory, wordBankFile, continueMode);
            }
            finally
            {
                _view.SetLoadingState(false);
            }
        }

        private async Task InitializeCore(string userId, string language, string subCategory, string wordBankFile, bool continueMode)
        {
            _currentUserId = userId;
            // 使用视图上当前选择的学科（已从设置加载），而不是外部传入的语言参数
            _currentSubject = _view.Subject;

            subCategory = await LoadSubCategoriesAsync(_currentSubject, subCategory);
            _currentSubCategory = subCategory;

            _studyEngine.Initialize(userId, _currentSubject, subCategory, wordBankFile, _view.LearningMode, _view.SortOrder, continueMode);
            UpdateLearningList();
            await DisplayCurrentItemAsync();
        }

        private async Task<string> LoadSubCategoriesAsync(string subject, string subCategory)
        {
            try
            {
                var subCategories = _contentLoaderService.GetSubCategoriesBySubject(subject);
                _view.RefreshSubCategories(subCategories);

                if (string.IsNullOrEmpty(subCategory) || !subCategories.Contains(subCategory))
                {
                    return _view.SubCategory;
                }
                else
                {
                    _view.SubCategory = subCategory;
                    return subCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load subcategories");
                return subCategory;
            }
        }

        private void UpdateLearningList()
        {
            try
            {
                var items = _studyEngine.GetAllItems();
                var itemTexts = items.Select(item => item.GetMainContent()).ToList();
                var currentIndex = _studyEngine.CurrentIndex;
                _view.UpdateLearningList(itemTexts, currentIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update learning list");
            }
        }

        private async Task DisplayCurrentItemAsync()
        {
            // 取消之前的操作，确保立即响应新的切换请求
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            _isLoading = true;
            try
            {
                _currentExplanation = "";

                var item = _studyEngine.GetCurrentItem();
                if (item == null)
                {
                    _view.CurrentContent = "学习已完成!";
                    _view.Statistics = "恭喜完成所有内容！";
                    _view.EnableButtons(false);
                    _logger.LogInformation("Learning session completed");
                    return;
                }

                // 立即更新视图内容，确保列表选中项和内容同步
                _view.CurrentContent = item.GetMainContent();
                _view.CurrentDisplayText = item.GetDisplayText();
                _view.CurrentDisplayStruct = item.GetDisplayStruct();
                _view.CurrentItem = item;
                _view.ProgressMax = _studyEngine.TotalCount;
                _view.ProgressValue = _studyEngine.CurrentIndex + 1;

                UpdateStatistics();
                UpdateListSelection();



                if (_view.IsVoiceEnabled && _autoPronunciationCount < MaxAutoPronunciationCount)
                {
                    await PlayPronunciationAsync(item, _currentExplanation, _cts.Token);
                    _autoPronunciationCount++;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("DisplayCurrentItemAsync was cancelled");
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void UpdateListSelection()
        {
            try
            {
                _view.UpdateLearningListSelection(_studyEngine.CurrentIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update list selection");
            }
        }

        private void UpdateStatistics()
        {
            var stats = _studyEngine.GetStatistics();
            _view.Statistics = $"进度: {_studyEngine.CurrentIndex + 1}/{_studyEngine.TotalCount} | " +
                $"正确: {stats.CorrectCount} | " +
                $"正确率: {stats.AccuracyRate:F1}%";
        }

        private async Task PlayPronunciationAsync(LearningItem item, string explanation, CancellationToken cancellationToken)
        {
            if (!_ttsService.Available) return;

            var scope = _view.PronunciationScope;
            string lang = _currentSubject == Constants.Subject.Chinese ? "zh" : "en";

            if (scope == PronunciationScope.Original || scope == PronunciationScope.Both)
            {
                string text = item.GetMainContent();
                await _ttsService.SpeakAsync(text, lang);
                await Task.Delay(500, cancellationToken);
            }

            if ((scope == PronunciationScope.Explanation || scope == PronunciationScope.Both) && !string.IsNullOrWhiteSpace(explanation))
            {
                await _ttsService.SpeakAsync(explanation, lang);
            }
        }

        public async Task MarkAsKnownAsync()
        {
            _studyEngine.MarkCurrentAsKnown();
            SaveProgress();
            await MoveToNextAsync();
        }

        public async Task MarkAsUnknownAsync()
        {
            _studyEngine.MarkCurrentAsUnknown();
            SaveProgress();
            await MoveToNextAsync();
        }

        private void SaveProgress()
        {
            try
            {
                _studyEngine.SaveProgress();
                _logger.LogInformation("Learning progress saved for user {UserId}", _currentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save learning progress");
            }
        }

        public async Task HandlePronounceAsync()
        {
            try
            {
                _autoPronunciationCount = 0;
                var item = _studyEngine.GetCurrentItem();
                if (item != null)
                {
                    await PlayPronunciationAsync(item, _currentExplanation, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandlePronounceAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandlePronounceAsync");
            }
        }

        public async Task MoveToNextAsync()
        {
            try
            {
                _cts?.Token.ThrowIfCancellationRequested();
                _autoPronunciationCount = 0;

                if (_studyEngine.HasNext())
                {
                    _studyEngine.MoveNext();
                    await DisplayCurrentItemAsync();
                }
                else
                {
                    _view.CurrentContent = "学习已完成!";
                    _view.Statistics = "恭喜完成所有内容！";
                    _view.EnableButtons(false);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MoveToNext was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MoveToNext");
            }
        }

        public async Task HandleItemSelectedAsync(int index)
        {
            try
            {
                _cts?.Token.ThrowIfCancellationRequested();
                _autoPronunciationCount = 0;

                _studyEngine.SetCurrentIndex(index);
                await DisplayCurrentItemAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleItemSelectedAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleItemSelectedAsync, index: {Index}", index);
            }
        }

        public async Task HandleSettingsChangedAsync()
        {
            try
            {
                _logger.LogInformation("Settings changed");

                string newSubject = _view.Subject;
                string newSubCategory = _view.SubCategory;
                string newMode = _view.LearningMode;
                string newSortOrder = _view.SortOrder;

                bool subjectChanged = newSubject != _currentSubject;
                bool subCategoryChanged = newSubCategory != _currentSubCategory;
                bool modeChanged = newMode != _studyEngine.CurrentMode;
                bool sortChanged = newSortOrder != _studyEngine.CurrentSortOrder;

                if (subjectChanged)
                {
                    _currentSubject = newSubject;
                    var subCategories = _contentLoaderService.GetSubCategoriesBySubject(newSubject);
                    _view.RefreshSubCategories(subCategories);

                    if (subCategories.Count > 0)
                    {
                        newSubCategory = subCategories[0];
                        _currentSubCategory = newSubCategory;
                    }
                }
                else if (subCategoryChanged)
                {
                    _currentSubCategory = newSubCategory;
                }

                if (subjectChanged || subCategoryChanged)
                {
                    string userId = string.IsNullOrWhiteSpace(_currentUserId) ? "default" : _currentUserId;
                    _studyEngine.Initialize(userId, _currentSubject, _currentSubCategory, "", newMode, newSortOrder, true);
                }
                else if (modeChanged || sortChanged)
                {
                    _studyEngine.ApplySettings(newMode, newSortOrder);
                }

                UpdateLearningList();
                await DisplayCurrentItemAsync();
                _view.EnableButtons(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reload learning content");
                _view.ShowMessage($"重新加载学习内容失败：{ex.Message}");
            }
        }

        public void Exit()
        {
            _studyEngine.SaveProgress();
            _settingsManager.SaveSettings(_view);
        }

        public void OpenStatistics()
        {
            try
            {
                _windowManager.OpenStatisticsWindow();
                _logger.LogInformation("Opened statistics window");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open statistics window");
                _view.ShowMessage($"打开统计窗口失败：{ex.Message}");
            }
        }

        public void SendToPdfQuestion()
        {
            var item = _studyEngine.GetCurrentItem();
            if (item != null)
            {
                OnSendToPdfQuestion?.Invoke(this, new SendToPdfEventArgs
                {
                    Text = item.GetMainContent(),
                    Language = _currentSubject
                });
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _ttsService.StopAsync().ConfigureAwait(false);
            _logger.LogInformation("LearningFlowHandler disposed");
        }

        public event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }
}