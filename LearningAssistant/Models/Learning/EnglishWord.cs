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

        /// <summary>
        /// 音节拼读
        /// </summary>
        public string SyllableBreakdown { get; set; } = string.Empty;

        /// <summary>
        /// 近义词
        /// </summary>
        public string Synonyms { get; set; } = string.Empty;

        /// <summary>
        /// 反义词
        /// </summary>
        public string Antonyms { get; set; } = string.Empty;

        /// <summary>
        /// 词形变化（过去式、过去分词、现在分词、复数等）
        /// </summary>
        public string WordForms { get; set; } = string.Empty;

        /// <summary>
        /// 词根词缀
        /// </summary>
        public string WordRootAffix { get; set; } = string.Empty;

        /// <summary>
        /// 常见搭配
        /// </summary>
        public string Collocations { get; set; } = string.Empty;

        /// <summary>
        /// 常用短语
        /// </summary>
        public string Phrases { get; set; } = string.Empty;

        /// <summary>
        /// 同义词辨析
        /// </summary>
        public string SynonymAnalysis { get; set; } = string.Empty;

        /// <summary>
        /// 英式音标
        /// </summary>
        public string UkPhonetic { get; set; } = string.Empty;

        /// <summary>
        /// 美式音标
        /// </summary>
        public string UsPhonetic { get; set; } = string.Empty;

        /// <summary>
        /// 词汇等级（CET4、CET6、GRE等）
        /// </summary>
        public string VocabularyLevel { get; set; } = string.Empty;

        /// <summary>
        /// 词源
        /// </summary>
        public string Etymology { get; set; } = string.Empty;

        /// <summary>
        /// 易混词
        /// </summary>
        public string ConfusableWords { get; set; } = string.Empty;

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
            if (!string.IsNullOrWhiteSpace(UkPhonetic))
                parts.Add($"英式: {UkPhonetic}");
            if (!string.IsNullOrWhiteSpace(UsPhonetic))
                parts.Add($"美式: {UsPhonetic}");
            if (!string.IsNullOrWhiteSpace(VocabularyLevel))
                parts.Add($"词级: {VocabularyLevel}");
            if (!string.IsNullOrWhiteSpace(SyllableBreakdown))
                parts.Add($"拼读: {SyllableBreakdown}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(WordForms))
                parts.Add($"词形变化: {WordForms}");
            if (!string.IsNullOrWhiteSpace(WordRootAffix))
                parts.Add($"词根词缀: {WordRootAffix}");
            if (!string.IsNullOrWhiteSpace(Collocations))
                parts.Add($"搭配: {Collocations}");
            if (!string.IsNullOrWhiteSpace(Phrases))
                parts.Add($"短语: {Phrases}");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add($"例句: {Example}");
            if (!string.IsNullOrWhiteSpace(Synonyms))
                parts.Add($"近义词: {Synonyms}");
            if (!string.IsNullOrWhiteSpace(Antonyms))
                parts.Add($"反义词: {Antonyms}");
            if (!string.IsNullOrWhiteSpace(SynonymAnalysis))
                parts.Add($"辨析: {SynonymAnalysis}");
            if (!string.IsNullOrWhiteSpace(ConfusableWords))
                parts.Add($"易混词: {ConfusableWords}");
            if (!string.IsNullOrWhiteSpace(Etymology))
                parts.Add($"词源: {Etymology}");
            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => Phonetic;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(PartOfSpeech))
                parts.Add("词性:?");
            if (!string.IsNullOrWhiteSpace(Phonetic))
                parts.Add("音标:?");
            if (!string.IsNullOrWhiteSpace(UkPhonetic))
                parts.Add("英式:?");
            if (!string.IsNullOrWhiteSpace(UsPhonetic))
                parts.Add("美式:?");
            if (!string.IsNullOrWhiteSpace(VocabularyLevel))
                parts.Add("词级:?");
            if (!string.IsNullOrWhiteSpace(SyllableBreakdown))
                parts.Add("拼读:?");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add("释义:?");
            if (!string.IsNullOrWhiteSpace(WordForms))
                parts.Add("词形变化:?");
            if (!string.IsNullOrWhiteSpace(WordRootAffix))
                parts.Add("词根词缀:?");
            if (!string.IsNullOrWhiteSpace(Collocations))
                parts.Add("搭配:?");
            if (!string.IsNullOrWhiteSpace(Phrases))
                parts.Add("短语:?");
            if (!string.IsNullOrWhiteSpace(Example))
                parts.Add("例句:?");
            if (!string.IsNullOrWhiteSpace(Synonyms))
                parts.Add("近义词:?");
            if (!string.IsNullOrWhiteSpace(Antonyms))
                parts.Add("反义词:?");
            if (!string.IsNullOrWhiteSpace(SynonymAnalysis))
                parts.Add("辨析:?");
            if (!string.IsNullOrWhiteSpace(ConfusableWords))
                parts.Add("易混词:?");
            if (!string.IsNullOrWhiteSpace(Etymology))
                parts.Add("词源:?");
            return string.Join("\n", parts);
        }
    }
}