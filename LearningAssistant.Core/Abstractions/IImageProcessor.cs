using LearningAssistant.Abstractions;

namespace LearningAssistant.Services.Pdf
{
    /// <summary>
    /// 图像处理抽象接口 - 提供平台无关的图像操作
    /// </summary>
    public interface IImageProcessor
    {
        /// <summary>
        /// 裁剪图像
        /// </summary>
        /// <param name="imageData">PNG 格式源图像字节数组</param>
        /// <param name="region">裁剪区域</param>
        /// <returns>裁剪后的 PNG 格式字节数组，失败返回 null</returns>
        byte[]? CropImage(byte[] imageData, RectInt region);

        /// <summary>
        /// 获取图像尺寸
        /// </summary>
        /// <param name="imageData">PNG 格式图像字节数组</param>
        /// <returns>(宽度, 高度)，无效图像返回 (0, 0)</returns>
        (int Width, int Height) GetImageSize(byte[] imageData);

        /// <summary>
        /// 获取系统 DPI 缩放因子（相对于 96 DPI）
        /// </summary>
        float GetDpiScaleFactor();
    }
}
