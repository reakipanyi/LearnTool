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
    public int PageCount => _pageCount;
    public IReadOnlyList<string> Diagnostics => _matcher.Diagnostics;

    /// <summary>用给定规则对当前已扫描曲目试算，不修改已生效的 profile</summary>
    public AudioMatchResult PreviewRules(IReadOnlyList<AudioMatchRule> rules)
    {
        // 传入副本，避免试算修改缓存曲目的映射状态
        var tracks = (_profile?.Tracks ?? new List<AudioTrack>())
            .Select(t => new AudioTrack
            {
                FilePath = t.FilePath,
                FileName = t.FileName,
                RelativePath = t.RelativePath,
                SubtitlePath = t.SubtitlePath
            })
            .ToList();
        return _matcher.Match(_currentPdfPath ?? "", _pageCount, tracks, rules, _profile?.ManualOverrides);
    }

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

        var scanChanged = fingerprint != _profile.ScanFingerprint || _profile.Tracks.Count == 0;
        var pageCountChanged = _profile.PageCount != pageCount;
        // 指纹与页数均未变化时复用缓存映射；否则重算，避免规则结果丢失或页数变化后映射越界
        if (scanChanged || pageCountChanged || _profile.PageIndex.Count == 0)
        {
            _result = _matcher.Match(pdfPath, pageCount, tracks, _profile.Rules, _profile.ManualOverrides);
            _profile.Tracks = tracks;
            _profile.ScanFingerprint = fingerprint;
            _profile.PageCount = pageCount;
            PersistPageIndex();
            _store.Save(_profile);
        }
        else
        {
            _result = new AudioMatchResult { Tracks = _profile.Tracks };
            foreach (var kv in _profile.PageIndex)
            {
                if (kv.Key < 0 || kv.Key >= pageCount) continue;
                _result.PageIndex[kv.Key] = ResolveTrack(kv.Value, markManual: false);
            }
            // 手动覆盖始终最高优先级
            foreach (var kv in _profile.ManualOverrides)
            {
                if (kv.Key < 0 || kv.Key >= pageCount) continue;
                _result.PageIndex[kv.Key] = ResolveTrack(kv.Value, markManual: true);
            }
            _result.UnmatchedPages.Clear();
            for (int p = 0; p < pageCount; p++)
                if (!_result.PageIndex.ContainsKey(p)) _result.UnmatchedPages.Add(p);
        }
        _currentPageIndex = -1;
    }

    /// <summary>把当前匹配结果按页持久化，供下次指纹命中时恢复</summary>
    private void PersistPageIndex()
    {
        if (_profile == null || _result == null) return;
        _profile.PageIndex = _result.PageIndex.ToDictionary(
            kv => kv.Key,
            kv => string.IsNullOrEmpty(kv.Value.RelativePath) ? kv.Value.FileName : kv.Value.RelativePath);
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
            cur = _result.Tracks.FindIndex(t => string.Equals(t.FilePath, current.FilePath, StringComparison.OrdinalIgnoreCase));
        }

        int count = _result.Tracks.Count;
        int next = cur < 0
            ? (delta > 0 ? 0 : count - 1)          // 当前页无音频时：下一曲从首曲开始，上一曲从末曲开始
            : ((cur + delta) % count + count) % count; // 正负方向均能正确环绕
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
        // 同步缓存映射，避免指纹命中时恢复到手动指定前的旧结果
        _profile.PageIndex[page] = file;
        _store.Save(_profile);
        if (_result != null)
        {
            _result.PageIndex[page] = ResolveTrack(file, markManual: true);
            _result.UnmatchedPages.Remove(page);
        }
    }

    private AudioTrack? CurrentTrack()
    {
        if (_result == null) return null;
        return _result.PageIndex.TryGetValue(_currentPageIndex, out var t) ? t : null;
    }

    private AudioTrack ResolveTrack(string fileNameOrRelativePath, bool markManual)
    {
        // 相对路径优先，避免不同子目录下同名文件被误解析
        var track = _profile?.Tracks.FirstOrDefault(t =>
                        string.Equals(t.RelativePath, fileNameOrRelativePath, StringComparison.OrdinalIgnoreCase))
                    ?? _profile?.Tracks.FirstOrDefault(t =>
                        string.Equals(t.FilePath, fileNameOrRelativePath, StringComparison.OrdinalIgnoreCase))
                    ?? _profile?.Tracks.FirstOrDefault(t =>
                        string.Equals(t.FileName, Path.GetFileName(fileNameOrRelativePath), StringComparison.OrdinalIgnoreCase));
        if (track == null)
        {
            track = new AudioTrack
            {
                FilePath = Path.IsPathRooted(fileNameOrRelativePath)
                    ? fileNameOrRelativePath
                    : Path.Combine(Path.GetDirectoryName(_currentPdfPath) ?? "", fileNameOrRelativePath),
                FileName = Path.GetFileName(fileNameOrRelativePath),
                RelativePath = fileNameOrRelativePath
            };
            _profile?.Tracks.Add(track);
        }
        if (markManual) track.IsManual = true;
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