namespace LearningAssistant.Models.Pdf
{
    /// <summary>
    /// 统一撤销栈中记录的操作类型。
    /// 用于工具栏撤销按钮按时间顺序智能撤销最近一次操作（画笔或高亮）。
    /// </summary>
    public enum UndoActionKind
    {
        /// <summary>
        /// 画笔/标注绘制操作
        /// </summary>
        Stroke,

        /// <summary>
        /// 高亮添加/删除操作
        /// </summary>
        Highlight
    }
}
