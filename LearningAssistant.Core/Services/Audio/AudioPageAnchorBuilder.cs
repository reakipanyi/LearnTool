using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 整本音频的「页 → 时间」锚点。只接受有可信来源的页（跟读切片/手动），绝不按页数均分；
/// 插值层仅在显式开启时生成，且置信度低于 0.5。
/// </summary>
public static class AudioPageAnchorBuilder
{
    public const string ShadowingSource = "shadowing";
    public const string ManualSource = "manual";
    public const string InterpolatedSource = "interpolated";

    /// <summary>
    /// 生成页码锚点。无来源的页不生成；相邻页锚点倒序时丢弃后者。
    /// </summary>
    /// <param name="durationMs">曲目时长，用于裁剪越界锚点。</param>
    /// <param name="shadowing">带 PageIndex 的切片句区间（只取每页最早句）。</param>
    /// <param name="manual">手动页码锚点，优先于切片。</param>
    /// <param name="options">留白参数。</param>
    /// <param name="interpolateMissing">是否生成推测层；默认关闭。</param>
    public static IReadOnlyList<AudioPageAnchor> Build(
        int durationMs,
        IReadOnlyList<AudioSentenceSpan>? shadowing,
        IReadOnlyList<AudioPageAnchor>? manual,
        AudioTimelineOptions? options,
        bool interpolateMissing = false)
    {
        if (durationMs <= 0) return Array.Empty<AudioPageAnchor>();

        int paddingMs = Math.Max(0, options?.PaddingMs ?? 100);
        var byPage = new Dictionary<int, AudioPageAnchor>();

        if (shadowing != null)
        {
            foreach (var span in shadowing)
            {
                if (span.PageIndex is not int page || page < 0) continue;
                if (byPage.TryGetValue(page, out var existing) && existing.StartMs <= span.StartMs) continue;

                byPage[page] = new AudioPageAnchor
                {
                    PageIndex = page,
                    StartMs = Math.Max(0, span.StartMs - paddingMs),
                    Source = ShadowingSource,
                    // 切片从本页第 2 句才开始打点时，页首会被漏掉；减留白属于估算
                    IsStartEstimated = span.StartMs > 0,
                    Confidence = Math.Clamp(span.Confidence, 0.1, 1.0)
                };
            }
        }

        if (manual != null)
        {
            foreach (var anchor in manual)
            {
                if (anchor.PageIndex < 0) continue;
                byPage[anchor.PageIndex] = new AudioPageAnchor
                {
                    PageIndex = anchor.PageIndex,
                    StartMs = anchor.StartMs,
                    Source = ManualSource,
                    IsStartEstimated = false,
                    Confidence = 1.0
                };
            }
        }

        if (byPage.Count == 0) return Array.Empty<AudioPageAnchor>();

        var ordered = byPage.Values
            .Where(a => a.StartMs >= 0 && a.StartMs <= durationMs)
            .OrderBy(a => a.PageIndex)
            .ToList();

        // 相邻页锚点必须严格递增；倒序时保留前一个，丢弃后者
        var result = new List<AudioPageAnchor>(ordered.Count);
        foreach (var anchor in ordered)
        {
            if (result.Count > 0 && anchor.StartMs <= result[^1].StartMs) continue;
            result.Add(anchor);
        }

        if (interpolateMissing)
            result = InsertInterpolated(result, durationMs);

        return result;
    }

    private static List<AudioPageAnchor> InsertInterpolated(List<AudioPageAnchor> anchors, int durationMs)
    {
        if (anchors.Count < 2) return anchors;

        var merged = new List<AudioPageAnchor>(anchors.Count);
        for (int i = 0; i < anchors.Count - 1; i++)
        {
            var current = anchors[i];
            var next = anchors[i + 1];
            merged.Add(current);

            int pageGap = next.PageIndex - current.PageIndex;
            if (pageGap <= 1) continue;

            int timeGap = next.StartMs - current.StartMs;
            if (timeGap <= 0) continue;

            for (int step = 1; step < pageGap; step++)
            {
                int startMs = current.StartMs + (int)Math.Round(timeGap * (step / (double)pageGap));
                if (startMs <= current.StartMs || startMs >= next.StartMs) continue;

                merged.Add(new AudioPageAnchor
                {
                    PageIndex = current.PageIndex + step,
                    StartMs = Math.Clamp(startMs, 0, durationMs),
                    Source = InterpolatedSource,
                    IsStartEstimated = true,
                    Confidence = 0.4
                });
            }
        }

        merged.Add(anchors[^1]);
        return merged;
    }
}
