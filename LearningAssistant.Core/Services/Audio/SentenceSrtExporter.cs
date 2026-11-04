using System.Text;
using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 把句子时间轴导出为 SRT 字幕。无文本的句子用「第 N 句」占位，
/// 时间与原音频一致（毫秒 → HH:MM:SS,mmm）。
/// </summary>
public static class SentenceSrtExporter
{
    public static string Format(IReadOnlyList<AudioSentenceSpan>? spans)
    {
        if (spans == null || spans.Count == 0) return string.Empty;

        var ordered = spans
            .Where(span => span != null && span.EndMs > span.StartMs)
            .OrderBy(span => span.StartMs)
            .ThenBy(span => span.EndMs)
            .ToList();

        if (ordered.Count == 0) return string.Empty;

        var builder = new StringBuilder();
        for (int i = 0; i < ordered.Count; i++)
        {
            var span = ordered[i];
            string text = string.IsNullOrWhiteSpace(span.Text)
                ? $"第 {i + 1} 句"
                : NormalizeText(span.Text!);

            builder.Append(i + 1).Append('\n');
            builder.Append(FormatTime(span.StartMs))
                .Append(" --> ")
                .Append(FormatTime(span.EndMs))
                .Append('\n');
            builder.Append(text).Append("\n\n");
        }

        return builder.ToString();
    }

    /// <summary>HH:MM:SS,mmm；SRT 必须用逗号分隔毫秒。</summary>
    public static string FormatTime(int milliseconds)
    {
        if (milliseconds < 0) milliseconds = 0;

        int hours = milliseconds / 3600000;
        milliseconds %= 3600000;
        int minutes = milliseconds / 60000;
        milliseconds %= 60000;
        int seconds = milliseconds / 1000;
        int millis = milliseconds % 1000;

        return $"{hours:00}:{minutes:00}:{seconds:00},{millis:000}";
    }

    private static string NormalizeText(string text)
        => text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
}
