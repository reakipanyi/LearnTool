namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 英语句子学习项
    /// </summary>
    public class EnglishSentence : LearningItem
    {
        /// <summary>
        /// 英文原句
        /// </summary>
        public string Sentence { get; set; } = string.Empty;
        
        /// <summary>
        /// 中文翻译
        /// </summary>
        public string Translation { get; set; } = string.Empty;
        
        /// <summary>
        /// 语法分析
        /// </summary>
        public string Grammar { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Sentence;
        
        /// <inheritdoc/>
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Translation))
                parts.Add($"翻译: {Translation}");
            if (!string.IsNullOrWhiteSpace(Grammar))
                parts.Add($"语法: {Grammar}");
            return string.Join(" | ", parts);
        }
        
        /// <inheritdoc/>
        public override string GetPronunciation() => string.Empty;
    }
}