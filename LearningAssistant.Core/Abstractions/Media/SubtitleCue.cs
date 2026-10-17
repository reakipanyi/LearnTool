namespace LearningAssistant.Abstractions.Media;
public sealed class SubtitleCue
{
    public int Index { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Text { get; set; } = "";
}