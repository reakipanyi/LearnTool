using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfTranslationService : IPdfTranslationService
    {
        private readonly ILogger<PdfTranslationService> _logger;
        private readonly IPdfOcrService _ocrService;
        private readonly ITranslationService _translationService;

        public PdfTranslationService(ILogger<PdfTranslationService> logger, IPdfOcrService ocrService, ITranslationService translationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
        }

        public bool IsAvailable => _translationService.IsAvailable;

        public async Task<string?> TranslateAsync(string text)
        {
            if (!_translationService.IsAvailable)
                return null;

            // 按原文语言自动选择目标语言：中文译英文，其余译中文，中英混排按页各自判定
            return await _translationService.TranslateAsync(text, "auto", ResolveTargetLanguage(text));
        }

        public async Task<string?> TranslateAsync(string text, string targetLanguage)
        {
            if (!_translationService.IsAvailable)
                return null;

            return await _translationService.TranslateAsync(text, "auto", targetLanguage);
        }

        public async Task<(string? Original, string? Translation)> OcrAndTranslateAsync(byte[] image)
        {
            if (!_ocrService.IsAvailable)
                return (null, null);

            try
            {
                var recognizedText = await _ocrService.RecognizeTextWithAutoLanguageAsync(image);
                if (string.IsNullOrWhiteSpace(recognizedText))
                    return (null, null);

                var translation = await _translationService.TranslateAsync(
                    recognizedText, "auto", ResolveTargetLanguage(recognizedText));
                return (recognizedText, translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR and translate failed");
                return (null, null);
            }
        }

        public async Task<(string? Original, string? Translation)> OcrAndTranslateAsync(byte[] image, RectInt region)
        {
            if (!_ocrService.IsAvailable)
                return (null, null);

            if (region.Width <= 0 || region.Height <= 0)
            {
                return await OcrAndTranslateAsync(image);
            }

            try
            {
                // 区域裁剪与越界校验统一由 PdfOcrService 处理，这里只做识别 + 翻译
                var recognizedText = await _ocrService.RecognizeTextWithAutoLanguageAsync(image, region);
                if (string.IsNullOrWhiteSpace(recognizedText))
                    return (null, null);

                var translation = await _translationService.TranslateAsync(
                    recognizedText, "auto", ResolveTargetLanguage(recognizedText));
                return (recognizedText, translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR and translate with region failed");
                return (null, null);
            }
        }

        /// <summary>中文原文译成英文，其余（英文/混排/未知）译成中文</summary>
        private static string ResolveTargetLanguage(string text)
            => StringLanguageDetector.DetectLanguage(text) == LanguageType.Chinese ? "en" : "zh";
    }
}