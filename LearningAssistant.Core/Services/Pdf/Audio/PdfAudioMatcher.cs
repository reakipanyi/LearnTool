using LearningAssistant.Models.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

public class PdfAudioMatcher : IPdfAudioMatcher
{
    private readonly List<string> _diagnostics = new();
    public IReadOnlyList<string> Diagnostics => _diagnostics;

    public AudioMatchResult Match(string pdfPath, int pageCount, IReadOnlyList<AudioTrack> tracks,
        IReadOnlyList<AudioMatchRule> rules, IReadOnlyDictionary<int, string>? manualOverrides)
    {
        _diagnostics.Clear();
        var result = new AudioMatchResult { Tracks = tracks.ToList() };
        var pageIndex = result.PageIndex;
        var priorityMap = new Dictionary<int, int>(); // page -> assigned priority

        foreach (var rule in rules.Where(r => r.Enabled).OrderByDescending(r => r.Priority))
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
            }
            else foreach (var h in hits)
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

        // 没有可用的捕获组时，退化为取文件名中的第一个数字，
        // 避免“明明匹配上了却因为没写捕获组而映射不到页码”
        var fallback = Regex.Match(track.FileName, @"\d+");
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