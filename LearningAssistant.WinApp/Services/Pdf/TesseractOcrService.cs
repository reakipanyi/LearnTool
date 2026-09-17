using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using Tesseract;

namespace LearningAssistant.Services.Pdf
{
    public class TesseractOcrService : IOcrService, IDisposable
    {
        private TesseractEngine? _engine;
        private readonly OcrConfig _config;
        private readonly ILogger<TesseractOcrService>? _logger;
        private bool _initialized = false;
        private string? _initErrorMessage;
        private string _currentLanguage = "eng";
        private string _tessDataPath = string.Empty;
        private bool _enablePreprocessing = true;
        private readonly IAppPaths _appPaths;


        public TesseractOcrService(OcrConfig config, IAppPaths appPaths, ILogger<TesseractOcrService>? logger = null)
        {
            _config = config;
            _appPaths = appPaths ?? throw new ArgumentNullException(nameof(appPaths));
            _logger = logger;
            _currentLanguage = string.IsNullOrWhiteSpace(_config.Language) ? "chi_sim+eng" : _config.Language;
            StartBackgroundInitialization();
        }

        private void StartBackgroundInitialization()
        {
            if (_initialized) return;
            _initialized = true;

            Task.Run(() =>
            {
                try
                {
                    InitializeEngineInternal(_currentLanguage);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "Background OCR initialization failed");
                }
            });
        }

        public bool IsAvailable => _engine != null;

        public string? InitErrorMessage => _initErrorMessage;

        public string CurrentLanguage => _currentLanguage;

        public bool EnablePreprocessing
        {
            get => _enablePreprocessing;
            set => _enablePreprocessing = value;
        }

        private bool InitializeEngineInternal(string language)
        {
            try
            {

                if (!Directory.Exists(_appPaths.TesseractDataDir))
                {
                    _initErrorMessage = $"Tesseract数据目录不存在，已在 {_appPaths.TesseractDataDir} 创建目录。\n\n请从 https://github.com/tesseract-ocr/tessdata 下载语言数据文件（如 chi_sim.traineddata、eng.traineddata）并放入该目录";
                    return false;
                }

                _tessDataPath = _appPaths.TesseractDataDir;

                var resolvedLanguage = ResolveLanguageWithFallback(language);
                if (resolvedLanguage == null)
                    return false;

                _engine = new TesseractEngine(_appPaths.TesseractDataDir, resolvedLanguage, EngineMode.Default);
                _engine.DefaultPageSegMode = PageSegMode.Auto;
                _currentLanguage = resolvedLanguage;
                _initErrorMessage = null;
                return true;
            }
            catch (DllNotFoundException ex)
            {
                _initErrorMessage = $"无法加载Tesseract原生库: {ex.Message}\n请确保已安装Tesseract运行时或相关依赖";
                return false;
            }
            catch (Exception ex)
            {
                _initErrorMessage = $"OCR引擎初始化失败: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
                return false;
            }
        }

        /// <summary>
        /// 解析语言参数，若目标语言的数据文件缺失则自动降级。
        /// 优先返回所有文件均存在的语言组合；若纯英语文件也不存在则返回 null。
        /// </summary>
        private string? ResolveLanguageWithFallback(string language)
        {
            var langs = language.Split('+');
            var missing = new List<string>();
            foreach (var lang in langs)
            {
                var path = Path.Combine(_appPaths.TesseractDataDir, $"{lang}.traineddata");
                if (!File.Exists(path))
                    missing.Add(lang);
            }

            if (missing.Count == 0)
                return language;

            // 组合语言中部分缺失 → 尝试降级为仅 eng
            _logger?.LogWarning("OCR语言数据文件缺失: {Missing}，降级为 eng", string.Join(", ", missing));
            var engPath = Path.Combine(_appPaths.TesseractDataDir, "eng.traineddata");
            if (File.Exists(engPath))
                return "eng";

            // eng 也不存在 → 报错
            var missingList = string.Join("\n", langs.Select(l => Path.Combine(_appPaths.TesseractDataDir, $"{l}.traineddata")));
            _initErrorMessage = $"缺少语言数据文件:\n{missingList}\n\n当前目录: {_appPaths.TesseractDataDir}\n\n请从 https://github.com/tesseract-ocr/tessdata 下载所需的语言数据文件";
            return null;
        }

        private void InitializeEngine()
        {
            if (_initialized)
                return;

            _initialized = true;
            InitializeEngineInternal(_currentLanguage);
        }

        public bool SetLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
                return false;

            if (_engine != null)
            {
                _engine.Dispose();
                _engine = null;
            }

            return InitializeEngineInternal(language);
        }

        public void SetPageSegMode(PageSegMode mode)
        {
            _engine?.SetVariable("tessedit_pageseg_mode", ((int)mode).ToString());
        }
        /*
        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            return await RecognizeTextAsync(image, new Rectangle(0, 0, image.Width, image.Height));
        } 
        */
        public async Task<string> RecognizeTextAsync(byte[] image)
        {
            if (_engine == null) return string.Empty;

            var bmp = BytesToBitmap(image);
            if (bmp == null) return string.Empty;

            return await Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = ms.ToArray();
                    using var pix = Pix.LoadFromMemory(bytes);
                    using var page = _engine.Process(pix);
                    return page.GetText() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "OCR识别失败");
                    return string.Empty;
                }
            });
        }

        public async Task<string> RecognizeTextAsync(byte[] image, RectInt region)
        {
            if (_engine == null)
                return string.Empty;

            var bmp = BytesToBitmap(image);
            if (bmp == null)
                return string.Empty;

            var rect = new Rectangle(region.X, region.Y, region.Width, region.Height);

            return await Task.Run(() =>
            {
                try
                {
                    bool needCrop = rect.X > 0 || rect.Y > 0 ||
                                    rect.Width < bmp.Width || rect.Height < bmp.Height;

                    Bitmap processedImage = null;
                    Bitmap imageToProcess = null;
                    Bitmap originalToProcess = null;

                    try
                    {
                        processedImage = _enablePreprocessing ? PreprocessImage(bmp) : (Bitmap)bmp.Clone();
                        imageToProcess = needCrop ? processedImage.Clone(rect, processedImage.PixelFormat) : processedImage;

                        using (var ms = new MemoryStream())
                        {
                            imageToProcess.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                            ms.Position = 0;

                            using (var pix = Pix.LoadFromMemory(ms.ToArray()))
                            using (var page = _engine.Process(pix))
                            {
                                var text = page.GetText()?.Trim() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(text))
                                    return CleanAndNormalizeText(text);
                            }
                        }
                    }
                    finally
                    {
                        if (imageToProcess != null && !object.ReferenceEquals(imageToProcess, processedImage))
                            imageToProcess.Dispose();
                        if (processedImage != null && _enablePreprocessing)
                            processedImage.Dispose();
                    }

                    try
                    {
                        originalToProcess = needCrop ? bmp.Clone(rect, bmp.PixelFormat) : bmp;

                        using (var originalMs = new MemoryStream())
                        {
                            originalToProcess.Save(originalMs, System.Drawing.Imaging.ImageFormat.Png);
                            originalMs.Position = 0;

                            using (var originalPix = Pix.LoadFromMemory(originalMs.ToArray()))
                            using (var originalPage = _engine.Process(originalPix))
                            {
                                var originalText = originalPage.GetText()?.Trim() ?? string.Empty;
                                return CleanAndNormalizeText(originalText);
                            }
                        }
                    }
                    finally
                    {
                        if (originalToProcess != null && !object.ReferenceEquals(originalToProcess, bmp))
                            originalToProcess.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "OCR识别异常");
                    return string.Empty;
                }
            });
        }

        private string CleanAndNormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Replace("\r\n", "\n");
            text = text.Replace("\r", "\n");

            text = text.Replace("\n\n", "\n");

            // 在换行前后添加空格，避免文字连在一起
            // 使用正则表达式在换行前后添加空格，但避免重复空格
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\S)\n(\S)", "$1 $2");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\n(\S)", " $1");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\S)\n", "$1 ");

            // 最后将剩余的换行符替换为空格
            text = text.Replace("\n", " ");

            text = text.Replace("  ", " ");
            text = text.Replace("  ", " ");

            text = text.Replace("。 ", "。");
            text = text.Replace("！ ", "！");
            text = text.Replace("？ ", "？");
            text = text.Replace("； ", "；");
            text = text.Replace("： ", "：");

            text = text.Trim();

            return text;
        }

        private Bitmap PreprocessImage(Bitmap image)
        {
            var grayImage = new Bitmap(image.Width, image.Height);
            using (var g = Graphics.FromImage(grayImage))
            {
                var grayMatrix = new System.Drawing.Imaging.ColorMatrix(new float[][]
                {
                    new float[] { 0.299f, 0.299f, 0.299f, 0, 0 },
                    new float[] { 0.587f, 0.587f, 0.587f, 0, 0 },
                    new float[] { 0.114f, 0.114f, 0.114f, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { 0, 0, 0, 0, 1 }
                });
                var attributes = new System.Drawing.Imaging.ImageAttributes();
                attributes.SetColorMatrix(grayMatrix);
                g.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                    0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }

            var binarizedImage = ApplyAdaptiveThreshold(grayImage);

            grayImage.Dispose();
            return binarizedImage;
        }

        private Bitmap ApplyAdaptiveThreshold(Bitmap grayImage)
        {
            var result = new Bitmap(grayImage.Width, grayImage.Height);

            int threshold = CalculateGlobalThreshold(grayImage);
            int margin = 15;
            int lowerThreshold = Math.Max(0, threshold - margin);
            int upperThreshold = Math.Min(255, threshold + margin);

            for (int y = 0; y < grayImage.Height; y++)
            {
                for (int x = 0; x < grayImage.Width; x++)
                {
                    var pixel = grayImage.GetPixel(x, y);
                    int brightness = (pixel.R + pixel.G + pixel.B) / 3;

                    Color outputColor;
                    if (brightness > upperThreshold)
                    {
                        outputColor = Color.White;
                    }
                    else if (brightness < lowerThreshold)
                    {
                        outputColor = Color.Black;
                    }
                    else
                    {
                        outputColor = pixel;
                    }
                    result.SetPixel(x, y, outputColor);
                }
            }

            return result;
        }

        private int CalculateGlobalThreshold(Bitmap grayImage)
        {
            int[] histogram = new int[256];
            long totalPixels = (long)grayImage.Width * grayImage.Height;

            for (int y = 0; y < grayImage.Height; y++)
            {
                for (int x = 0; x < grayImage.Width; x++)
                {
                    var pixel = grayImage.GetPixel(x, y);
                    int brightness = (pixel.R + pixel.G + pixel.B) / 3;
                    histogram[brightness]++;
                }
            }

            double sum = 0;
            for (int i = 0; i < 256; i++)
                sum += (long)i * histogram[i];

            double sumB = 0;
            long wB = 0;
            long wF = 0;
            double maxVariance = 0;
            int threshold = 128;

            for (int i = 0; i < 256; i++)
            {
                wB += histogram[i];
                if (wB == 0) continue;

                wF = totalPixels - wB;
                if (wF == 0) break;

                sumB += (long)i * histogram[i];
                double mB = sumB / wB;
                double mF = (sum - sumB) / wF;

                double variance = wB * wF * (mB - mF) * (mB - mF);

                if (variance > maxVariance)
                {
                    maxVariance = variance;
                    threshold = i;
                }
            }

            return threshold;
        }

        public void Dispose()
        {
            _engine?.Dispose();
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
