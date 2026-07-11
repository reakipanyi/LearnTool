namespace LearningAssistant.Services.Learning
{
    public record ContentField(
        string Label,
        string Value,
        string? SpeakText = null,
        bool IsPronunciation = false,
        int Order = 0,
        string Language = "en")
    {
        public bool HasSpeakText => !string.IsNullOrWhiteSpace(SpeakText);
    }
}