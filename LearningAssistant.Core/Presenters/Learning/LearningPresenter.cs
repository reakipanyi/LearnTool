using LearningAssistant.Managers;
using LearningAssistant.Services.Learning;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public class LearningPresenter : IDisposable
    {
        private readonly ILogger<LearningPresenter> _logger;
        private readonly ILearningView _view;
        private readonly ILearningFlowHandler _flowHandler;
        private readonly ILearningEventMediator _eventMediator;
        private readonly ILearningSettingsManager _settingsManager;

        public LearningPresenter(
            ILogger<LearningPresenter> logger,
            ILearningView view,
            ILearningFlowHandler flowHandler,
            ILearningEventMediator eventMediator,
            ILearningSettingsManager settingsManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _flowHandler = flowHandler ?? throw new ArgumentNullException(nameof(flowHandler));
            _eventMediator = eventMediator ?? throw new ArgumentNullException(nameof(eventMediator));
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));

            SubscribeToViewEvents();
            SubscribeToMediatorEvents();
            LoadInitialSettings();

            _flowHandler.OnSendToPdfQuestion += FlowHandler_OnSendToPdfQuestion;

            _logger.LogInformation("LearningPresenter initialized");
        }

        private void LoadInitialSettings()
        {
            _settingsManager.LoadInitialSettings(_view);
        }

        private void SubscribeToViewEvents()
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
            _view.ItemSelectedFromList += View_ItemSelectedFromList;
            _view.SearchTextChanged += View_SearchTextChanged;
            _view.FieldSpeakRequested += View_FieldSpeakRequested;
            _view.FieldStopRequested += View_FieldStopRequested;
            _view.FieldCopyRequested += View_FieldCopyRequested;
        }

        private void UnsubscribeFromViewEvents()
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
            _view.ItemSelectedFromList -= View_ItemSelectedFromList;
            _view.SearchTextChanged -= View_SearchTextChanged;
            _view.FieldSpeakRequested -= View_FieldSpeakRequested;
            _view.FieldStopRequested -= View_FieldStopRequested;
            _view.FieldCopyRequested -= View_FieldCopyRequested;
        }

        private void UnsubscribeFromMediatorEvents()
        {
            _eventMediator.MarkAsKnown -= MarkAsKnownHandler;
            _eventMediator.MarkAsUnknown -= MarkAsUnknownHandler;
            _eventMediator.Pronounce -= PronounceHandler;
            _eventMediator.Next -= NextHandler;
            _eventMediator.Exit -= ExitHandler;
            _eventMediator.SendToPdfQuestion -= SendToPdfQuestionHandler;
            _eventMediator.SettingsChanged -= SettingsChangedHandler;
            _eventMediator.OpenStatistics -= OpenStatisticsHandler;
            _eventMediator.FieldSpeakRequested -= FieldSpeakRequestedHandler;
            _eventMediator.FieldStopRequested -= FieldStopRequestedHandler;
        }

        private void SubscribeToMediatorEvents()
        {
            _eventMediator.MarkAsKnown += MarkAsKnownHandler;
            _eventMediator.MarkAsUnknown += MarkAsUnknownHandler;
            _eventMediator.Pronounce += PronounceHandler;
            _eventMediator.Next += NextHandler;
            _eventMediator.Exit += ExitHandler;
            _eventMediator.SendToPdfQuestion += SendToPdfQuestionHandler;
            _eventMediator.SettingsChanged += SettingsChangedHandler;
            _eventMediator.OpenStatistics += OpenStatisticsHandler;
            _eventMediator.FieldSpeakRequested += FieldSpeakRequestedHandler;
            _eventMediator.FieldStopRequested += FieldStopRequestedHandler;
        }

        private async void MarkAsKnownHandler(object? sender, MarkAsKnownEventArgs e) => await _flowHandler.MarkAsKnownAsync();
        private async void MarkAsUnknownHandler(object? sender, MarkAsUnknownEventArgs e) => await _flowHandler.MarkAsUnknownAsync();
        private async void PronounceHandler(object? sender, EventArgs e) => await _flowHandler.HandlePronounceAsync();
        private async void NextHandler(object? sender, EventArgs e) => await _flowHandler.MoveToNextAsync();
        private void ExitHandler(object? sender, EventArgs e) => _flowHandler.Exit();
        private void SendToPdfQuestionHandler(object? sender, SendToPdfEventArgs e) => HandleSendToPdfQuestion(e.Text, e.Language);
        private async void SettingsChangedHandler(object? sender, EventArgs e) => await _flowHandler.HandleSettingsChangedAsync();
        private void OpenStatisticsHandler(object? sender, EventArgs e) => _flowHandler.OpenStatistics();
        private async void FieldSpeakRequestedHandler(object? sender, FieldSpeakEventArgs e) => await _flowHandler.HandleFieldSpeakAsync(e);
        private async void FieldStopRequestedHandler(object? sender, EventArgs e) => await _flowHandler.HandleFieldStopAsync();

        public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Initializing learning session for user {UserId}, category {SubCategory}", userId, subCategory);
            await _flowHandler.InitializeAsync(userId, language, subCategory, wordBankFile, continueMode);
        }

        private void View_MarkAsKnownClicked(object? sender, EventArgs e)
        {
            try
            {
                _eventMediator.RaiseMarkAsKnown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记为已知失败");
            }
        }

        private void View_MarkAsUnknownClicked(object? sender, EventArgs e)
        {
            try
            {
                _eventMediator.RaiseMarkAsUnknown();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "标记为未知失败");
            }
        }

        private void View_PronounceClicked(object? sender, EventArgs e)
        {
            try
            {
                _eventMediator.RaisePronounce();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发音失败");
            }
        }

        private void View_NextClicked(object? sender, EventArgs e)
        {
            try
            {
                _eventMediator.RaiseNext();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "切换到下一项失败");
            }
        }

        private void View_ExitClicked(object? sender, EventArgs e)
        {
            _eventMediator.RaiseExit();
            OnExit?.Invoke(this, EventArgs.Empty);
        }

        private void View_AddToPdfQuestionClicked(object? sender, EventArgs e)
        {
            _eventMediator.RaiseSendToPdfQuestion("", "");
        }

        private void View_SettingsChanged(object? sender, EventArgs e)
        {
            _eventMediator.RaiseSettingsChanged();
        }

        private void View_OpenStatisticsClicked(object? sender, EventArgs e)
        {
            _eventMediator.RaiseOpenStatistics();
        }

        private void View_ExportErrorBookClicked(object? sender, EventArgs e)
        {
            _eventMediator.RaiseExportErrorBook();
        }

        private async void View_ItemSelectedFromList(object? sender, ItemSelectedEventArgs e)
        {
            try
            {
                await _flowHandler.HandleItemSelectedAsync(e.Index);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从列表选择项失败, Index: {Index}", e.Index);
            }
        }

        private void View_SearchTextChanged(object? sender, EventArgs e)
        {
            _flowHandler.HandleSearchTextChanged(_view.SearchText);
        }

        private void View_FieldSpeakRequested(object? sender, FieldSpeakEventArgs e)
        {
            try
            {
                _eventMediator.RaiseFieldSpeakRequested(e.SpeakText, e.Language, e.SpeakKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "字段发音请求失败");
            }
        }

        private void View_FieldStopRequested(object? sender, EventArgs e)
        {
            try
            {
                _eventMediator.RaiseFieldStopRequested();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "字段停止发音请求失败");
            }
        }

        private void View_FieldCopyRequested(object? sender, FieldCopyEventArgs e)
        {
            try
            {
                _view.CopyToClipboard(e.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "字段复制失败");
            }
        }

        private void HandleSendToPdfQuestion(string text, string language)
        {
            OnSendToPdfQuestion?.Invoke(this, new SendToPdfEventArgs { Text = text, Language = language });
        }

        private void FlowHandler_OnSendToPdfQuestion(object? sender, SendToPdfEventArgs e)
        {
            OnSendToPdfQuestion?.Invoke(this, e);
        }

        public void Dispose()
        {
            _settingsManager.SaveSettings(_view);
            UnsubscribeFromViewEvents();
            UnsubscribeFromMediatorEvents();
            (_flowHandler as IDisposable)?.Dispose();
            _logger.LogInformation("LearningPresenter disposed");
        }

        public event EventHandler? OnExit;
        public event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }
}