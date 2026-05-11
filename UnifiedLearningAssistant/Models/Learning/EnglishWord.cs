namespace UnifiedLearningAssistant.Models.Learning
{
    public class EnglishWord : LearningItem
    {
        public string Word { get; set; } = string.Empty;
        public string Phonetic { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string PartOfSpeech { get; set; } = string.Empty;

        public override string GetMainContent() => Word;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                parts.Add($"词性: {PartOfSpeech}");
            if (!string.IsNullOrWhiteSpace(Phonetic))
                parts.Add($"音标: {Phonetic}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => Phonetic;
    }
}