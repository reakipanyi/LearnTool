namespace UnifiedLearningAssistant.Models.Learning
{
    public class EnglishSentence : LearningItem
    {
        public string Sentence { get; set; } = string.Empty;
        public string Translation { get; set; } = string.Empty;
        public string Grammar { get; set; } = string.Empty;

        public override string GetMainContent() => Sentence;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Translation))
                parts.Add($"翻译: {Translation}");
            if (!string.IsNullOrWhiteSpace(Grammar))
                parts.Add($"语法: {Grammar}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => string.Empty;
    }
}