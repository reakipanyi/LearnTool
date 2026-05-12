using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Models.Config;
using UnifiedLearningAssistant.Services.Persistence;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Presenters
{
    public class SettingPresenter : IDisposable
    {
        private readonly ILogger<SettingPresenter> _logger;
        private readonly ISettingView _view;
        private readonly IDataPersistenceService _persistenceService;
        private AppConfig _config;

        public SettingPresenter(ILogger<SettingPresenter> logger, ISettingView view, IDataPersistenceService persistenceService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _persistenceService = persistenceService ?? throw new ArgumentNullException(nameof(persistenceService));

            _view.SaveClicked += View_SaveClicked;
            _view.CancelClicked += View_CancelClicked;
            _logger.LogInformation("SettingPresenter initialized");
        }

        public void Initialize()
        {
            _logger.LogInformation("Initializing SettingPresenter");
            _config = _persistenceService.LoadConfig();
            LoadConfigToView();
        }

        private void LoadConfigToView()
        {
            _view.ApiKey = _config.AiConfig.ApiKey;
            _view.ApiEndpoint = _config.AiConfig.BaseUrl;
            _view.Model = _config.AiConfig.Model;
            _view.TTSEnabled = !string.IsNullOrWhiteSpace(_config.TtsConfig.ApiKey);
            _view.TtsApiKey = _config.TtsConfig.ApiKey;
            _view.TtsVoice = _config.TtsConfig.Voice;
            _view.TTSSpeed = (int)(_config.TtsConfig.Speed * 100);
            _view.TTSVolume = (int)(_config.TtsConfig.Volume * 100);
            _view.FontSize = _config.AppSettings.DefaultFontSize;
            _view.Theme = _config.AppSettings.Theme;
            _view.BaiduAppId = _config.TranslationConfig.BaiduAppId;
            _view.BaiduSecret = _config.TranslationConfig.BaiduSecret;
        }

        private void View_SaveClicked(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Saving settings");
                _config.AiConfig.ApiKey = _view.ApiKey;
                _config.AiConfig.BaseUrl = _view.ApiEndpoint;
                _config.AiConfig.Model = _view.Model;
                _config.TtsConfig.ApiKey = _view.TTSEnabled ? _view.TtsApiKey : "";
                _config.TtsConfig.Voice = _view.TtsVoice;
                _config.TtsConfig.Speed = _view.TTSSpeed / 100f;
                _config.TtsConfig.Volume = _view.TTSVolume / 100f;
                _config.AppSettings.DefaultFontSize = _view.FontSize;
                _config.AppSettings.Theme = _view.Theme;
                _config.TranslationConfig.BaiduAppId = _view.BaiduAppId;
                _config.TranslationConfig.BaiduSecret = _view.BaiduSecret;

                _persistenceService.SaveConfig(_config);
                _persistenceService.PersistCache();
                _view.ShowMessage("设置已保存");
                _view.CloseView();
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                _view.ShowMessage($"保存失败: {ex.Message}");
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