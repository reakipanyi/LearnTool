using LearningAssistant.Common;
using LearningAssistant.Models.Learning;
using LearningAssistant.Services;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public class LearningPresenter : IDisposable
    {
        private readonly ILogger<LearningPresenter> _logger;
        private readonly ILearningView _view;
        private readonly IStudyEngine _studyEngine;
        private readonly IAIService _aiService;
        private readonly ITTSService _ttsService;
        private readonly IContentLoaderService _contentLoaderService;
        private readonly IExportService _exportService;
        private readonly IWindowManager _windowManager;
        private readonly ILearningSettingsManager _settingsManager;
        private readonly ILearningExportService _exportHelper;
        private CancellationTokenSource? _cts;
        private string _currentExplanation = "";
        private bool _isLoading = false;

        private string _currentUserId = "";
        private string _currentLanguage = "";
        private string _currentSubCategory = "";
        private int _autoPronunciationCount = 0;
        private const int MaxAutoPronunciationCount = 5;

        public LearningPresenter(ILogger<LearningPresenter> logger, ILearningView view, IStudyEngine studyEngine, 
            IAIService aiService, ITTSService ttsService, IContentLoaderService contentLoaderService, 
            IExportService exportService, IWindowManager windowManager,
            ILearningSettingsManager settingsManager, ILearningExportService exportHelper)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _contentLoaderService = contentLoaderService ?? throw new ArgumentNullException(nameof(contentLoaderService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _windowManager = windowManager ?? throw new ArgumentNullException(nameof(windowManager));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _exportHelper = exportHelper ?? throw new ArgumentNullException(nameof(exportHelper));
            _cts = new CancellationTokenSource();

            SubscribeToEvents();
            LoadInitialSettings();
            _logger.LogInformation("LearningPresenter initialized");
        }

        private void LoadInitialSettings()
        {
            _settingsManager.LoadInitialSettings(_view);
        }

        private void SubscribeToEvents()
        {
            _view.MarkAsKnownClicked += View_MarkAsKnownClicked;
            _view.MarkAsUnknownClicked += View_MarkAsUnknownClicked;
            _view.PronounceClicked += View_PronounceClicked;
            _view.NextClicked += View_NextClicked;
            _view.ExitClicked += View_ExitClicked;
            _view.AddToPdfQuestionClicked += View_AddToPdfQuestionClicked;
            _view.SettingsChanged += View_SettingsChanged;
            _view.OpenStatisticsClicked += View_OpenStatisticsClicked;
            _view.ExportErrorBookClicked += View_ExportErrorBookClicked;
        }

        private void UnsubscribeFromEvents()
        {
            _view.MarkAsKnownClicked -= View_MarkAsKnownClicked;
            _view.MarkAsUnknownClicked -= View_MarkAsUnknownClicked;
            _view.PronounceClicked -= View_PronounceClicked;
            _view.NextClicked -= View_NextClicked;
            _view.ExitClicked -= View_ExitClicked;
            _view.AddToPdfQuestionClicked -= View_AddToPdfQuestionClicked;
            _view.SettingsChanged -= View_SettingsChanged;
            _view.OpenStatisticsClicked -= View_OpenStatisticsClicked;
            _view.ExportErrorBookClicked -= View_ExportErrorBookClicked;
        }

        public void Initialize(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Initializing learning session for user {UserId}, category {SubCategory}, continueMode={ContinueMode}", userId, subCategory, continueMode);
            InitializeCore(userId, language, subCategory, wordBankFile, continueMode).ConfigureAwait(false);
        }

        public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Async initializing learning session for user {UserId}, category {SubCategory}, continueMode={ContinueMode}", userId, subCategory, continueMode);
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
            _currentLanguage = language;

            await LoadSubCategoriesAsync(language, ref subCategory);

            _currentSubCategory = subCategory;

            string mode = _view.LearningMode;
            string sortOrder = _view.SortOrder;

            _studyEngine.Initialize(userId, language, subCategory, wordBankFile, mode, sortOrder, continueMode);
            UpdateLearningList();
            await DisplayCurrentItemAsync();
        }

        private Task LoadSubCategoriesAsync(string language, ref string subCategory)
        {
            try
            {
                var subCategories = _contentLoaderService.GetSubCategories(language);
                _view.RefreshSubCategories(subCategories);

                if (string.IsNullOrEmpty(subCategory) || !subCategories.Contains(subCategory))
                {
                    subCategory = _view.SubCategory;
                }
                else
                {
                    _view.SubCategory = subCategory;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load subcategories");
            }
            return Task.CompletedTask;
        }

        private void SaveProgress()
        {
            try
            {
                _studyEngine.SaveProgress();
                _logger.LogInformation("Learning progress saved for user {UserId}, category {SubCategory}", _currentUserId, _currentSubCategory);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save learning progress");
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
            if (_isLoading)
            {
                _logger.LogDebug("DisplayCurrentItemAsync already running, skipping");
                return;
            }

            _isLoading = true;

            try
            {
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                _currentExplanation = "";
                _view.AIExplanation = "";

                var item = _studyEngine.GetCurrentItem();
                if (item == null)
                {
                    _view.CurrentContent = "学习已完成!";
                    _view.Statistics = "恭喜完成所有内容！";
                    _view.EnableButtons(false);
                    _logger.LogInformation("Learning session completed");
                    return;
                }

                _view.SetCurrentItem(item);
                _view.CurrentContent = item.GetMainContent();
                _view.CurrentDisplayText = item.GetDisplayText();
                _view.ProgressMax = _studyEngine.TotalCount;
                _view.ProgressValue = _studyEngine.CurrentIndex + 1;

                UpdateStatistics();
                UpdateListSelection();

                if (_studyEngine.CurrentMode == Constants.LearningMode.Study && _view.IsAIExplanationEnabled)
                {
                    await LoadAIExplanationAsync(item.GetMainContent(), _cts.Token);
                }
                else
                {
                    _view.AIExplanation = "";
                }

                if (_view.IsVoiceEnabled && _autoPronunciationCount < MaxAutoPronunciationCount)
                {
                    await PlayPronunciationAsync(item, _currentExplanation, _cts.Token);
                    _autoPronunciationCount++;
                }
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

        private async Task LoadAIExplanationAsync(string text, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Loading AI explanation for '{Text}'", text);
                var explanation = await _aiService.GetExplanationAsync(text, _currentLanguage, _currentSubCategory);
                _currentExplanation = explanation;

                var modelName = _aiService.ModelName;
                _view.AIExplanation = $"【{modelName}】\n{explanation}";
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("LoadAIExplanationAsync was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load AI explanation");
                _view.AIExplanation = "无法获取解释";
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
            string lang = _currentLanguage == Constants.Language.Chinese ? "zh" : "en";

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

        private async void View_MarkAsKnownClicked(object? sender, EventArgs e)
        {
            _studyEngine.MarkCurrentAsKnown();
            SaveProgress();
            await MoveToNextAsync();
        }

        private async void View_MarkAsUnknownClicked(object? sender, EventArgs e)
        {
            _studyEngine.MarkCurrentAsUnknown();
            SaveProgress();
            await MoveToNextAsync();
        }

        private async void View_PronounceClicked(object? sender, EventArgs e)
        {
            await HandlePronounceAsync();
        }

        private async Task HandlePronounceAsync()
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

        private async void View_NextClicked(object? sender, EventArgs e)
        {
            await MoveToNextAsync();
        }

        private async Task MoveToNextAsync()
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

        private void View_ExitClicked(object? sender, EventArgs e)
        {
            _studyEngine.SaveProgress();
            _settingsManager.SaveSettings(_view);
            OnExit?.Invoke(this, EventArgs.Empty);
        }

        private void View_AddToPdfQuestionClicked(object? sender, EventArgs e)
        {
            var item = _studyEngine.GetCurrentItem();
            if (item != null)
            {
                OnSendToPdfQuestion?.Invoke(this, new SendToPdfEventArgs
                {
                    Text = item.GetMainContent(),
                    Language = _currentLanguage
                });
            }
        }

        private async void View_SettingsChanged(object? sender, EventArgs e)
        {
            await HandleSettingsChangedAsync();
        }

        private async Task HandleSettingsChangedAsync()
        {
            try
            {
                _logger.LogInformation("Settings changed");

                string newLanguage = _view.Language;
                string newSubCategory = _view.SubCategory;
                string newMode = _view.LearningMode;
                string newSortOrder = _view.SortOrder;

                bool languageChanged = newLanguage != _currentLanguage;
                bool subCategoryChanged = newSubCategory != _currentSubCategory;
                bool modeChanged = newMode != _studyEngine.CurrentMode;
                bool sortChanged = newSortOrder != _studyEngine.CurrentSortOrder;

                if (languageChanged)
                {
                    _currentLanguage = newLanguage;
                    var subCategories = _contentLoaderService.GetSubCategories(newLanguage);
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

                if (languageChanged || subCategoryChanged)
                {
                    string userId = string.IsNullOrWhiteSpace(_currentUserId) ? "default" : _currentUserId;
                    _studyEngine.Initialize(userId, _currentLanguage, _currentSubCategory, "", newMode, newSortOrder, true);
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
                _logger.LogError(ex, "Failed to reload learning content after settings changed");
                _view.ShowMessage($"重新加载学习内容失败：{ex.Message}");
            }
        }

        private void View_OpenStatisticsClicked(object? sender, EventArgs e)
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

        private void View_ExportErrorBookClicked(object? sender, EventArgs e)
        {
            string result = _exportHelper.ExportErrorBook(_exportService, _currentUserId);
            _view.ShowMessage(result);
        }

        public void Dispose()
        {
            _settingsManager.SaveSettings(_view);
            UnsubscribeFromEvents();
            _cts?.Cancel();
            _cts?.Dispose();
            _ttsService.StopAsync().ConfigureAwait(false);
            _logger.LogInformation("LearningPresenter disposed");
        }

        public event EventHandler? OnExit;
        public event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }

    public class SendToPdfEventArgs : EventArgs
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}