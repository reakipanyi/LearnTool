namespace UnifiedLearningAssistant.Models.Learning
{
    public class ChinesePoem : LearningItem
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Dynasty { get; set; } = string.Empty;
        public List<string> Verses { get; set; } = new List<string>();
        public string Annotation { get; set; } = string.Empty;

        public override string GetMainContent() => Title;
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Author))
                parts.Add($"作者: {Author}");
            if (!string.IsNullOrWhiteSpace(Dynasty))
                parts.Add($"朝代: {Dynasty}");
            if (Verses != null && Verses.Any())
                parts.Add($"诗句: {string.Join("，", Verses)}");
            return string.Join(" | ", parts);
        }
        public override string GetPronunciation() => string.Empty;

    }
}
