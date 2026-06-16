
namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 语文综合学习项
    /// 包含生字、组词、短语、句子和古诗的组合学习
    /// </summary>
    public class ChineseComprehensive : LearningItem
    {
        /// <summary>
        /// 学习项类型
        /// </summary>
        public ChineseItemType ItemType { get; set; } = ChineseItemType.Character;

        /// <summary>
        /// 汉字内容（适用于：生字）
        /// </summary>
        public string Character { get; set; } = string.Empty;

        /// <summary>
        /// 拼音（适用于：生字、组词、短语、句子）
        /// </summary>
        public string Pinyin { get; set; } = string.Empty;

        /// <summary>
        /// 组词（适用于：组词）
        /// </summary>
        public List<string> WordCombinations { get; set; } = new List<string>();

        /// <summary>
        /// 短语/成语内容（适用于：短语、成语）
        /// </summary>
        public string Phrase { get; set; } = string.Empty;

        /// <summary>
        /// 句子内容（适用于：句子）
        /// </summary>
        public string Sentence { get; set; } = string.Empty;

        /// <summary>
        /// 古诗标题（适用于：古诗）
        /// </summary>
        public string PoemTitle { get; set; } = string.Empty;

        /// <summary>
        /// 古诗作者（适用于：古诗）
        /// </summary>
        public string PoemAuthor { get; set; } = string.Empty;

        /// <summary>
        /// 古诗朝代（适用于：古诗）
        /// </summary>
        public string PoemDynasty { get; set; } = string.Empty;

        /// <summary>
        /// 古诗内容（适用于：古诗）
        /// </summary>
        public List<string> PoemLines { get; set; } = new List<string>();

        /// <summary>
        /// 释义/解释（适用于所有类型）
        /// </summary>
        public string Meaning { get; set; } = string.Empty;

        /// <summary>
        /// 例句/示例（适用于：生字、组词、短语）
        /// </summary>
        public string Example { get; set; } = string.Empty;

        /// <summary>
        /// 笔画数（适用于：生字）
        /// </summary>
        public string StrokeCount { get; set; } = string.Empty;

        /// <summary>
        /// 部首（适用于：生字）
        /// </summary>
        public string Radical { get; set; } = string.Empty;

        /// <summary>
        /// 难度级别
        /// </summary>
        public int DifficultyLevel { get; set; } = 1;

        /// <inheritdoc/>
        public override string GetMainContent()
        {
            return ItemType switch
            {
                ChineseItemType.Character => Character,
                ChineseItemType.Phrase => Phrase,
                ChineseItemType.Idiom => Phrase,
                ChineseItemType.Sentence => Sentence,
                ChineseItemType.Poem => PoemTitle,
                _ => Character
            };
        }

        /// <inheritdoc/>
        public override string GetDisplayText()
        {
            var parts = new List<string>();

            switch (ItemType)
            {
                case ChineseItemType.Character:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add($"拼音: {Pinyin}");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add($"释义: {Meaning}");
                    if (WordCombinations.Count > 0)
                        parts.Add($"组词: {string.Join("、", WordCombinations)}");
                    if (!string.IsNullOrWhiteSpace(StrokeCount))
                        parts.Add($"笔画: {StrokeCount}画");
                    if (!string.IsNullOrWhiteSpace(Radical))
                        parts.Add($"部首: {Radical}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"示例: {Example}");
                    break;



                case ChineseItemType.Phrase:
                case ChineseItemType.Idiom:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add($"拼音: {Pinyin}");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add($"释义: {Meaning}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"示例: {Example}");
                    break;

                case ChineseItemType.Sentence:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add($"拼音: {Pinyin}");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add($"解释: {Meaning}");
                    break;

                case ChineseItemType.Poem:
                    if (!string.IsNullOrWhiteSpace(PoemAuthor))
                        parts.Add($"作者: {PoemAuthor}");
                    if (!string.IsNullOrWhiteSpace(PoemDynasty))
                        parts.Add($"朝代: {PoemDynasty}");
                    if (PoemLines.Count > 0)
                        parts.Add($"内容: {string.Join("，", PoemLines.Take(2))}...");
                    break;
            }

            return string.Join(" | ", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => Pinyin;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();

            switch (ItemType)
            {
                case ChineseItemType.Character:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add("拼音");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add("释义");
                    if (WordCombinations.Count > 0)
                        parts.Add("组词");
                    if (!string.IsNullOrWhiteSpace(StrokeCount))
                        parts.Add("笔画");
                    if (!string.IsNullOrWhiteSpace(Radical))
                        parts.Add("部首");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add("示例");
                    break;

                case ChineseItemType.Phrase:
                case ChineseItemType.Idiom:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add("拼音");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add("释义");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add("示例");
                    break;

                case ChineseItemType.Sentence:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add("拼音");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add("解释");
                    break;

                case ChineseItemType.Poem:
                    if (!string.IsNullOrWhiteSpace(PoemAuthor))
                        parts.Add("作者");
                    if (!string.IsNullOrWhiteSpace(PoemDynasty))
                        parts.Add("朝代");
                    if (PoemLines.Count > 0)
                        parts.Add("内容");
                    break;
            }

            return string.Join(" | ", parts);
        }
    }

    /// <summary>
    /// 语文学习项类型
    /// </summary>
    public enum ChineseItemType
    {
        Character,      // 生字 
        Phrase,         // 短语
        Idiom,          // 成语
        Sentence,       // 句子
        Poem            // 古诗
    }
}

