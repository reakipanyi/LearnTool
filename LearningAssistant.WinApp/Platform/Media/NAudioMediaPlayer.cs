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

    public MediaLoopMode LoopMode { get; set; } = MediaLoopMode.None;
    public (TimeSpan? A, TimeSpan? B) AbLoop { get => (_abLoop.A, _abLoop.B); set => (_abLoop.A, _abLoop.B) = value; }
    public IReadOnlyList<SubtitleCue>? SubtitleCues { get; private set; }

    public NAudioMediaPlayer(ISubtitleParser subtitleParser) => _subtitleParser = subtitleParser;

    public void Load(string mediaPath)
    {
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
            _waveOut = new WaveOutEvent();
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

    public void Dispose() { _timer?.Stop(); _timer?.Dispose(); Stop(); _waveOut?.Dispose(); _reader?.Dispose(); }
}