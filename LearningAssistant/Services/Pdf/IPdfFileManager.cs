namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF文件管理服务接口 - 管理PDF文件夹加载、文件切换和会话恢复
    /// 支持PDF和图片模式（图片模式用于扫描版PDF）
    /// </summary>
    public interface IPdfFileManager
    {
        /// <summary>
        /// 加载文件夹（包含多个PDF或图片文件）
        /// </summary>
        /// <param name="folderPath">文件夹路径</param>
        void LoadFolder(string folderPath);

        /// <summary>
        /// 异步加载指定文件
        /// </summary>
        /// <param name="fileName">文件名（不含路径）</param>
        Task LoadFileAsync(string fileName);

        /// <summary>
        /// 保存当前会话状态
        /// 包括当前文件路径、页面索引、文件夹路径等
        /// </summary>
        void SaveSession();

        /// <summary>
        /// 加载会话状态
        /// </summary>
        /// <returns>元组(文件夹路径, 文件路径, 文件页码映射表)</returns>
        (string? Folder, string? FilePath, Dictionary<string, int>? FilePageMap) LoadSession();

        /// <summary>
        /// 加载上次会话并恢复状态
        /// </summary>
        void LoadLastSessionAndRestore();

        /// <summary>
        /// 当前打开的文件完整路径
        /// </summary>
        string CurrentFilePath { get; }

        /// <summary>
        /// 当前页面索引（从0开始）
        /// </summary>
        int CurrentPageIndex { get; set; }

        /// <summary>
        /// 设置当前页面索引并保存会话
        /// </summary>
        void SetCurrentPageIndex(int pageIndex);

        /// <summary>
        /// 是否为图片模式（用于扫描版PDF）
        /// </summary>
        bool IsImageMode { get; }

        /// <summary>
        /// 当前文件夹下的所有图片文件列表
        /// </summary>
        List<string> ImageFiles { get; }

        /// <summary>
        /// 文件加载完成事件
        /// </summary>
        event EventHandler<FileLoadedEventArgs>? FileLoaded;

        /// <summary>
        /// 文件夹加载完成事件
        /// </summary>
        event EventHandler<FolderLoadedEventArgs>? FolderLoaded;
    }

    /// <summary>
    /// 文件加载完成事件参数
    /// </summary>
    public class FileLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 加载的文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 是否为图片模式
        /// </summary>
        public bool IsImageMode { get; set; }

        /// <summary>
        /// 文件总页数
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// 初始页面索引
        /// </summary>
        public int InitialPageIndex { get; set; }
    }

    /// <summary>
    /// 文件夹加载完成事件参数
    /// </summary>
    public class FolderLoadedEventArgs : EventArgs
    {
        /// <summary>
        /// 文件夹路径
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// 文件夹下的文件列表
        /// </summary>
        public List<string> Files { get; set; } = new List<string>();
    }
}
