using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Threading;

namespace LearningAssistant.Services.Pdf
{
    public class PdfRenderer : IPdfRenderer
    {
        private readonly ILogger<PdfRenderer> _logger;
        private IPdfService? _pdfService;
        private string _currentFilePath = string.Empty;
        private List<string> _imageFiles = new List<string>();
        private bool _isImageMode = false;
        private bool _isNightMode = false;

        private readonly Dictionary<int, string> _pageTexts = new Dictionary<int, string>();
        private readonly object _renderLock = new object();
        private readonly LinkedList<string> _cacheAccessOrder = new LinkedList<string>();
        private readonly Dictionary<string, Bitmap> _renderCache = new Dictionary<string, Bitmap>();
        private const int RenderCacheSize = 30;
        private readonly Dictionary<int, Bitmap> _thumbnailCache = new Dictionary<int, Bitmap>();
        private bool _isGeneratingThumbnails = false;
        private CancellationTokenSource? _thumbnailCts;
        private readonly SemaphoreSlim _renderSemaphore = new SemaphoreSlim(2, 5);
        private readonly SemaphoreSlim _preRenderSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _preRenderCts;
        private readonly HashSet<int> _preRenderingPages = new HashSet<int>();
        private CancellationTokenSource? _cts;

        public event EventHandler<ThumbnailGeneratedEventArgs>? ThumbnailGenerated;

        public PdfRenderer(ILogger<PdfRenderer> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cts = new CancellationTokenSource();
        }

        public string CurrentFilePath => _currentFilePath;

        public bool IsNightMode => _isNightMode;

        public int PageCount => _isImageMode ? _imageFiles.Count : (_pdfService?.PageCount ?? 0);

        public void Initialize(IPdfService pdfService, string filePath)
        {
            _pdfService = pdfService ?? throw new ArgumentNullException(nameof(pdfService));
            _currentFilePath = filePath;
            _isImageMode = false;
            ClearCache();
        }

        public void InitializeImageMode(List<string> imageFiles)
        {
            _imageFiles = imageFiles ?? new List<string>();
            _isImageMode = true;
            _currentFilePath = _imageFiles.Any() ? _imageFiles[0] : string.Empty;
            ClearCache();
        }

        public void ClearCache()
        {
            var oldCts = Interlocked.Exchange(ref _thumbnailCts, new CancellationTokenSource());
            oldCts?.Cancel();
            oldCts?.Dispose();

            var oldPreRenderCts = Interlocked.Exchange(ref _preRenderCts, null);
            oldPreRenderCts?.Cancel();
            oldPreRenderCts?.Dispose();

            lock (_renderLock)
            {
                foreach (var bitmap in _thumbnailCache.Values)
                {
                    try { bitmap.Dispose(); } catch { }
                }
                _thumbnailCache.Clear();
                _renderCache.Clear();
                _cacheAccessOrder.Clear();
                _preRenderingPages.Clear();
            }
        }

        public void SetNightMode(bool enabled)
        {
            if (_isNightMode != enabled)
            {
                _isNightMode = enabled;
                ClearCache();
            }
        }

        public async Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height)
        {
            Bitmap? bitmap;
            
            if (_isImageMode)
            {
                bitmap = await LoadImageAsync(pageIndex);
            }
            else
            {
                if (_pdfService == null) return null;
                if (pageIndex < 0 || pageIndex >= _pdfService.PageCount) return null;

                var pageSize = _pdfService.GetPageSize(pageIndex);
                if (pageSize.Width <= 0 || pageSize.Height <= 0)
                {
                    _logger.LogWarning("Invalid page size for page {PageIndex}", pageIndex);
                    return null;
                }

                int renderWidth = width;
                int renderHeight = (int)(renderWidth * pageSize.Height / pageSize.Width);

                bitmap = await GetRenderedPageAsync(pageIndex, renderWidth, renderHeight);
            }

            return ApplyNightMode(bitmap);
        }

        private async Task<Bitmap?> LoadImageAsync(int index)
        {
            if (index < 0 || index >= _imageFiles.Count)
                return null;

            string imagePath = _imageFiles[index];

            return await Task.Run(() =>
            {
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    return new Bitmap(stream);
                }
            });
        }

        private string GetRenderCacheKey(int pageIndex, int renderWidth, int renderHeight)
        {
            return $"{_currentFilePath}_{pageIndex}_{renderWidth}_{renderHeight}_{_isNightMode}";
        }

        private void UpdateCacheAccessOrder(string cacheKey)
        {
            lock (_renderLock)
            {
                var node = _cacheAccessOrder.Find(cacheKey);
                if (node != null)
                {
                    _cacheAccessOrder.Remove(node);
                }
                _cacheAccessOrder.AddLast(cacheKey);
            }
        }

        private async Task<Bitmap?> GetRenderedPageAsync(int pageIndex, int renderW, int renderH)
        {
            if (_isImageMode)
            {
                if (pageIndex < 0 || pageIndex >= _imageFiles.Count) return null;
            }
            else
            {
                if (_pdfService == null) return null;
                if (pageIndex < 0 || pageIndex >= _pdfService.PageCount) return null;
            }

            return await GetRenderedPageInternalAsync(pageIndex, renderW, renderH);
        }

        private async Task<Bitmap?> GetRenderedImagePageAsync(int pageIndex, int renderW, int renderH)
        {
            return await GetRenderedPageAsync(pageIndex, renderW, renderH);
        }

        private async Task<Bitmap?> GetRenderedPageInternalAsync(int pageIndex, int renderW, int renderH)
        {
            var cacheKey = GetRenderCacheKey(pageIndex, renderW, renderH);
            lock (_renderLock)
            {
                if (_renderCache.TryGetValue(cacheKey, out var cached))
                {
                    UpdateCacheAccessOrder(cacheKey);
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
                        _renderCache[cacheKey] = CreateDeepCopy(bmp);
                        UpdateCacheAccessOrder(cacheKey);

                        while (_renderCache.Count > RenderCacheSize && _cacheAccessOrder.Count > 0)
                        {
                            var oldestKey = _cacheAccessOrder.First.Value;
                            if (_renderCache.TryGetValue(oldestKey, out var oldBmp))
                            {
                                _renderCache.Remove(oldestKey);
                                oldBmp?.Dispose();
                            }
                            _cacheAccessOrder.RemoveFirst();
                        }
                    }
                }

                _ = Task.Run(() => SmartPreRenderAsync(pageIndex, renderW, renderH),
                    _cts?.Token ?? CancellationToken.None);

                return bmp;
            }
            finally
            {
                _renderSemaphore.Release();
            }
        }

        private async Task SmartPreRenderAsync(int currentPage, int renderW, int renderH)
        {
            var cts = _cts;
            if (cts == null || cts.IsCancellationRequested) return;

            // 取消上一次预渲染
            var oldPreRenderCts = Interlocked.Exchange(ref _preRenderCts, new CancellationTokenSource());
            oldPreRenderCts?.Cancel();
            oldPreRenderCts?.Dispose();

            var preRenderCts = _preRenderCts;
            if (preRenderCts == null) return;

            var pagesToRender = new List<int>();
            if (currentPage + 1 < PageCount) pagesToRender.Add(currentPage + 1);
            if (currentPage - 1 >= 0) pagesToRender.Add(currentPage - 1);

            if (PageCount > 50)
            {
                if (currentPage + 2 < PageCount) pagesToRender.Add(currentPage + 2);
                if (currentPage - 2 >= 0) pagesToRender.Add(currentPage - 2);
            }

            var rnd = new Random();
            pagesToRender = pagesToRender.OrderBy(_ => rnd.Next()).ToList();

            foreach (var p in pagesToRender)
            {
                if (preRenderCts.IsCancellationRequested || cts.IsCancellationRequested) return;
                
                if (p < 0 || p >= PageCount) continue;

                // 检查缓存是否已有
                var cacheKey = GetRenderCacheKey(p, renderW, renderH);
                lock (_renderLock)
                {
                    if (_renderCache.ContainsKey(cacheKey)) continue;
                }

                try
                {
                    await _preRenderSemaphore.WaitAsync(preRenderCts.Token);
                    try
                    {
                        if (preRenderCts.IsCancellationRequested) return;

                        // 再次检查缓存
                        lock (_renderLock)
                        {
                            if (_renderCache.ContainsKey(cacheKey)) continue;
                        }

                        if (_isImageMode)
                        {
                            await GetRenderedImagePageAsync(p, renderW, renderH);
                        }
                        else
                        {
                            await GetRenderedPageAsync(p, renderW, renderH);
                        }
                    }
                    finally
                    {
                        _preRenderSemaphore.Release();
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to pre-render page {PageIndex}", p);
                }
            }
        }

        private Bitmap? CreateDeepCopy(Bitmap? source)
        {
            if (source == null)
                return null;

            var result = new Bitmap(source.Width, source.Height, source.PixelFormat);
            using (var g = Graphics.FromImage(result))
            {
                g.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height));
            }
            return result;
        }

        private Bitmap? RenderPageToBitmap(int pageIndex, int renderW, int renderH)
        {
            if (_isImageMode)
            {
                if (pageIndex < 0 || pageIndex >= _imageFiles.Count) return null;

                string imagePath = _imageFiles[pageIndex];
                using (var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
                {
                    using (var original = new Bitmap(stream))
                    {
                        float pageRatio = (float)original.Width / original.Height;
                        float boxRatio = (float)renderW / Math.Max(1, renderH);
                        int w = renderW, h = renderH;
                        if (pageRatio > boxRatio)
                        {
                            h = Math.Max(1, (int)Math.Round(renderW / pageRatio));
                        }
                        else
                        {
                            w = Math.Max(1, (int)Math.Round(renderH * pageRatio));
                        }

                        var result = new Bitmap(w, h);
                        using (var g = Graphics.FromImage(result))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                            g.DrawImage(original, 0, 0, w, h);
                        }
                        return result;
                    }
                }
            }

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

        public async Task<Bitmap?> GetThumbnailAsync(int pageIndex)
        {
            lock (_renderLock)
            {
                if (_thumbnailCache.TryGetValue(pageIndex, out var cached))
                {
                    return CreateDeepCopy(cached);
                }
            }

            var thumbnail = await Task.Run(() => RenderPageToBitmap(pageIndex, 100, 140));
            if (thumbnail != null)
            {
                lock (_renderLock)
                {
                    if (!_thumbnailCache.ContainsKey(pageIndex))
                    {
                        _thumbnailCache[pageIndex] = CreateDeepCopy(thumbnail);
                    }
                }
            }
            return thumbnail;
        }

        public async Task GenerateThumbnailsAsync()
        {
            await GenerateVisibleThumbnailsAsync(0);
        }

        public async Task GenerateVisibleThumbnailsAsync(int currentPage, int visibleCount = 5)
        {
            if (_isGeneratingThumbnails) return;
            _isGeneratingThumbnails = true;
            var currentCts = _thumbnailCts;

            try
            {
                if (_pdfService == null && !_isImageMode)
                {
                    _logger.LogWarning("GenerateVisibleThumbnailsAsync: _pdfService is null");
                    return;
                }

                int start = Math.Max(0, currentPage - visibleCount);
                int end = Math.Min(PageCount - 1, currentPage + visibleCount);

                for (int i = start; i <= end; i++)
                {
                    try
                    {
                        if (_cts?.IsCancellationRequested ?? false) break;
                        if (currentCts?.IsCancellationRequested ?? false) break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }

                    try
                    {
                        var thumbnail = await GetThumbnailAsync(i);
                        ThumbnailGenerated?.Invoke(this, new ThumbnailGeneratedEventArgs
                        {
                            PageIndex = i,
                            Thumbnail = thumbnail,
                            DirectoryPath = _isImageMode && i >= 0 && i < _imageFiles.Count
                                ? (Path.GetDirectoryName(_imageFiles[i]) ?? string.Empty)
                                : string.Empty
                        });
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

        public void Dispose()
        {
            ClearCache();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _renderSemaphore.Dispose();
            _preRenderSemaphore.Dispose();
        }

        public Bitmap ApplyNightMode(Bitmap bitmap)
        {
            if (!_isNightMode || bitmap == null)
                return bitmap;

            var inverted = new Bitmap(bitmap.Width, bitmap.Height);
            using (var graphics = Graphics.FromImage(inverted))
            {
                var colorMatrix = new System.Drawing.Imaging.ColorMatrix(
                    new float[][]
                    {
                        new float[] { -1, 0, 0, 0, 0 },
                        new float[] { 0, -1, 0, 0, 0 },
                        new float[] { 0, 0, -1, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 },
                        new float[] { 1, 1, 1, 0, 1 }
                    });

                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    attributes.SetColorMatrix(colorMatrix);
                    graphics.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                        0, 0, bitmap.Width, bitmap.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            return inverted;
        }
    }
}