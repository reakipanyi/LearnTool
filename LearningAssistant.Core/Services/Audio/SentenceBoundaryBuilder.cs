using LearningAssistant.Models.Audio;
using LearningAssistant.Services.Shadowing;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 纯函数断句后处理：粗遍定句 → 超长切分 → 短句归并 → 补白 → 消重叠 → 兜底 → 自评。
/// 只产出 VAD 来源，切片/字幕/手动来源由 SentenceBoundaryResolver 合并时写入。
/// </summary>
public static class SentenceBoundaryBuilder
{
    /// <summary>
    /// 由粗/细两遍 VAD 段生成句子时间轴。
    /// 粗遍用 SentenceGapMs 定句，细遍只用于切超长段；padding 统一在最后加。
    /// </summary>
    public static IReadOnlyList<AudioSentenceSpan> Build(
        IReadOnlyList<VadSegment> coarse,
        IReadOnlyList<VadSegment> fine,
        int durationMs,
        AudioTimelineOptions options)
    {
        options ??= new AudioTimelineOptions();
        if (durationMs <= 0)
            return Array.Empty<AudioSentenceSpan>();

        var coarseSpans = Normalize(coarse, durationMs);
        coarseSpans = MergeByGap(coarseSpans, options.SentenceGapMs);
        var fineSpans = Normalize(fine, durationMs);

        var pieces = new List<Piece>();
        foreach (var segment in coarseSpans)
            pieces.AddRange(SplitLong(segment.StartMs, segment.EndMs, fineSpans, options));

        MergeTooShort(pieces, options, enforceMax: false);

        ApplyPadding(pieces, durationMs, options.PaddingMs);
        DeOverlap(pieces);
        MergeTooShort(pieces, options, enforceMax: true);
        ApplySelfEvaluation(pieces);

        if (pieces.Count <= 1)
        {
            var fallback = new AudioSentenceSpan
            {
                StartMs = 0,
                EndMs = durationMs,
                BoundarySource = "vad-fallback",
                Confidence = 0.4
            };
            fallback.Index = 1;
            return new[] { fallback };
        }

        pieces.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
        var result = new List<AudioSentenceSpan>(pieces.Count);
        for (int i = 0; i < pieces.Count; i++)
        {
            var piece = pieces[i];
            result.Add(new AudioSentenceSpan
            {
                Index = i + 1,
                StartMs = piece.StartMs,
                EndMs = piece.EndMs,
                BoundarySource = piece.Source,
                Confidence = piece.Confidence
            });
        }
        return result;
    }

    #region 规范化与合并

    private static List<VadSegment> Normalize(IReadOnlyList<VadSegment>? segments, int durationMs)
    {
        var result = new List<VadSegment>();
        if (segments == null) return result;

        foreach (var segment in segments)
        {
            int start = Math.Max(0, segment.StartMs);
            int end = Math.Min(durationMs, segment.EndMs);
            if (end <= start) continue;
            result.Add(new VadSegment(start, end));
        }

        result.Sort((a, b) =>
        {
            int byStart = a.StartMs.CompareTo(b.StartMs);
            return byStart != 0 ? byStart : a.EndMs.CompareTo(b.EndMs);
        });

        var merged = new List<VadSegment>(result.Count);
        foreach (var segment in result)
        {
            if (merged.Count > 0 && segment.StartMs <= merged[^1].EndMs)
            {
                var last = merged[^1];
                merged[^1] = new VadSegment(last.StartMs, Math.Max(last.EndMs, segment.EndMs));
            }
            else
            {
                merged.Add(segment);
            }
        }
        return merged;
    }

    /// <summary>粗遍段之间小于句间阈值的间隔视为同一句（防止调用方传入未被粗遍合并的段）。</summary>
    private static List<VadSegment> MergeByGap(List<VadSegment> segments, int sentenceGapMs)
    {
        if (segments.Count <= 1 || sentenceGapMs <= 0) return segments;

        var merged = new List<VadSegment>(segments.Count);
        foreach (var segment in segments)
        {
            if (merged.Count > 0 && segment.StartMs - merged[^1].EndMs < sentenceGapMs)
            {
                var last = merged[^1];
                merged[^1] = new VadSegment(last.StartMs, Math.Max(last.EndMs, segment.EndMs));
            }
            else
            {
                merged.Add(segment);
            }
        }
        return merged;
    }

    #endregion

    #region 超长切分

    private sealed class Piece
    {
        public int StartMs;
        public int EndMs;
        public double Confidence;
        public string Source;

        public Piece(int startMs, int endMs, double confidence, string source)
        {
            StartMs = startMs;
            EndMs = endMs;
            Confidence = confidence;
            Source = source;
        }
    }

    /// <summary>超长段用内部最长的细遍间隔反复切分；无合法边界时在 MaxSentenceMs 处硬切。</summary>
    private static IEnumerable<Piece> SplitLong(
        int startMs,
        int endMs,
        List<VadSegment> fine,
        AudioTimelineOptions options)
    {
        if (endMs <= startMs)
            yield break;

        int maxMs = options.MaxSentenceMs;
        int minMs = Math.Max(1, options.MinSentenceMs);
        if (maxMs <= 0 || endMs - startMs <= maxMs)
        {
            yield return new Piece(startMs, endMs, 1.0, "vad");
            yield break;
        }

        int bestGapLength = -1;
        int bestSplit = -1;
        for (int i = 0; i < fine.Count - 1; i++)
        {
            int gapStart = fine[i].EndMs;
            int gapEnd = fine[i + 1].StartMs;
            if (gapEnd <= gapStart) continue;
            if (gapStart < startMs || gapEnd > endMs) continue;

            int split = (int)(((long)gapStart + gapEnd) / 2);
            if (split - startMs < minMs || endMs - split < minMs) continue;

            int length = gapEnd - gapStart;
            if (length > bestGapLength)
            {
                bestGapLength = length;
                bestSplit = split;
            }
        }

        if (bestSplit >= 0)
        {
            foreach (var piece in SplitLong(startMs, bestSplit, fine, options))
                yield return piece;
            foreach (var piece in SplitLong(bestSplit, endMs, fine, options))
                yield return piece;
            yield break;
        }

        int hardCut = startMs + maxMs;
        if (hardCut >= endMs)
        {
            yield return new Piece(startMs, endMs, 1.0, "vad");
            yield break;
        }

        yield return new Piece(startMs, hardCut, 0.9, "vad-hard");
        foreach (var piece in SplitLong(hardCut, endMs, fine, options))
            yield return piece;
    }

    #endregion

    #region 短句归并与消重叠

    private static void MergeTooShort(List<Piece> pieces, AudioTimelineOptions options, bool enforceMax)
    {
        if (pieces.Count <= 1) return;

        int minSentence = Math.Max(1, options.MinSentenceMs);
        int minSpeech = Math.Max(1, options.MinSpeechMs);
        double largeGap = options.SentenceGapMs * 2.0;

        int index = 0;
        while (index < pieces.Count && pieces.Count > 1)
        {
            var piece = pieces[index];
            if (piece.EndMs - piece.StartMs >= minSentence)
            {
                index++;
                continue;
            }

            bool hasPrev = index > 0;
            bool hasNext = index + 1 < pieces.Count;
            int prevGap = hasPrev ? piece.StartMs - pieces[index - 1].EndMs : int.MaxValue;
            int nextGap = hasNext ? pieces[index + 1].StartMs - piece.EndMs : int.MaxValue;

            // 前后都是大停顿且本身保留最短语音 → 允许独立短句（如 "Yes."）
            if (piece.EndMs - piece.StartMs >= minSpeech
                && prevGap >= largeGap && nextGap >= largeGap)
            {
                index++;
                continue;
            }

            if (!hasPrev && !hasNext)
            {
                index++;
                continue;
            }

            int targetIndex = hasPrev && (!hasNext || prevGap <= nextGap) ? index - 1 : index + 1;
            var target = pieces[targetIndex];
            int mergedStart = Math.Min(piece.StartMs, target.StartMs);
            int mergedEnd = Math.Max(piece.EndMs, target.EndMs);

            if (enforceMax && options.MaxSentenceMs > 0
                && mergedEnd - mergedStart > options.MaxSentenceMs)
            {
                // 合并会超过上限：保留短句，仅降置信度
                piece.Confidence = Math.Max(0, piece.Confidence - 0.1);
                index++;
                continue;
            }

            target.StartMs = mergedStart;
            target.EndMs = mergedEnd;
            target.Confidence = Math.Min(target.Confidence, piece.Confidence);
            if (string.Equals(piece.Source, "vad-hard", StringComparison.Ordinal))
                target.Source = "vad-hard";

            pieces.RemoveAt(index);
            // 并入前段时检查前段合并后的结果；并入后段时后段已前移到当前下标
            index = Math.Max(0, targetIndex < index ? targetIndex : index);
        }
    }

    private static void ApplyPadding(List<Piece> pieces, int durationMs, int paddingMs)
    {
        if (paddingMs <= 0) return;

        foreach (var piece in pieces)
        {
            piece.StartMs = Math.Max(0, piece.StartMs - paddingMs);
            piece.EndMs = Math.Min(durationMs, piece.EndMs + paddingMs);
        }
        pieces.Sort((a, b) => a.StartMs.CompareTo(b.StartMs));
    }

    private static void DeOverlap(List<Piece> pieces)
    {
        for (int i = 1; i < pieces.Count; i++)
        {
            var prev = pieces[i - 1];
            var current = pieces[i];
            if (prev.EndMs <= current.StartMs) continue;

            int mid = (int)(((long)prev.EndMs + current.StartMs) / 2);
            prev.EndMs = mid;
            current.StartMs = mid;
        }
    }

    #endregion

    #region 自评

    /// <summary>
    /// 精听目标 3–8 秒：短句过多、超长句过多或中位数过长时降级。
    /// 用“置信度上限”而不是每次乘系数，保证重复 Build 结果稳定。
    /// </summary>
    private static void ApplySelfEvaluation(List<Piece> pieces)
    {
        int count = pieces.Count;
        if (count == 0) return;

        int shortCount = 0;
        int longCount = 0;
        foreach (var piece in pieces)
        {
            int duration = piece.EndMs - piece.StartMs;
            if (duration < 1000) shortCount++;
            if (duration > 8000) longCount++;
        }

        var durations = new int[count];
        for (int i = 0; i < count; i++)
            durations[i] = pieces[i].EndMs - pieces[i].StartMs;
        Array.Sort(durations);
        int median = durations[count / 2];

        if ((double)shortCount / count > 0.4
            || (double)longCount / count > 0.3
            || median > 10000)
        {
            foreach (var piece in pieces)
                piece.Confidence = Math.Min(piece.Confidence, 0.8);
        }
    }

    #endregion
}
