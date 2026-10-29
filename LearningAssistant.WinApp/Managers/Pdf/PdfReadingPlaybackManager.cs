using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Pdf;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers
{
    /// <summary>
    /// PDF 连续朗读调度：按页生成句子，逐句朗读并驱动同步高亮，页末自动翻页，
    /// 并按“文档路径 + 修改时间”保存断点。高亮只作为视图层叠加，不写入标注数据。
    /// </summary>
    public sealed class PdfReadingPlaybackManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly IPdfReaderFormAccess _form;
        private readonly PdfReadingPositionStore _positionStore;
        private readonly IPdfAutoLayoutService? _autoLayoutService;

        /// <summary>页级句子缓存：一次朗读会话内复用，换文档时清空</summary>
        private readonly Dictionary<int, List<PdfSentence>> _sentenceCache = new();

        private readonly SemaphoreSlim _resumeGate = new(0);

        private CancellationTokenSource? _runCts;
        /// <summary>当前句的取消源：暂停/跳句时取消，避免合成完成后仍开始播放</summary>
        private CancellationTokenSource? _speechCts;
        private bool _paused;
        private bool _skipRequested;
        private int _skipDelta;
        private int _speechVersion;
        private bool _autoAdvancingPage;
        private bool _disposed;

        public PdfReadingPlaybackManager(
            ILogger logger,
            IPdfReaderFormAccess form,
            PdfReadingPositionStore positionStore,
            IPdfAutoLayoutService? autoLayoutService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _form = form ?? throw new ArgumentNullException(nameof(form));
            _positionStore = positionStore ?? throw new ArgumentNullException(nameof(positionStore));
            _autoLayoutService = autoLayoutService;
        }

        /// <summary>朗读状态或当前句变化，供界面刷新控制条与高亮。</summary>
        public event EventHandler? StateChanged;

        /// <summary>是否处于朗读会话（播放或暂停）。</summary>
        public bool IsActive => _runCts != null;

        /// <summary>是否已暂停。</summary>
        public bool IsPaused => _paused;

        /// <summary>当前正在朗读的句子；停止后为 null。</summary>
        public PdfSentence? CurrentSentence { get; private set; }

        /// <summary>语速倍数（1.0 为正常速度）。</summary>
        public double Speed { get; private set; } = 1.0;

        /// <summary>最近一次读取到的本文档断点。</summary>
        public PdfReadingPosition? SavedPosition { get; private set; }

        /// <summary>
        /// 取指定页的句子：文本层优先，扫描页回退到已缓存的 OCR 版面块。
        /// </summary>
        public List<PdfSentence> GetSentencesForPage(int pageIndex)
        {
            if (pageIndex < 0)
                return new List<PdfSentence>();
            if (_sentenceCache.TryGetValue(pageIndex, out var cached))
                return cached;

            var sentences = new List<PdfSentence>();
            try
            {
                var lines = _form.Presenter?.GetPageTextLines(pageIndex);
                sentences = PdfSentenceSegmenter.BuildSentences(pageIndex, lines);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "读取第 {Page} 页文本行失败", pageIndex);
            }

            if (sentences.Count == 0 && _autoLayoutService != null && !string.IsNullOrWhiteSpace(_form.CurrentPdfPath))
            {
                try
                {
                    var layout = _autoLayoutService.TryGetCachedPage(_form.CurrentPdfPath!, pageIndex);
                    if (layout != null)
                        sentences = PdfSentenceSegmenter.BuildSentencesFromBlocks(pageIndex, layout.Blocks);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "读取第 {Page} 页 OCR 版面失败", pageIndex);
                }
            }

            _sentenceCache[pageIndex] = sentences;
            return sentences;
        }

        /// <summary>
        /// 从指定页/句开始连续朗读；返回 false 表示前置条件不满足。
        /// </summary>
        public bool Start(int pageIndex, int sentenceIndex)
        {
            var presenter = _form.Presenter;
            if (presenter == null)
            {
                _form.ShowWarning("请先打开 PDF 文件");
                return false;
            }

            if (!presenter.IsTTSServiceAvailable())
            {
                _form.ShowWarning("朗读服务不可用，请检查 TTS 配置");
                return false;
            }

            StopCore();
            _sentenceCache.Clear();
            _paused = false;
            _skipRequested = false;
            _skipDelta = 0;

            var cts = new CancellationTokenSource();
            _runCts = cts;
            CurrentSentence = null;
            RaiseState();
            InvalidateCanvas();

            _ = RunAsync(Math.Max(0, pageIndex), Math.Max(0, sentenceIndex), cts);
            return true;
        }

        /// <summary>暂停：立即停掉当前音频，恢复后重读当前句。</summary>
        public void Pause()
        {
            if (_runCts == null || _paused)
                return;

            _paused = true;
            CancelSpeech();
            RaiseState();
        }

        /// <summary>恢复朗读当前句。</summary>
        public void Resume()
        {
            if (_runCts == null || !_paused)
                return;

            _paused = false;
            _resumeGate.Release();
            RaiseState();
        }

        /// <summary>上一句（delta = -1）或下一句（delta = 1），跨页时先切页再朗读。</summary>
        public void Skip(int delta)
        {
            if (_runCts == null || delta == 0)
                return;

            _skipRequested = true;
            _skipDelta = delta;
            // 跳句即视为继续播放，避免暂停态下跳句后卡住
            _paused = false;
            CancelSpeech();
            _resumeGate.Release();
            RaiseState();
        }

        /// <summary>调整语速：正在朗读时从当前句重读，保证立即生效。</summary>
        public void SetSpeed(double speed)
        {
            double value = Math.Clamp(speed, 0.5, 2.0);
            if (Math.Abs(value - Speed) < 0.001)
                return;

            Speed = value;
            if (_runCts != null && CurrentSentence != null)
                CancelSpeech();
            RaiseState();
        }

        /// <summary>停止朗读并清空当前句高亮。</summary>
        public void Stop()
        {
            bool had = _runCts != null || CurrentSentence != null;
            StopCore();
            CurrentSentence = null;
            if (had)
            {
                RaiseState();
                InvalidateCanvas();
            }
        }

        /// <summary>文档切换：停止朗读、清缓存并读取新文档断点。</summary>
        public void OnDocumentChanged()
        {
            StopCore();
            _sentenceCache.Clear();
            _paused = false;
            _skipRequested = false;
            CurrentSentence = null;
            SavedPosition = _positionStore.Load(_form.CurrentPdfPath);
            RaiseState();
            InvalidateCanvas();
        }

        /// <summary>
        /// 手动翻页时调用：自动翻页由朗读流程发起，其余翻页视为用户离开当前朗读位置，直接停止。
        /// </summary>
        public void HandlePageChanged()
        {
            if (_runCts == null || _autoAdvancingPage)
                return;

            Stop();
        }

        /// <summary>清除本文档断点。</summary>
        public void ClearSavedPosition()
        {
            _positionStore.Clear(_form.CurrentPdfPath);
            SavedPosition = null;
        }

        private async Task RunAsync(int pageIndex, int sentenceIndex, CancellationTokenSource cts)
        {
            var token = cts.Token;
            int page = pageIndex;
            int sentence = sentenceIndex;
            bool completed = false;

            try
            {
                var presenter = _form.Presenter;
                int pageCount = presenter?.PageCount ?? 0;
                if (pageCount <= 0)
                {
                    _form.ShowWarning("文档尚未加载完成");
                    return;
                }

                page = Math.Clamp(page, 0, pageCount - 1);

                while (!token.IsCancellationRequested)
                {
                    if (_paused)
                    {
                        await _resumeGate.WaitAsync(token);
                        continue;
                    }

                    if (_skipRequested)
                    {
                        _skipRequested = false;
                        int delta = _skipDelta;
                        _skipDelta = 0;
                        MoveSentence(ref page, ref sentence, delta, pageCount);
                        SavePosition(page, sentence);
                        RaiseState();
                        InvalidateCanvas();
                        continue;
                    }

                    var sentences = GetSentencesForPage(page);
                    if (sentences.Count == 0)
                    {
                        if (page + 1 < pageCount)
                        {
                            page++;
                            sentence = 0;
                            SavePosition(page, sentence);
                            continue;
                        }

                        completed = true;
                        _form.ShowToast("已到文档末尾，未找到可朗读的文字");
                        break;
                    }

                    if (sentence >= sentences.Count)
                    {
                        if (page + 1 < pageCount)
                        {
                            page++;
                            sentence = 0;
                            SavePosition(page, sentence);
                            continue;
                        }

                        completed = true;
                        _form.ShowToast("已到文档末尾，朗读结束");
                        break;
                    }

                    sentence = Math.Max(0, sentence);

                    if (_form.CurrentPageIndex != page)
                    {
                        try
                        {
                            _autoAdvancingPage = true;
                            if (presenter != null)
                                await presenter.RenderPage(page);
                        }
                        finally
                        {
                            _autoAdvancingPage = false;
                        }

                        token.ThrowIfCancellationRequested();
                        await WaitForPageAsync(page, token);
                    }

                    var current = sentences[sentence];
                    CurrentSentence = current;
                    SavePosition(page, sentence);
                    RaiseState();
                    InvalidateCanvas();

                    int version = ++_speechVersion;
                    if (presenter == null)
                        break;

                    var speechCts = new CancellationTokenSource();
                    _speechCts = speechCts;
                    try
                    {
                        await presenter.SpeakTextAndWaitAsync(current.Text, (float)Speed, speechCts.Token);
                    }
                    finally
                    {
                        if (ReferenceEquals(_speechCts, speechCts))
                            _speechCts = null;
                        speechCts.Dispose();
                    }

                    if (token.IsCancellationRequested)
                        break;

                    // 暂停/跳句/停止会改写代次，此时不推进到下一句
                    if (_paused || _skipRequested || version != _speechVersion)
                        continue;

                    sentence++;
                }
            }
            catch (OperationCanceledException)
            {
                // 停止朗读
            }
            catch (ObjectDisposedException)
            {
                // 窗口关闭后调度器已释放，等待信号量时可能抛此异常
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "连续朗读失败");
                _form.ShowWarning("连续朗读失败：" + ex.Message);
            }
            finally
            {
                if (ReferenceEquals(_runCts, cts))
                {
                    _runCts = null;
                    _paused = false;
                    CurrentSentence = null;
                    if (!completed)
                        SavePosition(page, Math.Max(0, sentence));
                    RaiseState();
                    InvalidateCanvas();
                }

                cts.Dispose();
            }
        }

        private void MoveSentence(ref int page, ref int sentence, int delta, int pageCount)
        {
            if (delta > 0)
            {
                var sentences = GetSentencesForPage(page);
                if (sentence + 1 < sentences.Count)
                {
                    sentence++;
                    return;
                }

                if (page + 1 < pageCount)
                {
                    page++;
                    sentence = 0;
                }
                else
                {
                    sentence = Math.Max(0, sentences.Count - 1);
                }
                return;
            }

            if (sentence > 0)
            {
                sentence--;
                return;
            }

            if (page > 0)
            {
                page--;
                sentence = Math.Max(0, GetSentencesForPage(page).Count - 1);
            }
        }

        private async Task WaitForPageAsync(int page, CancellationToken token)
        {
            // 渲染完成即设置页码；留少量等待给异步显示收尾，避免连续朗读抢跑
            for (int i = 0; i < 40 && _form.CurrentPageIndex != page; i++)
                await Task.Delay(50, token);
        }

        private void SavePosition(int pageIndex, int sentenceIndex)
        {
            _positionStore.Save(_form.CurrentPdfPath, pageIndex, sentenceIndex);
            // 同步内存断点，保证停止后「续听」立即指向最新句，而不是文档打开时的旧位置
            SavedPosition = new PdfReadingPosition
            {
                PageIndex = pageIndex,
                SentenceIndex = sentenceIndex,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private void CancelSpeech()
        {
            _speechVersion++;

            var cts = _speechCts;
            _speechCts = null;
            try
            {
                cts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 朗读循环恰好结束
            }

            _ = _form.Presenter?.StopSpeakingAsync();
        }

        private void StopCore()
        {
            var cts = _runCts;
            if (cts == null)
                return;

            _runCts = null;
            _paused = false;
            _skipRequested = false;
            _skipDelta = 0;

            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // 朗读循环恰好结束
            }

            CancelSpeech();
        }

        private void RaiseState() => StateChanged?.Invoke(this, EventArgs.Empty);

        private void InvalidateCanvas()
        {
            var picture = _form.PictureBoxPdf;
            if (picture != null && !picture.IsDisposed)
                picture.Invalidate();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            StopCore();
            _resumeGate.Dispose();
        }
    }
}
