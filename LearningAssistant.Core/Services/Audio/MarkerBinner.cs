using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>一个可视列的抽稀结果：只保留代表句（该列第一句）与同列句数。</summary>
public readonly record struct MarkerBucket(int FirstIndex, int Count)
{
    /// <summary>空桶：该列没有句首。</summary>
    public static readonly MarkerBucket Empty = new(-1, 0);

    public bool IsEmpty => FirstIndex < 0;
}

/// <summary>
/// 句子刻度抽稀：按可视像素列分桶，每列只保留代表句，避免整本 12000 句时逐句绘制。
/// 命中测试仍用全量二分，抽稀只影响绘制。
/// </summary>
public static class MarkerBinner
{
    /// <summary>按列宽与总时长分桶；句子恰好落在右端外时由「末句强制保留」补画。</summary>
    public static MarkerBucket[] Build(IReadOnlyList<AudioSentenceSpan>? sentences, int columns, int durationMs)
    {
        if (columns <= 0) return Array.Empty<MarkerBucket>();

        var buckets = new MarkerBucket[columns];
        for (int i = 0; i < columns; i++)
            buckets[i] = MarkerBucket.Empty;

        if (sentences == null || sentences.Count == 0 || durationMs <= 0)
            return buckets;

        for (int i = 0; i < sentences.Count; i++)
        {
            int column = ColumnOf(sentences[i].StartMs, columns, durationMs);
            if (column < 0) continue;

            var bucket = buckets[column];
            buckets[column] = bucket.IsEmpty
                ? new MarkerBucket(i, 1)
                : new MarkerBucket(bucket.FirstIndex, bucket.Count + 1);
        }

        return buckets;
    }

    /// <summary>抽稀后仍必须绘制的句：首句、末句、当前句与悬停句。</summary>
    public static int[] ForcedIndices(int count, int currentIndex, int hoverIndex)
    {
        if (count <= 0) return Array.Empty<int>();

        var indices = new List<int>(4) { 0 };
        if (count > 1) indices.Add(count - 1);
        AddIfMissing(indices, currentIndex, count);
        AddIfMissing(indices, hoverIndex, count);
        return indices.ToArray();
    }

    private static void AddIfMissing(List<int> indices, int index, int count)
    {
        if (index < 0 || index >= count || indices.Contains(index)) return;
        indices.Add(index);
    }

    /// <summary>毫秒 → 列；与轨道绘制用的 round(ratio * width) 保持一致。</summary>
    public static int ColumnOf(int startMs, int columns, int durationMs)
    {
        if (columns <= 0 || durationMs <= 0) return -1;

        double ratio = Math.Clamp(startMs / (double)durationMs, 0d, 1d);
        int column = (int)Math.Round(ratio * columns);
        return column >= columns ? -1 : column;
    }
}
