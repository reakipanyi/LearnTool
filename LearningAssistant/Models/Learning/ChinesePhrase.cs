namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 中文短语学习项
    /// </summary>
    public class ChinesePhrase : LearningItem
    {
        /// <summary>
        /// 短语
        /// </summary>
        public string Phrase { get; set; } = string.Empty;
        
        /// <summary>
        /// 拼音
        /// </summary>
        public string Pinyin { get; set; } = string.Empty;
        
        /// <summary>
        /// 释义
        /// </summary>
        public string Meaning { get; set; } = string.Empty;
        
        /// <summary>
        /// 例句
        /// </summary>
        public string Example { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Phrase;
        
        /// <inheritdoc/>
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
        
        /// <inheritdoc/>
        public override string GetPronunciation() => Pinyin;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add("拼音");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add("释义");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add("例句");
            return string.Join(" | ", parts);
        }
    }
}