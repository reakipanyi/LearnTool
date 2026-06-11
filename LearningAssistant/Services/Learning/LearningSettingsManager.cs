using LearningAssistant.Models.Config;
using LearningAssistant.Services.Persistence;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Learning
{
    public interface ILearningSettingsManager
    {
        void LoadInitialSettings(ILearningView view);
        void SaveSettings(ILearningView view);
    }

    public class LearningSettingsManager : ILearningSettingsManager
    {
        private readonly ILogger<LearningSettingsManager> _logger;
        private readonly IDataPersistenceService? _persistenceService;
        private bool _settingsSaved = false;

        public LearningSettingsManager(ILogger<LearningSettingsManager> logger, IDataPersistenceService? persistenceService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _persistenceService = persistenceService;
        }

        public void LoadInitialSettings(ILearningView view)
        {
            if (_persistenceService == null)
                return;

            try
            {
                var config = _persistenceService.LoadConfig();
                view.IsVoiceEnabled = config.AppSettings.IsVoiceEnabled;
                view.IsAIExplanationEnabled = config.AppSettings.IsAIExplanationEnabled;

                var scope = config.AppSettings.PronunciationScope;
                view.PronunciationScope = scope == 0 ? PronunciationScope.Original
                                          : scope == 1 ? PronunciationScope.Explanation
                                          : PronunciationScope.Both;

                _logger.LogInformation("Initial settings loaded from config: Voice={Voice}, AI={AI}, Scope={Scope}",
                    config.AppSettings.IsVoiceEnabled, config.AppSettings.IsAIExplanationEnabled, scope);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load initial settings from config, using defaults");
            }
        }

        public void SaveSettings(ILearningView view)
        {
            if (_settingsSaved)
            {
                _logger.LogDebug("Settings already saved, skipping");
                return;
            }

            try
            {
                if (_persistenceService != null)
                {
                    var config = _persistenceService.LoadConfig();
                    config.AppSettings.IsVoiceEnabled = view.IsVoiceEnabled;
                    config.AppSettings.IsAIExplanationEnabled = view.IsAIExplanationEnabled;
                    config.AppSettings.PronunciationScope = (int)view.PronunciationScope;
                    _persistenceService.SaveConfig(config);
                    _settingsSaved = true;
                    _logger.LogInformation("Settings saved to config file");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings to config");
            }
        }

        public void ResetSavedFlag()
        {
            _settingsSaved = false;
        }
    }
}