using LearningAssistant.Abstractions;
using LearningAssistant.Common;
using LearningAssistant.Models.Config;
using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Tesseract;

namespace LearningAssistant.Services.Pdf
{
    public class TesseractOcrService : IOcrService, IDisposable
    {
        private TesseractEngine? _engine;
        // TesseractEngine 非线程安全，识别与语言切换必须串行，避免并发识别产生乱码或访问已释放引擎
        private readonly SemaphoreSlim _engineLock = new SemaphoreSlim(1, 1);
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

            // 等待进行中的识别结束再重建引擎，避免识别期间引擎被释放
            _engineLock.Wait();
            try
            {
                if (_engine != null)
                {
                    _engine.Dispose();
                    _engine = null;
                }

                return InitializeEngineInternal(language);
            }
            finally
            {
                _engineLock.Release();
            }
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
                _engineLock.Wait();
                try
                {
                    var engine = _engine;
                    if (engine == null) return string.Empty;

                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    var bytes = ms.ToArray();
                    using var pix = Pix.LoadFromMemory(bytes);
                    using var page = engine.Process(pix);
                    return page.GetText() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "OCR识别失败");
                    return string.Empty;
                }
                finally
                {
                    _engineLock.Release();
                }
            });
        }

        public async Task<LearningAssistant.Models.Pdf.OcrLayoutResult?> RecognizeLayoutAsync(byte[] image)
        {
            if (_engine == null) return null;

            var bmp = BytesToBitmap(image);
            if (bmp == null) return null;

            return await Task.Run(() =>
            {
                _engineLock.Wait();
                try
                {
                    var engine = _engine;
                    if (engine == null) return null;

                    using var ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    using var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = engine.Process(pix);

                    var result = new LearningAssistant.Models.Pdf.OcrLayoutResult
                    {
                        ImageWidth = bmp.Width,
                        ImageHeight = bmp.Height,
                        Text = page.GetText()?.Trim() ?? string.Empty
                    };

                    using var iterator = page.GetIterator();
                    iterator.Begin();
                    do
                    {
                        if (!iterator.TryGetBoundingBox(PageIteratorLevel.TextLine, out var rect))
                            continue;

                        var lineText = iterator.GetText(PageIteratorLevel.TextLine);
                        if (string.IsNullOrWhiteSpace(lineText))
                            continue;

                        int width = rect.X2 - rect.X1;
                        int height = rect.Y2 - rect.Y1;
                        if (width <= 0 || height <= 0)
                            continue;

                        result.Lines.Add(new LearningAssistant.Models.Pdf.OcrTextLine
                        {
                            Text = System.Text.RegularExpressions.Regex.Replace(lineText, @"\s+", " ").Trim(),
                            Bounds = new RectInt(rect.X1, rect.Y1, width, height)
                        });
                    }
                    while (iterator.Next(PageIteratorLevel.TextLine));

                    return result;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "OCR 行框识别失败");
                    return null;
                }
                finally
                {
                    bmp.Dispose();
                    _engineLock.Release();
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
                _engineLock.Wait();
                try
                {
                    var engine = _engine;
                    if (engine == null) return string.Empty;

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
                            using (var page = engine.Process(pix))
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
                            using (var originalPage = engine.Process(originalPix))
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
                finally
                {
                    _engineLock.Release();
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
            // 统一为 24bpp，后续 LockBits 可固定按 3 字节/像素处理
            var grayImage = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
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
            int width = grayImage.Width;
            int height = grayImage.Height;
            var result = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            int threshold = CalculateGlobalThreshold(grayImage);
            int margin = 15;
            int lowerThreshold = Math.Max(0, threshold - margin);
            int upperThreshold = Math.Min(255, threshold + margin);

            var rect = new Rectangle(0, 0, width, height);
            var srcData = grayImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var dstData = result.LockBits(rect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                int srcStride = srcData.Stride;
                int dstStride = dstData.Stride;
                int srcBytes = srcStride * height;
                int dstBytes = dstStride * height;

                var srcBuffer = new byte[srcBytes];
                var dstBuffer = new byte[dstBytes];
                Marshal.Copy(srcData.Scan0, srcBuffer, 0, srcBytes);

                for (int y = 0; y < height; y++)
                {
                    int srcRow = y * srcStride;
                    int dstRow = y * dstStride;
                    for (int x = 0; x < width; x++)
                    {
                        int si = srcRow + x * 3;
                        int brightness = (srcBuffer[si] + srcBuffer[si + 1] + srcBuffer[si + 2]) / 3;

                        byte output;
                        if (brightness > upperThreshold)
                        {
                            output = 255;
                        }
                        else if (brightness < lowerThreshold)
                        {
                            output = 0;
                        }
                        else
                        {
                            output = (byte)brightness;
                        }

                        int di = dstRow + x * 3;
                        dstBuffer[di] = output;
                        dstBuffer[di + 1] = output;
                        dstBuffer[di + 2] = output;
                    }
                }

                Marshal.Copy(dstBuffer, 0, dstData.Scan0, dstBytes);
            }
            finally
            {
                grayImage.UnlockBits(srcData);
                result.UnlockBits(dstData);
            }

            return result;
        }

        private int CalculateGlobalThreshold(Bitmap grayImage)
        {
            int[] histogram = new int[256];
            long totalPixels = (long)grayImage.Width * grayImage.Height;

            var rect = new Rectangle(0, 0, grayImage.Width, grayImage.Height);
            var data = grayImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            try
            {
                int stride = data.Stride;
                int bytes = stride * grayImage.Height;
                var buffer = new byte[bytes];
                Marshal.Copy(data.Scan0, buffer, 0, bytes);

                for (int y = 0; y < grayImage.Height; y++)
                {
                    int row = y * stride;
                    for (int x = 0; x < grayImage.Width; x++)
                    {
                        int i = row + x * 3;
                        int brightness = (buffer[i] + buffer[i + 1] + buffer[i + 2]) / 3;
                        histogram[brightness]++;
                    }
                }
            }
            finally
            {
                grayImage.UnlockBits(data);
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
            _engineLock.Wait();
            try
            {
                _engine?.Dispose();
                _engine = null;
            }
            finally
            {
                _engineLock.Release();
            }
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
            using var source = Image.FromStream(ms);
            return new Bitmap(source);
        }
    }
}
