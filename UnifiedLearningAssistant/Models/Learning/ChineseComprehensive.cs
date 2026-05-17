
namespace UnifiedLearningAssistant.Models.Learning
{
    /// &lt;summary&gt;
    /// 语文综合学习项
    /// 包含生字、组词、短语、句子和古诗的组合学习
    /// &lt;/summary&gt;
    public class ChineseComprehensive : LearningItem
    {
        /// &lt;summary&gt;
        /// 学习项类型
        /// &lt;/summary&gt;
        public ChineseItemType ItemType { get; set; } = ChineseItemType.Character;

        /// &lt;summary&gt;
        /// 汉字内容（适用于：生字）
        /// &lt;/summary&gt;
        public string Character { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 拼音（适用于：生字、组词、短语、句子）
        /// &lt;/summary&gt;
        public string Pinyin { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 组词（适用于：组词）
        /// &lt;/summary&gt;
        public List&lt;string&gt; WordCombinations { get; set; } = new List&lt;string&gt;();

        /// &lt;summary&gt;
        /// 短语/成语内容（适用于：短语、成语）
        /// &lt;/summary&gt;
        public string Phrase { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 句子内容（适用于：句子）
        /// &lt;/summary&gt;
        public string Sentence { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 古诗标题（适用于：古诗）
        /// &lt;/summary&gt;
        public string PoemTitle { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 古诗作者（适用于：古诗）
        /// &lt;/summary&gt;
        public string PoemAuthor { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 古诗朝代（适用于：古诗）
        /// &lt;/summary&gt;
        public string PoemDynasty { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 古诗内容（适用于：古诗）
        /// &lt;/summary&gt;
        public List&lt;string&gt; PoemLines { get; set; } = new List&lt;string&gt;();

        /// &lt;summary&gt;
        /// 释义/解释（适用于所有类型）
        /// &lt;/summary&gt;
        public string Meaning { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 例句/示例（适用于：生字、组词、短语）
        /// &lt;/summary&gt;
        public string Example { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 笔画数（适用于：生字）
        /// &lt;/summary&gt;
        public string StrokeCount { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 部首（适用于：生字）
        /// &lt;/summary&gt;
        public string Radical { get; set; } = string.Empty;

        /// &lt;summary&gt;
        /// 难度级别
        /// &lt;/summary&gt;
        public int DifficultyLevel { get; set; } = 1;

        /// &lt;inheritdoc/&gt;
        public override string GetMainContent()
        {
            return ItemType switch
            {
                ChineseItemType.Character => Character,
                ChineseItemType.WordCombination => WordCombinations.FirstOrDefault() ?? Phrase,
                ChineseItemType.Phrase => Phrase,
                ChineseItemType.Idiom => Phrase,
                ChineseItemType.Sentence => Sentence,
                ChineseItemType.Poem => PoemTitle,
                _ => Character
            };
        }

        /// &lt;inheritdoc/&gt;
        public override string GetDisplayText()
        {
            var parts = new List&lt;string&gt;();
            
            switch (ItemType)
            {
                case ChineseItemType.Character:
                    if (!string.IsNullOrWhiteSpace(Pinyin))
                        parts.Add($"拼音: {Pinyin}");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add($"释义: {Meaning}");
                    if (!string.IsNullOrWhiteSpace(StrokeCount))
                        parts.Add($"笔画: {StrokeCount}画");
                    if (!string.IsNullOrWhiteSpace(Radical))
                        parts.Add($"部首: {Radical}");
                    if (!string.IsNullOrWhiteSpace(Example))
                        parts.Add($"示例: {Example}");
                    break;

                case ChineseItemType.WordCombination:
                    if (WordCombinations.Count &gt; 0)
                        parts.Add($"组词: {string.Join("、", WordCombinations)}");
                    if (!string.IsNullOrWhiteSpace(Meaning))
                        parts.Add($"释义: {Meaning}");
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
                    if (PoemLines.Count &gt; 0)
                        parts.Add($"内容: {string.Join("，", PoemLines.Take(2))}...");
                    break;
            }

            return string.Join(" | ", parts);
        }

        /// &lt;inheritdoc/&gt;
        public override string GetPronunciation() => Pinyin;
    }

    /// &lt;summary&gt;
    /// 语文学习项类型
    /// &lt;/summary&gt;
    public enum ChineseItemType
    {
        Character,      // 生字
        WordCombination,// 组词
        Phrase,         // 短语
        Idiom,          // 成语
        Sentence,       // 句子
        Poem            // 古诗
    }
}

