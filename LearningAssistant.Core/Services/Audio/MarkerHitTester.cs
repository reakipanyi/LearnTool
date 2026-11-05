using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 轨道的「最近句」命中：落在句子内部直接命中；空白处取前后句里更近的一侧。
/// 纯函数，供 UI 命中测试与单测复用。
/// </summary>
public static class MarkerHitTester
{
    public static AudioSentenceSpan? Nearest(IReadOnlyList<AudioSentenceSpan>? sentences, int positionMs)
    {
        if (sentences == null || sentences.Count == 0) return null;

        int started = LastStartedAt(sentences, positionMs);
        if (started >= 0)
        {
            var current = sentences[started];
            if (positionMs < current.EndMs) return current;
        }

        int next = started + 1;
        if (started < 0) return sentences[0];
        if (next >= sentences.Count) return sentences[started];

        int prevGap = positionMs - sentences[started].EndMs;
        int nextGap = sentences[next].StartMs - positionMs;
        return prevGap <= nextGap ? sentences[started] : sentences[next];
    }

    /// <summary>返回最后一个 StartMs ≤ positionMs 的下标；全部都在后面时返回 -1。</summary>
    private static int LastStartedAt(IReadOnlyList<AudioSentenceSpan> sentences, int positionMs)
    {
        int low = 0;
        int high = sentences.Count - 1;
        int result = -1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (sentences[mid].StartMs <= positionMs)
            {
                result = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return result;
    }
}
