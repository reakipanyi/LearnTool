using LearningAssistant.Abstractions.Media;
using LearningAssistant.Services.Media;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace LearningAssistant.Platform.Media;

public class NAudioMediaPlayer : IMediaPlayer
{
    private const double MinSpeed = 0.5;
    private const double MaxSpeed = 2.0;

    private readonly ISubtitleParser _subtitleParser;
    private WaveOutEvent? _waveOut;
    private AudioFileReader? _reader;
    private VarispeedSampleProvider? _varispeed;
    private NAudio.Wave.SampleProviders.SmbPitchShiftingSampleProvider? _pitchShift;
    private SpectrumTapSampleProvider? _spectrumTap;
    private System.Windows.Forms.Timer? _timer;
    private readonly AbLoopState _abLoop = new();
    private bool _manualStop;
    private double _speed = 1.0;
    private double _volume = 1.0;
    private System.Windows.Forms.Timer? _fadeTimer;
    private int _fadeGeneration;
    private MMDeviceEnumerator? _deviceEnumerator;
    private System.Windows.Forms.Timer? _deviceTimer;
    private string? _lastDeviceId;

    public event EventHandler<TimeSpan>? PositionChanged;
    public event EventHandler<MediaPlaybackState>? StateChanged;
    public event EventHandler? PlaybackFinished;
    /// <summary>系统默认播放设备变化（拔插/切换默认设备）时触发</summary>
    public event EventHandler? OutputDeviceChanged;

    public MediaPlaybackState State { get; private set; } = MediaPlaybackState.Idle;
    public TimeSpan Position => _reader?.CurrentTime ?? TimeSpan.Zero;
    public TimeSpan Duration => _reader?.TotalTime ?? TimeSpan.Zero;

    public double Speed
    {
        get => _speed;
        set
        {
            // 变速不变调：变速取样后串接音高修正，再把音高拉回原调
            double speed = Math.Clamp(value, MinSpeed, MaxSpeed);
            if (Math.Abs(_speed - speed) < 0.001) return;
            _speed = speed;
            RebuildOutputChain();
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

    public NAudioMediaPlayer(ISubtitleParser subtitleParser)
    {
        _subtitleParser = subtitleParser;
        StartDeviceWatch();
    }

    /// <summary>
    /// 监听系统默认播放设备变化；无声卡或系统不支持 CoreAudio 时静默降级，
    /// 不影响正常播放。MMDeviceEnumerator 未暴露事件，这里用 2 秒轮询设备 ID。
    /// </summary>
    private void StartDeviceWatch()
    {
        try
        {
            _deviceEnumerator = new MMDeviceEnumerator();
            _lastDeviceId = GetDefaultDeviceId();
            _deviceTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            _deviceTimer.Tick += (_, _) => CheckDefaultDevice();
            _deviceTimer.Start();
        }
        catch (Exception ex)
        {
            _deviceTimer?.Dispose();
            _deviceTimer = null;
            _deviceEnumerator?.Dispose();
            _deviceEnumerator = null;
            System.Diagnostics.Trace.TraceWarning($"[NAudioMediaPlayer] 无法监听音频设备变化: {ex.Message}");
        }
    }

    private string? GetDefaultDeviceId()
    {
        try
        {
            using var device = _deviceEnumerator?.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            return device?.ID;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private void CheckDefaultDevice()
    {
        var current = GetDefaultDeviceId();
        if (string.Equals(current, _lastDeviceId, StringComparison.OrdinalIgnoreCase)) return;
        // 设备切换或设备被移除都会改变默认设备 ID，通知上层重载到新设备
        _lastDeviceId = current;
        OutputDeviceChanged?.Invoke(this, EventArgs.Empty);
    }

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
        _varispeed = null;
        _pitchShift = null;
        _spectrumTap = null;
        SubtitleCues = null;
        // 换曲时清除上一曲的 A-B 循环，避免沿用旧区间
        _abLoop.A = null;
        _abLoop.B = null;

        try
        {
            _reader = new AudioFileReader(mediaPath);
            _waveOut = new WaveOutEvent { Volume = (float)_volume };
            _waveOut.PlaybackStopped += OnPlaybackStopped;
            BuildOutputChain();
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

    /// <summary>
    /// 按当前倍速搭建输出链：1.0x 直接播放原始流；其它倍速先变速取样，
    /// 再用音高修正抵消变速带来的音高变化，做到变速不变调。
    /// </summary>
    private void BuildOutputChain()
    {
        if (_reader == null || _waveOut == null) return;

        _varispeed = null;
        _pitchShift = null;
        _spectrumTap = null;

        ISampleProvider output;
        if (Math.Abs(_speed - 1.0) < 0.001)
        {
            output = _reader;
        }
        else
        {
            _varispeed = new VarispeedSampleProvider(_reader, _speed);
            _pitchShift = new NAudio.Wave.SampleProviders.SmbPitchShiftingSampleProvider(_varispeed)
            {
                PitchFactor = (float)Math.Clamp(1.0 / _speed, 0.5, 2.0)
            };
            output = _pitchShift;
        }

        // 频谱旁路挂在最终输出前，不改变音频数据
        _spectrumTap = new SpectrumTapSampleProvider(output);
        _waveOut.Init(_spectrumTap);
    }

    /// <summary>倍速变化时重建输出链，保持源位置、音量与播放/暂停状态。</summary>
    private void RebuildOutputChain()
    {
        if (_reader == null || _waveOut == null) return;

        bool wasPlaying = State == MediaPlaybackState.Playing;
        var position = _reader.CurrentTime;
        float volume = _waveOut.Volume;

        // 主动 Stop 会触发 PlaybackStopped，先标记避免误判为播放结束
        _manualStop = true;
        _waveOut.Stop();
        _manualStop = false;
        _waveOut.PlaybackStopped -= OnPlaybackStopped;
        _waveOut.Dispose();
        _waveOut = null;

        _reader.CurrentTime = position;
        _varispeed = null;
        _pitchShift = null;
        _spectrumTap = null;

        _waveOut = new WaveOutEvent { Volume = volume };
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        BuildOutputChain();
        if (wasPlaying) _waveOut.Play();
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
    public void Stop() { _manualStop = true; _waveOut?.Stop(); _reader?.Seek(0, System.IO.SeekOrigin.Begin); _varispeed?.Reset(); SetState(MediaPlaybackState.Stopped); }
    public void Seek(TimeSpan position)
    {
        if (_reader == null || position < TimeSpan.Zero || position > _reader.TotalTime) return;
        _reader.CurrentTime = position;
        // 变速缓冲里可能残留定位前的采样，必须丢弃
        _varispeed?.Reset();
    }

    /// <summary>
    /// 把最近约 46ms 的单声道采样复制到 destination，供实时频谱使用。
    /// 未加载音频或采样不足时返回 false。
    /// </summary>
    public bool TryReadSpectrum(float[] destination, out int sampleRate)
    {
        var tap = _spectrumTap;
        sampleRate = tap?.WaveFormat.SampleRate ?? 0;
        return tap != null && tap.TryGetLatest(destination);
    }

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
        { _reader?.Seek(0, System.IO.SeekOrigin.Begin); _varispeed?.Reset(); _waveOut?.Play(); SetState(MediaPlaybackState.Playing); return; }
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
        _deviceTimer?.Stop();
        _deviceTimer?.Dispose();
        _deviceTimer = null;
        _deviceEnumerator?.Dispose();
        _deviceEnumerator = null;
        CancelFade();
        _timer?.Stop();
        _timer?.Dispose();
        Stop();
        _waveOut?.Dispose();
        _reader?.Dispose();
    }
}