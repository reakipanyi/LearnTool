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
            Regex regex;
            try { regex = new Regex(rule.FileNamePattern, RegexOptions.IgnoreCase); }
            catch (ArgumentException) { _diagnostics.Add($"规则 '{rule.Name}' 正则非法，已跳过"); continue; }

            var hits = new List<(AudioTrack Track, int? Value)>();
            foreach (var track in tracks)
            {
                var m = regex.Match(track.RelativePath);
                if (!m.Success) continue;
                hits.Add((track, ExtractValue(m, rule)));
            }
            if (hits.Count == 0) continue;

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
                var track = tracks.FirstOrDefault(t => string.Equals(t.FileName, Path.GetFileName(kv.Value), StringComparison.OrdinalIgnoreCase)
                        || string.Equals(t.RelativePath, kv.Value, StringComparison.OrdinalIgnoreCase))
                    ?? new AudioTrack { FilePath = Path.Combine(Path.GetDirectoryName(pdfPath) ?? "", kv.Value), FileName = Path.GetFileName(kv.Value), RelativePath = kv.Value };
                track.IsManual = true;
                pageIndex[kv.Key] = track;
            }

        for (int p = 0; p < pageCount; p++)
            if (!pageIndex.ContainsKey(p)) result.UnmatchedPages.Add(p);

        return result;
    }

    private static int? ExtractValue(Match m, AudioMatchRule rule)
    {
        if (!string.IsNullOrEmpty(rule.CaptureGroupName) && m.Groups[rule.CaptureGroupName].Success)
            return int.TryParse(m.Groups[rule.CaptureGroupName].Value, out var n) ? n : null;
        var idx = rule.CaptureGroupIndex ?? 1;
        if (idx >= 1 && idx < m.Groups.Count && m.Groups[idx].Success)
            return int.TryParse(m.Groups[idx].Value, out var c) ? c : null;
        return null;
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