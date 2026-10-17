using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Media;
using LearningAssistant.Services.Pdf;
using LearningAssistant.Services.Pdf.Audio;
using Microsoft.Extensions.Logging;

namespace LearningAssistant.Managers;

public class PdfAudioTrackManager : IDisposable
{
    private readonly ILogger _logger;
    private readonly IPdfAudioMatcher _matcher;
    private readonly IPdfAudioDirectoryScanner _scanner;
    private readonly IPdfAudioProfileStore _store;
    private readonly PdfAudioPlaybackService _playback;
    private AudioMatchResult? _result;
    private PdfAudioProfile? _profile;
    private string? _currentPdfPath;
    private int _currentPageIndex = -1;
    private int _pageCount;
    private bool _disposed;

    public PdfAudioPlaybackService Playback => _playback;
    public AudioMatchResult? Result => _result;
    public PdfAudioProfile? Profile => _profile;

    public PdfAudioTrackManager(ILogger logger,
        IPdfAudioMatcher matcher, IPdfAudioDirectoryScanner scanner,
        IPdfAudioProfileStore store, ISubtitleParser subtitleParser)
    {
        _logger = logger;
        _matcher = matcher;
        _scanner = scanner;
        _store = store;
        _playback = new PdfAudioPlaybackService(new NAudioMediaPlayer(subtitleParser));
    }

    public void LoadProfile(string pdfPath, int pageCount)
    {
        _currentPdfPath = pdfPath;
        _pageCount = pageCount;
        _profile = _store.Load(pdfPath) ?? new PdfAudioProfile { PdfPath = pdfPath };
        var dir = Path.GetDirectoryName(pdfPath) ?? "";
        var tracks = _scanner.Scan(dir, _profile.IncludeSubDirectories);
        var fingerprint = PdfAudioDirectoryScanner.ComputeScanFingerprint(tracks);

        if (fingerprint != _profile.ScanFingerprint || _profile.Tracks.Count == 0)
        {
            _result = _matcher.Match(pdfPath, pageCount, tracks, _profile.Rules, _profile.ManualOverrides);
            _profile.Tracks = tracks;
            _profile.ScanFingerprint = fingerprint;
            _store.Save(_profile);
        }
        else
        {
            _result = new AudioMatchResult { Tracks = _profile.Tracks };
            foreach (var kv in _profile.ManualOverrides)
                _result.PageIndex[kv.Key] = FindOrCreate(kv.Value);
        }
        _currentPageIndex = -1;
    }

    public void OnPageChanged(int pageIndex)
    {
        if (_result == null || pageIndex == _currentPageIndex) { _currentPageIndex = pageIndex; return; }
        _currentPageIndex = pageIndex;
        if (_result.PageIndex.TryGetValue(pageIndex, out var track))
        {
            if (_playback.Player.State == MediaPlaybackState.Playing)
            {
                _playback.Load(track);
                FireAndForget(_playback.PlayAsync(), "PlayAsync");
            }
            else
            {
                _playback.Load(track);
            }
        }
        else
        {
            _playback.Stop();
        }
    }

    public void Next() => Move(1);
    public void Prev() => Move(-1);

    private void Move(int delta)
    {
        if (_result == null || _result.Tracks.Count == 0) return;
        int cur = -1;
        if (_result.PageIndex.TryGetValue(_currentPageIndex, out var current))
        {
            cur = _result.Tracks.FindIndex(t => t.FilePath == current.FilePath);
        }
        var next = (_result.Tracks.Count + (cur < 0 ? 0 : cur) + delta) % _result.Tracks.Count;
        var track = _result.Tracks[next];
        _playback.Load(track);
        FireAndForget(_playback.PlayAsync(), "PlayAsync");
    }

    public bool PlayOrPause()
    {
        switch (_playback.Player.State)
        {
            case MediaPlaybackState.Playing:
                _playback.Pause();
                return true;
            case MediaPlaybackState.Paused:
                _playback.Resume();
                return true;
            default:
                // 已加载音频但处于待机/停止态，直接继续播放
                if (_playback.Player.Duration > TimeSpan.Zero)
                {
                    _playback.Resume();
                    return true;
                }
                var track = CurrentTrack();
                if (track != null)
                {
                    _playback.Load(track);
                    FireAndForget(_playback.PlayAsync(), "PlayAsync");
                    return true;
                }
                return false;
        }
    }

    public void Rescan()
    {
        if (_profile == null || _currentPdfPath == null) return;
        _profile.ScanFingerprint = ""; // 强制重算匹配
        LoadProfile(_currentPdfPath, _pageCount);
    }

    public void SetIncludeSubDirectories(bool value)
    {
        if (_profile == null || _currentPdfPath == null) return;
        if (_profile.IncludeSubDirectories == value) return;
        _profile.IncludeSubDirectories = value;
        _store.Save(_profile);
        Rescan();
    }

    public void SaveRules()
    {
        if (_profile == null) return;
        _store.Save(_profile);
    }

    public void SetManualOverride(int page, string file)
    {
        if (_profile == null) return;
        _profile.ManualOverrides[page] = file;
        _store.Save(_profile);
        if (_result != null) _result.PageIndex[page] = FindOrCreate(file);
    }

    private AudioTrack? CurrentTrack()
    {
        if (_result == null) return null;
        return _result.PageIndex.TryGetValue(_currentPageIndex, out var t) ? t : null;
    }

    private AudioTrack FindOrCreate(string fileName)
    {
        var track = _profile?.Tracks.FirstOrDefault(t =>
            string.Equals(t.FileName, Path.GetFileName(fileName), StringComparison.OrdinalIgnoreCase)
            || string.Equals(t.RelativePath, fileName, StringComparison.OrdinalIgnoreCase));
        if (track == null)
        {
            track = new AudioTrack
            {
                FilePath = Path.Combine(Path.GetDirectoryName(_currentPdfPath) ?? "", fileName),
                FileName = Path.GetFileName(fileName),
                RelativePath = fileName
            };
            _profile?.Tracks.Add(track);
        }
        track!.IsManual = true;
        return track;
    }

    private void FireAndForget(Task task, string operation)
    {
        _ = task.ContinueWith(t => _logger.LogError(t.Exception, "音频播放操作异常：{Operation}", operation),
            TaskContinuationOptions.OnlyOnFaulted);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _playback.Stop();
        _playback.Player.Dispose();
    }
}