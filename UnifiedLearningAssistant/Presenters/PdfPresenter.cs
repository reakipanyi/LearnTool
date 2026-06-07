using Microsoft.Extensions.Logging;
using System.Text.Json;
using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.TTS;
using LearningAssistant.Views;

namespace LearningAssistant.Presenters
{
    public class PdfPresenter : IDisposable
    {
        private IPdfView? _view;
        private readonly ILogger<PdfPresenter> _logger;
        private readonly IPdfService _pdfService;
        private readonly IOcrService _ocrService;
        private readonly ITranslationService _translationService;
        private readonly IAnnotationService _annotationService;
        private readonly IAiQuestionService _aiQuestionService;
        private readonly ITTSService _ttsService;
        // 新增功能：PDF生词本联动 - 添加学习引擎
        private readonly IStudyEngine _studyEngine;
        private CancellationTokenSource? _cts;

        private string _currentPdfPath = string.Empty;
        private int _currentPageIndex = 0;
        private string _lastFolderPath = "";
        private bool _isDisposed = false;
        private readonly Dictionary<int, string> _pageTexts = new Dictionary<int, string>();
        private readonly object _renderLock = new object();
        // 新增功能：优化PDF渲染 - 使用LRU缓存策略
        private readonly LinkedList<string> _cacheAccessOrder = new LinkedList<string>();
        private readonly Dictionary<string, Bitmap> _renderCache = new Dictionary<string, Bitmap>();
        private const int RenderCacheSize = 15; // 增加缓存大小
        // 新增功能：中等级 - PDF缩略图缓存
        private readonly Dictionary<int, Bitmap> _thumbnailCache = new Dictionary<int, Bitmap>();
        private bool _isGeneratingThumbnails = false;
        // 新增功能：低优先级 - 夜间模式
        private bool _isNightMode = false;
        private readonly string _sessionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lastsession.json");
        private readonly SemaphoreSlim _renderSemaphore = new SemaphoreSlim(2, 5);
        private readonly HashSet<int> _preRenderingPages = new HashSet<int>();
        // 存储每个文件的最后浏览页数
        private readonly Dictionary<string, int> _filePageMap = new Dictionary<string, int>();
        // 新增功能：PDF生词本联动 - 当前用户ID和语言
        private string _currentUserId = "Guest";
        private string _currentLanguage = Constants.Language.English;
        private string _currentSubCategory = Constants.SubCategory.EnglishWord;

        // 会话数据记录
        private record SessionData(
            string? Folder,
            string? FilePath,
            Dictionary<string, int> FilePageMap
        );

        public PdfPresenter(ILogger<PdfPresenter> logger, IPdfService pdfService, IOcrService ocrService,
            ITranslationService translationService, IAnnotationService annotationService,
            IAiQuestionService aiQuestionService, ITTSService ttsService, IStudyEngine studyEngine)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
            _aiQuestionService = aiQuestionService ?? throw new ArgumentNullException(nameof(aiQuestionService));
            _ttsService = ttsService ?? throw new ArgumentNullException(nameof(ttsService));
            // 新增功能：PDF生词本联动 - 注入学习引擎
            _studyEngine = studyEngine ?? throw new ArgumentNullException(nameof(studyEngine));
            _cts = new CancellationTokenSource();
            _logger.LogInformation("PdfPresenter initialized");
        }

        // 新增功能：PDF生词本联动 - 设置当前用户和学习配置
        public void SetCurrentUserAndConfig(string userId, string language, string subCategory)
        {
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
        }

        public void SetView(IPdfView view)
        {
            if (_view == view)
                return;

            UnsubscribeFromEvents();
            _view = view;
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (_view == null)
                return;

            _view.FileSelected += View_FileSelected;
            _view.PageChanged += View_PageChanged;
            _view.OcrSelectionComplete += View_OcrSelectionComplete;
            _view.AiQuestionAsked += View_AiQuestionAsked;
            _view.AddToLearningList += View_AddWordToLearningList;
            _view.SpeakOriginal += View_SpeakOriginal;
            _view.SpeakTranslation += View_SpeakTranslation;
            _view.SpeakText += View_SpeakText;
            _view.AskAiWithText += View_AskAiWithText;
            _view.SelectOcrClicked += View_SelectOcrClicked;
            _view.TranslateClicked += View_TranslateClicked;
            // 新增功能：低优先级 - 夜间模式事件
            _view.ToggleNightMode += View_ToggleNightMode;
            // 新增功能：OCR语言切换事件
            _view.LanguageChanged += View_LanguageChanged;
            _view.SpeakAnswer += View_SpeakAnswer;
        }

        private void UnsubscribeFromEvents()
        {
            if (_view == null)
                return;

            _view.FileSelected -= View_FileSelected;
            _view.PageChanged -= View_PageChanged;
            _view.OcrSelectionComplete -= View_OcrSelectionComplete;
            _view.AiQuestionAsked -= View_AiQuestionAsked;
            _view.AddToLearningList -= View_AddWordToLearningList;
            _view.SpeakOriginal -= View_SpeakOriginal;
            _view.SpeakTranslation -= View_SpeakTranslation;
            _view.SpeakText -= View_SpeakText;
            _view.AskAiWithText -= View_AskAiWithText;
            _view.SelectOcrClicked -= View_SelectOcrClicked;
            _view.TranslateClicked -= View_TranslateClicked;
            // 新增功能：低优先级 - 夜间模式事件
            _view.ToggleNightMode -= View_ToggleNightMode;
            // 新增功能：OCR语言切换事件
            _view.LanguageChanged -= View_LanguageChanged;
            _view.SpeakAnswer -= View_SpeakAnswer;
        }

        private void SaveSession()
        {
            try
            {
                var data = new SessionData(_lastFolderPath, _currentPdfPath, new Dictionary<string, int>(_filePageMap));
                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(_sessionPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save session");
            }
        }

        public (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession()
        {
            try
            {
                if (!File.Exists(_sessionPath)) return (null, null, null);
                var json = File.ReadAllText(_sessionPath);
                var data = JsonSerializer.Deserialize<SessionData>(json);
                if (data == null) return (null, null, null);
                return (data.Folder, data.FilePath, data.FilePageMap);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load session");
                return (null, null, null);
            }
        }

        public void LoadLastSessionAndRestore()
        {
            try
            {
                var (folder, filePath, filePageMap) = LoadSession();
                if (filePageMap != null)
                {
                    foreach (var kvp in filePageMap)
                    {
                        _filePageMap[kvp.Key] = kvp.Value;
                    }
                }

                if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
                {
                    _lastFolderPath = folder;
                    LoadFolder(folder);

                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        var fileName = Path.GetFileName(filePath);
                        _currentPdfPath = filePath;
                        // 重要：通知 View 更新当前 PDF 路径
                        _view.SetCurrentPdfPath(filePath);

                        ClearRenderCache();
                        ClearThumbnailCache();

                        LoadPdfFile(filePath);

                        // 恢复最后浏览的页数
                        if (_filePageMap.TryGetValue(filePath, out int savedPage))
                        {
                            _currentPageIndex = savedPage;
                        }
                        else
                        {
                            _currentPageIndex = 0;
                        }

                        _view.SetCurrentPageIndex(_currentPageIndex);
                        _ = RenderAndDisplayCurrentPageAsync();

                        _ = GenerateThumbnailsAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore last session");
            }
        }

        private string GetRenderCacheKey(int pageIndex)
        {
            return $"{_currentPdfPath}_{pageIndex}";
        }

        public int PageCount => _pdfService?.PageCount ?? 0;

        public void LoadFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
                _view.ShowError("文件夹不存在");
                return;
            }

            _lastFolderPath = folder;
            var pdfFiles = Directory.EnumerateFiles(folder, "*.pdf")
                                   .Select(f => Path.GetFileName(f))
                                   .ToList();
            _view.SetFileList(pdfFiles);
            SaveSession();
        }

        private void LoadPdfFile(string filePath)
        {
            _pdfService.Load(filePath);
            _view.SetPageCount(_pdfService.PageCount);
        }

        public async Task LoadPdf(string fileName)
        {
            try
            {
                var filePath = Path.Combine(_lastFolderPath, fileName);
                if (!File.Exists(filePath))
                {
                    _view.ShowError("文件不存在");
                    return;
                }

                // 新增功能：中等级 - UI响应性改进，显示加载指示器
                _view.SetLoadingState(true);

                // 先保存当前文件的进度
                if (!string.IsNullOrEmpty(_currentPdfPath))
                {
                    _filePageMap[_currentPdfPath] = _currentPageIndex;
                }

                ClearRenderCache();
                ClearThumbnailCache();

                _currentPdfPath = filePath;
                // 重要：通知 View 更新当前 PDF 路径
                _view.SetCurrentPdfPath(filePath);
                LoadPdfFile(filePath);

                // 检查是否有保存的页数
                if (_filePageMap.TryGetValue(filePath, out int savedPage))
                {
                    _currentPageIndex = savedPage;
                }
                else
                {
                    _currentPageIndex = 0;
                }

                _view.SetCurrentPageIndex(_currentPageIndex);
                await RenderAndDisplayCurrentPageAsync();

                // 新增功能：中等级 - 异步生成PDF缩略图
                _ = GenerateThumbnailsAsync();

                SaveSession();
                _logger.LogInformation("Loaded PDF: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load PDF file: {Path}", fileName);
                _view.ShowError("无法加载PDF文件");
            }
            finally
            {
                // 新增功能：中等级 - UI响应性改进，隐藏加载指示器
                _view.SetLoadingState(false);
            }
        }

        public async Task RenderPage(int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= _pdfService.PageCount)
                return;

            _currentPageIndex = pageIndex;
            _view.SetCurrentPageIndex(pageIndex);
            _view.SetLoadingState(true);
            try
            {
                await RenderAndDisplayCurrentPageAsync();
            }
            finally
            {
                _view.SetLoadingState(false);
            }
        }

        // 公开一个可供外部调用的异步渲染入口
        public async Task RenderAndDisplayCurrentPageAsync()
        {
            try
            {
                var pageSize = _pdfService.GetPageSize(_currentPageIndex);
                if (pageSize.Width <= 0 || pageSize.Height <= 0)
                {
                    _logger.LogWarning("Invalid page size for page {PageIndex}", _currentPageIndex);
                    return;
                }

                int renderWidth = 1000;
                int renderHeight = (int)(renderWidth * pageSize.Height / pageSize.Width);

                var bitmap = await GetRenderedPageAsync(_currentPageIndex, renderWidth, renderHeight);
                if (bitmap != null)
                {
                    // 新增功能：低优先级 - 夜间模式反色
                    if (_isNightMode)
                    {
                        InvertColors(bitmap);
                    }
                    _view.DisplayImage(bitmap);
                }

                // 新增功能：中等级 - 高亮当前页面对应的缩略图
                _view.HighlightThumbnail(_currentPageIndex);

                // 保存当前页面进度
                _filePageMap[_currentPdfPath] = _currentPageIndex;
                SaveSession();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render page {PageIndex}", _currentPageIndex);
                _view.ShowError("渲染页面失败");
            }
        }

        // 新增功能：优化PDF渲染 - LRU缓存策略
        private void UpdateCacheAccessOrder(string cacheKey)
        {
            lock (_renderLock)
            {
                // 如果已存在，移到链表末尾（最近使用）
                var node = _cacheAccessOrder.Find(cacheKey);
                if (node != null)
                {
                    _cacheAccessOrder.Remove(node);
                }
                _cacheAccessOrder.AddLast(cacheKey);
            }
        }

        public async Task<Bitmap> GetRenderedPageAsync(int pageIndex, int renderW, int renderH)
        {
            if (_pdfService == null) return null;
            if (pageIndex < 0 || pageIndex >= _pdfService.PageCount) return null;

            var cacheKey = GetRenderCacheKey(pageIndex);
            lock (_renderLock)
            {
                if (_renderCache.TryGetValue(cacheKey, out var cached))
                {
                    // 更新访问顺序
                    UpdateCacheAccessOrder(cacheKey);
                    // 使用深拷贝创建完全独立的位图
                    return CreateDeepCopy(cached);
                }
            }

            await _renderSemaphore.WaitAsync();
            try
            {
                lock (_renderLock)
                {
                    if (_renderCache.TryGetValue(cacheKey, out var cachedAfterWait))
                    {
                        UpdateCacheAccessOrder(cacheKey);
                        // 使用深拷贝创建完全独立的位图
                        return CreateDeepCopy(cachedAfterWait);
                    }
                }

                var bmp = await Task.Run(() => RenderPageToBitmap(pageIndex, renderW, renderH),
                    _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                if (bmp == null) return null;

                lock (_renderLock)
                {
                    if (!_renderCache.ContainsKey(cacheKey))
                    {
                        // 使用深拷贝存储到缓存
                        _renderCache[cacheKey] = CreateDeepCopy(bmp);
                        UpdateCacheAccessOrder(cacheKey);

                        // 如果超过缓存大小，移除最久未使用的
                        while (_renderCache.Count > RenderCacheSize && _cacheAccessOrder.Count > 0)
                        {
                            var oldestKey = _cacheAccessOrder.First.Value;
                            if (_renderCache.TryGetValue(oldestKey, out var oldBmp))
                            {
                                _renderCache.Remove(oldestKey);
                                // 不要直接释放位图，避免影响可能正在使用的图像
                                // 让垃圾收集器处理
                            }
                            _cacheAccessOrder.RemoveFirst();
                        }
                    }
                }

                // 新增功能：优化PDF渲染 - 智能预渲染
                _ = Task.Run(() => SmartPreRenderAsync(pageIndex, renderW, renderH),
                    _cts?.Token ?? CancellationToken.None);

                return bmp;
            }
            finally
            {
                _renderSemaphore.Release();
            }
        }

        // 新增功能：优化PDF渲染 - 智能预渲染策略
        private async Task SmartPreRenderAsync(int currentPage, int renderW, int renderH)
        {
            if (_cts?.IsCancellationRequested ?? true) return;

            // 只预渲染最可能需要的页面（当前页前后各1-2页，根据总页数调整）
            var pagesToRender = new List<int>();

            if (currentPage - 1 >= 0) pagesToRender.Add(currentPage - 1);
            if (currentPage + 1 < PageCount) pagesToRender.Add(currentPage + 1);

            // 如果文档很长，再增加更多预渲染
            if (PageCount > 50)
            {
                if (currentPage - 2 >= 0) pagesToRender.Add(currentPage - 2);
                if (currentPage + 2 < PageCount) pagesToRender.Add(currentPage + 2);
            }

            // 打乱顺序以平衡
            var rnd = new Random();
            pagesToRender = pagesToRender.OrderBy(_ => rnd.Next()).ToList();

            foreach (var p in pagesToRender)
            {
                if (_cts?.IsCancellationRequested ?? true) return;
                if (p < 0 || p >= PageCount) continue;

                bool shouldRender = false;
                lock (_renderLock)
                {
                    var key = GetRenderCacheKey(p);
                    if (!_renderCache.ContainsKey(key) && !_preRenderingPages.Contains(p))
                    {
                        _preRenderingPages.Add(p);
                        shouldRender = true;
                    }
                }

                if (shouldRender)
                {
                    try
                    {
                        var bmp = RenderPageToBitmap(p, renderW, renderH);
                        if (bmp == null) continue;

                        lock (_renderLock)
                        {
                            var key = GetRenderCacheKey(p);
                            if (!_renderCache.ContainsKey(key))
                            {
                                _renderCache[key] = (Bitmap)bmp.Clone();
                                UpdateCacheAccessOrder(key);
                            }
                            _preRenderingPages.Remove(p);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to pre-render page {PageIndex}", p);
                        lock (_renderLock)
                        {
                            _preRenderingPages.Remove(p);
                        }
                    }
                }
            }
        }

        private Bitmap CreateDeepCopy(Bitmap source)
        {
            if (source == null)
                return null;

            // 使用MemoryStream创建完全独立的深拷贝
            using (var ms = new MemoryStream())
            {
                source.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);
                return new Bitmap(ms);
            }
        }

        private void ClearRenderCache()
        {
            lock (_renderLock)
            {
                // 不要直接释放位图，避免影响正在显示的图像
                // 让垃圾收集器处理这些位图
                _renderCache.Clear();
                _cacheAccessOrder.Clear();
                _preRenderingPages.Clear();
            }
        }

        private readonly object _pageTextsLock = new object();

        public async Task EnsurePageTextAsync(int pageIndex)
        {
            lock (_pageTextsLock)
            {
                if (_pageTexts.ContainsKey(pageIndex)) return;
            }
            try
            {
                var text = _pdfService.GetPdfText(pageIndex);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    lock (_pageTextsLock)
                    {
                        _pageTexts[pageIndex] = text;
                    }
                    _view?.SetPageText(pageIndex, text);
                    return;
                }

                var bmp = _pdfService.RenderPage(pageIndex, 1200, 1600);
                var ocrText = await OcrBitmapAsync(bmp);
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    lock (_pageTextsLock)
                    {
                        _pageTexts[pageIndex] = ocrText;
                    }
                    _view?.SetPageText(pageIndex, ocrText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure page text for page {PageIndex}", pageIndex);
            }
        }

        public bool IsOcrAvailable()
        {
            return _ocrService.IsAvailable;
        }

        public async Task<string?> OcrBitmapAsync(Bitmap bmp)
        {
            if (!_ocrService.IsAvailable)
                return null;

            return await _ocrService.RecognizeTextAsync(bmp);
        }

        public async Task<string?> TranslateAsync(string text)
        {
            if (!_translationService.IsAvailable)
                return null;

            return await _translationService.TranslateAsync(text);
        }

        public async Task OcrCropAndTranslateAsync(Bitmap img, Rectangle selRect, Rectangle imgDisplayRect)
        {
            if (!_ocrService.IsAvailable)
            {
                var errorMsg = _ocrService.InitErrorMessage ?? "OCR服务未配置";
                _view.ShowWarning($"OCR服务未初始化:\n{errorMsg}");
                return;
            }

            if (img == null || img.Width == 0 || img.Height == 0)
            {
                _view.ShowWarning("图像无效");
                return;
            }

            if (selRect.Width <= 0 || selRect.Height <= 0)
            {
                // 如果没有选择区域，识别整个图像
                await OcrFullImageInternalAsync(img);
                return;
            }

            try
            {
                var imageDisplayRect = _view.GetImageDisplayRect();

                if (imageDisplayRect.Width == 0 || imageDisplayRect.Height == 0)
                {
                    _view.ShowWarning("图像显示区域无效");
                    return;
                }

                float scaleX = (float)img.Width / imageDisplayRect.Width;
                float scaleY = (float)img.Height / imageDisplayRect.Height;

                float actualX = (selRect.X - imageDisplayRect.X) * scaleX;
                float actualY = (selRect.Y - imageDisplayRect.Y) * scaleY;
                float actualWidth = selRect.Width * scaleX;
                float actualHeight = selRect.Height * scaleY;

                actualX = Math.Max(0, actualX);
                actualY = Math.Max(0, actualY);

                if (actualX >= img.Width || actualY >= img.Height)
                {
                    _view.ShowWarning("选择区域超出图像范围");
                    return;
                }

                actualWidth = Math.Min(img.Width - actualX, actualWidth);
                actualHeight = Math.Min(img.Height - actualY, actualHeight);

                if (actualWidth <= 0 || actualHeight <= 0)
                {
                    _view.ShowWarning("选择区域无效，请重新选择");
                    return;
                }

                if (actualWidth < 10 || actualHeight < 10)
                {
                    _view.ShowWarning("选择区域太小，请选择更大的区域");
                    return;
                }

                var intRect = new Rectangle(
                    (int)Math.Round(actualX),
                    (int)Math.Round(actualY),
                    (int)Math.Round(actualWidth),
                    (int)Math.Round(actualHeight));

                if (intRect.Width <= 0 || intRect.Height <= 0 ||
                    intRect.X + intRect.Width > img.Width ||
                    intRect.Y + intRect.Height > img.Height)
                {
                    _view.ShowWarning("裁剪区域无效，请重新选择");
                    return;
                }

                using var cropped = img.Clone(intRect, img.PixelFormat);

                // 调试模式：不显示截图面板
                // _view.ShowOcrOverlay((Bitmap)cropped.Clone());

                var recognizedText = await _ocrService.RecognizeTextAsync(cropped);

                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    string translation = await _translationService.TranslateAsync(recognizedText) ?? "翻译失败";
                    _view.SetOcrResultText(recognizedText);
                    _view.SetOriginalText(recognizedText);
                    _view.SetTranslationText(translation);

                }
                else
                {
                    _view.ShowWarning("未识别到文字，请尝试调整选择区域");
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _logger.LogError(ex, "OCR识别失败：裁剪区域超出范围");
                _view.ShowError("裁剪区域超出范围，请重新选择");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR识别失败");
                _view.ShowError("OCR识别失败：" + ex.Message);
            }
        }

        private async Task OcrFullImageInternalAsync(Bitmap img)
        {
            try
            {
                _logger.LogInformation($"开始OCR识别整个图像，尺寸: {img.Width} x {img.Height}");

                var recognizedText = await _ocrService.RecognizeTextAsync(img);

                _logger.LogInformation($"OCR识别结果: {(recognizedText == null ? "null" : (recognizedText.Length == 0 ? "空字符串" : $"成功，{recognizedText.Length}个字符"))}");

                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    string translation = await _translationService.TranslateAsync(recognizedText) ?? "翻译失败";
                    _view.SetOcrResultText(recognizedText);
                    _view.SetOriginalText(recognizedText);
                    _view.SetTranslationText(translation);
                }
                else
                {
                    _view.ShowWarning("未识别到文字");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR识别失败");
                _view.ShowError("OCR识别失败：" + ex.Message);
            }
        }

        public async Task SpeakTextAsync(string text, string language, float speed)
        {
            if (!_ttsService.Available)
                return;

            string lang = language == Constants.Language.Chinese ? "zh" : "en";
            await _ttsService.SpeakAsync(text, lang, speed);
        }

        public async Task<string> GetAiAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await _aiQuestionService.AskAsync(question, context);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("GetAiAnswerAsync was cancelled");
                return "操作已取消";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get AI answer");
                return "获取答案失败";
            }
        }

        public void TogglePenMode(bool enabled)
        {
        }

        public void ClearAnnotationsCurrentPage()
        {
            _annotationService.ClearAnnotation(_currentPdfPath, _currentPageIndex);
        }

        public void AddAnnotationStroke(float[] normalizedPoints, int colorArgb, float thickness, int imageWidth, int imageHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentPdfPath)) return;
                var pageSize = _pdfService.GetPageSize(_currentPageIndex);
                float pageW = pageSize.Width > 0 ? pageSize.Width : imageWidth;
                float pageH = pageSize.Height > 0 ? pageSize.Height : imageHeight;
                var pts = new List<float>();
                for (int i = 0; i + 1 < normalizedPoints.Length; i += 2)
                {
                    var nx = normalizedPoints[i];
                    var ny = normalizedPoints[i + 1];
                    var imgX = nx * imageWidth;
                    var imgY = ny * imageHeight;
                    var pageX = imgX * (pageW / Math.Max(1, (float)imageWidth));
                    var pageY = imgY * (pageH / Math.Max(1, (float)imageHeight));
                    pts.Add(pageX / pageW);
                    pts.Add(pageY / pageH);
                }
                var stroke = new AnnotationStroke
                {
                    Points = pts.ToArray(),
                    ColorArgb = colorArgb,
                    Thickness = thickness
                };
                _annotationService.AddStroke(_currentPdfPath, _currentPageIndex, stroke);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add annotation stroke");
            }
        }

        public void SaveAnnotationForCurrentPage(Bitmap overlay)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentPdfPath)) return;
                _annotationService.SaveAnnotation(_currentPdfPath, _currentPageIndex, overlay);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save annotation for current page");
            }
        }

        public Bitmap RenderPageToBitmap(int pageIndex, int renderW, int renderH)
        {
            if (_pdfService == null) return null;
            if (pageIndex < 0 || pageIndex >= _pdfService.PageCount) return null;

            var pageSize = _pdfService.GetPageSize(pageIndex);
            if (pageSize.Width > 0 && pageSize.Height > 0)
            {
                var pageRatio = pageSize.Width / pageSize.Height;
                var boxRatio = (float)renderW / Math.Max(1, renderH);
                int w = renderW, h = renderH;
                if (pageRatio > boxRatio)
                {
                    h = Math.Max(1, (int)Math.Round(renderW / pageRatio));
                }
                else
                {
                    w = Math.Max(1, (int)Math.Round(renderH * pageRatio));
                }
                return _pdfService.RenderPage(pageIndex, w, h);
            }
            return _pdfService.RenderPage(pageIndex, renderW, renderH);
        }

        public void RememberCurrentPageForFile(string filePath, int pageIndex)
        {
        }

        public void RememberCurrentPageForCurrentFile(int pageIndex)
        {
            _currentPageIndex = pageIndex;
            _filePageMap[_currentPdfPath] = pageIndex;
            SaveSession();
        }

        public void NextPage()
        {
            if (_pdfService == null) return;

            var nextPage = _currentPageIndex + 1;
            if (nextPage < _pdfService.PageCount)
            {
                _ = RenderPage(nextPage);
            }
        }

        public void PreviousPage()
        {
            if (_pdfService == null) return;

            var prevPage = _currentPageIndex - 1;
            if (prevPage >= 0)
            {
                _ = RenderPage(prevPage);
            }
        }

        public void SetQuestionInput(string text)
        {
            _view.SetQuestionInput(text);
        }

        private void View_FileSelected(object? sender, EventArgs e)
        {
            var selectedFile = _view.GetSelectedFile();
            if (!string.IsNullOrWhiteSpace(selectedFile))
            {
                LoadPdf(selectedFile);
            }
        }

        private void View_PageChanged(object? sender, EventArgs e)
        {
            var pageText = _view.GetPageText();
            if (int.TryParse(pageText, out int pageNum) && pageNum > 0)
            {
                RenderPage(pageNum - 1);
            }
        }

        private async void View_OcrSelectionComplete(object? sender, EventArgs e)
        {
            try
            {
                _cts?.Token.ThrowIfCancellationRequested();

                var img = _view.GetCurrentImage() as Bitmap;
                var selection = _view.GetSelectionRect();
                var displayRect = _view.GetDisplayRect();
                if (img != null && selection.HasValue)
                {
                    await OcrCropAndTranslateAsync(img, selection.Value, displayRect);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("View_OcrSelectionComplete was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_OcrSelectionComplete");
            }
        }

        private async void View_AiQuestionAsked(object? sender, EventArgs e)
        {
            try
            {
                _cts?.Token.ThrowIfCancellationRequested();

                var question = _view.GetQuestionText();
                if (string.IsNullOrWhiteSpace(question))
                {
                    question = _view.GetPageText();
                }

                if (string.IsNullOrWhiteSpace(question))
                {
                    _view.ShowWarning("请输入要提问的内容");
                    return;
                }

                _view.SetLoadingState(true);
                var answer = await GetAiAnswerAsync(question, "", _cts?.Token ?? CancellationToken.None);
                _view.UpdateAiAnswer(answer);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("View_AiQuestionAsked was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_AiQuestionAsked");
                _view.ShowError("AI提问失败: " + ex.Message);
            }
            finally
            {
                _view.SetLoadingState(false);
            }
        }

        private void View_AddWordToLearningList(object? sender, EventArgs e)
        {
            if (_view == null || _studyEngine == null)
            {
                _logger.LogWarning("View_AddWordToLearningList called with null view or study engine");
                return;
            }

            var word = _view.GetOriginalText();
            if (string.IsNullOrWhiteSpace(word))
            {
                word = _view.GetQuestionText();
            }

            if (!string.IsNullOrWhiteSpace(word))
            {
                try
                {
                    string cleanWord = word.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                    _studyEngine.Initialize(_currentUserId, _currentLanguage, _currentSubCategory, "", "", "");
                    _studyEngine.AddUnknownItem(cleanWord, _currentSubCategory);
                    _view.ShowMessage($"已将 \"{cleanWord}\" 添加到生词本");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add word to learning list: {Word}", word);
                    _view.ShowError("添加到生词本失败: " + ex.Message);
                }
            }
            else
            {
                _view.ShowWarning("请先输入要添加的单词");
            }
            
            // 触发添加到编辑器的事件
            if (!string.IsNullOrWhiteSpace(word))
            {
                string cleanWord = word.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                _view.RaiseAddToEditor(cleanWord, _currentLanguage);
            }
            
            OnAddWordToLearningList?.Invoke(this, new AddWordEventArgs { Word = word ?? string.Empty, Language = _currentLanguage });
        }

        private void View_SpeakOriginal(object? sender, EventArgs e)
        {
            try
            {
                var text = _view.GetOriginalText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // 简单检测文本语言，决定朗读的语音设置
                    var isChinese = text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
                    var langCode = isChinese ? "zh" : "en";
                    _ = SpeakTextAsync(text, langCode, 1.0f);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_SpeakOriginal");
            }
        }

        private void View_SpeakTranslation(object? sender, EventArgs e)
        {
            try
            {
                var text = _view.GetTranslationText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _ = SpeakTextAsync(text, "zh", 1.0f);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_SpeakTranslation");
            }
        }

        private void View_SpeakText(object? sender, string text)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    // 简单检测文本语言，如果包含中文字符则用中文语音，否则用英文
                    bool isChinese = text.Any(c => c >= 0x4E00 && c <= 0x9FFF);
                    string lang = isChinese ? "zh" : "en";
                    _ = SpeakTextAsync(text, lang, 1.0f);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_SpeakText");
            }
        }

        private void View_AskAiWithText(object? sender, string text)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _view.SetQuestionInput(text);
                    View_AiQuestionAsked(sender, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_AskAiWithText");
            }
        }

        private void View_SpeakAnswer(object? sender, EventArgs e)
        {
            try
            {
                var answerText = _view.GetAiAnswerText();
                if (!string.IsNullOrWhiteSpace(answerText))
                {
                    // 简单检测文本语言，如果包含中文字符则用中文语音，否则用英文
                    bool isChinese = answerText.Any(c => c >= 0x4E00 && c <= 0x9FFF);
                    string lang = isChinese ? "zh" : "en";
                    _ = SpeakTextAsync(answerText, lang, 1.0f);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_SpeakAnswer");
            }
        }

        private void View_SelectOcrClicked(object? sender, EventArgs e)
        {
            View_OcrSelectionComplete(sender, e);
        }

        private async void View_TranslateClicked(object? sender, EventArgs e)
        {
            try
            {
                var originalText = _view.GetOriginalText();
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    _view.ShowWarning("请先输入要翻译的文本");
                    return;
                }

                _view.SetLoadingState(true);
                var translation = await TranslateAsync(originalText);
                if (!string.IsNullOrWhiteSpace(translation))
                {
                    _view.SetTranslationText(translation);
                }
                else
                {
                    _view.ShowWarning("翻译失败，请检查网络连接");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_TranslateClicked");
                _view.ShowError("翻译失败: " + ex.Message);
            }
            finally
            {
                _view.SetLoadingState(false);
            }
        }

        // 新增功能：低优先级 - 夜间模式切换
        private void View_ToggleNightMode(object? sender, EventArgs e)
        {
            _isNightMode = !_isNightMode;
            // 重新渲染当前页面以应用反色
            _ = RenderAndDisplayCurrentPageAsync();
        }

        private void View_LanguageChanged(object? sender, EventArgs e)
        {
            if (_view == null || _ocrService == null)
            {
                _logger.LogWarning("View_LanguageChanged called with null view or OCR service");
                return;
            }

            var language = _view.GetCurrentLanguage();
            if (!string.IsNullOrWhiteSpace(language))
            {
                try
                {
                    bool success = _ocrService.SetLanguage(language);
                    if (!success)
                    {
                        _view.ShowWarning($"无法切换到语言: {language}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to change OCR language to {Language}", language);
                    _view.ShowError("语言切换失败: " + ex.Message);
                }
            }
        }

        // 新增功能：低优先级 - 图像反色方法
        private void InvertColors(Bitmap bitmap)
        {
            try
            {
                // 使用LockBits提高性能
                var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
                var data = bitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);

                // 计算像素字节大小
                int bytesPerPixel = Image.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                // 创建缓冲区
                IntPtr ptr = data.Scan0;
                int bytes = Math.Abs(data.Stride) * bitmap.Height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(ptr, rgbValues, 0, bytes);

                // 反色处理
                for (int i = 0; i < rgbValues.Length; i += bytesPerPixel)
                {
                    // 反色：255 - 原值
                    if (bytesPerPixel >= 3)
                    {
                        rgbValues[i] = (byte)(255 - rgbValues[i]);     // Blue
                        rgbValues[i + 1] = (byte)(255 - rgbValues[i + 1]); // Green
                        rgbValues[i + 2] = (byte)(255 - rgbValues[i + 2]); // Red
                    }
                    // 如果有Alpha通道，不改变它
                }

                // 复制回位图
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, bytes);
                bitmap.UnlockBits(data);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "反色渲染失败");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                // 最后保存一次会话
                SaveSession();

                UnsubscribeFromEvents();
                _cts?.Cancel();

                ClearRenderCache();
                ClearThumbnailCache();

                _cts?.Dispose();
                _renderSemaphore.Dispose();
                _pdfService.Dispose();
                _logger.LogInformation("PdfPresenter disposed");
                _isDisposed = true;
            }
        }

        // 新增功能：中等级 - PDF缩略图相关方法
        private void ClearThumbnailCache()
        {
            lock (_renderLock)
            {
                foreach (var bitmap in _thumbnailCache.Values)
                {
                    try
                    {
                        bitmap.Dispose();
                    }
                    catch
                    {
                    }
                }
                _thumbnailCache.Clear();
            }
            _view.ClearThumbnails();
        }

        private async Task GenerateThumbnailsAsync()
        {
            if (_isGeneratingThumbnails) return;
            _isGeneratingThumbnails = true;

            try
            {
                if (_pdfService == null)
                {
                    _logger.LogWarning("GenerateThumbnailsAsync: _pdfService is null");
                    return;
                }

                var totalPages = _pdfService.PageCount;
                _logger.LogInformation($"GenerateThumbnailsAsync: Total pages = {totalPages}");

                if (totalPages == 0)
                {
                    _logger.LogWarning("GenerateThumbnailsAsync: No pages to generate thumbnails for");
                    return;
                }

                for (int i = 0; i < totalPages; i++)
                {
                    if (_cts?.IsCancellationRequested ?? false) break;

                    try
                    {
                        var thumbnail = await GetThumbnailAsync(i);
                        _logger.LogInformation($"Generated thumbnail for page {i}: {(thumbnail != null ? $"success ({thumbnail.Width}x{thumbnail.Height})" : "failed")}");
                        _view.AddThumbnail(i, thumbnail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to generate thumbnail for page {PageIndex}", i);
                    }
                }
            }
            finally
            {
                _isGeneratingThumbnails = false;
            }
        }

        private async Task<Bitmap> GetThumbnailAsync(int pageIndex)
        {
            lock (_renderLock)
            {
                if (_thumbnailCache.TryGetValue(pageIndex, out var cached))
                {
                    return (Bitmap)cached.Clone();
                }
            }

            var thumbnail = await Task.Run(() => RenderPageToBitmap(pageIndex, 100, 140));
            if (thumbnail != null)
            {
                lock (_renderLock)
                {
                    if (!_thumbnailCache.ContainsKey(pageIndex))
                    {
                        _thumbnailCache[pageIndex] = (Bitmap)thumbnail.Clone();
                    }
                }
            }
            return thumbnail;
        }

        public event EventHandler<AddWordEventArgs>? OnAddWordToLearningList;
    }

    public class AddWordEventArgs : EventArgs
    {
        public string Word { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}
