
namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 英语综合学习项
    /// 包含单词、短语和句子的组合学习
    /// </summary>
    public class EnglishComprehensive : LearningItem
    {
        /// <summary>
        /// 学习项类型
        /// </summary>
        public EnglishItemType ItemType { get; set; } = EnglishItemType.Word;

        /// <summary>
        /// 单词内容（适用于：单词）
        /// </summary>
        public string Word { get; set; } = string.Empty;

        /// <summary>
        /// 音标（适用于：单词、短语）
        /// </summary>
        public string Phonetic { get; set; } = string.Empty;

        /// <summary>
        /// 短语内容（适用于：短语）
        /// </summary>
        public string Phrase { get; set; } = string.Empty;

        /// <summary>
        /// 句子内容（适用于：句子）
        /// </summary>
        public string Sentence { get; set; } = string.Empty;

        /// <summary>
        /// 中文释义（适用于所有类型）
        /// </summary>
        public string ChineseMeaning { get; set; } = string.Empty;

        /// <summary>
        /// 英文释义（适用于所有类型）
        /// </summary>
        public string EnglishMeaning { get; set; } = string.Empty;

        /// <summary>
        /// 词性（适用于：单词、短语）
        /// </summary>
        public string PartOfSpeech { get; set; } = string.Empty;

        /// <summary>
        /// 音节拼读（适用于：单词）
        /// </summary>
        public string SyllableBreakdown { get; set; } = string.Empty;

        /// <summary>
        /// 例句（适用于：单词、短语、句子）
        /// </summary>
        public string Example { get; set; } = string.Empty;

        /// <summary>
        /// 例句中文翻译（适用于：单词、短语、句子）
        /// </summary>
        public string ExampleTranslation { get; set; } = string.Empty;

        /// <summary>
        /// 同义词（适用于：单词）
        /// </summary>
        public List<string> Synonyms { get; set; } = new List<string>();

        /// <summary>
        /// 反义词（适用于：单词）
        /// </summary>
        public List<string> Antonyms { get; set; } = new List<string>();

        /// <summary>
        /// 相关词汇（适用于：所有类型）
        /// </summary>
        public List<string> RelatedWords { get; set; } = new List<string>();

        /// <summary>
        /// 难度级别
        /// </summary>
        public int DifficultyLevel { get; set; } = 1;

        /// <summary>
        /// 主题分类
        /// </summary>
        public string Topic { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent()
        {
            return ItemType switch
            {
                EnglishItemType.Word => Word,
                EnglishItemType.Phrase => Phrase,
                EnglishItemType.Sentence => Sentence,
                _ => Word
            };
        }

        /// <inheritdoc/>
        public override string GetDisplayText()
        {
            var parts = new List<string>();

            switch (ItemType)
            {
                case EnglishItemType.Word:
                    if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                        parts.Add($"词性: {PartOfSpeech}");
                    if (!string.IsNullOrWhiteSpace(Phonetic))
                        parts.Add($"音标: {Phonetic}");
                    if (!string.IsNullOrWhiteSpace(SyllableBreakdown))
                        parts.Add($"拼读: {SyllableBreakdown}");
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add($"释义: {ChineseMeaning}");
                    if (!string.IsNullOrWhiteSpace(EnglishMeaning))
                        parts.Add($"英文释义: {EnglishMeaning}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"例句: {Example}");
                    if (Synonyms.Count > 0)
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

            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => Phonetic;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();

            switch (ItemType)
            {
                case EnglishItemType.Word:
                    if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                        parts.Add("词性:?");
                    if (!string.IsNullOrWhiteSpace(Phonetic))
                        parts.Add("音标:?");
                    if (!string.IsNullOrWhiteSpace(SyllableBreakdown))
                        parts.Add("拼读:?");
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add("释义:?");
                    if (!string.IsNullOrWhiteSpace(EnglishMeaning))
                        parts.Add("英文释义:?");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add("例句:?");
                    if (Synonyms.Count > 0)
                        parts.Add("同义词:?");
                    break;

                case EnglishItemType.Phrase:
                    if (!string.IsNullOrWhiteSpace(Phonetic))
                        parts.Add("音标:?");
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add("释义:?");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add("例句:?");
                    break;

                case EnglishItemType.Sentence:
                    if (!string.IsNullOrWhiteSpace(ChineseMeaning))
                        parts.Add("翻译:?");
                    if (!string.IsNullOrWhiteSpace(EnglishMeaning))
                        parts.Add("英文解释:?");
                    if (!string.IsNullOrWhiteSpace(Topic))
                        parts.Add("主题:?");
                    break;
            }

            return string.Join("\n", parts);
        }
    }

    /// <summary>
    /// 英语学习项类型
    /// </summary>
    public enum EnglishItemType
    {
        Word,     // 单词
        Phrase,   // 短语
        Sentence  // 句子
    }
}

