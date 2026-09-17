using LearningAssistant.Abstractions;
using Microsoft.Extensions.Logging;
using System.Drawing.Drawing2D;

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
                var bmp = BytesToBitmap(image);
                if (bmp == null)
                    return (null, null);

                var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);

                if (rect.X < 0 || rect.Y < 0 ||
                    rect.X + rect.Width > bmp.Width ||
                    rect.Y + rect.Height > bmp.Height)
                {
                    _logger.LogWarning("OCR region out of bounds");
                    return (null, null);
                }

                using var cropped = CropImage(bmp, rect);
                var recognizedText = await _ocrService.RecognizeTextAsync(BitmapToBytes(cropped));

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