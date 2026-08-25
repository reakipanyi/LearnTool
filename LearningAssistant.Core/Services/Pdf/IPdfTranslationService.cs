using System.Drawing;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// PDF翻译服务接口 - 提供文本和图像的翻译功能
    /// 支持OCR识别后翻译
    /// </summary>
    public interface IPdfTranslationService
    {
        /// <summary>
        /// 翻译服务是否可用
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// 翻译文本
        /// </summary>
        /// <param name="text">要翻译的文本</param>
        /// <returns>翻译结果，失败返回null</returns>
        Task<string?> TranslateAsync(string text);

        /// <summary>
        /// OCR识别图片内容并翻译
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <returns>元组(原始识别文本, 翻译文本)</returns>
        Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image);

        /// <summary>
        /// OCR识别图片指定区域内容并翻译
        /// </summary>
        /// <param name="image">图片Bitmap</param>
        /// <param name="region">要识别的矩形区域</param>
        /// <returns>元组(原始识别文本, 翻译文本)</returns>
        Task<(string? Original, string? Translation)> OcrAndTranslateAsync(Bitmap image, Rectangle region);
    }
}
