using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Tesseract;

namespace LearningAssistant.Services.Pdf
{
    public class TesseractOcrService : IOcrService, IDisposable
    {
        private TesseractEngine? _engine;
        private readonly OcrConfig _config;
        private bool _initialized = false;
        private string? _initErrorMessage;
        private string _currentLanguage = "eng";
        private string _tessDataPath = string.Empty;
        private bool _enablePreprocessing = true;


        public TesseractOcrService(OcrConfig config)
        {
            _config = config;
            _currentLanguage = string.IsNullOrWhiteSpace(_config.Language) ? "eng" : _config.Language;
            InitializeEngine();
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

                if (!Directory.Exists(AppPaths.TesseractDataDir))
                {
                    _initErrorMessage = $"Tesseract数据目录不存在，已在 {AppPaths.TesseractDataDir} 创建目录。\n\n请从 https://github.com/tesseract-ocr/tessdata 下载语言数据文件（如 chi_sim.traineddata、eng.traineddata）并放入该目录";
                    return false;
                }

                _tessDataPath = AppPaths.TesseractDataDir;

                var langFiles = language.Split('+')
                    .Select(lang => Path.Combine(AppPaths.TesseractDataDir, $"{lang}.traineddata"))
                    .ToList();

                var missingFiles = langFiles.Where(f => !File.Exists(f)).ToList();
                if (missingFiles.Any())
                {
                    var missingList = string.Join("\n", missingFiles);
                    _initErrorMessage = $"缺少语言数据文件:\n{missingList}\n\n当前目录: {AppPaths.TesseractDataDir}\n\n请从 https://github.com/tesseract-ocr/tessdata 下载所需的语言数据文件";
                    return false;
                }

                _engine = new TesseractEngine(AppPaths.TesseractDataDir, language, EngineMode.Default);
                _engine.DefaultPageSegMode = PageSegMode.Auto;
                _currentLanguage = language;
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
        public async Task<string> RecognizeTextAsync(Bitmap bmp)
        {
            if (_engine == null) return string.Empty;

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
                catch
                {
                    return string.Empty;
                }
            });
        }

        public async Task<string> RecognizeTextAsync(Bitmap image, Rectangle region)
        {
            if (_engine == null || image == null)
                return string.Empty;

            return await Task.Run(() =>
            {
                try
                {
                    bool needCrop = region.X > 0 || region.Y > 0 ||
                                    region.Width < image.Width || region.Height < image.Height;

                    Bitmap processedImage = null;
                    Bitmap imageToProcess = null;
                    Bitmap originalToProcess = null;

                    try
                    {
                        processedImage = _enablePreprocessing ? PreprocessImage(image) : (Bitmap)image.Clone();
                        imageToProcess = needCrop ? processedImage.Clone(region, processedImage.PixelFormat) : processedImage;

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
                        originalToProcess = needCrop ? image.Clone(region, image.PixelFormat) : image;

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
                        if (originalToProcess != null && !object.ReferenceEquals(originalToProcess, image))
                            originalToProcess.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"OCR识别异常: {ex.Message}");
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








    }
}
