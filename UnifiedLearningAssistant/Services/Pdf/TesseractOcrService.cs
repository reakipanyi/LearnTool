using Tesseract;
using UnifiedLearningAssistant.Models.Config;

namespace UnifiedLearningAssistant.Services.Pdf
{
    public class TesseractOcrService : IOcrService, IDisposable
    {
        private TesseractEngine? _engine;
        private readonly OcrConfig _config;
        private bool _initialized = false;
        private string? _initErrorMessage;

        public TesseractOcrService(OcrConfig config)
        {
            _config = config;
            InitializeEngine();
        }

        public bool IsAvailable => _engine != null;

        public string? InitErrorMessage => _initErrorMessage;

        private void InitializeEngine()
        {
            if (_initialized)
                return;

            _initialized = true;

            try
            {
                string tessDataPath;

                // 首先检查配置的路径
                if (!string.IsNullOrWhiteSpace(_config.DataPath))
                {
                    // 如果是相对路径，转换为相对于应用程序目录的绝对路径
                    if (!Path.IsPathRooted(_config.DataPath))
                    {
                        // 首先尝试相对于项目目录的 tessdata
                        var projectDir = AppDomain.CurrentDomain.BaseDirectory;
                        tessDataPath = Path.GetFullPath(Path.Combine(projectDir, _config.DataPath));
                        
                        // 如果项目目录下没有，尝试上一级目录（源码目录）
                        if (!Directory.Exists(tessDataPath))
                        {
                            var sourceDir = Path.GetFullPath(Path.Combine(projectDir, "..", "..", ".."));
                            tessDataPath = Path.GetFullPath(Path.Combine(sourceDir, _config.DataPath));
                        }
                    }
                    else
                    {
                        tessDataPath = Path.GetFullPath(_config.DataPath);
                    }
                }
                else
                {
                    // 默认路径
                    tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                }

                // 规范化路径
                tessDataPath = Path.GetFullPath(tessDataPath);

                // 记录所有尝试的路径用于调试
                var triedPaths = new List<string> { tessDataPath };

                // 如果路径不存在，尝试其他可能的位置
                if (!Directory.Exists(tessDataPath))
                {
                    // 尝试应用程序根目录下的 tessdata
                    var baseDirTessdata = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                    triedPaths.Add(baseDirTessdata);
                    
                    if (Directory.Exists(baseDirTessdata))
                    {
                        tessDataPath = baseDirTessdata;
                    }
                    else
                    {
                        // 尝试源码目录
                        var sourceDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                        var sourceTessdata = Path.Combine(sourceDir, "tessdata");
                        triedPaths.Add(sourceTessdata);
                        
                        if (Directory.Exists(sourceTessdata))
                        {
                            tessDataPath = sourceTessdata;
                        }
                    }
                }

                // 最终检查
                if (!Directory.Exists(tessDataPath))
                {
                    // 尝试创建在应用程序根目录
                    tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
                    Directory.CreateDirectory(tessDataPath);
                    
                    var triedList = string.Join("\n", triedPaths);
                    _initErrorMessage = $"Tesseract数据目录不存在，已在 {tessDataPath} 创建目录。\n\n尝试过的路径:\n{triedList}\n\n请从 https://github.com/tesseract-ocr/tessdata 下载语言数据文件（如 chi_sim.traineddata、eng.traineddata）并放入该目录";
                    return;
                }

                var language = string.IsNullOrWhiteSpace(_config.Language) ? "chi_sim" : _config.Language;
                var langFiles = language.Split('+')
                    .Select(lang => Path.Combine(tessDataPath, $"{lang}.traineddata"))
                    .ToList();
                
                var missingFiles = langFiles.Where(f => !File.Exists(f)).ToList();
                if (missingFiles.Any())
                {
                    var missingList = string.Join("\n", missingFiles);
                    _initErrorMessage = $"缺少语言数据文件:\n{missingList}\n\n当前目录: {tessDataPath}\n\n请从 https://github.com/tesseract-ocr/tessdata 下载所需的语言数据文件";
                    return;
                }

                _engine = new TesseractEngine(tessDataPath, language, EngineMode.Default);
                _engine.DefaultPageSegMode = PageSegMode.Auto;
            }
            catch (DllNotFoundException ex)
            {
                _initErrorMessage = $"无法加载Tesseract原生库: {ex.Message}\n请确保已安装Tesseract运行时或相关依赖";
            }
            catch (Exception ex)
            {
                _initErrorMessage = $"OCR引擎初始化失败: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}";
            }
        }

        public async Task<string> RecognizeTextAsync(Bitmap image)
        {
            return await RecognizeTextAsync(image, new Rectangle(0, 0, image.Width, image.Height));
        }

        public async Task<string> RecognizeTextAsync(Bitmap image, Rectangle region)
        {
            if (_engine == null)
                return string.Empty;

            return await Task.Run(() =>
            {
                try
                {
                    using var ms = new MemoryStream();
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Position = 0;

                    using var pix = Pix.LoadFromMemory(ms.ToArray());
                    using var page = _engine.Process(pix);
                    return page.GetText()?.Trim() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            });
        }

        public void Dispose()
        {
            _engine?.Dispose();
        }
    }
}