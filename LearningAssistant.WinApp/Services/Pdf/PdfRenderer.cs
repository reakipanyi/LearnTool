using LearningAssistant.Abstractions;
using LearningAssistant.Models.Pdf;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
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

        // 预渲染过程中再次触发的预渲染请求会被忽略，避免"渲染→预渲染→再渲染"无限链式调度
        private static readonly AsyncLocal<bool> _inPreRender = new();
        private readonly object _renderLock = new object();

        // 渲染缓存按字节预算做 LRU 淘汰，避免高倍缩放时缓存多张大图导致内存峰值失控
        private sealed class RenderCacheEntry
        {
            public string Key = string.Empty;
            public Bitmap Bitmap = null!;
            public long Bytes;
        }

        private readonly LinkedList<RenderCacheEntry> _cacheAccessOrder = new();
        private readonly Dictionary<string, LinkedListNode<RenderCacheEntry>> _renderCache = new();
        private const long MaxRenderCacheBytes = 256L * 1024 * 1024;
        private long _renderCacheBytes;

        private const int MaxThumbnailCacheCount = 300;
        private readonly Dictionary<int, Bitmap> _thumbnailCache = new Dictionary<int, Bitmap>();
        private readonly Queue<int> _thumbnailCacheOrder = new Queue<int>();
        private bool _isGeneratingThumbnails = false;
        private CancellationTokenSource? _thumbnailCts;
        private readonly SemaphoreSlim _renderSemaphore = new SemaphoreSlim(2, 5);
        private readonly SemaphoreSlim _preRenderSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? _preRenderCts;
        // 记录后台预渲染任务，Dispose 时先取消并等待退出，再释放信号量
        private readonly ConcurrentDictionary<long, Task> _backgroundTasks = new();
        private long _backgroundTaskSeq;
        private volatile bool _disposed;
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
                _thumbnailCacheOrder.Clear();

                // 渲染缓存中保存的是内部深拷贝，调用方拿到的均为副本，可安全释放
                foreach (var entry in _cacheAccessOrder)
                {
                    try { entry.Bitmap.Dispose(); } catch { }
                }
                _cacheAccessOrder.Clear();
                _renderCache.Clear();
                _renderCacheBytes = 0;
            }
        }

        public void SetNightMode(bool enabled)
        {
            // 夜间反色已移至显示层，缓存不再区分昼夜，切换时无需清空缓存
            _isNightMode = enabled;
        }

        public async Task<Bitmap?> RenderPageAsync(int pageIndex, int width, int height, CancellationToken cancellationToken = default)
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

                // 尺寸量化 + 硬上限，防止高倍缩放生成数百 MB 的位图
                (renderWidth, renderHeight) = PdfRenderSizePolicy.Apply(renderWidth, renderHeight);

                bitmap = await GetRenderedPageAsync(pageIndex, renderWidth, renderHeight, cancellationToken);
            }

            if (bitmap == null) return null;

            // 渲染期间已被取消：结果直接丢弃
            if (cancellationToken.IsCancellationRequested)
            {
                bitmap.Dispose();
                return null;
            }

            // 夜间反色由视图在绘制层完成，这里始终返回原色位图，保证导出/打印/OCR 使用原色
            return bitmap;
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
                    using (var source = Image.FromStream(stream))
                    {
                        // 复制为独立位图，避免位图依赖已关闭的文件流导致偶发 GDI+ 错误
                        return new Bitmap(source);
                    }
                }
            });
        }

        private string GetRenderCacheKey(int pageIndex, int renderWidth, int renderHeight)
        {
            // 统一在 key 处量化，保证渲染与预渲染即使传入原始尺寸也命中同一缓存项；
            // key 不含夜间模式，昼夜切换共享同一份原色缓存
            (renderWidth, renderHeight) = PdfRenderSizePolicy.Apply(renderWidth, renderHeight);
            return $"{_currentFilePath}_{pageIndex}_{renderWidth}_{renderHeight}";
        }

        /// <summary>
        /// 命中缓存时返回独立副本，并把该条目移到 LRU 末端。调用方负责释放返回值。
        /// </summary>
        private Bitmap? TryGetCachedCopy(string cacheKey)
        {
            lock (_renderLock)
            {
                if (_renderCache.TryGetValue(cacheKey, out var node))
                {
                    TouchCacheEntry(node);
                    return CreateDeepCopy(node.Value.Bitmap);
                }
            }
            return null;
        }

        private void TouchCacheEntry(LinkedListNode<RenderCacheEntry> node)
        {
            if (node.List == null) return;
            _cacheAccessOrder.Remove(node);
            _cacheAccessOrder.AddLast(node);
        }

        /// <summary>
        /// 写入缓存（内部持有深拷贝），超出字节预算时从最旧条目开始淘汰并释放。
        /// </summary>
        private void AddCacheEntry(string cacheKey, Bitmap bitmap)
        {
            lock (_renderLock)
            {
                if (_renderCache.ContainsKey(cacheKey)) return;

                var copy = CreateDeepCopy(bitmap);
                if (copy == null) return;

                long bytes = EstimateBitmapBytes(copy);
                if (bytes > MaxRenderCacheBytes)
                {
                    // 单张就超过预算的极端尺寸不进入缓存，避免把缓存整体挤空
                    copy.Dispose();
                    return;
                }

                var entry = new RenderCacheEntry { Key = cacheKey, Bitmap = copy, Bytes = bytes };
                var node = _cacheAccessOrder.AddLast(entry);
                _renderCache[cacheKey] = node;
                _renderCacheBytes += bytes;

                TrimRenderCache();
            }
        }

        private void TrimRenderCache()
        {
            while (_renderCacheBytes > MaxRenderCacheBytes && _cacheAccessOrder.First != null)
            {
                var oldest = _cacheAccessOrder.First;
                _cacheAccessOrder.RemoveFirst();
                _renderCache.Remove(oldest.Value.Key);
                _renderCacheBytes -= oldest.Value.Bytes;
                try { oldest.Value.Bitmap.Dispose(); } catch { }
            }
        }

        private static long EstimateBitmapBytes(Bitmap bitmap)
        {
            // 统一按 4 字节/像素估算，覆盖 32bpp ARGB 的真实峰值
            return (long)bitmap.Width * bitmap.Height * 4;
        }

        private async Task<Bitmap?> GetRenderedPageAsync(int pageIndex, int renderW, int renderH,
            CancellationToken cancellationToken = default)
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

            return await GetRenderedPageInternalAsync(pageIndex, renderW, renderH, cancellationToken);
        }

        private async Task<Bitmap?> GetRenderedImagePageAsync(int pageIndex, int renderW, int renderH,
            CancellationToken cancellationToken = default)
        {
            return await GetRenderedPageAsync(pageIndex, renderW, renderH, cancellationToken);
        }

        private async Task<Bitmap?> GetRenderedPageInternalAsync(int pageIndex, int renderW, int renderH,
            CancellationToken cancellationToken)
        {
            // 再次收敛尺寸（预渲染等入口可能绕过 RenderPageAsync）
            (renderW, renderH) = PdfRenderSizePolicy.Apply(renderW, renderH);

            var cacheKey = GetRenderCacheKey(pageIndex, renderW, renderH);
            var cachedCopy = TryGetCachedCopy(cacheKey);
            if (cachedCopy != null)
            {
                return cachedCopy;
            }

            try
            {
                // 排队阶段即可取消：快速翻页时旧请求不再占用渲染槽位
                await _renderSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (ObjectDisposedException)
            {
                // 渲染器已释放，直接放弃
                return null;
            }

            try
            {
                if (cancellationToken.IsCancellationRequested) return null;

                var cachedAfterWait = TryGetCachedCopy(cacheKey);
                if (cachedAfterWait != null)
                {
                    return cachedAfterWait;
                }

                var bmp = await Task.Run(() => RenderPageToBitmap(pageIndex, renderW, renderH),
                    cancellationToken).ConfigureAwait(false);
                if (bmp == null) return null;

                // 渲染期间已被取消：结果直接丢弃，不写入缓存，也不再触发预渲染
                if (cancellationToken.IsCancellationRequested)
                {
                    bmp.Dispose();
                    return null;
                }

                AddCacheEntry(cacheKey, bmp);

                if (!_inPreRender.Value)
                {
                    SchedulePreRender(pageIndex, renderW, renderH);
                }

                return bmp;
            }
            finally
            {
                try { _renderSemaphore.Release(); } catch (ObjectDisposedException) { }
            }
        }

        /// <summary>
        /// 登记后台预渲染任务，Dispose 时据此等待任务退出后再释放信号量。
        /// </summary>
        private void SchedulePreRender(int pageIndex, int renderW, int renderH)
        {
            if (_disposed) return;

            var token = _cts?.Token ?? CancellationToken.None;
            long taskId = Interlocked.Increment(ref _backgroundTaskSeq);
            var task = Task.Run(() => SmartPreRenderAsync(pageIndex, renderW, renderH), token);
            _backgroundTasks[taskId] = task;
            _ = task.ContinueWith(_ => _backgroundTasks.TryRemove(taskId, out _),
                CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        private async Task SmartPreRenderAsync(int currentPage, int renderW, int renderH)
        {
            if (_disposed) return;

            // 标记当前预渲染上下文，其内部渲染不再触发新的预渲染
            _inPreRender.Value = true;

            var cts = _cts;
            if (cts == null || cts.IsCancellationRequested) return;

            // 取消上一次预渲染
            var oldPreRenderCts = Interlocked.Exchange(ref _preRenderCts, new CancellationTokenSource());
            oldPreRenderCts?.Cancel();
            oldPreRenderCts?.Dispose();

            var preRenderCts = _preRenderCts;
            if (preRenderCts == null) return;

            // 固定顺序：下一页 → 下下页 → 上一页 → 上上页，保证用户继续翻页时优先命中缓存
            var pagesToRender = new List<int>();
            if (currentPage + 1 < PageCount) pagesToRender.Add(currentPage + 1);
            if (PageCount > 50 && currentPage + 2 < PageCount) pagesToRender.Add(currentPage + 2);
            if (currentPage - 1 >= 0) pagesToRender.Add(currentPage - 1);
            if (PageCount > 50 && currentPage - 2 >= 0) pagesToRender.Add(currentPage - 2);

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

                        // 预渲染只用于填充缓存，返回的位图副本需立即释放
                        var preRendered = _isImageMode
                            ? await GetRenderedImagePageAsync(p, renderW, renderH)
                            : await GetRenderedPageAsync(p, renderW, renderH);
                        preRendered?.Dispose();
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
                catch (ObjectDisposedException)
                {
                    // 渲染器已释放，预渲染直接退出
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
                return _pdfService.RenderPageBitmap(pageIndex, w, h);
            }
            return _pdfService.RenderPageBitmap(pageIndex, renderW, renderH);
        }

        public async Task<byte[]?> GetThumbnailAsync(int pageIndex)
        {
            lock (_renderLock)
            {
                if (_thumbnailCache.TryGetValue(pageIndex, out var cached))
                {
                    using var copy = CreateDeepCopy(cached);
                    return BitmapToBytes(copy);
                }
            }

            var thumbnail = await Task.Run(() => RenderPageToBitmap(pageIndex, 100, 140));
            if (thumbnail == null) return null;

            try
            {
                lock (_renderLock)
                {
                    if (!_thumbnailCache.ContainsKey(pageIndex))
                    {
                        _thumbnailCache[pageIndex] = CreateDeepCopy(thumbnail);
                        _thumbnailCacheOrder.Enqueue(pageIndex);
                        TrimThumbnailCache();
                    }
                }
                return BitmapToBytes(thumbnail);
            }
            finally
            {
                thumbnail.Dispose();
            }
        }

        /// <summary>
        /// 缩略图缓存按数量上限淘汰最早插入项，避免长篇文档常驻内存持续增长。
        /// </summary>
        private void TrimThumbnailCache()
        {
            while (_thumbnailCacheOrder.Count > MaxThumbnailCacheCount)
            {
                int oldest = _thumbnailCacheOrder.Dequeue();
                if (_thumbnailCache.TryGetValue(oldest, out var bmp))
                {
                    _thumbnailCache.Remove(oldest);
                    try { bmp.Dispose(); } catch { }
                }
            }
        }

        public async Task GenerateThumbnailsAsync()
        {
            if (_isGeneratingThumbnails) return;
            _isGeneratingThumbnails = true;
            var currentCts = _thumbnailCts;

            try
            {
                if (_pdfService == null && !_isImageMode)
                {
                    _logger.LogWarning("GenerateThumbnailsAsync: _pdfService is null");
                    return;
                }

                int total = PageCount;
                for (int i = 0; i < total; i++)
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
            if (_disposed) return;
            _disposed = true;

            ClearCache();

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // 等待后台预渲染退出后再释放信号量，避免其 WaitAsync/Release 抛 ObjectDisposedException
            try
            {
                var pending = _backgroundTasks.Values.ToArray();
                if (pending.Length > 0)
                {
                    Task.WhenAll(pending).Wait(1000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Background pre-render tasks did not finish before dispose");
            }

            _renderSemaphore.Dispose();
            _preRenderSemaphore.Dispose();
        }

        public byte[] ApplyNightMode(byte[] bitmap)
        {
            // 白天模式直接返回，避免多余的解码与位图分配
            if (!_isNightMode)
                return bitmap;

            using var bmp = BytesToBitmap(bitmap);
            if (bmp == null)
                return bitmap;

            using var inverted = ApplyNightMode(bmp);
            return BitmapToBytes(inverted) ?? bitmap;
        }

        public Bitmap ApplyNightMode(Bitmap source)
        {
            var inverted = new Bitmap(source.Width, source.Height);
            try
            {
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
                        graphics.DrawImage(source, new Rectangle(0, 0, source.Width, source.Height),
                            0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
                    }
                }

                return inverted;
            }
            catch
            {
                inverted.Dispose();
                throw;
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