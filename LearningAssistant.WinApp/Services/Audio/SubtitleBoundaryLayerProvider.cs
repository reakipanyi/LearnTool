using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Services.Media;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 把 `.srt` / `.lrc` 字幕作为句子边界的可信来源（优先级低于切片、高于 VAD），
/// 同时把字幕文本叠加到已解析句子上。
/// 解析结果按「路径 + 修改时间 + 大小」缓存，字幕文件被编辑后下次请求自动重读。
/// </summary>
public sealed class SubtitleBoundaryLayerProvider
{
    public const string Source = "subtitle";

    private const int DefaultCueDurationMs = 4000;

    private readonly ISubtitleParser _parser;
    private readonly object _sync = new();
    private readonly Dictionary<string, CachedSubtitle> _cache = new(StringComparer.OrdinalIgnoreCase);

    public SubtitleBoundaryLayerProvider(ISubtitleParser parser)
    {
        _parser = parser ?? throw new ArgumentNullException(nameof(parser));
    }

    /// <summary>字幕边界层；无字幕、文件缺失或解析失败时返回空。</summary>
    public Task<IReadOnlyList<SentenceBoundaryLayer>> GetLayersAsync(
        AudioTrack track,
        int durationMs,
        CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var cues = Normalize(LoadCues(track), durationMs);
            if (cues.Count == 0)
                return (IReadOnlyList<SentenceBoundaryLayer>)Array.Empty<SentenceBoundaryLayer>();

            var spans = new List<AudioSentenceSpan>(cues.Count);
            for (int i = 0; i < cues.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var cue = cues[i];
                spans.Add(new AudioSentenceSpan
                {
                    StartMs = (int)Math.Round(cue.StartTime.TotalMilliseconds),
                    EndMs = (int)Math.Round(cue.EndTime.TotalMilliseconds),
                    BoundarySource = Source,
                    Confidence = 0.95,
                    Text = cue.Text,
                    TextSource = SubtitleTextOverlay.TextSource
                });
            }

            return new[]
            {
                new SentenceBoundaryLayer(Source, SentenceBoundaryResolver.SubtitlePriority, spans)
            };
        }, cancellationToken);
    }

    /// <summary>把字幕文本叠加到 VAD / 切片产生的句子上，不覆盖已有文本。</summary>
    public Task<IReadOnlyList<AudioSentenceSpan>> EnrichAsync(
        AudioTrack track,
        int durationMs,
        IReadOnlyList<AudioSentenceSpan> spans,
        CancellationToken cancellationToken)
    {
        return Task.Run(
            () => SubtitleTextOverlay.Apply(spans, Normalize(LoadCues(track), durationMs)),
            cancellationToken);
    }

    /// <summary>
    /// 字幕文件签名（路径 + 修改时间 + 大小）；拼进时间轴去重键，
    /// 用户改了字幕就能触发重新合成，而 VAD 缓存仍然命中。
    /// </summary>
    public string? GetSignature(AudioTrack track)
    {
        string? path = track?.SubtitlePath;
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var info = new FileInfo(path);
            if (!info.Exists) return path + "|missing";
            return $"{path}|{info.LastWriteTimeUtc.Ticks}|{info.Length}";
        }
        catch (Exception)
        {
            return path + "|error";
        }
    }

    private IReadOnlyList<SubtitleCue> LoadCues(AudioTrack track)
    {
        string? path = track?.SubtitlePath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return Array.Empty<SubtitleCue>();

        long ticks;
        long length;
        try
        {
            var info = new FileInfo(path);
            ticks = info.LastWriteTimeUtc.Ticks;
            length = info.Length;
        }
        catch (Exception)
        {
            return Array.Empty<SubtitleCue>();
        }

        lock (_sync)
        {
            var cached = _cache.TryGetValue(path, out var entry) ? entry : null;
            if (cached != null && cached.Ticks == ticks && cached.Length == length)
                return cached.Cues;

            IReadOnlyList<SubtitleCue> cues;
            try
            {
                cues = _parser.Parse(path);
            }
            catch (Exception)
            {
                cues = Array.Empty<SubtitleCue>();
            }

            _cache[path] = new CachedSubtitle(length, ticks, cues);
            return cues;
        }
    }

    /// <summary>把字幕裁剪到 [0,duration] 并补齐缺失的结束时间（LRC 末条、坏 SRT）。</summary>
    private static IReadOnlyList<SubtitleCue> Normalize(IReadOnlyList<SubtitleCue> cues, int durationMs)
    {
        if (cues == null || cues.Count == 0)
            return Array.Empty<SubtitleCue>();

        var ordered = cues
            .Where(cue => !string.IsNullOrWhiteSpace(cue.Text))
            .OrderBy(cue => cue.StartTime)
            .ToList();

        var result = new List<SubtitleCue>(ordered.Count);
        for (int i = 0; i < ordered.Count; i++)
        {
            var cue = ordered[i];
            int startMs = (int)Math.Round(cue.StartTime.TotalMilliseconds);
            if (durationMs > 0 && startMs >= durationMs) break;
            if (startMs < 0) startMs = 0;

            int endMs;
            if (cue.EndTime > cue.StartTime)
            {
                endMs = (int)Math.Round(cue.EndTime.TotalMilliseconds);
            }
            else if (i + 1 < ordered.Count && ordered[i + 1].StartTime > cue.StartTime)
            {
                endMs = (int)Math.Round(ordered[i + 1].StartTime.TotalMilliseconds);
            }
            else
            {
                endMs = startMs + DefaultCueDurationMs;
            }

            if (durationMs > 0) endMs = Math.Min(endMs, durationMs);
            if (endMs <= startMs) continue;

            result.Add(new SubtitleCue
            {
                Index = result.Count + 1,
                StartTime = TimeSpan.FromMilliseconds(startMs),
                EndTime = TimeSpan.FromMilliseconds(endMs),
                Text = cue.Text.Trim()
            });
        }
        return result;
    }

    private sealed record CachedSubtitle(long Length, long Ticks, IReadOnlyList<SubtitleCue> Cues);
}
