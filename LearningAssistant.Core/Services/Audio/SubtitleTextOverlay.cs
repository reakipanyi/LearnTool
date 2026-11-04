using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 字幕文本叠加：把字幕文本补到已解析的句子区间上，只填空文本、不覆盖切片文本。
/// 一句内最多拼接 <see cref="MaxLines"/> 条字幕，避免长句把整段字幕塞进悬停提示。
/// 只做纯时间运算，不依赖 UI 与音频解码。
/// </summary>
public static class SubtitleTextOverlay
{
    public const string TextSource = "subtitle";

    public const int MaxLines = 2;

    /// <summary>
    /// 返回叠加文本后的句子列表；已有 Text 的句子保持原样（切片文本优先于字幕）。
    /// </summary>
    public static IReadOnlyList<AudioSentenceSpan> Apply(
        IReadOnlyList<AudioSentenceSpan> spans,
        IReadOnlyList<SubtitleCue>? cues,
        int maxLines = MaxLines)
    {
        if (spans == null || spans.Count == 0 || cues == null || cues.Count == 0)
            return spans ?? Array.Empty<AudioSentenceSpan>();

        maxLines = Math.Max(1, maxLines);
        var orderedCues = cues
            .Where(cue => !string.IsNullOrWhiteSpace(cue.Text))
            .Select(cue => new CueRange(
                (int)Math.Round(cue.StartTime.TotalMilliseconds),
                (int)Math.Round(cue.EndTime.TotalMilliseconds),
                cue.Text.Trim()))
            .Where(cue => cue.EndMs > cue.StartMs)
            .OrderBy(cue => cue.StartMs)
            .ToList();

        if (orderedCues.Count == 0)
            return spans;

        var result = new List<AudioSentenceSpan>(spans.Count);
        foreach (var span in spans)
        {
            if (!string.IsNullOrWhiteSpace(span.Text) || orderedCues.Count == 0)
            {
                result.Add(span);
                continue;
            }

            var texts = new List<string>(maxLines);
            foreach (var cue in orderedCues)
            {
                if (cue.StartMs >= span.EndMs) break;
                if (cue.EndMs <= span.StartMs) continue;
                if (!Qualifies(span, cue)) continue;
                if (texts.Any(text => string.Equals(text, cue.Text, StringComparison.Ordinal))) continue;

                texts.Add(cue.Text);
                if (texts.Count >= maxLines) break;
            }

            result.Add(texts.Count == 0 ? span : WithText(span, string.Join(" ", texts)));
        }
        return result;
    }

    /// <summary>
    /// 字幕中点在句内，或字幕几乎覆盖整句，才认为这句在念这条字幕。
    /// 用中点而不是起点，避免把跨句字幕（多为下一句开头）补到前一句上。
    /// </summary>
    private static bool Qualifies(AudioSentenceSpan span, CueRange cue)
    {
        int overlap = Math.Min(span.EndMs, cue.EndMs) - Math.Max(span.StartMs, cue.StartMs);
        if (overlap <= 0) return false;

        int midpoint = (int)(((long)cue.StartMs + cue.EndMs) / 2);
        if (midpoint >= span.StartMs && midpoint < span.EndMs) return true;

        int spanLength = span.EndMs - span.StartMs;
        return spanLength > 0 && overlap >= spanLength * 0.8;
    }

    private static AudioSentenceSpan WithText(AudioSentenceSpan span, string text)
        => new()
        {
            Index = span.Index,
            StartMs = span.StartMs,
            EndMs = span.EndMs,
            PageIndex = span.PageIndex,
            BoundarySource = span.BoundarySource,
            Confidence = span.Confidence,
            SegmentId = span.SegmentId,
            HighlightId = span.HighlightId,
            Text = text,
            TextSource = TextSource
        };

    private readonly record struct CueRange(int StartMs, int EndMs, string Text);
}
