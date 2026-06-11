using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfTranslationService : IPdfTranslationService
    {
        private readonly ILogger<PdfTranslationService> _logger;
        private readonly IOcrService _ocrService;
        private readonly ITranslationService _translationService;

        public PdfTranslationService(ILogger<PdfTranslationService> logger, IOcrService ocrService, ITranslationService translationService)
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

            return await _translationService.TranslateAsync(text);
        }

        public async Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image)
        {
            if (!_ocrService.IsAvailable)
                return (null, null);

            try
            {
                var recognizedText = await _ocrService.RecognizeTextAsync(image);
                if (string.IsNullOrWhiteSpace(recognizedText))
                    return (null, null);

                var translation = await _translationService.TranslateAsync(recognizedText);
                return (recognizedText, translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR and translate failed");
                return (null, null);
            }
        }

        public async Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image, Rectangle region)
        {
            if (!_ocrService.IsAvailable)
                return (null, null);

            if (region.Width <= 0 || region.Height <= 0)
            {
                return await OcrAndTranslateAsync(image);
            }

            try
            {
                if (region.X < 0 || region.Y < 0 ||
                    region.X + region.Width > image.Width ||
                    region.Y + region.Height > image.Height)
                {
                    _logger.LogWarning("OCR region out of bounds");
                    return (null, null);
                }

                using var cropped = image.Clone(region, image.PixelFormat);
                var recognizedText = await _ocrService.RecognizeTextAsync(cropped);
                
                if (string.IsNullOrWhiteSpace(recognizedText))
                    return (null, null);

                var translation = await _translationService.TranslateAsync(recognizedText);
                return (recognizedText, translation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR and translate with region failed");
                return (null, null);
            }
        }
    }
}