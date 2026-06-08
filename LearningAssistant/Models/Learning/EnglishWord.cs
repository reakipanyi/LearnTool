namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 英语单词学习项
    /// </summary>
    public class EnglishWord : LearningItem
    {
        /// <summary>
        /// 单词
        /// </summary>
        public string Word { get; set; } = string.Empty;
        
        /// <summary>
        /// 音标
        /// </summary>
        public string Phonetic { get; set; } = string.Empty;
        
        /// <summary>
        /// 释义
        /// </summary>
        public string Meaning { get; set; } = string.Empty;
        
        /// <summary>
        /// 例句
        /// </summary>
        public string Example { get; set; } = string.Empty;
        
        /// <summary>
        /// 词性（如noun、verb等）
        /// </summary>
        public string PartOfSpeech { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Word;
        
        /// <inheritdoc/>
        public override string GetDisplayText() 
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                parts.Add($"词性: {PartOfSpeech}");
            if (!string.IsNullOrWhiteSpace(Phonetic))
                parts.Add($"音标: {Phonetic}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            return string.Join(" | ", parts);
        }
        
        /// <inheritdoc/>
        public override string GetPronunciation() => Phonetic;
    }
}