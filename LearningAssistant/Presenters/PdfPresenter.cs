using LearningAssistant.Common;
using LearningAssistant.Models.Pdf;
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
        private readonly IPdfAiService _pdfAiService;
        private readonly IPdfTtsService _pdfTtsService;
        private readonly IPdfStudyIntegration _pdfStudyIntegration;
        private readonly IPdfExportService _pdfExportService;
        private readonly IAnnotationService _annotationService;
        private readonly IHighlightService _highlightService;
        private readonly IPdfService _pdfService;

        private string _currentUserId = "Guest";
        private string _currentLanguage = Constants.Language.English;
        private string _currentSubCategory = Constants.SubCategory.EnglishWord;

        public event EventHandler<AddWordEventArgs>? OnAddWordToLearningList;

        public PdfPresenter(ILogger<PdfPresenter> logger,
            IPdfRenderer pdfRenderer,
            IPdfFileManager pdfFileManager,
            IPdfOcrService pdfOcrService,
            IPdfTranslationService pdfTranslationService,
            IPdfAiService pdfAiService,
            IPdfTtsService pdfTtsService,
            IPdfStudyIntegration pdfStudyIntegration,
            IPdfExportService pdfExportService,
            IAnnotationService annotationService,
            IHighlightService highlightService,
            IPdfService pdfService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pdfRenderer = pdfRenderer ?? throw new ArgumentNullException(nameof(pdfRenderer));
            _pdfFileManager = pdfFileManager ?? throw new ArgumentNullException(nameof(pdfFileManager));
            _pdfOcrService = pdfOcrService ?? throw new ArgumentNullException(nameof(pdfOcrService));
            _pdfTranslationService = pdfTranslationService ?? throw new ArgumentNullException(nameof(pdfTranslationService));
            _pdfAiService = pdfAiService ?? throw new ArgumentNullException(nameof(pdfAiService));
            _pdfTtsService = pdfTtsService ?? throw new ArgumentNullException(nameof(pdfTtsService));
            _pdfStudyIntegration = pdfStudyIntegration ?? throw new ArgumentNullException(nameof(pdfStudyIntegration));
            _pdfExportService = pdfExportService ?? throw new ArgumentNullException(nameof(pdfExportService));
            _annotationService = annotationService ?? throw new ArgumentNullException(nameof(annotationService));
            _highlightService = highlightService ?? throw new ArgumentNullException(nameof(highlightService));
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));

            SubscribeToServiceEvents();
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
            _view.AiQuestionAsked += View_AiQuestionAsked;
            _view.AddToLearningList += View_AddWordToLearningList;
            _view.SpeakOriginal += View_SpeakOriginal;
            _view.SpeakTranslation += View_SpeakTranslation;
            _view.SpeakText += View_SpeakText;
            _view.AskAiWithText += View_AskAiWithText;
            _view.SelectOcrClicked += View_SelectOcrClicked;
            _view.TranslateClicked += View_TranslateClicked;
            _view.ToggleNightMode += View_ToggleNightMode;
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
            _view.ToggleNightMode -= View_ToggleNightMode;
            _view.LanguageChanged -= View_LanguageChanged;
            _view.SpeakAnswer -= View_SpeakAnswer;
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

        public Bitmap? RenderPageToBitmap(int pageIndex, int width, int height)
        {
            return _pdfRenderer.RenderPageAsync(pageIndex, width, height).GetAwaiter().GetResult();
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

        public async Task RenderAndDisplayCurrentPageAsync()
        {
            try
            {
                var bitmap = await _pdfRenderer.RenderPageAsync(_pdfFileManager.CurrentPageIndex, 1000, 1400);
                if (bitmap != null)
                {
                    if (_pdfRenderer.IsNightMode)
                    {
                        _pdfRenderer.ApplyNightMode(bitmap);
                    }
                    _view?.DisplayImage(bitmap);
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
            return await _pdfTranslationService.TranslateAsync(text);
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
                var result = await _pdfTranslationService.OcrAndTranslateAsync(img);
                UpdateOcrResult(result.Original, result.Translation);
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
                var result = await _pdfTranslationService.OcrAndTranslateAsync(img, intRect);
                UpdateOcrResult(result.Original, result.Translation);
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
                _view?.SetTranslationText(translation ?? "翻译失败");
            }
            else
            {
                _view?.ShowWarning("未识别到文字，请尝试调整选择区域");
            }
        }

        public async Task SpeakTextAsync(string text, string language, float speed)
        {
            await _pdfTtsService.SpeakTextAsync(text, language, speed);
        }

        public async Task<string> GetAiAnswerAsync(string question, string context = "", CancellationToken cancellationToken = default)
        {
            return await _pdfAiService.GetAnswerAsync(question, context, cancellationToken);
        }

        public void TogglePenMode(bool enabled)
        {
        }

        public void ClearAnnotationsCurrentPage()
        {
            _annotationService.ClearAnnotation(_pdfFileManager.CurrentFilePath, _pdfFileManager.CurrentPageIndex);
        }

        public void AddAnnotationStroke(float[] normalizedPoints, int colorArgb, float thickness, int imageWidth, int imageHeight)
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
                    Thickness = thickness
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

        public void RememberCurrentPageForCurrentFile(int pageIndex)
        {
            _pdfFileManager.CurrentPageIndex = pageIndex;
        }

        public void NextPage()
        {
            var nextPage = _pdfFileManager.CurrentPageIndex + 1;
            if (nextPage < _pdfRenderer.PageCount)
            {
                _ = RenderPage(nextPage);
            }
        }

        public void PreviousPage()
        {
            var prevPage = _pdfFileManager.CurrentPageIndex - 1;
            if (prevPage >= 0)
            {
                _ = RenderPage(prevPage);
            }
        }

        public void SetQuestionInput(string text)
        {
            _view?.SetQuestionInput(text);
        }

        public (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession()
        {
            return _pdfFileManager.LoadSession();
        }

        public void LoadLastSessionAndRestore()
        {
            _pdfFileManager.LoadLastSessionAndRestore();
        }

        public async void ExportHighlightsToExcel()
        {
            if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath))
            {
                _logger.LogWarning("No PDF loaded, cannot export highlights");
                _view?.ShowMessage("请先打开一个PDF文件", "提示");
                return;
            }

            var saveFileName = $"高亮导出_{Path.GetFileNameWithoutExtension(_pdfFileManager.CurrentFilePath)}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var savePath = _view?.ShowSaveFileDialog(saveFileName, "Excel文件 (*.xlsx)|*.xlsx");
            if (string.IsNullOrEmpty(savePath))
                return;

            _view?.ShowLoading("正在导出高亮...");

            try
            {
                bool success = await _pdfExportService.ExportHighlightsToExcelAsync(
                    savePath,
                    _pdfFileManager.CurrentFilePath,
                    _pdfFileManager.IsImageMode,
                    _pdfFileManager.IsImageMode ? _pdfFileManager.ImageFiles : null,
                    _pdfFileManager.IsImageMode ? null : _pdfService
                );

                if (success)
                {
                    _logger?.LogInformation($"Successfully exported highlights to {savePath}");
                    _view?.ShowMessage($"成功导出高亮到\n{savePath}", "导出成功");

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

        public async Task<bool> ExportHighlightsToExcelAsync(string outputPath, List<Models.Pdf.PdfHighlight>? highlights = null)
        {
            if (string.IsNullOrEmpty(_pdfFileManager.CurrentFilePath))
            {
                _logger.LogWarning("No PDF loaded, cannot export highlights");
                return false;
            }

            if (highlights != null && highlights.Count > 0)
            {
                return await _pdfExportService.ExportHighlightsToExcelAsync(
                    outputPath,
                    highlights,
                    _pdfFileManager.CurrentFilePath,
                    _pdfFileManager.IsImageMode,
                    _pdfFileManager.IsImageMode ? _pdfFileManager.ImageFiles : null,
                    _pdfFileManager.IsImageMode ? null : _pdfService
                );
            }

            return await _pdfExportService.ExportHighlightsToExcelAsync(
                outputPath,
                _pdfFileManager.CurrentFilePath,
                _pdfFileManager.IsImageMode,
                _pdfFileManager.IsImageMode ? _pdfFileManager.ImageFiles : null,
                _pdfFileManager.IsImageMode ? null : _pdfService
            );
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

            _ = RenderAndDisplayCurrentPageAsync();
            _ = _pdfRenderer.GenerateThumbnailsAsync();
        }

        private void OnFolderLoaded(object? sender, FolderLoadedEventArgs e)
        {
            _view?.SetFileList(e.Files);
        }

        private void OnThumbnailGenerated(object? sender, ThumbnailGeneratedEventArgs e)
        {
            _view?.AddThumbnail(e.PageIndex, e.Thumbnail);
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

        private async void View_AiQuestionAsked(object? sender, EventArgs e)
        {
            try
            {
                var question = _view?.GetQuestionText();
                if (string.IsNullOrWhiteSpace(question))
                {
                    question = _view?.GetPageText();
                }

                if (string.IsNullOrWhiteSpace(question))
                {
                    _view?.ShowWarning("请输入要提问的内容");
                    return;
                }

                _view?.SetLoadingState(true);
                var answer = await GetAiAnswerAsync(question, "", CancellationToken.None);
                _view?.UpdateAiAnswer(answer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_AiQuestionAsked");
                _view?.ShowError("AI提问失败: " + ex.Message);
            }
            finally
            {
                _view?.SetLoadingState(false);
            }
        }

        private void View_AddWordToLearningList(object? sender, EventArgs e)
        {
            var word = _view?.GetOriginalText();
            if (string.IsNullOrWhiteSpace(word))
            {
                word = _view?.GetQuestionText();
            }

            if (!string.IsNullOrWhiteSpace(word))
            {
                bool success = _pdfStudyIntegration.AddWordToLearningList(word);
                if (success)
                {
                    string cleanWord = word.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ").Trim();
                    _view?.ShowMessage($"已将 \"{cleanWord}\" 添加到生词本");
                    _view?.RaiseAddToEditor(cleanWord, _currentLanguage);
                }
                else
                {
                    _view?.ShowError("添加到生词本失败");
                }
            }
            else
            {
                _view?.ShowWarning("请先输入要添加的单词");
            }
        }

        private void View_SpeakOriginal(object? sender, EventArgs e)
        {
            try
            {
                var text = _view?.GetOriginalText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _ = _pdfTtsService.SpeakTextAsync(text);
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
                var text = _view?.GetTranslationText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    _ = _pdfTtsService.SpeakTextAsync(text, "zh", 1.0f);
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
                    _ = _pdfTtsService.SpeakTextAsync(text);
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
                    _view?.SetQuestionInput(text);
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
                var answerText = _view?.GetAiAnswerText();
                if (!string.IsNullOrWhiteSpace(answerText))
                {
                    _ = _pdfTtsService.SpeakTextAsync(answerText);
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
                    _view?.ShowWarning("翻译失败，请检查网络连接");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in View_TranslateClicked");
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

        public void Dispose()
        {
            UnsubscribeFromEvents();
            UnsubscribeFromServiceEvents();
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