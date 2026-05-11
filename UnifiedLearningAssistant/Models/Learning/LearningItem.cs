namespace UnifiedLearningAssistant.Models.Learning
{
    public abstract class LearningItem
    {
        public string Id { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public abstract string GetMainContent();
        public abstract string GetDisplayText();
        public abstract string GetPronunciation();
    }
}