namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF OCR（光学字符识别）服务接口 - 从图像中提取文字
    /// </summary>
    public interface IPdfOcrService
    {
        /// <summary>
        /// OCR服务是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 初始化失败时的错误信息
        /// </summary>
        string? InitErrorMessage { get; }

        /// <summary>
        /// 识别图片中的文字
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <returns>识别出的文本，失败返回null</returns>
        Task<string?> RecognizeTextAsync(Bitmap image);

        /// <summary>
        /// 识别图片指定区域的文字
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <param name="region">要识别的矩形区域</param>
        /// <returns>识别出的文本，失败返回null</returns>
        Task<string?> RecognizeTextAsync(Bitmap image, Rectangle region);

        /// <summary>
        /// 设置OCR识别语言
        /// </summary>
        /// <param name="language">语言代码（如 eng, chi_sim）</param>
        /// <returns>设置成功返回true</returns>
        bool SetLanguage(string language);
    }
}
