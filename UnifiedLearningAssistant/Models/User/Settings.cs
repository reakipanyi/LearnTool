namespace UnifiedLearningAssistant.Models.User
{
    public class Settings
    {
        public bool IsVoiceEnabled { get; set; } = true;
        public int PronunciationScope { get; set; } = 0; // 0=原文, 1=释义, 2=两者
    }
}
