using LearningAssistant.Managers;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Models.Shadowing;
using LearningAssistant.Services.Pdf;

namespace LearningAssistant.Services.Shadowing
{
    public enum ShadowingMarkState
    {
        Idle,
        /// <summary>打点中 · 正在播放，点主按钮即打断。</summary>
        Playing,
        /// <summary>已打断 · 待录，可开始录音或续播。</summary>
        Marked,
        /// <summary>录音中，播放器静音且进度冻结。</summary>
        Recording,
        /// <summary>结束打点后正在后台切原声。</summary>
        Slicing
    }

    public sealed class ShadowingMarkSummary
    {
        public int CreatedSegments { get; init; }
        public int FailedSegments { get; init; }
        public int DiscardedSegments { get; init; }
        public int TotalDurationMs { get; init; }
    }

    /// <summary>
    /// 跟读打点状态机：空闲 → 播放中 → 打断（记源时间戳）→ 录音 → 待录/续播 → 结束打点切原声。
    /// 时间戳一律为源音频时间；打点期间锁定当前页，翻页只提示不切歌。
    /// </summary>
    public sealed class ShadowingMarkSession : IDisposable
    {
        private const int MinSegmentMs = 300;
        private const int MaxSegmentMs = 60_000;

        private readonly PdfAudioTrackManager _manager;
        private readonly IAudioRecorderService _recorder;
        private readonly ShadowingSegmentStore _store = new();
        private readonly AudioSliceService _slicer = new();
        private readonly string _pdfPath;
        private readonly int _pageIndex;
        private readonly string _pdfFileName;
        private readonly SynchronizationContext? _uiContext;
        private readonly List<PendingSegment> _segments = new();

        private string? _trackPath;
        private string _fingerprint = string.Empty;
        private int _durationMs;
        private string _sessionId = string.Empty;
        private double _captureSpeed = 1.0;
        private int _lastBoundaryMs;
        private PendingSegment? _pending;
        private string? _recordingSegmentId;
        private bool _discardRecording;
        private bool _disposed;

        public ShadowingMarkState State { get; private set; } = ShadowingMarkState.Idle;
        public int SegmentCount => _segments.Count;
        public int? CurrentTimestampMs => _pending?.EndMs;
        /// <summary>当前段已有的 take 数；大于 0 时主按钮变“续播”。</summary>
        public int PendingTakeCount => _pending?.Takes.Count ?? 0;
        public ShadowingTake? LastTake { get; private set; }
        public int SlicingDone { get; private set; }
        public int SlicingTotal { get; private set; }
        public double CaptureSpeed => _captureSpeed;
        public int RecordingElapsedMs => (int)(_recorder.CurrentPosition.TotalMilliseconds);

        public event EventHandler? StateChanged;
        public event EventHandler<string>? HintChanged;
        public event EventHandler<ShadowingTake>? TakeSaved;
        public event EventHandler<ShadowingMarkSummary>? Completed;

        public ShadowingMarkSession(
            PdfAudioTrackManager manager,
            IAudioRecorderService recorder,
            string pdfPath,
            int pageIndex)
        {
            _manager = manager;
            _recorder = recorder;
            _pdfPath = pdfPath;
            _pageIndex = pageIndex;
            _pdfFileName = Path.GetFileName(pdfPath);
            _uiContext = SynchronizationContext.Current;
            _recorder.RecordingCompleted += OnRecordingCompleted;
        }

        /// <summary>进入打点会话：锁定当前页并从当前进度（或开头）播放。</summary>
        public bool Begin()
        {
            if (State != ShadowingMarkState.Idle)
                return false;
            if (_manager.CurrentTrack == null || !File.Exists(_manager.CurrentTrack.FilePath))
            {
                RaiseHint("当前页无可用音频，无法进入跟读打点。");
                return false;
            }

            var track = _manager.CurrentTrack;
            _trackPath = track.FilePath;
            _durationMs = (int)_manager.Playback.Player.Duration.TotalMilliseconds;
            if (_durationMs <= 0)
                _durationMs = AudioSliceService.ReadDurationMs(track.FilePath);
            _fingerprint = ShadowingSegmentStore.BuildFingerprint(track.FilePath, _durationMs);
            _sessionId = ShadowingSegmentStore.CreateSessionId(DateTime.Now);
            _captureSpeed = _manager.Profile?.PlaybackSpeed ?? 1.0;

            _segments.Clear();
            _pending = null;
            LastTake = null;

            var position = _manager.Playback.Player.Position;
            if (_durationMs > 0 && position.TotalMilliseconds >= _durationMs - 50)
                position = TimeSpan.Zero;
            if (position > TimeSpan.Zero)
                _manager.Playback.Seek(position);
            _lastBoundaryMs = (int)position.TotalMilliseconds;

            _manager.PageLock = _pageIndex;
            _manager.Play();
            SetState(ShadowingMarkState.Playing);
            RaiseHint("已进入跟读打点：播放中点主按钮即打断。");
            return true;
        }

        /// <summary>
        /// 打断：暂停并记录源时间戳。距上一段不足 300ms 时忽略且不打断播放。
        /// 返回当前时间戳（毫秒）；被忽略时返回 null。
        /// </summary>
        public int? Mark()
        {
            if (State != ShadowingMarkState.Playing)
                return null;

            _manager.Pause();
            int endMs = (int)_manager.Playback.Player.Position.TotalMilliseconds;
            if (endMs - _lastBoundaryMs < MinSegmentMs)
            {
                _manager.Play();
                RaiseHint($"打断点距上一段不足 {MinSegmentMs}ms，已忽略。");
                return null;
            }
            if (endMs - _lastBoundaryMs > MaxSegmentMs)
            {
                RaiseHint("本段超过 60 秒，建议再打断细分；已按 60 秒记录。");
                endMs = _lastBoundaryMs + MaxSegmentMs;
            }

            var segment = new PendingSegment(ShadowingSegmentStore.CreateSegmentId(_pageIndex), _lastBoundaryMs, endMs);
            _segments.Add(segment);
            _pending = segment;
            _lastBoundaryMs = endMs;
            SetState(ShadowingMarkState.Marked);
            RaiseHint($"已打断 · {FormatTimestamp(endMs)} · 可开始跟读或续播。");
            return endMs;
        }

        /// <summary>开始跟读：录音归属于刚打断的这一段。</summary>
        public bool StartRecording()
        {
            if (State != ShadowingMarkState.Marked || _pending == null)
                return false;

            _manager.Pause();
            _discardRecording = false;
            _recordingSegmentId = _pending.Id;
            try
            {
                _recorder.StartRecording(
                    _pdfPath, _pageIndex, _pdfFileName,
                    AudioRecordingScope.Sentence, _pending.Id);
                SetState(ShadowingMarkState.Recording);
                RaiseHint("正在录音，再点一次结束。");
                return true;
            }
            catch (Exception ex)
            {
                _recordingSegmentId = null;
                RaiseHint($"启动录音失败：{ex.Message}");
                return false;
            }
        }

        public void StopRecording()
        {
            if (State != ShadowingMarkState.Recording)
                return;
            _recorder.StopRecording();
            SetState(ShadowingMarkState.Marked);
        }

        /// <summary>续播：从最近一次打断的时间戳继续播放，不改变已记录的时间戳。</summary>
        public bool ResumeFromMark()
        {
            if (State != ShadowingMarkState.Marked || _pending == null)
                return false;

            _manager.Playback.Seek(TimeSpan.FromMilliseconds(_pending.EndMs));
            if (!_manager.Play())
            {
                RaiseHint("续播失败，请检查音频是否仍在。");
                return false;
            }
            SetState(ShadowingMarkState.Playing);
            RaiseHint("已从打断位置续播，可继续打断。");
            return true;
        }

        /// <summary>
        /// 结束打点：按记录的时间戳逐条切原声并写入本页 segments.json。
        /// 已成功的切片不回滚，失败项只提示不写清单。
        /// </summary>
        public async Task<ShadowingMarkSummary?> FinishAsync(CancellationToken cancellationToken = default)
        {
            if (State is ShadowingMarkState.Idle or ShadowingMarkState.Slicing)
                return null;
            if (_trackPath == null)
                return null;

            if (State == ShadowingMarkState.Playing)
                _manager.Pause();
            if (State == ShadowingMarkState.Recording)
                StopRecording();

            var candidates = _segments
                .Where(s => s.EndMs - s.StartMs >= MinSegmentMs)
                .ToList();
            int discarded = _segments.Count - candidates.Count;
            if (candidates.Count == 0)
            {
                EndSession();
                RaiseHint("没有可生成的原声切片。");
                return new ShadowingMarkSummary { DiscardedSegments = discarded };
            }

            SlicingTotal = candidates.Count;
            SlicingDone = 0;
            SetState(ShadowingMarkState.Slicing);

            var requests = candidates
                .Select(s => new AudioSliceRequest(
                    s.Id,
                    _trackPath,
                    s.StartMs,
                    s.EndMs,
                    _store.ResolvePath(_store.GetSliceRelativePath(s.Id))))
                .ToList();

            var progress = new Progress<(int Done, int Total)>(p =>
            {
                SlicingDone = p.Done;
                SlicingTotal = p.Total;
                RaiseStateChanged();
            });

            var results = await _slicer.SliceAllAsync(requests, progress, cancellationToken).ConfigureAwait(true);
            var resultById = results.ToDictionary(r => r.SegmentId, StringComparer.Ordinal);

            var manifest = new ShadowingSegmentManifest
            {
                PageIndex = _pageIndex,
                Source = new ShadowingSegmentSource
                {
                    TrackPath = _trackPath,
                    Fingerprint = _fingerprint,
                    DurationMs = _durationMs
                },
                Session = new ShadowingSegmentSession
                {
                    Id = _sessionId,
                    CaptureSpeed = _captureSpeed,
                    LockedPage = true,
                    UpdatedAt = DateTime.Now
                }
            };

            int totalDuration = 0;
            foreach (var candidate in candidates)
            {
                var result = resultById.GetValueOrDefault(candidate.Id);
                if (result == null || !result.Success)
                {
                    RaiseHint($"第 {candidate.StartMs / 1000.0:0.0}s 切片失败：{result?.Error ?? "未知错误"}");
                    continue;
                }

                manifest.Segments.Add(new ShadowingSegment
                {
                    Id = candidate.Id,
                    StartMs = candidate.StartMs,
                    EndMs = candidate.EndMs,
                    SlicePath = _store.GetSliceRelativePath(candidate.Id),
                    Takes = candidate.Takes
                });
                totalDuration += result.DurationMs;
            }

            if (manifest.Segments.Count > 0)
                await _store.SaveSegmentsAsync(_pdfPath, manifest, cancellationToken).ConfigureAwait(true);

            var summary = new ShadowingMarkSummary
            {
                CreatedSegments = manifest.Segments.Count,
                FailedSegments = candidates.Count - manifest.Segments.Count,
                DiscardedSegments = discarded,
                TotalDurationMs = totalDuration
            };

            EndSession();
            RaiseHint(summary.CreatedSegments > 0
                ? $"已生成 {summary.CreatedSegments} 段原声 · 可打开跟读练习。"
                : "本次打点未生成原声切片。");
            Completed?.Invoke(this, summary);
            return summary;
        }

        /// <summary>放弃本次打点：不写切片、不保存 take，已落盘的旧切片不受影响。</summary>
        public void Cancel()
        {
            if (State == ShadowingMarkState.Recording)
            {
                _discardRecording = true;
                _recorder.StopRecording();
            }

            _segments.Clear();
            _pending = null;
            EndSession();
            RaiseHint("已放弃本次打点。");
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            // 关闭窗口时先停麦克风，避免录音线程在解除订阅后继续占用设备
            if (State == ShadowingMarkState.Recording)
                _recorder.StopRecording();
            _disposed = true;
            _recorder.RecordingCompleted -= OnRecordingCompleted;
            _manager.PageLock = null;
        }

        private void OnRecordingCompleted(object? sender, AudioRecording recording)
        {
            if (_disposed || recording.Scope != AudioRecordingScope.Sentence)
                return;
            if (_recordingSegmentId == null
                || !string.Equals(recording.SentenceId, _recordingSegmentId, StringComparison.Ordinal))
            {
                return;
            }

            var segmentId = _recordingSegmentId;
            _recordingSegmentId = null;

            if (_uiContext != null)
                _uiContext.Post(_ => HandleRecordingCompleted(segmentId, recording), null);
            else
                HandleRecordingCompleted(segmentId, recording);
        }

        private void HandleRecordingCompleted(string segmentId, AudioRecording recording)
        {
            if (_disposed)
                return;

            if (_discardRecording)
            {
                _discardRecording = false;
                TryDelete(recording.FilePath);
                SetState(ShadowingMarkState.Marked);
                return;
            }

            if (recording.DurationMs < MinSegmentMs || string.IsNullOrWhiteSpace(recording.FilePath)
                || !File.Exists(recording.FilePath))
            {
                TryDelete(recording.FilePath);
                SetState(ShadowingMarkState.Marked);
                RaiseHint("录音过短（<300ms），已丢弃。");
                return;
            }

            var segment = _segments.FirstOrDefault(s => string.Equals(s.Id, segmentId, StringComparison.Ordinal));
            if (segment == null)
            {
                TryDelete(recording.FilePath);
                return;
            }

            try
            {
                string relative = _store.AllocateTakeRelativePath(segmentId, recording.CreatedAt);
                string destination = _store.ResolvePath(relative);
                string? directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);
                File.Move(recording.FilePath, destination, overwrite: true);

                int expectedMs = Math.Max(0, segment.EndMs - segment.StartMs);
                var score = TextSimilarityScorer.ScoreCompletion(expectedMs, recording.DurationMs);
                var take = new ShadowingTake
                {
                    Id = ShadowingSegmentStore.CreateTakeId(_pageIndex),
                    Path = relative,
                    DurationMs = recording.DurationMs,
                    Score = score.IsValid ? score.Score : null,
                    ScoreKind = "reference",
                    CreatedAt = recording.CreatedAt
                };
                segment.Takes.Add(take);
                SelectBestTake(segment);

                LastTake = take;
                SetState(ShadowingMarkState.Marked);
                RaiseHint($"已保存第 {segment.Takes.Count} 次跟读"
                          + (take.Score.HasValue ? $" · 参考分 {take.Score}" : string.Empty));
                TakeSaved?.Invoke(this, take);
            }
            catch (Exception ex)
            {
                SetState(ShadowingMarkState.Marked);
                RaiseHint($"保存跟读录音失败：{ex.Message}");
            }
        }

        /// <summary>自动采用最高分的 take；同分时保留最新一条。</summary>
        private static void SelectBestTake(PendingSegment segment)
        {
            if (segment.Takes.Count == 0)
                return;
            var best = segment.Takes
                .OrderBy(t => t.Score ?? -1)
                .ThenBy(t => t.CreatedAt)
                .Last();
            foreach (var take in segment.Takes)
                take.Chosen = ReferenceEquals(take, best);
        }

        private void EndSession()
        {
            _manager.PageLock = null;
            _recordingSegmentId = null;
            SetState(ShadowingMarkState.Idle);
        }

        private void SetState(ShadowingMarkState state)
        {
            State = state;
            RaiseStateChanged();
        }

        private void RaiseStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);

        private void RaiseHint(string message) => HintChanged?.Invoke(this, message);

        private static void TryDelete(string? path)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        public static string FormatTimestamp(int milliseconds)
        {
            var time = TimeSpan.FromMilliseconds(Math.Max(0, milliseconds));
            return time.TotalHours >= 1
                ? $"{(int)time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}"
                : $"{time.Minutes:00}:{time.Seconds:00}.{time.Milliseconds:000}";
        }

        private sealed class PendingSegment
        {
            public PendingSegment(string id, int startMs, int endMs)
            {
                Id = id;
                StartMs = startMs;
                EndMs = endMs;
            }

            public string Id { get; }
            public int StartMs { get; }
            public int EndMs { get; }
            public List<ShadowingTake> Takes { get; } = new();
        }
    }
}
