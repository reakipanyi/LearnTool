namespace UnifiedLearningAssistant.Models.Learning
{
    public class ChineseCharacter : LearningItem
    {
        public string Character { get; set; } = string.Empty;
        public string Pinyin { get; set; } = string.Empty;
        public string Meaning { get; set; } = string.Empty;
        public string StrokeCount { get; set; } = string.Empty;
        public string Radical { get; set; } = string.Empty;


        public override string GetMainContent() => Character;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(StrokeCount))
                parts.Add($"笔画: {StrokeCount}画");
            if (!string.IsNullOrWhiteSpace(Radical))
                parts.Add($"部首: {Radical}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => Pinyin;



    }
}
