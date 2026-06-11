namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// OCR服务接口 - 提供图像文字识别功能
    /// 用于从扫描版PDF或截图中提取文字
    /// </summary>
    public interface IOcrService
    {
        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <returns>识别出的文本</returns>
        Task<string> RecognizeTextAsync(Bitmap image);

        /// <summary>
        /// 识别图片指定区域的文字
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <param name="region">要识别的矩形区域</param>
        /// <returns>识别出的文本</returns>
        Task<string> RecognizeTextAsync(Bitmap image, Rectangle region);

        /// <summary>
        /// OCR服务是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 初始化失败时的错误信息
        /// </summary>
        string? InitErrorMessage { get; }

        /// <summary>
        /// 当前设置的OCR语言
        /// </summary>
        string CurrentLanguage { get; }

        /// <summary>
        /// 设置OCR识别语言
        /// </summary>
        /// <param name="language">语言代码（如 eng, chi_sim, chi_tra, jpn, kor）</param>
        /// <returns>设置成功返回true</returns>
        bool SetLanguage(string language);
    }
}
