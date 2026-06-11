namespace LearningAssistant.Views
{
    /// <summary>
    /// 结果视图接口 - 提供学习结果展示界面的显示和交互功能
    /// </summary>
    public interface IResultView
    {
        /// <summary>
        /// 正确率文本
        /// </summary>
        string AccuracyRate { set; }

        /// <summary>
        /// 已掌握项数文本
        /// </summary>
        string KnownItems { set; }

        /// <summary>
        /// 未掌握项数文本
        /// </summary>
        string UnknownItems { set; }

        /// <summary>
        /// 统计信息文本
        /// </summary>
        string Statistics { set; }

        /// <summary>
        /// 复习未知项点击事件
        /// </summary>
        event EventHandler? ReviewUnknownClicked;

        /// <summary>
        /// 关闭点击事件
        /// </summary>
        event EventHandler? CloseClicked;

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 关闭结果视图
        /// </summary>
        void CloseView();
    }
}
