using System.Text.Json;
using Microsoft.Extensions.Logging;
using UnifiedLearningAssistant.Common;
using UnifiedLearningAssistant.Models.Pdf;
using UnifiedLearningAssistant.Services.AI;
using UnifiedLearningAssistant.Services.Pdf;
using UnifiedLearningAssistant.Services.TTS;
using UnifiedLearningAssistant.Services.Learning;
using UnifiedLearningAssistant.Views;

namespace UnifiedLearningAssistant.Presenters
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

        private string _currentPdfPath = "";
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
        // 新增功能：低优先级 - PDF搜索
        private string _currentSearchText = "";
        private List<int> _searchResultPages = new List<int>();
        private int _currentSearchIndex = -1;
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
            _view.AddWordToLearningList += View_AddWordToLearningList;
            _view.SpeakTranslation += View_SpeakTranslation;
            _view.SelectOcrClicked += View_SelectOcrClicked;
            _view.TranslateClicked += View_TranslateClicked;
            // 新增功能：低优先级 - 搜索事件
            _view.SearchTextChanged += View_SearchTextChanged;
            _view.SearchNext += View_SearchNext;
            _view.SearchPrevious += View_SearchPrevious;
            _view.ToggleSearchPanel += View_ToggleSearchPanel;
            // 新增功能：低优先级 - 夜间模式事件
            _view.ToggleNightMode += View_ToggleNightMode;
        }

        private void UnsubscribeFromEvents()
        {
            if (_view == null)
                return;

            _view.FileSelected -= View_FileSelected;
            _view.PageChanged -= View_PageChanged;
            _view.OcrSelectionComplete -= View_OcrSelectionComplete;
            _view.AiQuestionAsked -= View_AiQuestionAsked;
            _view.AddWordToLearningList -= View_AddWordToLearningList;
            _view.SpeakTranslation -= View_SpeakTranslation;
            _view.SelectOcrClicked -= View_SelectOcrClicked;
            _view.TranslateClicked -= View_TranslateClicked;
            // 新增功能：低优先级 - 搜索事件
            _view.SearchTextChanged -= View_SearchTextChanged;
            _view.SearchNext -= View_SearchNext;
            _view.SearchPrevious -= View_SearchPrevious;
            _view.ToggleSearchPanel -= View_ToggleSearchPanel;
            // 新增功能：低优先级 - 夜间模式事件
            _view.ToggleNightMode -= View_ToggleNightMode;
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
                        LoadPdfFile(filePath);
                        
                        // 恢复最后浏览的页数
                        if (_filePageMap.TryGetValue(filePath, out int savedPage))
                        {
                            _currentPageIndex = savedPage;
                            _view.SetCurrentPageIndex(savedPage);
                            _ = RenderAndDisplayCurrentPageAsync();
                        }
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

        public async void LoadPdf(string fileName)
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

        public async void RenderPage(int pageIndex)
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

        private async Task RenderAndDisplayCurrentPageAsync()
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
                    return (Bitmap)cached.Clone();
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
                        return (Bitmap)cachedAfterWait.Clone();
                    }
                }

                var bmp = await Task.Run(() => RenderPageToBitmap(pageIndex, renderW, renderH), 
                    _cts?.Token ?? CancellationToken.None).ConfigureAwait(false);
                if (bmp == null) return null;

                lock (_renderLock)
                {
                    if (!_renderCache.ContainsKey(cacheKey))
                    {
                        _renderCache[cacheKey] = (Bitmap)bmp.Clone();
                        UpdateCacheAccessOrder(cacheKey);
                        
                        // 如果超过缓存大小，移除最久未使用的
                        while (_renderCache.Count > RenderCacheSize && _cacheAccessOrder.Count > 0)
                        {
                            var oldestKey = _cacheAccessOrder.First.Value;
                            if (_renderCache.TryGetValue(oldestKey, out var oldBmp))
                            {
                                _renderCache.Remove(oldestKey);
                                try 
                                { 
                                    oldBmp.Dispose(); 
                                } 
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to dispose old cached bitmap");
                                }
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

        private void ClearRenderCache()
        {
            lock (_renderLock)
            {
                foreach (var kvp in _renderCache)
                {
                    try
                    {
                        kvp.Value.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to dispose cached bitmap during clear");
                    }
                }
                _renderCache.Clear();
                _cacheAccessOrder.Clear();
                _preRenderingPages.Clear();
            }
        }

        public async Task EnsurePageTextAsync(int pageIndex)
        {
            if (_pageTexts.ContainsKey(pageIndex)) return;
            try
            {
                var text = _pdfService.GetPdfText(pageIndex);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _pageTexts[pageIndex] = text;
                    _view.SetPageText(pageIndex, text);
                    return;
                }

                var bmp = _pdfService.RenderPage(pageIndex, 1200, 1600);
                var ocrText = await OcrBitmapAsync(bmp);
                if (!string.IsNullOrWhiteSpace(ocrText))
                {
                    _pageTexts[pageIndex] = ocrText;
                    _view.SetPageText(pageIndex, ocrText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to ensure page text for page {PageIndex}", pageIndex);
            }
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

            try
            {
                float scaleX = (float)img.Width / imgDisplayRect.Width;
                float scaleY = (float)img.Height / imgDisplayRect.Height;

                var actualRect = new Rectangle(
                    (int)(selRect.X * scaleX),
                    (int)(selRect.Y * scaleY),
                    (int)(selRect.Width * scaleX),
                    (int)(selRect.Height * scaleY));

                using var cropped = img.Clone(actualRect, img.PixelFormat);
                var recognizedText = await _ocrService.RecognizeTextAsync(cropped);

                if (!string.IsNullOrWhiteSpace(recognizedText))
                {
                    string translation = await _translationService.TranslateAsync(recognizedText) ?? "翻译失败";
                    _view.ShowTranslationDialog(recognizedText, translation, "");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR识别失败");
                _view.ShowError("OCR识别失败");
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
                
                var question = _view.GetPageText();
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
            }
        }

        private void View_AddWordToLearningList(object? sender, EventArgs e)
        {
            // 新增功能：PDF生词本联动 - 获取当前选中的单词并添加到学习列表
            var word = _view.GetPageText(); // 假设问题文本框中有要添加的单词
            if (!string.IsNullOrWhiteSpace(word))
            {
                // 初始化StudyEngine（如果需要）
                _studyEngine.Initialize(_currentUserId, _currentLanguage, _currentSubCategory, "", "", "");
                // 添加到未掌握列表
                _studyEngine.AddUnknownItem(word.Trim(), _currentSubCategory);
                _view.ShowMessage($"已将 \"{word.Trim()}\" 添加到生词本");
            }
            OnAddWordToLearningList?.Invoke(this, new AddWordEventArgs { Word = word, Language = _currentLanguage });
        }

        private void View_SpeakTranslation(object? sender, EventArgs e)
        {
        }

        private void View_SelectOcrClicked(object? sender, EventArgs e)
        {
            View_OcrSelectionComplete(sender, e);
        }

        private void View_TranslateClicked(object? sender, EventArgs e)
        {
        }

        // 新增功能：低优先级 - 搜索事件处理程序
        private void View_SearchTextChanged(object? sender, string searchText)
        {
            _currentSearchText = searchText;
            _ = PerformSearchAsync(searchText);
        }

        private void View_SearchNext(object? sender, EventArgs e)
        {
            if (_searchResultPages.Count > 0)
            {
                _currentSearchIndex = (_currentSearchIndex + 1) % _searchResultPages.Count;
                var nextPage = _searchResultPages[_currentSearchIndex];
                RenderPage(nextPage);
            }
        }

        private void View_SearchPrevious(object? sender, EventArgs e)
        {
            if (_searchResultPages.Count > 0)
            {
                _currentSearchIndex = _currentSearchIndex - 1;
                if (_currentSearchIndex < 0)
                    _currentSearchIndex = _searchResultPages.Count - 1;
                var prevPage = _searchResultPages[_currentSearchIndex];
                RenderPage(prevPage);
            }
        }

        private void View_ToggleSearchPanel(object? sender, EventArgs e)
        {
        }

        // 新增功能：低优先级 - 夜间模式切换
        private void View_ToggleNightMode(object? sender, EventArgs e)
        {
            _isNightMode = !_isNightMode;
            // 重新渲染当前页面以应用反色
            _ = RenderAndDisplayCurrentPageAsync();
        }

        private async Task PerformSearchAsync(string searchText)
        {
            _searchResultPages.Clear();
            _currentSearchIndex = -1;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _view.UpdateSearchResultCount(0);
                return;
            }

            try
            {
                var totalPages = _pdfService?.PageCount ?? 0;
                for (int i = 0; i < totalPages; i++)
                {
                    // 确保我们有当前页面的文本
                    await EnsurePageTextAsync(i);

                    if (_pageTexts.TryGetValue(i, out string? pageText) &&
                        !string.IsNullOrWhiteSpace(pageText) &&
                        pageText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _searchResultPages.Add(i);
                    }
                }

                _view.UpdateSearchResultCount(_searchResultPages.Count);

                // 如果找到结果，跳转到第一个匹配页
                if (_searchResultPages.Count > 0)
                {
                    _currentSearchIndex = 0;
                    RenderPage(_searchResultPages[0]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索失败");
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
                var totalPages = _pdfService?.PageCount ?? 0;
                if (totalPages == 0) return;

                for (int i = 0; i < totalPages; i++)
                {
                    if (_cts?.IsCancellationRequested ?? false) break;
                    
                    await Task.Run(async () =>
                    {
                        try
                        {
                            var thumbnail = await GetThumbnailAsync(i);
                            _view.AddThumbnail(i, thumbnail);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate thumbnail for page {PageIndex}", i);
                        }
                    });
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
