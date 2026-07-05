using LearningAssistant.Models.Config;
using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfTtsService : IPdfTtsService
    {
        private readonly ILogger<PdfTtsService> _logger;
        private readonly ITTSService _ttsService;
        private readonly TtsConfig _ttsConfig;

        public PdfTtsService(ILogger<PdfTtsService> logger, ITTSService ttsService, TtsConfig ttsConfig)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            _ttsConfig = ttsConfig ?? throw new ArgumentNullException(nameof(ttsConfig));
        }

        public bool IsAvailable => _ttsService.Available;

        public async Task SpeakTextAsync(string text, string language, float speed)
        {
            float actualSpeed = speed > 0 ? speed : _ttsConfig.Speed;
            _logger.LogInformation("PdfTtsService.SpeakTextAsync: paramSpeed={ParamSpeed}, actualSpeed={ActualSpeed}, configSpeed={ConfigSpeed}", 
                speed, actualSpeed, _ttsConfig.Speed);
            await _ttsService.SpeakAsync(text, language, actualSpeed);
        }

        public async Task SpeakTextAsync(string text, float speed = -1f)
        {
            float actualSpeed = speed > 0 ? speed : _ttsConfig.Speed;
            bool isChinese = text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
            string lang = isChinese ? "zh" : "en";
            _logger.LogInformation("PdfTtsService.SpeakTextAsync: paramSpeed={ParamSpeed}, actualSpeed={ActualSpeed}, lang={Lang}, configSpeed={ConfigSpeed}", 
                speed, actualSpeed, lang, _ttsConfig.Speed);
            await _ttsService.SpeakAsync(text, lang, actualSpeed);
        }
    }
}