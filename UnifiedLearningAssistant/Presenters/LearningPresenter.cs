using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Learning;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Presenters
{
    public class LearningPresenter : IDisposable
    {
        private readonly ILogger<LearningPresenter> _logger;
        private readonly ILearningView _view;
        private readonly IStudyEngine _studyEngine;
        private readonly IAIService _aiService;
        private readonly ITTSService _ttsService;
        private CancellationTokenSource? _cts;

        private string _currentUserId = "";
        private string _currentLanguage = "";
        private string _currentSubCategory = "";

        public LearningPresenter(ILogger<LearningPresenter> logger, ILearningView view, IStudyEngine studyEngine, IAIService aiService, ITTSService ttsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _cts = new CancellationTokenSource();

            SubscribeToEvents();
            _logger.LogInformation("LearningPresenter initialized");
        }

        private void SubscribeToEvents()
        {
            _view.MarkAsKnownClicked += View_MarkAsKnownClicked;
            _view.MarkAsUnknownClicked += View_MarkAsUnknownClicked;
            _view.PronounceClicked += View_PronounceClicked;
            _view.NextClicked += View_NextClicked;
            _view.ExitClicked += View_ExitClicked;
            _view.AddToPdfQuestionClicked += View_AddToPdfQuestionClicked;
        }

        private void UnsubscribeFromEvents()
        {
            _view.MarkAsKnownClicked -= View_MarkAsKnownClicked;
            _view.MarkAsUnknownClicked -= View_MarkAsUnknownClicked;
            _view.PronounceClicked -= View_PronounceClicked;
            _view.NextClicked -= View_NextClicked;
            _view.ExitClicked -= View_ExitClicked;
            _view.AddToPdfQuestionClicked -= View_AddToPdfQuestionClicked;
        }

        public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, string mode, string sortOrder)
        {
            _logger.LogInformation("Initializing learning session for user {UserId}, category {SubCategory}", userId, subCategory);
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;

            _studyEngine.Initialize(userId, language, subCategory, wordBankFile, mode, sortOrder);
            await DisplayCurrentItemAsync();
        }

        private async Task DisplayCurrentItemAsync()
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

            _view.SetCurrentItem(item);
            _view.CurrentContent = item.GetMainContent();
            _view.CurrentDisplayText = item.GetDisplayText();
            _view.ProgressValue = _studyEngine.CurrentIndex + 1;
            _view.ProgressMax = _studyEngine.TotalCount;

            UpdateStatistics();

            if (_studyEngine.CurrentMode == Constants.LearningMode.Study)
            {
                await LoadAIExplanationAsync(item.GetMainContent(), _cts.Token);
            }
            else
            {
                _view.AIExplanation = "";
            }

            if (_view.IsVoiceEnabled)
            {
                await PlayPronunciationAsync(item, _cts.Token);
            }
        }

        private async Task LoadAIExplanationAsync(string text, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogDebug("Loading AI explanation for '{Text}'", text);
                var explanation = await _aiService.GetExplanationAsync(text, _currentLanguage, _currentSubCategory);
                _view.AIExplanation = explanation;
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

        private async Task PlayPronunciationAsync(LearningItem item, CancellationToken cancellationToken)
        {
            if (!_ttsService.Available) return;


            string text = item.GetMainContent();
            string lang = _currentLanguage == Constants.Language.Chinese ? "zh" : "en";
            await _ttsService.SpeakAsync(text, lang);

        }

        private void View_MarkAsKnownClicked(object? sender, EventArgs e)
        {
            _studyEngine.MarkCurrentAsKnown();
            MoveToNext();
        }

        private void View_MarkAsUnknownClicked(object? sender, EventArgs e)
        {
            _studyEngine.MarkCurrentAsUnknown();
            MoveToNext();
        }

        private async void View_PronounceClicked(object? sender, EventArgs e)
        {
            try
            {
                var item = _studyEngine.GetCurrentItem();
                if (item != null)
                {
                    await PlayPronunciationAsync(item, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("View_PronounceClicked was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_PronounceClicked");
            }
        }

        private void View_NextClicked(object? sender, EventArgs e)
        {
            MoveToNext();
        }

        private async void MoveToNext()
        {
            try
            {
                _cts?.Token.ThrowIfCancellationRequested();

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

        public void Dispose()
        {
            UnsubscribeFromEvents();
            _cts?.Cancel();
            _cts?.Dispose();
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