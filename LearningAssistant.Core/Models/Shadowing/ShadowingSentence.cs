namespace LearningAssistant.Models.Shadowing
{
    /// <summary>
    /// 跟读分句结果中的单个句子。
    /// StartOffset/Length 相对于规范化后的段落文本（而非原始 PDF 文本）。
    /// </summary>
    public class ShadowingSentence
    {
        /// <summary>
        /// 稳定句子标识：规范化文本 + 同文本出现序号，文本不变时分句变化仍可命中旧录音。
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 在当前段落中的顺序（从 0 开始）。
        /// </summary>
        public int Index { get; set; }

        public int StartOffset { get; set; }

        public int Length { get; set; }

        public string Text { get; set; } = string.Empty;
    }
}
