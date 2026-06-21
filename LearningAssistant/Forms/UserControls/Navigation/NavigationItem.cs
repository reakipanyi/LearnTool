namespace LearningAssistant.Forms.UserControls.Navigation
{
    /// <summary>
    /// 导航项数据模型
    /// </summary>
    public class NavigationItem
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// 图标（emoji 或文字图标）
        /// </summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>
        /// 显示文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 描述/提示
        /// </summary>
        public string Tooltip { get; set; } = string.Empty;

        /// <summary>
        /// 排序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否显示
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 分组（用于分隔线）
        /// </summary>
        public string Group { get; set; } = "default";
    }
}
