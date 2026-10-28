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
        /// 按锚点、高亮 ID 或文案顺序找回旧切片，复用其 ID 与 take。
        /// 旧版匿名切片没有锚点，不会被自动复用。
        /// </summary>
        public ShadowingSegment? FindExisting(ShadowingCue cue)
        {
            if (cue == null)
                return null;

            if (!string.IsNullOrWhiteSpace(cue.AnchorKey))
            {
                var byAnchor = _existingSegments.FirstOrDefault(segment =>
                    string.Equals(segment.HighlightAnchor, cue.AnchorKey, StringComparison.Ordinal));
                if (byAnchor != null)
                    return byAnchor;
            }

            if (!string.IsNullOrWhiteSpace(cue.HighlightId))
            {
                var byHighlight = _existingSegments.FirstOrDefault(segment =>
                    string.Equals(segment.HighlightId, cue.HighlightId, StringComparison.Ordinal)
                    && segment.CuePartIndex == cue.PartIndex);
                if (byHighlight != null)
                    return byHighlight;
            }

            // 页面文字没有高亮时，用句子 ID 生成的锚点通常已经命中；这里再兜底一次文案匹配
            return _existingSegments.FirstOrDefault(segment =>
                segment.CuePartIndex == cue.PartIndex
                && string.Equals(segment.Text, cue.Text, StringComparison.Ordinal));
        }
    }
}
