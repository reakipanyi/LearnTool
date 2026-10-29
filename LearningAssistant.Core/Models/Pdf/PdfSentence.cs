namespace LearningAssistant.Models.Pdf
{
    /// <summary>
    /// 朗读用句子：由同一页的若干文本行（或 OCR 段落块）合并而成，
    /// 保留行框用于朗读时的同步高亮，不写入标注/高亮数据。
    /// </summary>
    public sealed class PdfSentence
    {
        /// <summary>所属页码（0 基）。</summary>
        public int PageIndex { get; set; }

        /// <summary>页内序号（0 基），用于断点续听与上一句/下一句定位。</summary>
        public int IndexInPage { get; set; }

        /// <summary>朗读文本（跨行已拼接为完整句子）。</summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>组成该句的文本行框（归一化坐标），用于绘制高亮。</summary>
        public List<PdfTextLine> Lines { get; set; } = new();
    }
}
