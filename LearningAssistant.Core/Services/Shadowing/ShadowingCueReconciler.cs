using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    public enum CueBindingStatus
    {
        /// <summary>锚点命中，且切片文件存在。</summary>
        Matched,

        /// <summary>锚点命中，但 SlicePath 为空或切片文件缺失。</summary>
        MissingSlice,

        /// <summary>没有任何记录命中该 cue。</summary>
        Unbound,

        /// <summary>命中多条记录，取优先级最高的一条。</summary>
        Ambiguous
    }

    public enum CueUnboundReason
    {
        /// <summary>该页尚未开始/完成对齐。</summary>
        NotAlignedYet,

        /// <summary>已对齐但该句被跳过。</summary>
        Skipped
    }

    public sealed record CueBinding(
        ShadowingCue Cue,
        ShadowingSegment? Segment,
        CueBindingStatus Status,
        int CandidateCount,
        CueUnboundReason? UnboundReason);

    public sealed class ReconcileResult
    {
        /// <summary>与 cues 等长、同序。</summary>
        public IReadOnlyList<CueBinding> Bindings { get; init; } = Array.Empty<CueBinding>();

        /// <summary>未被任何 cue 命中的切片；不自动删除，仅上报。</summary>
        public IReadOnlyList<ShadowingSegment> Orphans { get; init; } = Array.Empty<ShadowingSegment>();

        /// <summary>同一 cue 命中的额外记录。</summary>
        public IReadOnlyList<ShadowingSegment> Duplicates { get; init; } = Array.Empty<ShadowingSegment>();

        public bool HasDrift => Orphans.Count > 0 || Duplicates.Count > 0
            || Bindings.Any(b => b.Status is CueBindingStatus.MissingSlice or CueBindingStatus.Ambiguous);

        public int MatchedCount => Bindings.Count(b => b.Status is CueBindingStatus.Matched or CueBindingStatus.Ambiguous);
        public int UnboundCount => Bindings.Count(b => b.Status == CueBindingStatus.Unbound);
    }

    /// <summary>
    /// 高亮列表 ↔ 原声切片的纯函数对账器：分阶段全局匹配（I3），O(n) 字典索引，
    /// 不读文件、不写文件、不改入参（切片存在性由 sliceExists 注入）。
    /// </summary>
    public static class ShadowingCueReconciler
    {
        /// <summary>
        /// 分阶段全局对账；sliceExists 由调用方注入（默认 true），便于单测。
        /// alignedOnce 表示该页是否已完成过对齐（区分“已跳过”与“未对齐”）。
        /// </summary>
        public static ReconcileResult Reconcile(
            IReadOnlyList<ShadowingCue> cues,
            IReadOnlyList<ShadowingSegment> segments,
            Func<ShadowingSegment, bool>? sliceExists = null,
            bool alignedOnce = false)
        {
            cues ??= Array.Empty<ShadowingCue>();
            segments ??= Array.Empty<ShadowingSegment>();
            sliceExists ??= static _ => true;

            bool SliceUsable(ShadowingSegment segment)
            {
                if (string.IsNullOrWhiteSpace(segment.SlicePath))
                    return false;
                try
                {
                    return sliceExists(segment);
                }
                catch
                {
                    return false;
                }
            }

            // 三个字典索引：anchor → list、(highlightId, part) → list、规范化文本 → list
            var byAnchor = new Dictionary<string, List<ShadowingSegment>>(StringComparer.Ordinal);
            var byHighlight = new Dictionary<(string Id, int Part), List<ShadowingSegment>>();
            var byText = new Dictionary<string, List<ShadowingSegment>>(StringComparer.Ordinal);
            foreach (var segment in segments)
            {
                if (!string.IsNullOrWhiteSpace(segment.HighlightAnchor))
                {
                    if (!byAnchor.TryGetValue(segment.HighlightAnchor, out var list))
                        byAnchor[segment.HighlightAnchor] = list = new List<ShadowingSegment>();
                    list.Add(segment);
                }

                if (!string.IsNullOrWhiteSpace(segment.HighlightId))
                {
                    var key = (segment.HighlightId, segment.CuePartIndex);
                    if (!byHighlight.TryGetValue(key, out var list))
                        byHighlight[key] = list = new List<ShadowingSegment>();
                    list.Add(segment);
                }

                string normalized = ShadowingAnchorKeys.NormalizeKey(segment.Text);
                if (normalized.Length > 0)
                {
                    if (!byText.TryGetValue(normalized, out var list))
                        byText[normalized] = list = new List<ShadowingSegment>();
                    list.Add(segment);
                }
            }

            var bound = new ShadowingSegment?[cues.Count];
            var candidateCounts = new int[cues.Count];
            var used = new HashSet<string>(StringComparer.Ordinal);
            var duplicates = new List<ShadowingSegment>();

            // P1–P3 分阶段全局匹配：先让所有 cue 走最高优先级，再对未绑定 cue 降级，
            // 禁止逐 cue 一路降级导致前面的句子抢占后面句子的精确命中
            for (int stage = 1; stage <= 3; stage++)
            {
                for (int i = 0; i < cues.Count; i++)
                {
                    if (bound[i] != null)
                        continue;

                    List<ShadowingSegment>? candidates = stage switch
                    {
                        1 => string.IsNullOrWhiteSpace(cues[i].AnchorKey) ? null
                            : byAnchor.GetValueOrDefault(cues[i].AnchorKey),
                        2 => string.IsNullOrWhiteSpace(cues[i].LegacyAnchorKey) ? null
                            : byAnchor.GetValueOrDefault(cues[i].LegacyAnchorKey!),
                        _ => string.IsNullOrWhiteSpace(cues[i].HighlightId) ? null
                            : byHighlight.GetValueOrDefault((cues[i].HighlightId!, cues[i].PartIndex))
                    };
                    if (candidates == null)
                        continue;

                    var available = candidates.Where(s => !used.Contains(s.Id)).ToList();
                    if (available.Count == 0)
                        continue;

                    available.Sort(ComparePrimary);
                    bound[i] = available[0];
                    candidateCounts[i] = available.Count;
                    used.Add(available[0].Id);
                    duplicates.AddRange(available.Skip(1));
                }
            }

            // P4 文本兜底：segment 没有“同文本序号”字段，无法逐对比较，改为分组分配——
            // (规范化文本, PartIndex) 相同的未绑定 cue（按句序）与未绑定且无锚点的 segment（按 StartMs）按位置配对
            var pendingCues = Enumerable.Range(0, cues.Count)
                .Where(i => bound[i] == null)
                .Select(i => (Index: i, Key: (Text: ShadowingAnchorKeys.NormalizeKey(cues[i].Text), cues[i].PartIndex)))
                .Where(x => x.Key.Text.Length > 0)
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => x.Select(y => y.Index).ToList());
            if (pendingCues.Count > 0)
            {
                var pendingSegments = segments
                    .Where(s => !used.Contains(s.Id) && string.IsNullOrWhiteSpace(s.HighlightAnchor))
                    .Select(s => (Segment: s, Key: (Text: ShadowingAnchorKeys.NormalizeKey(s.Text), s.CuePartIndex)))
                    .Where(x => x.Key.Text.Length > 0)
                    .GroupBy(x => x.Key)
                    .ToDictionary(
                        x => x.Key,
                        x => x.Select(y => y.Segment)
                              .OrderBy(s => s.StartMs)
                              .ThenBy(s => s.Id, StringComparer.Ordinal)
                              .ToList());

                foreach (var (key, cueIndexes) in pendingCues)
                {
                    if (!pendingSegments.TryGetValue(key, out var peers))
                        continue;

                    int pairCount = Math.Min(cueIndexes.Count, peers.Count);
                    for (int k = 0; k < pairCount; k++)
                    {
                        bound[cueIndexes[k]] = peers[k];
                        // 按位置一一配对无歧义；多余 segment 不进 Duplicates，统一进 Orphans
                        candidateCounts[cueIndexes[k]] = 1;
                        used.Add(peers[k].Id);
                    }
                    // 多余 segment 不进 Duplicates，随未使用记录统一进 Orphans
                }
            }

            var unboundReason = segments.Count == 0
                ? CueUnboundReason.NotAlignedYet
                : alignedOnce ? CueUnboundReason.Skipped : CueUnboundReason.NotAlignedYet;
            var bindings = new List<CueBinding>(cues.Count);
            for (int i = 0; i < cues.Count; i++)
            {
                var segment = bound[i];
                if (segment == null)
                {
                    bindings.Add(new CueBinding(cues[i], null, CueBindingStatus.Unbound, 0, unboundReason));
                    continue;
                }

                var status = SliceUsable(segment)
                    ? candidateCounts[i] > 1 ? CueBindingStatus.Ambiguous : CueBindingStatus.Matched
                    : CueBindingStatus.MissingSlice;
                bindings.Add(new CueBinding(cues[i], segment, status, candidateCounts[i], null));
            }

            return new ReconcileResult
            {
                Bindings = bindings,
                Orphans = segments
                    .Where(s => !used.Contains(s.Id))
                    .OrderBy(s => s.StartMs)
                    .ThenBy(s => s.Id, StringComparer.Ordinal)
                    .ToList(),
                Duplicates = duplicates
            };
        }

        /// <summary>
        /// 供 FindExisting 复用；只覆盖 P1–P3（逐对可判），P4 文本分组分配由 Reconcile 内部完成。
        /// 返回 0 表示不匹配，1 为最高优先级。
        /// </summary>
        public static int MatchPriority(ShadowingCue cue, ShadowingSegment segment)
        {
            if (!string.IsNullOrWhiteSpace(cue.AnchorKey)
                && string.Equals(segment.HighlightAnchor, cue.AnchorKey, StringComparison.Ordinal))
                return 1;

            if (!string.IsNullOrWhiteSpace(cue.LegacyAnchorKey)
                && string.Equals(segment.HighlightAnchor, cue.LegacyAnchorKey, StringComparison.Ordinal))
                return 2;

            if (!string.IsNullOrWhiteSpace(cue.HighlightId)
                && string.Equals(segment.HighlightId, cue.HighlightId, StringComparison.Ordinal)
                && segment.CuePartIndex == cue.PartIndex)
                return 3;

            return 0;
        }

        /// <summary>
        /// 「重新对齐此句」的起点：该句前一条已绑定切片的 EndMs，无前序时为 0。
        /// </summary>
        public static int FindRestartStartMs(IReadOnlyList<CueBinding> bindings, int cueIndex)
        {
            for (int i = Math.Min(cueIndex, bindings.Count) - 1; i >= 0; i--)
            {
                var segment = bindings[i].Segment;
                if (segment != null)
                    return segment.EndMs;
            }

            return 0;
        }

        // 主候选排序：SlicePath 非空 > 有 Takes > Id 字典序（保证置换不变性）
        private static int ComparePrimary(ShadowingSegment a, ShadowingSegment b)
        {
            int scoreA = (string.IsNullOrWhiteSpace(a.SlicePath) ? 0 : 2) + (a.Takes?.Count > 0 ? 1 : 0);
            int scoreB = (string.IsNullOrWhiteSpace(b.SlicePath) ? 0 : 2) + (b.Takes?.Count > 0 ? 1 : 0);
            int cmp = scoreB.CompareTo(scoreA);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
