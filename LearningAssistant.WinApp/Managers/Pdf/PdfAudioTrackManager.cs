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
    private AudioTrack? _loadedTrack;
    private PdfAudioProfile? _profile;
    private string? _currentPdfPath;
    private int _currentPageIndex = -1;
    private int _pageCount;
    private bool _disposed;
    /// <summary>播放列表中的目标索引；上下曲按它累积，避免连点被合并或乱序</summary>
    private int _playlistIndex = -1;

    public PdfAudioPlaybackService Playback => _playback;
    public AudioMatchResult? Result => _result;
    public PdfAudioProfile? Profile => _profile;
    public int PageCount => _pageCount;
    public IReadOnlyList<string> Diagnostics => _matcher.Diagnostics;
    /// <summary>当前播放列表索引；无曲目时为 -1</summary>
    public int CurrentPlaylistIndex => _playlistIndex;
    public AudioTrack? CurrentTrack => _loadedTrack;
    public bool IsPlaying => _playback.Player.State == MediaPlaybackState.Playing;

    /// <summary>是否跟随翻页切换音频；关闭后翻页完全不动作</summary>
    public bool FollowPage { get; private set; } = true;
    /// <summary>上一曲/下一曲/列表选曲后，正文是否跳到曲目起始页；与 FollowPage 独立</summary>
    public bool SyncPageOnTrackChange { get; private set; } = true;
    /// <summary>播放列表循环方式；None 时首尾按钮禁用，不自动环绕</summary>
    public MediaLoopMode LoopMode { get; set; } = MediaLoopMode.None;

    /// <summary>请求正文跳到指定页（由宿主持有导航能力），翻页触发的切换不会触发</summary>
    public event EventHandler<int>? PageSyncRequested;

    /// <summary>打开档案时自动纠正非法组合的提示，供面板显示一次</summary>
    public string? ScopeCorrectionMessage { get; private set; }

    /// <summary>能否切到下一曲（列表末且未开启循环时为 false）</summary>
    public bool CanGoNext
    {
        get
        {
            int count = _result?.Tracks.Count ?? 0;
            if (count == 0) return false;
            return LoopMode == MediaLoopMode.RepeatAll || _playlistIndex < count - 1;
        }
    }

    /// <summary>能否切到上一曲（列表首且未开启循环时为 false）</summary>
    public bool CanGoPrev
    {
        get
        {
            int count = _result?.Tracks.Count ?? 0;
            if (count == 0 || _playlistIndex < 0) return false;
            return LoopMode == MediaLoopMode.RepeatAll || _playlistIndex > 0;
        }
    }

    /// <summary>切曲完成后触发（含上一曲/下一曲、列表选择、翻页切曲），供 UI 同步标题与高亮</summary>
    public event EventHandler<AudioTrack>? TrackChanged;

    /// <summary>当前页提示（无音频 / 文件缺失等），由播放器内联显示，不弹模态框</summary>
    public event EventHandler<string?>? PageHintChanged;
    public string? PageHint { get; private set; }

    private const int PageDebounceMs = 250;
    private readonly SynchronizationContext? _syncContext = SynchronizationContext.Current;
    private CancellationTokenSource? _pageChangeCts;

    /// <summary>用给定规则/匹配方式对当前已扫描曲目试算，不修改已生效的 profile</summary>
    public AudioMatchResult PreviewRules(IReadOnlyList<AudioMatchRule> rules,
        PdfAudioMatchMode matchMode = PdfAudioMatchMode.PerPage, string? singleAudioFile = null)
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
        return _matcher.Match(_currentPdfPath ?? "", _pageCount, tracks, rules, _profile?.ManualOverrides,
            matchMode, singleAudioFile);
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
        _playback.Player.PlaybackFinished += OnPlaybackFinished;
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
        _loadedTrack = null;
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
        FollowPage = profile.FollowPage;
        SyncPageOnTrackChange = profile.SyncPageOnTrackChange;
        ScopeCorrectionMessage = null;

        // A41：整篇单音频与“跟随翻页”互斥，打开时纠正并提示一次
        if (!string.IsNullOrWhiteSpace(profile.SingleAudioFile)
            && profile.PlaybackScope == PlaybackScope.FollowPage)
        {
            profile.PlaybackScope = PlaybackScope.WholeBook;
            ScopeCorrectionMessage = "已按整篇模式修正播放范围";
            _store.Save(profile);
        }

        var scanChanged = fingerprint != profile.ScanFingerprint || profile.Tracks.Count == 0;
        var pageCountChanged = profile.PageCount != pageCount;
        // 指纹与页数均未变化时复用缓存映射；否则重算，避免规则结果丢失或页数变化后映射越界
        // 整篇单音频模式缓存里只有一条曲目，直接重算更简单，也避免与切换后的选择不一致
        if (profile.MatchMode == PdfAudioMatchMode.SingleAudio
            || scanChanged || pageCountChanged || profile.PageIndex.Count == 0)
        {
            _result = _matcher.Match(pdfPath, pageCount, tracks, profile.Rules, profile.ManualOverrides,
                profile.MatchMode, profile.SingleAudioFile);
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
        // 整篇单音频每次都会重算，不需要把每一页都写进缓存
        if (_profile.MatchMode == PdfAudioMatchMode.SingleAudio)
        {
            _profile.PageIndex.Clear();
            return;
        }
        _profile.PageIndex = _result.PageIndex.ToDictionary(
            kv => kv.Key,
            kv => string.IsNullOrEmpty(kv.Value.RelativePath) ? kv.Value.FileName : kv.Value.RelativePath);
    }

    /// <summary>
    /// 翻页入口：只记录目标页并重置 250ms 计时器，页面稳定后才执行切换。
    /// 快速连翻时中间页的请求随取消令牌一并丢弃，不会连续切歌。
    /// </summary>
    public void OnPageChanged(int pageIndex)
    {
        _currentPageIndex = pageIndex;
        if (_result == null) return;

        _pageChangeCts?.Cancel();
        _pageChangeCts?.Dispose();
        _pageChangeCts = new CancellationTokenSource();
        _ = ApplyPageChangeAfterDelayAsync(pageIndex, _pageChangeCts.Token);
    }

    private async Task ApplyPageChangeAfterDelayAsync(int pageIndex, CancellationToken token)
    {
        try
        {
            await Task.Delay(PageDebounceMs, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (_syncContext != null)
            _syncContext.Post(_ => ApplyPageChange(pageIndex, token), null);
        else
            ApplyPageChange(pageIndex, token);
    }

    private void ApplyPageChange(int pageIndex, CancellationToken token)
    {
        if (_disposed || token.IsCancellationRequested) return;
        // 已翻到其它页，丢弃过期请求
        if (pageIndex != _currentPageIndex || _result == null) return;

        var target = _result.PageIndex.GetValueOrDefault(pageIndex);
        var decision = PlaybackDecision.Resolve(
            _profile?.PlaybackScope ?? PlaybackScope.FollowPage,
            follow: FollowPage,
            MapPlayerState(),
            _loadedTrack?.FilePath,
            target?.FilePath);

        switch (decision)
        {
            case PlaybackAction.LoadAndPlay:
                LoadCore(target!, syncPage: false);
                FireAndForget(_playback.PlayAsync(), "PlayAsync");
                break;
            case PlaybackAction.LoadOnly:
                LoadCore(target!, syncPage: false);
                break;
            case PlaybackAction.StopWithHint:
                _playback.Stop();
                _loadedTrack = null;
                _playlistIndex = -1;
                SetPageHint($"P{pageIndex + 1} 无音频 · 去绑定");
                break;
            case PlaybackAction.None:
            default:
                if (target != null) SetPageHint(null);
                break;
        }
    }

    /// <summary>把媒体播放器状态映射为翻页判定用的六态。</summary>
    private PdfPlayerState MapPlayerState()
    {
        switch (_playback.Player.State)
        {
            case MediaPlaybackState.Playing:
                return PdfPlayerState.Playing;
            case MediaPlaybackState.Paused:
                return PdfPlayerState.PausedByUser;
            case MediaPlaybackState.Stopped:
                return PdfPlayerState.Ended;
            default:
                // Load 完成但尚未播放：有媒体即视为可续播的暂停态，否则视为未加载
                return _playback.Player.Duration > TimeSpan.Zero
                    ? PdfPlayerState.PausedByUser
                    : PdfPlayerState.Idle;
        }
    }

    private void SetPageHint(string? hint)
    {
        if (string.Equals(PageHint, hint, StringComparison.Ordinal)) return;
        PageHint = hint;
        PageHintChanged?.Invoke(this, hint);
    }

    /// <summary>供 UI 在无音频时内联提示，不弹模态框。</summary>
    public void SetHint(string? hint) => SetPageHint(hint);

    /// <summary>加载音频并记录当前曲目；用户主动切曲时按开关同步正文页码。</summary>
    public void Load(AudioTrack track) => LoadCore(track, syncPage: true);

    private void LoadCore(AudioTrack track, bool syncPage)
    {
        _playback.Load(track);
        _loadedTrack = track;
        _playlistIndex = IndexOfTrack(track);
        TrackChanged?.Invoke(this, track);

        if (!syncPage || !SyncPageOnTrackChange) return;
        int page = FindFirstPageForTrack(track);
        if (page >= 0 && page != _currentPageIndex)
            PageSyncRequested?.Invoke(this, page);
    }

    private int FindFirstPageForTrack(AudioTrack track)
    {
        if (_result == null) return -1;
        int best = -1;
        foreach (var kv in _result.PageIndex)
        {
            if (!string.Equals(kv.Value.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase)) continue;
            if (best < 0 || kv.Key < best) best = kv.Key;
        }
        return best;
    }

    /// <summary>切换“跟随翻页”并写回档案。</summary>
    public void SetFollowPage(bool follow)
    {
        FollowPage = follow;
        if (_profile == null) return;
        _profile.FollowPage = follow;
        _store.Save(_profile);
    }

    /// <summary>切换“切歌同步页码”并写回档案。</summary>
    public void SetSyncPageOnTrackChange(bool sync)
    {
        SyncPageOnTrackChange = sync;
        if (_profile == null) return;
        _profile.SyncPageOnTrackChange = sync;
        _store.Save(_profile);
    }

    private int IndexOfTrack(AudioTrack track)
    {
        if (_result == null) return -1;
        return _result.Tracks.FindIndex(t =>
            string.Equals(t.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsLoaded(AudioTrack track)
        => _loadedTrack != null
           && string.Equals(_loadedTrack.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase);

    public void Next() => Move(1);
    public void Prev() => Move(-1);

    private void Move(int delta)
    {
        if (_result == null || _result.Tracks.Count == 0) return;

        int count = _result.Tracks.Count;
        // 以目标索引为基准累积：连点 3 次就前进 3 条，不合并、不丢弃
        int baseIndex = _playlistIndex;
        if (baseIndex < 0 && _result.PageIndex.TryGetValue(_currentPageIndex, out var current))
            baseIndex = IndexOfTrack(current);
        if (baseIndex < 0)
            baseIndex = delta > 0 ? -1 : 0; // 当前无音频：下一曲从首曲开始，上一曲从末曲开始

        int next = baseIndex + delta;
        if (next < 0 || next >= count)
        {
            // 未开启列表循环时到边界即止，按钮同步禁用，不做隐式环绕
            if (LoopMode != MediaLoopMode.RepeatAll) return;
            next = (next % count + count) % count;
        }

        var track = _result.Tracks[next];
        Load(track);
        FireAndForget(_playback.PlayAsync(), "PlayAsync");
    }

    /// <summary>播完一首后按循环设置处理；事件可能来自音频线程，统一切回 UI 线程。</summary>
    private void OnPlaybackFinished(object? sender, EventArgs e)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => HandlePlaybackFinished(), null);
        else
            HandlePlaybackFinished();
    }

    private void HandlePlaybackFinished()
    {
        if (_disposed) return;
        switch (LoopMode)
        {
            case MediaLoopMode.RepeatOne:
                _playback.Seek(TimeSpan.Zero);
                FireAndForget(_playback.PlayAsync(), "PlayAsync");
                break;
            case MediaLoopMode.RepeatAll:
                Move(1);
                break;
        }
    }

    /// <summary>幂等播放：已在播放时不做任何事，不依赖 UI 反向推断状态。</summary>
    public bool Play()
    {
        if (IsPlaying) return true;

        switch (_playback.Player.State)
        {
            case MediaPlaybackState.Paused:
                _playback.Resume();
                return true;
            case MediaPlaybackState.Idle:
            case MediaPlaybackState.Stopped:
                // 已加载曲目从当前位置继续；已播完则从头开始
                if (_playback.Player.Duration > TimeSpan.Zero)
                {
                    if (_playback.Player.Position >= _playback.Player.Duration - TimeSpan.FromMilliseconds(50))
                        _playback.Seek(TimeSpan.Zero);
                    _playback.Resume();
                    return true;
                }
                break;
        }

        var track = _loadedTrack ?? CurrentPageTrack();
        if (track == null) return false;
        if (!IsLoaded(track)) Load(track);
        FireAndForget(_playback.PlayAsync(), "PlayAsync");
        return true;
    }

    /// <summary>幂等暂停：未在播放时不做任何事。</summary>
    public bool Pause()
    {
        if (!IsPlaying) return false;
        _playback.Pause();
        return true;
    }

    /// <summary>切换播放/暂停；判定始终以播放器真实状态为准。</summary>
    public bool TogglePlayback() => IsPlaying ? Pause() : Play();

    /// <summary>兼容旧调用：语义等同 TogglePlayback。</summary>
    public bool PlayOrPause() => TogglePlayback();

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

    /// <summary>规则或匹配方式变更后直接用已扫描曲目重算，无需再次扫描目录</summary>
    public void SaveRulesAndRematch()
    {
        if (_profile == null || _currentPdfPath == null) return;
        _result = _matcher.Match(_currentPdfPath, _pageCount, _profile.Tracks,
            _profile.Rules, _profile.ManualOverrides,
            _profile.MatchMode, _profile.SingleAudioFile);
        PersistPageIndex();
        _store.Save(_profile);
        _currentPageIndex = -1;
        ProfileLoaded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 清空当前 PDF 的规则、手动匹配、缓存映射与单音频选择，恢复默认匹配。
    /// 只影响当前打开的 PDF，不会改动其它文件已保存的配置。
    /// </summary>
    public void ResetMatching()
    {
        if (_profile == null) return;
        _profile.Rules.Clear();
        _profile.ManualOverrides.Clear();
        _profile.PageIndex.Clear();
        _profile.SingleAudioFile = null;
        _profile.MatchMode = PdfAudioMatchMode.PerPage;
        SaveRulesAndRematch();
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

    private AudioTrack? CurrentPageTrack()
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
        _pageChangeCts?.Cancel();
        _pageChangeCts?.Dispose();
        _pageChangeCts = null;
        _playback.Player.PlaybackFinished -= OnPlaybackFinished;
        _playback.Stop();
        _playback.Player.Dispose();
    }
}