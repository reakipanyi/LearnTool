using LearningAssistant.Abstractions.Media;
using LearningAssistant.Models.Audio;
using LearningAssistant.Models.Pdf;
using LearningAssistant.Platform.Media;
using LearningAssistant.Services.Audio;
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
    private readonly SentenceTimelineService? _timelineService;
    private readonly AudioBoundaryOverlayStore? _overlayStore;
    private readonly ShadowingBoundaryApplyService? _shadowingApplyService;
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

    /// <summary>跟读打点期间锁定的页码；非空时翻页只提示、不切换音频。</summary>
    public int? PageLock { get; set; }

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
            if (_profile?.PlaybackScope == PlaybackScope.WholeBook) return false;
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
            if (_profile?.PlaybackScope == PlaybackScope.WholeBook) return false;
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

    /// <summary>播放设置（范围 / 跟随翻页 / 切歌同步）变化后触发，供侧栏与播放器同步开关</summary>
    public event EventHandler? SettingsChanged;

    private const int PageDebounceMs = 250;
    private readonly SynchronizationContext? _syncContext = SynchronizationContext.Current;
    private CancellationTokenSource? _pageChangeCts;

    private CancellationTokenSource? _timelineCts;
    private AudioTimelineOptions _timelineOptions = new();
    private AudioSentenceTimeline? _timeline;
    private AudioBoundaryOverlay? _overlay;
    private IReadOnlyList<AudioPageAnchor> _pageAnchors = Array.Empty<AudioPageAnchor>();
    private bool _pointSentenceMode = true;
    private bool _showPageLayer = true;
    private bool _sentenceTimelineEnabled = true;
    private string _timelinePreset = AudioTimelinePresets.Fine;
    private readonly List<AudioBoundaryOverlay> _overlayHistory = new();
    private AudioSentenceSpan? _previewSpan;
    /// <summary>预览/单句循环期间禁止把“听一句”的位置写成阅读进度。</summary>
    private bool _suppressProgressSave;

    /// <summary>当前曲目的句子时间轴（已套用手动修正）；分析中为空。</summary>
    public AudioSentenceTimeline? Timeline => _timeline;

    /// <summary>当前曲目的页码锚点；整本单音频才有意义。</summary>
    public IReadOnlyList<AudioPageAnchor> PageAnchors => _pageAnchors;

    /// <summary>整本单音频模式（页码层只在此模式显示）。</summary>
    public bool IsSingleAudio => _profile?.MatchMode == PdfAudioMatchMode.SingleAudio;

    /// <summary>页码层覆盖率：已校准页数 / 总页数。</summary>
    public double PageAnchorCoverage => _pageCount > 0
        ? Math.Clamp(_pageAnchors.Count / (double)_pageCount, 0, 1)
        : 0;

    /// <summary>手动覆盖层与源音频指纹不一致（边界可能偏移）。</summary>
    public bool IsOverlayStale { get; private set; }

    public bool SentenceTimelineEnabled => _sentenceTimelineEnabled;

    public bool PointSentenceMode => _pointSentenceMode;

    public bool ShowPageLayer => _showPageLayer;

    public string TimelinePreset => _timelinePreset;

    /// <summary>时间轴分析进度 0..1；-1 表示未在分析（完成或未开始）。</summary>
    public double TimelineProgress { get; private set; } = -1;

    /// <summary>时间轴发布（含清空）后触发，供播放条刷新刻度。</summary>
    public event EventHandler<AudioSentenceTimeline?>? TimelineChanged;

    public event EventHandler<double>? TimelineProgressChanged;

    /// <summary>单句预览开始/结束时触发；参数为当前预览句，结束后为 null。</summary>
    public event EventHandler<AudioSentenceSpan?>? PreviewChanged;

    /// <summary>点击页码刻度后请求正文跳页。</summary>
    public event EventHandler<int>? PageNavigationRequested;

    /// <summary>手动修正已写入覆盖层（用于一次性的“跟读切片仍旧”提示）。</summary>
    public event EventHandler? BoundaryOverlayChanged;

    public bool IsPreviewing => _previewSpan != null;

    public AudioSentenceSpan? PreviewSpan => _previewSpan;

    /// <summary>单句循环：预览到句尾后自动重播同一句，直到取消或关闭。</summary>
    public bool LoopSentence { get; set; }

    /// <summary>当前断句参数；切换灵敏度后按新参数重算当前曲目。</summary>
    public AudioTimelineOptions TimelineOptions => _timelineOptions;

    /// <summary>切换断句灵敏度预设（只存预设名）并重新分析当前曲目。</summary>
    public void SetTimelinePreset(string preset)
    {
        _timelinePreset = AudioTimelinePresets.Normalize(preset);
        _timelineOptions = AudioTimelinePresets.Create(_timelinePreset);
        if (_profile != null)
        {
            _profile.AudioTimelinePreset = _timelinePreset;
            SaveProfile();
        }
        if (_loadedTrack != null) StartTimeline(_loadedTrack, forceReanalyze: true);
    }

    public void SetPointSentenceMode(bool enabled)
    {
        if (_pointSentenceMode == enabled) return;
        _pointSentenceMode = enabled;
        if (_profile != null)
        {
            _profile.PointSentenceMode = enabled;
            SaveProfile();
        }
    }

    public void SetShowPageLayer(bool enabled)
    {
        if (_showPageLayer == enabled) return;
        _showPageLayer = enabled;
        if (_profile != null)
        {
            _profile.ShowPageLayer = enabled;
            SaveProfile();
        }
    }

    /// <summary>忽略缓存重新断句。</summary>
    public void ReanalyzeTimeline()
    {
        if (_loadedTrack != null) StartTimeline(_loadedTrack, forceReanalyze: true);
    }

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
        IPdfAudioProfileStore store, ISubtitleParser subtitleParser,
        SentenceTimelineService? timelineService = null,
        AudioBoundaryOverlayStore? overlayStore = null,
        ShadowingBoundaryApplyService? shadowingApplyService = null)
    {
        _logger = logger;
        _matcher = matcher;
        _scanner = scanner;
        _store = store;
        _timelineService = timelineService;
        _overlayStore = overlayStore;
        _shadowingApplyService = shadowingApplyService;
        _playback = new PdfAudioPlaybackService(new NAudioMediaPlayer(subtitleParser));
        _playback.Player.PlaybackFinished += OnPlaybackFinished;
        _playback.Player.OutputDeviceChanged += OnOutputDeviceChanged;
        _playback.Player.PlaybackRangeCompleted += OnPlaybackRangeCompleted;
        if (_timelineService != null)
            _timelineService.TimelineChanged += OnTimelineChanged;
    }

    private int _loadGeneration;
    private bool _profileStoreWritable = true;
    /// <summary>档案解析失败信息；非空时禁止回写，避免覆盖损坏文件</summary>
    public string? LoadErrorMessage { get; private set; }
    /// <summary>异步加载/重新扫描完成后触发，供 UI 刷新</summary>
    public event EventHandler? ProfileLoaded;

    public async Task LoadProfileAsync(string pdfPath, int pageCount, CancellationToken cancellationToken = default)
    {
        var generation = ++_loadGeneration;
        // 切换到其它文档时先停掉上一本的音频，避免旧曲目继续出声
        bool documentChanged = !string.IsNullOrEmpty(_currentPdfPath)
            && !string.Equals(_currentPdfPath, pdfPath, StringComparison.OrdinalIgnoreCase);
        if (documentChanged) ResetPlaybackForDocumentSwitch();

        _currentPdfPath = pdfPath;
        _pageCount = pageCount;
        var loaded = _store.Load(pdfPath, pageCount);
        LoadErrorMessage = loaded.Error;
        _profileStoreWritable = loaded.Error == null;
        _profile = loaded.Profile ?? new PdfAudioProfile { PdfPath = pdfPath };
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

    /// <summary>切换文档时收尾：写回旧文档播放进度并停止播放，清空曲目相关状态。</summary>
    private void ResetPlaybackForDocumentSwitch()
    {
        SavePlaybackState();
        CancelPreview();
        ResetTimeline();
        _overlay = null;
        IsOverlayStale = false;
        _timeline = null;
        _pageAnchors = Array.Empty<AudioPageAnchor>();
        _playback.Stop();
        _loadedTrack = null;
        _result = null;
        _playlistIndex = -1;
        _currentPageIndex = -1;
        SetPageHint(null);
        TimelineChanged?.Invoke(this, null);
    }

    private void ApplyLoadedProfile(string pdfPath, int pageCount, List<AudioTrack> tracks, string fingerprint)
    {
        var profile = _profile!;
        FollowPage = profile.FollowPage;
        SyncPageOnTrackChange = profile.SyncPageOnTrackChange;
        _sentenceTimelineEnabled = profile.SentenceTimelineEnabled;
        _pointSentenceMode = profile.PointSentenceMode;
        _showPageLayer = profile.ShowPageLayer;
        _timelinePreset = AudioTimelinePresets.Normalize(profile.AudioTimelinePreset);
        _timelineOptions = AudioTimelinePresets.Create(_timelinePreset);
        // 倍速按书记忆；打开书籍时先把目标值同步给播放器，后续 Load 按该倍速搭建输出链
        _playback.SetSpeed(profile.PlaybackSpeed);
        ScopeCorrectionMessage = null;

        // 默认整篇听：未手动选过播放范围时，有同名/唯一整篇音频就直接进入整篇模式
        if (!profile.ScopeExplicit && string.IsNullOrWhiteSpace(profile.SingleAudioFile))
        {
            var auto = ResolveAutoSingleAudio(pdfPath, tracks);
            if (auto != null)
            {
                profile.SingleAudioFile = auto;
                profile.MatchMode = PdfAudioMatchMode.SingleAudio;
                profile.PlaybackScope = PlaybackScope.WholeBook;
            }
        }

        // A41：整篇单音频与“跟随翻页”互斥，打开时纠正并提示一次
        if (!string.IsNullOrWhiteSpace(profile.SingleAudioFile)
            && profile.PlaybackScope == PlaybackScope.FollowPage)
        {
            profile.PlaybackScope = PlaybackScope.WholeBook;
            ScopeCorrectionMessage = "已按整篇模式修正播放范围";
            SaveProfile();
        }

        // 每次打开都按当前规则重算：规则、手动覆盖与手动编辑过的档案修改必须立即生效，
        // 不再用扫描指纹命中旧缓存，避免“改了规则还是旧结果”
        _result = _matcher.Match(pdfPath, pageCount, tracks, profile.Rules, profile.ManualOverrides,
            profile.MatchMode, profile.SingleAudioFile);
        profile.Tracks = tracks;
        profile.ScanFingerprint = fingerprint;
        profile.PageCount = pageCount;
        PersistPageIndex();
        SaveProfile();
        _currentPageIndex = -1;
    }

    /// <summary>统一持久化入口：解析失败时禁止回写，写盘异常只记录不打断播放。</summary>
    private void SaveProfile()
    {
        if (_profile == null) return;
        if (!_profileStoreWritable)
        {
            _logger.LogWarning("跳过档案回写：{Error}", LoadErrorMessage);
            return;
        }
        try
        {
            _store.Save(_profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存音频档案失败");
        }
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
            SaveProfile();
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

        // 打点会话锁定当前页：翻页只提示，不切歌
        if (PageLock.HasValue)
        {
            if (pageIndex != PageLock.Value)
                SetPageHint($"打点中，已锁定 P{PageLock.Value + 1}");
            return;
        }

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
                SavePlaybackState();
                CancelPreview();
                ResetTimeline();
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
        // 换曲/切范围时旧预览必须作废，否则旧区间会在新曲目上继续生效
        CancelPreview();
        // 先取出上次记录再换曲，换曲后该字段会覆盖为当前曲目
        var savedTrack = _profile?.LastTrackPath;
        double savedSeconds = _profile?.LastPositionSeconds ?? 0;

        _playback.Load(track);
        _loadedTrack = track;
        _playlistIndex = IndexOfTrack(track);

        if (_profile != null)
        {
            var resume = TimeSpan.Zero;
            if (!string.IsNullOrWhiteSpace(savedTrack)
                && string.Equals(savedTrack, GetTrackKey(track), StringComparison.OrdinalIgnoreCase))
                resume = ClampResume(TimeSpan.FromSeconds(savedSeconds));
            if (resume > TimeSpan.Zero) _playback.Seek(resume);

            _profile.LastTrackPath = GetTrackKey(track);
            _profile.LastPositionSeconds = Math.Max(0, _playback.Player.Position.TotalSeconds);
            SaveProfile();
        }

        TrackChanged?.Invoke(this, track);
        LoadOverlay(track);
        StartTimeline(track);

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
        if (FollowPage != follow)
        {
            FollowPage = follow;
            if (_profile != null) _profile.FollowPage = follow;
            SaveProfile();
        }
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>切换“切歌同步页码”并写回档案。</summary>
    public void SetSyncPageOnTrackChange(bool sync)
    {
        if (SyncPageOnTrackChange != sync)
        {
            SyncPageOnTrackChange = sync;
            if (_profile != null) _profile.SyncPageOnTrackChange = sync;
            SaveProfile();
        }
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// 自动识别整篇音频：优先使用已保存的选择，否则取与 PDF 同名的音频；
    /// 目录里只有一条音频时也直接返回。识别不到返回 null，由调用方再让用户选择。
    /// </summary>
    public string? AutoResolveSingleAudio()
    {
        if (_profile == null || _currentPdfPath == null) return null;
        if (!string.IsNullOrWhiteSpace(_profile.SingleAudioFile)) return _profile.SingleAudioFile;

        var tracks = _result?.Tracks ?? _profile.Tracks;
        return ResolveAutoSingleAudio(_currentPdfPath, tracks);
    }

    /// <summary>从曲目列表里找出可作整篇音频的那一条：同名音频优先，其次目录只有一条音频。</summary>
    private string? ResolveAutoSingleAudio(string pdfPath, IReadOnlyList<AudioTrack> tracks)
    {
        if (tracks.Count == 0) return null;

        var sameName = PdfAudioMatcher.FindSameNameTracks(pdfPath, tracks);
        var track = sameName.Count == 1 ? sameName[0] : tracks.Count == 1 ? tracks[0] : null;
        return track == null ? null : ToProfilePath(track.FilePath);
    }

    /// <summary>
    /// 切换播放范围。整篇听需要提供整篇音频；按页听会移除整篇音频选择，
    /// 但保留按页绑定与匹配规则。
    /// </summary>
    public void SetPlaybackScope(PlaybackScope scope, string? singleAudioFile = null)
    {
        if (_profile == null) return;

        bool changed = false;
        if (scope == PlaybackScope.WholeBook)
        {
            if (!string.IsNullOrWhiteSpace(singleAudioFile))
            {
                _profile.SingleAudioFile = singleAudioFile;
                _profile.MatchMode = PdfAudioMatchMode.SingleAudio;
                changed = true;
            }
            // 没有整篇音频时无法进入整篇听，保持原范围
            if (string.IsNullOrWhiteSpace(_profile.SingleAudioFile)) return;
        }
        else
        {
            // 整篇单音频与按页听互斥，切回时只移除整篇选择，按页绑定不受影响
            if (!string.IsNullOrWhiteSpace(_profile.SingleAudioFile))
            {
                _profile.SingleAudioFile = null;
                changed = true;
            }
            if (_profile.MatchMode != PdfAudioMatchMode.PerPage)
            {
                _profile.MatchMode = PdfAudioMatchMode.PerPage;
                changed = true;
            }
        }

        if (_profile.PlaybackScope != scope)
        {
            _profile.PlaybackScope = scope;
            changed = true;
        }
        // 用户手动切换过播放范围后，打开书籍不再自动改回整篇听
        if (!_profile.ScopeExplicit)
        {
            _profile.ScopeExplicit = true;
            changed = true;
        }
        if (!changed) return;

        int page = _currentPageIndex;
        SaveProfile();
        SaveRulesAndRematch();

        // 范围切换后把播放器切到该范围对应的曲目，避免整篇模式下仍指向旧的按页曲目
        var target = scope == PlaybackScope.WholeBook
            ? _result?.Tracks.FirstOrDefault()
            : page >= 0 ? _result?.PageIndex.GetValueOrDefault(page) : null;
        if (target != null && !IsLoaded(target)) LoadCore(target, syncPage: false);

        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>清除指定页的手动绑定并回退到规则匹配结果。</summary>
    public void ClearPageOverride(int page)
    {
        if (_profile == null) return;
        if (!_profile.ManualOverrides.Remove(page)) return;
        _profile.PageIndex.Remove(page);
        SaveRulesAndRematch();
    }

    /// <summary>试听指定曲目：不改变绑定，也不因多页曲目跳到起始页。</summary>
    public bool Preview(AudioTrack track)
    {
        if (track == null) return false;
        if (!IsLoaded(track)) LoadCore(track, syncPage: false);
        if (IsPlaying) return true;
        FireAndForget(_playback.PlayAsync(), "PlayAsync");
        return true;
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

    /// <summary>曲目稳定标识：优先相对路径，外部目录本身就是绝对路径。</summary>
    private static string GetTrackKey(AudioTrack track)
        => string.IsNullOrWhiteSpace(track.RelativePath) ? track.FileName : track.RelativePath;

    /// <summary>恢复进度时的边界处理：距结尾 3 秒内视为已播完，下次从头开始。</summary>
    private TimeSpan ClampResume(TimeSpan position)
    {
        var duration = _playback.Player.Duration;
        if (position <= TimeSpan.Zero) return TimeSpan.Zero;
        if (duration <= TimeSpan.Zero) return position;
        if (duration - position <= TimeSpan.FromSeconds(3)) return TimeSpan.Zero;
        return position >= duration ? duration - TimeSpan.FromSeconds(1) : position;
    }

    #region 句子时间轴与单句预览

    /// <summary>
    /// 预览离给定位置最近的一句：跳到句首播到句尾自动暂停，不写阅读进度。
    /// continueAfter 为 true 时只跳句首并继续正常播放；无时间轴时退化为普通 seek。
    /// </summary>
    public bool PreviewNearest(TimeSpan position, bool continueAfter = false)
    {
        var span = FindNearestSpan((int)Math.Round(position.TotalMilliseconds));
        if (span == null)
        {
            Seek(position);
            return false;
        }

        return StartPreview(span, continueAfter);
    }

    private bool StartPreview(AudioSentenceSpan span, bool continueAfter)
    {
        var start = TimeSpan.FromMilliseconds(span.StartMs);
        var end = TimeSpan.FromMilliseconds(Math.Max(span.StartMs + 1, span.EndMs));
        var player = _playback.Player;

        if (continueAfter)
        {
            CancelPreview();
            _playback.Seek(start);
            if (player.State != MediaPlaybackState.Playing) Play();
            return true;
        }

        if (!player.SetPlaybackRange(start, end))
        {
            _logger.LogWarning("播放器不支持单句区间，预览退化为普通定位：{Track}", _loadedTrack?.FileName);
            _playback.Seek(start);
        }

        _previewSpan = span;
        _suppressProgressSave = true;
        PreviewChanged?.Invoke(this, span);

        switch (player.State)
        {
            case MediaPlaybackState.Paused:
                _playback.Resume();
                break;
            case MediaPlaybackState.Playing:
                break;
            default:
                FireAndForget(_playback.PlayAsync(), "Preview");
                break;
        }
        return true;
    }

    /// <summary>普通定位：同时结束预览并恢复阅读进度记录。</summary>
    public void Seek(TimeSpan position)
    {
        CancelPreview();
        _playback.Seek(position);
    }

    /// <summary>点击页码刻度：跳到页起点并请求正文同步页码。</summary>
    public void ActivatePageAnchor(AudioPageAnchor anchor)
    {
        if (anchor == null) return;
        Seek(TimeSpan.FromMilliseconds(Math.Max(0, anchor.StartMs)));
        PageNavigationRequested?.Invoke(this, anchor.PageIndex);
    }

    /// <summary>计算「把边界写回跟读切片」的变更清单，供 UI 确认；无时间轴时返回空计划。</summary>
    public ShadowingBoundaryApplyPlan PlanShadowingBoundaryApply()
    {
        if (_shadowingApplyService == null || _loadedTrack == null || _currentPdfPath == null || _timeline == null)
            return new ShadowingBoundaryApplyPlan();

        return _shadowingApplyService.Plan(_currentPdfPath, _loadedTrack, _timeline);
    }

    /// <summary>执行边界写回；返回实际更新的切片数。</summary>
    public Task<int> ApplyShadowingBoundaryAsync(
        ShadowingBoundaryApplyPlan plan,
        CancellationToken cancellationToken = default)
    {
        if (_shadowingApplyService == null || _currentPdfPath == null || plan.Changes.Count == 0)
            return Task.FromResult(0);

        return _shadowingApplyService.ApplyAsync(_currentPdfPath, plan.Changes, cancellationToken);
    }

    /// <summary>当前播放位置所在（或最近）的句子，供修正菜单定位。</summary>
    public AudioSentenceSpan? CurrentSentence
        => FindNearestSpan((int)Math.Round(_playback.Player.Position.TotalMilliseconds));

    /// <summary>拖动一个句子边界并写入手动覆盖层。</summary>
    public bool AdjustSentenceBoundary(AudioSentenceSpan span, bool isStart, int timeMs)
    {
        if (span == null || _timeline == null || _overlayStore == null) return false;
        int durationMs = _timeline.DurationMs;
        timeMs = Math.Clamp(timeMs, 0, durationMs);

        int newStart = isStart ? timeMs : span.StartMs;
        int newEnd = isStart ? span.EndMs : timeMs;
        if (newEnd - newStart < 100) return false;

        if (!BeginOverlayEdit()) return false;

        string id = EnsureSpanId(span);
        _overlay!.Edits.RemoveAll(e => string.Equals(e.Id, id, StringComparison.Ordinal));
        _overlay.Edits.Add(new AudioSentenceEdit
        {
            Id = id,
            StartMs = newStart,
            EndMs = newEnd,
            Kind = "adjust"
        });
        return CommitOverlay();
    }

    /// <summary>把当前句与下一句合并为一句。</summary>
    public bool MergeWithNext(AudioSentenceSpan span)
    {
        var sentences = _timeline?.Sentences;
        if (span == null || sentences == null || _overlayStore == null) return false;

        int index = sentences.IndexOf(span);
        if (index < 0)
            index = sentences.ToList().FindIndex(s => string.Equals(s.SegmentId, span.SegmentId, StringComparison.Ordinal));
        if (index < 0 || index >= sentences.Count - 1) return false;

        var next = sentences[index + 1];
        if (!BeginOverlayEdit()) return false;

        string id = EnsureSpanId(span);
        string nextId = EnsureSpanId(next);
        _overlay!.Edits.RemoveAll(e => string.Equals(e.Id, id, StringComparison.Ordinal)
                                       || string.Equals(e.Id, nextId, StringComparison.Ordinal));
        _overlay.Edits.Add(new AudioSentenceEdit
        {
            Id = id,
            StartMs = Math.Min(span.StartMs, next.StartMs),
            EndMs = Math.Max(span.EndMs, next.EndMs),
            Kind = "merge"
        });
        return CommitOverlay();
    }

    /// <summary>在播放位置把当前句拆成两句。</summary>
    public bool SplitAt(TimeSpan position)
    {
        if (_timeline == null || _overlayStore == null) return false;
        int ms = (int)Math.Round(position.TotalMilliseconds);
        var span = FindNearestSpan(ms);
        if (span == null || ms <= span.StartMs + 100 || ms >= span.EndMs - 100) return false;

        if (!BeginOverlayEdit()) return false;

        string id = EnsureSpanId(span);
        _overlay!.Edits.RemoveAll(e => string.Equals(e.Id, id, StringComparison.Ordinal));
        _overlay.Edits.Add(new AudioSentenceEdit { Id = id + ":a", StartMs = span.StartMs, EndMs = ms, Kind = "split" });
        _overlay.Edits.Add(new AudioSentenceEdit { Id = id + ":b", StartMs = ms, EndMs = span.EndMs, Kind = "split" });
        return CommitOverlay();
    }

    /// <summary>
    /// 在当前句内部插入一个边界：只记录前半段，后半段由剩余句自动保留。
    /// 用于「中间漏掉一小句」的长尾场景，比「拆分」少一条手动记录。
    /// </summary>
    public bool InsertBoundaryAt(TimeSpan position)
    {
        if (_timeline == null || _overlayStore == null) return false;
        int ms = (int)Math.Round(position.TotalMilliseconds);
        var span = FindNearestSpan(ms);
        if (span == null || ms <= span.StartMs + 100 || ms >= span.EndMs - 100) return false;

        if (!BeginOverlayEdit()) return false;

        string id = EnsureSpanId(span);
        _overlay!.Edits.RemoveAll(e => string.Equals(e.Id, id, StringComparison.Ordinal));
        _overlay.Edits.Add(new AudioSentenceEdit { Id = id + ":a", StartMs = span.StartMs, EndMs = ms, Kind = "insert" });
        return CommitOverlay();
    }

    /// <summary>撤销最近一次手动修正（会话内最多 20 步）。</summary>
    public bool UndoBoundaryEdit()
    {
        if (_overlayHistory.Count == 0 || _overlayStore == null || _timeline == null) return false;

        var snapshot = _overlayHistory[^1];
        _overlayHistory.RemoveAt(_overlayHistory.Count - 1);
        _overlay = snapshot;
        return CommitOverlay(pushHistory: false);
    }

    /// <summary>清除本曲目手动修正（不动自动断句缓存）。</summary>
    public bool ClearBoundaryOverlay()
    {
        if (_overlayStore == null || _timeline == null || _overlay == null) return false;
        if (!BeginOverlayEdit()) return false;

        _overlay.Edits.Clear();
        _overlay.PageAnchors.Clear();
        return CommitOverlay();
    }

    private bool BeginOverlayEdit()
    {
        if (_overlayStore == null || _timeline == null || _overlay == null) return false;

        _overlayHistory.Add(CloneOverlay(_overlay));
        if (_overlayHistory.Count > 20)
            _overlayHistory.RemoveAt(0);
        return true;
    }

    private bool CommitOverlay(bool pushHistory = true)
    {
        if (_overlayStore == null || _overlay == null || _loadedTrack == null) return false;
        string? pdfPath = _currentPdfPath ?? _profile?.PdfPath;
        if (string.IsNullOrWhiteSpace(pdfPath)) return false;

        _overlay.TrackKey = _loadedTrack.FileName;
        _overlay.Fingerprint = AudioTimelineStore.ComputeFingerprint(
            _loadedTrack.FilePath, _loadedTrack.DurationMs);

        try
        {
            _overlayStore.Save(pdfPath, _loadedTrack.FilePath, _overlay);
            IsOverlayStale = false;
            PublishTimeline(_timelineService?.Current);
            BoundaryOverlayChanged?.Invoke(this, EventArgs.Empty);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存手动断句修正失败：{Track}", _loadedTrack.FileName);
            if (pushHistory && _overlayHistory.Count > 0)
                _overlayHistory.RemoveAt(_overlayHistory.Count - 1);
            return false;
        }
    }

    private static AudioBoundaryOverlay CloneOverlay(AudioBoundaryOverlay overlay) => new()
    {
        SchemaVersion = overlay.SchemaVersion,
        TrackKey = overlay.TrackKey,
        Fingerprint = overlay.Fingerprint,
        Edits = overlay.Edits.Select(e => new AudioSentenceEdit
        {
            Id = e.Id,
            StartMs = e.StartMs,
            EndMs = e.EndMs,
            Kind = e.Kind
        }).ToList(),
        PageAnchors = overlay.PageAnchors.Select(a => new AudioPageAnchor
        {
            PageIndex = a.PageIndex,
            StartMs = a.StartMs,
            Source = a.Source,
            IsStartEstimated = a.IsStartEstimated,
            Confidence = a.Confidence
        }).ToList()
    };

    private static string EnsureSpanId(AudioSentenceSpan span)
        => string.IsNullOrEmpty(span.SegmentId)
            ? $"p{span.PageIndex ?? -1}-{span.StartMs}-{span.EndMs}"
            : span.SegmentId;

    /// <summary>取消单句预览；播放位置保持不变，并恢复阅读进度记录。</summary>
    public void CancelPreview()
    {
        bool had = _previewSpan != null;
        _previewSpan = null;
        _suppressProgressSave = false;
        _playback.Player.ClearPlaybackRange();
        if (had) PreviewChanged?.Invoke(this, null);
    }

    private AudioSentenceSpan? FindNearestSpan(int positionMs)
    {
        var sentences = Timeline?.Sentences;
        if (sentences == null || sentences.Count == 0) return null;

        int low = 0, high = sentences.Count - 1, firstAfter = sentences.Count;
        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (sentences[mid].StartMs >= positionMs)
            {
                firstAfter = mid;
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }

        AudioSentenceSpan? best = null;
        int bestDistance = int.MaxValue;
        for (int i = Math.Max(0, firstAfter - 1); i <= Math.Min(sentences.Count - 1, firstAfter); i++)
        {
            var span = sentences[i];
            int distance = positionMs < span.StartMs
                ? span.StartMs - positionMs
                : positionMs >= span.EndMs ? positionMs - span.EndMs + 1 : 0;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = span;
            }
        }
        return best;
    }

    private void OnTimelineChanged(object? sender, AudioSentenceTimeline? timeline)
    {
        if (_disposed) return;
        PublishTimeline(timeline);
    }

    private void PublishTimeline(AudioSentenceTimeline? baseTimeline)
    {
        _timeline = ApplyOverlay(baseTimeline);
        RebuildPageAnchors();
        TimelineChanged?.Invoke(this, _timeline);
    }

    /// <summary>
    /// 把手动修正作为最高优先级层叠到自动时间轴上；只影响展示与预览，不写回 segments.json。
    /// </summary>
    private AudioSentenceTimeline? ApplyOverlay(AudioSentenceTimeline? baseTimeline)
    {
        if (baseTimeline == null) return null;
        var edits = _overlay?.Edits;
        if (edits == null || edits.Count == 0) return baseTimeline;

        var spans = edits
            .Where(e => e.EndMs > e.StartMs)
            .Select(e =>
            {
                var sourceSpan = FindBestOverlap(baseTimeline.Sentences, e.StartMs, e.EndMs);
                return new AudioSentenceSpan
                {
                    StartMs = e.StartMs,
                    EndMs = e.EndMs,
                    PageIndex = sourceSpan?.PageIndex,
                    BoundarySource = "manual",
                    Confidence = 1.0,
                    SegmentId = string.IsNullOrEmpty(e.Id) ? sourceSpan?.SegmentId : e.Id,
                    HighlightId = sourceSpan?.HighlightId,
                    Text = sourceSpan?.Text,
                    TextSource = sourceSpan?.TextSource
                };
            })
            .ToList();
        if (spans.Count == 0) return baseTimeline;

        // 手动层内部只处理“真重叠”；相邻（拆分后的两半）必须保持独立
        spans.Sort((a, b) => a.StartMs != b.StartMs
            ? a.StartMs.CompareTo(b.StartMs)
            : a.EndMs.CompareTo(b.EndMs));
        var manual = new List<AudioSentenceSpan>(spans.Count);
        foreach (var span in spans)
        {
            if (manual.Count > 0 && span.StartMs < manual[^1].EndMs)
                span.StartMs = manual[^1].EndMs;
            if (span.EndMs <= span.StartMs) continue;
            manual.Add(span);
        }

        var merged = new List<AudioSentenceSpan>(manual);
        foreach (var span in baseTimeline.Sentences)
            AddRemainders(merged, span, manual);

        merged.Sort((a, b) => a.StartMs != b.StartMs
            ? a.StartMs.CompareTo(b.StartMs)
            : a.EndMs.CompareTo(b.EndMs));
        for (int i = 0; i < merged.Count; i++)
            merged[i].Index = i + 1;

        var clone = new AudioSentenceTimeline
        {
            SchemaVersion = baseTimeline.SchemaVersion,
            TrackKey = baseTimeline.TrackKey,
            Fingerprint = baseTimeline.Fingerprint,
            DurationMs = baseTimeline.DurationMs,
            Analysis = baseTimeline.Analysis,
            Sentences = merged
        };
        return clone;
    }

    private static AudioSentenceSpan? FindBestOverlap(
        IReadOnlyList<AudioSentenceSpan> spans, int startMs, int endMs)
    {
        AudioSentenceSpan? best = null;
        int bestOverlap = 0;
        foreach (var span in spans)
        {
            int overlap = Math.Min(endMs, span.EndMs) - Math.Max(startMs, span.StartMs);
            if (overlap > bestOverlap)
            {
                bestOverlap = overlap;
                best = span;
            }
        }
        return best;
    }

    /// <summary>把手动区间从自动句上剔除；被完全覆盖的自动句直接消失，部分覆盖保留剩余片段。</summary>
    private static void AddRemainders(
        List<AudioSentenceSpan> output, AudioSentenceSpan span, List<AudioSentenceSpan> manual)
    {
        var pieces = new List<(int Start, int End)> { (span.StartMs, span.EndMs) };
        foreach (var cut in manual)
        {
            if (cut.EndMs <= span.StartMs || cut.StartMs >= span.EndMs) continue;

            var next = new List<(int Start, int End)>(pieces.Count + 1);
            foreach (var piece in pieces)
            {
                if (cut.EndMs <= piece.Start || cut.StartMs >= piece.End)
                {
                    next.Add(piece);
                    continue;
                }
                if (piece.Start < cut.StartMs) next.Add((piece.Start, cut.StartMs));
                if (cut.EndMs < piece.End) next.Add((cut.EndMs, piece.End));
            }
            pieces = next;
        }

        foreach (var piece in pieces)
        {
            if (piece.End <= piece.Start) continue;
            output.Add(new AudioSentenceSpan
            {
                StartMs = piece.Start,
                EndMs = piece.End,
                PageIndex = span.PageIndex,
                BoundarySource = span.BoundarySource,
                Confidence = span.Confidence,
                SegmentId = span.SegmentId,
                HighlightId = span.HighlightId,
                Text = span.Text,
                TextSource = span.TextSource
            });
        }
    }

    private void RebuildPageAnchors()
    {
        if (_timeline == null)
        {
            _pageAnchors = Array.Empty<AudioPageAnchor>();
            return;
        }

        _pageAnchors = AudioPageAnchorBuilder.Build(
            _timeline.DurationMs,
            _timeline.Sentences,
            _overlay?.PageAnchors,
            _timelineOptions,
            interpolateMissing: false);
    }

    private void LoadOverlay(AudioTrack track)
    {
        _overlay = null;
        IsOverlayStale = false;
        string? pdfPath = _currentPdfPath ?? _profile?.PdfPath;
        if (_overlayStore == null || string.IsNullOrWhiteSpace(pdfPath)) return;

        try
        {
            _overlay = _overlayStore.Load(pdfPath, track.FilePath);
            if (_overlay == null)
            {
                _overlay = new AudioBoundaryOverlay
                {
                    TrackKey = track.FileName,
                    Fingerprint = AudioTimelineStore.ComputeFingerprint(track.FilePath, track.DurationMs)
                };
                return;
            }

            string fingerprint = AudioTimelineStore.ComputeFingerprint(track.FilePath, track.DurationMs);
            IsOverlayStale = AudioBoundaryOverlayStore.IsFingerprintStale(_overlay, fingerprint);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "读取手动断句覆盖层失败：{Track}", track.FileName);
        }
    }

    /// <summary>把当前页起点设为给定播放位置（手动页码锚点），保存到覆盖层。</summary>
    public bool SetPageStart(int pageIndex, TimeSpan position)
    {
        if (_overlayStore == null || _loadedTrack == null) return false;
        string? pdfPath = _currentPdfPath ?? _profile?.PdfPath;
        if (string.IsNullOrWhiteSpace(pdfPath)) return false;

        int startMs = (int)Math.Round(position.TotalMilliseconds);
        _overlay ??= new AudioBoundaryOverlay
        {
            TrackKey = _loadedTrack.FileName,
            Fingerprint = AudioTimelineStore.ComputeFingerprint(_loadedTrack.FilePath, _loadedTrack.DurationMs)
        };

        _overlay.PageAnchors.RemoveAll(a => a.PageIndex == pageIndex);
        _overlay.PageAnchors.Add(new AudioPageAnchor
        {
            PageIndex = pageIndex,
            StartMs = Math.Max(0, startMs),
            Source = AudioPageAnchorBuilder.ManualSource,
            IsStartEstimated = false,
            Confidence = 1.0
        });

        try
        {
            _overlayStore.Save(pdfPath, _loadedTrack.FilePath, _overlay);
            IsOverlayStale = false;
            RebuildPageAnchors();
            TimelineChanged?.Invoke(this, _timeline);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "保存页码锚点失败：{Track}", _loadedTrack.FileName);
            return false;
        }
    }

    private void StartTimeline(AudioTrack track, bool forceReanalyze = false)
    {
        var service = _timelineService;
        if (service == null || !_sentenceTimelineEnabled)
        {
            _timeline = null;
            _pageAnchors = Array.Empty<AudioPageAnchor>();
            TimelineChanged?.Invoke(this, null);
            return;
        }

        ResetTimeline();

        int durationMs = (int)Math.Round(_playback.Player.Duration.TotalMilliseconds);
        if (durationMs <= 0) durationMs = track.DurationMs;
        SetTimelineProgress(0);
        if (durationMs <= 0)
        {
            SetTimelineProgress(-1);
            return;
        }

        var cts = new CancellationTokenSource();
        _timelineCts = cts;
        _ = RunTimelineAsync(track, durationMs, forceReanalyze, cts);
    }

    private void ResetTimeline()
    {
        var cts = _timelineCts;
        _timelineCts = null;
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
        }
        SetTimelineProgress(-1);
        _timelineService?.Reset();
    }

    private async Task RunTimelineAsync(AudioTrack track, int durationMs, bool forceReanalyze, CancellationTokenSource cts)
    {
        try
        {
            var progress = new Progress<double>(value =>
            {
                if (_disposed || _timelineCts != cts) return;
                SetTimelineProgress(value);
            });

            await _timelineService!
                .EnsureTimelineAsync(track, durationMs, _timelineOptions, forceReanalyze,
                    progress, cts.Token)
                .ConfigureAwait(true);

            if (_disposed || _timelineCts != cts) return;
            SetTimelineProgress(-1);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "句子时间轴加载失败：{Track}", track.FileName);
            if (_disposed || _timelineCts != cts) return;
            SetTimelineProgress(-1);
        }
    }

    private void SetTimelineProgress(double value)
    {
        TimelineProgress = value;
        TimelineProgressChanged?.Invoke(this, value);
    }

    /// <summary>播放区间结束事件可能来自音频线程，统一回到 UI 线程处理。</summary>
    private void OnPlaybackRangeCompleted(object? sender, PlaybackRangeCompletedEventArgs e)
    {
        if (_syncContext != null)
            _syncContext.Post(_ => HandlePlaybackRangeCompleted(e), null);
        else
            HandlePlaybackRangeCompleted(e);
    }

    private void HandlePlaybackRangeCompleted(PlaybackRangeCompletedEventArgs e)
    {
        if (_disposed) return;

        switch (e.Reason)
        {
            case PlaybackRangeEndReason.Replaced:
                // 新预览会自行接管状态，不在这里清空
                return;
            case PlaybackRangeEndReason.Completed:
                if (_previewSpan == null) return;
                // 单句循环：到句尾立即重播同一句，位置与“不写进度”状态保持不变
                if (LoopSentence)
                {
                    StartPreview(_previewSpan, continueAfter: false);
                    return;
                }
                _previewSpan = null;
                // 停在句尾属于“听一句”，仍不写阅读进度，直到用户做普通播放/定位
                PreviewChanged?.Invoke(this, null);
                break;
            default:
                bool had = _previewSpan != null;
                _previewSpan = null;
                _suppressProgressSave = false;
                if (had) PreviewChanged?.Invoke(this, null);
                break;
        }
    }

    #endregion

    /// <summary>把当前曲目与播放位置按书写回档案；暂停、播完、切曲与关闭时调用。</summary>
    public void SavePlaybackState()
    {
        if (_suppressProgressSave) return;
        if (_profile == null || _loadedTrack == null) return;
        var player = _playback.Player;
        double seconds = Math.Max(0, player.Position.TotalSeconds);
        // 已播到结尾：下次从头开始
        if (player.Duration > TimeSpan.Zero && player.Duration - player.Position <= TimeSpan.FromSeconds(3))
            seconds = 0;
        _profile.LastTrackPath = GetTrackKey(_loadedTrack);
        _profile.LastPositionSeconds = seconds;
        SaveProfile();
    }

    /// <summary>列出与当前目录文件名重叠率超过 80% 的旧档案，供用户确认重挂。</summary>
    public IReadOnlyList<PdfAudioDirectoryReattachCandidate> FindReattachCandidates()
        => _currentPdfPath == null
            ? Array.Empty<PdfAudioDirectoryReattachCandidate>()
            : _store.FindReattachCandidates(_currentPdfPath);

    /// <summary>把候选目录档案重挂到当前目录，并重新加载当前书。</summary>
    public async Task<bool> ReattachDirectoryAsync(string candidateProfilePath)
    {
        if (_currentPdfPath == null) return false;
        if (!_store.ReattachDirectory(_currentPdfPath, candidateProfilePath)) return false;
        await LoadProfileAsync(_currentPdfPath, _pageCount);
        return true;
    }

    /// <summary>设置并持久化本书播放倍速（0.5–2.0），同时立即应用到当前输出链。</summary>
    public void SetPlaybackSpeed(double speed)
    {
        if (_profile == null) return;
        speed = Math.Clamp(speed, 0.5, 2.0);
        if (Math.Abs(_profile.PlaybackSpeed - speed) < 0.001) return;
        _profile.PlaybackSpeed = speed;
        _playback.SetSpeed(speed);
        SaveProfile();
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Next() => Move(1);
    public void Prev() => Move(-1);

    private void Move(int delta)
    {
        if (_result == null || _result.Tracks.Count == 0) return;
        // 整篇听只有一条曲目，不提供上一曲/下一曲语义
        if (_profile?.PlaybackScope == PlaybackScope.WholeBook) return;

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

    /// <summary>系统默认播放设备变化：用新设备重新加载当前曲目，保持进度与播放状态。</summary>
    private void OnOutputDeviceChanged(object? sender, EventArgs e)
    {
        if (_disposed || _loadedTrack == null) return;
        var track = _loadedTrack;
        bool wasPlaying = _playback.Player.State == MediaPlaybackState.Playing;
        var position = _playback.Player.Position;
        _playback.Load(track);
        if (position > TimeSpan.Zero) _playback.Seek(position);
        if (wasPlaying) _ = _playback.PlayAsync();
        SetPageHint("系统默认播放设备已变化，已重新加载音频");
    }

    private void HandlePlaybackFinished()
    {
        if (_disposed) return;
        SavePlaybackState();
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
        // 预览结束后的继续播放属于正常播放，恢复阅读进度记录
        if (_previewSpan == null) _suppressProgressSave = false;

        switch (_playback.Player.State)
        {
            case MediaPlaybackState.Paused:
                _playback.Resume();
                return true;
            case MediaPlaybackState.Idle:
            case MediaPlaybackState.Stopped:
                // 已加载曲目从当前位置继续；已播完则从头开始
                // 切换文档后播放器仍残留旧文件的时长，必须确认当前曲目已加载，避免续播上一本的声音
                if (_loadedTrack != null && _playback.Player.Duration > TimeSpan.Zero)
                {
                    if (_playback.Player.Position >= _playback.Player.Duration - TimeSpan.FromMilliseconds(50))
                        _playback.Seek(TimeSpan.Zero);
                    _playback.Resume();
                    return true;
                }
                break;
        }

        var track = _loadedTrack ?? LastPlayedTrack() ?? CurrentPageTrack() ?? ResolvePlaybackFallback();
        if (track == null) return false;
        if (!IsLoaded(track)) Load(track);
        FireAndForget(_playback.PlayAsync(), "PlayAsync");
        return true;
    }

    /// <summary>档案中记录的上次播放曲目；曲目已不在扫描结果中时返回 null。</summary>
    public AudioTrack? LastPlayedTrack()
    {
        var key = _profile?.LastTrackPath;
        if (_profile == null || string.IsNullOrWhiteSpace(key)) return null;

        return _profile.Tracks.FirstOrDefault(t =>
            File.Exists(t.FilePath)
            && (string.Equals(t.FilePath, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.RelativePath, key, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.FileName, key, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// 播放按钮兜底：刚打开或切换文档时管理器可能还没有当前页码（档案异步加载完成会把页码重置为 -1），
    /// 此时整篇模式取整篇音频，逐页模式取第一页已匹配曲目，避免点播放毫无反应。
    /// </summary>
    private AudioTrack? ResolvePlaybackFallback()
    {
        if (_result == null || _result.Tracks.Count == 0)
            return null;

        string? singleFile = _profile?.SingleAudioFile;
        if (_profile?.MatchMode == PdfAudioMatchMode.SingleAudio
            && !string.IsNullOrWhiteSpace(singleFile))
        {
            var single = _result.Tracks.FirstOrDefault(t =>
                string.Equals(t.FilePath, singleFile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.RelativePath, singleFile, StringComparison.OrdinalIgnoreCase)
                || string.Equals(t.FileName, Path.GetFileName(singleFile), StringComparison.OrdinalIgnoreCase));
            if (single != null)
                return single;
        }

        return _result.PageIndex.OrderBy(kv => kv.Key).Select(kv => kv.Value).FirstOrDefault()
            ?? _result.Tracks[0];
    }

    /// <summary>幂等暂停：未在播放时不做任何事。</summary>
    public bool Pause()
    {
        if (!IsPlaying) return false;
        _playback.Pause();
        // SavePlaybackState 内部会在预览期间跳过写入
        SavePlaybackState();
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
        SaveProfile();
        await LoadProfileAsync(_currentPdfPath, _pageCount);
    }

    /// <summary>应用/清除书级规则补丁，并按“目录规则 + 补丁”重算生效规则。</summary>
    public void ApplyRulePatch(PdfAudioRulePatch? patch)
    {
        if (_profile == null) return;

        // 空补丁等同于完全继承，存 null 让档案更干净
        if (patch != null && !patch.ReplaceAll
            && patch.DisabledRuleIds.Count == 0 && patch.ExtraRules.Count == 0)
            patch = null;

        _profile.RulePatch = patch;
        _profile.Rules = PdfAudioRuleMerger.Merge(_profile.DirectoryRules, patch);
        SaveRulesAndRematch();
    }

    /// <summary>导出整个目录档案（不含缓存）。</summary>
    public PdfAudioExportResult ExportDirectory()
        => _currentPdfPath == null
            ? PdfAudioExportResult.Fail("请先打开 PDF")
            : _store.ExportDirectory(_currentPdfPath);

    /// <summary>导出当前书档案（不含缓存）。</summary>
    public PdfAudioExportResult ExportBook()
        => _currentPdfPath == null
            ? PdfAudioExportResult.Fail("请先打开 PDF")
            : _store.ExportBook(_currentPdfPath);

    /// <summary>读取当前目录档案原文，供应用内手动编辑。</summary>
    public PdfAudioExportResult ReadProfileJson()
        => _currentPdfPath == null
            ? PdfAudioExportResult.Fail("请先打开 PDF")
            : _store.ReadDirectoryJson(_currentPdfPath);

    /// <summary>保存手动编辑后的档案原文，并重新加载当前书使修改立即生效。</summary>
    public async Task<PdfAudioExportResult> SaveProfileJsonAsync(string json)
    {
        if (_currentPdfPath == null) return PdfAudioExportResult.Fail("请先打开 PDF");

        var result = _store.SaveDirectoryJson(_currentPdfPath, json);
        if (!result.Success) return result;

        await LoadProfileAsync(_currentPdfPath, _pageCount);
        return result;
    }

    /// <summary>解析导入内容并生成差异预览，不写盘。</summary>
    public PdfAudioImportPreview PreviewImport(string json)
        => _currentPdfPath == null
            ? PdfAudioImportPreview.Fail("请先打开 PDF")
            : _store.PreviewImport(_currentPdfPath, json);

    /// <summary>按预览结果合并写入并重新加载当前书。</summary>
    public async Task ApplyImportAsync(PdfAudioImportPreview preview)
    {
        if (_currentPdfPath == null) throw new InvalidOperationException("请先打开 PDF");
        if (!preview.Success) throw new InvalidOperationException(preview.Error ?? "导入预览无效");

        _store.ApplyImport(_currentPdfPath, preview);
        await LoadProfileAsync(_currentPdfPath, _pageCount);
    }

    /// <summary>规则或匹配方式变更后直接用已扫描曲目重算，无需再次扫描目录</summary>
    public void SaveRulesAndRematch()
    {
        if (_profile == null || _currentPdfPath == null) return;
        // 无补丁时 Rules 就是目录规则原文，保持 DirectoryRules 同步供补丁对话框使用
        if (_profile.RulePatch == null)
            _profile.DirectoryRules = _profile.Rules.Select(PdfAudioRuleMerger.Clone).ToList();
        _result = _matcher.Match(_currentPdfPath, _pageCount, _profile.Tracks,
            _profile.Rules, _profile.ManualOverrides,
            _profile.MatchMode, _profile.SingleAudioFile);
        PersistPageIndex();
        SaveProfile();
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
        _profile.DirectoryRules.Clear();
        _profile.RulePatch = null;
        _profile.ManualOverrides.Clear();
        _profile.PageIndex.Clear();
        _profile.SingleAudioFile = null;
        _profile.MatchMode = PdfAudioMatchMode.PerPage;
        _profile.PlaybackScope = PlaybackScope.FollowPage;
        _profile.ScopeExplicit = false;
        SaveRulesAndRematch();
    }

    public void SetManualOverride(int page, string file)
    {
        if (_profile == null) return;
        _profile.ManualOverrides[page] = file;
        // 同步缓存映射，避免指纹命中时恢复到手动指定前的旧结果
        _profile.PageIndex[page] = file;
        SaveProfile();
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
        // 关闭前把当前曲目位置写回，避免下次打开丢失进度
        SavePlaybackState();
        _disposed = true;
        _pageChangeCts?.Cancel();
        _pageChangeCts?.Dispose();
        _pageChangeCts = null;
        var timelineCts = _timelineCts;
        _timelineCts = null;
        if (timelineCts != null)
        {
            timelineCts.Cancel();
            timelineCts.Dispose();
        }
        if (_timelineService != null)
        {
            _timelineService.TimelineChanged -= OnTimelineChanged;
            _timelineService.Reset();
        }
        _playback.Player.PlaybackFinished -= OnPlaybackFinished;
        _playback.Player.OutputDeviceChanged -= OnOutputDeviceChanged;
        _playback.Player.PlaybackRangeCompleted -= OnPlaybackRangeCompleted;
        _playback.Stop();
        _playback.Player.Dispose();
    }
}