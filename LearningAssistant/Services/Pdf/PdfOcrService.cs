using Microsoft.Extensions.Logging;

namespace LearningAssistant.Services.Pdf
{
    public class PdfOcrService : IPdfOcrService
    {
        private readonly ILogger<PdfOcrService> _logger;
        private readonly IOcrService _ocrService;

        public PdfOcrService(ILogger<PdfOcrService> logger, IOcrService ocrService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
        }

        public bool IsAvailable => _ocrService.IsAvailable;

        public string? InitErrorMessage => _ocrService.InitErrorMessage;

        public async Task<string?> RecognizeTextAsync(Bitmap image)
        {
            if (!_ocrService.IsAvailable)
                return null;

            return await _ocrService.RecognizeTextAsync(image);
        }

        public async Task<string?> RecognizeTextAsync(Bitmap image, Rectangle region)
        {
            if (!_ocrService.IsAvailable)
                return null;

            if (region.Width <= 0 || region.Height <= 0)
            {
                return await _ocrService.RecognizeTextAsync(image);
            }

            if (region.X < 0 || region.Y < 0 ||
                region.X + region.Width > image.Width ||
                region.Y + region.Height > image.Height)
            {
                _logger.LogWarning("OCR region out of bounds");
                return null;
            }

            using var cropped = image.Clone(region, image.PixelFormat);
            return await _ocrService.RecognizeTextAsync(cropped);
        }

        public bool SetLanguage(string language)
        {
            try
            {
                return _ocrService.SetLanguage(language);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change OCR language to {Language}", language);
                return false;
            }
        }
    }
}