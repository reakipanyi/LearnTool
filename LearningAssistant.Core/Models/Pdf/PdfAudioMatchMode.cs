namespace LearningAssistant.Models.Pdf
{
    /// <summary>PDF 与音频的整体匹配方式。</summary>
    public enum PdfAudioMatchMode
    {
        /// <summary>一个 PDF 对应多个音频：按规则/手动把音频分配到各页</summary>
        PerPage = 0,

        /// <summary>一个 PDF 只对应一个音频：整篇共用一个音频，翻页不切换</summary>
        SingleAudio = 1
    }
}
