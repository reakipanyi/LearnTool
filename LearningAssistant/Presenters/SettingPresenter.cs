using Microsoft.Extensions.Logging;
using LearningAssistant.Common;
using LearningAssistant.Forms;
using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Views;

namespace LearningAssistant.Presenters
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
            _view.OpenWebViewClicked += View_OpenWebViewClicked;
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
            _view.Provider = _config.AiConfig.Provider;
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
            _view.BaiduNetdiskClientId = _config.CloudStorageConfig.BaiduClientId;
            _view.BaiduNetdiskClientSecret = _config.CloudStorageConfig.BaiduClientSecret;
            _view.IsVoiceEnabled = _config.AppSettings.IsVoiceEnabled;
            _view.PronunciationScope = _config.AppSettings.PronunciationScope;
            _view.IsAIExplanationEnabled = _config.AppSettings.IsAIExplanationEnabled;
        }

        private void View_SaveClicked(object? sender, EventArgs e)
        {
            try
            {
                _logger.LogInformation("Saving settings");
                
                // 验证 Provider
                var provider = _view.Provider;
                if (string.IsNullOrEmpty(provider) || !AiConfig.Providers.ContainsKey(provider))
                {
                    _view.ShowMessage("请选择有效的 AI 服务商");
                    return;
                }
                
                _config.AiConfig.Provider = provider;
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
                _config.CloudStorageConfig.BaiduClientId = _view.BaiduNetdiskClientId;
                _config.CloudStorageConfig.BaiduClientSecret = _view.BaiduNetdiskClientSecret;
                _config.AppSettings.IsVoiceEnabled = _view.IsVoiceEnabled;
                _config.AppSettings.PronunciationScope = _view.PronunciationScope;
                _config.AppSettings.IsAIExplanationEnabled = _view.IsAIExplanationEnabled;

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

        private void View_OpenWebViewClicked(object? sender, EventArgs e)
        {
            try
            {
                var provider = _view.Provider;
                if (!string.IsNullOrEmpty(provider) && AiConfig.Providers.TryGetValue(provider, out var providerInfo))
                {
                    var webviewUrl = providerInfo.WebViewUrl;
                    if (!string.IsNullOrEmpty(webviewUrl))
                    {
                        var webViewForm = new AIWebViewForm(initialUrl: webviewUrl);
                        webViewForm.Show();
                    }
                    else
                    {
                        _view.ShowMessage("该服务商暂不支持网页版");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open WebView");
                _view.ShowMessage($"打开网页版失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _view.SaveClicked -= View_SaveClicked;
            _view.CancelClicked -= View_CancelClicked;
            _view.OpenWebViewClicked -= View_OpenWebViewClicked;
            _logger.LogInformation("SettingPresenter disposed");
        }
    }
}
