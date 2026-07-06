using KidWinApp.Services;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

namespace LearningAssistant.Services.Pdf
{
    public class PdfOcrService : IPdfOcrService
    {
        private readonly ILogger<PdfOcrService> _logger;
        private readonly IOcrService _ocrService;
        private string? _currentLanguage;

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

            using var cropped = CropImage(image, region);
            return await _ocrService.RecognizeTextAsync(cropped);
        }

        public async Task<string?> RecognizeTextWithAutoLanguageAsync(Bitmap image)
        {
            if (!_ocrService.IsAvailable)
                return null;

            var recognizedText = await _ocrService.RecognizeTextAsync(image);
            if (!string.IsNullOrWhiteSpace(recognizedText))
            {
                await AutoDetectAndSetLanguageAsync(recognizedText);
            }
            return recognizedText;
        }

        public async Task<string?> RecognizeTextWithAutoLanguageAsync(Bitmap image, Rectangle region)
        {
            if (!_ocrService.IsAvailable)
                return null;

            string? recognizedText;

            if (region.Width <= 0 || region.Height <= 0)
            {
                recognizedText = await _ocrService.RecognizeTextAsync(image);
            }
            else
            {
                if (region.X < 0 || region.Y < 0 ||
                    region.X + region.Width > image.Width ||
                    region.Y + region.Height > image.Height)
                {
                    _logger.LogWarning("OCR region out of bounds");
                    return null;
                }

                using var cropped = CropImage(image, region);
                recognizedText = await _ocrService.RecognizeTextAsync(cropped);
            }

            if (!string.IsNullOrWhiteSpace(recognizedText))
            {
                await AutoDetectAndSetLanguageAsync(recognizedText);
            }

            return recognizedText;
        }

        private async Task AutoDetectAndSetLanguageAsync(string text)
        {
            var langType = StringLanguageDetector.DetectLanguage(text);
            var targetLang = langType switch
            {
                LanguageType.Chinese => "chi_sim",
                LanguageType.English => "eng",
                LanguageType.Mixed => "chi_sim+eng",
                _ => "eng"
            };

            if (targetLang != _currentLanguage)
            {
                _logger?.LogInformation("自动检测到语言类型 {LangType}，切换OCR语言为 {TargetLang}", langType, targetLang);
                var success = SetLanguage(targetLang);
                if (success)
                {
                    _currentLanguage = targetLang;
                }
                await Task.CompletedTask;
            }
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

        private static Bitmap CropImage(Bitmap source, Rectangle region)
        {
            var cropped = new Bitmap(region.Width, region.Height);
            using var graphics = Graphics.FromImage(cropped);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, region.Width, region.Height),
                              region, GraphicsUnit.Pixel);
            return cropped;
        }
    }
}