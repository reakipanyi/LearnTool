namespace UnifiedLearningAssistant.Models.Learning
{
    public class ChinesePhrase : LearningItem
    {
        public string Phrase { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        public override string GetMainContent() => Phrase;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => Pinyin;
    }
}