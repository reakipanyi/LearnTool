namespace LearningAssistant.Services.Learning
{
    public record ContentField(
        string Label,
        string Value,
        string? SpeakText = null,
        bool CanSpeak = false,
        int Order = 0,
        string Language = "en",
        bool IsAnswer = false)
    {
        public bool HasSpeakText => !string.IsNullOrWhiteSpace(SpeakText);
    }
}