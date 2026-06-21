namespace LearningAssistant.Models.DragDrop
{
    /// <summary>
    /// 拖拽数据类型
    /// </summary>
    public enum DragDataType
    {
        /// <summary>
        /// 文件
        /// </summary>
        File,

        /// <summary>
        /// 文本
        /// </summary>
        Text,

        /// <summary>
        /// HTML
        /// </summary>
        Html,

        /// <summary>
        /// 图片
        /// </summary>
        Image,

        /// <summary>
        /// 自定义对象
        /// </summary>
        Custom,

        /// <summary>
        /// 学习项
        /// </summary>
        LearningItem,

        /// <summary>
        /// 书签
        /// </summary>
        Bookmark,

        /// <summary>
        /// 笔记
        /// </summary>
        Note
    }

    /// <summary>
    /// 拖拽数据
    /// </summary>
    public class DragData
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        public DragDataType DataType { get; set; }

        /// <summary>
        /// 文件路径列表（当 DataType 为 File 时）
        /// </summary>
        public List<string> FilePaths { get; set; } = new();

        /// <summary>
        /// 文本内容（当 DataType 为 Text 或 Html 时）
        /// </summary>
        public string TextContent { get; set; } = string.Empty;

        /// <summary>
        /// 自定义数据
        /// </summary>
        public object? CustomData { get; set; }

        /// <summary>
        /// 数据格式名
        /// </summary>
        public string FormatName { get; set; } = string.Empty;

        /// <summary>
        /// 源控件标识
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// 目标控件标识
        /// </summary>
        public string? TargetId { get; set; }

        /// <summary>
        /// 拖拽效果
        /// </summary>
        public DragDropEffect AllowedEffect { get; set; }
    }

    /// <summary>
    /// 拖拽效果
    /// </summary>
    [Flags]
    public enum DragDropEffect
    {
        /// <summary>
        /// 无
        /// </summary>
        None = 0,

        /// <summary>
        /// 复制
        /// </summary>
        Copy = 1,

        /// <summary>
        /// 移动
        /// </summary>
        Move = 2,

        /// <summary>
        /// 链接
        /// </summary>
        Link = 4,

        /// <summary>
        /// 全部
        /// </summary>
        All = Copy | Move | Link
    }

    /// <summary>
    /// 拖拽事件参数
    /// </summary>
    public class DragDropEventArgs : EventArgs
    {
        /// <summary>
        /// 拖拽数据
        /// </summary>
        public DragData Data { get; }

        /// <summary>
        /// 是否处理
        /// </summary>
        public bool Handled { get; set; }

        /// <summary>
        /// 拖拽效果
        /// </summary>
        public DragDropEffect Effect { get; set; }

        public DragDropEventArgs(DragData data)
        {
            Data = data;
        }
    }
}
