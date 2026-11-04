using LearningAssistant.Models.Audio;

namespace LearningAssistant.Services.Audio;

/// <summary>
/// 断句灵敏度预设的唯一阈值来源。书级档案只存预设名，不存具体阈值。
/// </summary>
public static class AudioTimelinePresets
{
    public const string Fine = "fine";
    public const string Standard = "standard";
    public const string Loose = "loose";

    public static string Normalize(string? name)
    {
        if (string.Equals(name, Standard, StringComparison.OrdinalIgnoreCase)) return Standard;
        if (string.Equals(name, Loose, StringComparison.OrdinalIgnoreCase)) return Loose;
        return Fine;
    }

    public static AudioTimelineOptions Create(string? name) => Normalize(name) switch
    {
        Standard => new AudioTimelineOptions
        {
            SentenceGapMs = 450,
            FineGapMs = 200,
            MinSentenceMs = 1000,
            MaxSentenceMs = 12000,
            PaddingMs = 120
        },
        Loose => new AudioTimelineOptions
        {
            SentenceGapMs = 600,
            FineGapMs = 250,
            MinSentenceMs = 1500,
            MaxSentenceMs = 20000,
            PaddingMs = 150
        },
        _ => new AudioTimelineOptions()
    };
}
