using LearningAssistant.Models.Pdf;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioMatcher : IPdfAudioMatcher
{
    private readonly List<string> _diagnostics = new();
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides,
        PdfAudioMatchMode matchMode = PdfAudioMatchMode.PerPage, string? singleAudioFile = null)
    {
        _diagnostics.Clear();
        if (matchMode == PdfAudioMatchMode.SingleAudio)
            return MatchSingleAudio(pdfPath, pageCount, tracks, singleAudioFile);

        var result = new AudioMatchResult { Tracks = tracks.ToList() };
        var pageIndex = result.PageIndex;
        var priorityMap = new Dictionary<int, int>(); // page -> assigned priority

        var enabledRules = rules.Where(r => r.Enabled).OrderByDescending(r => r.Priority).ToList();
        // 同名音频（或目录内唯一的音频）代表整本书，优先级应高于“文件名数字猜页码”，
        // 否则 01_Vegetables.mp3 会被错误塞到当前书的第 1 页
        var wholeBook = FindWholeBookTrack(pdfPath, tracks, enabledRules.Count > 0, out var wholeBookIsSameName);
        // 没有任何启用的规则时按文件名数字兜底，避免首次使用全部显示“未匹配”
        if (enabledRules.Count == 0 && wholeBook == null)
            ApplyDefaultMatching(pageIndex, pageCount, tracks);

        foreach (var rule in enabledRules)
        {
            var regex = PdfAudioPatternCompiler.TryCompile(rule, out var patternDiagnostic);
            if (patternDiagnostic != null)
                _diagnostics.Add(patternDiagnostic);
            if (regex == null) continue;

            var hits = new List<(AudioTrack Track, int? Value)>();
            foreach (var track in tracks)
            {
                // 文件名优先，避免上级目录中的数字先被匹配；文件名不中时再回退完整相对路径
                var m = regex.Match(track.FileName);
                if (!m.Success) m = regex.Match(track.RelativePath);
                if (!m.Success) continue;
                hits.Add((track, ExtractValue(m, rule, track)));
            }

            if (hits.Count == 0)
            {
                _diagnostics.Add($"规则「{rule.Name}」未匹配到任何音频（示例：{SampleTrackNames(tracks)}）");
                continue;
            }

            if (rule.MapMode == AudioPageMapMode.Custom)
            {
                _diagnostics.Add($"规则「{rule.Name}」使用了未实现的 Custom 模式，已跳过");
                continue;
            }


            if (rule.MapMode == AudioPageMapMode.Sequence)
            {
                var ordered = hits.OrderBy(h => h.Value ?? 0).ThenBy(h => h.Track.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
                for (int i = 0; i < ordered.Count; i++)
                    Assign(pageIndex, priorityMap, i + rule.PageOffset, pageCount, ordered[i].Track, rule.Priority);
                continue;
            }

            // 命中了文件却提取不到页码（如整本同名音频、无数字文件名）时按文件名顺序兜底，
            // 避免规则表里看着已启用、结果却是全部未匹配
            if (hits.All(h => h.Value == null))
            {
                var ordered = hits.OrderBy(h => h.Track.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (rule.MapMode == AudioPageMapMode.Range)
                    {
                        for (int p = 0; p < rule.PagesPerUnit; p++)
                            Assign(pageIndex, priorityMap, i * rule.PagesPerUnit + rule.PageOffset + p,
                                pageCount, ordered[i].Track, rule.Priority);
                    }
                    else
                    {
                        Assign(pageIndex, priorityMap, i + rule.PageOffset, pageCount, ordered[i].Track, rule.Priority);
                    }
                }
                _diagnostics.Add($"规则「{rule.Name}」未从文件名提取到页码，已按文件名顺序依次匹配");
                continue;
            }

            foreach (var h in hits)
            {
                if (h.Value is not int v) continue;
                var pages = rule.MapMode switch
                {
                    AudioPageMapMode.PageIndex => new[] { v - 1 + rule.PageOffset },
                    AudioPageMapMode.Range => Enumerable.Range((v - 1) * rule.PagesPerUnit + rule.PageOffset, rule.PagesPerUnit),
                    _ => Array.Empty<int>()
                };
                foreach (var p in pages) Assign(pageIndex, priorityMap, p, pageCount, h.Track, rule.Priority);
            }
        }

        // 同名音频或目录内唯一音频视为整篇音频，补齐规则未覆盖的页码
        ApplyWholeBookFallback(pageCount, wholeBook, wholeBookIsSameName, pageIndex);

        if (manualOverrides != null)
            foreach (var kv in manualOverrides)
            {
                if (kv.Key < 0 || kv.Key >= pageCount) continue;
                // 相对路径优先；绝对路径只按完整路径匹配，避免目录外文件被同名曲目顶替
                var track = Path.IsPathRooted(kv.Value)
                    ? tracks.FirstOrDefault(t => string.Equals(t.FilePath, kv.Value, StringComparison.OrdinalIgnoreCase))
                    : tracks.FirstOrDefault(t => string.Equals(t.RelativePath, kv.Value, StringComparison.OrdinalIgnoreCase))
                      ?? tracks.FirstOrDefault(t => string.Equals(t.FileName, Path.GetFileName(kv.Value), StringComparison.OrdinalIgnoreCase));
                track ??= new AudioTrack
                {
                    FilePath = Path.IsPathRooted(kv.Value)
                        ? kv.Value
                        : Path.Combine(Path.GetDirectoryName(pdfPath) ?? "", kv.Value),
                    FileName = Path.GetFileName(kv.Value),
                    RelativePath = kv.Value
                };
                track.IsManual = true;
                pageIndex[kv.Key] = track;
                // 手动指定的音频可能不在扫描列表中，需补入曲目集合，否则列表/下一曲无法访问
                if (!result.Tracks.Exists(t => string.Equals(t.FilePath, track.FilePath, StringComparison.OrdinalIgnoreCase)))
                    result.Tracks.Add(track);
            }

        for (int p = 0; p < pageCount; p++)
            if (!pageIndex.ContainsKey(p)) result.UnmatchedPages.Add(p);

        return result;
    }

    /// <summary>
    /// 整篇单音频：只保留选定的那一个音频，并映射到全部页；忽略按页规则与手动覆盖。
    /// </summary>
    private AudioMatchResult MatchSingleAudio(string pdfPath, int pageCount,
        IReadOnlyList<AudioTrack> tracks, string? singleAudioFile)
    {
        var result = new AudioMatchResult();

        if (string.IsNullOrWhiteSpace(singleAudioFile))
        {
            _diagnostics.Add("当前为整篇单音频模式，但尚未选择音频文件");
            return result;
        }

        var track = ResolveSingleTrack(pdfPath, tracks, singleAudioFile);
        track.IsSingleAudio = true;
        result.Tracks = new List<AudioTrack> { track };
        track.MappedPageStart = 0;
        track.MappedPageEnd = Math.Max(0, pageCount - 1);

        for (int p = 0; p < pageCount; p++)
            result.PageIndex[p] = track;

        return result;
    }

    /// <summary>按相对路径、完整路径、文件名的顺序解析整篇音频，未命中时按给定路径构造曲目。</summary>
    private static AudioTrack ResolveSingleTrack(string pdfPath, IReadOnlyList<AudioTrack> tracks, string value)
    {
        var track = Path.IsPathRooted(value)
            ? tracks.FirstOrDefault(t => string.Equals(t.FilePath, value, StringComparison.OrdinalIgnoreCase))
            : tracks.FirstOrDefault(t => string.Equals(t.RelativePath, value, StringComparison.OrdinalIgnoreCase))
              ?? tracks.FirstOrDefault(t => string.Equals(t.FileName, Path.GetFileName(value), StringComparison.OrdinalIgnoreCase));

        return track ?? new AudioTrack
        {
            FilePath = Path.IsPathRooted(value)
                ? value
                : Path.Combine(Path.GetDirectoryName(pdfPath) ?? "", value),
            FileName = Path.GetFileName(value),
            RelativePath = value
        };
    }

    /// <summary>
    /// 无启用规则时的默认匹配：取文件名中的第一个数字作为页码（1 起）。
    /// 只在文件名中查找，避免上级目录的数字干扰；同页冲突时保留先扫描到的曲目。
    /// </summary>
    private void ApplyDefaultMatching(Dictionary<int, AudioTrack> pageIndex, int pageCount,
        IReadOnlyList<AudioTrack> tracks)
    {
        if (tracks.Count == 0)
        {
            _diagnostics.Add("目录下没有音频文件，自动匹配未执行");
            return;
        }

        _diagnostics.Add("未配置启用的匹配规则，已按文件名中的第一个数字自动匹配页码");

        foreach (var track in tracks)
        {
            // 只在去掉扩展名后的文件名里找数字，避免把 .mp3 的 3 当成页码
            var match = Regex.Match(Path.GetFileNameWithoutExtension(track.FileName), @"\d+");
            if (!match.Success || !int.TryParse(match.Value, out var number)) continue;

            var page = number - 1;
            if (page < 0 || page >= pageCount) continue;
            if (pageIndex.ContainsKey(page)) continue;

            pageIndex[page] = track;
            track.MappedPageStart = page;
            track.MappedPageEnd = page;
        }
    }

    /// <summary>
    /// 确定可作为整篇音频的曲目：优先与 PDF 同名的音频；
    /// 没有任何启用规则且目录只有一条无数字音频时，也按整篇处理。
    /// 已有规则且未显式同名时不做推断，避免覆盖用户规则的本意。
    /// </summary>
    private static AudioTrack? FindWholeBookTrack(string pdfPath, IReadOnlyList<AudioTrack> tracks,
        bool hasRules, out bool isSameName)
    {
        isSameName = false;
        if (tracks.Count == 0) return null;

        var sameName = FindSameNameTracks(pdfPath, tracks);
        if (sameName.Count == 1)
        {
            isSameName = true;
            return sameName[0];
        }

        if (!hasRules && tracks.Count == 1
            && !Regex.IsMatch(Path.GetFileNameWithoutExtension(tracks[0].FileName) ?? "", @"\d"))
            return tracks[0];

        return null;
    }

    /// <summary>把整篇音频补齐到仍然未匹配的页码。</summary>
    private void ApplyWholeBookFallback(int pageCount, AudioTrack? track, bool isSameName,
        Dictionary<int, AudioTrack> pageIndex)
    {
        if (track == null || pageCount <= 0) return;

        int filled = 0;
        for (int p = 0; p < pageCount; p++)
        {
            if (pageIndex.ContainsKey(p)) continue;
            pageIndex[p] = track;
            filled++;
        }
        if (filled == 0) return;

        track.MappedPageStart = 0;
        track.MappedPageEnd = pageCount - 1;
        _diagnostics.Add(isSameName
            ? $"音频「{track.FileName}」与 PDF 同名，已作为整篇音频匹配到全部 {pageCount} 页"
            : $"目录中只有 1 个音频「{track.FileName}」，已作为整篇音频匹配到全部 {pageCount} 页");
    }

    /// <summary>
    /// 找出与 PDF 同名的音频：先按去掉扩展名的完全同名匹配；
    /// 没有完全同名时退化为归一化比较（忽略空格、下划线、连字符与大小写），
    /// 例如「02_I Go.mp3」可以匹配「02_IGo.pdf」。
    /// </summary>
    public static List<AudioTrack> FindSameNameTracks(string pdfPath, IReadOnlyList<AudioTrack> tracks)
    {
        if (string.IsNullOrWhiteSpace(pdfPath) || tracks.Count == 0) return new List<AudioTrack>();

        var pdfStem = Path.GetFileNameWithoutExtension(pdfPath);
        var exact = tracks
            .Where(t => string.Equals(Path.GetFileNameWithoutExtension(t.FileName) ?? "",
                pdfStem ?? "", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (exact.Count > 0) return exact;

        var pdfKey = NormalizeBookName(pdfPath);
        if (pdfKey.Length == 0) return new List<AudioTrack>();
        return tracks
            .Where(t => string.Equals(NormalizeBookName(t.FileName), pdfKey, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// 书名归一化：去掉扩展名、空白与常见分隔符后转小写，用于宽松的同名音频识别。
    /// </summary>
    public static string NormalizeBookName(string? nameOrPath)
    {
        if (string.IsNullOrWhiteSpace(nameOrPath)) return string.Empty;
        var stem = Path.GetFileNameWithoutExtension(nameOrPath);
        if (string.IsNullOrEmpty(stem)) return string.Empty;

        var builder = new StringBuilder(stem.Length);
        foreach (var ch in stem)
        {
            if (char.IsWhiteSpace(ch) || ch == '_' || ch == '-' || ch == '·') continue;
            builder.Append(char.ToLowerInvariant(ch));
        }
        return builder.ToString();
    }

    private static int? ExtractValue(Match m, AudioMatchRule rule, AudioTrack track)
    {
        if (!string.IsNullOrEmpty(rule.CaptureGroupName))
        {
            var named = m.Groups[rule.CaptureGroupName];
            if (named.Success && int.TryParse(named.Value, out var namedValue))
                return namedValue;
        }

        if (rule.CaptureGroupIndex is int idx)
        {
            if (idx >= 1 && idx < m.Groups.Count && m.Groups[idx].Success
                && int.TryParse(m.Groups[idx].Value, out var configured))
                return configured;
        }
        else if (m.Groups.Count > 1 && m.Groups[1].Success
                 && int.TryParse(m.Groups[1].Value, out var firstGroup))
        {
            return firstGroup;
        }

        // 没有可用的捕获组时，退化为取文件名（去扩展名）中的第一个数字，
        // 避免“明明匹配上了却因为没写捕获组而映射不到页码”
        var fallback = Regex.Match(Path.GetFileNameWithoutExtension(track.FileName), @"\d+");
        if (fallback.Success && int.TryParse(fallback.Value, out var number))
            return number;

        return null;
    }

    private static string SampleTrackNames(IReadOnlyList<AudioTrack> tracks)
    {
        if (tracks.Count == 0) return "目录下没有音频文件";

        var names = tracks.Take(3)
            .Select(t => string.IsNullOrEmpty(t.FileName) ? t.RelativePath : t.FileName);
        return string.Join("、", names);
    }

    private static void Assign(Dictionary<int, AudioTrack> pageIndex, Dictionary<int, int> priorityMap,
        int page, int pageCount, AudioTrack track, int priority)
    {
        if (page < 0 || page >= pageCount) return;
        if (priorityMap.TryGetValue(page, out var existing) && existing >= priority) return;
        priorityMap[page] = priority;
        pageIndex[page] = track;
        track.MappedPageStart = page; track.MappedPageEnd = page;
    }
}