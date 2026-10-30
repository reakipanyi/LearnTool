using LearningAssistant.Models.Shadowing;

namespace LearningAssistant.Services.Shadowing
{
    /// <summary>
    /// 本页原声对齐状态机：按跟读句顺序接收播放器位置，用空格打点生成时间范围。
    /// 不直接读音频，也不写磁盘，便于单元测试和失败回滚。
    /// </summary>
    public sealed class ShadowingAlignmentSession
    {
        /// <summary>与 AudioSliceService 的最短切片长度保持一致，避免产生无法播放的碎片。</summary>
        public const int MinimumSegmentMs = 300;

        private readonly int _pageIndex;
        private readonly IReadOnlyList<ShadowingCue> _cues;
        private readonly IReadOnlyList<ShadowingSegment> _existingSegments;
        private readonly List<ShadowingSegment> _createdSegments = new();
        private readonly List<int> _createdCueIndexes = new();

        private int _cursorStartMs;
        private bool _started;

        public ShadowingAlignmentSession(
            int pageIndex,
            IReadOnlyList<ShadowingCue> cues,
            IReadOnlyList<ShadowingSegment>? existingSegments = null)
        {
            _pageIndex = pageIndex;
            _cues = cues ?? Array.Empty<ShadowingCue>();
            _existingSegments = existingSegments ?? Array.Empty<ShadowingSegment>();
        }

        public int CurrentIndex { get; private set; }

        public int CursorStartMs => _cursorStartMs;

        public bool IsStarted => _started;

        public bool IsCompleted => CurrentIndex >= _cues.Count;

        public int TotalCount => _cues.Count;

        public ShadowingCue? CurrentCue
            => CurrentIndex >= 0 && CurrentIndex < _cues.Count ? _cues[CurrentIndex] : null;

        public IReadOnlyList<ShadowingSegment> CreatedSegments => _createdSegments;

        public IReadOnlyList<int> CreatedCueIndexes => _createdCueIndexes;

        public void Start(int startMs, int startIndex = 0)
        {
            _cursorStartMs = Math.Max(0, startMs);
            CurrentIndex = Math.Clamp(startIndex, 0, _cues.Count);
            _started = true;
        }

        /// <summary>
        /// 结束当前句并生成一条切片；返回 null 表示时间间隔过短或已无待对齐句。
        /// 成功后会前进到下一句，游标移动到本次终点。
        /// </summary>
        public ShadowingSegment? Mark(int endMs)
        {
            if (!_started || IsCompleted)
                return null;

            int start = Math.Max(0, _cursorStartMs);
            int end = Math.Max(start, endMs);
            if (end - start < MinimumSegmentMs)
                return null;

            var cue = _cues[CurrentIndex];
            var existing = FindExisting(cue);
            var segment = new ShadowingSegment
            {
                Id = existing?.Id
                     ?? (string.IsNullOrWhiteSpace(cue.AnchorKey)
                         ? ShadowingSegmentStore.CreateSegmentId(_pageIndex)
                         : ShadowingSegmentStore.CreateAnchoredSegmentId(_pageIndex, cue.AnchorKey)),
                StartMs = start,
                EndMs = end,
                Text = cue.Text,
                TextSource = cue.TextSource,
                HighlightId = cue.HighlightId,
                HighlightAnchor = cue.AnchorKey,
                CuePartIndex = cue.PartIndex,
                BoundarySource = "tap",
                AlignmentConfidence = 1.0,
                SlicePath = existing?.SlicePath ?? string.Empty,
                Takes = existing?.Takes?.ToList() ?? new List<ShadowingTake>()
            };

            _createdSegments.Add(segment);
            _createdCueIndexes.Add(CurrentIndex);
            _cursorStartMs = end;
            CurrentIndex++;
            return segment;
        }

        /// <summary>跳过当前句（无对应语音或暂不想对齐），游标保持不动。</summary>
        public bool Skip()
        {
            if (!_started || IsCompleted)
                return false;

            CurrentIndex++;
            return true;
        }

        /// <summary>撤销最近一次打点并回退游标；不涉及磁盘写入。</summary>
        public bool Undo()
        {
            if (_createdSegments.Count == 0)
                return false;

            int last = _createdSegments.Count - 1;
            var segment = _createdSegments[last];
            int cueIndex = _createdCueIndexes[last];

            _createdSegments.RemoveAt(last);
            _createdCueIndexes.RemoveAt(last);
            _cursorStartMs = segment.StartMs;
            CurrentIndex = cueIndex;
            return true;
        }

        /// <summary>
        /// 手工修正下一句起点；用于跳过片头、音乐或重新定位。
        /// 已生成段落不受影响。
        /// </summary>
        public bool SetCursorStart(int startMs)
        {
            if (!_started)
                return false;

            _cursorStartMs = Math.Max(0, startMs);
            return true;
        }

        /// <summary>
        /// 按 V2 锚点、V1 兼容锚点、高亮 ID 顺序找回旧切片，复用其 ID 与 take。
        /// 旧版匿名切片没有锚点，不会被自动复用。
        /// </summary>
        public ShadowingSegment? FindExisting(ShadowingCue cue)
        {
            if (cue == null)
                return null;

            // 与对账器 P1–P3 完全一致的口径（V2 锚点 → V1 兼容锚点 → HighlightId+Part），
            // 不做文本兜底：无锚点的极旧数据在打点期不自动复用，统一由对账上报后走「重新对齐」
            ShadowingSegment? best = null;
            int bestPriority = int.MaxValue;
            foreach (var segment in _existingSegments)
            {
                int priority = ShadowingCueReconciler.MatchPriority(cue, segment);
                if (priority == 0)
                    continue;

                if (best == null
                    || priority < bestPriority
                    || (priority == bestPriority && ComparePrimary(segment, best) < 0))
                {
                    best = segment;
                    bestPriority = priority;
                }
            }

            return best;
        }

        // 与对账器主候选排序一致：SlicePath 非空 > 有 Takes > Id 字典序
        private static int ComparePrimary(ShadowingSegment a, ShadowingSegment b)
        {
            int scoreA = (string.IsNullOrWhiteSpace(a.SlicePath) ? 0 : 2) + (a.Takes?.Count > 0 ? 1 : 0);
            int scoreB = (string.IsNullOrWhiteSpace(b.SlicePath) ? 0 : 2) + (b.Takes?.Count > 0 ? 1 : 0);
            int cmp = scoreB.CompareTo(scoreA);
            return cmp != 0 ? cmp : string.CompareOrdinal(a.Id, b.Id);
        }
    }
}
