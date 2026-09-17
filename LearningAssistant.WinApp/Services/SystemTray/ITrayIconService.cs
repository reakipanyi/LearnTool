namespace LearningAssistant.Services.SystemTray
{
    /// <summary>
    /// 系统托盘服务接口
    /// 提供系统托盘图标、菜单、后台运行等功能
    /// </summary>
    public interface ITrayIconService
    {
        /// <summary>
        /// 是否显示系统托盘图标
        /// </summary>
        bool ShowTrayIcon { get; set; }

        /// <summary>
        /// 最小化时是否隐藏到托盘
        /// </summary>
        bool MinimizeToTray { get; set; }

        /// <summary>
        /// 关闭时是否最小化到托盘（后台运行）
        /// </summary>
        bool CloseToTray { get; set; }

        /// <summary>
        /// 初始化系统托盘
        /// </summary>
        /// <param name="mainWindow">主窗口</param>
        void Initialize(Form mainWindow);

        /// <summary>
        /// 显示托盘图标
        /// </summary>
        void Show();

        /// <summary>
        /// 隐藏托盘图标
        /// </summary>
        void Hide();

        /// <summary>
        /// 显示主窗口
        /// </summary>
        void ShowMainWindow();

        /// <summary>
        /// 隐藏主窗口到托盘
        /// </summary>
        void HideToTray();

        /// <summary>
        /// 显示通知气泡
        /// </summary>
        /// <param name="title">标题</param>
        /// <param name="message">消息</param>
        /// <param name="timeout">超时时间（毫秒）</param>
        void ShowNotification(string title, string message, int timeout = 3000);

        /// <summary>
        /// 添加菜单项
        /// </summary>
        /// <param name="text">菜单项文本</param>
        /// <param name="onClick">点击事件</param>
        /// <param name="index">插入位置（-1表示最后）</param>
        void AddMenuItem(string text, EventHandler onClick, int index = -1);

        /// <summary>
        /// 添加分隔线
        /// </summary>
        void AddSeparator(int index = -1);

        /// <summary>
        /// 移除菜单项
        /// </summary>
        /// <param name="text">菜单项文本</param>
        void RemoveMenuItem(string text);

        /// <summary>
        /// 设置托盘图标
        /// </summary>
        /// <param name="icon">图标</param>
        void SetIcon(Icon icon);

        /// <summary>
        /// 设置托盘提示文本
        /// </summary>
        /// <param name="text">提示文本</param>
        void SetTooltip(string text);

        /// <summary>
        /// 获取托盘右键菜单
        /// </summary>
        /// <returns>ContextMenuStrip 对象</returns>
        ContextMenuStrip? GetContextMenu();

        /// <summary>
        /// 更新菜单项文本
        /// </summary>
        /// <param name="oldText">原文本</param>
        /// <param name="newText">新文本</param>
        /// <param name="enabled">是否启用</param>
        void UpdateMenuItem(string oldText, string newText, bool enabled = true);

        /// <summary>
        /// 清理资源
        /// </summary>
        void Cleanup();

        /// <summary>
        /// 托盘图标双击事件
        /// </summary>
        event EventHandler? TrayDoubleClick;

        /// <summary>
        /// 主窗口显示状态变更事件
        /// </summary>
        event EventHandler<bool>? VisibilityChanged;
    }
}
