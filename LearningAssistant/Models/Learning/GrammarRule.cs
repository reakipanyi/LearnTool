namespace LearningAssistant.Models.Learning
{
    /// <summary>
    /// 语法规则学习项
    /// 支持中文语法和英文语法
    /// </summary>
    public class GrammarRule : LearningItem
    {
        /// <summary>
        /// 语法点名称/标题
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// 语言类型（chinese/english）
        /// </summary>
        public string Language { get; set; } = "chinese";

        /// <summary>
        /// 语法分类（如：时态、从句、词性、句子成分等）
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// 语法规则定义/说明
        /// </summary>
        public string Definition { get; set; } = string.Empty;

        /// <summary>
        /// 规则详细解释
        /// </summary>
        public string Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 结构公式/公式表示
        /// </summary>
        public string Formula { get; set; } = string.Empty;

        /// <summary>
        /// 例句1
        /// </summary>
        public string Example1 { get; set; } = string.Empty;

        /// <summary>
        /// 例句1翻译/解析
        /// </summary>
        public string Example1Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 例句2
        /// </summary>
        public string Example2 { get; set; } = string.Empty;

        /// <summary>
        /// 例句2翻译/解析
        /// </summary>
        public string Example2Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 例句3
        /// </summary>
        public string Example3 { get; set; } = string.Empty;

        /// <summary>
        /// 例句3翻译/解析
        /// </summary>
        public string Example3Explanation { get; set; } = string.Empty;

        /// <summary>
        /// 常见错误/易错点
        /// </summary>
        public string CommonMistakes { get; set; } = string.Empty;

        /// <summary>
        /// 注意事项/使用要点
        /// </summary>
        public string Tips { get; set; } = string.Empty;

        /// <summary>
        /// 特殊用法
        /// </summary>
        public string SpecialUsage { get; set; } = string.Empty;

        /// <summary>
        /// 相关语法点
        /// </summary>
        public string RelatedRules { get; set; } = string.Empty;

        /// <summary>
        /// 难度等级（1-5）
        /// </summary>
        public int DifficultyLevel { get; set; } = 1;

        /// <summary>
        /// 适用阶段（小学、初中、高中、大学等）
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <inheritdoc/>
        public override string GetMainContent() => Title;

        /// <inheritdoc/>
        public override string GetDisplayText()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Category))
                parts.Add($"分类: {Category}");
            if (!string.IsNullOrWhiteSpace(Level))
                parts.Add($"阶段: {Level}");
            if (!string.IsNullOrWhiteSpace(Definition))
                parts.Add($"定义: {Definition}");
            if (!string.IsNullOrWhiteSpace(Formula))
                parts.Add($"公式: {Formula}");
            if (!string.IsNullOrWhiteSpace(Explanation))
                parts.Add($"解释: {Explanation}");
            if (!string.IsNullOrWhiteSpace(Example1))
                parts.Add($"例1: {Example1}");
            if (!string.IsNullOrWhiteSpace(Example1Explanation))
                parts.Add($"解析1: {Example1Explanation}");
            if (!string.IsNullOrWhiteSpace(Example2))
                parts.Add($"例2: {Example2}");
            if (!string.IsNullOrWhiteSpace(Example2Explanation))
                parts.Add($"解析2: {Example2Explanation}");
            if (!string.IsNullOrWhiteSpace(Example3))
                parts.Add($"例3: {Example3}");
            if (!string.IsNullOrWhiteSpace(Example3Explanation))
                parts.Add($"解析3: {Example3Explanation}");
            if (!string.IsNullOrWhiteSpace(CommonMistakes))
                parts.Add($"易错点: {CommonMistakes}");
            if (!string.IsNullOrWhiteSpace(Tips))
                parts.Add($"提示: {Tips}");
            if (!string.IsNullOrWhiteSpace(SpecialUsage))
                parts.Add($"特殊用法: {SpecialUsage}");
            if (!string.IsNullOrWhiteSpace(RelatedRules))
                parts.Add($"相关语法: {RelatedRules}");

            return string.Join("\n", parts);
        }

        /// <inheritdoc/>
        public override string GetPronunciation() => string.Empty;

        /// <inheritdoc/>
        public override string GetDisplayStruct()
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Category))
                parts.Add("分类:?");
            if (!string.IsNullOrWhiteSpace(Level))
                parts.Add("阶段:?");
            if (!string.IsNullOrWhiteSpace(Definition))
                parts.Add("定义:?");
            if (!string.IsNullOrWhiteSpace(Formula))
                parts.Add("公式:?");
            if (!string.IsNullOrWhiteSpace(Explanation))
                parts.Add("解释:?");
            if (!string.IsNullOrWhiteSpace(Example1))
                parts.Add("例1:?");
            if (!string.IsNullOrWhiteSpace(Example1Explanation))
                parts.Add("解析1:?");
            if (!string.IsNullOrWhiteSpace(Example2))
                parts.Add("例2:?");
            if (!string.IsNullOrWhiteSpace(Example2Explanation))
                parts.Add("解析2:?");
            if (!string.IsNullOrWhiteSpace(Example3))
                parts.Add("例3:?");
            if (!string.IsNullOrWhiteSpace(Example3Explanation))
                parts.Add("解析3:?");
            if (!string.IsNullOrWhiteSpace(CommonMistakes))
                parts.Add("易错点:?");
            if (!string.IsNullOrWhiteSpace(Tips))
                parts.Add("提示:?");
            if (!string.IsNullOrWhiteSpace(SpecialUsage))
                parts.Add("特殊用法:?");
            if (!string.IsNullOrWhiteSpace(RelatedRules))
                parts.Add("相关语法:?");

            return string.Join("\n", parts);
        }
    }
}
