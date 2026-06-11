namespace LearningAssistant.Views
{
    /// <summary>
    /// 主视图接口 - 提供主窗口的显示和交互功能
    /// </summary>
    public interface IMainView
    {
        /// <summary>
        /// 当前选中的用户名
        /// </summary>
        string SelectedUser { get; set; }

        /// <summary>
        /// 学习进度摘要文本
        /// </summary>
        string ProgressSummary { get; set; }

        /// <summary>
        /// 状态栏文本
        /// </summary>
        string StatusText { get; set; }

        /// <summary>
        /// 用户切换事件
        /// </summary>
        event EventHandler? UserChanged;

        /// <summary>
        /// 打开学习窗口点击事件
        /// </summary>
        event EventHandler? OpenLearningWindowClicked;

        /// <summary>
        /// 打开设置点击事件
        /// </summary>
        event EventHandler? OpenSettingsClicked;

        /// <summary>
        /// 打开编辑器点击事件
        /// </summary>
        event EventHandler? OpenEditorClicked;

        /// <summary>
        /// 标签页切换事件
        /// </summary>
        event EventHandler? TabChanged;

        /// <summary>
        /// 新建用户点击事件
        /// </summary>
        event EventHandler? NewUserClicked;

        /// <summary>
        /// 显示消息对话框
        /// </summary>
        /// <param name="msg">消息内容</param>
        void ShowMessage(string msg);

        /// <summary>
        /// 刷新用户列表
        /// </summary>
        /// <param name="users">用户ID列表</param>
        void RefreshUserList(IEnumerable<string> users);

        /// <summary>
        /// 更新状态文本
        /// </summary>
        /// <param name="status">状态文本</param>
        void UpdateStatus(string status);

        /// <summary>
        /// 更新连续学习天数信息
        /// </summary>
        /// <param name="consecutiveDays">连续学习天数</param>
        /// <param name="studyTimeSummary">学习时间摘要</param>
        void UpdateStreakInfo(int consecutiveDays, string studyTimeSummary);
    }
}
