namespace LearningAssistant.Models.Pdf
{
    /// <summary>
    /// PDF 阅读器交互偏好，按用户持久化。
    /// 只记录“操作完成后怎么回到默认态”这类行为开关，不涉及标注数据本身。
    /// </summary>
    public sealed class PdfReaderInteractionPreferences
    {
        /// <summary>
        /// 画完一次标注后是否自动回到选择模式；默认关闭（即默认连续使用当前工具）。
        /// 开启后每次落笔完成会回到选择模式。
        /// </summary>
        public bool AutoReturnToSelectAfterDraw { get; set; }

        /// <summary>把外部读入的值整理为当前版本的偏好对象。</summary>
        public PdfReaderInteractionPreferences Normalize()
        {
            return new PdfReaderInteractionPreferences
            {
                AutoReturnToSelectAfterDraw = AutoReturnToSelectAfterDraw
            };
        }
    }
}
