using LearningAssistant.Abstractions;
using LearningAssistant.Common;
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

        public async Task<string?> RecognizeTextAsync(byte[] image)
        {
            if (!_ocrService.IsAvailable)
                return null;

            return await _ocrService.RecognizeTextAsync(image);
        }

        public async Task<string?> RecognizeTextAsync(byte[] image, RectInt region)
        {
            if (!_ocrService.IsAvailable)
                return null;

            if (region.Width <= 0 || region.Height <= 0)
            {
                return await _ocrService.RecognizeTextAsync(image);
            }

            var bmp = BytesToBitmap(image);
            if (bmp == null) return null;
            var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);

            if (rect.X < 0 || rect.Y < 0 ||
                rect.X + rect.Width > bmp.Width ||
                rect.Y + rect.Height > bmp.Height)
            {
                _logger.LogWarning("OCR region out of bounds");
                return null;
            }

            using var cropped = CropImage(bmp, rect);
            return await _ocrService.RecognizeTextAsync(BitmapToBytes(cropped));
        }

        public async Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image)
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

        public async Task<string?> RecognizeTextWithAutoLanguageAsync(byte[] image, RectInt region)
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
                var bmp = BytesToBitmap(image);
                if (bmp == null) return null;
                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);

                if (rect.X < 0 || rect.Y < 0 ||
                    rect.X + rect.Width > bmp.Width ||
                    rect.Y + rect.Height > bmp.Height)
                {
                    _logger.LogWarning("OCR region out of bounds");
                    return null;
                }

                using var cropped = CropImage(bmp, rect);
                recognizedText = await _ocrService.RecognizeTextAsync(BitmapToBytes(cropped));
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

        private static byte[]? BitmapToBytes(Bitmap? bmp)
        {
            if (bmp == null) return null;
            using var ms = new MemoryStream();
            bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private static Bitmap? BytesToBitmap(byte[]? data)
        {
            if (data == null || data.Length == 0) return null;
            using var ms = new MemoryStream(data);
            return new Bitmap(ms);
        }
    }
}