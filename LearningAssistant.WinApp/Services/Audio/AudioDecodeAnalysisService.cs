using LearningAssistant.Abstractions.Audio;
using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Shadowing;
using NAudio.Wave;

namespace LearningAssistant.Services.Audio;

/// <summary>一次 VAD 分析的结果：粗/细两遍区间与扫描模式。</summary>
public sealed class AudioVadAnalysisResult
{
    public int DurationMs { get; init; }

    public IReadOnlyList<VadSegment> Coarse { get; init; } = Array.Empty<VadSegment>();

    public IReadOnlyList<VadSegment> Fine { get; init; } = Array.Empty<VadSegment>();

    /// <summary>实际使用的分析器名，写入时间轴 Engine。</summary>
    public string Engine { get; init; } = "energy-vad";

    /// <summary>是否只分析了空洞。</summary>
    public bool UsedHoleScan { get; init; }

    /// <summary>空洞扫描落点校验失败后是否回退整轨。</summary>
    public bool FellBackToFullScan { get; init; }
}

/// <summary>
/// 音频解码分析：流式解码为单声道，同时喂给粗/细两遍分析器。
/// 默认使用能量 VAD；注册 <see cref="IAudioSegmentAnalyzerFactory"/> 后可换成 WebRTC/Silero。
/// 支持整轨扫描与空洞扫描；空洞前后扩展 1.5 秒用于噪声估计，但只保留完全落在洞内的句子。
/// </summary>
public sealed class AudioDecodeAnalysisService
{
    private const int BlockSize = 8192;
    private const int HoleExpansionMs = 1500;
    private const int SeekToleranceMs = 250;

    private readonly IAudioSegmentAnalyzerFactory? _analyzerFactory;

    public AudioDecodeAnalysisService()
    {
    }

    /// <summary>使用外部分析器工厂；工厂名需与参数中的 AnalyzerName 一致才会生效。</summary>
    public AudioDecodeAnalysisService(IAudioSegmentAnalyzerFactory analyzerFactory)
    {
        _analyzerFactory = analyzerFactory;
    }

    public Task<AudioVadAnalysisResult> AnalyzeAsync(
        string audioPath,
        AudioTimelineOptions options,
        IReadOnlyList<AudioTimeRange>? holes = null,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(
            () => Analyze(audioPath, options, holes, progress, cancellationToken),
            cancellationToken);
    }

    private AudioVadAnalysisResult Analyze(
        string audioPath,
        AudioTimelineOptions options,
        IReadOnlyList<AudioTimeRange>? holes,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(audioPath))
            throw new FileNotFoundException("找不到音频文件", audioPath);

        options ??= new AudioTimelineOptions();
        var coarseOptions = CreateAnalyzerOptions(options, options.SentenceGapMs);
        var fineOptions = CreateAnalyzerOptions(options, options.FineGapMs);

        using var reader = new AudioFileReader(audioPath);
        int durationMs = (int)Math.Round(reader.TotalTime.TotalMilliseconds);
        if (durationMs <= 0)
        {
            return new AudioVadAnalysisResult
            {
                DurationMs = durationMs,
                Engine = ResolveEngine(options),
                UsedHoleScan = false
            };
        }

        if (holes == null || holes.Count == 0)
            return AnalyzeFull(reader, durationMs, options, coarseOptions, fineOptions, progress, cancellationToken);

        var coarseAll = new List<VadSegment>();
        var fineAll = new List<VadSegment>();
        int holeCount = holes.Count;

        for (int i = 0; i < holeCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var hole = holes[i];
            int expandStart = Math.Max(0, hole.StartMs - HoleExpansionMs);
            int expandEnd = Math.Min(durationMs, hole.EndMs + HoleExpansionMs);
            if (expandEnd <= expandStart) continue;

            int landedMs;
            try
            {
                reader.CurrentTime = TimeSpan.FromMilliseconds(expandStart);
                landedMs = (int)Math.Round(reader.CurrentTime.TotalMilliseconds);
            }
            catch (Exception)
            {
                return AnalyzeFullAfterFallback(reader, durationMs, options, coarseOptions, fineOptions, progress, cancellationToken);
            }

            if (Math.Abs(landedMs - expandStart) > SeekToleranceMs)
                return AnalyzeFullAfterFallback(reader, durationMs, options, coarseOptions, fineOptions, progress, cancellationToken);

            int index = i;
            var range = DecodeRange(
                reader,
                landedMs,
                expandEnd,
                options,
                coarseOptions,
                fineOptions,
                1.0 / holeCount,
                ratio => progress?.Report((index + ratio) / holeCount),
                cancellationToken);

            AddFiltered(coarseAll, range.Coarse, landedMs, hole.StartMs, hole.EndMs);
            AddFiltered(fineAll, range.Fine, landedMs, hole.StartMs, hole.EndMs);
        }

        progress?.Report(1);
        return new AudioVadAnalysisResult
        {
            DurationMs = durationMs,
            Coarse = Sort(coarseAll),
            Fine = Sort(fineAll),
            Engine = ResolveEngine(options),
            UsedHoleScan = true
        };
    }

    private AudioVadAnalysisResult AnalyzeFull(
        AudioFileReader reader,
        int durationMs,
        AudioTimelineOptions options,
        SilenceVadOptions coarseOptions,
        SilenceVadOptions fineOptions,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        int startMs = Math.Max(0, (int)Math.Round(reader.CurrentTime.TotalMilliseconds));
        var range = DecodeRange(
            reader,
            startMs,
            durationMs,
            options,
            coarseOptions,
            fineOptions,
            1.0,
            ratio => progress?.Report(ratio),
            cancellationToken);

        var coarse = AddFiltered(new List<VadSegment>(), range.Coarse, startMs, 0, durationMs);
        var fine = AddFiltered(new List<VadSegment>(), range.Fine, startMs, 0, durationMs);

        progress?.Report(1);
        return new AudioVadAnalysisResult
        {
            DurationMs = durationMs,
            Coarse = Sort(coarse),
            Fine = Sort(fine),
            Engine = ResolveEngine(options),
            UsedHoleScan = false
        };
    }

    private AudioVadAnalysisResult AnalyzeFullAfterFallback(
        AudioFileReader reader,
        int durationMs,
        AudioTimelineOptions options,
        SilenceVadOptions coarseOptions,
        SilenceVadOptions fineOptions,
        IProgress<double>? progress,
        CancellationToken cancellationToken)
    {
        try
        {
            reader.CurrentTime = TimeSpan.Zero;
        }
        catch (Exception)
        {
            // 无法回零时仍按当前位置继续，最多少分析开头一小段
        }

        var result = AnalyzeFull(reader, durationMs, options, coarseOptions, fineOptions, progress, cancellationToken);
        return new AudioVadAnalysisResult
        {
            DurationMs = result.DurationMs,
            Coarse = result.Coarse,
            Fine = result.Fine,
            Engine = result.Engine,
            UsedHoleScan = false,
            FellBackToFullScan = true
        };
    }

    private sealed class DecodedRange
    {
        public IReadOnlyList<VadSegment> Coarse { get; init; } = Array.Empty<VadSegment>();

        public IReadOnlyList<VadSegment> Fine { get; init; } = Array.Empty<VadSegment>();
    }

    /// <summary>从当前位置解码到 endMs，分别喂入粗/细分析器。</summary>
    private DecodedRange DecodeRange(
        AudioFileReader reader,
        int startMs,
        int endMs,
        AudioTimelineOptions options,
        SilenceVadOptions coarseOptions,
        SilenceVadOptions fineOptions,
        double progressScale,
        Action<double>? report,
        CancellationToken cancellationToken)
    {
        int sampleRate = reader.WaveFormat.SampleRate;
        var coarse = CreateAnalyzer(sampleRate, coarseOptions, options.AnalyzerName);
        var fine = CreateAnalyzer(sampleRate, fineOptions, options.AnalyzerName);

        var buffer = new float[BlockSize];
        var mono = new float[BlockSize];
        int channels = Math.Max(1, reader.WaveFormat.Channels);

        int read;
        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int frames = read / channels;
            for (int f = 0; f < frames; f++)
            {
                int at = f * channels;
                float sum = 0;
                for (int c = 0; c < channels; c++)
                    sum += buffer[at + c];
                mono[f] = sum / channels;
            }

            coarse.Push(mono.AsSpan(0, frames));
            fine.Push(mono.AsSpan(0, frames));

            if (reader.CurrentTime.TotalMilliseconds >= endMs)
                break;

            double spanMs = Math.Max(1, endMs - startMs);
            double position = Math.Clamp(reader.CurrentTime.TotalMilliseconds - startMs, 0, spanMs);
            report?.Invoke(Math.Clamp(position / spanMs * progressScale, 0, progressScale));
        }

        return new DecodedRange
        {
            Coarse = coarse.Complete(),
            Fine = fine.Complete()
        };
    }

    /// <summary>按 AnalyzerName 选择分析器；未匹配到工厂时回退能量 VAD。</summary>
    private IAudioSegmentAnalyzer CreateAnalyzer(int sampleRate, SilenceVadOptions options, string? analyzerName)
    {
        if (_analyzerFactory != null
            && !string.IsNullOrWhiteSpace(_analyzerFactory.Name)
            && string.Equals(_analyzerFactory.Name, analyzerName, StringComparison.OrdinalIgnoreCase))
        {
            return _analyzerFactory.Create(sampleRate, options);
        }

        return new SilenceVadAnalyzer(sampleRate, options);
    }

    /// <summary>实际使用的引擎名，用于时间轴审计字段。</summary>
    private string ResolveEngine(AudioTimelineOptions options)
    {
        if (_analyzerFactory != null
            && !string.IsNullOrWhiteSpace(_analyzerFactory.Name)
            && string.Equals(_analyzerFactory.Name, options.AnalyzerName, StringComparison.OrdinalIgnoreCase))
        {
            return _analyzerFactory.Name;
        }

        return "energy-vad";
    }

    private static SilenceVadOptions CreateAnalyzerOptions(AudioTimelineOptions options, int gapMs)
    {
        return new SilenceVadOptions
        {
            FrameMilliseconds = 20,
            MinSegmentMilliseconds = Math.Max(1, options.MinSpeechMs),
            MinSilenceMilliseconds = Math.Max(20, gapMs),
            PaddingMilliseconds = 0,
            NoiseMultiplier = options.NoiseMultiplier,
            MinAbsoluteRms = options.MinAbsoluteRms
        };
    }

    private static List<VadSegment> AddFiltered(
        List<VadSegment> target,
        IReadOnlyList<VadSegment> segments,
        int offsetMs,
        int clipStartMs,
        int clipEndMs)
    {
        foreach (var segment in segments)
        {
            int start = segment.StartMs + offsetMs;
            int end = segment.EndMs + offsetMs;
            if (start < clipStartMs || end > clipEndMs) continue;
            if (end <= start) continue;
            target.Add(new VadSegment(start, end));
        }
        return target;
    }

    private static IReadOnlyList<VadSegment> Sort(List<VadSegment> segments)
    {
        segments.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
        return segments;
    }
}
