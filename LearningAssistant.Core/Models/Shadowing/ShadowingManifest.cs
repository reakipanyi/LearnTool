namespace LearningAssistant.Models.Shadowing
{
    /// <summary>
    /// 某个 PDF 页面文本的分句清单，用于稳定映射句子与跟读录音。
    /// </summary>
    public class ShadowingManifest
    {
        public string PdfPath { get; set; } = string.Empty;

        public int PageIndex { get; set; }

        /// <summary>
        /// 生成该清单时的分句器版本；版本变化时可用于判断是否需要重新分句。
        /// </summary>
        public string SegmenterVersion { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public List<ShadowingSentence> Sentences { get; set; } = new();
    }
}
