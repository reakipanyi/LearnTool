using LearningAssistant.Abstractions.Media;
using System.Text;
using System.Text.RegularExpressions;

namespace LearningAssistant.Services.Media;

public class SubtitleParser : ISubtitleParser
{
    private static readonly Regex SrtTime = new(@"(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})\s*-->\s*(\d{1,2}):(\d{2}):(\d{2})[,.](\d{1,3})", RegexOptions.Compiled);
    private static readonly Regex LrcTag = new(@"\[(\d{1,2}):(\d{2})(?:[.:](\d{1,3}))?\]", RegexOptions.Compiled);

    public IReadOnlyList<SubtitleCue> Parse(string filePath)
    {
        var text = ReadAllText(filePath);
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var cues = ext == ".srt" ? ParseSrt(text) : ParseLrc(text);
        for (int i = 0; i < cues.Count - 1; i++)
            if (cues[i].EndTime <= TimeSpan.Zero)
                cues[i].EndTime = cues[i + 1].StartTime;
        return cues;
    }

    private static string ReadAllText(string path)
    {
        try { return File.ReadAllText(path, Encoding.UTF8); }
        catch (DecoderFallbackException) { return File.ReadAllText(path, Encoding.GetEncoding("GB18030")); }
    }

    private static List<SubtitleCue> ParseSrt(string text)
    {
        var list = new List<SubtitleCue>();
        var blocks = text.Replace("\r\n", "\n").Split("\n\n");
        int index = 0;
        foreach (var block in blocks)
        {
            var m = SrtTime.Match(block);
            if (!m.Success) continue;
            var start = ToTimeSpan(m, 1);
            var end = ToTimeSpan(m, 5);
            var body = string.Join(" ", block.Split('\n').Skip(2).Where(l => !string.IsNullOrWhiteSpace(l))).Trim();
            if (string.IsNullOrWhiteSpace(body)) continue;
            list.Add(new SubtitleCue { Index = ++index, StartTime = start, EndTime = end, Text = body });
        }
        return list;
    }

    private static List<SubtitleCue> ParseLrc(string text)
    {
        var list = new List<SubtitleCue>();
        int index = 0;
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (string.IsNullOrWhiteSpace(raw)) continue;
            var matches = LrcTag.Matches(raw);
            if (matches.Count == 0) continue;
            var body = LrcTag.Replace(raw, "").Trim();
            if (string.IsNullOrWhiteSpace(body)) continue;
            foreach (Match m in matches)
            {
                var min = int.Parse(m.Groups[1].Value);
                var sec = int.Parse(m.Groups[2].Value);
                var ms = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
                list.Add(new SubtitleCue { Index = ++index, StartTime = new TimeSpan(0, 0, min, sec, ms), Text = body });
            }
        }
        return list.OrderBy(c => c.StartTime).ToList();
    }

    private static TimeSpan ToTimeSpan(Match m, int group)
    {
        int h = int.Parse(m.Groups[group].Value);
        int mm = int.Parse(m.Groups[group + 1].Value);
        int ss = int.Parse(m.Groups[group + 2].Value);
        int ms = int.Parse(m.Groups[group + 3].Value);
        return new TimeSpan(0, h, mm, ss, ms);
    }
}