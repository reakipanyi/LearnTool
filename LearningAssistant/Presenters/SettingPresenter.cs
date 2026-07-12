using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public class SettingPresenter : IDisposable
    {
        private readonly ILogger<SettingPresenter> _logger;
        private readonly ISettingView _view;
        private readonly IDataPersistenceService _persistenceService;
        private readonly AppConfig _appConfig;
        private readonly IServiceProvider _serviceProvider;

        public SettingPresenter(ILogger<SettingPresenter> logger, ISettingView view, IDataPersistenceService persistenceService, AppConfig appConfig, IServiceProvider serviceProvider)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));
            _appConfig = appConfig ?? throw new ArgumentNullException(nameof(appConfig));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _view.SaveClicked += View_SaveClicked;
            _view.CancelClicked += View_CancelClicked;
            _logger.LogInformation("SettingPresenter initialized");
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing SettingPresenter");
            LoadConfigToView();
        }

        private void LoadConfigToView()
        {
            var provider = string.IsNullOrWhiteSpace(_appConfig.TtsConfig.Provider) ? TtsProviders.KokoroSharp : _appConfig.TtsConfig.Provider;
            _view.TtsProvider = provider;
            _view.TTSEnabled = provider.Equals(TtsProviders.KokoroSharp, StringComparison.OrdinalIgnoreCase) ||
                               !string.IsNullOrWhiteSpace(_appConfig.TtsConfig.ApiKey);
            _view.TtsApiKey = _appConfig.TtsConfig.ApiKey;
            _view.TtsVoice = _appConfig.TtsConfig.Voice;
            _view.TTSSpeed = (int)(_appConfig.TtsConfig.Speed * 100);
            _view.TTSVolume = (int)(_appConfig.TtsConfig.Volume * 100);
            _view.FontSize = _appConfig.AppSettings.DefaultFontSize;
            _view.Theme = _appConfig.AppSettings.Theme;
            _view.BaiduAppId = _appConfig.TranslationConfig.BaiduAppId;
            _view.BaiduSecret = _appConfig.TranslationConfig.BaiduSecret;
            _view.IsVoiceEnabled = _appConfig.AppSettings.IsVoiceEnabled;
            _view.PronunciationScope = _appConfig.AppSettings.PronunciationScope;
            _view.IsAIExplanationEnabled = _appConfig.AppSettings.IsAIExplanationEnabled;
        }

        private void View_SaveClicked(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Saving settings");

                string oldVoice = _appConfig.TtsConfig.Voice;
                string oldProvider = _appConfig.TtsConfig.Provider;

                _appConfig.TtsConfig.Provider = _view.TtsProvider;
                _appConfig.TtsConfig.ApiKey = _view.TtsApiKey;
                _appConfig.TtsConfig.Voice = _view.TtsVoice;
                _appConfig.TtsConfig.Speed = _view.TTSSpeed / 100f;
                _appConfig.TtsConfig.Volume = _view.TTSVolume / 100f;
                _appConfig.AppSettings.DefaultFontSize = _view.FontSize;
                _appConfig.AppSettings.Theme = _view.Theme;
                _appConfig.TranslationConfig.BaiduAppId = _view.BaiduAppId;
                _appConfig.TranslationConfig.BaiduSecret = _view.BaiduSecret;
                _appConfig.AppSettings.IsVoiceEnabled = _view.IsVoiceEnabled;
                _appConfig.AppSettings.PronunciationScope = _view.PronunciationScope;
                _appConfig.AppSettings.IsAIExplanationEnabled = _view.IsAIExplanationEnabled;

                _persistenceService.SaveConfig(_appConfig);
                _persistenceService.PersistCache();

                if (!string.Equals(oldVoice, _appConfig.TtsConfig.Voice, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(oldProvider, _appConfig.TtsConfig.Provider, StringComparison.OrdinalIgnoreCase))
                {
                    NotifyTtsServiceSettingsChanged();
                }

                //_view.ShowMessage("设置已保存");
                _view.CloseView();
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                _view.ShowMessage($"保存失败: {ex.Message}");
            }
        }

        private void NotifyTtsServiceSettingsChanged()
        {
            try
            {
                var ttsService = _serviceProvider.GetService<ITTSService>();
                if (ttsService is KokoroSharpTtsService kokoroService)
                {
                    kokoroService.ReloadVoiceSettings();
                    _logger.LogInformation("Notified KokoroSharp TTS service to reload voice settings");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to notify TTS service of settings change");
            }
        }

        private void View_CancelClicked(object? sender, EventArgs e)
        {
            _view.CloseView();
        }

        public void Dispose()
        {
            _view.SaveClicked -= View_SaveClicked;
            _view.CancelClicked -= View_CancelClicked;
            _logger.LogInformation("SettingPresenter disposed");
        }
    }
}