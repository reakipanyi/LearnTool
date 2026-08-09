using LearningAssistant.Common;
using LearningAssistant.Common.Events;
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
        void HandleSearchTextChanged(string searchText);
        void Exit();
        void OpenStatistics();
        void SendToPdfQuestion();

        event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;

        Task HandleFieldSpeakAsync(FieldSpeakEventArgs args);
        Task HandleFieldStopAsync();
    }

    public class LearningFlowHandler : ILearningFlowHandler
    {
        private readonly ILogger<LearningFlowHandler> _logger;
        private readonly IStudyEngine _studyEngine;
        private readonly IAIService? _aiService;
        private readonly ITTSService? _ttsService;
        private readonly ISpeechCoordinator? _speechCoordinator;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IExportService _exportService;
        private readonly IWindowManager _windowManager;
        private readonly ILearningSettingsManager _settingsManager;
        private readonly ILearningView _view;
        private readonly IEventBus? _eventBus;
        private readonly ISpacedRepetitionService? _spacedRepetitionService;

        private CancellationTokenSource? _cts;
        private string _currentExplanation = "";
        private bool _isLoading = false;
        private string _currentUserId = "";
        private SubjectType _currentSubject = SubjectType.Chinese;
        private SubCategoryType _currentSubCategory = SubCategoryType.ChineseCharacter;
        private int _autoPronunciationCount = 0;
        private readonly DateTime _sessionStartTime = DateTime.Now;
        private const int MaxAutoPronunciationCount = 5;
        
        private int _pendingSaveCount = 0;
        private const int SaveBatchSize = 10;

        public LearningFlowHandler(
            ILogger<LearningFlowHandler> logger,
            IStudyEngine studyEngine,
            IAIService? aiService,
            ITTSService? ttsService,
            ISpeechCoordinator? speechCoordinator,
            IContentLoaderService contentLoaderService,
            IExportService exportService,
            IWindowManager windowManager,
            ILearningSettingsManager settingsManager,
            ILearningView view,
            IEventBus? eventBus = null,
            ISpacedRepetitionService? spacedRepetitionService = null)
        {
            _logger = logger;
            _studyEngine = studyEngine;
            _aiService = aiService;
            _ttsService = ttsService;
            _speechCoordinator = speechCoordinator;
            _contentLoaderService = contentLoaderService;
            _exportService = exportService;
            _windowManager = windowManager;
            _settingsManager = settingsManager;
            _view = view;
            _eventBus = eventBus;
            _spacedRepetitionService = spacedRepetitionService;
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
            _currentSubject = _view.Subject;
            _autoPronunciationCount = 0;

            var selectedSubCategory = await LoadSubCategoriesAsync(_currentSubject, subCategory);
            _currentSubCategory = selectedSubCategory;

            var context = _view.CurrentContext with { UserId = userId, WordBankFile = wordBankFile, SubCategory = selectedSubCategory };
            _studyEngine.Initialize(context, continueMode);
            UpdateLearningList();
            SyncLearningItemStates();
            await DisplayCurrentItemAsync();
        }

        private async Task<SubCategoryType> LoadSubCategoriesAsync(SubjectType subject, string subCategory)
        {
            try
            {
                var subCategories = _contentLoaderService.GetSubCategories(subject);
                _view.RefreshSubCategories(subCategories);

                if (string.IsNullOrEmpty(subCategory) || !subCategories.Any(s => s.ToString().Equals(subCategory, StringComparison.OrdinalIgnoreCase)))
                {
                    return _view.SubCategory;
                }
                else
                {
                    if (SubjectSubCategoryMapping.TryParseSubCategory(subCategory, out var parsedSubCat))
                    {
                        _view.SubCategory = parsedSubCat;
                        return parsedSubCat;
                    }
                    return _view.SubCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load subcategories");
                return _view.SubCategory;
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

        private void SyncLearningItemStates()
        {
            try
            {
                var knownItems = new HashSet<string>(_studyEngine.KnownItems);
                var unknownItems = new HashSet<string>(_studyEngine.UnknownItems);
                _view.UpdateLearningItemStates(knownItems, unknownItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync learning item states");
            }
        }

        private async Task DisplayCurrentItemAsync()
        {
            // 取消之前的操作，确保立即响应新的切换请求
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // 停止当前正在播放的发音，避免切换时产生杂音
            if (_speechCoordinator != null)
            {
                await _speechCoordinator.StopAsync();
            }

            _isLoading = true;
            try
            {
                var item = _studyEngine.GetCurrentItem();
                if (item == null)
                {
                    _view.CurrentContent = "学习已完成!";
                    _view.Statistics = "恭喜完成所有内容！";
                    _view.EnableButtons(false);
                    _logger.LogInformation("Learning session completed");
                    return;
                }

                _currentExplanation = item.Meaning?.Content ?? "";

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
                    if (_ttsService == null)
                    {
                        _logger.LogWarning("TTS service is null, cannot play pronunciation");
                    }
                    else if (!_ttsService.Available)
                    {
                        _logger.LogWarning("TTS service is not available");
                    }
                    else
                    {
                        _autoPronunciationCount++;
                        await PlayPronunciationAsync(item, _currentExplanation, _cts.Token);
                    }
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
            if (_speechCoordinator == null) return;

            try
            {
                var scope = _view.PronunciationScope;
                string lang = _currentSubject == SubjectType.Chinese ? "zh" : "en";

                _logger.LogInformation("Playing pronunciation - Scope: {Scope}, Language: {Lang}, Text: {Text}, Explanation: {Explanation}", 
                    scope, lang, item.GetMainContent(), explanation);

                string text = item.GetMainContent();
                
                string? speakText = null;
                string? speakExplanation = null;
                
                if (scope == PronunciationScope.Original || scope == PronunciationScope.Both)
                    speakText = text;
                
                if ((scope == PronunciationScope.Explanation || scope == PronunciationScope.Both) && 
                    !string.IsNullOrWhiteSpace(explanation))
                    speakExplanation = explanation;

                if (!string.IsNullOrWhiteSpace(speakText) || !string.IsNullOrWhiteSpace(speakExplanation))
                {
                    await _speechCoordinator.SpeakAsync(speakText ?? text, lang, speakExplanation, cancellationToken, "__GLOBAL__");
                }

                await PreloadNextPronunciationAsync();
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("PlayPronunciationAsync was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to play pronunciation");
            }
        }

        private async Task PreloadNextPronunciationAsync()
        {
            if (!_view.IsVoiceEnabled || _speechCoordinator == null) return;

            try
            {
                var nextIndex = _studyEngine.CurrentIndex + 1;
                if (nextIndex < _studyEngine.TotalCount)
                {
                    var allItems = _studyEngine.GetAllItems();
                    var nextItem = allItems[nextIndex];
                    string text = nextItem.GetMainContent();
                    string lang = _currentSubject == SubjectType.Chinese ? "zh" : "en";

                    await _speechCoordinator.PreloadAsync(text, lang);
                    _logger.LogDebug("Preloaded pronunciation for: {Text}", text);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Preload pronunciation failed (non-critical)");
            }
        }

        

        public async Task MarkAsKnownAsync()
        {
            var currentItem = _studyEngine.GetCurrentItem();
            _studyEngine.MarkCurrentAsKnown();
            UpdateStatistics();
            SaveProgress();

            if (currentItem != null)
            {
                SyncToSpacedRepetition(currentItem.GetMainContent(), currentItem.GetDisplayText(), true);

                if (_eventBus != null)
                {
                    _eventBus.Publish(new ItemLearnedEvent
                    {
                        UserId = _currentUserId,
                        ItemId = currentItem.GetMainContent(),
                        ItemContent = currentItem.GetMainContent(),
                        SubCategory = _currentSubCategory.ToString(),
                        LearnedAt = DateTime.Now
                    });
                    _logger.LogInformation("Published ItemLearnedEvent for user {UserId}, item {ItemContent}", _currentUserId, currentItem.GetMainContent());
                }
            }

            await MoveToNextAsync();
        }

        private void SyncToSpacedRepetition(string content, string answer, bool isKnown)
        {
            if (_spacedRepetitionService == null) return;
            if (string.IsNullOrWhiteSpace(content)) return;

            try
            {
                var allItems = _spacedRepetitionService.GetAllItems(_currentUserId);
                var existingItem = allItems.FirstOrDefault(i =>
                    string.Equals(i.Content.Trim(), content.Trim(), StringComparison.OrdinalIgnoreCase));

                if (isKnown)
                {
                    if (existingItem != null)
                    {
                        _spacedRepetitionService.CalculateNextReview(existingItem, 4);
                    }
                    else
                    {
                        var newItem = _spacedRepetitionService.CreateNewItem(_currentUserId, content, answer);
                        newItem.Category = _currentSubCategory.ToString();
                        newItem.Subject = _currentSubject.ToString();
                        _spacedRepetitionService.UpdateItem(newItem);
                        _spacedRepetitionService.CalculateNextReview(newItem, 4);
                    }
                }
                else
                {
                    if (existingItem != null)
                    {
                        _spacedRepetitionService.CalculateNextReview(existingItem, 2);
                    }
                    else
                    {
                        var newItem = _spacedRepetitionService.CreateNewItem(_currentUserId, content, answer);
                        newItem.Category = _currentSubCategory.ToString();
                        newItem.Subject = _currentSubject.ToString();
                        _spacedRepetitionService.UpdateItem(newItem);
                        _spacedRepetitionService.CalculateNextReview(newItem, 2);
                    }
                }

                _logger.LogDebug("同步到间隔重复系统: 用户 {UserId}, 内容 {Content}, 已知 {IsKnown}",
                    _currentUserId, content.Substring(0, Math.Min(20, content.Length)), isKnown);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "同步到间隔重复系统失败: {Content}", content);
            }
        }

        public async Task MarkAsUnknownAsync()
        {
            var currentItem = _studyEngine.GetCurrentItem();
            _studyEngine.MarkCurrentAsUnknown();
            UpdateStatistics();
            SaveProgress();

            if (currentItem != null)
            {
                SyncToSpacedRepetition(currentItem.GetMainContent(), currentItem.GetDisplayText(), false);

                if (_eventBus != null)
                {
                    _eventBus.Publish(new ItemWrongEvent
                    {
                        UserId = _currentUserId,
                        ItemId = currentItem.GetMainContent(),
                        ItemContent = currentItem.GetMainContent(),
                        CorrectAnswer = currentItem.GetDisplayText(),
                        UserAnswer = "",
                        SubCategory = _currentSubCategory.ToString(),
                        WrongAt = DateTime.Now
                    });
                    _logger.LogInformation("Published ItemWrongEvent for user {UserId}, item {ItemContent}", _currentUserId, currentItem.GetMainContent());
                }
            }

            await MoveToNextAsync();
        }

        private void SaveProgress()
        {
            _pendingSaveCount++;
            
            if (_pendingSaveCount >= SaveBatchSize)
            {
                _pendingSaveCount = 0;
                PerformSaveProgress();
            }
        }

        private void PerformSaveProgress()
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
                    await PlayPronunciationAsync(item, _currentExplanation, _cts?.Token ?? CancellationToken.None);
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

        public async Task HandleFieldSpeakAsync(FieldSpeakEventArgs args)
        {
            try
            {
                _autoPronunciationCount = 0;
                
                if (_speechCoordinator != null)
                {
                    _logger.LogInformation("Field speak requested - Text: {Text}, Language: {Lang}, Key: {Key}", 
                        args.SpeakText, args.Language, args.SpeakKey);
                    
                    await _speechCoordinator.SpeakAsync(args.SpeakText, args.Language, CancellationToken.None, args.SpeakKey);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("HandleFieldSpeakAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleFieldSpeakAsync");
            }
        }

        public async Task HandleFieldStopAsync()
        {
            try
            {
                if (_speechCoordinator != null)
                {
                    await _speechCoordinator.StopAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in HandleFieldStopAsync");
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

                    PublishSessionCompleted();
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

        private void PublishSessionCompleted()
        {
            if (_eventBus == null) return;

            var stats = _studyEngine.GetStatistics();
            var sessionDuration = DateTime.Now - _sessionStartTime;

            _eventBus.Publish(new LearningSessionCompletedEvent
            {
                UserId = _currentUserId,
                TotalItems = _studyEngine.TotalCount,
                CorrectCount = stats.CorrectCount,
                Accuracy = stats.AccuracyRate,
                SubCategory = _currentSubCategory.ToString(),
                Duration = sessionDuration
            });
            _logger.LogInformation("Published LearningSessionCompletedEvent for user {UserId}, duration {Duration}", _currentUserId, sessionDuration);
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

        public void HandleSearchTextChanged(string searchText)
        {
            try
            {
                var allItems = _studyEngine.GetAllItems();
                var filtered = string.IsNullOrWhiteSpace(searchText)
                    ? allItems
                    : allItems.Where(i => i.GetMainContent().Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                                         i.GetDisplayText().Contains(searchText, StringComparison.OrdinalIgnoreCase)).ToList();

                _view.UpdateLearningList(filtered.Select(i => i.GetMainContent()).ToList(), 0);
                _logger.LogInformation("Search text changed to: {SearchText}, filtered count: {Count}", searchText, filtered.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to handle search text change: {SearchText}", searchText);
            }
        }

        public async Task HandleSettingsChangedAsync()
        {
            try
            {
                _logger.LogInformation("Settings changed");
                _autoPronunciationCount = 0;

                var newSubject = _view.Subject;
                var newSubCategory = _view.SubCategory;
                var newMode = _view.LearningMode;
                var newSortOrder = _view.SortOrder;
                var newUserId = _view.CurrentContext.UserId;

                bool subjectChanged = newSubject != _currentSubject;
                bool subCategoryChanged = newSubCategory != _currentSubCategory;
                bool modeChanged = newMode != _studyEngine.CurrentMode;
                bool sortChanged = newSortOrder != _studyEngine.CurrentSortOrder;
                bool userIdChanged = newUserId != _currentUserId;

                if (subjectChanged)
                {
                    _currentSubject = newSubject;
                    var subCategories = _contentLoaderService.GetSubCategories(newSubject);
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

                if (userIdChanged)
                {
                    _currentUserId = newUserId;
                }

                if (subjectChanged || subCategoryChanged || userIdChanged)
                {
                    string userId = string.IsNullOrWhiteSpace(_currentUserId) ? Constants.DefaultUserId : _currentUserId;
                    var context = _view.CurrentContext with { UserId = userId, SubCategory = _currentSubCategory, WordBankFile = "" };
                    _studyEngine.Initialize(context, true);
                    SyncLearningItemStates();
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
                string lang = _currentSubject == SubjectType.Chinese ? "zh" : "en";
                
                if (_eventBus != null)
                {
                    _eventBus.Publish(new SendToPdfSearchEvent
                    {
                        UserId = _currentUserId,
                        SearchText = item.GetMainContent(),
                        Language = lang
                    });
                    _logger.LogInformation("Published SendToPdfSearchEvent for text: {Text}", item.GetMainContent());
                }
                else
                {
                    OnSendToPdfQuestion?.Invoke(this, new SendToPdfEventArgs
                    {
                        Text = item.GetMainContent(),
                        Language = lang
                    });
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            if (_speechCoordinator != null)
            {
                try
                {
                    Task.Run(async () => await _speechCoordinator.StopAsync()).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to stop speech coordinator during dispose");
                }
            }
            
            _logger.LogInformation("LearningFlowHandler disposed");
        }

        public event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }
}