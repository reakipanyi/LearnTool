using LearningAssistant.Common;
using LearningAssistant.Common.Events;
using LearningAssistant.Forms;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.AI;
using LearningAssistant.Services.Learning;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Views;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Presenters
{
    public class PdfPresenter : IDisposable
    {
        private IPdfView? _view;
        private readonly ILogger<PdfPresenter> _logger;
        private readonly IPdfRenderer _pdfRenderer;
        private readonly IPdfFileManager _pdfFileManager;
        private readonly IPdfOcrService _pdfOcrService;
        private readonly IPdfTranslationService _pdfTranslationService;
        private readonly IAIService? _aiService;
        private readonly IPdfTtsService? _pdfTtsService;
        private readonly IPdfStudyIntegration _pdfStudyIntegration;
        private readonly IExportService _exportService;
        private readonly IAnnotationService _annotationService;
        private readonly IHighlightService _highlightService;
        private readonly IPdfService _pdfService;
        private readonly IEventBus? _eventBus;

        private string _currentUserId = "Default";
        private string _currentLanguage = Constants.Language.English;
        private string _currentSubCategory = Constants.SubCategory.EnglishWord;

        public event EventHandler<AddWordEventArgs>? OnAddWordToLearningList;

        public PdfPresenter(ILogger<PdfPresenter> logger,
            IPdfRenderer pdfRenderer,
            IPdfFileManager pdfFileManager,
            IPdfOcrService pdfOcrService,
            IPdfTranslationService pdfTranslationService,
            IAIService? aiService,
            IPdfTtsService? pdfTtsService,
            IPdfStudyIntegration pdfStudyIntegration,
            IExportService exportService,
            IAnnotationService annotationService,
            IHighlightService highlightService,
            IPdfService pdfService,
            IEventBus? eventBus = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
            _pdfFileManager = pdfFileManager ?? throw new ArgumentNullException(nameof(pdfFileManager));
            _pdfOcrService = pdfOcrService ?? throw new ArgumentNullException(nameof(pdfOcrService));
            _pdfTranslationService = pdfTranslationService ?? throw new ArgumentNullException(nameof(pdfTranslationService));
            _aiService = aiService;
            _pdfTtsService = pdfTtsService;
            _pdfStudyIntegration = pdfStudyIntegration ?? throw new ArgumentNullException(nameof(pdfStudyIntegration));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _eventBus = eventBus;

            SubscribeToServiceEvents();
            SubscribeToEventBus();
            _logger.LogInformation("PdfPresenter initialized");
        }

        private void SubscribeToServiceEvents()
        {
            _pdfFileManager.FileLoaded += OnFileLoaded;
            _pdfFileManager.FolderLoaded += OnFolderLoaded;
            _pdfRenderer.ThumbnailGenerated += OnThumbnailGenerated;
            _pdfStudyIntegration.WordAdded += OnWordAdded;
        }

        private void UnsubscribeFromServiceEvents()
        {
            _pdfFileManager.FileLoaded -= OnFileLoaded;
            _pdfFileManager.FolderLoaded -= OnFolderLoaded;
            _pdfRenderer.ThumbnailGenerated -= OnThumbnailGenerated;
            _pdfStudyIntegration.WordAdded -= OnWordAdded;
        }

        private void SubscribeToEventBus()
        {
            if (_eventBus != null)
            {
                _eventBus.Subscribe<SendToPdfSearchEvent>(OnSendToPdfSearch);
                _eventBus.Subscribe<PDFHighlightEvent>(OnPdfHighlight);
            }
        }

        private void UnsubscribeFromEventBus()
        {
            if (_eventBus != null)
            {
                _eventBus.Unsubscribe<SendToPdfSearchEvent>(OnSendToPdfSearch);
                _eventBus.Unsubscribe<PDFHighlightEvent>(OnPdfHighlight);
            }
        }

        private void OnSendToPdfSearch(SendToPdfSearchEvent @event)
        {
            _logger.LogInformation("Received SendToPdfSearchEvent: {SearchText}", @event.SearchText);
            if (!string.IsNullOrEmpty(@event.SearchText))
            {
                SearchText(@event.SearchText);
            }
        }

        private void OnPdfHighlight(PDFHighlightEvent @event)
        {
            _logger.LogInformation("Received PDFHighlightEvent: {HighlightedText}", @event.HighlightedText);
        }

        public void SetCurrentUserAndConfig(string userId, string language, string subCategory)
        {
            _currentUserId = userId;
            _currentLanguage = language;
            _currentSubCategory = subCategory;
            _pdfStudyIntegration.SetCurrentUserAndConfig(userId, language, subCategory);
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

            _view.SpeakOriginal += View_SpeakOriginal;
            _view.SpeakTranslation += View_SpeakTranslation;
            _view.SpeakText += View_SpeakText;
            _view.SelectOcrClicked += View_SelectOcrClicked;
            _view.TranslateClicked += View_TranslateClicked;
            _view.ToggleNightMode += View_ToggleNightMode;
            _view.LanguageChanged += View_LanguageChanged;
            _view.AiQuestionAsked += View_AiQuestionAsked;
        }

        private void UnsubscribeFromEvents()
        {
            if (_view == null)
                return;

            _view.FileSelected -= View_FileSelected;
            _view.PageChanged -= View_PageChanged;
            _view.OcrSelectionComplete -= View_OcrSelectionComplete;

            _view.SpeakOriginal -= View_SpeakOriginal;
            _view.SpeakTranslation -= View_SpeakTranslation;
            _view.SpeakText -= View_SpeakText;
            _view.SelectOcrClicked -= View_SelectOcrClicked;
            _view.TranslateClicked -= View_TranslateClicked;
            _view.ToggleNightMode -= View_ToggleNightMode;
            _view.LanguageChanged -= View_LanguageChanged;
            _view.AiQuestionAsked -= View_AiQuestionAsked;
        }

        public int PageCount => _pdfRenderer.PageCount;

        public void LoadFolder(string folder)
        {
            _pdfFileManager.LoadFolder(folder);
        }

        public void LoadImageFolder(string folder)
        {
            _pdfFileManager.LoadFolder(folder);
        }

        public async Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height)
        {
            return await _pdfRenderer.RenderPageAsync(pageIndex, width, height);
        }

        public async Task LoadPdf(string fileName)
        {
            _view?.SetLoadingState(true);
            try
            {
                await _pdfFileManager.LoadFileAsync(fileName);
            }
            finally
            {
                _view?.SetLoadingState(false);
            }
        }

        public async Task RenderPage(int pageIndex)
        {
            int maxPage = _pdfRenderer.PageCount;
            if (pageIndex < 0 || pageIndex >= maxPage)
                return;

            _pdfFileManager.CurrentPageIndex = pageIndex;
            _view?.SetCurrentPageIndex(pageIndex);
            _view?.SetLoadingState(true);
            try
            {
                await RenderAndDisplayCurrentPageAsync();
            }
            finally
            {
                _view?.SetLoadingState(false);
            }
        }

        private async Task FireAndForgetWithLogging(Task task, string operationName)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in fire-and-forget operation: {OperationName}", operationName);
            }
        }

        public async Task RenderAndDisplayCurrentPageAsync()
        {
            try
            {
                var bitmap = await _pdfRenderer.RenderPageAsync(_pdfFileManager.CurrentPageIndex, 1000, 1400);
                if (bitmap != null)
                {
                    _view?.DisplayImage(bitmap);
                }

                if (_view is PdfReaderFormV2 v2View && v2View.IsDualPage)
                {
                    int nextPageIndex = _pdfFileManager.CurrentPageIndex + 1;
                    if (nextPageIndex < _pdfRenderer.PageCount)
                    {
                        var secondBitmap = await _pdfRenderer.RenderPageAsync(nextPageIndex, 1000, 1400);
                        _view?.SetSecondPageImage(secondBitmap);
                    }
                    else
                    {
                        _view?.SetSecondPageImage(null);
                    }
                }
                else
                {
                    _view?.SetSecondPageImage(null);
                }

                _view?.HighlightThumbnail(_pdfFileManager.CurrentPageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render page {PageIndex}", _pdfFileManager.CurrentPageIndex);
                _view?.ShowError("渲染页面失败");
            }
        }

        public bool IsOcrAvailable()
        {
            return _pdfOcrService.IsAvailable;
        }

        public async Task<string?> OcrBitmapAsync(Bitmap bmp)
        {
            return await _pdfOcrService.RecognizeTextAsync(bmp);
        }

        public async Task<string?> TranslateAsync(string text)
        {
            if (!_pdfTranslationService.IsAvailable)
            {
                _view?.ShowWarning("翻译服务不可用，请检查API配置");
                return null;
            }
            return await _pdfTranslationService.TranslateAsync(text);
        }

        public bool IsTranslationServiceAvailable()
        {
            return _pdfTranslationService != null && _pdfTranslationService.IsAvailable;
        }

        public bool IsTTSServiceAvailable()
        {
            return _pdfTtsService?.IsAvailable ?? false;
        }

        public async Task OcrCropAndTranslateAsync(Bitmap img, Rectangle selRect, Rectangle imgDisplayRect)
        {
            if (!_pdfOcrService.IsAvailable)
            {
                var errorMsg = _pdfOcrService.InitErrorMessage ?? "OCR服务未配置";
                _view?.ShowWarning($"OCR服务未初始化:\n{errorMsg}");
                return;
            }

            if (img == null || img.Width == 0 || img.Height == 0)
            {
                _view?.ShowWarning("图像无效");
                return;
            }

            if (selRect.Width <= 0 || selRect.Height <= 0)
            {
                if (_view?.AutoTranslateAfterOcr == true)
                {
                    var result = await _pdfTranslationService.OcrAndTranslateAsync(img);
                    UpdateOcrResult(result.Original, result.Translation);
                }
                else
                {
                    var original = await OcrBitmapAsync(img);
                    UpdateOcrResult(original, null);
                }
                return;
            }

            var imageDisplayRect = _view?.GetImageDisplayRect() ?? imgDisplayRect;
            if (imageDisplayRect.Width == 0 || imageDisplayRect.Height == 0)
            {
                _view?.ShowWarning("图像显示区域无效");
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
                _view?.ShowWarning("选择区域超出图像范围");
                return;
            }

            actualWidth = Math.Min(img.Width - actualX, actualWidth);
            actualHeight = Math.Min(img.Height - actualY, actualHeight);

            if (actualWidth <= 0 || actualHeight <= 0)
            {
                _view?.ShowWarning("选择区域无效，请重新选择");
                return;
            }

            if (actualWidth < 10 || actualHeight < 10)
            {
                _view?.ShowWarning("选择区域太小，请选择更大的区域");
                return;
            }

            var intRect = new Rectangle(
                (int)Math.Round(actualX),
                (int)Math.Round(actualY),
                (int)Math.Round(actualWidth),
                (int)Math.Round(actualHeight));

            try
            {
                if (_view?.AutoTranslateAfterOcr == true)
                {
                    var result = await _pdfTranslationService.OcrAndTranslateAsync(img, intRect);
                    UpdateOcrResult(result.Original, result.Translation);
                }
                else
                {
                    using var cropped = new Bitmap(intRect.Width, intRect.Height);
                    using (var g = Graphics.FromImage(cropped))
                    {
                        g.DrawImage(img, new Rectangle(0, 0, intRect.Width, intRect.Height), intRect, GraphicsUnit.Pixel);
                    }
                    var original = await OcrBitmapAsync(cropped);
                    UpdateOcrResult(original, null);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OCR识别失败");
                _view?.ShowError("OCR识别失败：" + ex.Message);
            }
        }

        private void UpdateOcrResult(string? original, string? translation)
        {
            if (!string.IsNullOrWhiteSpace(original))
            {
                _view?.SetOcrResultText(original);
                _view?.SetOriginalText(original);
                _view?.SetTranslationText(translation ?? "");

                bool autoSpeak = _view?.AutoSpeakAfterOcr ?? false;
                _logger?.LogInformation("UpdateOcrResult: AutoSpeakAfterOcr={AutoSpeak}, TtsService={HasTts}, TextLength={TextLen}", 
                    autoSpeak, _pdfTtsService != null, original.Length);

                if (autoSpeak && _pdfTtsService != null)
                {
                    _ = FireAndForgetWithLogging(TryAutoSpeakAsync(original), "AutoSpeakAsync");
                }
            }
            else
            {
                _view?.ShowWarning("未识别到文字，请尝试调整选择区域");
            }
        }

        public async Task SpeakTextAsync(string text, string language, float speed)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    _view?.ShowWarning("请先输入要朗读的文本");
                    return;
                }

                if (_pdfTtsService == null || !_pdfTtsService.IsAvailable)
                {
                    _view?.ShowWarning("朗读服务不可用，请检查TTS配置");
                    return;
                }

                await _pdfTtsService.SpeakTextAsync(text, language, speed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SpeakTextAsync");
                _view?.ShowError("朗读失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 自动朗读（用于高亮识别后），不弹窗，静默处理错误
        /// </summary>
        public async Task TryAutoSpeakAsync(string text)
        {
            try
            {
                if (_pdfTtsService == null || string.IsNullOrWhiteSpace(text))
                    return;
                _logger?.LogInformation("TryAutoSpeakAsync: start, text length={Len}", text.Length);
                await _pdfTtsService.SpeakTextAsync(text);
                _logger?.LogInformation("TryAutoSpeakAsync: completed");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "TryAutoSpeakAsync failed");
            }
        }

        public async Task<string> GetAiAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            if (_aiService == null)
            {
                return "AI服务不可用";
            }
            return await _aiService.AskQuestionAsync(question, context, cancellationToken);
        }

        public async Task<string> GenerateAiContentAsync(string prompt, CancellationToken cancellationToken = default)
        {
            if (_aiService == null)
            {
                return "AI服务不可用";
            }
            return await _aiService.AskQuestionAsync(prompt, "", cancellationToken);
        }

        public void TogglePenMode(bool enabled)
        {
            // TODO: 实现画笔模式切换
            _logger.LogDebug("TogglePenMode called with enabled={Enabled}", enabled);
        }

        public void ClearAnnotationsCurrentPage()
        {
            _annotationService.ClearAnnotation(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex);
        }

        public void UndoAnnotationStroke()
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return;
                _annotationService.RemoveLastStroke(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to undo annotation stroke");
            }
        }

        public void RemoveAnnotation(PdfAnnotationItem annotation)
        {
            try
            {
                if (string.IsNullOrEmpty(annotation.PdfPath)) return;

                if (annotation.Type == AnnotationType.Stroke)
                {
                    var strokes = _annotationService.GetStrokes(annotation.PdfPath, annotation.PageIndex).ToList();
                    for (int i = strokes.Count - 1; i >= 0; i--)
                    {
                        if (strokes[i].Id == annotation.Id)
                        {
                            _annotationService.RemoveStrokeAt(annotation.PdfPath, annotation.PageIndex, i);
                            break;
                        }
                    }
                }
                else if (annotation.Type == AnnotationType.Text)
                {
                    var texts = _annotationService.GetTexts(annotation.PdfPath, annotation.PageIndex).ToList();
                    for (int i = texts.Count - 1; i >= 0; i--)
                    {
                        if (texts[i].Id == annotation.Id)
                        {
                            _annotationService.RemoveTextAt(annotation.PdfPath, annotation.PageIndex, i);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove annotation");
            }
        }

        public void ClearAllAnnotations(string pdfPath)
        {
            try
            {
                for (int pageIndex = 0; pageIndex < PageCount; pageIndex++)
                {
                    _annotationService.ClearAllStrokes(pdfPath, pageIndex);
                    _annotationService.ClearAllTexts(pdfPath, pageIndex);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear all annotations");
            }
        }

        public void UpdateTextAnnotation(PdfAnnotationItem annotation, string newText, int colorArgb, float fontSize, string fontFamily)
        {
            try
            {
                if (string.IsNullOrEmpty(annotation.PdfPath)) return;

                var texts = _annotationService.GetTexts(annotation.PdfPath, annotation.PageIndex).ToList();
                for (int i = texts.Count - 1; i >= 0; i--)
                {
                    var text = texts[i];
                    if (text.Content == annotation.Text &&
                        text.ColorArgb == annotation.ColorArgb &&
                        Math.Abs(text.NormalizedX - annotation.NormalizedX) < 0.001 &&
                        Math.Abs(text.NormalizedY - annotation.NormalizedY) < 0.001)
                    {
                        var updatedText = new AnnotationText
                        {
                            Content = newText,
                            NormalizedX = annotation.NormalizedX,
                            NormalizedY = annotation.NormalizedY,
                            ColorArgb = colorArgb,
                            FontSize = fontSize,
                            FontFamily = fontFamily,
                            CreatedAt = text.CreatedAt
                        };
                        _annotationService.UpdateTextAt(annotation.PdfPath, annotation.PageIndex, i, updatedText);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update text annotation");
            }
        }

        public void AddAnnotationStroke(float[] normalizedPoints, int colorArgb, float thickness, int imageWidth, int imageHeight, string? shapeType = null)
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return;
                var pageSize = _pdfService.GetPageSize(_pdfFileManager.CurrentPageIndex);
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
                    Thickness = thickness,
                    ShapeType = shapeType
                };
                _annotationService.AddStroke(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex, stroke);
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
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return;
                _annotationService.SaveAnnotation(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex, overlay);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save annotation for current page");
            }
        }

        public void AddAnnotationText(float normalizedX, float normalizedY, string text, int colorArgb, float fontSize, string fontFamily, int imageWidth, int imageHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return;
                var pageSize = _pdfService.GetPageSize(_pdfFileManager.CurrentPageIndex);
                float pageW = pageSize.Width > 0 ? pageSize.Width : imageWidth;
                float pageH = pageSize.Height > 0 ? pageSize.Height : imageHeight;

                var imgX = normalizedX * imageWidth;
                var imgY = normalizedY * imageHeight;
                var pageX = imgX * (pageW / Math.Max(1, (float)imageWidth));
                var pageY = imgY * (pageH / Math.Max(1, (float)imageHeight));

                var annotationText = new AnnotationText
                {
                    NormalizedX = pageX / pageW,
                    NormalizedY = pageY / pageH,
                    Content = text,
                    ColorArgb = colorArgb,
                    FontSize = fontSize,
                    FontFamily = fontFamily
                };
                _annotationService.AddText(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex, annotationText);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to add annotation text");
            }
        }

        public Bitmap? LoadAnnotationForCurrentPage(int targetWidth, int targetHeight)
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return null;
                var pageSize = _pdfService.GetPageSize(_pdfFileManager.CurrentPageIndex);
                return _annotationService.LoadAnnotation(
                    _pdfFileManager.CurrentFilePath,
                    _pdfFileManager.CurrentPageIndex,
                    targetWidth,
                    targetHeight,
                    pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load annotation for current page");
                return null;
            }
        }

        public void RememberCurrentPageForCurrentFile(int pageIndex)
        {
            _pdfFileManager.CurrentPageIndex = pageIndex;
        }

        public IEnumerable<AnnotationStroke> GetCurrentPageStrokes()
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath))
                    return Enumerable.Empty<AnnotationStroke>();

                return _annotationService.GetStrokes(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get strokes for current page");
                return Enumerable.Empty<AnnotationStroke>();
            }
        }

        public void RemoveStrokeAtCurrentPage(int index)
        {
            try
            {
                if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath)) return;

                _annotationService.RemoveStrokeAt(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex, index);
                _logger.LogInformation("Removed stroke at index {Index} from page {PageIndex}", index, _pdfFileManager.CurrentPageIndex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove stroke at index {Index}", index);
            }
        }

        public (float Width, float Height) GetPageSize()
        {
            try
            {
                var pageSize = _pdfService.GetPageSize(_pdfFileManager.CurrentPageIndex);
                return (pageSize.Width, pageSize.Height);
            }
            catch
            {
                return (0, 0);
            }
        }

        public void NextPage()
        {
            int step = (_view is PdfReaderFormV2 v2View && v2View.IsDualPage) ? 2 : 1;
            var nextPage = _pdfFileManager.CurrentPageIndex + step;
            if (nextPage < _pdfRenderer.PageCount)
            {
                _ = FireAndForgetWithLogging(RenderPage(nextPage), "RenderPage");
            }
            else if (_pdfFileManager.CurrentPageIndex < _pdfRenderer.PageCount - 1)
            {
                _ = FireAndForgetWithLogging(RenderPage(_pdfRenderer.PageCount - 1), "RenderPage");
            }
        }

        public void PreviousPage()
        {
            int step = (_view is PdfReaderFormV2 v2View && v2View.IsDualPage) ? 2 : 1;
            var prevPage = _pdfFileManager.CurrentPageIndex - step;
            if (prevPage >= 0)
            {
                _ = FireAndForgetWithLogging(RenderPage(prevPage), "RenderPage");
            }
            else if (_pdfFileManager.CurrentPageIndex > 0)
            {
                _ = FireAndForgetWithLogging(RenderPage(0), "RenderPage");
            }
        }


        public (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession()
        {
            return _pdfFileManager.LoadSession();
        }

        public void LoadLastSessionAndRestore()
        {
            _pdfFileManager.LoadLastSessionAndRestore();
        }

        public void ExportHighlights()
        {
            _ = ExportHighlightsToExcelAsync();
        }

        public void PrintPdf()
        {
            if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath))
            {
                _view?.ShowMessage("请先打开一个PDF文件", "提示");
                return;
            }

            if (_pdfFileManager.IsImageMode)
            {
                _view?.ShowMessage("图片模式暂不支持打印", "提示");
                return;
            }

            try
            {
                _logger.LogInformation("Printing PDF: {FilePath}", _pdfFileManager.CurrentFilePath);
                bool success = _pdfService.Print();
                if (success)
                {
                    _logger.LogInformation("PDF print job sent successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to print PDF");
                _view?.ShowError("打印失败：" + ex.Message);
            }
        }

        public void SearchText(string text)
        {
            // TODO: 实现PDF文本搜索
            _logger.LogWarning("SearchText not yet implemented: {Text}", text);
            throw new NotImplementedException("PDF文本搜索功能尚未实现");
        }

        public void ZoomIn()
        {
            _view?.ZoomIn();
            _logger.LogDebug("ZoomIn called");
        }

        public void ZoomOut()
        {
            _view?.ZoomOut();
            _logger.LogDebug("ZoomOut called");
        }

        public async Task ExportHighlightsToExcelAsync()
        {
            if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath))
            {
                _logger.LogWarning("No PDF loaded, cannot export highlights");
                _view?.ShowMessage("请先打开一个PDF文件", "提示");
                return;
            }

            bool includeAnnotations = _view?.ShowConfirm("是否同时导出标注笔画？", "导出选项") ?? false;

            var saveFileName = $"高亮导出_{Path.GetFileNameWithoutExtension(_pdfFileManager.CurrentFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var savePath = _view?.ShowSaveFileDialog(saveFileName, "Excel文件 (*.xlsx)|*.xlsx");
            if (string.IsNullOrEmpty(savePath))
                return;

            _view?.ShowLoading("正在导出...");

            try
            {
                bool highlightSuccess = await _exportService.ExportHighlightsToExcelAsync(
                    savePath,
                    _pdfFileManager.CurrentFilePath,
                    _pdfFileManager.IsImageMode,
                    _pdfFileManager.IsImageMode ? _pdfFileManager.ImageFiles : null,
                    _pdfFileManager.IsImageMode ? null : _pdfService
                );

                if (includeAnnotations)
                {
                    var allStrokes = new List<AnnotationStroke>();
                    for (int pageIndex = 0; pageIndex < PageCount; pageIndex++)
                    {
                        var strokes = _annotationService.GetStrokes(_pdfFileManager.CurrentFilePath, pageIndex);
                        foreach (var stroke in strokes)
                        {
                            stroke.PageIndex = pageIndex;
                            allStrokes.Add(stroke);
                        }
                    }

                    if (allStrokes.Count > 0)
                    {
                        var annotationSavePath = savePath.Replace(".xlsx", "_标注.xlsx");
                        bool annotationSuccess = await _exportService.ExportAnnotationsToExcelAsync(
                            allStrokes,
                            _pdfFileManager.CurrentFilePath,
                            annotationSavePath,
                            _pdfFileManager.IsImageMode ? null : _pdfService,
                            PageCount
                        );
                        if (!annotationSuccess && !highlightSuccess)
                        {
                            _view?.ShowMessage("导出失败，请重试", "错误");
                        }
                    }
                }

                if (highlightSuccess)
                {
                    _logger?.LogInformation($"Successfully exported highlights to {savePath}");
                    string message = "成功导出高亮";
                    if (includeAnnotations)
                        message += "和标注";
                    _view?.ShowMessage($"{message}到\n{savePath}", "导出成功");

                    if (_view?.ShowConfirm("是否打开导出目录？", "导出成功") ?? false)
                    {
                        var exportFolderPath = Path.GetDirectoryName(savePath);
                        if (!string.IsNullOrEmpty(exportFolderPath))
                        {
                            System.Diagnostics.Process.Start("explorer.exe", exportFolderPath);
                        }
                    }
                }
                else
                {
                    _view?.ShowMessage("导出失败，请重试", "错误");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to export highlights");
                _view?.ShowMessage($"导出失败: {ex.Message}", "错误");
            }
            finally
            {
                _view?.HideLoading();
            }
        }

        private void OnFileLoaded(object? sender, FileLoadedEventArgs e)
        {
            _view?.SetCurrentPdfPath(e.FilePath);
            _view?.SetImageMode(e.IsImageMode);
            _view?.SetPageCount(e.PageCount);
            _view?.SetCurrentPageIndex(e.InitialPageIndex);

            if (e.IsImageMode)
            {
                _pdfRenderer.InitializeImageMode(_pdfFileManager.ImageFiles);
            }
            else
            {
                _pdfRenderer.Initialize(_pdfService, e.FilePath);
            }

            _ = FireAndForgetWithLogging(RenderAndDisplayCurrentPageAsync(), "RenderAndDisplayCurrentPageAsync");
            _ = FireAndForgetWithLogging(_pdfRenderer.GenerateVisibleThumbnailsAsync(e.InitialPageIndex), "GenerateVisibleThumbnailsAsync");
        }

        private void OnFolderLoaded(object? sender, FolderLoadedEventArgs e)
        {
            _view?.SetFileList(e.Files);
        }

        private void OnThumbnailGenerated(object? sender, ThumbnailGeneratedEventArgs e)
        {
            // 图片模式下传入 DirectoryPath 以便按目录分组展示，PDF 模式 DirectoryPath 为空
            _view?.AddThumbnail(e.PageIndex, e.Thumbnail, e.DirectoryPath);
        }

        private void OnWordAdded(object? sender, WordAddedEventArgs e)
        {
            OnAddWordToLearningList?.Invoke(this, new AddWordEventArgs
            {
                Word = e.Word,
                Language = e.Language
            });
        }

        private void View_FileSelected(object? sender, EventArgs e)
        {
            var selectedFile = _view?.GetSelectedFile();
            if (!string.IsNullOrWhiteSpace(selectedFile))
            {
                LoadPdf(selectedFile);
            }
        }

        private void View_PageChanged(object? sender, EventArgs e)
        {
            var pageText = _view?.GetPageText();
            if (int.TryParse(pageText, out int pageNum) && pageNum > 0)
            {
                RenderPage(pageNum - 1);
            }
        }

        private async void View_OcrSelectionComplete(object? sender, EventArgs e)
        {
            try
            {
                var img = _view?.GetCurrentImage() as Bitmap;
                var selection = _view?.GetSelectionRect();
                var displayRect = _view?.GetDisplayRect();
                if (img != null && selection.HasValue)
                {
                    await OcrCropAndTranslateAsync(img, selection.Value, displayRect ?? Rectangle.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_OcrSelectionComplete");
            }
        }


        private async void View_SpeakOriginal(object? sender, EventArgs e)
        {
            await SpeakOriginalAsync();
        }

        private async Task SpeakOriginalAsync()
        {
            try
            {
                var text = _view?.GetOriginalText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    _view?.ShowWarning("请先输入或选择要朗读的文本");
                    return;
                }

                if (_pdfTtsService == null || !_pdfTtsService.IsAvailable)
                {
                    _view?.ShowWarning("朗读服务不可用，请检查TTS配置");
                    return;
                }

                await _pdfTtsService.SpeakTextAsync(text);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SpeakOriginalAsync");
                _view?.ShowError("朗读失败: " + ex.Message);
            }
        }

        private async void View_SpeakTranslation(object? sender, EventArgs e)
        {
            await SpeakTranslationAsync();
        }

        private async Task SpeakTranslationAsync()
        {
            try
            {
                var text = _view?.GetTranslationText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    _view?.ShowWarning("请先进行翻译");
                    return;
                }

                if (_pdfTtsService == null || !_pdfTtsService.IsAvailable)
                {
                    _view?.ShowWarning("朗读服务不可用，请检查TTS配置");
                    return;
                }

                await _pdfTtsService.SpeakTextAsync(text, "zh", -1f);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SpeakTranslationAsync");
                _view?.ShowError("朗读失败: " + ex.Message);
            }
        }

        private async void View_SpeakText(object? sender, string text)
        {
            await SpeakTextAsync(text);
        }

        private async Task SpeakTextAsync(string text)
        {
            await SpeakTextAsync(text, _currentLanguage, 1.0f);
        }




        private void View_SelectOcrClicked(object? sender, EventArgs e)
        {
            _ = ProcessOcrSelectionAsync();
        }

        private async Task ProcessOcrSelectionAsync()
        {
            try
            {
                var img = _view?.GetCurrentImage() as Bitmap;
                var selection = _view?.GetSelectionRect();
                var displayRect = _view?.GetDisplayRect();
                if (img != null && selection.HasValue)
                {
                    await OcrCropAndTranslateAsync(img, selection.Value, displayRect ?? Rectangle.Empty);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ProcessOcrSelectionAsync");
            }
        }

        private async void View_TranslateClicked(object? sender, EventArgs e)
        {
            await TranslateTextAsync();
        }

        private async Task TranslateTextAsync()
        {
            try
            {
                var originalText = _view?.GetOriginalText();
                if (string.IsNullOrWhiteSpace(originalText))
                {
                    _view?.ShowWarning("请先输入要翻译的文本");
                    return;
                }

                _view?.SetLoadingState(true);
                var translation = await TranslateAsync(originalText);
                if (!string.IsNullOrWhiteSpace(translation))
                {
                    _view?.SetTranslationText(translation);
                }
                else
                {
                    if (!_pdfTranslationService.IsAvailable)
                    {
                        _view?.ShowWarning("翻译服务不可用，请检查API配置");
                    }
                    else
                    {
                        _view?.ShowWarning("翻译失败，请检查网络连接或稍后重试");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TranslateTextAsync");
                _view?.ShowError("翻译失败: " + ex.Message);
            }
            finally
            {
                _view?.SetLoadingState(false);
            }
        }

        private void View_ToggleNightMode(object? sender, EventArgs e)
        {
            _pdfRenderer.SetNightMode(!_pdfRenderer.IsNightMode);
            _ = RenderAndDisplayCurrentPageAsync();
        }

        private void View_LanguageChanged(object? sender, EventArgs e)
        {
            var language = _view?.GetCurrentLanguage();
            if (!string.IsNullOrWhiteSpace(language))
            {
                bool success = _pdfOcrService.SetLanguage(language);
                if (!success)
                {
                    _view?.ShowWarning($"无法切换到语言: {language}");
                }
            }
        }

        private void View_AiQuestionAsked(object? sender, EventArgs e)
        {
            _logger.LogInformation("AI question button clicked");
            _view?.RaiseAiQuestionAsked();
        }



        public void Dispose()
        {
            UnsubscribeFromEvents();
            UnsubscribeFromServiceEvents();
            UnsubscribeFromEventBus();
            _pdfRenderer.Dispose();
            _logger.LogInformation("PdfPresenter disposed");
        }
    }

    public class AddWordEventArgs : EventArgs
    {
        public string Word { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }
}