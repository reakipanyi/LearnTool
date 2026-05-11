namespace UnifiedLearningAssistant.Models.Learning
{
    public class EnglishPhrase : LearningItem
    {
        public string Phrase { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        public override string GetMainContent() => Phrase;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => string.Empty;
    }
}