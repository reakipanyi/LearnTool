namespace UnifiedLearningAssistant.Models.Learning
{
    /// <summary>
    /// 汉字组词学习项
    /// </summary>
    public class ChineseWordCombination : LearningItem
    {
        /// <summary>
        /// 汉字
        /// </summary>
        public string Character { get; set; } = string.Empty;
        
        /// <summary>
        /// 拼音
        /// </summary>
        public string Pinyin { get; set; } = string.Empty;
        
        /// <summary>
        /// 组成的词语列表
        /// </summary>
        public List<string> Words { get; set; } = new List<string>();

        /// <inheritdoc/>
        public override string GetMainContent() => Character;
        
        /// <inheritdoc/>
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (Words != null && Words.Any())
                parts.Add($"组词: {string.Join(", ", Words.Take(5))}");
            return string.Join(" | ", parts);
        }
        
        /// <inheritdoc/>
        public override string GetPronunciation() => Pinyin;
    }
}