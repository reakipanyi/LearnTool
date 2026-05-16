
namespace UnifiedLearningAssistant.Models.Learning
{
    /// &lt;summary&gt;
    /// 英语综合学习项
    /// 包含单词、短语和句子的组合学习
    /// &lt;/summary&gt;
    public class EnglishComprehensive : LearningItem
    {
        /// &lt;summary&gt;
        /// 学习项类型
        /// &lt;/summary&gt;
        public EnglishItemType ItemType { get; set; } = EnglishItemType.Word;

        /// &lt;summary&gt;
        /// 单词内容（适用于：单词）
        /// &lt;/summary&gt;
        public string Word { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 音标（适用于：单词、短语）
        /// &lt;/summary&gt;
        public string Phonetic { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 短语内容（适用于：短语）
        /// &lt;/summary&gt;
        public string Phrase { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 句子内容（适用于：句子）
        /// &lt;/summary&gt;
        public string Sentence { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 中文释义（适用于所有类型）
        /// &lt;/summary&gt;
        public string ChineseMeaning { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 英文释义（适用于所有类型）
        /// &lt;/summary&gt;
        public string EnglishMeaning { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 词性（适用于：单词、短语）
        /// &lt;/summary&gt;
        public string PartOfSpeech { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 例句（适用于：单词、短语、句子）
        /// &lt;/summary&gt;
        public string Example { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 例句中文翻译（适用于：单词、短语、句子）
        /// &lt;/summary&gt;
        public string ExampleTranslation { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 同义词（适用于：单词）
        /// &lt;/summary&gt;
        public List&lt;string&gt; Synonyms { get; set; } = new List&lt;string&gt;();

        /// &lt;summary&gt;
        /// 反义词（适用于：单词）
        /// &lt;/summary&gt;
        public List&lt;string&gt; Antonyms { get; set; } = new List&lt;string&gt;();

        /// &lt;summary&gt;
        /// 相关词汇（适用于：所有类型）
        /// &lt;/summary&gt;
        public List&lt;string&gt; RelatedWords { get; set; } = new List&lt;string&gt;();

        /// &lt;summary&gt;
        /// 难度级别
        /// &lt;/summary&gt;
        public int DifficultyLevel { get; set; } = 1;

        /// &lt;summary&gt;
        /// 主题分类
        /// &lt;/summary&gt;
        public string Topic { get; set; } = string.Empty;

        /// &lt;inheritdoc/&gt;
        public override string GetMainContent()
        {
            return ItemType switch
            {
                EnglishItemType.Word =&gt; Word,
                EnglishItemType.Phrase =&gt; Phrase,
                EnglishItemType.Sentence =&gt; Sentence,
                _ =&gt; Word
            };
        }

        /// &lt;inheritdoc/&gt;
        public override string GetDisplayText()
        {
            var parts = new List&lt;string&gt;();

            switch (ItemType)
            {
                case EnglishItemType.Word:
                    if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                        parts.Add($"词性: {PartOfSpeech}");
                    if (!string.IsNullOrWhiteSpace(Phonetic))
                        parts.Add($"音标: {Phonetic}");
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add($"释义: {ChineseMeaning}");
                    if (!string.IsNullOrWhiteSpace(EnglishMeaning))
                        parts.Add($"英文释义: {EnglishMeaning}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"例句: {Example}");
                    if (Synonyms.Count &gt; 0)
                        parts.Add($"同义词: {string.Join(", ", Synonyms)}");
                    break;

                case EnglishItemType.Phrase:
                    if (!string.IsNullOrWhiteSpace(Phonetic))
                        parts.Add($"音标: {Phonetic}");
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add($"释义: {ChineseMeaning}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"例句: {Example}");
                    break;

                case EnglishItemType.Sentence:
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add($"翻译: {ChineseMeaning}");
                    if (!string.IsNullOrWhiteSpace(EnglishMeaning))
                        parts.Add($"英文解释: {EnglishMeaning}");
                    if (!string.IsNullOrWhiteSpace(Topic))
                        parts.Add($"主题: {Topic}");
                    break;
            }

            return string.Join(" | ", parts);
        }

        /// &lt;inheritdoc/&gt;
        public override string GetPronunciation() =&gt; Phonetic;
    }

    /// &lt;summary&gt;
    /// 英语学习项类型
    /// &lt;/summary&gt;
    public enum EnglishItemType
    {
        Word,     // 单词
        Phrase,   // 短语
        Sentence  // 句子
    }
}

