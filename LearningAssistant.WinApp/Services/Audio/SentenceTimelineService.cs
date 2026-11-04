using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 提供切片/字幕/手动等外部边界层；P0 阶段由 PdfAudioTrackManager 接入只读切片。
/// </summary>
public delegate Task<IReadOnlyList<SentenceBoundaryLayer>> AudioBoundaryLayerProvider(
    AudioTrack track,
    int durationMs,
    CancellationToken cancellationToken);

/// <summary>把字幕等文本叠加到已解析句子上；不得修改句子时间。</summary>
public delegate Task<IReadOnlyList<AudioSentenceSpan>> AudioSentenceTextProvider(
    AudioTrack track,
    int durationMs,
    IReadOnlyList<AudioSentenceSpan> spans,
    CancellationToken cancellationToken);

/// <summary>
/// 外部边界来源的签名（如字幕文件 mtime）；拼进去重键，来源变化即重新合成。
/// </summary>
public delegate string? AudioBoundarySignatureProvider(AudioTrack track);

/// <summary>
/// 句子时间轴编排：缓存命中 → 覆盖率决策 → VAD 分析 → 多层边界解析 → 发布。
/// 负责取消、代际与同参数请求去重；缓存只保存 VAD 结果，切片/字幕/手动每次重新合并。
/// 字幕文件签名参与去重键：字幕被编辑后自动重新合成，但 VAD 缓存仍命中。
/// </summary>
public sealed class SentenceTimelineService : IDisposable
{
    private const string VadSource = "vad";
    private const string ShadowingSource = "shadowing";
    private const string SubtitleSource = "subtitle";
    private const string ManualSource = "manual";

    private readonly AudioTimelineStore _store;
    private readonly AudioDecodeAnalysisService _analysisService;
    private readonly AudioBoundaryLayerProvider? _layerProvider;
    private readonly AudioSentenceTextProvider? _textProvider;
    private readonly AudioBoundarySignatureProvider? _signatureProvider;
    private readonly SynchronizationContext? _uiContext;
    private readonly object _sync = new();

    private CancellationTokenSource? _cts;
    private Task<AudioSentenceTimeline?>? _inflight;
    private string? _inflightKey;
    private int _generation;
    private bool _disposed;

    public SentenceTimelineService(
        AudioTimelineStore store,
        AudioDecodeAnalysisService analysisService,
        AudioBoundaryLayerProvider? layerProvider = null,
        AudioSentenceTextProvider? textProvider = null,
        AudioBoundarySignatureProvider? signatureProvider = null)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        _layerProvider = layerProvider;
        _textProvider = textProvider;
        _signatureProvider = signatureProvider;
        _uiContext = SynchronizationContext.Current;
    }

    public AudioSentenceTimeline? Current { get; private set; }

    public event EventHandler<AudioSentenceTimeline?>? TimelineChanged;

    /// <summary>
    /// 确保曲目时间轴可用。相同指纹与参数未完成请求会复用同一任务；forceReanalyze 跳过缓存。
    /// 返回 null 表示取消、文件不存在或分析失败。
    /// </summary>
    public Task<AudioSentenceTimeline?> EnsureTimelineAsync(
        AudioTrack track,
        int durationMs,
        AudioTimelineOptions? options = null,
        bool forceReanalyze = false,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (track == null || string.IsNullOrWhiteSpace(track.FilePath) || !File.Exists(track.FilePath))
            return Task.FromResult<AudioSentenceTimeline?>(null);

        options ??= new AudioTimelineOptions();
        int duration = durationMs > 0 ? durationMs : track.DurationMs;
        string fingerprint = duration > 0
            ? AudioTimelineStore.ComputeFingerprint(track.FilePath, duration)
            : "";
        string trackKey = string.IsNullOrWhiteSpace(track.RelativePath) ? track.FilePath : track.RelativePath;
        string signature = SafeSignature(track);
        string key = fingerprint + "|" + AudioTimelineStore.ComputeOptionsSignature(options)
                     + "|" + signature;

        lock (_sync)
        {
            if (_disposed)
                return Task.FromResult<AudioSentenceTimeline?>(null);

            if (!forceReanalyze && _inflight != null && _inflightKey == key && !_inflight.IsCompleted)
                return _inflight;

            CancelActiveLocked();

            var cts = new CancellationTokenSource();
            if (cancellationToken.CanBeCanceled)
            {
                cancellationToken.Register(() =>
                {
                    try { cts.Cancel(); }
                    catch (ObjectDisposedException) { }
                });
            }
            _cts = cts;
            _inflightKey = key;
            int generation = ++_generation;
            _inflight = Task.Run(() => RunAsync(
                track, trackKey, fingerprint, duration, options, forceReanalyze, progress, cts.Token, generation));
            return _inflight;
        }
    }

    /// <summary>曲目切换时调用：取消进行中的分析并清空当前时间轴。</summary>
    public void Reset()
    {
        CancellationTokenSource? cts;
        lock (_sync)
        {
            _generation++;
            cts = _cts;
            _cts = null;
            _inflight = null;
            _inflightKey = null;
        }

        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { }
        cts?.Dispose();

        Publish(null);
    }

    /// <summary>删除指定指纹的自动缓存，若正是当前时间轴则清空。</summary>
    public void Invalidate(string? fingerprint)
    {
        if (string.IsNullOrWhiteSpace(fingerprint))
            return;

        _store.Delete(fingerprint);
        if (string.Equals(Current?.Fingerprint, fingerprint, StringComparison.OrdinalIgnoreCase))
            Reset();
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;
            _generation++;
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
            _inflight = null;
            _inflightKey = null;
        }
    }

    private async Task<AudioSentenceTimeline?> RunAsync(
        AudioTrack track,
        string trackKey,
        string fingerprint,
        int durationMs,
        AudioTimelineOptions options,
        bool forceReanalyze,
        IProgress<double>? progress,
        CancellationToken cancellationToken,
        int generation)
    {
        string workingFingerprint = fingerprint;
        int workingDuration = durationMs;

        try
        {
            AudioSentenceTimeline? cached = null;
            if (workingDuration > 0 && !forceReanalyze)
            {
                cached = _store.Load(workingFingerprint, options);
                if (cached != null && cached.DurationMs > 0)
                    workingDuration = cached.DurationMs;
            }
            if (!IsCurrent(generation, cancellationToken)) return null;

            var layers = await LoadLayersAsync(track, workingDuration, cancellationToken).ConfigureAwait(false);
            if (!IsCurrent(generation, cancellationToken)) return null;

            var sliceSpans = CollectSpans(layers, ShadowingSource);
            var subtitleSpans = CollectSpans(layers, SubtitleSource);
            var manualSpans = CollectSpans(layers, ManualSource);
            var plan = AnalysisCoveragePlan.Plan(sliceSpans, manualSpans, workingDuration, subtitleSpans);

            IReadOnlyList<AudioSentenceSpan> vadSpans;
            string engine;
            if (cached != null)
            {
                vadSpans = cached.Sentences;
                engine = cached.Analysis?.Engine ?? "energy-vad";
            }
            else if (plan.Mode == AnalysisCoverageMode.Full)
            {
                // 切片已覆盖 ≥90%，不再跑 VAD；也不写缓存，避免把“无 VAD”结果固化
                vadSpans = Array.Empty<AudioSentenceSpan>();
                engine = "boundary-only";
            }
            else
            {
                var analysis = await _analysisService
                    .AnalyzeAsync(
                        track.FilePath,
                        options,
                        plan.Mode == AnalysisCoverageMode.Partial ? plan.Holes : null,
                        progress,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (!IsCurrent(generation, cancellationToken)) return null;

                if (analysis.DurationMs > 0 && analysis.DurationMs != workingDuration)
                {
                    workingDuration = analysis.DurationMs;
                    if (string.IsNullOrEmpty(workingFingerprint))
                        workingFingerprint = AudioTimelineStore.ComputeFingerprint(track.FilePath, workingDuration);
                }

                vadSpans = SentenceBoundaryBuilder.Build(
                    analysis.Coarse, analysis.Fine, workingDuration, options);
                engine = "energy-vad";
                SaveVadCache(trackKey, workingFingerprint, workingDuration, options, vadSpans, engine);
            }

            if (!IsCurrent(generation, cancellationToken)) return null;

            var mergedLayers = new List<SentenceBoundaryLayer>(layers);
            if (vadSpans.Count > 0)
                mergedLayers.Add(new SentenceBoundaryLayer(VadSource, SentenceBoundaryResolver.VadPriority, vadSpans));

            var resolved = SentenceBoundaryResolver.Resolve(workingDuration, options, mergedLayers.ToArray());
            if (!IsCurrent(generation, cancellationToken)) return null;

            resolved = await ApplyTextAsync(track, workingDuration, resolved, cancellationToken).ConfigureAwait(false);
            if (!IsCurrent(generation, cancellationToken)) return null;

            var timeline = BuildTimeline(
                trackKey,
                workingFingerprint,
                workingDuration,
                options,
                resolved,
                engine);

            Publish(timeline);
            return timeline;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception)
        {
            if (!string.IsNullOrEmpty(workingFingerprint))
                _store.SaveFailure(workingFingerprint, TimeSpan.FromMinutes(10), "audio timeline analysis failed");
            return null;
        }
    }

    private async Task<IReadOnlyList<SentenceBoundaryLayer>> LoadLayersAsync(
        AudioTrack track,
        int durationMs,
        CancellationToken cancellationToken)
    {
        if (_layerProvider == null) return Array.Empty<SentenceBoundaryLayer>();

        try
        {
            return await _layerProvider(track, durationMs, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 切片/字幕读取失败不应阻断 VAD 兜底
            return Array.Empty<SentenceBoundaryLayer>();
        }
    }

    private static List<AudioSentenceSpan> CollectSpans(
        IReadOnlyList<SentenceBoundaryLayer> layers,
        string source)
    {
        var result = new List<AudioSentenceSpan>();
        foreach (var layer in layers)
        {
            if (!string.Equals(layer.Source, source, StringComparison.OrdinalIgnoreCase)) continue;
            result.AddRange(layer.Spans);
        }
        return result;
    }

    private async Task<IReadOnlyList<AudioSentenceSpan>> ApplyTextAsync(
        AudioTrack track,
        int durationMs,
        IReadOnlyList<AudioSentenceSpan> spans,
        CancellationToken cancellationToken)
    {
        if (_textProvider == null) return spans;

        try
        {
            var enriched = await _textProvider(track, durationMs, spans, cancellationToken).ConfigureAwait(false);
            return enriched != null && enriched.Count > 0 ? enriched : spans;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            // 文本叠加失败不应阻断时间轴发布
            return spans;
        }
    }

    private string SafeSignature(AudioTrack track)
    {
        if (_signatureProvider == null) return "";
        try
        {
            return _signatureProvider(track) ?? "";
        }
        catch (Exception)
        {
            return "";
        }
    }

    private void SaveVadCache(
        string trackKey,
        string fingerprint,
        int durationMs,
        AudioTimelineOptions options,
        IReadOnlyList<AudioSentenceSpan> vadSpans,
        string engine)
    {
        if (string.IsNullOrEmpty(fingerprint)) return;

        var timeline = new AudioSentenceTimeline
        {
            SchemaVersion = AudioTimelineStore.CurrentSchemaVersion,
            TrackKey = trackKey,
            Fingerprint = fingerprint,
            DurationMs = durationMs,
            Sentences = vadSpans.ToList(),
            Analysis = new AudioTimelineAnalysis
            {
                Engine = engine,
                AnalyzedAt = DateTime.Now,
                Options = options,
                Confidence = vadSpans.Count == 0 ? 0.3 : vadSpans.Min(span => span.Confidence),
                SourceCounts = new Dictionary<string, int> { [VadSource] = vadSpans.Count }
            }
        };

        _store.Save(timeline);
    }

    private static AudioSentenceTimeline BuildTimeline(
        string trackKey,
        string fingerprint,
        int durationMs,
        AudioTimelineOptions options,
        IReadOnlyList<AudioSentenceSpan> sentences,
        string engine)
    {
        var counts = new Dictionary<string, int>();
        foreach (var span in sentences)
        {
            string source = span.BoundarySource ?? "unknown";
            counts[source] = counts.TryGetValue(source, out int value) ? value + 1 : 1;
        }

        return new AudioSentenceTimeline
        {
            SchemaVersion = AudioTimelineStore.CurrentSchemaVersion,
            TrackKey = trackKey,
            Fingerprint = fingerprint,
            DurationMs = durationMs,
            Sentences = sentences.ToList(),
            Analysis = new AudioTimelineAnalysis
            {
                Engine = engine,
                AnalyzedAt = DateTime.Now,
                Options = options,
                Confidence = sentences.Count == 0 ? 0.3 : sentences.Min(span => span.Confidence),
                SourceCounts = counts
            }
        };
    }

    private bool IsCurrent(int generation, CancellationToken cancellationToken)
        => !_disposed && !cancellationToken.IsCancellationRequested && generation == Volatile.Read(ref _generation);

    private void Publish(AudioSentenceTimeline? timeline)
    {
        void Apply()
        {
            lock (_sync)
            {
                if (_disposed) return;
                Current = timeline;
            }
            TimelineChanged?.Invoke(this, timeline);
        }

        if (_uiContext != null && SynchronizationContext.Current != _uiContext)
            _uiContext.Post(_ => Apply(), null);
        else
            Apply();
    }

    private void CancelActiveLocked()
    {
        var cts = _cts;
        _cts = null;
        _inflight = null;
        _inflightKey = null;
        try { cts?.Cancel(); }
        catch (ObjectDisposedException) { }
        cts?.Dispose();
    }
}
