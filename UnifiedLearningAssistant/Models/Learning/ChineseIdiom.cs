namespace UnifiedLearningAssistant.Models.Learning
{
    public class ChineseIdiom : LearningItem
    {
        public string Idiom { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;

        public override string GetMainContent() => Idiom;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            if (!string.IsNullOrWhiteSpace(Origin))
                parts.Add($"出处: {Origin}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => Pinyin;
    }
}