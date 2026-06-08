namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 古诗词学习项
    /// </summary>
    public class ChinesePoem : LearningItem
    {
        /// <summary>
        /// 诗题
        /// </summary>
        public string Title { get; set; } = string.Empty;
        
        /// <summary>
        /// 作者
        /// </summary>
        public string Author { get; set; } = string.Empty;
        
        /// <summary>
        /// 朝代
        /// </summary>
        public string Dynasty { get; set; } = string.Empty;
        
        /// <summary>
        /// 诗句列表
        /// </summary>
        public List<string> Verses { get; set; } = new List<string>();
        
        /// <summary>
        /// 注释
        /// </summary>
        public string Annotation { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Title;
        
        /// <inheritdoc/>
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
        
        /// <inheritdoc/>
        public override string GetPronunciation() => string.Empty;

    }
}
