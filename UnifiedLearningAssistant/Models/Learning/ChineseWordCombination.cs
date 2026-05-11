namespace UnifiedLearningAssistant.Models.Learning
{
    public class ChineseWordCombination : LearningItem
    {
        public string Character { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public List<string> Words { get; set; } = new List<string>();

        public override string GetMainContent() => Character;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (Words != null && Words.Any())
                parts.Add($"组词: {string.Join(", ", Words.Take(5))}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => Pinyin;
    }
}