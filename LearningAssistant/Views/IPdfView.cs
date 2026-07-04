namespace LearningAssistant.Views
{
    /// <summary>
    /// PDF视图接口 - 提供PDF阅读界面的显示和交互功能
    /// 支持PDF模式、图片模式和OCR标注
    /// </summary>
    public interface IPdfView
    {
        /// <summary>
        /// 设置是否为图片模式（扫描版PDF）
        /// </summary>
        /// <param name="isImageMode">是否为图片模式</param>
        void SetImageMode(bool isImageMode);

        /// <summary>
        /// 获取是否为图片模式
        /// </summary>
        /// <returns>是图片模式返回true</returns>
        bool GetImageMode();

        /// <summary>
        /// 设置文件列表
        /// </summary>
        /// <param name="files">文件路径列表</param>
        void SetFileList(IEnumerable<string> files);

        /// <summary>
        /// 设置页面总数
        /// </summary>
        /// <param name="count">页面总数</param>
        void SetPageCount(int count);

        /// <summary>
        /// 设置当前页面索引
        /// </summary>
        /// <param name="pageIndex">页面索引（从0开始）</param>
        void SetCurrentPageIndex(int pageIndex);

        /// <summary>
        /// 设置当前PDF文件路径
        /// </summary>
        /// <param name="filePath">PDF文件路径</param>
        void SetCurrentPdfPath(string filePath);

        /// <summary>
        /// 设置指定页面的文本内容
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="text">页面文本</param>
        void SetPageText(int pageIndex, string text);

        /// <summary>
        /// 显示图片
        /// </summary>
        /// <param name="bmp">要显示的Bitmap图像</param>
        void DisplayImage(Bitmap bmp);

        /// <summary>
        /// 设置第二页图像（双页模式）
        /// </summary>
        /// <param name="bmp">第二页Bitmap图像</param>
        void SetSecondPageImage(Bitmap? bmp);

        /// <summary>
        /// 显示警告消息
        /// </summary>
        /// <param name="message">警告消息内容</param>
        void ShowWarning(string message);

        /// <summary>
        /// 显示错误消息
        /// </summary>
        /// <param name="message">错误消息内容</param>
        void ShowError(string message);


        /// <summary>
        /// 设置加载状态
        /// </summary>
        /// <param name="isLoading">是否正在加载</param>
        void SetLoadingState(bool isLoading);

        /// <summary>
        /// 显示消息
        /// </summary>
        /// <param name="message">消息内容</param>
        void ShowMessage(string message);

        /// <summary>
        /// 显示带标题的消息
        /// </summary>
        /// <param name="message">消息内容</param>
        /// <param name="title">消息标题</param>
        void ShowMessage(string message, string title);

        /// <summary>
        /// 显示加载提示
        /// </summary>
        /// <param name="message">加载提示文本</param>
        void ShowLoading(string message);

        /// <summary>
        /// 隐藏加载提示
        /// </summary>
        void HideLoading();

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        /// <param name="message">确认消息内容</param>
        /// <param name="title">对话框标题</param>
        /// <returns>用户点击确定返回true</returns>
        bool ShowConfirm(string message, string title);

        /// <summary>
        /// 显示保存文件对话框
        /// </summary>
        /// <param name="defaultFileName">默认文件名</param>
        /// <param name="filter">文件过滤器（如 "Excel Files|*.xlsx"）</param>
        /// <returns>用户选择的文件路径，取消返回null</returns>
        string? ShowSaveFileDialog(string defaultFileName, string filter);

        /// <summary>
        /// 清空缩略图列表
        /// </summary>
        void ClearThumbnails();

        /// <summary>
        /// 添加缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="thumbnail">缩略图</param>
        void AddThumbnail(int pageIndex, Image thumbnail);

        /// <summary>
        /// 添加缩略图（带所属目录路径，用于图片模式按目录分组展示）
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        /// <param name="thumbnail">缩略图</param>
        /// <param name="directoryPath">所属目录路径，为空时不分组</param>
        void AddThumbnail(int pageIndex, Image thumbnail, string directoryPath);

        /// <summary>
        /// 高亮指定页面的缩略图
        /// </summary>
        /// <param name="pageIndex">页面索引</param>
        void HighlightThumbnail(int pageIndex);

        /// <summary>
        /// 启用夜间模式
        /// </summary>
        void NightMode();

        /// <summary>
        /// 设置当前OCR语言
        /// </summary>
        /// <param name="language">语言代码</param>
        void SetCurrentLanguage(string language);

        /// <summary>
        /// 更新语言按钮文本
        /// </summary>
        /// <param name="text">按钮文本</param>
        void UpdateLanguageButtonText(string text);

        /// <summary>
        /// 获取当前OCR语言
        /// </summary>
        /// <returns>语言代码</returns>
        string GetCurrentLanguage();

        /// <summary>
        /// 获取当前选中的文件
        /// </summary>
        /// <returns>文件路径</returns>
        string GetSelectedFile();

        /// <summary>
        /// 获取页面文本
        /// </summary>
        /// <returns>页面文本内容</returns>
        string GetPageText();

        /// <summary>
        /// 获取翻译文本
        /// </summary>
        /// <returns>翻译内容</returns>
        string GetTranslationText();

        /// <summary>
        /// 获取原文文本
        /// </summary>
        /// <returns>原文内容</returns>
        string GetOriginalText();

        /// <summary>
        /// 设置翻译文本
        /// </summary>
        /// <param name="text">翻译文本</param>
        void SetTranslationText(string text);

        /// <summary>
        /// 设置原文文本
        /// </summary>
        /// <param name="text">原文文本</param>
        void SetOriginalText(string text);

        /// <summary>
        /// 设置OCR结果文本
        /// </summary>
        /// <param name="text">OCR识别结果</param>
        void SetOcrResultText(string text);


        /// <summary>
        /// 获取当前显示的图片
        /// </summary>
        /// <returns>当前图片，无则返回null</returns>
        Image? GetCurrentImage();

        /// <summary>
        /// 获取用户选择的矩形区域
        /// </summary>
        /// <returns>选择矩形，无则返回null</returns>
        Rectangle? GetSelectionRect();

        /// <summary>
        /// 获取显示区域矩形
        /// </summary>
        /// <returns>显示区域矩形</returns>
        Rectangle GetDisplayRect();

        /// <summary>
        /// 获取图片显示矩形
        /// </summary>
        /// <returns>图片显示矩形</returns>
        Rectangle GetImageDisplayRect();

        /// <summary>
        /// 显示OCR叠加层
        /// </summary>
        /// <param name="image">叠加图像，null则清除</param>
        void ShowOcrOverlay(Bitmap? image);

        /// <summary>
        /// 隐藏OCR叠加层
        /// </summary>
        void HideOcrOverlay();

        /// <summary>
        /// 设置图片文件列表
        /// </summary>
        /// <param name="imageFiles">图片文件路径列表</param>
        void SetImageList(IEnumerable<string> imageFiles);

        /// <summary>
        /// 文件选中事件
        /// </summary>
        event EventHandler? FileSelected;

        /// <summary>
        /// 页面切换事件
        /// </summary>
        event EventHandler? PageChanged;

        /// <summary>
        /// OCR选择完成事件
        /// </summary>
        event EventHandler? OcrSelectionComplete;



        /// <summary>
        /// 添加到学习列表事件
        /// </summary>
        event EventHandler? AddToLearningList;

        /// <summary>
        /// 添加到编辑器事件
        /// </summary>
        event EventHandler<AddToEditorEventArgs>? AddToEditor;

        /// <summary>
        /// 触发添加到编辑器
        /// </summary>
        /// <param name="text">文本内容</param>
        /// <param name="language">语言</param>
        void RaiseAddToEditor(string text, string language);

        /// <summary>
        /// 朗读原文事件
        /// </summary>
        event EventHandler? SpeakOriginal;

        /// <summary>
        /// 朗读翻译事件
        /// </summary>
        event EventHandler? SpeakTranslation;

        /// <summary>
        /// 朗读指定文本事件
        /// </summary>
        /// <param name="text">文本内容</param>
        event EventHandler<string>? SpeakText;


        /// <summary>
        /// 选择OCR区域点击事件
        /// </summary>
        event EventHandler? SelectOcrClicked;

        /// <summary>
        /// 翻译点击事件
        /// </summary>
        event EventHandler? TranslateClicked;

        /// <summary>
        /// 切换夜间模式点击事件
        /// </summary>
        event EventHandler? ToggleNightMode;

        /// <summary>
        /// OCR语言切换事件
        /// </summary>
        event EventHandler? LanguageChanged;

        /// <summary>
        /// AI问答按钮点击事件
        /// </summary>
        event EventHandler? AiQuestionAsked;

        /// <summary>
        /// 触发AI问答面板
        /// </summary>
        void RaiseAiQuestionAsked();

        /// <summary>
        /// 获取当前缩放级别（百分比）
        /// </summary>
        int GetZoomLevel();

        /// <summary>
        /// 设置缩放级别
        /// </summary>
        /// <param name="level">缩放级别（百分比，如100表示100%）</param>
        void SetZoomLevel(int level);

        /// <summary>
        /// 放大
        /// </summary>
        void ZoomIn();

        /// <summary>
        /// 缩小
        /// </summary>
        void ZoomOut();

        /// <summary>
        /// 重置缩放为100%
        /// </summary>
        void ResetZoom();

    }

    /// <summary>
    /// 添加到编辑器事件参数
    /// </summary>
    public class AddToEditorEventArgs : EventArgs
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 语言代码
        /// </summary>
        public string Language { get; set; } = string.Empty;
    }
}
