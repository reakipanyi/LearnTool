using LearningAssistant.Services.TTS;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfTtsService : IPdfTtsService
    {
        private readonly ILogger<PdfTtsService> _logger;
        private readonly ITTSService _ttsService;

        public PdfTtsService(ILogger<PdfTtsService> logger, ITTSService ttsService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
        }

        public bool IsAvailable => _ttsService.Available;

        public async Task SpeakTextAsync(string text, string language, float speed)
        {
            if (!_ttsService.Available)
                return;

            await _ttsService.SpeakAsync(text, language, speed);
        }

        public async Task SpeakTextAsync(string text, float speed = 1.0f)
        {
            if (!_ttsService.Available)
                return;

            bool isChinese = text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
            string lang = isChinese ? "zh" : "en";
            await _ttsService.SpeakAsync(text, lang, speed);
        }
    }
}