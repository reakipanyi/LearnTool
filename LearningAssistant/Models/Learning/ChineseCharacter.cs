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
        /// 笔顺
        /// </summary>
        public string StrokeOrder { get; set; } = string.Empty;

        /// <summary>
        /// 组成的词语列表
        /// </summary>
        public string Words { get; set; }

        //public List<string> Words { get; set; } = new List<string>();

        /// <summary>
        /// 形近字
        /// </summary>
        public string SimilarCharacters { get; set; } = string.Empty;

        /// <summary>
        /// 近义词
        /// </summary>
        public string Synonyms { get; set; } = string.Empty;

        /// <summary>
        /// 反义词
        /// </summary>
        public string Antonyms { get; set; } = string.Empty;

        /// <summary>
        /// 易错点/注意事项
        /// </summary>
        public string CommonMistakes { get; set; } = string.Empty;

        /// <summary>
        /// 例句
        /// </summary>
        public string ExampleSentence { get; set; } = string.Empty;

        /// <summary>
        /// 字级（一级、二级、三级）
        /// </summary>
        public string CharacterLevel { get; set; } = string.Empty;

        /// <summary>
        /// 结构（左右结构、上下结构、独体字等）
        /// </summary>
        public string Structure { get; set; } = string.Empty;

        /// <summary>
        /// 造字法（象形、指事、会意、形声等）
        /// </summary>
        public string CharacterFormation { get; set; } = string.Empty;

        /// <summary>
        /// 多音字信息（如果是多音字）
        /// </summary>
        public string OtherPronunciations { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Character;

        /// <inheritdoc/>
        public override string GetDisplayText()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add($"拼音: {Pinyin}");
            if (!string.IsNullOrWhiteSpace(OtherPronunciations))
                parts.Add($"其他读音: {OtherPronunciations}");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add($"释义: {Meaning}");
            if (!string.IsNullOrWhiteSpace(StrokeCount))
                parts.Add($"笔画: {StrokeCount}画");
            if (!string.IsNullOrWhiteSpace(Radical))
                parts.Add($"部首: {Radical}");
            if (!string.IsNullOrWhiteSpace(Structure))
                parts.Add($"结构: {Structure}");
            if (!string.IsNullOrWhiteSpace(CharacterLevel))
                parts.Add($"字级: {CharacterLevel}");
            if (!string.IsNullOrWhiteSpace(CharacterFormation))
                parts.Add($"造字法: {CharacterFormation}");
            if (!string.IsNullOrWhiteSpace(StrokeOrder))
                parts.Add($"笔顺: {StrokeOrder}");
            if (!string.IsNullOrWhiteSpace(Words))
                parts.Add($"组词: {Words}");
            if (!string.IsNullOrWhiteSpace(SimilarCharacters))
                parts.Add($"形近字: {SimilarCharacters}");
            if (!string.IsNullOrWhiteSpace(Synonyms))
                parts.Add($"近义词: {Synonyms}");
            if (!string.IsNullOrWhiteSpace(Antonyms))
                parts.Add($"反义词: {Antonyms}");
            if (!string.IsNullOrWhiteSpace(ExampleSentence))
                parts.Add($"例句: {ExampleSentence}");
            if (!string.IsNullOrWhiteSpace(CommonMistakes))
                parts.Add($"易错点: {CommonMistakes}");
            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => Pinyin;



        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(Pinyin))
                parts.Add("拼音:?");
            if (!string.IsNullOrWhiteSpace(OtherPronunciations))
                parts.Add("其他读音:?");
            if (!string.IsNullOrWhiteSpace(Meaning))
                parts.Add("释义:?");
            if (!string.IsNullOrWhiteSpace(StrokeCount))
                parts.Add("笔画:?");
            if (!string.IsNullOrWhiteSpace(Radical))
                parts.Add("部首:?");
            if (!string.IsNullOrWhiteSpace(Structure))
                parts.Add("结构:?");
            if (!string.IsNullOrWhiteSpace(CharacterLevel))
                parts.Add("字级:?");
            if (!string.IsNullOrWhiteSpace(CharacterFormation))
                parts.Add("造字法:?");
            if (!string.IsNullOrWhiteSpace(StrokeOrder))
                parts.Add("笔顺:?");
            if (!string.IsNullOrWhiteSpace(Words))
                parts.Add("组词:?");
            if (!string.IsNullOrWhiteSpace(SimilarCharacters))
                parts.Add("形近字:?");
            if (!string.IsNullOrWhiteSpace(Synonyms))
                parts.Add("近义词:?");
            if (!string.IsNullOrWhiteSpace(Antonyms))
                parts.Add("反义词:?");
            if (!string.IsNullOrWhiteSpace(ExampleSentence))
                parts.Add("例句:?");
            if (!string.IsNullOrWhiteSpace(CommonMistakes))
                parts.Add("易错点:?");
            return string.Join("\n", parts);
        }

    }
}
