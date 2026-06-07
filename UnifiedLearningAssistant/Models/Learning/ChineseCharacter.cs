namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 汉字学习项
    /// </summary>
    public class ChineseCharacter : LearningItem
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
        /// 释义
        /// </summary>
        public string Meaning { get; set; } = string.Empty;
        
        /// <summary>
        /// 笔画数
        /// </summary>
        public string StrokeCount { get; set; } = string.Empty;
        
        /// <summary>
        /// 部首
        /// </summary>
        public string Radical { get; set; } = string.Empty;

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
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(StrokeCount))
                parts.Add($"笔画: {StrokeCount}画");
            if (!string.IsNullOrWhiteSpace(Radical))
                parts.Add($"部首: {Radical}");
            if (Words != null && Words.Any())
                parts.Add($"组词: {string.Join(", ", Words.Take(5))}");
            return string.Join(" | ", parts);
        }
        
        /// <inheritdoc/>
        public override string GetPronunciation() => Pinyin;



    }
}
