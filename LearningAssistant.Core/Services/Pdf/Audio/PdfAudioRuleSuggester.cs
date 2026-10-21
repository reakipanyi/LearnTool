using LearningAssistant.Models.Pdf;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Pdf;

public sealed class PdfAudioRuleSuggestion
{
    public AudioMatchRule Rule { get; init; } = new();
    public int OverrideCount { get; init; }
    public string? Note { get; init; }
}

/// <summary>把已手动指定的「页 → 音频」反推为一条正则规则，供用户确认后保存</summary>
public static class PdfAudioRuleSuggester
{
    private static readonly string[] AudioExts =
        { "mp3", "wav", "m4a", "aac", "flac", "ogg", "wma" };

    public static PdfAudioRuleSuggestion? Suggest(
        IReadOnlyDictionary<int, string> manualOverrides, IReadOnlyList<AudioTrack> tracks)
    {
        if (manualOverrides.Count == 0) return null;

        var items = new List<Item>();
        foreach (var kv in manualOverrides.OrderBy(k => k.Key))
        {
            var relativePath = Resolve(kv.Value, tracks)?.RelativePath ?? kv.Value;
            var stem = Path.GetFileNameWithoutExtension(relativePath);
            if (string.IsNullOrEmpty(stem)) continue;

            var matches = Regex.Matches(stem, @"\d+");
            if (matches.Count == 0) continue;
            var last = matches[^1];
            if (!int.TryParse(last.Value, out var number)) continue;

            items.Add(new Item
            {
                Page = kv.Key,
                Number = number,
                RelativePath = relativePath,
                Prefix = stem[..last.Index],
                Suffix = stem[(last.Index + last.Length)..]
            });
        }
        if (items.Count == 0) return null;

        var pattern = FindPattern(items);
        if (pattern == null) return null;

        var deltas = items.Select(i => i.Page - i.Number).ToList();
        AudioPageMapMode mode;
        int offset;
        string? note = null;

        if (deltas.Distinct().Count() == 1)
        {
            mode = AudioPageMapMode.PageIndex;
            offset = deltas[0];
        }
        else if (TrySequenceOffset(pattern, items, tracks, out var sequenceOffset))
        {
            mode = AudioPageMapMode.Sequence;
            offset = sequenceOffset;
        }
        else
        {
            mode = AudioPageMapMode.PageIndex;
            offset = deltas.GroupBy(d => d).OrderByDescending(g => g.Count()).First().Key;
            note = "页码与文件名数字不成固定偏移，已按最常见偏移生成，请核对试算结果";
        }

        if (items.Count < manualOverrides.Count)
            note = Combine(note, "部分手动匹配的文件名不含数字，未纳入规则");

        return new PdfAudioRuleSuggestion
        {
            Rule = new AudioMatchRule
            {
                Name = "由手动匹配归纳",
                FileNamePattern = pattern,
                CaptureGroupIndex = 1,
                MapMode = mode,
                PageOffset = offset,
                PagesPerUnit = 1,
                Enabled = true
            },
            OverrideCount = items.Count,
            Note = note
        };
    }

    private static string? FindPattern(List<Item> items)
    {
        var boundary = @"(?:^|[\\/])";
        var extension = @"\.(?:" + string.Join("|", AudioExts) + ")$";
        var commonPrefix = LongestCommonPrefix(items.Select(i => i.Prefix).ToList());
        var commonSuffix = LongestCommonSuffix(items.Select(i => i.Suffix).ToList());

        var candidates = new[]
        {
            boundary + Regex.Escape(commonPrefix) + @"(\d+)" + Regex.Escape(commonSuffix) + extension,
            boundary + @"(?:.*\D)?(\d+)" + Regex.Escape(commonSuffix) + extension,
            @"(\d+)(?!.*\d)"
        };

        foreach (var candidate in candidates.Distinct(StringComparer.Ordinal))
        {
            if (Verify(candidate, items)) return candidate;
        }
        return null;
    }

    private static bool Verify(string pattern, List<Item> items)
    {
        Regex regex;
        try { regex = new Regex(pattern, RegexOptions.IgnoreCase); }
        catch (ArgumentException) { return false; }

        // 要求覆盖所有手动指定的文件，且捕获值与文件名数字一致
        foreach (var item in items)
        {
            var match = regex.Match(item.RelativePath);
            if (!match.Success) return false;
            if (!int.TryParse(match.Groups[1].Value, out var value) || value != item.Number)
                return false;
        }
        return true;
    }

    private static bool TrySequenceOffset(string pattern, List<Item> items,
        IReadOnlyList<AudioTrack> tracks, out int offset)
    {
        offset = 0;
        Regex regex;
        try { regex = new Regex(pattern, RegexOptions.IgnoreCase); }
        catch (ArgumentException) { return false; }

        var hits = new List<(string Path, int Value)>();
        foreach (var track in tracks)
        {
            var match = regex.Match(track.RelativePath);
            if (!match.Success) continue;
            hits.Add((track.RelativePath, int.TryParse(match.Groups[1].Value, out var v) ? v : 0));
        }
        var ordered = hits.OrderBy(h => h.Value)
            .ThenBy(h => h.Path, StringComparer.OrdinalIgnoreCase)
            .Select((h, index) => (h.Path, Index: index))
            .ToList();

        int? candidateOffset = null;
        foreach (var item in items)
        {
            var hit = ordered.FirstOrDefault(h =>
                string.Equals(h.Path, item.RelativePath, StringComparison.OrdinalIgnoreCase));
            if (hit.Path == null) return false;
            var delta = item.Page - hit.Index;
            if (candidateOffset == null) candidateOffset = delta;
            else if (candidateOffset != delta) return false;
        }
        if (candidateOffset == null) return false;
        offset = candidateOffset.Value;
        return true;
    }

    private static AudioTrack? Resolve(string value, IReadOnlyList<AudioTrack> tracks)
        => tracks.FirstOrDefault(t => string.Equals(t.RelativePath, value, StringComparison.OrdinalIgnoreCase))
           ?? tracks.FirstOrDefault(t => string.Equals(t.FilePath, value, StringComparison.OrdinalIgnoreCase))
           ?? tracks.FirstOrDefault(t => string.Equals(t.FileName, Path.GetFileName(value), StringComparison.OrdinalIgnoreCase));

    private static string LongestCommonPrefix(List<string> values)
    {
        var first = values[0];
        var length = first.Length;
        foreach (var value in values.Skip(1))
        {
            length = Math.Min(length, value.Length);
            var i = 0;
            while (i < length && char.ToUpperInvariant(first[i]) == char.ToUpperInvariant(value[i])) i++;
            length = i;
        }
        return first[..length];
    }

    private static string LongestCommonSuffix(List<string> values)
    {
        var first = values[0];
        var length = first.Length;
        foreach (var value in values.Skip(1))
        {
            length = Math.Min(length, value.Length);
            var i = 0;
            while (i < length &&
                   char.ToUpperInvariant(first[first.Length - 1 - i]) ==
                   char.ToUpperInvariant(value[value.Length - 1 - i])) i++;
            length = i;
        }
        return length == 0 ? "" : first[(first.Length - length)..];
    }

    private static string? Combine(string? first, string second)
        => first == null ? second : first + "；" + second;

    private sealed class Item
    {
        public int Page { get; init; }
        public int Number { get; init; }
        public string RelativePath { get; init; } = "";
        public string Prefix { get; init; } = "";
        public string Suffix { get; init; } = "";
    }
}
