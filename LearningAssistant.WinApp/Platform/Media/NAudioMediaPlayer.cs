using LearningAssistant.Abstractions.Media;
using LearningAssistant.Services.Media;
using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

public class NAudioMediaPlayer : IMediaPlayer
{
    private readonly ISubtitleParser _subtitleParser;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _reader;
    private System.Windows.Forms.Timer? _timer;
    private readonly AbLoopState _abLoop = new();
    private bool _manualStop;
    private double _speed = 1.0;
    private double _volume = 1.0;
    private System.Windows.Forms.Timer? _fadeTimer;
    private int _fadeGeneration;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<MediaPlaybackState>? StateChanged;
    public event EventHandler? PlaybackFinished;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public double Speed
    {
        get => _speed;
        set
        {
            // 阶段一：倍速引擎尚未落地（待时域拉伸 spike），仅接受 1.0 正常速度，
            // 其它值忽略以严格保证「不变调」。spike 通过后再接入真正的变速实现。
            if (value != 1.0)
            {
                System.Diagnostics.Trace.TraceWarning($"[NAudioMediaPlayer] 音频倍速尚未实现，忽略请求的倍速 {value}，保持 1.0 不变调。");
                return;
            }
            _speed = value;
        }
    }

    public double Volume
    {
        get => _volume;
        set
        {
            // 用户主动改音量时终止渐变，避免渐变把用户设置覆盖回去
            CancelFade();
            _volume = Math.Clamp(value, 0d, 1d);
            if (_waveOut != null) _waveOut.Volume = (float)_volume;
        }
    }

    public MediaLoopMode LoopMode { get; set; } = MediaLoopMode.None;
    public (TimeSpan? A, TimeSpan? B) AbLoop { get => (_abLoop.A, _abLoop.B); set => (_abLoop.A, _abLoop.B) = value; }
    public IReadOnlyList<SubtitleCue>? SubtitleCues { get; private set; }

    public NAudioMediaPlayer(ISubtitleParser subtitleParser) => _subtitleParser = subtitleParser;

    public void Load(string mediaPath)
    {
        CancelFade();
        // 先释放旧资源，防止旧 WaveOutEvent 的 PlaybackStopped 事件异步干扰新播放状态
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Stop();
            _waveOut.Dispose();
            _waveOut = null;
        }
        _reader?.Dispose();
        _reader = null;
        SubtitleCues = null;
        // 换曲时清除上一曲的 A-B 循环，避免沿用旧区间
        _abLoop.A = null;
        _abLoop.B = null;

        try
        {
            _reader = new AudioFileReader(mediaPath);
            _waveOut = new WaveOutEvent { Volume = (float)_volume };
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            _waveOut.Init(_reader);
            _timer = new System.Windows.Forms.Timer { Interval = 100 };
            _timer.Tick += (_, _) => OnTick();
            _timer.Start();
            SetState(MediaPlaybackState.Idle);
        }
        catch (Exception ex)
        {
            // 文件被删除/占用或格式不支持时不能让 UI 线程崩溃，退回未加载状态
            _timer?.Stop();
            _timer?.Dispose();
            _timer = null;
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Dispose();
                _waveOut = null;
            }
            _reader?.Dispose();
            _reader = null;
            SetState(MediaPlaybackState.Idle);
            System.Diagnostics.Trace.TraceError($"[NAudioMediaPlayer] 加载音频失败: {mediaPath} - {ex.Message}");
        }
    }

    public Task PlayAsync(CancellationToken ct = default)
    {
        if (_waveOut == null) return Task.CompletedTask;
        _manualStop = false;
        _waveOut.Play();
        SetState(MediaPlaybackState.Playing);
        return Task.CompletedTask;
    }

    public void Pause() { _waveOut?.Pause(); SetState(MediaPlaybackState.Paused); }
    public void Resume() { _ = PlayAsync(); }
    public void Stop() { _manualStop = true; _waveOut?.Stop(); _reader?.Seek(0, System.IO.SeekOrigin.Begin); SetState(MediaPlaybackState.Stopped); }
    public void Seek(TimeSpan position) { if (_reader != null && position >= TimeSpan.Zero && position <= _reader.TotalTime) _reader.CurrentTime = position; }

    public void FadeOut(int milliseconds, bool blocking = false) => FadeTo(0d, milliseconds, blocking);

    public void FadeIn(int milliseconds) => FadeTo(_volume, milliseconds, blocking: false, fromZero: true);

    /// <summary>渐变只改 WaveOut 的实际音量，不改变 Volume 目标值，换曲后仍能恢复用户音量。</summary>
    private void FadeTo(double target, int milliseconds, bool blocking, bool fromZero = false)
    {
        target = Math.Clamp(target, 0d, 1d);
        if (_waveOut == null || milliseconds <= 0)
        {
            if (_waveOut != null) _waveOut.Volume = (float)target;
            return;
        }

        CancelFade();
        int generation = ++_fadeGeneration;
        double start = fromZero ? 0d : _waveOut.Volume;
        long startedAt = Environment.TickCount64;

        double Progress()
            => Math.Clamp((Environment.TickCount64 - startedAt) / (double)milliseconds, 0d, 1d);

        if (blocking)
        {
            while (generation == _fadeGeneration && _waveOut != null)
            {
                double t = Progress();
                _waveOut.Volume = (float)(start + (target - start) * t);
                if (t >= 1d) break;
                Thread.Sleep(3);
            }
            if (generation == _fadeGeneration && _waveOut != null)
                _waveOut.Volume = (float)target;
            return;
        }

        var timer = new System.Windows.Forms.Timer { Interval = 5 };
        timer.Tick += (_, _) =>
        {
            if (generation != _fadeGeneration || _waveOut == null)
            {
                timer.Stop();
                timer.Dispose();
                return;
            }

            double t = Progress();
            _waveOut.Volume = (float)(start + (target - start) * t);
            if (t < 1d) return;

            timer.Stop();
            timer.Dispose();
            if (!ReferenceEquals(_fadeTimer, timer)) return;
            _fadeTimer = null;
            _fadeGeneration++;
        };
        _fadeTimer = timer;
        timer.Start();
    }

    private void CancelFade()
    {
        _fadeGeneration++;
        if (_fadeTimer == null) return;
        _fadeTimer.Stop();
        _fadeTimer.Dispose();
        _fadeTimer = null;
    }

    public void LoadSubtitle(string? subtitlePath)
    {
        SubtitleCues = string.IsNullOrWhiteSpace(subtitlePath) || !File.Exists(subtitlePath)
            ? null : _subtitleParser.Parse(subtitlePath);
    }

    private void OnTick()
    {
        if (_abLoop.ShouldLoopBack(Position, out var target)) Seek(target);
        PositionChanged?.Invoke(this, Position);
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (_manualStop) return;
        if (LoopMode == MediaLoopMode.RepeatOne || LoopMode == MediaLoopMode.RepeatAll)
        { _reader?.Seek(0, System.IO.SeekOrigin.Begin); _waveOut?.Play(); SetState(MediaPlaybackState.Playing); return; }
        SetState(MediaPlaybackState.Stopped);
        PlaybackFinished?.Invoke(this, EventArgs.Empty);
    }

    private void SetState(MediaPlaybackState s)
    {
        if (State == s) return;
        State = s;
        StateChanged?.Invoke(this, s);
    }

    public void Dispose()
    {
        CancelFade();
        _timer?.Stop();
        _timer?.Dispose();
        Stop();
        _waveOut?.Dispose();
        _reader?.Dispose();
    }
}