using LearningAssistant.Models.Shadowing;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.TTS;

namespace LearningAssistant.Services.Shadowing
{
    public enum ShadowingAudioState
    {
        Pending,
        Synthesizing,
        Ready,
        Failed
    }

    public sealed class ShadowingSentenceState
    {
        public ShadowingSentence Sentence { get; }

        public ShadowingAudioState AudioState { get; internal set; } = ShadowingAudioState.Pending;

        public string? AudioPath { get; internal set; }

        public int DurationMs { get; internal set; }

        /// <summary>切片缺失后回退 TTS 合成；界面需标注，避免误认为原声。</summary>
        public bool IsTtsFallback { get; internal set; }

        public ShadowingSentenceState(ShadowingSentence sentence) => Sentence = sentence;
    }

    /// <summary>
    /// 跟读练习的音频服务：按句合成并缓存、顺序预取、使用 NAudioMediaPlayer 播放。
    /// 公开方法需在 UI 线程调用（播放器内部使用 WinForms 计时器）。
    /// </summary>
    public sealed class ShadowingPracticeService : IDisposable
    {
        private readonly ITTSService? _tts;
        private readonly NAudioMediaPlayer _player;
        private readonly IReadOnlyList<ShadowingSentenceState> _states;
        /// <summary>非空表示进入“原声切片”模式：优先播放切片，缺失时才回退 TTS。</summary>
        private readonly IReadOnlyList<string?>? _presetAudioPaths;
        private readonly SemaphoreSlim _synthesisLock = new(1, 1);
        private readonly CancellationTokenSource _lifetime = new();
        private readonly SynchronizationContext? _uiContext;
        private int _playingIndex = -1;
        private bool _disposed;

        public event EventHandler<int>? StateChanged;
        public event EventHandler<int>? SentenceFinished;
        public event EventHandler<string>? ErrorOccurred;

        public IReadOnlyList<ShadowingSentenceState> Sentences => _states;

        public bool IsAvailable => _tts?.Available == true || HasAnyPreset();

        public ShadowingPracticeService(
            IEnumerable<ShadowingSentence> sentences,
            ITTSService? tts,
            ISubtitleParser subtitleParser,
            IReadOnlyList<string?>? presetAudioPaths = null)
        {
            _tts = tts;
            _presetAudioPaths = presetAudioPaths;
            _player = new NAudioMediaPlayer(subtitleParser);
            _states = sentences.Select(s => new ShadowingSentenceState(s)).ToList();
            _uiContext = SynchronizationContext.Current;

            _player.PlaybackFinished += OnPlaybackFinished;
        }

        /// <summary>
        /// 顺序预取从 startIndex 起 count 句的音频（并发为 1，实测提高并发只会拉长单句延迟）。
        /// </summary>
        public async Task PrefetchAsync(int startIndex, int count, CancellationToken cancellationToken = default)
        {
            if (_states.Count == 0)
                return;

            int end = Math.Min(_states.Count, startIndex + Math.Max(0, count));
            for (int i = Math.Max(0, startIndex); i < end; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await EnsureAudioAsync(i, cancellationToken).ConfigureAwait(true);
            }
        }

        /// <summary>
        /// 确保指定句子已有可用音频；已缓存且文件存在时直接返回。
        /// </summary>
        public async Task<bool> EnsureAudioAsync(int index, CancellationToken cancellationToken = default)
        {
            if (index < 0 || index >= _states.Count)
                return false;

            var state = _states[index];
            if (state.AudioState == ShadowingAudioState.Ready
                && !string.IsNullOrEmpty(state.AudioPath)
                && File.Exists(state.AudioPath))
            {
                return true;
            }
            if (TryApplyPreset(index, state))
                return true;

            using var linked = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, _lifetime.Token);

            await _synthesisLock.WaitAsync(linked.Token).ConfigureAwait(true);
            try
            {
                if (state.AudioState == ShadowingAudioState.Ready
                    && !string.IsNullOrEmpty(state.AudioPath)
                    && File.Exists(state.AudioPath))
                {
                    return true;
                }
                if (TryApplyPreset(index, state))
                    return true;

                if (_tts == null || !_tts.Available)
                {
                    state.AudioState = ShadowingAudioState.Failed;
                    RaiseStateChanged(index);
                    RaiseError(_presetAudioPaths != null
                        ? "原声切片缺失，且语音服务不可用。"
                        : "语音服务不可用。");
                    return false;
                }

                state.AudioState = ShadowingAudioState.Synthesizing;
                RaiseStateChanged(index);

                string language = DetectLanguage(state.Sentence.Text);
                string? path = await _tts.SpeakToCacheAsync(
                    state.Sentence.Text, language, 1.0f, linked.Token).ConfigureAwait(true);

                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                {
                    state.AudioState = ShadowingAudioState.Failed;
                    state.AudioPath = null;
                    RaiseStateChanged(index);
                    RaiseError($"第 {index + 1} 句音频合成失败。");
                    return false;
                }

                state.AudioPath = path;
                state.AudioState = ShadowingAudioState.Ready;
                state.DurationMs = ReadDurationMs(path);
                // 切片模式下的合成结果必须标注为回退，避免被当成原声
                state.IsTtsFallback = _presetAudioPaths != null;
                RaiseStateChanged(index);
                return true;
            }
            catch (OperationCanceledException)
            {
                state.AudioState = ShadowingAudioState.Pending;
                RaiseStateChanged(index);
                return false;
            }
            catch (Exception ex)
            {
                state.AudioState = ShadowingAudioState.Failed;
                RaiseStateChanged(index);
                RaiseError($"第 {index + 1} 句音频合成异常：{ex.Message}");
                return false;
            }
            finally
            {
                _synthesisLock.Release();
            }
        }

        /// <summary>
        /// 播放指定句子；合成完成后立即预取当前句与下一句。
        /// </summary>
        public async Task<bool> PlaySentenceAsync(int index, CancellationToken cancellationToken = default)
        {
            if (_states.Count == 0 || index < 0 || index >= _states.Count)
                return false;

            StopPlayback();

            bool ready = await EnsureAudioAsync(index, cancellationToken).ConfigureAwait(true);
            if (!ready || string.IsNullOrEmpty(_states[index].AudioPath))
                return false;

            _playingIndex = index;
            _player.Load(_states[index].AudioPath!);
            await _player.PlayAsync(cancellationToken).ConfigureAwait(true);

            _ = PrefetchAsync(index + 1, 1, _lifetime.Token);
            return true;
        }

        public void StopPlayback()
        {
            _playingIndex = -1;
            _player.Stop();
        }

        /// <summary>
        /// 获取已合成句子的音频时长；未就绪时返回 0。
        /// </summary>
        public int GetAudioDurationMs(int index)
        {
            if (index < 0 || index >= _states.Count)
                return 0;
            return _states[index].DurationMs;
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _lifetime.Cancel();
            _player.PlaybackFinished -= OnPlaybackFinished;
            _player.Dispose();
            _synthesisLock.Dispose();
            _lifetime.Dispose();
        }

        private void OnPlaybackFinished(object? sender, EventArgs e)
        {
            int index = Interlocked.Exchange(ref _playingIndex, -1);
            if (index < 0)
                return;

            RaiseOnUi(() => SentenceFinished?.Invoke(this, index));
        }

        private void RaiseStateChanged(int index) => RaiseOnUi(() => StateChanged?.Invoke(this, index));

        private void RaiseError(string message) => RaiseOnUi(() => ErrorOccurred?.Invoke(this, message));

        private void RaiseOnUi(Action action)
        {
            if (_uiContext != null)
                _uiContext.Post(_ => action(), null);
            else
                action();
        }

        /// <summary>切片模式：索引对应的原声切片存在则直接就绪，无需合成。</summary>
        private bool TryApplyPreset(int index, ShadowingSentenceState state)
        {
            if (_presetAudioPaths == null || index < 0 || index >= _presetAudioPaths.Count)
                return false;

            string? path = _presetAudioPaths[index];
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            state.AudioPath = path;
            state.DurationMs = ReadDurationMs(path);
            state.IsTtsFallback = false;
            state.AudioState = ShadowingAudioState.Ready;
            RaiseStateChanged(index);
            return true;
        }

        private bool HasAnyPreset()
            => _presetAudioPaths?.Any(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p)) == true;

        private static string DetectLanguage(string text)
        {
            foreach (char c in text)
            {
                if ((c >= 0x4E00 && c <= 0x9FFF) || (c >= 0x3400 && c <= 0x4DBF))
                    return "zh";
            }
            return "en";
        }

        private static int ReadDurationMs(string path)
        {
            try
            {
                using var reader = new NAudio.Wave.AudioFileReader(path);
                return (int)reader.TotalTime.TotalMilliseconds;
            }
            catch
            {
                return 0;
            }
        }
    }
}
