using LearningAssistant.Forms;
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
        }

        private void SubscribeToMediatorEvents()
        {
            _eventMediator.MarkAsKnown += async (_, _) => await _flowHandler.MarkAsKnownAsync();
            _eventMediator.MarkAsUnknown += async (_, _) => await _flowHandler.MarkAsUnknownAsync();
            _eventMediator.Pronounce += async (_, _) => await _flowHandler.HandlePronounceAsync();
            _eventMediator.Next += async (_, _) => await _flowHandler.MoveToNextAsync();
            _eventMediator.Exit += (_, _) => _flowHandler.Exit();
            _eventMediator.SendToPdfQuestion += (_, args) => HandleSendToPdfQuestion(args.Text, args.Language);
            _eventMediator.SettingsChanged += async (_, _) => await _flowHandler.HandleSettingsChangedAsync();
            _eventMediator.OpenStatistics += (_, _) => _flowHandler.OpenStatistics();
            _eventMediator.ExportErrorBook += (_, _) => _flowHandler.ExportErrorBook();
        }

        public void Initialize(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Initializing learning session for user {UserId}, category {SubCategory}", userId, subCategory);
            InitializeCore(userId, language, subCategory, wordBankFile, continueMode).ConfigureAwait(false);
        }

        public async Task InitializeAsync(string userId, string language, string subCategory, string wordBankFile, bool continueMode = true)
        {
            _logger.LogInformation("Async initializing learning session for user {UserId}, category {SubCategory}", userId, subCategory);
            await _flowHandler.InitializeAsync(userId, language, subCategory, wordBankFile, continueMode);
        }

        private async Task InitializeCore(string userId, string language, string subCategory, string wordBankFile, bool continueMode)
        {
            await _flowHandler.InitializeAsync(userId, language, subCategory, wordBankFile, continueMode);
        }

        private async void View_MarkAsKnownClicked(object? sender, EventArgs e)
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

        private async void View_MarkAsUnknownClicked(object? sender, EventArgs e)
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

        private async void View_PronounceClicked(object? sender, EventArgs e)
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

        private async void View_NextClicked(object? sender, EventArgs e)
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
            await _flowHandler.HandleItemSelectedAsync(e.Index);
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
            (_flowHandler as IDisposable)?.Dispose();
            _logger.LogInformation("LearningPresenter disposed");
        }

        public event EventHandler? OnExit;
        public event EventHandler<SendToPdfEventArgs>? OnSendToPdfQuestion;
    }
}