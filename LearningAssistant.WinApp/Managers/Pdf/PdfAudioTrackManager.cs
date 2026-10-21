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

    private int _loadGeneration;
    /// <summary>异步加载/重新扫描完成后触发，供 UI 刷新</summary>
    public event EventHandler? ProfileLoaded;

    public async Task LoadProfileAsync(string pdfPath, int pageCount, CancellationToken cancellationToken = default)
    {
        var generation = ++_loadGeneration;
        _currentPdfPath = pdfPath;
        _pageCount = pageCount;
        _profile = _store.Load(pdfPath) ?? new PdfAudioProfile { PdfPath = pdfPath };
        // 扫描期间不沿用上一份文档的匹配结果，避免翻页播放错音频
        _result = null;
        _currentPageIndex = -1;

        var directory = Path.GetDirectoryName(pdfPath) ?? "";
        var includeSubDirectories = _profile.IncludeSubDirectories;
        // 外部目录在进入扫描前取快照，避免后台线程读取被 UI 线程修改的集合
        var externalDirectories = _profile.ExternalAudioDirectories.ToList();
        List<AudioTrack> tracks;
        string fingerprint;
        try
        {
            (tracks, fingerprint) = await Task.Run(() =>
            {
                var scanned = ScanAll(directory, includeSubDirectories, externalDirectories);
                return (scanned, PdfAudioDirectoryScanner.ComputeScanFingerprint(scanned));
            }, cancellationToken).ConfigureAwait(true);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        // 期间已打开其它文档，丢弃过期结果
        if (_disposed || generation != _loadGeneration) return;

        ApplyLoadedProfile(pdfPath, pageCount, tracks, fingerprint);
        ProfileLoaded?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyLoadedProfile(string pdfPath, int pageCount, List<AudioTrack> tracks, string fingerprint)
    {
        var profile = _profile!;
        var scanChanged = fingerprint != profile.ScanFingerprint || profile.Tracks.Count == 0;
        var pageCountChanged = profile.PageCount != pageCount;
        // 指纹与页数均未变化时复用缓存映射；否则重算，避免规则结果丢失或页数变化后映射越界
        if (scanChanged || pageCountChanged || profile.PageIndex.Count == 0)
        {
            _result = _matcher.Match(pdfPath, pageCount, tracks, profile.Rules, profile.ManualOverrides);
            profile.Tracks = tracks;
            profile.ScanFingerprint = fingerprint;
            profile.PageCount = pageCount;
            PersistPageIndex();
            _store.Save(profile);
        }
        else
        {
            _result = new AudioMatchResult { Tracks = profile.Tracks };
            foreach (var kv in profile.PageIndex)
            {
                if (kv.Key < 0 || kv.Key >= pageCount) continue;
                _result.PageIndex[kv.Key] = ResolveTrack(kv.Value, markManual: false);
            }
            // 手动覆盖始终最高优先级
            foreach (var kv in profile.ManualOverrides)
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

    /// <summary>扫描 PDF 同目录及已登记的外部音频目录，按完整路径去重</summary>
    private List<AudioTrack> ScanAll(string pdfDirectory, bool includeSubDirectories,
        IReadOnlyList<string> externalDirectories)
    {
        var tracks = _scanner.Scan(pdfDirectory, includeSubDirectories);
        var seen = new HashSet<string>(tracks.Select(t => t.FilePath), StringComparer.OrdinalIgnoreCase);
        foreach (var external in externalDirectories)
        {
            if (string.IsNullOrWhiteSpace(external) || !Directory.Exists(external)) continue;
            foreach (var track in _scanner.Scan(external, includeSubDirectories: true))
            {
                if (!seen.Add(track.FilePath)) continue;
                // 外部目录曲目改用绝对路径标识，避免与 PDF 同目录的同名文件混淆
                track.RelativePath = track.FilePath;
                tracks.Add(track);
            }
        }
        return tracks
            .OrderBy(t => t.RelativePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>把外部音频目录合并到当前曲目并立即重新匹配，返回新增曲目数量</summary>
    public async Task<int> AddExternalDirectoryAsync(string directoryPath)
    {
        if (_profile == null || _currentPdfPath == null) return 0;
        var fullPath = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(fullPath))
            throw new DirectoryNotFoundException($"音频目录不存在：{fullPath}");

        var before = _profile.Tracks
            .Select(t => t.FilePath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!_profile.ExternalAudioDirectories.Any(d =>
                string.Equals(d, fullPath, StringComparison.OrdinalIgnoreCase)))
        {
            _profile.ExternalAudioDirectories.Add(fullPath);
            _store.Save(_profile);
            await LoadProfileAsync(_currentPdfPath, _pageCount);
        }
        return _profile.Tracks.Count(t => !before.Contains(t.FilePath));
    }

    /// <summary>批量应用 AI 匹配结果（页 → 音频完整路径），写入手动覆盖并立即生效</summary>
    public void ApplyManualMatches(IReadOnlyDictionary<int, string> pageToFile)
    {
        if (_profile == null) return;
        foreach (var kv in pageToFile)
        {
            if (kv.Key < 0 || kv.Key >= _pageCount || string.IsNullOrWhiteSpace(kv.Value)) continue;
            var value = ToProfilePath(kv.Value);
            _profile.ManualOverrides[kv.Key] = value;
            // 同步缓存映射，避免指纹命中时恢复到应用前的旧结果
            _profile.PageIndex[kv.Key] = value;
        }
        SaveRulesAndRematch();
    }

    /// <summary>PDF 同目录内用相对路径，目录外保留绝对路径</summary>
    private string ToProfilePath(string fullPath)
    {
        var pdfDir = Path.GetDirectoryName(_currentPdfPath ?? "");
        if (string.IsNullOrEmpty(pdfDir)) return fullPath;
        var baseDir = Path.GetFullPath(pdfDir);
        var target = Path.GetFullPath(fullPath);
        return target.StartsWith(baseDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            ? Path.GetRelativePath(baseDir, target)
            : target;
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

    public async Task RescanAsync()
    {
        if (_currentPdfPath == null) return;
        await LoadProfileAsync(_currentPdfPath, _pageCount);
    }

    public async Task SetIncludeSubDirectoriesAsync(bool value)
    {
        if (_profile == null || _currentPdfPath == null) return;
        if (_profile.IncludeSubDirectories == value) return;
        _profile.IncludeSubDirectories = value;
        _store.Save(_profile);
        await LoadProfileAsync(_currentPdfPath, _pageCount);
    }

    /// <summary>规则变更后直接用已扫描曲目重算，无需再次扫描目录</summary>
    public void SaveRulesAndRematch()
    {
        if (_profile == null || _currentPdfPath == null) return;
        _result = _matcher.Match(_currentPdfPath, _pageCount, _profile.Tracks,
            _profile.Rules, _profile.ManualOverrides);
        PersistPageIndex();
        _store.Save(_profile);
        _currentPageIndex = -1;
        ProfileLoaded?.Invoke(this, EventArgs.Empty);
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
        // 相对路径优先；绝对路径只按完整路径匹配，避免被同名曲目误解析
        AudioTrack? track = Path.IsPathRooted(fileNameOrRelativePath)
            ? _profile?.Tracks.FirstOrDefault(t =>
                string.Equals(t.FilePath, fileNameOrRelativePath, StringComparison.OrdinalIgnoreCase))
            : _profile?.Tracks.FirstOrDefault(t =>
                    string.Equals(t.RelativePath, fileNameOrRelativePath, StringComparison.OrdinalIgnoreCase))
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